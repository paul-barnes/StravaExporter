using CommandLine;
using System.Collections.Generic;

namespace StravaExporter
{
    [Verb("Authorize", HelpText = "Authorize this app with Strava to access user data")]
    public class AuthorizeOptions
    {
        [Option('p', "port", Default = 8080, HelpText = "Port to use to listen for Strava's callback response on the redirect url")]
        public int Port { get; set; }
    }

    public abstract class CommonOptions
    {
        [Option('o', "output-path", HelpText = "Output directory. Will be taken from config file \"output_directory\" if not provided.")]
        public string OutputPath { get; set; }
    }

    [Verb("Activity", HelpText = "Export the specified activity id(s) to TCX file(s)")]
    public class ActivityOptions : CommonOptions
    {
        [Value(0, Required = true, HelpText = "Activity id(s)", MetaName = "Activity Id(s)")]
        public IList<long> Activities { get; set; } // represents 'free floating' command line values not parsed as options
    }

    [Verb("Export", HelpText = "Export all activities in the past 'days' number of days")]
    public class ExportOptions : CommonOptions
    {
        [Option('d', "days", Default = 5, HelpText = "Download all activities from this many days ago to now")]
        public int Days { get; set; }
    }
}
