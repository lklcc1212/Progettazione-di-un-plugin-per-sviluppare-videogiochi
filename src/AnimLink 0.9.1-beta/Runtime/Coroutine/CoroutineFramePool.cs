namespace AnimLink
{
    using System.Collections.Generic;

    internal static class CoroutineFramePool
    {
        private readonly static Queue<CoroutineScheduler.CoroutineFrame> _pool = new(4);

        public static CoroutineScheduler.CoroutineFrame Get()
        {
            if (_pool.Count == 0)
            {
                return new();
            }
            return _pool.Dequeue();
        }

        public static void Release(CoroutineScheduler.CoroutineFrame coroutineNode)
        {
            coroutineNode.Reset();
            _pool.Enqueue(coroutineNode);
        }
    }
}