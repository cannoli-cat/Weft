using System.Collections.Generic;
using Weft.Runtime.Services;
using Weft.Unity.Services;

namespace Weft.Runtime.Scheduling {
    public sealed class WeftScheduler {
        private readonly List<IWeftProcess> processes = new();

        private readonly ITimeService time = new WeftUnityTimeService();

        public void Tick() {
            for (var i = processes.Count - 1; i >= 0; i--) {
                var p = processes[i];
                
                if (p.Completed) {
                    processes.RemoveAt(i);
                    continue;
                }
                
                if (p.ResumeAt > 0 && time.Now < p.ResumeAt) 
                    continue;
                
                if (p.WaitingForPid >= 0) {
                    var target = processes.Find(o => o.Pid == p.WaitingForPid);
                    if (target is { Completed: false })
                        continue;
                    
                    p.WaitingForPid = -1;
                }

                var res = p.Step();
                if (!res.success) {
                    TryReportError(p, res.error);
                    processes.RemoveAt(i);
                }
            }
        }
        
        private static void TryReportError(IWeftProcess p, string error) {
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

        /// <summary>
        /// Spawn any process that implements IWeftProcess.
        /// </summary>
        public int Spawn(IWeftProcess proc) {
            proc.Context.Pid = proc.Pid;
            processes.Add(proc);
            return proc.Pid;
        }

        public bool Kill(int pid) => processes.RemoveAll(p => p.Pid == pid) > 0;

        public IReadOnlyList<IWeftProcess> Ps() => processes;
    }
}