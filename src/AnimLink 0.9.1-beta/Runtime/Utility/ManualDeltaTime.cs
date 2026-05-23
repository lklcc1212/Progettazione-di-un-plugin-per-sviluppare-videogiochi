#if UNITY_EDITOR
namespace AnimLink
{
    using UnityEngine;

    /// <summary>
    /// 手动计算DeltaTime(用于EditorPreview)
    /// </summary>
    internal class ManualDeltaTime
    {
        private float? _lastTime;

        /// <summary>
        /// 获取当前与上次调用的时间间隔（单位：秒）
        /// </summary>
        public float GetManualDeltaTime()
        {
            if (_lastTime == null) Reset();
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = Mathf.Max(0, currentTime - _lastTime.Value);
            _lastTime = currentTime;
            return deltaTime;
        }

        /// <summary>
        /// 重置计时器（将_lastTime更新为当前时间）
        /// </summary>
        public void Reset()
        {
            _lastTime = Time.realtimeSinceStartup;
        }
    }
}
#endif