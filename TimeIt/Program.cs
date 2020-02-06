using System;
using System.Collections.Generic;
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
        /// Siletion options. If enabled, stdout and stderr won't be redirected.
        /// </summary>
        const string SilentOption = "-s";

        /// <summary>
        /// Console print lock. We synchronize stdout and stderr output because of diffrent foreground color.
        /// </summary>
        private static readonly object m_lock = new object();

        /// <summary>
        /// Measured child process.
        /// </summary>
        private static ProcessTree m_processTree = null;

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

        static void Main(string[] args)
        {
            bool silent = false;
            int offset = 0;
            // Check that we have received the process filename.
            if (args.Length < 1)
            {
                ColoredPrint("TimeIt.exe filename [arguments]", ConsoleColor.Red, true);
                return;
            }

            if (args[0] == SilentOption)
            {
                silent = true;
                offset = 1;
            }

            // First argument is process file.
            string processFile = args[0 + offset];
            // Rest of arguments is to be passed to the created process.
            string arguments = string.Join(" ", args.Skip(1 + offset));

            // Check that process file realy exist.
            //if (!File.Exists(processFile))
            //{
            //    ColoredPrint("Process file doesn't exist.", ConsoleColor.Red, true);
            //    return;
            //}

            // Dont create new window and redirect outputs.
            ProcessStartInfo startInfo = new ProcessStartInfo(processFile, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = !silent,
                RedirectStandardError = !silent
            };

            // Create child process.
            Process rootProcess = new Process() { StartInfo = startInfo };

            // Handle cancelation, kill the child process.
            Console.CancelKeyPress += KillMeasuredProcess;

            // Handle stdout and stderr streams.
            rootProcess.OutputDataReceived += PrintMeasuredProcessStdout;
            rootProcess.ErrorDataReceived += PrintMeasuredProcessStderr;

            // Start the measured process and begin reading of stdout, stderr.
            rootProcess.Start();

            if (!silent)
            {
                rootProcess.BeginOutputReadLine();
                rootProcess.BeginErrorReadLine();
            }

            m_processTree = new ProcessTree(rootProcess);

            // Wait until measured process exits.
            rootProcess.WaitForExit();
            
            m_processTree.MeasureExecutionTimeOfTree();

            //foreach (var sp in m_processTree)
            //{
            //    Console.WriteLine(sp.Name);
            //    Console.WriteLine(sp.Times.FormatProcessTimes());
            //}

            //foreach (var process in subprocesses)
            //{
            //    ColoredPrint($"Printing info about process: {process.Item2}", ConsoleColor.DarkBlue);
            //    // Query measured process times and log them.
            //    if (TryGetProcessTimes(process.Item1, out ProcessTimes pt))
            //    {
            //        string times = FormatProcessTimes(pt);

            //        ColoredPrint(times, ConsoleColor.Blue);
            //    }
            //}

            {
                string times = m_processTree.GetOverallTreeTime().FormatProcessTimes()
                StringBuilder logBuilder = new StringBuilder();
                logBuilder.Append(processFile).Append(' ').Append(arguments).Append('\n');
                logBuilder.Append(times);
                WriteToLogFile(logBuilder.ToString());
                ColoredPrint(times, ConsoleColor.DarkGreen);
            }
        }

        private static void KillMeasuredProcess(object sender, ConsoleCancelEventArgs e)
        {
            if (m_processTree != null)
            {
                ColoredPrint("Cancelation request received, killing the child process tree...", ConsoleColor.Red, true);
                m_processTree.KillProcessTree();
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
