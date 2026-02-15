namespace Weft.Runtime {
    public sealed class WeftLimits {
        public int GasPerStep { get; private set; } = 2000; // max AST visits per tick
        public int MaxCallDepth { get; private set; } = 256; // runaway recursion guard
        public int MaxHeapBytes { get; private set; } = 16 * 1024 * 1024; // soft cap for user data
        public bool Deterministic { get; private set; } = false; // use sim time + seeded RNG
        
        // Rate limits that are default for any bound function that doesn't specify its own
        public double DefaultRps { get; private set; } = 0; // calls/sec
        public double DefaultBurst { get; private set; } = 0; // token bucket capacity

        public static WeftLimits Default { get; } = new();

        public WeftLimits Clone() => new() {
            GasPerStep = GasPerStep, MaxCallDepth = MaxCallDepth, MaxHeapBytes = MaxHeapBytes,
            Deterministic = Deterministic, DefaultRps = DefaultRps, DefaultBurst = DefaultBurst
        };

        public WeftLimits WithGas(int v)                  { var c = Clone(); c.GasPerStep = v; return c; }
        public WeftLimits WithDeterministic(bool v)       { var c = Clone(); c.Deterministic = v; return c; }
        public WeftLimits WithDefaultRate(double rps, double burst)
        { var c = Clone(); c.DefaultRps = rps; c.DefaultBurst = burst; return c; }
    }
}