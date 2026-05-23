namespace AnimLink
{
    using System;

    public sealed class Wait_While : IYieldInstruction
    {
        internal Func<bool> Predicate;

        public bool KeepWaiting => Predicate();

        public Wait_While(Func<bool> predicate)
        {
            Predicate = predicate;
        }

        public void Reset()
        {
        }
    }
}
