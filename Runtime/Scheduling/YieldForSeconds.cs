namespace Weft.Runtime.Scheduling {
    public interface IYieldRequest { }
    
    public sealed class YieldForSeconds : IYieldRequest {
        public double Seconds { get; }
        
        public YieldForSeconds(double seconds) => Seconds = seconds;
    }
}