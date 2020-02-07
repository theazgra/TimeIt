using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using TimeIt.Cli;
using TimeIt.ProcessUtils;

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

        static void ReportProcessTimes(ParsedOptions options, ProcessTimes pTimes)
        {
            string times = pTimes.FormatProcessTimes();
            StringBuilder logBuilder = new StringBuilder();
            logBuilder.Append(options.ProcessFile).Append(' ').Append(options.ProcessArguments).Append('\n');
            logBuilder.Append(times);
            WriteToLogFile(logBuilder.ToString());
            ColoredPrint(times, ConsoleColor.DarkGreen);
        }

        static void Main(string[] args)
        {
            SimpleArgParser simpleArgParser = new SimpleArgParser(args, new CliFlag[] {
                new CliFlag('s') { HasValue = false, Description = "Silent mode" },
                new CliFlag('n'){ HasValue = true, Description = "Measured process name" },
                new CliFlag('v') {HasValue  = false, Description = "Report all subprocesses"}
            });

            ParsedOptions options = simpleArgParser.GetParsedOptions();

            // Check that we have received the process filename.
            if (args.Length < 1)
            {
                ColoredPrint("TimeIt.exe filename [arguments]", ConsoleColor.Red, true);
                return;
            }

            // Dont create new window and redirect outputs.
            ProcessStartInfo startInfo = new ProcessStartInfo(options.ProcessFile, options.ProcessArguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = !options.Silent,
                RedirectStandardError = !options.Silent
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

            if (!options.Silent)
            {
                rootProcess.BeginOutputReadLine();
                rootProcess.BeginErrorReadLine();
            }

            m_processTree = new ProcessTree(rootProcess);

            // Wait until measured process exits.
            rootProcess.WaitForExit();

            m_processTree.MeasureExecutionTimeOfTree();

            if (options.Verbose)
            {
                foreach (SubProcess subProcess in m_processTree)
                {
                    string times =  $"{subProcess.Name}\n{subProcess.Times.FormatProcessTimes()}";
                    ColoredPrint(times, ConsoleColor.DarkGreen);
                }
            }
            else if (options.HasMeasuredProcessName)
            {
                if (m_processTree.TryGetMeasuredProcess(options.MeasuredProcessName, out ProcessTimes times))
                {
                    ReportProcessTimes(options, times);
                    return;
                }
                else
                {
                    ColoredPrint($"Unable to find requested process with name '{options.MeasuredProcessName}'", ConsoleColor.Red, true);
                    return;
                }
            }

            ReportProcessTimes(options, m_processTree.GetOverallTreeTime());
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
