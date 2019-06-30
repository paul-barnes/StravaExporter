using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using CommandLine;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json;

namespace StravaExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
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
        }

        static void HandleAuthorize(AuthorizeOptions opt)
        {
            new StravaAuthorizer().Authorize(opt.Port);
        }

        static void HandleActivity(ActivityOptions opt)
        {
            ValidateOutputPath(opt);

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

            DownloadActivities(new ActivitiesApi(), opt.OutputPath, ref activityIds);

            Console.WriteLine();
            Console.WriteLine("Exported {0} activities", activityIds.Count);
        }

        static void HandleExport(ExportOptions opt)
        {
            ValidateOutputPath(opt);

            Configuration.Default.AccessToken = RefreshAccessToken().GetAwaiter().GetResult();

            // downloading all activities between today and opt.Days ago 
            Console.WriteLine();
            Console.WriteLine("Downloading activities for the past {0} days and saving to directory {1}", opt.Days, opt.OutputPath);
            Console.WriteLine();

            List<long> activityIds;
            var activitiesApi = new ActivitiesApi();

            bool bFoundActivities = GetActivities(opt, activitiesApi, out activityIds);
            DownloadActivities(activitiesApi, opt.OutputPath, ref activityIds);
            
            Console.WriteLine();
            if (!bFoundActivities)
                Console.WriteLine("No activities existed to export");
            else if (activityIds.Count == 0)
                Console.WriteLine("No new activities were found to export");
            else
                Console.WriteLine("Exported {0} activities", activityIds.Count);
        }

        static void ValidateOutputPath(CommonOptions opt)
        {
            if (string.IsNullOrEmpty(opt.OutputPath))
            {
                opt.OutputPath = System.Configuration.ConfigurationManager.AppSettings["output_directory"];
                if (string.IsNullOrEmpty(opt.OutputPath))
                    throw new Exception("No output path specified in command line and none found in config file");
            }
            if (!Directory.Exists(opt.OutputPath))
                throw new Exception(string.Format("The specified output directory {0} does not exist", opt.OutputPath));
        }

        static void DownloadActivities(ActivitiesApi activitiesApi, string outputPath, ref List<long> activityIds)
        {
            var exportedActivityIds = new List<long>();

            var streamsApi = new StreamsApi();
            for (int i = 0; i < activityIds.Count; ++i)
            {
                long activityId = activityIds[i];

                DetailedActivity detailedActivity = activitiesApi.GetActivityById(activityId, true);

                string pathname = BuildFilePathName(outputPath, detailedActivity);
                if (File.Exists(pathname))
                {
                    // this only happens with Activity verb for downloading specific activity id(s);
                    // with Export verb, we automatically skip activities which were already 
                    // downloaded (GetActivities just does not return them)
                    Console.WriteLine("The file {0} exists; overwrite? [Y/N]", pathname);
                    string s = Console.ReadLine();
                    if (s != "Y" && s != "y")
                        continue;
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
                        "velocity_smooth"
                        //"moving"
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
                //MovingStream moving = streams.Moving; // bool?

                WriteTCX(pathname,
                    detailedActivity,
                    dist != null ? dist.Data : null,
                    time != null ? time.Data : null,
                    latlng != null ? latlng.Data : null,
                    altitude != null ? altitude.Data : null,
                    hr != null ? hr.Data : null,
                    cadence != null ? cadence.Data : null,
                    watts != null ? watts.Data : null,
                    velocity != null ? velocity.Data : null);

                exportedActivityIds.Add(activityId);
                Console.WriteLine("Exported activity {0}", pathname);
            }

            activityIds = exportedActivityIds;
        }

        static bool GetActivities(ExportOptions opt, ActivitiesApi activitiesApi, out List<long> activityIds)
        {
            activityIds = new List<long>();

            bool bFoundActivities = false;

            // get everything from now to opt.Days ago; 
            // adjust the time on "after" however so we include 
            // things that happened any time on that oldest day
            // and not just since the current time on that day
            DateTime dtBefore = DateTime.UtcNow;
            int before = GetEpochTime(dtBefore);
            DateTime dtAfter = new DateTime(dtBefore.Year, dtBefore.Month, dtBefore.Day - opt.Days, 0, 0, 0);
            int after = GetEpochTime(dtAfter);

            for (int page = 1; true; ++page)
            {
                var activities = activitiesApi.GetLoggedInAthleteActivities(before, after, page, 30);
                if (activities == null || activities.Count == 0)
                    break;
                bFoundActivities = true;
                foreach (var activity in activities)
                {
                    if (activity.Type != ActivityType.Ride && activity.Type != ActivityType.Run)
                    {
                        Console.WriteLine("Skipping activity [{0}] on [{1}] with type [{2}]", activity.Name, activity.StartDateLocal.ToString(), activity.Type.ToString());
                        continue;
                    }
                    string pathname = BuildFilePathName(opt.OutputPath, activity);
                    if (File.Exists(pathname))
                    {
                        Console.WriteLine("Skipping activity because it has already been exported: {0} ", pathname);
                        continue;
                    }
                    activityIds.Add(activity.Id.Value);
                }
            }
            Console.WriteLine();
            return bFoundActivities;
        }

        static string RemoveInvalidFilenameChars(string s)
        {
            char[] badChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\r', '\n', '\t' };
            foreach (char c in badChars)
                s = s.Replace(c, '_');
            return s;
        }

        static string BuildFilePathName(string outputPath, string activityName, DateTime startDate)
        {
            string name = string.Format("{0}_{1}.tcx",
                RemoveInvalidFilenameChars(activityName),
                startDate.ToString("yyyyMMddTHHmmss"));
            name = name.Replace(' ', '_');
            while (name.Contains("__"))
                name = name.Replace("__", "_");
            return Path.Combine(outputPath, name);
        }

        static string BuildFilePathName(string outputPath, SummaryActivity activity)
        {
            return BuildFilePathName(outputPath, activity.Name, activity.StartDateLocal.Value);
        }
        static string BuildFilePathName(string outputPath, DetailedActivity activity)
        {
            return BuildFilePathName(outputPath, activity.Name, activity.StartDateLocal.Value);
        }

        static int GetEpochTime(DateTime dateTimeUTC)
        {
            // return Unix epoch time, seconds since 1/1/1970
            TimeSpan t = dateTimeUTC - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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

        static void WriteTCX(string pathName, DetailedActivity activity, 
            List<float?> dist, List<int?> time, List<LatLng> latlng, 
            List<float?> altitude, List<int?> heartRates, List<int?> cadence, 
            List<int?> watts, List<float?> velocity)
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
            else
                throw new Exception("Unsupported activity type");

            xmlWriter.WriteStartElement("Id");
            xmlWriter.WriteString(activity.StartDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            xmlWriter.WriteEndElement();

            var laps = activity.Laps;

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

                if (lap.ElapsedTime != null)
                {
                    xmlWriter.WriteStartElement("TotalTimeSeconds");
                    xmlWriter.WriteValue(lap.ElapsedTime);
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
                        xmlWriter.WriteStartElement("Cadence");
                        xmlWriter.WriteValue(cadence[i]);
                        xmlWriter.WriteEndElement();

                        if (cadence[i] > maxCadence)
                            maxCadence = cadence[i].Value;
                    }

                    if ((velocity != null && velocity[i] != null) ||
                        (watts != null && watts[i] != null))
                    {
                        xmlWriter.WriteStartElement("Extensions");
                        xmlWriter.WriteStartElement("ns3", "TPX", null);

                        if (velocity != null && velocity[i] != null)
                        {
                            xmlWriter.WriteStartElement("ns3", "Speed", null);
                            xmlWriter.WriteValue(velocity[i]);
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

                    if (maxCadence > 0)
                    {
                        xmlWriter.WriteStartElement("ns3", "MaxBikeCadence", null);
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
            string client_id = System.Configuration.ConfigurationManager.AppSettings["client_id"];
            string client_secret = System.Configuration.ConfigurationManager.AppSettings["client_secret"];
            string refresh_token = System.Configuration.ConfigurationManager.AppSettings["refresh_token"];

            if (string.IsNullOrEmpty(refresh_token))
            {
                new StravaAuthorizer().Authorize();
                refresh_token = System.Configuration.ConfigurationManager.AppSettings["refresh_token"];
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
                SetAppSetting("refresh_token", responseDict["refresh_token"]);
            //SetAppSetting("access_token", access_token);
            return access_token;
        }

        internal static bool SetAppSetting(string Key, string Value)
        {
            bool result = false;
            try
            {
                System.Configuration.Configuration config =
                  System.Configuration.ConfigurationManager.OpenExeConfiguration(
                                       System.Configuration.ConfigurationUserLevel.None);

                config.AppSettings.Settings.Remove(Key);
                var kvElem = new System.Configuration.KeyValueConfigurationElement(Key, Value);
                config.AppSettings.Settings.Add(kvElem);

                // Save the configuration file.
                config.Save(System.Configuration.ConfigurationSaveMode.Modified);

                // Force a reload of a changed section.
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                result = true;
            }
            finally
            { }
            return result;
        }
    }
}
