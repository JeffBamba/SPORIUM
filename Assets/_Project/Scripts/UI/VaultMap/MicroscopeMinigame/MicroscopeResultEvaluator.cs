namespace _Project
{
    public enum MicroscopeOutcome
    {
        FullWithBonus,
        Full,
        Partial,
        Unknown,
        Corrupted
    }
    public static class MicroscopeResultEvaluator
    {
        public static MicroscopeOutcome Evaluate()
        {
            return MicroscopeOutcome.Unknown;
        }
    }
}