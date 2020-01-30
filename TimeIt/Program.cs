using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TimeIt
{
    class Program
    {
        /// <summary>
        /// Console print lock. We synchronize stdout and stderr output because of diffrent foreground color.
        /// </summary>
        private static object m_lock = new object();

        /// <summary>
        /// Measured child process.
        /// </summary>
        private static Process m_childProcess = null;

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
        static extern bool GetProcessTimes(IntPtr hProcess,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpExitTime,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        /// <summary>
        /// Convert <see cref="System.Runtime.InteropServices.ComTypes.FILETIME"/> to ticks.
        /// </summary>
        /// <param name="fileTime">WinAPi filetime.</param>
        /// <returns>Converted ticks.</returns>
        static long ComFileTimeToTicks(System.Runtime.InteropServices.ComTypes.FILETIME fileTime) => (((long)fileTime.dwHighDateTime) << 32) | unchecked((uint)fileTime.dwLowDateTime);

        /// <summary>
        /// Tries to query measured process times.
        /// </summary>
        /// <param name="process">Measured process handle.</param>
        /// <param name="processTimes">Measured process times.</param>
        /// <returns>True if received the process times.</returns>
        static bool TryGetProcessTimes(Process process, out ProcessTimes processTimes)
        {
            processTimes = new ProcessTimes();

            System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime, lpExitTime, lpKernel, lpUser;

            bool result = GetProcessTimes(process.Handle, out lpCreationTime, out lpExitTime, out lpKernel, out lpUser);
            if (!result)
            {
                Console.Error.WriteLine("Unable to query process time.");
                return false;
            }

            DateTime creation = DateTime.FromFileTime(ComFileTimeToTicks(lpCreationTime));
            DateTime exit = DateTime.FromFileTime(ComFileTimeToTicks(lpExitTime));

            processTimes.wallTime = PreciseTimeSpan.FromTicks((exit - creation).Ticks);
            processTimes.kernelTime = PreciseTimeSpan.FromTicks(ComFileTimeToTicks(lpKernel));
            processTimes.userTime = PreciseTimeSpan.FromTicks(ComFileTimeToTicks(lpUser));
            return true;
        }

        /// <summary>
        /// Format measured process times to string.
        /// </summary>
        /// <param name="processTimes">Mesured process times.</param>
        /// <returns>Formatted string of measured times.</returns>
        static string FormatProcessTimes(ProcessTimes processTimes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('\n');
            const string formatString = "{0}h {1}min {2}sec {3} ms {4} ns";
            sb.AppendLine("Wall time:\t" + string.Format(formatString, processTimes.wallTime.Hours, processTimes.wallTime.Minutes,
                                                         processTimes.wallTime.Seconds, processTimes.wallTime.Milliseconds,
                                                         processTimes.wallTime.Nanoseconds));

            sb.AppendLine("Kernel time:\t" + string.Format(formatString, processTimes.kernelTime.Hours, processTimes.kernelTime.Minutes,
                                                           processTimes.kernelTime.Seconds, processTimes.kernelTime.Milliseconds,
                                                           processTimes.kernelTime.Nanoseconds));

            sb.AppendLine("User time:\t" + string.Format(formatString, processTimes.userTime.Hours, processTimes.userTime.Minutes,
                                                         processTimes.userTime.Seconds, processTimes.userTime.Milliseconds,
                                                         processTimes.userTime.Nanoseconds));
            return sb.ToString();
        }

        /// <summary>
        /// Append message to the log file.
        /// </summary>
        /// <param name="msg">Message to append.</param>
        static void WriteToLogFile(string msg)
        {
            string logFile = Path.Combine(Directory.GetCurrentDirectory(), "TimeItLog.txt");
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.Write(msg);
                writer.WriteLine("----------------------------------------------------------");
            }
        }

        /// <summary>
        /// Print message to stdout with selected foreground color and then revert to original color.
        /// </summary>
        /// <param name="message">Message to print.</param>
        /// <param name="color">Foreground color.</param>
        /// <param name="error">True if write to stderr.</param>
        static void ColoredPrint(string message, ConsoleColor color, bool error = false)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                if (error)
                    Console.Error.WriteLine(message);
                else
                    Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }

        }

        /// <summary>
        /// Kill process tree.
        /// </summary>
        /// <param name="pid">The parent process Id.</param>
        static void KillProcessTree(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ParentProcessID={pid}");
            var childrenObjects = processSearcher.Get();
            foreach (var child in childrenObjects)
            {
                int childProcessId = Convert.ToInt32(child["ProcessID"]);
                KillProcessTree(childProcessId);
            }
            try
            {
                Process process = Process.GetProcessById(pid);
                //ColoredPrint($"Killing process {process.Id}:{process.ProcessName}", ConsoleColor.Red, true);
                process.Kill();
            }
            catch (Exception e)
            {
                Console.WriteLine("EXCEPTION: " + e.Message);
            }
        }

        static void Main(string[] args)
        {
            // Check that we have received the process filename.
            if (args.Length < 1)
            {
                ColoredPrint("TimeIt.exe filename [arguments]", ConsoleColor.Red, true);
                return;
            }

            // First argument is process file.
            string processFile = args[0];
            // Rest of arguments is to be passed to the created process.
            string arguments = string.Join(" ", args.Skip(1));

            // Check that process file realy exist.
            if (!File.Exists(processFile))
            {
                ColoredPrint("Process file doesn't exist.", ConsoleColor.Red, true);
                return;
            }

            // Dont create new window and redirect outputs.
            ProcessStartInfo startInfo = new ProcessStartInfo(processFile, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Create child process.
            m_childProcess = new Process() { StartInfo = startInfo };

            // Handle cancelation, kill the child process.
            Console.CancelKeyPress += KillMeasuredProcess;

            // Handle stdout and stderr streams.
            m_childProcess.OutputDataReceived += PrintMeasuredProcessStdout;
            m_childProcess.ErrorDataReceived += PrintMeasuredProcessStderr;

            // Start the measured process and begin reading of stdout, stderr.
            m_childProcess.Start();
            m_childProcess.BeginOutputReadLine();
            m_childProcess.BeginErrorReadLine();

            // Wait until measured process exits.
            m_childProcess.WaitForExit();

            // Query measured process times and log them.
            if (TryGetProcessTimes(m_childProcess, out ProcessTimes processTimes))
            {
                string times = FormatProcessTimes(processTimes);
                StringBuilder logBuilder = new StringBuilder();
                logBuilder.Append(processFile).Append(' ').Append(arguments).Append('\n');
                logBuilder.Append(times);
                WriteToLogFile(logBuilder.ToString());
                ColoredPrint(times, ConsoleColor.DarkGreen);
            }
        }

        private static void KillMeasuredProcess(object sender, ConsoleCancelEventArgs e)
        {
            if (m_childProcess != null)
            {
                ColoredPrint("Cancelation request received, killing the child process tree...", ConsoleColor.Red, true);
                KillProcessTree(m_childProcess.Id);
            }
        }

        private static void PrintMeasuredProcessStdout(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lock (m_lock)
                {
                    Console.WriteLine(e.Data);
                }
            }
        }

        private static void PrintMeasuredProcessStderr(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lock (m_lock)
                {
                    ColoredPrint(e.Data, ConsoleColor.Red, true);
                }
            }
        }
    }
}
