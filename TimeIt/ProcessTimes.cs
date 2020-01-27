using System;

namespace TimeIt
{
    struct ProcessTimes
    {
        /// <summary>
        /// Total wall time of the measured process.
        /// </summary>
        internal TimeSpan wallTime;

        /// <summary>
        /// Process time spent in kernel mode.
        /// </summary>
        internal TimeSpan kernelTime;

        /// <summary>
        /// Process time spent in user mode.
        /// </summary>
        internal TimeSpan userTime;
    }
}
