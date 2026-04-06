# Weft

A lightweight, sandboxed ingame scripting language and bytecode VM for Unity. Let players and designers write game logic at runtime.

![Weft Robot Demo](docs/robot-demo.gif)

## Features

- **JavaScript-like syntax**: Familiar `let`, `const`, `if/else`, `for`, `while`, functions, closures, arrays, and objects
- **Bytecode compiler + stack-based VM**: Scripts are compiled to bytecode and executed on a register-free stack machine, not interpreted from an AST
- **Gas-limited execution**: Every script runs on a fuel budget per frame, preventing infinite loops from freezing your game
- **Cooperative multitasking**: `sleep()`, `spawn()`, and `await_pid()` let scripts yield, run concurrently, and wait on each other
- **Modular by design**: Toggle language features on and off per-project (conditionals, loops, functions, collections, math) through a ScriptableObject config
- **Custom bindings**: Expose C# functions to scripts with `[WeftFunction]` attributes on `WeftBindings` subclasses, or implement `IWeftModule` and `WeftModuleSO` for full control
- **Script caching**: Compiled bytecode is cached and reused automatically

## Installation

Testing has been done on **Unity 6000.0+**. Use older versions at your own risk!

Add via the Unity Package Manager using a git URL **(core only!)**:

```
https://github.com/cannoli-cat/weft.git
```

Or download the latest `.unitypackage` and import **(with demos!)**.

## Quick Start

### 1. Add the engine to your scene

Create an empty GameObject and add the `WeftEngine` component. Assign a `WeftOptionsSO` asset (Create -> Weft -> Engine Options) to configure which language features and modules are enabled.

### 2. Run a script from C#

```csharp
// Run a script on a target GameObject
WeftEngine.Run(@"
    print(""Hello from Weft!"");
    const x = 10;
    for (let i = 0; i < x; i++) {
        print(i);
    }
", targetGameObject);
```

### 3. Expose your own functions

Subclass `WeftBindings` and add it to any GameObject to make C# methods callable from scripts:

```csharp
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Unity.Engine;

public class EnemyBindings : WeftBindings {

    [WeftFunction("attack")]
    private object Attack(ScriptContext ctx, string target) {
        // your game logic here
        return true;
    }

    [WeftFunction("wait")]
    private object Wait(ScriptContext ctx, double seconds) {
        return new YieldForSeconds(seconds);
    }
}
```

Scripts can then call these directly:

```javascript
attack("goblin");
wait(0.5);
attack("goblin");
print("combo!");
```

## Architecture

```
Source -> Lexer -> Parser -> AST -> Compiler -> Bytecode -> VM -> Scheduler
```

| Stage | What it does |
|---|---|
| **Lexer** | Tokenizes source into keywords, identifiers, literals, operators |
| **Parser** | Builds an AST with configurable language features (can disable loops, functions, etc.) |
| **Compiler** | Walks the AST and emits stack-based bytecode with upvalue tracking for closures |
| **VM** | Executes bytecode with a gas budget, supports yields and host function calls |
| **Scheduler** | Round-robin ticks all live processes, handles sleep timers and inter-process waits |

## Modules

Language features are toggled through modules. Enable only what your project needs:

| Module | Enables | Key Functions |
|---|---|---|
| **Core** | Variables, expressions | `print()`, `clear()`, `type()`, `number()`, `string()` |
| **Conditionals** | `if` / `else` / ternary | - |
| **Loops** | `while` / `for` / `do-while` / `break` / `continue` | - |
| **Augmented Assignment** | `+=` `-=` `*=` `/=` `%=` | - |
| **Functions** | `function` declarations, closures, anonymous functions | - |
| **Collections** | Arrays, objects, string methods, `.` and `[]` access | `push()`, `pop()`, `remove()`, `indexOf()`, `split()`, `replace()`, `trim()`, `contains()` + more |
| **Math** | Built-in math | `abs()`, `sqrt()`, `pow()`, `floor()`, `ceil()`, `round()`, `min()`, `max()`, `random()`, `randomRange()` |
| **Time** | Yielding | `sleep(seconds)` |
| **Process** | Concurrency | `spawn(src)`, `kill(pid)`, `ps()`, `await_pid(pid)` |

## Custom Modules

For deeper integration, implement `IWeftModule` to register functions and inject services:

```csharp
public sealed class InventoryModule : IWeftModule {
    public string Id => "inventory";
    public LanguageFeatures ParserFeatures => LanguageFeatures.None;

    public void Register(IBindingRegistrar registrar) {
        registrar.Bind("addItem", (ctx, args) => {
            ctx.Resolve<Inventory>().Add((string)args[0], (int)(double)args[1]);
            return null;
        });
    }

    public void Setup(ScriptContext ctx) {
        var inv = ctx.GameObject.GetComponent<Inventory>();
        if (inv != null) ctx.Services.Add(inv);
    }
}
```

Wrap it in a `WeftModuleSO` ScriptableObject and drag it into your `WeftOptionsSO` to enable it from the Inspector. See `ScriptsDemo` for an Inventory implementation.

## Demos

The package includes two demo scenes in the `Demos` folder:

**Scripts Demo**: A standalone script editor showcasing all language features such as: variables, loops, conditionals, functions, closures, string methods, math, collections, and more. Includes ~20 example scripts you can run and edit.

**Robot Demo**: A grid-based robot that scripts can control in real time. Includes example scripts ranging from basic movement to a full depth-first search that collects every gem on the board. Demonstrates custom bindings, yielding for step-by-step animation, and the `WeftBindings` attribute pattern.
