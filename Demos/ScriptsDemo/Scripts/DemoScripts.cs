namespace Weft.Demos.ScriptsDemo.Scripts {
    public static class DemoScripts {
        public struct Entry {
            public string name;
            public string code;
        }

        public static readonly Entry[] All = {
            new() {
                name = "Hello World",
                code = @"// Hello World. The basics
print(""Hello from Weft!"");
print(""2 + 2 = "" + (2 + 2));
"
            },
            new() {
                name = "Variables & Math",
                code = @"// Variables, arithmetic, and string concatenation
var x = 10;
var y = 3;

print(""x = "" + x);
print(""y = "" + y);
print(""x + y = "" + (x + y));
print(""x * y = "" + (x * y));
print(""x / y = "" + (x / y));
print(""x % y = "" + (x % y));

var name = ""Weft"";
print(""Hello, "" + name + ""!"");
"
            },
            new() {
                name = "Conditionals",
                code = @"// If/else, comparisons, and logical operators
var score = 85;

if (score >= 90) {
    print(""Grade: A"");
} else if (score >= 80) {
    print(""Grade: B"");
} else {
    print(""Grade: C or below"");
}

var isHigh = score > 80;
var isPassing = score >= 60;
print(""High score? "" + isHigh);
print(""High AND passing? "" + (isHigh && isPassing));
"
            },
            new() {
                name = "While Loop",
                code = @"// While loop: count to 5
var i = 1;
while (i <= 5) {
    print(""i = "" + i);
    i++;
}
print(""Done!"");
"
            },
            new() {
                name = "For Loop",
                code = @"// For loop: sum of 1 to 10
var total = 0;
for (var i = 1; i <= 10; i++) {
    total += i;
}
print(""Sum of 1..10 = "" + total);
"
            },
            new() {
                name = "Do-While Loop",
                code = @"// Do-while: always runs at least once
var n = 1;
do {
    print(""n = "" + n);
    n *= 2;
} while (n <= 16);
print(""Final n = "" + n);
"
            },
            new() {
                name = "Break & Continue",
                code = @"// Break exits the loop, continue skips to next iteration
var i = 0;
while (true) {
    i++;
    if (i % 2 == 0) {
        continue;
    }
    if (i > 10) {
        break;
    }
    print(""odd: "" + i);
}
print(""Loop ended at i = "" + i);
"
            },
            new() {
                name = "Nested Loops",
                code = @"// Multiplication table (1-4)
for (var row = 1; row <= 4; row++) {
    var line = """";
    for (var col = 1; col <= 4; col++) {
        line = line + (row * col) + ""\t"";
    }
    print(line);
}
"
            },
            new() {
                name = "Increment & Assign",
                code = @"// Increment, decrement, and augmented assignment
var counter = 0;
print(""start: "" + counter);

counter++;
print(""after ++: "" + counter);

counter += 10;
print(""after += 10: "" + counter);

counter -= 3;
print(""after -= 3: "" + counter);

counter *= 2;
print(""after *= 2: "" + counter);

--counter;
print(""after --: "" + counter);
"
            },
            new() {
                name = "Yielding (sleep)",
                code = @"// sleep() pauses execution and resumes later
print(""Starting countdown..."");

print(""3..."");
sleep(1);
print(""2..."");
sleep(1);
print(""1..."");
sleep(1);

print(""Go!"");
"
            },
            new() {
                name = "Process Spawning",
                code = @"// spawn() runs a new script concurrently
spawn(""print(\""I am process A\""); sleep(1); print(\""A done\"");"");
spawn(""print(\""I am process B\""); sleep(0.5); print(\""B done\"");"");

print(""Main script spawned two processes."");
print(""B finishes first because it sleeps less.\n"");
"
            },
            new() {
                name = "Inventory Example",
                code = @"// Inventory example using custom module
addItem(""apple"", 3);
addItem(""banana"", 2);
addItem(""apple"", 1);

print(""Has apple? "" + hasItem(""apple""));
print(""Has sword? "" + hasItem(""sword""));

printInventory();

removeItem(""banana"", 2);
print(""After removing bananas:"");
printInventory();
"
            },
            new() {
                name = "Collections",
                code = @"// Arrays. Create, access, modify
var fruits = [""apple"", ""banana"", ""cherry""];
print(""Length: "" + fruits.length);
print(""First: "" + fruits[0]);
print(""Last: "" + fruits[2]);

// Modify by index
fruits[1] = ""blueberry"";
print(""After swap: "" + fruits[1]);

// Push and pop
fruits.push(""dragonfruit"");
print(""After push, length: "" + fruits.length);

var removed = fruits.pop();
print(""Popped: "" + removed);

// Loop over an array
for (var i = 0; i < fruits.length; i++) {
    print(i + "": "" + fruits[i]);
}

// Remove by index
fruits.remove(0);
print(""After removing index 0:"");
for (var i = 0; i < fruits.length; i++) {
    print(""  "" + fruits[i]);
}
"
            },
            new() {
                name = "Functions",
                code = @"// User-defined functions
function add(a, b) {
    return a + b;
}

function greet(name) {
    print(""Hello, "" + name + ""!"");
}

greet(""Weft"");
print(""3 + 5 = "" + add(3, 5));

// Functions calling functions
function square(x) {
    return x * x;
}

function sumOfSquares(a, b) {
    return add(square(a), square(b));
}

print(""3² + 4² = "" + sumOfSquares(3, 4));
"
            },
            new() {
                name = "Recursion",
                code = @"// Recursive functions
function fib(n) {
    if (n <= 1) { return n; }
    return fib(n - 1) + fib(n - 2);
}

for (var i = 0; i < 10; i++) {
    print(""fib("" + i + "") = "" + fib(i));
}

function factorial(n) {
    if (n <= 1) { return 1; }
    return n * factorial(n - 1);
}

print(""5! = "" + factorial(5));
print(""10! = "" + factorial(10));
"
            },
            new() {
                name = "String Methods",
                code = @"// String methods demo
var msg = ""  Hello, Weft!  "";
print(""length: "" + msg.length);
print(""trimmed: '"" + msg.trim() + ""'"");
print(""upper: "" + msg.trim().toUpper());
print(""indexOf 'Weft': "" + msg.indexOf(""Weft""));
print(""contains: "" + msg.contains(""Weft""));
print(""replace: "" + msg.trim().replace(""Weft"", ""World""));
print(""slice(0,5): "" + msg.trim().slice(0, 5));

var csv = ""red,green,blue"";
var colors = csv.split("","");
for (var i = 0; i < colors.length; i++) {
    print(i + "": "" + colors[i]);
}
"
            },
            new() {
                name = "Math Functions",
                code = @"// Built in math functions
print(""abs(-5): "" + abs(-5));
print(""floor(3.7): "" + floor(3.7));
print(""ceil(3.2): "" + ceil(3.2));
print(""round(3.5): "" + round(3.5));
print(""sqrt(16): "" + sqrt(16));
print(""pow(2, 8): "" + pow(2, 8));
print(""min(3, 7): "" + min(3, 7));
print(""max(3, 7): "" + max(3, 7));
print(""random: "" + random());
print(""randomRange(1, 10): "" + randomRange(1, 10));
"
            },
            new() {
                name = "Type & Casting",
                code = @"// type(), number(), string()
print(type(42));        // number
print(type(""hello"")); // string
print(type(true));      // bool
print(type(null));      // null
print(type([1, 2]));    // array

var n = number(""42"");
print(n + 8);           // 50

var s = string(123);
print(s + "" is text""); // 123 is text
"
            },
            new() {
                name = "Closures",
                code = @"// Closures are functions that capture variables from enclosing scope
function makeAdder(x) {
    function inner(y) {
        return x + y;
    }
    return inner;
}

var add5 = makeAdder(5);
var add10 = makeAdder(10);

print(""add5(3) = "" + add5(3));
print(""add10(3) = "" + add10(3));

// Closure over mutable variable
function makeCounter() {
    var count = 0;
    function increment() {
        count = count + 1;
        return count;
    }
    return increment;
}

var counter = makeCounter();
print(""counter: "" + counter() + "", "" + counter() + "", "" + counter());

// Nested closure captures through two levels
function outer() {
    var a = 10;
    function middle() {
        var b = 20;
        function inner() {
            return a + b;
        }
        return inner();
    }
    return middle();
}

print(""nested: "" + outer());
"
            }
        };
    }
}