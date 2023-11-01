namespace HackerNewsStoriesApi.Utils
{
    // original source https://www.rocksolidknowledge.com/articles/locking-and-asyncawait

    public class TimedLock
    {
        private readonly SemaphoreSlim toLock;

        public TimedLock()
        {
            toLock = new SemaphoreSlim(1, 1);
        }

        public async Task<LockReleaser> Lock(TimeSpan timeout)
        {
            if (await toLock.WaitAsync(timeout))
            {
                return new LockReleaser(toLock);
            }
            throw new TimeoutException();
        }

        public struct LockReleaser : IDisposable
        {
            private readonly SemaphoreSlim toRelease;

            public LockReleaser(SemaphoreSlim toRelease)
            {
                this.toRelease = toRelease;
            }
            public void Dispose()
            {
                toRelease.Release();
            }
        }
    }
}
