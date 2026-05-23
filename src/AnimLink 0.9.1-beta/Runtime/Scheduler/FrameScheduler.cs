namespace AnimLink
{
    using System;
    using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    internal static class FrameScheduler
    {
        #region === Internal Fields ===

        private static readonly Dictionary<ushort, Func<ushort, bool>> _updateFuncs = new(32);

        //保证遍历时可加入/删除元素
        private static readonly List<ushort> _pendingUpdateRemovals = new(8);
        private static readonly Dictionary<ushort, Func<ushort, bool>> _pendingUpdateAdds = new(8);
#if UNITY_EDITOR
        private static readonly List<ushort> _pendingEditorRemovals = new(8);
        private static readonly Dictionary<ushort, Func<ushort, bool>> _pendingEditorAdds = new(8);
#endif

        private static bool _processingUpdate = false;
#if UNITY_EDITOR
        private static bool _processingEditor = false;
#endif

        internal static int ActiveUpdateFuncCount => _updateFuncs.Count;

#if UNITY_EDITOR
        private static readonly Dictionary<ushort, Func<ushort, bool>> _editorFuncs = new();
#endif

        #endregion

        #region === Update ===

        public static void Tick()
        {
            SchedulerCore.ProcessDict(_updateFuncs, _pendingUpdateRemovals, _pendingUpdateAdds, ref _processingUpdate);
        }

        internal static ushort StartUpdate(Func<ushort, bool> func, Action<ushort> onComplete = null)
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

            if (_processingUpdate)
            {
                // 遍历期间不能修改 _updateFuncs，推入 pending
                _pendingUpdateAdds[id] = wrapped;
            }
            else
            {
                _updateFuncs[id] = wrapped;
            }

            return id;
        }

        internal static void StopUpdate(ushort id)
        {
            if (_processingUpdate)
                // 正在遍历，加入 pending，稍后统一移除
                _pendingUpdateRemovals.Add(id);
            else
                _updateFuncs.Remove(id);
        }

        internal static bool IsUpdateRunning(ushort id)
        {
            if (id == 0) return false;
            return _updateFuncs.ContainsKey(id);
        }

        #endregion

        #region === Editor Update ===

#if UNITY_EDITOR
        private static bool _editorHooked = false;

        private static void EnsureEditorHook()
        {
            if (_editorHooked) return;

            _editorHooked = true;

            EditorApplication.update += () =>
            {
                SchedulerCore.ProcessDict(_editorFuncs, _pendingEditorRemovals, _pendingEditorAdds, ref _processingEditor);
            };
        }

        internal static ushort StartEditor(Func<ushort, bool> func, Action<ushort> onComplete = null)
        {
            EnsureEditorHook();
            ushort id = IDGenerator.NextID();

            Func<ushort, bool> wrapped =
                onComplete == null
               ? func
                : (key =>
                {
                    bool alive = func(key);
                    if (!alive) onComplete(key);
                    return alive;
                });

            if (_processingEditor)
            {
                // 遍历期间不能修改 _editorFuncs，推入 pending
                _pendingEditorAdds[id] = wrapped;
            }
            else
            {
                _editorFuncs[id] = wrapped;
            }

            return id;
        }

        internal static void StopEditor(ushort id)
        {
            if (_processingEditor)
                _pendingEditorRemovals.Add(id);
            else
                _editorFuncs.Remove(id);
        }

        internal static bool IsEditorRunning(ushort id)
        {
            if (id == 0) return false;

            return _editorFuncs.ContainsKey(id);
        }
#endif

        #endregion
    }
}