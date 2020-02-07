using System.Text;

namespace TimeIt.ProcessUtils
{
    internal readonly struct ProcessTimes
    {
        /// <summary>
        /// Total wall time of the measured process.
        /// </summary>
        internal readonly PreciseTimeSpan WallTime;

        /// <summary>
        /// Process time spent in user mode.
        /// </summary>
        internal readonly PreciseTimeSpan UserTime;

        /// <summary>
        /// Process time spent in kernel mode.
        /// </summary>
        internal readonly PreciseTimeSpan KernelTime;

        public static ProcessTimes Zero => new ProcessTimes(PreciseTimeSpan.Zero, PreciseTimeSpan.Zero, PreciseTimeSpan.Zero);


        internal ProcessTimes(PreciseTimeSpan wallTime, PreciseTimeSpan userTime, PreciseTimeSpan kernelTime)
        {
            WallTime = wallTime;
            UserTime = userTime;
            KernelTime = kernelTime;
        }

        internal ProcessTimes(long wallTimeTicks, long userTimeTicks, long kernelTimeTicks)
        {
            WallTime = PreciseTimeSpan.FromTicks(wallTimeTicks);
            UserTime = PreciseTimeSpan.FromTicks(userTimeTicks);
            KernelTime = PreciseTimeSpan.FromTicks(kernelTimeTicks);
        }

        /// <summary>
        /// Format measured process times to string.
        /// </summary>
        /// <param name="processTimes">Mesured process times.</param>
        /// <returns>Formatted string of measured times.</returns>
        internal string FormatProcessTimes()
        {
            StringBuilder sb = new StringBuilder();
            const string formatString = "{0}h {1}min {2}sec {3} ms {4} ns";
            sb.AppendLine("Wall time:\t" + string.Format(formatString, WallTime.Hours, WallTime.Minutes,
                                                         WallTime.Seconds, WallTime.Milliseconds,
                                                         WallTime.Nanoseconds));

            sb.AppendLine("Kernel time:\t" + string.Format(formatString, KernelTime.Hours, KernelTime.Minutes,
                                                           KernelTime.Seconds, KernelTime.Milliseconds,
                                                           KernelTime.Nanoseconds));

            sb.AppendLine("User time:\t" + string.Format(formatString, UserTime.Hours, UserTime.Minutes,
                                                         UserTime.Seconds, UserTime.Milliseconds,
                                                         UserTime.Nanoseconds));
            return sb.ToString();
        }
    }
}
