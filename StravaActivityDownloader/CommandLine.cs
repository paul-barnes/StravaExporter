using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace StravaExporter
{
    public class Options
    {
        [Option('d', "days", Default = 5, HelpText = "Number of days since today to download activities for")]
        public int Days { get; set; }

        [Option('o', "output-path", HelpText = "Output directory")]
        public string OutputPath { get; set; }
    }
}
