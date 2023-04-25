using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Threading
{
	/// <summary>
	/// 表示一个异步队列管道，队列管道内可缓冲多条消息而不阻塞
	/// </summary>
	public class PipelineQueue<T>
	{
		/// <summary>
		/// 管道是否为空
		/// </summary>
		public bool IsEmpty => this.valueQueue.IsEmpty;
		/// <summary>
		/// 获取管道中元素的数量
		/// </summary>
		public int Count => this.valueQueue.Count;
		/// <summary>
		/// 表示异步管道
		/// </summary>
		public PipelineQueue()
		{
			this.valueQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();
		}
		/// <summary>
		/// 通知异步接收者和发送者，管道已经被关闭
		/// </summary>
		public void Close()
		{
			this.close = true;
			while (true)
			{
				if (this.close)
				{
					return;
				}
				if (this.tcs == null)
				{  //没有等待任务的情况，需要二阶段确认
					return;
				}
				else
				{    //已经有正在等待的任务的情况，二阶段确认后，只需要在去除等待任务时加Read锁
					var t = this.tcs;
					if (t != null)
					{
						t.TrySetException(new ObjectDisposedException(nameof(PipelineQueue<T>)));
						if (Equals(System.Threading.Interlocked.CompareExchange(ref this.tcs, null, t), t)) return;
					}
				}
			}
		}
		/// <summary>
		/// 向管道中写入数据
		/// </summary>
		/// <param name="message"></param>
		public void WriteMessage(T message)
		{
			if (this.close)
			{
				throw new ObjectDisposedException(nameof(PipelineQueue<T>));
			}
			this.valueQueue.Enqueue(message);
			while (true)
			{
				if (this.tcs == null)
				{  //没有等待任务的情况，需要二阶段确认
					return;
				}
				else
				{    //已经有正在等待的任务的情况，二阶段确认后，只需要在去除等待任务时加Read锁
					var t = this.tcs;
					if (t != null)
					{
						t.TrySetResult(0);
						if (Equals(System.Threading.Interlocked.CompareExchange(ref this.tcs, null, t), t)) return;
					}
				}
				if (this.valueQueue.IsEmpty) return;
			}
		}
		/// <summary>
		/// 从管道取出数据，不允许多个线程同时进行等待
		/// </summary>
		/// <returns></returns>
		public async Task<T> ReadMessage()
		{
			if (this.close)
			{
				throw new ObjectDisposedException(nameof(PipelineQueue<T>));
			}
			var t_tcs = this.tcs;   //预先检查是否存在任务，可以避免加自旋锁
			if (t_tcs != null)
			{
				throw new Exception("不允许多个线程同时等待管道");
			}
			while (true)
			{
				if (this.valueQueue.TryDequeue(out var t))  //管道中已经有数据则直接返回
				{
					return t;
				}
				t_tcs = new TaskCompletionSource<int>();
				if (!Equals(System.Threading.Interlocked.CompareExchange(ref this.tcs, t_tcs, null), null)) throw new Exception("不允许多个线程同时等待管道");
				if (this.close)
				{
					throw new ObjectDisposedException(nameof(PipelineQueue<T>));
				}
				if (this.valueQueue.TryDequeue(out t))  //管道中已经有数据则直接返回
				{
					System.Threading.Interlocked.CompareExchange(ref this.tcs, null, t_tcs);
					t_tcs.TrySetResult(0);
					return t;
				}
				await t_tcs.Task;
				System.Threading.Interlocked.CompareExchange(ref this.tcs, null, t_tcs);
			}
		}
		/// <summary>
		/// 以指定的毫秒数超时值从管道取出数据，不允许多个线程同时进行等待
		/// </summary>
		/// <param name="time">等待的毫秒数</param>
		/// <returns></returns>
		/// <exception cref="TaskCanceledException">等待的任务超时</exception>
		public async Task<T> ReadMessage(int time)
		{
			if (this.close)
			{
				throw new ObjectDisposedException(nameof(PipelineQueue<T>));
			}
			var t_tcs = this.tcs;   //预先检查是否存在任务，可以避免加自旋锁
			if (t_tcs != null)
			{
				throw new Exception("不允许多个线程同时等待管道");
			}
			while (true)
			{
				if (this.valueQueue.TryDequeue(out var t))  //管道中已经有数据则直接返回
				{
					return t;
				}
				t_tcs = new TaskCompletionSource<int>();
				if (!Equals(System.Threading.Interlocked.CompareExchange(ref this.tcs, t_tcs, null), null)) throw new Exception("不允许多个线程同时等待管道");
				if (this.close)
				{
					throw new ObjectDisposedException(nameof(PipelineQueue<T>));
				}
				if (this.valueQueue.TryDequeue(out t))  //管道中已经有数据则直接返回
				{
					System.Threading.Interlocked.CompareExchange(ref this.tcs, null, t_tcs);
					t_tcs.TrySetResult(0);
					return t;
				}
				System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
				_=Task.Delay(time, cts.Token).ContinueWith(x => t_tcs.TrySetException(new TimeoutException()), cts.Token);
				await t_tcs.Task;
				cts.Cancel();
				System.Threading.Interlocked.CompareExchange(ref this.tcs, null, t_tcs);
			}
		}
		private readonly System.Collections.Concurrent.ConcurrentQueue<T> valueQueue;
		private volatile TaskCompletionSource<int> tcs;
		private volatile bool close = false;
	}
}
