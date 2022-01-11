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

        public AsyncLock(int count = 1)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException("count");
            if (count == 1) m_semaphore = new AsyncSemaphore(count);
            else m_semaphore = new AsyncSemaphoreConcurrency(count);
            m_releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                m_releaser :
#if NET5_0_OR_GREATER
                wait.ContinueWith(static (_, state) => new Releaser((AsyncLock)state),
#else
                wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
#endif
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
            protected readonly ConcurrentQueue<TaskCompletionSource<bool>> m_waiters = new ConcurrentQueue<TaskCompletionSource<bool>>();
            protected int m_currentCount;
            public AsyncSemaphore(int count)
            {
                m_currentCount = count;
            }
            public virtual Task WaitAsync()
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

            public virtual void Release()
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
            public virtual bool TryWait()
            {
                return System.Threading.Interlocked.CompareExchange(ref m_currentCount, 0, 1) == 1;
            }
        }
        class AsyncSemaphoreConcurrency : AsyncSemaphore
		{
            public AsyncSemaphoreConcurrency(int count) : base(count)
            {
            }
            public override Task WaitAsync()
            {
                if (System.Threading.Interlocked.Decrement(ref m_currentCount) > 0)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    System.Threading.Interlocked.Increment(ref m_currentCount);
                    var waiter = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
            public override void Release()
            {
                if (m_waiters.TryDequeue(out var toRelease))
                {
                    toRelease.SetResult(true);
                    return;
                }
                System.Threading.Interlocked.Increment(ref m_currentCount);
                if (m_waiters.TryDequeue(out toRelease))
                {
                    //再次检测队列中是否有等待，若有则再次加锁
                    if (System.Threading.Interlocked.Decrement(ref m_currentCount) > 0)
                    {
                        toRelease.SetResult(true);
                        return;
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref m_currentCount);
                        //加锁失败则将等待放回队列
                        m_waiters.Enqueue(toRelease);
                    }
                }
            }
            public override bool TryWait()
            {
                if (System.Threading.Interlocked.Decrement(ref m_currentCount) > 0)
				{
                    return true;
				}
				else
				{
                    System.Threading.Interlocked.Increment(ref m_currentCount);
                    return false;
                }
            }
        }
    }
}
