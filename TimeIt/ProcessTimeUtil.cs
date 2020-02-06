using System;
using System.Runtime.InteropServices;

namespace TimeIt
{
    internal static class ProcessTimeUtil
    {
        /// <summary>
        /// Get process times.
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="lpCreationTime">Creation time of the measured process.</param>
        /// <param name="lpExitTime">Exit time of the measured process.</param>
        /// <param name="lpKernelTime">Kernel time of the measured process.</param>
        /// <param name="lpUserTime">User time of the measured process.</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessTimes(IntPtr hProcess,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME lpExitTime,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        /// <summary>
        /// Convert <see cref="System.Runtime.InteropServices.ComTypes.FILETIME"/> to ticks.
        /// </summary>
        /// <param name="fileTime">WinAPi filetime.</param>
        /// <returns>Converted ticks.</returns>
        internal static long ComFileTimeToTicks(System.Runtime.InteropServices.ComTypes.FILETIME fileTime) => (((long)fileTime.dwHighDateTime) << 32) | unchecked((uint)fileTime.dwLowDateTime);

        /// <summary>
        /// Tries to query measured process times.
        /// </summary>
        /// <param name="ptr">Process handle.</param>
        /// <param name="processTimes">Measured process times.</param>
        /// <returns>True if received the process times.</returns>
        internal static bool TryGetProcessTimes(IntPtr ptr, out ProcessTimes processTimes)
        {
            processTimes = new ProcessTimes();

            System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime, lpExitTime, lpKernel, lpUser;

            bool result = GetProcessTimes(ptr, out lpCreationTime, out lpExitTime, out lpKernel, out lpUser);
            if (!result)
            {
                Console.Error.WriteLine("Unable to query process time.");
                return false;
            }

            DateTime creation = DateTime.FromFileTime(ComFileTimeToTicks(lpCreationTime));
            DateTime exit = DateTime.FromFileTime(ComFileTimeToTicks(lpExitTime));

            processTimes = new ProcessTimes(PreciseTimeSpan.FromTicks((exit - creation).Ticks),
                                            PreciseTimeSpan.FromTicks(ComFileTimeToTicks(lpUser)),
                                            PreciseTimeSpan.FromTicks(ComFileTimeToTicks(lpKernel)));

            return true;
        }
    }
}
