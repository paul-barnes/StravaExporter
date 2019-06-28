using CommandLine;

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
