namespace Weft.Language.Compilation {
    public class UpvalueCell {
        public int location;
        public object value;
        public bool isClosed;

        public UpvalueCell(int location) {
            this.location = location;
            isClosed = false;
        }
    }
}