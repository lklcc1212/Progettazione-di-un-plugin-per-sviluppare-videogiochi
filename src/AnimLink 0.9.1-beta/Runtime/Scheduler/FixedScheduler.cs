namespace AnimLink
{
    using System;
    using System.Collections.Generic;
    using static AnimLink.FrameScheduler;

    internal static class FixedScheduler
    {
        #region === Internal Fields ===

        private static readonly Dictionary<ushort, Func<ushort, bool>> _fixedFuncs = new(32);

        //保证遍历时可加入/删除元素
        private static readonly List<ushort> _pendingFixedRemovals = new(8);
        private static readonly Dictionary<ushort, Func<ushort, bool>> _pendingFixedAdds = new(8);

        private static bool _processingFixed = false;

        internal static int ActiveFixedFuncCount => _fixedFuncs.Count;

        #endregion ====================

        internal static void Tick()
        {
            SchedulerCore.ProcessDict(_fixedFuncs, _pendingFixedRemovals, _pendingFixedAdds, ref _processingFixed);
        }

        internal static ushort StartFixedUpdate(Func<ushort, bool> func, Action<ushort> onComplete = null)
        {
            ushort id = IDGenerator.NextID();

            Func<ushort, bool> wrapped = onComplete == null
                ? func
                : (key) =>
                {
                    bool alive = func(key);
                    if (!alive) onComplete(key);
                    return alive;
                };

            if (_processingFixed)
            {
                // 遍历期间不能修改 _fixedFuncs，推入 pending
                _pendingFixedAdds[id] = wrapped;
            }
            else
            {
                _fixedFuncs[id] = wrapped;
            }

            return id;
        }

        internal static void StopFixedUpdate(ushort id)
        {
            if (_processingFixed)
                _pendingFixedRemovals.Add(id);
            else
                _fixedFuncs.Remove(id);
        }

        internal static bool IsFixedUpdateRunning(ushort id)
        {
            if (id == 0) return false;
            return _fixedFuncs.ContainsKey(id);
        }
    }
}