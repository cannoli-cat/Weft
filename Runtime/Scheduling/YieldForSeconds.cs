namespace Weft.Runtime.Scheduling {
    public interface IYieldRequest { }
    
    public sealed class YieldForSeconds : IYieldRequest {
        public double Seconds { get; }
        public object ReturnValue { get; }
    
        public YieldForSeconds(double seconds, object returnValue = null) {
            Seconds = seconds;
            ReturnValue = returnValue;
        }
    }
}