using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIt.Cli
{
    internal class SimpleArgParser
    {
        public string ProcessFile { get; private set; }
        public string ProcessArgumets { get; private set; }

        const char FLAG_CHAR = '-';

        private string[] m_args;
        private List<CliFlag> m_matchedFlags;
        IEnumerable<CliFlag> m_possibleFlags;
        internal SimpleArgParser(IEnumerable<CliFlag> flags)
        {
            m_possibleFlags = flags;
        }

        public bool Parse(string[] args)
        {
            m_args = args;
            return InternalParse();
        }


        private bool InternalParse()
        {
            m_matchedFlags = new List<CliFlag>();

            for (int i = 0; i < m_args.Length; i++)
            {
                if (m_args[i][0] == FLAG_CHAR && m_possibleFlags.SingleOrDefault(f => f.Match(m_args[i][1])) is CliFlag flag)
                {
                    if (flag.HasValue)
                    {
                        if (i == m_args.Length - 1)
                        {
                            Console.Error.WriteLine("Missing value for flag {0}", flag.Matcher);
                            return false;
                        }
                        m_matchedFlags.Add(new CliFlag(m_args[i][1], m_args[i + 1]));
                        i += 1;
                    }
                    else
                    {
                        m_matchedFlags.Add(new CliFlag(m_args[i][1]));
                    }
                }
                else if (m_args[i][0] == FLAG_CHAR)
                {
                    Console.Error.WriteLine("Unknown flag {0}", m_args[i]);
                    return false;
                }
                else
                {
                    ProcessFile = m_args[i];
                    ProcessArgumets = string.Join(" ", m_args.Skip(i + 1));
                    break;
                }
            }
            return true;
        }

        internal bool HasMatched(char flagChar) => m_matchedFlags.Any(f => f.Match(flagChar));
        internal T GetFlagValue<T>(char flagChar) => (T)Convert.ChangeType(m_matchedFlags.SingleOrDefault(f => f.Match(flagChar))?.Value, typeof(T));

        internal void Report()
        {
            foreach (var flag in m_matchedFlags)
            {
                Console.WriteLine(flag);
            }
            Console.WriteLine("ProcessFile: {0}", ProcessFile);
            Console.WriteLine("ProcessArgumets: {0}", ProcessArgumets);
        }

        internal ParsedOptions GetParsedOptions()
        {
            ParsedOptions options = new ParsedOptions()
            {
                ProcessFile = this.ProcessFile,
                ProcessArguments = this.ProcessArgumets,
                Verbose = HasMatched(CliFlag.VerboseFlag),
                Silent = HasMatched(CliFlag.SilentFlag),
                MeasuredProcessName = HasMatched(CliFlag.ProcessNameFlag) ? GetFlagValue<string>(CliFlag.ProcessNameFlag) : null
            };
            return options;

        }
    }
}
