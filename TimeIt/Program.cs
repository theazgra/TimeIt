using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using TimeIt.Cli;
using TimeIt.ProcessUtils;

namespace TimeIt
{
    class Program
    {
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
        static void AppendToLogFile(string msg)
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
        /// Report process time to console and append to log file.
        /// </summary>
        /// <param name="options">Parsed CLI options with process file and arguments.</param>
        /// <param name="pTimes">Measured process times.</param>
        static void ReportProcessTimes(ParsedOptions options, ProcessTimes pTimes, string measuredProcessName)
        {
            string times = pTimes.FormatProcessTimes();
            StringBuilder logBuilder = new StringBuilder();
            logBuilder.Append(DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"))
                .Append('\n')
                .Append("Measured process: ").Append(measuredProcessName).Append('\n')
                .Append("Command: ").Append(options.ProcessFile).Append(' ').Append(options.ProcessArguments)
                .Append('\n')
                .Append(times);
            string report = logBuilder.ToString();
            AppendToLogFile(report);
            ColoredPrint(report, ConsoleColor.DarkGreen);
        }

        static void Main(string[] args)
        {
            // Check that we have received the process filename.
            if (args.Length < 1)
            {
                ColoredPrint("TimeIt.exe filename [arguments]", ConsoleColor.Red, true);
                return;
            }

            // Define cli options.
            SimpleArgParser simpleArgParser = new SimpleArgParser(args, new CliFlag[] {
                new CliFlag(CliFlag.SilentFlag) { HasValue = false, Description = "Silent mode" },
                new CliFlag(CliFlag.ProcessNameFlag){ HasValue = true, Description = "Measured process name" },
                new CliFlag(CliFlag.VerboseFlag) { HasValue  = false, Description = "Report all subprocesses"}
            });

            // Parse cli options.
            ParsedOptions options = simpleArgParser.GetParsedOptions();


            // Dont create new window and redirect outputs if silent is not defined.
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

            // Create process tree from root process.
            m_processTree = new ProcessTree(rootProcess);

            // Wait until measured process exits.
            rootProcess.WaitForExit();

            // Measure execution time of all processes in the tree.
            m_processTree.MeasureExecutionTimeOfTree();

            if (options.Verbose)
            {
                foreach (SubProcess subProcess in m_processTree)
                {
                    string times = $"{subProcess.Name}\n{subProcess.Times.FormatProcessTimes()}";
                    ColoredPrint(times, ConsoleColor.DarkGreen);
                }
            }
            else if (options.HasMeasuredProcessName)
            {
                // If user defined -n option report that process time.
                if (m_processTree.TryGetMeasuredProcess(options.MeasuredProcessName, out ProcessTimes times))
                {
                    ReportProcessTimes(options, times, options.MeasuredProcessName);
                    return;
                }
                else
                {
                    ColoredPrint($"Unable to find requested process with name '{options.MeasuredProcessName}'", ConsoleColor.Red, true);
                    return;
                }
            }

            ReportProcessTimes(options, m_processTree.GetOverallTreeTime(), "TotalProcessTree");
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
