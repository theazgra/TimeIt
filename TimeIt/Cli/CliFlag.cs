using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIt.Cli
{
    internal class CliFlag
    {
        internal const char SilentFlag = 's';
        internal const char VerboseFlag = 'v';
        internal const char HelpFlag = 'h';
        internal const char ProcessNameFlag = 'n';

        public bool HasValue { get; set; }
        public char Matcher { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }

        internal CliFlag(char name)
        {
            Matcher = name;
            HasValue = false;
        }

        internal CliFlag(char name, string value)
        {
            Matcher = name;
            Value = value;
            HasValue = true;
        }

        public override string ToString()
        {
            if (HasValue)
            {
                return $"FlagWithValue: {Matcher}; Value: {Value}";
            }
            else
            {
                return $"Flag: {Matcher}";
            }
        }

        internal bool Match(char flag)
        {
            return (Matcher == flag);
        }
    }
}
