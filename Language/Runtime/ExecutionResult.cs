namespace Weft.Language.Runtime {
    public readonly struct ExecutionResult {
        public readonly bool success;
        public readonly object value;
        public readonly string error;
        public readonly bool isYield;
        public readonly double resumeAt;
        public readonly int waitForPid;
        public readonly bool isBreak;
        public readonly bool isContinue;

        private ExecutionResult(bool success, object value, string error,
            bool isYield, double resumeAt, int waitForPid,
            bool isBreak, bool isContinue) {
            this.success = success;
            this.value = value;
            this.error = error;
            this.isYield = isYield;
            this.resumeAt = resumeAt;
            this.waitForPid = waitForPid;
            this.isBreak = isBreak;
            this.isContinue = isContinue;
        }

        public static ExecutionResult SuccessResult(object value = null) =>
            new(true, value, null, false, 0, -1, false, false);

        public static ExecutionResult ErrorResult(string error) =>
            new(false, null, error, false, 0, -1, false, false);

        public static ExecutionResult YieldUntil(double resumeAtSec) =>
            new(true, null, null, true, resumeAtSec, -1, false, false);

        public static ExecutionResult YieldUntilPid(int pid) =>
            new(true, null, null, true, 0, pid, false, false);

        public static ExecutionResult Break() =>
            new(true, null, null, false, 0, -1, true, false);

        public static ExecutionResult Continue() =>
            new(true, null, null, false, 0, -1, false, true);
    }
}