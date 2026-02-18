namespace Weft.Language.Compilation {
    public class WeftClosure {
        public int funcPc;
        public int arity;
        public UpvalueCell[] upvalues;

        public WeftClosure(int funcPc, int arity, UpvalueCell[] upvalues) {
            this.funcPc = funcPc;
            this.arity = arity;
            this.upvalues = upvalues;
        }
    }
}