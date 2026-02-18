using System.Collections.Generic;
using UnityEngine;
using Weft.Runtime;
using Weft.Runtime.Modules;

namespace Weft.Unity.Engine {
    [CreateAssetMenu(menuName = "Weft/Engine Options")]
    public sealed class WeftOptionsSO : ScriptableObject {
        [Header("Built-in Modules")]
        [Tooltip("Variables, print, clear — almost always on")]
        public bool core = true;

        [Tooltip("if / else")]
        public bool conditionals = true;

        [Tooltip("while / for / do-while / break / continue")]
        public bool loops = true;

        [Tooltip("+= -= *= /= %=")]
        public bool augmentedAssignment = true;

        [Tooltip("sleep(seconds)")]
        public bool time = true;

        [Tooltip("spawn / kill / ps / await_pid")]
        public bool processes = false;

        [Tooltip("arrays, push / pop / remove, index access")]
        public bool collections = false;

        [Tooltip("func / return")]
        public bool functions = false;
        
        [Tooltip("abs, sqrt, pow, floor, ceil, round, min, max...")]
        public bool math = false;
        
        [Header("Custom Modules (drop your ScriptableObject modules here)")]
        public WeftModuleSO[] customModules;

        [Header("Limits")]
        public int gasPerStep = 2000;
        public bool deterministic = false;

        public WeftOptions ToRuntime() {
            var modules = new List<IWeftModule>();

            if (core) modules.Add(new CoreModule());
            if (conditionals) modules.Add(new ConditionalsModule());
            if (loops) modules.Add(new LoopsModule());
            if (augmentedAssignment) modules.Add(new AugAssignModule());
            if (time) modules.Add(new TimeModule());
            if (processes) modules.Add(new ProcessModule());
            if (collections) modules.Add(new CollectionsModule());
            if (functions) modules.Add(new FunctionsModule());
            if (math) modules.Add(new MathModule());

            if (customModules != null) {
                foreach (var custom in customModules) {
                    if (custom != null)
                        modules.Add(custom.CreateModule());
                }
            }

            return new WeftOptions(
                modules,
                WeftLimits.Default
                    .WithGas(gasPerStep)
                    .WithDeterministic(deterministic)
            );
        }
    }
}