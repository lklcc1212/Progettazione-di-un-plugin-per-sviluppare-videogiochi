namespace AnimLink
{
    /// <summary>
    /// <see langword="ushort"/> ID 生成器
    /// </summary>
    internal static class IDGenerator
    {
        private static ushort _idCounter = 0;

        /// <summary>
        /// 获取下一个唯一ID。
        /// <br>-注意：</br>
        /// <br>1. ID = 0 被视为无效ID，永远不会分配。</br>
        /// <br>2. ID 从 1 开始递增，如果溢出回到 0，会跳过 0 并继续递增。</br>
        /// </summary>
        /// <returns>按递增顺序生成的唯一ID（从 1 开始）。</returns>
        public static ushort NextID()
        {
            if (++_idCounter == 0) ++_idCounter;
            return _idCounter;
        }
    }
}