using CommandLine;

namespace StravaExporter
{
    public class Options
    {
        [Option('d', "days", Default = 5, HelpText = "Download all activities from this many days ago to now")]
        public int Days { get; set; }

        [Option('a', "activity", HelpText = "Download only this specific Strava activity by id; overrides -d")]
        public long Activity { get; set; }

        [Option('o', "output-path", HelpText = "Output directory. Will be taken from config file \"output_directory\" if not provided.")]
        public string OutputPath { get; set; }
    }
}
