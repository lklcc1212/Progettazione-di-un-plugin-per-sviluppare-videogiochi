namespace AnimLink
{
    public interface IYieldInstruction
    {
        bool KeepWaiting { get; }

        public void Reset();
    }
}