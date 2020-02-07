using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace TimeIt.ProcessUtils
{
    internal class ProcessTree : IEnumerable<SubProcess>
    {
        private const string MOS_QUERY = "SELECT * FROM Win32_Process WHERE ParentProcessID={0}";

        private List<SubProcess> m_subProcesses = new List<SubProcess>();

        private Process m_root;

        internal ProcessTree(Process rootProcess)
        {
            m_root = rootProcess;
            FillProcessTree();
        }

        /// <summary>
        /// Recursively find all subprocesses of root process.
        /// </summary>
        private void FillProcessTree()
        {
            void AddSubprocess(int pid)
            {
                using (ManagementObjectSearcher processSearcher = new ManagementObjectSearcher(string.Format(MOS_QUERY, pid)))
                {
                    var childrenObjects = processSearcher.Get();
                    foreach (var child in childrenObjects)
                    {
                        // Retrieve child process id.
                        int childProcessId = Convert.ToInt32(child["ProcessID"]);

                        // Recursively explore subprocess children.
                        AddSubprocess(childProcessId);
                    }

                    // Find running process by pid.
                    Process process = Process.GetProcessById(pid);

                    m_subProcesses.Add(new SubProcess(process));
                }
            }

            m_subProcesses.Clear();
            AddSubprocess(m_root.Id);
        }

        internal ProcessTimes GetOverallTreeTime()
        {
            long maxWallTimeTicks = 0;
            long userTimeTicks = 0;
            long kernelTimeTicks = 0;
            foreach (SubProcess child in m_subProcesses)
            {
                maxWallTimeTicks = Math.Max(maxWallTimeTicks, child.Times.WallTime.Ticks);
                userTimeTicks += child.Times.UserTime.Ticks;
                kernelTimeTicks += child.Times.KernelTime.Ticks;
            }
            return new ProcessTimes(maxWallTimeTicks, userTimeTicks, kernelTimeTicks);
        }

        /// <summary>
        /// Measure execution time of all processes in the tree.
        /// </summary>
        internal void MeasureExecutionTimeOfTree()
        {
            foreach (SubProcess child in m_subProcesses)
            {
                if (!child.TryMeasureExecutionTimes())
                {
                    Console.Error.WriteLine($"Failed to measure execution time of {child.Name}");
                }
            }
        }

        /// <summary>
        /// Kill all processes in the tree.
        /// </summary>
        internal void KillProcessTree()
        {
            foreach (SubProcess child in m_subProcesses)
            {
                child.Kill();
            }
        }

        public IEnumerator<SubProcess> GetEnumerator()
        {
            return ((IEnumerable<SubProcess>)m_subProcesses).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SubProcess>)m_subProcesses).GetEnumerator();
        }
    }
}
