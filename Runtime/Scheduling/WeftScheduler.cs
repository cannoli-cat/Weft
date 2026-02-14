using System.Collections.Generic;
using Weft.Language.AST;
using Weft.Runtime.Services;
using Weft.Unity.Engine;
using Weft.Unity.Services;

namespace Weft.Runtime.Scheduling {
    public sealed class WeftScheduler {
        private readonly List<WeftProcess> processes = new();

        private readonly ITimeService time = new WeftUnityTimeService();

        public void Tick() {
            for (var i = processes.Count - 1; i >= 0; i--) {
                var p = processes[i];
                
                if (p.Completed) {
                    processes.RemoveAt(i);
                    continue;
                }
                
                if (p.ResumeAt > 0 && time.Now < p.ResumeAt) 
                    continue; // still sleeping
                
                // PID wait; skip if target is still running
                if (p.WaitingForPid >= 0) {
                    var target = processes.Find(o => o.Pid == p.WaitingForPid);
                    if (target is { Completed: false })
                        continue;
                    
                    p.WaitingForPid = -1; // target done or gone, clear and resume
                }

                var res = p.Step();
                if (!res.success) {
                    TryReportError(p, res.error);
                    processes.RemoveAt(i);
                }
            }
        }
        
        private static void TryReportError(WeftProcess p, string error) {
            try {
                if (p.Context?.Services is WeftServiceProvider sp &&
                    sp.TryGet<WeftConsoleService>(out var console)) {
                    console.Report($"ERR: {error}", true);
                } else {
                    UnityEngine.Debug.LogError($"[weft pid:{p.Pid}] {error}");
                }
            } catch {
                UnityEngine.Debug.LogError($"[weft pid:{p.Pid}] {error}");
            }
        }

        public int Spawn(List<AstNode> program, ScriptContext ctx, int gasPerStep) {
            var proc = new WeftProcess(program, ctx, gasPerStep);
            ctx.Pid = proc.Pid;
            processes.Add(proc);
            return proc.Pid;
        }
        
        public bool Kill(int pid) => processes.RemoveAll(p => p.Pid == pid) > 0;

        public IReadOnlyList<WeftProcess> Ps() => processes;
    }
}