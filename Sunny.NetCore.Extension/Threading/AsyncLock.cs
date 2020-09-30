using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sunny.NetCore.Extension.Threading
{
    public class AsyncLock
    {
        private readonly AsyncSemaphore m_semaphore;
        private readonly Task<Releaser> m_releaser;

        public AsyncLock()
        {
            m_semaphore = new AsyncSemaphore();
            m_releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                m_releaser :
                wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                    this, System.Threading.CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        public bool TryLock(out Releaser releaser)
        {
            var taken = m_semaphore.TryWait();
            releaser = taken ? m_releaser.Result : default;
            return taken;
        }
        public struct Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

            public void Dispose()
            {
                if (m_toRelease != null)
                    m_toRelease.m_semaphore.Release();
            }
        }
        class AsyncSemaphore
        {
            private readonly ConcurrentQueue<TaskCompletionSource<bool>> m_waiters = new ConcurrentQueue<TaskCompletionSource<bool>>();
            private int m_currentCount = 1;

            public Task WaitAsync()
            {
                if (System.Threading.Interlocked.CompareExchange(ref m_currentCount, 0, 1) == 1)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }

            public void Release()
            {
                if (m_waiters.TryDequeue(out var toRelease))
                {
                    toRelease.SetResult(true);
                    return;
                }
                if (System.Threading.Interlocked.CompareExchange(ref m_currentCount, 1, 0) != 0)
                {
                    throw new Exception("解锁失败");
                }
                if (m_waiters.TryDequeue(out toRelease))
                {
                    //再次检测队列中是否有等待，若有则再次加锁
                    if (System.Threading.Interlocked.CompareExchange(ref m_currentCount, 0, 1) == 1)
                    {
                        toRelease.SetResult(true);
                        return;
                    }
                    else
                    {
                        //加锁失败则将等待放回队列
                        m_waiters.Enqueue(toRelease);
                    }
                }
            }
            public bool TryWait()
            {
                return System.Threading.Interlocked.CompareExchange(ref m_currentCount, 0, 1) == 1;
            }
        }
    }
}
