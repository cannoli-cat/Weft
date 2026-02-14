using System;

namespace Weft.Runtime {
    [Flags]
    public enum LanguageFeatures {
        None = 0,
        Conditionals = 1 << 0, // if/else/ternary
        Loops = 1 << 1, // while/for/break/continue
        Functions = 1 << 2, // func/return/closures
        Collections = 1 << 3, // arrays/maps + . / [] access
        AugAssign = 1 << 4, // += -= *= /= %=

        All = Conditionals | Loops | Functions | Collections | AugAssign
    }
}