using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using AdysTech.CredentialManager;
using CommandLine;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json;
using StravaExporter.Properties;
using ImpromptuInterface;
using Dynamitey;
using System.Threading;
using System.Collections.Concurrent;

namespace StravaExporter
{
    public interface IActivityInfo
    {
        long? Id { get; }
        string Name { get; }
        DateTime? StartDateLocal { get; }
    }

    public static class Extensions
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            return Task.WhenAll(
                from item in source
                select Task.Run(() => body(item)));
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }
    }
    class Program
    {
        internal static string client_id = "36425";
        internal static string client_secret = "ee2979ecc9afe77bff701ed5fd5ff192632fea88";
        static void Main(string[] args)
        {
            try
            {
                if (Settings.Default.SettingsUpgradeRequired)
                {
                    try
                    {
                        Settings.Default.Upgrade();
                        Settings.Default.SettingsUpgradeRequired = false;
                    }
                    catch (Exception e)
                    {
                        // Upgrade failed - tell the user or whatever
                        string s = e.Message;
                    }
                }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var parser = new Parser(settings =>
                {
                    settings.CaseSensitive = false;
                    settings.HelpWriter = Console.Error;
                });
                var cmd = parser.ParseArguments<AuthorizeOptions, ActivityOptions, ExportOptions>(args);
                cmd
                    .WithParsed<AuthorizeOptions>(o => HandleAuthorize(o))
                    .WithParsed<ActivityOptions>(o => HandleActivity(o))
                    .WithParsed<ExportOptions>(o => HandleExport(o));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                Settings.Default.Save();
            }
        }

        static bool CanPromptForCredentials(CommonOptions opts)
        {
            if (!string.IsNullOrEmpty(opts.Username) && !string.IsNullOrEmpty(opts.Password))
                return false;
            if (opts.Silent)
                return false;
            return true;
        }

        static NetworkCredential GetStravaCredentials(CommonOptions opts, bool allowSavedCredentials)
        {
            bool save = opts.SaveCredentials;
            NetworkCredential cred = null;
            if(!string.IsNullOrEmpty(opts.Username) && !string.IsNullOrEmpty(opts.Password))
                cred = new NetworkCredential(opts.Username, opts.Password);
            else if(allowSavedCredentials)
                cred = CredentialManager.GetCredentials(StravaHttpClient.BASE_URL, CredentialType.Generic);
            if(cred == null)
            {
                if (opts.Silent)
                    throw new Exception("No Strava Credentials found");
                save = true; // show the save checkbox on dialog and return state of checkbox
                cred = CredentialManager.PromptForCredentials(StravaHttpClient.BASE_URL, ref save, "Please provide credentials", "Strava Credentials");
                if (cred == null)
                    throw new Exception("No Strava Credentials provided");
            }
            if(save)
            {
                try
                {
                    var icred = cred.ToICredential();
                    icred.Persistance = Persistance.LocalMachine;
                    icred.TargetName = StravaHttpClient.BASE_URL;
                    icred.SaveCredential();
                }
                catch(Exception e)
                {
                    Console.WriteLine("Failed to save the credentials: " + e.Message);
                }
            }

            return cred;
        }

        static void HandleAuthorize(AuthorizeOptions opt)
        {
            new StravaAuthorizer().Authorize(client_id, client_secret, opt.Port);
        }

        static void HandleActivity(ActivityOptions opt)
        {
            ValidateOptions(opt);

            Configuration.Default.AccessToken = RefreshAccessToken().GetAwaiter().GetResult();

            var activityIds = opt.Activities.ToList<long>();
            if (activityIds.Count == 0)
                throw new Exception("No activities were specified. Please provide one or more Strava activity ids separated by space");

            Console.WriteLine();
            if(activityIds.Count == 1)
                Console.WriteLine("Downloading the specified activity and saving to directory {0}", opt.OutputPath);
            else
                Console.WriteLine("Downloading the specified activities and saving to directory {0}", opt.OutputPath);
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            int nbExported;
            var activitiesApi = new ActivitiesApi();
            if (opt.OutputFormat == OutputFormat.MakeTCX)
                nbExported = DownloadActivitiesAndBuildTCX(activitiesApi, opt, activityIds);
            else
                nbExported = DownloadActivities(activitiesApi, opt, 
                    GetDetailedActivities(activitiesApi, activityIds).AllActLike<IActivityInfo>(),
                    true);

            Console.WriteLine();
            Console.WriteLine("Exported {0} activities in {1:F3} seconds", nbExported, sw.ElapsedMilliseconds / 1000.0);

            if (opt.SaveConfiguration)
            {
                Settings.Default.OutputDirectory = opt.OutputPath;
                Settings.Default.OutputFormat = opt.OutputFormatString;
                Console.WriteLine("Saved the output directory {0} and output format {1} as the defaults", opt.OutputPath, opt.OutputFormatString);
            }
        }

        static void HandleExport(ExportOptions opt)
        {
            ValidateOptions(opt);

            Configuration.Default.AccessToken = RefreshAccessToken().GetAwaiter().GetResult();

            // downloading all activities between today and opt.Days ago 
            Console.WriteLine();
            Console.WriteLine("Downloading activities for the past {0} days and saving to directory {1}",
                opt.Days != null ? opt.Days.Value : Settings.Default.Days,
                opt.OutputPath);

            Console.WriteLine();

            List<long> activityIds;
            List<SummaryActivity> summaryActivities;
            var activitiesApi = new ActivitiesApi();

            var sw = Stopwatch.StartNew();

            int nbActivities = GetActivities(opt, activitiesApi, out activityIds, out summaryActivities);
            int nbExported;
            if (opt.OutputFormat == OutputFormat.MakeTCX)
                nbExported = DownloadActivitiesAndBuildTCX(activitiesApi, opt, activityIds);
            else
                nbExported = DownloadActivities(activitiesApi, opt, summaryActivities.AllActLike<IActivityInfo>(), false);
            
            Console.WriteLine();
            if (nbActivities == 0)
                Console.WriteLine("No activities existed to export");
            else if (nbExported == 0)
                Console.WriteLine("No new activities were found to export");
            else
                Console.WriteLine("Exported {0} of {1} activities in {2:F3} seconds", nbExported, nbActivities, sw.ElapsedMilliseconds / 1000.0);

            if(opt.SaveConfiguration)
            {
                Settings.Default.OutputDirectory = opt.OutputPath;
                if (opt.Days != null)
                    Settings.Default.Days = opt.Days.Value;
                Settings.Default.OutputFormat = opt.OutputFormatString;
                Console.WriteLine("Saved the output path, days, and output format as the defaults.");
            }
        }

        static void ValidateOptions(CommonOptions opt)
        {
            if (string.IsNullOrEmpty(opt.OutputPath))
            {
                opt.OutputPath = Settings.Default.OutputDirectory;
                if (string.IsNullOrEmpty(opt.OutputPath))
                    throw new Exception("No output path specified in command line and none found in config file");
            }
            if (!Directory.Exists(opt.OutputPath))
                throw new Exception(string.Format("The specified output directory {0} does not exist", opt.OutputPath));

            if (string.IsNullOrEmpty(opt.OutputFormatString))
            {
                if (!string.IsNullOrEmpty(Settings.Default.OutputFormat))
                    opt.OutputFormatString = Settings.Default.OutputFormat;
                else
                    opt.OutputFormatString = OutputFormat.Original.ToString();
            }

            OutputFormat f;
            if(!System.Enum.TryParse(opt.OutputFormatString, true, out f))
                throw new Exception(string.Format("The specified output format {0} is invalid", opt.OutputFormatString));
        }

        static void FixHRSpikesAbove(int cap, List<int?> time, List<int?> heartRates)
        {
            var indices = new List<int>();
            for(int i=0; i<heartRates.Count; ++i)
                if(heartRates[i].HasValue && heartRates[i].Value > cap)
                    indices.Add(i);

            int avgHr = cap, prevAvgHr = -1;
            while(Math.Abs(avgHr - prevAvgHr) > 1)
            {
                prevAvgHr = avgHr;
                foreach (int idx in indices)
                    heartRates[idx] = avgHr;
                avgHr = GetLapAvgHeartRate(time, heartRates, 0, heartRates.Count - 1);
            } 
            if(indices.Count > 0)
            {
                Console.WriteLine(string.Format("Replaced {0} heart rate datapoints above {1} with {2}, the average hr without these spurious values",
                    indices.Count, cap, avgHr));
            }
        }

        private static int HelpDownloadActivities(StravaHttpClient client, CommonOptions opts, IEnumerable<IActivityInfo> activities)
        {
            int nbExported = 0;
            object syncConsole = new object();
            Task allTasks = activities.ForEachAsync((IActivityInfo activityInfo) =>
            {
                Task<string> t =  client.DownloadActivity(activityInfo.Id.Value, opts.OutputPath, GetFileBaseName(activityInfo), opts.OutputFormat);
                return t.ContinueWith( (prevTask) =>
                {
                    lock (syncConsole)
                    {
                        if (prevTask.IsFaulted)
                        {
                            Console.WriteLine();
                            Console.WriteLine("An error occurred downloading {0}:", GetFileBaseName(activityInfo), prevTask.Exception.ToString());
                            Console.WriteLine(prevTask.Exception.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Downloaded {0}", Path.GetFileName(prevTask.Result));
                            ++nbExported;
                        }
                    }
                });
            });

            try
            {
                allTasks.Wait();
            }
            catch(Exception)
            {
                // don't actually get here because the ContinueWith task to output the results does not propagate the exception
            }
            return nbExported;
        }

        private static int HelpDownloadActivitiesWithOverwritePrompt(StravaHttpClient client, CommonOptions opts, IEnumerable<IActivityInfo> activities)
        {
            int nbExported = 0;
            foreach (var activityInfo in activities)
            {
                var downloadable = client.BeginDownloadActivity(activityInfo.Id.Value,
                    opts.OutputPath, GetFileBaseName(activityInfo), opts.OutputFormat).GetAwaiter().GetResult();

                // we don't/can't know the extension when downloading original file format, until 
                // we make the request and get the filename from the headers 
                // so we've given the DownloadableActivity what it needs to build the full pathname
                // once the extension is known.
                string pathname = downloadable.GetTargetPathname();
                if (File.Exists(pathname))
                {
                    // this only happens with Activity verb for downloading specific activity id(s);
                    // with Export verb, we automatically skip activities which were already 
                    // downloaded (GetActivities just does not return them)
                    if (opts.Silent)
                    {
                        Console.WriteLine("The file {0} exists; skipping.", Path.GetFileName(pathname));
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("The file {0} exists; overwrite? [Y/N]", Path.GetFileName(pathname));
                        string s = Console.ReadLine();
                        if (s != "Y" && s != "y")
                            continue;
                    }
                }

                downloadable.Download().GetAwaiter().GetResult();
                ++nbExported;
                Console.WriteLine("Exported activity {0}", Path.GetFileName(pathname));
            }
            return nbExported;
        }

        static int DownloadActivities(
            ActivitiesApi activitiesApi,
            CommonOptions opts,
            IEnumerable<IActivityInfo> activities,
            bool promptToOverwrite)
        {
            if (activities.Count() == 0)
                return 0;

            int nbExported = 0;

            var credentials = GetStravaCredentials(opts, true);

            ServicePointManager.FindServicePoint(new Uri(StravaHttpClient.BASE_URL)).ConnectionLimit = Math.Min(Environment.ProcessorCount, 8);

            using (var client = new StravaHttpClient())
            {
                bool bLoggedIn = client.LogIn(credentials.UserName, credentials.Password).GetAwaiter().GetResult();
                if (!bLoggedIn)
                {
                    if(CanPromptForCredentials(opts))
                    {
                        credentials = GetStravaCredentials(opts, false);
                        bLoggedIn = client.LogIn(credentials.UserName, credentials.Password).GetAwaiter().GetResult();
                    }
                }
                if(!bLoggedIn)
                    throw new Exception("Failed to log into Strava. Please check your credentials.");

                if (promptToOverwrite)
                    nbExported = HelpDownloadActivitiesWithOverwritePrompt(client, opts, activities);
                else
                    nbExported = HelpDownloadActivities(client, opts, activities);
            }
            return nbExported;
        }

        // old way, where we build the tcx ourselves from the data from the streams api
        static int DownloadActivitiesAndBuildTCX(
            ActivitiesApi activitiesApi, 
            CommonOptions opts,
            List<long> activityIds)
        {
            int nbExported = 0;
            string outputPath = opts.OutputPath;
            int? fixHrSpikesAbove = opts.FixHRSpikesAbove;

            var streamsApi = new StreamsApi();
            for (int i = 0; i < activityIds.Count; ++i)
            {
                long activityId = activityIds[i];

                DetailedActivity detailedActivity = activitiesApi.GetActivityById(activityId, true);

                string pathname = BuildFilePathName(outputPath, detailedActivity.ActLike<IActivityInfo>(), ".tcx");
                if (File.Exists(pathname))
                {
                    // this only happens with Activity verb for downloading specific activity id(s);
                    // with Export verb, we automatically skip activities which were already 
                    // downloaded (GetActivities just does not return them)
                    if (opts.Silent)
                    {
                        Console.WriteLine("The file {0} exists; skipping.", Path.GetFileName(pathname));
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("The file {0} exists; overwrite? [Y/N]", Path.GetFileName(pathname));
                        string s = Console.ReadLine();
                        if (s != "Y" && s != "y")
                            continue;
                    }
                }

                StreamSet streams = streamsApi.GetActivityStreams(activityId,
                    new List<string>()
                    {
                        "distance",
                        "time",
                        "latlng",
                        "altitude",
                        "heartrate",
                        "cadence",
                        //"temp",
                        "watts",
                        "velocity_smooth",
                        "moving"
                    },
                    true);

                DistanceStream dist = streams.Distance; // float?
                TimeStream time = streams.Time; // int?
                LatLngStream latlng = streams.Latlng; // IO.Swagger.Model.LatLng
                AltitudeStream altitude = streams.Altitude; // float?
                HeartrateStream hr = streams.Heartrate; // int?
                CadenceStream cadence = streams.Cadence; // int?
                //TemperatureStream temp = streams.Temp; // int?
                PowerStream watts = streams.Watts; // int?
                SmoothVelocityStream velocity = streams.VelocitySmooth; // float?
                MovingStream moving = streams.Moving; // bool?

                if (fixHrSpikesAbove.HasValue && detailedActivity.HasHeartRate != null &&
                    detailedActivity.HasHeartRate.Value && hr != null)
                {
                    FixHRSpikesAbove(fixHrSpikesAbove.Value, time.Data, hr.Data);
                }

                WriteTCX(pathname,
                    detailedActivity,
                    dist != null ? dist.Data : null,
                    time != null ? time.Data : null,
                    latlng != null ? latlng.Data : null,
                    altitude != null ? altitude.Data : null,
                    hr != null ? hr.Data : null,
                    cadence != null ? cadence.Data : null,
                    watts != null ? watts.Data : null,
                    velocity != null ? velocity.Data : null,
                    moving != null ? moving.Data : null);

                ++nbExported;
                Console.WriteLine("Exported activity {0}", Path.GetFileName(pathname));
            }
            return nbExported;
        }

        static List<DetailedActivity> GetDetailedActivities(ActivitiesApi activitiesApi, IEnumerable<long> activityIds)
        {
            var activities = new List<DetailedActivity>();
            foreach (var id in activityIds)
                activities.Add(activitiesApi.GetActivityById(id, true));
            return activities;
        }


        static int GetActivities(ExportOptions opt, ActivitiesApi activitiesApi, out List<long> activityIds, out List<SummaryActivity> summaryActivities)
        {
            activityIds = new List<long>();
            summaryActivities = new List<SummaryActivity>();

            // get everything from now to opt.Days ago; 
            // adjust the time on "after" however so we include 
            // things that happened any time on that oldest day
            // and not just since the current time on that day
            int days = opt.Days != null ? opt.Days.Value : Settings.Default.Days;
            DateTime dtBefore = DateTime.UtcNow;
            int before = GetEpochTime(dtBefore);
            DateTime dtAfter = new DateTime(dtBefore.Year, dtBefore.Month, dtBefore.Day, 0, 0, 0, DateTimeKind.Utc);
            dtAfter = dtAfter.AddDays(-days);
            int after = GetEpochTime(dtAfter);

            for (int page = 1; true; ++page)
            {
                var activities = activitiesApi.GetLoggedInAthleteActivities(before, after, page, 30);
                if (activities == null || activities.Count == 0)
                    break;

                foreach (var activity in activities)
                {
                    if (opt.OutputFormat == OutputFormat.MakeTCX &&
                        activity.Type != ActivityType.Ride && 
                        activity.Type != ActivityType.Run && 
                        activity.Type != ActivityType.Walk)
                    {
                        // can't make a tcx for these; but if downloading from strava directly, we don't need to skip
                        Console.WriteLine("Skipping activity [{0}] on [{1}] with type [{2}]", activity.Name, activity.StartDateLocal.ToString(), activity.Type.ToString());
                        continue;
                    }
                    string pathname;
                    if (ActivityFileExists(opt.OutputPath, activity.ActLike<IActivityInfo>(), out pathname))
                    {
                        Console.WriteLine("Skipping activity because it has already been exported: {0} ", Path.GetFileName(pathname));
                        continue;
                    }
                    activityIds.Add(activity.Id.Value);
                    summaryActivities.Add(activity);
                }
            }
            Console.WriteLine();
            return activityIds.Count;
        }

        static string RemoveInvalidFilenameChars(string s)
        {
            char[] badChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\r', '\n', '\t' };
            foreach (char c in badChars)
                s = s.Replace(c, '_');
            return s;
        }

        static string GetFileBaseName(string activityName, DateTime startDate)
        {
            string name = string.Format("{0}_{1}",
                RemoveInvalidFilenameChars(activityName),
                startDate.ToString("yyyyMMddTHHmmss"));
            name = name.Replace(' ', '_');
            while (name.Contains("__"))
                name = name.Replace("__", "_");
            return name;
        }
        static string GetFileBaseName(IActivityInfo activityInfo)
        {
            return GetFileBaseName(activityInfo.Name, activityInfo.StartDateLocal.Value);
        }

        static string BuildFilePathName(string outputPath, string activityName, DateTime startDate, string extension)
        {
            string fName = GetFileBaseName(activityName, startDate);
            if (!string.IsNullOrEmpty(extension))
            {
                Debug.Assert(extension.StartsWith("."));
                fName = fName + extension;
            }
            return Path.Combine(outputPath, fName);
        }
        static string BuildFilePathName(string outputPath, IActivityInfo activity, string extension)
        {
            return BuildFilePathName(outputPath, activity.Name, activity.StartDateLocal.Value, extension);
        }
        //static string BuildFilePathName(string outputPath, SummaryActivity activity, string extension)
        //{
        //    return BuildFilePathName(outputPath, activity.Name, activity.StartDateLocal.Value, extension);
        //}
        //static string BuildFilePathName(string outputPath, DetailedActivity activity, string extension)
        //{
        //    return BuildFilePathName(outputPath, activity.Name, activity.StartDateLocal.Value, extension);
        //}

        static bool ActivityFileExists(string outputPath, string activityName, DateTime startDate, out string fileName)
        {
            string pattern = string.Format("{0}.*", GetFileBaseName(activityName, startDate));
            var files = Directory.GetFiles(outputPath, pattern);
            fileName = null;
            if (files.Length > 0)
                fileName = files[0];
            return files.Length > 0;
        }
        static bool ActivityFileExists(string outputPath, IActivityInfo activity, out string fileName)
        {
            return ActivityFileExists(outputPath, activity.Name, activity.StartDateLocal.Value, out fileName);
        }
        //static bool ActivityFileExists(string outputPath, SummaryActivity activity, out string fileName)
        //{
        //    return ActivityFileExists(outputPath, activity.Name, activity.StartDateLocal.Value, out fileName);
        //}
        //static bool ActivityFileExitst(string outputPath, DetailedActivity activity, out string fileName)
        //{
        //    return ActivityFileExists(outputPath, activity.Name, activity.StartDateLocal.Value, out fileName);
        //}

        static int GetEpochTime(DateTime dateTime)
        {
            // return Unix epoch time, seconds since 1/1/1970
            TimeSpan t = dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)t.TotalSeconds;
        }
        static int GetLapMaxHeartRate(List<int?> heartRates, int startIndex, int endIndex)
        {
            int max = -1;
            if(heartRates != null)
            {
                for(int i = startIndex; i<=endIndex; ++i)
                {
                    int? hr = heartRates[i];
                    if (hr != null && max < hr.Value)
                        max = hr.Value;
                }
            }
            return max;
        }

        static int GetLapAvgHeartRate(List<int?> time, List<int?> heartRates, int startIndex, int endIndex)
        {
            if (time == null || heartRates == null)
                return -1;

            if (startIndex == endIndex)
                return heartRates[startIndex].Value;

            double weightedSum = 0;
            double totalTime = 0;
            for(int i = startIndex+1; i <= endIndex; ++i)
            {
                int deltaT = time[i].Value - time[i - 1].Value;
                totalTime += deltaT;
                int hr1 = heartRates[i-1] == null ? 0 : heartRates[i-1].Value;
                int hr2 = heartRates[i] == null ? 0 : heartRates[i].Value;
                double hrOnInterval = (hr1 + hr2) / 2.0;
                weightedSum += hrOnInterval * deltaT;
            }
            return (int)(weightedSum / totalTime);
        }

        static void CalculateLapMovingTimeAndSpeed(
            List<int?> time, List<float?> velocity, 
            List<bool?> moving, int startIndex, int endIndex, 
            out int movingTime, out double avg_moving_speed)
        {
            avg_moving_speed = 0;
            double total_time = 0;
            for (int j = startIndex; j <= endIndex; ++j)
            {
                if (!moving[j].Value)
                    continue;

                if (j == startIndex)
                {
                    double delta_t = time[j + 1].Value - time[j].Value;
                    total_time += delta_t;
                    avg_moving_speed += velocity[j].Value * delta_t;
                }
                else if (j == endIndex)
                {
                    double delta_t = time[j].Value - time[j - 1].Value;
                    total_time += delta_t;
                    avg_moving_speed += velocity[j].Value * delta_t;
                }
                else
                {
                    double delta_t_left = (time[j].Value - time[j - 1].Value) / 2.0;
                    double delta_t_right = (time[j + 1].Value - time[j].Value) / 2.0;
                    double delta_t = delta_t_left + delta_t_right;
                    total_time += delta_t;
                    avg_moving_speed += velocity[j].Value * delta_t;
                }
            }
            avg_moving_speed /= total_time;
            movingTime = (int)Math.Round(total_time);
        }

        static void WriteTCX(string pathName, DetailedActivity activity, 
            List<float?> dist, List<int?> time, List<LatLng> latlng, 
            List<float?> altitude, List<int?> heartRates, List<int?> cadence, 
            List<int?> watts, List<float?> velocity, List<bool?> moving)
        {
            int nTrackPoints = time.Count;
            if (dist != null && dist.Count != nTrackPoints ||
                latlng != null && latlng.Count != nTrackPoints ||
                altitude != null && altitude.Count != nTrackPoints ||
                heartRates != null && heartRates.Count != nTrackPoints ||
                cadence != null && cadence.Count != nTrackPoints ||
                watts != null && watts.Count != nTrackPoints ||
                velocity != null && velocity.Count != nTrackPoints)
            {
                throw new Exception("Mismatched number of trackpoints");
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            XmlWriter xmlWriter = XmlWriter.Create(pathName, settings);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("TrainingCenterDatabase", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");

            //   xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
            xmlWriter.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

            //   xsi:schemaLocation="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd"
            xmlWriter.WriteAttributeString("xsi", "schemaLocation", null, "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd");

            //   xmlns:ns5="http://www.garmin.com/xmlschemas/ActivityGoals/v1"
            xmlWriter.WriteAttributeString("xmlns", "ns5", null, "http://www.garmin.com/xmlschemas/ActivityGoals/v1");

            //   xmlns:ns3="http://www.garmin.com/xmlschemas/ActivityExtension/v2"
            xmlWriter.WriteAttributeString("xmlns", "ns3", null, "http://www.garmin.com/xmlschemas/ActivityExtension/v2");

            //   xmlns:ns2="http://www.garmin.com/xmlschemas/UserProfile/v2"
            xmlWriter.WriteAttributeString("xmlns", "ns2", null, "http://www.garmin.com/xmlschemas/UserProfile/v2");

            //   xmlns:ns4="http://www.garmin.com/xmlschemas/ProfileExtension/v1"
            xmlWriter.WriteAttributeString("xmlns", "ns4", null, "http://www.garmin.com/xmlschemas/ProfileExtension/v1");

            xmlWriter.WriteStartElement("Activities");
            xmlWriter.WriteStartElement("Activity");

            if (activity.Type == ActivityType.Run)
                xmlWriter.WriteAttributeString("Sport", "Running");
            else if (activity.Type == ActivityType.Ride)
                xmlWriter.WriteAttributeString("Sport", "Biking");
            else if (activity.Type == ActivityType.Walk || activity.Type == ActivityType.Hike)
                xmlWriter.WriteAttributeString("Sport", "Other");
            else
                throw new Exception("Unsupported activity type");

            xmlWriter.WriteStartElement("Id");
            xmlWriter.WriteString(activity.StartDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            xmlWriter.WriteEndElement();

            var laps = activity.Laps;

            // been getting data where the StartIndex and EndIndex in the laps is
            // wrong, and leaving out a lot of the trackpoints; all laps too short
            // and data at the end of the stream is then ignored. detect this and
            // try to correct the indices here
            bool badLapIndices = false;
            int last_end = -1;
            foreach(var lap in laps)
            {
                if (lap.StartIndex.Value != last_end + 1)
                    badLapIndices = true;
                last_end = lap.EndIndex.Value;
            }
            if (last_end + 1 != nTrackPoints)
                badLapIndices = true;

            if (badLapIndices)
            {
                Console.WriteLine("Warning: Lap indices do not match stream data. Correcting the indices.");

                // disable unreachable code detected warning
#pragma warning disable CS0162
                if (true)
                {
                    // based on times in time stream vs lap times 
                    int start_idx = 0;
                    int start_time = 0;
                    foreach (var lap in laps)
                    {
                        int end_time = start_time + lap.ElapsedTime.Value;
                        int end_idx = time.BinarySearch(end_time);
                        if (end_idx < 0)
                            end_idx = ~end_idx - 1;
                        lap.StartIndex = start_idx;
                        lap.EndIndex = end_idx;
                        start_idx = end_idx + 1 < time.Count ? end_idx + 1 : end_idx;
                        start_time = time[start_idx].Value;
                    }
                }
                else
                {
                    // based on values in dist stream vs lap distances
                    int start_idx = 0;
                    float running_total_dist = 0;
                    foreach (var lap in laps)
                    {
                        running_total_dist += lap.Distance.Value;
                        int end_idx = dist.BinarySearch(running_total_dist);
                        if (end_idx < 0)
                            end_idx = ~end_idx - 1;
                        lap.StartIndex = start_idx;
                        lap.EndIndex = end_idx;
                        start_idx = end_idx + 1 < dist.Count ? end_idx + 1 : end_idx;
                    }
                }
#pragma warning restore CS0162

                if(laps.Last().EndIndex != nTrackPoints - 1)
                {
                    Console.WriteLine("Warning - last lap end index not the last trackpoint");
                    laps.Last().EndIndex = nTrackPoints - 1;
                }
            }

            // precompute lap avg and max heart rates; needed in advance for per lap 
            // calorie calculations based on lap avg hr
            int[] avgLapHeartRates = null;
            int[] maxLapHeartRates = null;
            double[] lapHeartRateWeights = null;

            if (activity.HasHeartRate != null && activity.HasHeartRate.Value && heartRates != null)
            {
                avgLapHeartRates = new int[laps.Count];
                maxLapHeartRates = new int[laps.Count];
                lapHeartRateWeights = new double[laps.Count];
                for (int i=0; i<laps.Count; ++i)
                {
                    var lap = laps[i];
                    avgLapHeartRates[i] = GetLapAvgHeartRate(time, heartRates, lap.StartIndex.Value, lap.EndIndex.Value);
                    maxLapHeartRates[i] = GetLapMaxHeartRate(heartRates, lap.StartIndex.Value, lap.EndIndex.Value);
                    lapHeartRateWeights[i] = (double)avgLapHeartRates[i] * (double)lap.ElapsedTime.Value;
                }
                double totalWeight = lapHeartRateWeights.Sum();
                for (int i = 0; i < lapHeartRateWeights.Length; ++i)
                    lapHeartRateWeights[i] /= totalWeight;
                double sum = lapHeartRateWeights.Sum();
                // total weight should be 1
                System.Diagnostics.Debug.Assert(Math.Abs(sum - 1.0) < 0.0000001);
                if (Math.Abs(sum - 1.0) > 0.0000001)
                    Console.WriteLine("WARNING: Heart rate weights did not sum to 1");
            }

            for(int j=0; j<laps.Count; ++j)
            {
                var lap = laps[j];

                xmlWriter.WriteStartElement("Lap");
                xmlWriter.WriteAttributeString("StartTime", lap.StartDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                // PB experimenting with calculating moving time to get SportTracks to show moving time and 
                // more accurate avg speed, but it's calculating it all itself from the trackpoints apparently
                // and including the non-moving time 
                //int movingTime;
                //double avgMovingSpeed;
                //CalculateLapMovingTimeAndSpeed(time, velocity, moving, lap.StartIndex.Value, lap.EndIndex.Value, out movingTime, out avgMovingSpeed);

                if (lap.ElapsedTime != null)
                {
                    xmlWriter.WriteStartElement("TotalTimeSeconds");
                    xmlWriter.WriteValue(lap.ElapsedTime);
                    //xmlWriter.WriteValue(movingTime);
                    xmlWriter.WriteEndElement();
                }

                if (lap.Distance != null)
                {
                    xmlWriter.WriteStartElement("DistanceMeters");
                    xmlWriter.WriteValue(lap.Distance);
                    xmlWriter.WriteEndElement();
                }

                if (lap.MaxSpeed != null)
                {
                    xmlWriter.WriteStartElement("MaximumSpeed");
                    xmlWriter.WriteValue(lap.MaxSpeed);
                    xmlWriter.WriteEndElement();
                }

                bool bHasPower = lap.DeviceWatts != null && lap.DeviceWatts.Value && lap.AverageWatts != null;

                // calories is required but laps do not have calories in strava api
                // but we probably have a total calorie count for the activity,
                // so we'll divide those total calories up among the laps by
                // weighting according to avg hr for the lap;
                // but if we have power, we can calculate calories from that
                int calories = 0;
                if(bHasPower)
                {
                    // different people recommend slightly different conversion factors
                    // between kJ and Cal, but we'll use a 1 to 1 conversion as do
                    // training peaks and trainer road; better to underestimate a bit
                    // (some other programs use a conversion factor like 
                    // Cal = 1.04 * kJ (GC) or
                    // Cal = 1.05 * kJ (Strava) 
                    // calculate the kJ as 
                    //      watts * time / 1000
                    // since a watt is a joule/second
                    calories = (int)Math.Round((double)lap.AverageWatts.Value * (double)lap.ElapsedTime.Value / 1000.0);
                }
                else if (activity.Calories != null && lapHeartRateWeights != null)
                {
                    // divide the total calories from Strava up between the laps based 
                    // on the average hr per lap. We calculate a weight from the avg hr 
                    // and lap time, and use that percentage of the total calories for 
                    // the lap calories
                    calories = (int)Math.Round(lapHeartRateWeights[j] * activity.Calories.Value);
                }

                xmlWriter.WriteStartElement("Calories");
                xmlWriter.WriteValue(calories);
                xmlWriter.WriteEndElement();

                if(avgLapHeartRates != null)
                {
                    xmlWriter.WriteStartElement("AverageHeartRateBpm");
                    xmlWriter.WriteStartElement("Value");
                    xmlWriter.WriteValue(avgLapHeartRates[j]);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }
                if(maxLapHeartRates != null)
                {
                    xmlWriter.WriteStartElement("MaximumHeartRateBpm");
                    xmlWriter.WriteStartElement("Value");
                    xmlWriter.WriteValue(maxLapHeartRates[j]);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }

                // intensity is required, and can be Active or Resting; strava api doesnt seem to provide this
                xmlWriter.WriteStartElement("Intensity");
                xmlWriter.WriteString("Active");
                xmlWriter.WriteEndElement();

                if (lap.AverageCadence != null)
                {
                    xmlWriter.WriteStartElement("Cadence");
                    xmlWriter.WriteValue((int)Math.Round(lap.AverageCadence.Value));
                    xmlWriter.WriteEndElement();
                }

                // trigger method is required and can be Manual, Distance, Time, Location, HeartRate
                // but strava api does not provide this
                xmlWriter.WriteStartElement("TriggerMethod");
                xmlWriter.WriteString("Manual");
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Track");

                int maxCadence = -1;
                int maxWatts = -1;
                for (int i = lap.StartIndex.Value; i <= lap.EndIndex.Value; ++i)
                {
                    xmlWriter.WriteStartElement("Trackpoint");

                    DateTime trkPtTime = activity.StartDate.Value.AddSeconds(time[i].Value);

                    xmlWriter.WriteStartElement("Time");
                    xmlWriter.WriteString(trkPtTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    xmlWriter.WriteEndElement();

                    if (latlng != null && latlng[i][0] != null && latlng[i][1] != null)
                    {
                        xmlWriter.WriteStartElement("Position");
                        xmlWriter.WriteStartElement("LatitudeDegrees");
                        xmlWriter.WriteValue(latlng[i][0]);
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("LongitudeDegrees");
                        xmlWriter.WriteValue(latlng[i][1]);
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                    }

                    if (altitude != null && altitude[i] != null)
                    {
                        xmlWriter.WriteStartElement("AltitudeMeters");
                        xmlWriter.WriteValue(altitude[i]);
                        xmlWriter.WriteEndElement();
                    }

                    if (dist != null && dist[i] != null)
                    {
                        xmlWriter.WriteStartElement("DistanceMeters");
                        xmlWriter.WriteValue(dist[i]);
                        xmlWriter.WriteEndElement();
                    }

                    if (heartRates != null && heartRates[i] != null)
                    {
                        xmlWriter.WriteStartElement("HeartRateBpm");
                        xmlWriter.WriteStartElement("Value");
                        xmlWriter.WriteValue(heartRates[i]);
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                    }

                    if (cadence != null && cadence[i] != null)
                    {
                        if (activity.Type == ActivityType.Ride)
                        {
                            xmlWriter.WriteStartElement("Cadence");
                            xmlWriter.WriteValue(cadence[i]);
                            xmlWriter.WriteEndElement();
                        }

                        if (cadence[i] > maxCadence)
                            maxCadence = cadence[i].Value;
                    }

                    if ((velocity != null && velocity[i] != null) ||
                        (watts != null && watts[i] != null) || 
                        (cadence != null && cadence[i] != null))
                    {
                        xmlWriter.WriteStartElement("Extensions");
                        xmlWriter.WriteStartElement("ns3", "TPX", null);

                        if (velocity != null && velocity[i] != null)
                        {
                            xmlWriter.WriteStartElement("ns3", "Speed", null);
                            xmlWriter.WriteValue(velocity[i]);
                            xmlWriter.WriteEndElement();
                        }
                        if (cadence != null && cadence[i] != null && activity.Type != ActivityType.Ride)
                        {
                            xmlWriter.WriteStartElement("ns3", "RunCadence", null);
                            xmlWriter.WriteValue(cadence[i]);
                            xmlWriter.WriteEndElement();
                        }
                        if (watts != null && watts[i] != null)
                        {
                            xmlWriter.WriteStartElement("ns3", "Watts", null);
                            xmlWriter.WriteValue(watts[i]);
                            xmlWriter.WriteEndElement();

                            if (watts[i] > maxWatts)
                                maxWatts = watts[i].Value;
                        }

                        xmlWriter.WriteEndElement(); // ns3:TPX
                        xmlWriter.WriteEndElement(); // Extensions
                    }

                    xmlWriter.WriteEndElement(); // Trackpoint
                }

                xmlWriter.WriteEndElement(); // </Track>

                if (lap.AverageSpeed != null || maxCadence > 0 ||
                    (lap.AverageWatts != null && bHasPower) ||
                    maxWatts > 0 )
                {
                    xmlWriter.WriteStartElement("Extensions");
                    xmlWriter.WriteStartElement("ns3", "LX", null);

                    if (lap.AverageSpeed != null)
                    {
                        xmlWriter.WriteStartElement("ns3", "AvgSpeed", null);
                        xmlWriter.WriteValue(lap.AverageSpeed);
                        xmlWriter.WriteEndElement();
                    }
                    if (lap.AverageCadence != null && activity.Type != ActivityType.Ride)
                    {
                        xmlWriter.WriteStartElement("ns3", "AvgRunCadence", null);
                        xmlWriter.WriteValue((int)Math.Round(lap.AverageCadence.Value));
                        xmlWriter.WriteEndElement();
                    }
                    if (maxCadence > 0)
                    {
                        if(activity.Type == ActivityType.Ride)
                            xmlWriter.WriteStartElement("ns3", "MaxBikeCadence", null);
                        else
                            xmlWriter.WriteStartElement("ns3", "MaxRunCadence", null);
                        xmlWriter.WriteValue(maxCadence);
                        xmlWriter.WriteEndElement();
                    }

                    if (bHasPower)
                    {
                        xmlWriter.WriteStartElement("ns3", "AvgWatts", null);
                        xmlWriter.WriteValue((int)Math.Round(lap.AverageWatts.Value));
                        xmlWriter.WriteEndElement();
                    }

                    if (maxWatts > 0)
                    {
                        xmlWriter.WriteStartElement("ns3", "MaxWatts", null);
                        xmlWriter.WriteValue(maxWatts);
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement(); // </ns3:LX>
                    xmlWriter.WriteEndElement(); // </Extensions>
                }
                xmlWriter.WriteEndElement(); // Lap
            }

            xmlWriter.WriteStartElement("Notes");
            xmlWriter.WriteValue(activity.Name);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        static async Task<string> RefreshAccessToken()
        {
            string refresh_token = Settings.Default.RefreshToken;

            if (string.IsNullOrEmpty(refresh_token))
            {
                new StravaAuthorizer().Authorize(client_id, client_secret);
                refresh_token = Settings.Default.RefreshToken;
            }

            HttpClient httpClient = new HttpClient();
            var builder = new UriBuilder("https://www.strava.com/oauth/token");
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["client_id"] = client_id;
            query["client_secret"] = client_secret;
            query["refresh_token"] = refresh_token;
            query["grant_type"] = "refresh_token";
            builder.Query = query.ToString();
            string url = builder.ToString();
            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);

            string access_token = responseDict["access_token"];
            if (refresh_token.CompareTo(responseDict["refresh_token"]) != 0)
                Properties.Settings.Default.RefreshToken = refresh_token;
            return access_token;
        }
    }
}
