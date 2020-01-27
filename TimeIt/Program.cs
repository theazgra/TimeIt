using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TimeIt
{
    class Program
    {
        private static object m_lock = new object();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetProcessTimes(IntPtr hProcess,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpExitTime,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
                                           out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        static long ComFileTimeToTicks(System.Runtime.InteropServices.ComTypes.FILETIME fileTime)
        {
            long ticks;
            // Convert 4 high-order bytes to a byte array
            byte[] highBytes = BitConverter.GetBytes(fileTime.dwHighDateTime);
            // Resize the array to 8 bytes (for a Long)
            Array.Resize(ref highBytes, 8);

            // Assign high-order bytes to first 4 bytes of Long
            ticks = BitConverter.ToInt64(highBytes, 0);
            // Shift high-order bytes into position
            ticks <<= 32;
            // Or with low-order bytes
            ticks |= (uint)fileTime.dwLowDateTime;
            // Return long 
            return ticks;
        }

        static string GetProcessTimesString(Process process)
        {
            System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime, lpExitTime, lpKernel, lpUser;

            bool result = GetProcessTimes(process.Handle, out lpCreationTime, out lpExitTime, out lpKernel, out lpUser);
            if (!result)
            {
                Console.Error.WriteLine("Unable to query process time.");
                return string.Empty;
            }

            DateTime creation = DateTime.FromFileTime(ComFileTimeToTicks(lpCreationTime));
            DateTime exit = DateTime.FromFileTime(ComFileTimeToTicks(lpExitTime));

            TimeSpan kernelTime = TimeSpan.FromTicks(ComFileTimeToTicks(lpKernel));
            TimeSpan userTime = TimeSpan.FromTicks(ComFileTimeToTicks(lpUser));
            TimeSpan wallTime = exit - creation;

            bool hour = (wallTime.Hours > 0) || (kernelTime.Hours > 0) || (userTime.Hours > 0);
            bool minute = (wallTime.Minutes > 0) || (kernelTime.Minutes > 0) || (userTime.Minutes > 0);

            StringBuilder sb = new StringBuilder();

            if (hour)
            {
                const string formatString = "{0}h {1}min {2}sec {3} ms";
                sb.AppendLine("Wall time:\t" + string.Format(formatString, wallTime.Hours, wallTime.Minutes, wallTime.Seconds, wallTime.Milliseconds));
                sb.AppendLine("Kernel time:\t" + string.Format(formatString, kernelTime.Hours, kernelTime.Minutes, kernelTime.Seconds, kernelTime.Milliseconds));
                sb.AppendLine("User time:\t" + string.Format(formatString, userTime.Hours, userTime.Minutes, userTime.Seconds, userTime.Milliseconds));
            }
            else if (minute)
            {
                const string formatString = "{0}min {1}sec {2}ms";
                sb.AppendLine("Wall time:\t" + string.Format(formatString, wallTime.Minutes, wallTime.Seconds, wallTime.Milliseconds));
                sb.AppendLine("Kernel time:\t" + string.Format(formatString, kernelTime.Minutes, kernelTime.Seconds, kernelTime.Milliseconds));
                sb.AppendLine("User time:\t" + string.Format(formatString, userTime.Minutes, userTime.Seconds, userTime.Milliseconds));
            }
            else
            {
                const string formatString = "{0}sec {1}ms";
                sb.AppendLine("Wall time:\t" + string.Format(formatString, wallTime.Seconds, wallTime.Milliseconds));
                sb.AppendLine("Kernel time:\t" + string.Format(formatString, kernelTime.Seconds, kernelTime.Milliseconds));
                sb.AppendLine("User time:\t" + string.Format(formatString, userTime.Seconds, userTime.Milliseconds));
            }
            return sb.ToString();
        }

        static void WriteToLogFile(string msg)
        {
            string logFile = Path.Combine(Directory.GetCurrentDirectory(), "TimeItLog.txt");
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.Write(msg);
                writer.WriteLine("----------------------------------------------------------");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("TimeIt.exe filename [arguments]");
                return;
            }
            StringBuilder logBuilder = new StringBuilder();

            string processFile = args[0];
            string arguments = string.Join(" ", args.Skip(1));
            logBuilder.Append(processFile).Append(' ').Append(arguments).Append('\n');

            ProcessStartInfo startInfo = new ProcessStartInfo(processFile, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = new Process()
            {
                StartInfo = startInfo,
            };

            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.OutputDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();

            string times = GetProcessTimesString(process);
            logBuilder.Append(times);
            WriteToLogFile(logBuilder.ToString());
            Console.WriteLine(times);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lock (m_lock)
                {
                    Console.WriteLine("stdout: " + e.Data);
                }

            }
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                var originalColor = Console.ForegroundColor;
                lock (m_lock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("stderr: " + e.Data);
                    Console.ForegroundColor = originalColor;
                }
            }
        }
    }
}
