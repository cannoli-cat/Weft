namespace Weft.Language.Compilation {
    public enum Op : byte {
        Const,
        Pop,
        
        LoadLocal,
        StoreLocal,

        Add, Sub, Mul, Div, Mod,
        Negate,

        Eq, Neq, Lt, Gt, Lte, Gte,
        Not,

        Jump,
        JumpIfFalse,
        JumpIfTrue,

        Call,
        Halt,
        
        CallFunc,
        Return,
    }
}