using UnityEngine;
using Weft.Runtime.Services;

namespace Weft.Unity.Services {
    public sealed class WeftUnityTimeService : ITimeService {
        public double Now => Time.realtimeSinceStartupAsDouble;
    }
}