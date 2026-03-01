namespace Weft.Runtime.Binding {
    public interface IWeftObject {
        object GetMember(string name);
        void SetMember(string name, object value);
    }
}