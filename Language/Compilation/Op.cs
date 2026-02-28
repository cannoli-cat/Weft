namespace Weft.Language.Compilation {
    public enum Op : byte {
        Const,
        Pop,
        Dup,
        Dup2,
        InsertUnder,
        
        LoadLocal,
        StoreLocal,
        
        LoadGlobal,
        StoreGlobal,

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
        
        Closure,
        CallClosure,
        LoadUpvalue,
        StoreUpvalue,
        CloseUpvalues,
        
        Peek,
        Poke,
    }
}