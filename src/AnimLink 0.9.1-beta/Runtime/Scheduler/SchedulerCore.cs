namespace AnimLink
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal static class SchedulerCore
    {
        private static SchedulerHost _host;
        private static readonly List<ushort> _tmpRemoval = new(8);

        public static SchedulerHost Host
        {
            get
            {
                if (_host != null) return _host;

                _host = Object.FindFirstObjectByType<SchedulerHost>();

                if (_host != null) return _host;

                var go = new GameObject("[AnimLink SchedulerHost]");
                Object.DontDestroyOnLoad(go);

                _host = go.AddComponent<SchedulerHost>();
                return _host;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            _ = Host; // 强制初始化
        }

        public static void ProcessDict(Dictionary<ushort, Func<ushort, bool>> dict, List<ushort> pendingRemovals, Dictionary<ushort, Func<ushort, bool>> pendingAdds, ref bool processingFlag)
        {
            processingFlag = true;
            // 遍历并收集返回 false 的 key
            _tmpRemoval.Clear();
            foreach (var kv in dict)
            {
                if (!kv.Value(kv.Key))
                    _tmpRemoval.Add(kv.Key);
            }
            processingFlag = false;

            // 先删除那些返回 false 的 key
            for (int i = 0; i < _tmpRemoval.Count; i++)
                dict.Remove(_tmpRemoval[i]);

            // 再删除 pending（可能是在遍历期间通过 StopXXX 添加的）
            if (pendingRemovals.Count > 0)
            {
                foreach (var id in pendingRemovals)
                    dict.Remove(id);
                pendingRemovals.Clear();
            }

            // 最后添加 pending（可能是在遍历期间通过 StartXXX 添加的）
            if (pendingAdds.Count > 0)
            {
                foreach (var kv in pendingAdds)
                    dict[kv.Key] = kv.Value;

                pendingAdds.Clear();
            }
        }
    }
}
