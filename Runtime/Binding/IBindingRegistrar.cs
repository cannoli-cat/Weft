namespace Weft.Runtime.Binding {
    public interface IBindingRegistrar {
        void Bind(string name, HostFunc fn, string capability = null, double rps = 0, double burst = 0);
    }
}