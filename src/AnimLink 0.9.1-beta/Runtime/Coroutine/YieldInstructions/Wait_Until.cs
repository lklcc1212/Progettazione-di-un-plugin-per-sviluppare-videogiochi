namespace AnimLink
{
    using System;

    public sealed class Wait_Until : IYieldInstruction
    {
        internal Func<bool> _predicate;

        public bool KeepWaiting => !_predicate();

        public Wait_Until(Func<bool> predicate)
        {
            _predicate = predicate;
        }

        public void Reset()
        {
        }
    }
}
