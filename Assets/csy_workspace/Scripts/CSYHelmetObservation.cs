namespace CsyMuseum
{
    // A self-check based on observation, not a claim that software detected a physical window.
    public sealed class CSYHelmetObservation
    {
        public bool Complete { get; private set; }
        public int Attempts { get; private set; }
        public void Answer(bool correct)
        {
            if (Complete) return;
            Attempts++;
            Complete = correct;
        }
        public void Reset() { Complete = false; Attempts = 0; }
    }
}
