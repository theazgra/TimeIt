using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIt.Cli
{
    internal class ParsedOptions
    {
        public string ProcessFile { get; set; }
        public string ProcessArguments { get; set; }
        public bool Silent { get; set; }
        public bool Verbose { get; set; }
        public string MeasuredProcessName { get; set; } = null;
        public bool HasMeasuredProcessName => MeasuredProcessName != null;
    }
}
