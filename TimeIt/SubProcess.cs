using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIt
{
    internal class SubProcess
    {
        /// <summary>
        /// Underlaying process. It is not safe to use this field after the process completes.
        /// </summary>
        private Process m_process;

        /// <summary>
        /// SubProcess Id.
        /// </summary>
        public int Pid { get; }

        /// <summary>
        /// Handle to SubProcess.
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// SubProcess name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// SubProcess execution times.
        /// </summary>
        public ProcessTimes Times { get; private set; }

        /// <summary>
        /// Create subprocess from process object.
        /// </summary>
        /// <param name="process">SubProcess process.</param>
        internal SubProcess(Process process)
        {
            m_process = process;

            Pid = process.Id;
            Handle = process.Handle;
            Name = process.ProcessName;
        }

        /// <summary>
        /// Stops the associated process.
        /// </summary>
        internal void Kill()
        {
            if (!m_process.HasExited)
            {
                m_process.Kill();
            }
        }

        /// <summary>
        /// Tries to query execution times of process.
        /// </summary>
        /// <returns>True if measurement was successful.</returns>
        internal bool TryMeasureExecutionTimes()
        {
            if (ProcessTimeUtil.TryGetProcessTimes(Handle, out ProcessTimes measuredTimes))
            {
                Times = measuredTimes;
                return true;
            }
            return false;
        }
    }
}
