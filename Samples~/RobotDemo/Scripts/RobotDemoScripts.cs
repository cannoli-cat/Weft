 namespace Weft.Samples.RobotDemo.Scripts {
    public static class RobotDemoScripts {
        public struct Entry {
            public string name;
            public string code;
        }
        
        public static readonly Entry[] All = {
            new() {
                name = "First Steps",
                code = @"// Move the robot and look around
print(""Facing: "" + facing());
print(""Position: "" + posX() + "", "" + posY());

move();
move();
right();
move();

print(""Now at: "" + posX() + "", "" + posY());
print(""Facing: "" + facing());
"
            },
            new() {
                name = "Scan & React",
                code = @"// Check what's ahead before moving
for (var i = 0; i < 6; i++) {
    var ahead = scan();
    print(""Ahead: "" + ahead);

    if (ahead == ""wall"") {
        print(""Wall! Turning right."");
        right();
    } else {
        move();
    }
}
print(""Done. Position: "" + posX() + "", "" + posY());
"
            },
            new() {
                name = "Gem Collector",
                code = @"// Simple gem-hunting loop
// Walk forward, grab gems, turn when blocked
var steps = 0;
while (!won() && steps < 60) {
    // grab anything at our feet
    if (collect()) {
        print(""Got a gem! ("" + gems() + "" total)"");
    }

    var ahead = scan();
    if (ahead == ""gem"" || ahead == ""empty"") {
        move();
    } else {
        // wall ahead, try turning
        if (scanDir(""right"") != ""wall"") {
            right();
        } else if (scanDir(""left"") != ""wall"") {
            left();
        } else {
            // dead end, turn around
            right();
            right();
        }
    }
    steps++;
}

collect(); // check final tile
print(""Collected "" + gems() + "" gems in "" + steps + "" steps."");
if (won()) {
    print(""All gems found!"");
}
"
            },
            new() {
                name = "Wall Follower",
                code = @"// Classic left-hand wall following algorithm
var steps = 0;
var maxSteps = 100;

while (!won() && steps < maxSteps) {
    collect();

    // left-hand rule: prefer turning left
    if (scanDir(""left"") != ""wall"") {
        left();
        move();
    } else if (scan() != ""wall"") {
        move();
    } else if (scanDir(""right"") != ""wall"") {
        right();
        move();
    } else {
        // boxed in, turn around
        right();
        right();
    }

    steps++;
}

collect();
if (won()) {
    print(""All "" + gems() + "" gems collected in "" + steps + "" steps!"");
} else {
    print(""Stopped after "" + steps + "" steps. "" + remaining() + "" gems left."");
}
"
            },
            new() {
                name = "DFS Explorer",
                code = @"// Depth-first search, guaranteed to find every gem
var visited = {};

function faceDir(dir) {
    while (facing() != dir) {
        right();
    }
}

function opposite(dir) {
    if (dir == ""north"") return ""south"";
    if (dir == ""south"") return ""north"";
    if (dir == ""east"") return ""west"";
    return ""east"";
}

function dfs() {
    var key = posX() + "","" + posY();
    if (visited[key]) return;
    if (won()) return;
    visited[key] = true;
    collect();

    var dirs = [""north"", ""east"", ""south"", ""west""];

    for (var i = 0; i < 4; i++) {
        if (won()) return;
        faceDir(dirs[i]);
        if (scan() == ""gem"") {
            move();
            dfs();
            faceDir(opposite(dirs[i]));
            move();
        }
    }
    for (var i = 0; i < 4; i++) {
        if (won()) return;
        faceDir(dirs[i]);
        if (scan() == ""empty"") {
            move();
            dfs();
            faceDir(opposite(dirs[i]));
            move();
        }
    }
}

dfs();
print(""Found all "" + gems() + "" gems!"");
"
            },
        };
    }
}