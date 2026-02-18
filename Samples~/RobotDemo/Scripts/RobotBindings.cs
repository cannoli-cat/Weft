using UnityEngine;
using Weft.Runtime.Binding;
using Weft.Runtime.Scheduling;
using Weft.Unity.Engine;

namespace Weft.Samples.RobotDemo.Scripts {
    /// <summary>
    /// Drop this MonoBehaviour on any GameObject alongside a RobotGrid
    /// to expose robot-control functions to Weft scripts.
    ///
    /// Available script functions:
    ///   move() - move forward one tile (returns true/false)
    ///   left() - turn left 90°
    ///   right() - turn right 90°
    ///   scan() - what's ahead? returns "wall", "gem", "empty", "out"
    ///   scanDir(dir) - scan relative: "ahead", "left", "right", "behind"
    ///   collect() - pick up gem at current position (returns true/false)
    ///   facing() - returns "north", "east", "south", "west"
    ///   posX() - robot's x coordinate
    ///   posY() - robot's y coordinate
    ///   gems() - how many gems collected so far
    ///   remaining() - how many gems left on the grid
    ///   won() - true if all gems collected
    ///
    /// Each movement/turn call includes a small sleep so the player
    /// can watch the robot move step by step.
    /// </summary>
    [RequireComponent(typeof(RobotGrid))]
    public class RobotBindings : WeftBindings {
        [Tooltip("Seconds to pause after each move/turn so the player can watch")]
        public float stepDelay = 0.15f;

        private RobotGrid grid;

        private void Awake() {
            grid = GetComponent<RobotGrid>();
        }

        [WeftFunction("move")]
        private object Move(ScriptContext ctx) {
            var g = GetGrid(ctx);
            var ok = g.MoveForward();
            if (stepDelay > 0) return new YieldForSeconds(stepDelay);
            return ok;
        }

        [WeftFunction("left")]
        private object Left(ScriptContext ctx) {
            GetGrid(ctx).TurnLeft();
            if (stepDelay > 0) return new YieldForSeconds(stepDelay);
            return null;
        }
        
        [WeftFunction("right")]
        private object Right(ScriptContext ctx) {
            GetGrid(ctx).TurnRight();
            if (stepDelay > 0) return new YieldForSeconds(stepDelay);
            return null;
        }
        
        [WeftFunction("scan")]
        private object Scan(ScriptContext ctx) => GetGrid(ctx).Scan();

        [WeftFunction("scanDir")]
        private object ScanDir(ScriptContext ctx, string direction) =>
            GetGrid(ctx).ScanDir(direction);
        
        [WeftFunction("facing")]
        private object Facing(ScriptContext ctx) => GetGrid(ctx).Facing;

        [WeftFunction("posX")]
        private object PosX(ScriptContext ctx) => (double)GetGrid(ctx).Position.x;
        
        [WeftFunction("posY")]
        private object PosY(ScriptContext ctx) => (double)GetGrid(ctx).Position.y;
        
        [WeftFunction("collect")]
        private object Collect(ScriptContext ctx) => GetGrid(ctx).Collect();
        
        [WeftFunction("gems")]
        private object Gems(ScriptContext ctx) => (double)GetGrid(ctx).GemsCollected;

        [WeftFunction("remaining")]
        private object Remaining(ScriptContext ctx) => (double)GetGrid(ctx).GemsRemaining;
        
        [WeftFunction("won")]
        private object Won(ScriptContext ctx) => GetGrid(ctx).GemsRemaining == 0;
        
        public override void Setup() {
            if (grid == null) grid = GetComponent<RobotGrid>();
            grid.BuildDefaultLevel();
        }

        /// <summary>
        /// Resolves the RobotGrid from the script's target GameObject,
        /// falling back to this component's grid.
        /// </summary>
        private RobotGrid GetGrid(ScriptContext ctx) {
            if (ctx.GameObject != null) {
                var g = ctx.GameObject.GetComponent<RobotGrid>();
                if (g != null) return g;
            }

            return grid;
        }
    }
}