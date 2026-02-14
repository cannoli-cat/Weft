using UnityEngine;
using Weft.Runtime.Scheduling;
using Weft.Runtime.Services;

namespace Weft.Unity.Services {
    public sealed class WeftUnityTimeService : ITimeService {
        public double Now => Time.realtimeSinceStartupAsDouble;

        public object Sleep(double seconds) => new YieldForSeconds(seconds);
    }
}