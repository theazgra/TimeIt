using System;

namespace TimeIt
{
    struct ProcessTimes
    {
        /// <summary>
        /// Total wall time of the measured process.
        /// </summary>
        internal PreciseTimeSpan wallTime;

        /// <summary>
        /// Process time spent in kernel mode.
        /// </summary>
        internal PreciseTimeSpan kernelTime;

        /// <summary>
        /// Process time spent in user mode.
        /// </summary>
        internal PreciseTimeSpan userTime;
    }
}
