using System.Collections.Generic;
using UnityEngine;

namespace Weft.Demos.RobotDemo.Scripts {
    /// <summary>
    /// A simple grid world for the robot demo.
    /// Walls block movement, gems can be collected.
    /// Drop this on a GameObject to define the world.
    /// </summary>
    public class RobotGrid : MonoBehaviour {
        [Header("Grid Size")]
        public int width = 8;
        public int height = 8;

        [Header("Starting State")]
        public Vector2Int robotStart = new(1, 1);
        public int robotStartDir = 0; // 0=N, 1=E, 2=S, 3=W

        public Vector2Int RobotPos { get; set; }
        public int RobotDir { get; set; } // 0=N, 1=E, 2=S, 3=W

        private readonly HashSet<Vector2Int> walls = new();
        private readonly HashSet<Vector2Int> gems = new();

        private int gemsCollected;
        private int totalGems;

        private static readonly Vector2Int[] DirVectors = {
            new(0, 1),  // N
            new(1, 0),  // E
            new(0, -1), // S
            new(-1, 0)  // W
        };

        private static readonly string[] DirNames = { "north", "east", "south", "west" };

        public void Reset() {
            walls.Clear();
            gems.Clear();
            gemsCollected = 0;
            RobotPos = robotStart;
            RobotDir = robotStartDir;
        }

        /// <summary>
        /// Builds a default level with border walls and a few gems.
        /// Call this or build your own layout before running scripts.
        /// </summary>
        public void BuildDefaultLevel() {
            Reset();

            // border walls
            for (var x = 0; x < width; x++) {
                walls.Add(new Vector2Int(x, 0));
                walls.Add(new Vector2Int(x, height - 1));
            }
            for (var y = 0; y < height; y++) {
                walls.Add(new Vector2Int(0, y));
                walls.Add(new Vector2Int(width - 1, y));
            }

            // some interior walls
            walls.Add(new Vector2Int(3, 2));
            walls.Add(new Vector2Int(3, 3));
            walls.Add(new Vector2Int(3, 4));
            walls.Add(new Vector2Int(5, 5));
            walls.Add(new Vector2Int(5, 6));

            // gems to collect
            PlaceGem(new Vector2Int(2, 3));
            PlaceGem(new Vector2Int(4, 5));
            PlaceGem(new Vector2Int(6, 2));
            PlaceGem(new Vector2Int(6, 6));
            PlaceGem(new Vector2Int(1, 6));

            totalGems = gems.Count;
        }

        public void AddWall(Vector2Int pos) => walls.Add(pos);

        public void PlaceGem(Vector2Int pos) => gems.Add(pos);

        public bool IsWall(Vector2Int pos) => walls.Contains(pos);
        public bool IsGem(Vector2Int pos) => gems.Contains(pos);

        public bool InBounds(Vector2Int pos) =>
            pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

        /// <summary>
        /// Try to move the robot one step in its facing direction.
        /// Returns true if the move succeeded.
        /// </summary>
        public bool MoveForward() {
            var next = RobotPos + DirVectors[RobotDir];
            if (!InBounds(next) || IsWall(next)) return false;
            RobotPos = next;
            return true;
        }

        public void TurnLeft() => RobotDir = (RobotDir + 3) % 4;
        public void TurnRight() => RobotDir = (RobotDir + 1) % 4;

        /// <summary>
        /// What's in the cell directly ahead of the robot?
        /// Returns "wall", "gem", "empty", or "out" if out of bounds.
        /// </summary>
        public string Scan() {
            var ahead = RobotPos + DirVectors[RobotDir];
            if (!InBounds(ahead)) return "out";
            if (IsWall(ahead)) return "wall";
            if (IsGem(ahead)) return "gem";
            return "empty";
        }

        /// <summary>
        /// Scan in a specific direction relative to facing: "ahead", "left", "right", "behind"
        /// </summary>
        public string ScanDir(string relative) {
            var dir = relative switch {
                "left" => (RobotDir + 3) % 4,
                "right" => (RobotDir + 1) % 4,
                "behind" => (RobotDir + 2) % 4,
                _ => RobotDir // "ahead" or default
            };

            var target = RobotPos + DirVectors[dir];
            if (!InBounds(target)) return "out";
            if (IsWall(target)) return "wall";
            if (IsGem(target)) return "gem";
            return "empty";
        }

        /// <summary>
        /// Pick up a gem at the robot's current position.
        /// Returns true if there was a gem to collect.
        /// </summary>
        public bool Collect() {
            if (!gems.Remove(RobotPos)) return false;
            gemsCollected++;
            return true;
        }

        public int GemsCollected => gemsCollected;
        public int GemsRemaining => gems.Count;
        public int TotalGems => totalGems;

        public string Facing => DirNames[RobotDir];

        public (int x, int y) Position => (RobotPos.x, RobotPos.y);
    }
}