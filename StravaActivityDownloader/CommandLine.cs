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
    public enum OutputFormat
    {
        Original,
        GPX,
        TCX,
        MakeTCX
    }

    public abstract class CommonOptions
    {
        [Option('o', "output-path", HelpText = "Output directory. Will be taken from config file \"output_directory\" if not provided.")]
        public string OutputPath { get; set; }

        [Option('s', "save-config", HelpText = "Save options --output-path, --days, and/or --output-fomat as the defaults")]
        public bool SaveConfiguration { get; set; }

        [Option("fix-hrspikes", HelpText = "Replace heart rate values above this limit with the average heart rate as computed without these values. Only used with MakeTCX output format.")]
        public int? FixHRSpikesAbove { get; set; }

        [Option('f', "output-format", HelpText = "Output format. Can be 'original', 'gpx', 'tcx', or 'MakeTCX'. MakeTCX generates the tcx from raw Strava data. Other options require a login to Strava and download the requested format directly. Default is original.")]
        public string OutputFormatString { get; set; }

        public OutputFormat OutputFormat { get => (OutputFormat)System.Enum.Parse(typeof(OutputFormat), OutputFormatString, true); }

        [Option('u', "user", HelpText = "Strava username (email)")]
        public string Username { get; set; }

        [Option('p', "password", HelpText = "Strava password")]
        public string Password { get; set; }

        [Option("save-credentials", HelpText = "Save the Strava credentials securely in Windows Credential Manager")]
        public bool SaveCredentials { get; set; }

        [Option("silent", Default = false, HelpText = "Do not prompt for credentials or display any other UI or prompts")]
        public bool Silent{ get; set; }

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
        [Option('d', "days", HelpText = "Download all activities from this many days ago to now. Default: 5")]
        public int? Days { get; set; }
    }
}
