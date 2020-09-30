using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Threading
{
    /// <summary>
    /// 异步缓冲区
    /// </summary>
	class AsyncBuffer<T> where T : class
	{
        //配置项：缓冲区容量，单位(行)
        private const int Capacity = 1000;
        //配置项：写入时缓冲区满后重试次数，单位(次)，此处可改进为读取时使用的异步等待模式，但会大幅提升架构复杂度，经过权衡采用定时重试方案更合适。
        private const int WriteWait = 50;
        //配置项：写入时缓冲区满后重试等待时间，单位(ms)
        private const int WriteWaitTime = 100;
        //配置项：读取时缓冲区中无数据的最大异步等待时间，单位(TimeSpan)
        private static readonly TimeSpan ReadWaitTime = new TimeSpan(0, 0, 10);
        private TaskCompletionSource<int> WaitTaskSource = new TaskCompletionSource<int>();
        private readonly ConcurrentQueue<T> DataStream = new ConcurrentQueue<T>();
        private int counter = 0;
        private Exception exception;
        /// <summary>
        /// 将新的一行数据添加到缓冲区
        /// </summary>
        public async ValueTask Enqueue(T row)
        {
            if (exception != null) throw new Exception("处理时发生了异常", exception);
            DataStream.Enqueue(row);
            WaitTaskSource.TrySetResult(default);

            //由于多线程环境检查积压是一个会影响并行能力的操作，所以每接收0xFF行检查一次缓冲区积压数量
            ++counter;
            if (counter > 0xFF)
            {
                //积压超限时连续检测5秒，如果一直处于超限状态则说明接收端出现了其它异常，防止无限期等待
                for (int i = 0; DataStream.Count > Capacity; ++i)
                {
                    if (exception != null) throw new Exception("处理时发生了异常", exception);
                    if (i > WriteWait) throw new Exception("数据接收端连续5秒没有消费数据，造成了异常积压");
                    WaitTaskSource.TrySetResult(default);
                    await Task.Delay(WriteWaitTime);
                }
                counter = 0;
            }
        }
        /// <summary>
        /// 通知接收方已经没有新的数据需要接收了
        /// </summary>
        public void Finish()
        {
            if (exception != null) throw new Exception("处理时发生了异常", exception);
            DataStream.Enqueue(null);
            Interlocked.MemoryBarrier();
            WaitTaskSource.TrySetResult(default);
        }
        /// <summary>
        /// 通知另一方由于异常而终止传输数据
        /// </summary>
        public void SetException(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (exception != null) return;
            exception = ex;
            DataStream.Enqueue(null);
            Interlocked.MemoryBarrier();
            WaitTaskSource.TrySetException(ex);
        }
        /// <summary>
        /// 从缓冲区读取一行，如果缓冲区为空则等待
        /// </summary>
        public async ValueTask<T> ReadRow()
        {
            T row;
            while (true)
            {
                //先读取一次
                if (DataStream.TryDequeue(out row))
                {
                    break;
                }
                //再以线程同步状态读取一次
                Interlocked.MemoryBarrier();
                if (DataStream.TryDequeue(out row))
                {
                    break;
                }
                //如果没有数据，则以线程同步模式先写入异步完成标记，然后再读取一次（此处顺序与数据发送端进行标记的顺序相反，防止多线程环境中的完成状态丢失），最后才进入异步等待。
                WaitTaskSource = new TaskCompletionSource<int>();
                Interlocked.MemoryBarrier();
                if (DataStream.TryDequeue(out row))
                {
                    break;
                }
                if (exception != null) throw new Exception("接收时发生了异常", exception);

                //异步等待设置超时值，防止数据发送端因其它错误造成接收端无限期等待
                var cancellationSource = new CancellationTokenSource();
                var timeout = Task.Delay(ReadWaitTime, cancellationSource.Token);
                if (await Task.WhenAny(timeout, WaitTaskSource.Task) == timeout)
                {
                    throw new Exception("数据发送端连续10秒没有产生数据，造成接收端异常等待");
                }
                else
                {
                    cancellationSource.Cancel();
                }
            }
            if (row == null)
            {
                if (exception != null) throw new Exception("接收时发生了异常", exception);
            }
            return row;
        }
    }
}
