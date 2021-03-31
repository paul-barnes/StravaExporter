using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace StravaExporter
{
    class StravaClientException : Exception
    {
        public StravaClientException(string message) : base(message) { }
    }
    class UnsupportedFormatException : StravaClientException
    {
        public UnsupportedFormatException(string message) : base(message) { }
    }

    class StravaHttpClient : HttpClient
    {
        public const string BASE_URL = "https://www.strava.com";

        public StravaHttpClient() : base(new HttpClientHandler() { AllowAutoRedirect = false }, true)
        {
            this.BaseAddress = new Uri(BASE_URL);
        }

        static bool IsRedirect(HttpStatusCode status)
        {
            return (int)status >= 300 && (int)status < 400;
        }

        public async Task<bool> LogIn(string email, string password)
        {
            string login_url = string.Format("{0}{1}", BASE_URL, "/login");
            HttpResponseMessage response = await this.GetAsync("/login");
            response.EnsureSuccessStatusCode();
            string html = await response.Content.ReadAsStringAsync();
            var meta = ExtractMetaTags(html);
            string csrf_param = meta["csrf-param"];
            string csrf_token = meta["csrf-token"];

            var postInfo = new Dictionary<string, string>();
            postInfo["email"] = email;
            postInfo["password"] = password;
            postInfo["remember_me"] = "on";
            postInfo[csrf_param] = csrf_token;

            string sPostInfo = JsonConvert.SerializeObject(postInfo);
            response = await PostAsync("/session", new StringContent(sPostInfo, Encoding.UTF8, "application/json"));

            if (!IsRedirect(response.StatusCode) || string.Compare(response.Headers.Location.ToString(), login_url, true) == 0)
                return false;
            return true;
        }

        static string GetExtension(HttpResponseMessage httpResponse, OutputFormat outputFormat)
        {
            string ext;
            string fName = httpResponse.Content.Headers.ContentDisposition.FileName.Trim('"');
            if (fName.Contains("."))
                ext = Path.GetExtension(fName);
            else
                ext = GetExtension(outputFormat);
            return ext;
        }
        static string GetExtension(OutputFormat outputFormat)
        {
            if (outputFormat == OutputFormat.Original)
                return ".dat";
            return string.Format(".{0}", outputFormat.ToString().ToLower());
        }
        static string GetTargetPathname(string outputPath, string baseFileName, string ext)
        {
            return Path.ChangeExtension(Path.Combine(outputPath, baseFileName), ext);
        }

        public class DownloadableActivity
        {
            private HttpResponseMessage httpResponse;
            private string activityName;
            private string outputPath;
            private string baseFileName;
            private OutputFormat outputFormat;
            public DownloadableActivity(HttpResponseMessage response, string activityName, string outputPath, string baseFileName, OutputFormat outputFormat)
            {
                this.httpResponse = response;
                this.activityName = activityName;
                this.outputPath = outputPath;
                this.baseFileName = baseFileName;
                this.outputFormat = outputFormat;
            }
            public async Task Download()
            {
                await SaveContent(httpResponse, activityName, GetTargetPathname(), outputFormat);
            }

            public string GetTargetPathname()
            {
                return StravaHttpClient.GetTargetPathname(outputPath, baseFileName, StravaHttpClient.GetExtension(httpResponse, outputFormat));
            }
        }

        private async Task<HttpResponseMessage> InitiateDownload(long activityId, string baseFileName, OutputFormat outputFormat)
        {
            string redirect_url = string.Format("{0}/activities/{1}", BASE_URL, activityId); // redirect here if the specified format is not available 
            string url = string.Format("/activities/{0}/export_{1}", activityId, outputFormat.ToString().ToLower());

            var response = await GetAsync(url);

            if (IsRedirect(response.StatusCode) && string.Compare(response.Headers.Location.ToString(), redirect_url, true) == 0)
                throw new UnsupportedFormatException(string.Format("The activity {0} with id {1} is not available in the specified {2} format", baseFileName, activityId, outputFormat.ToString()));

            response.EnsureSuccessStatusCode();
            return response;
        }

        public async Task<DownloadableActivity> BeginDownloadActivity(string activityName, long activityId, string outputPath, string baseFileName, OutputFormat outputFormat)
        {
            var httpResponse = await InitiateDownload(activityId, baseFileName, outputFormat);
            return new DownloadableActivity(httpResponse, activityName, outputPath, baseFileName, outputFormat);
        }

        private static void SkipLeadingWhitespace(Stream stream)
        {
            int ch;
            do
            {
                ch = stream.ReadByte();
            } while (ch != -1 && char.IsWhiteSpace((char)ch));
            if (stream.Position > 0)
                stream.Seek(-1, SeekOrigin.Current);
        }

        private static bool SaveTcxXmlStream(Stream xmlStream, string activityName, Stream outStream)
        {
            SkipLeadingWhitespace(xmlStream);
            var xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlStream);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("g", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");
                var notesNode = xmlDoc.DocumentElement.SelectSingleNode("g:Activities/g:Activity/g:Notes", nsmgr);
                if (notesNode != null && string.IsNullOrEmpty(notesNode.InnerText))
                    notesNode.InnerText = activityName;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unexpected error while saving tcx as xml: {0}", e.ToString());
                return false;
            }
            xmlDoc.Save(outStream);
            return true;
        }

        private static async Task SaveContent(HttpResponseMessage httpResponse, string activityName, string pathName, OutputFormat outputFormat)
        {
            using (FileStream fs = File.Create(pathName))
            {
                string ext = Path.GetExtension(pathName);
                if (string.Compare(ext, ".tcx", true) == 0)
                {
                    // my activities downloaded in 'original' format sourced from trainerroad include some whitespace 
                    // before the opening <xml> tag, and SportTracks chokes on importing them. Strip such whitespace 
                    // out here before saving. 
                    // also, I want to put the activity name in the Notes element so it shows up in SportTracks and is searchable
                    var stream = await httpResponse.Content.ReadAsStreamAsync();
                    if (!SaveTcxXmlStream(stream, activityName, fs))
                    {
                        // if something went wrong, this wrote nothing to the file stream.
                        // just write what we have, unchanged (except do skip the leading whitespace 
                        // from trainerroad to make valid xml)
                        stream.Seek(0, SeekOrigin.Begin);
                        SkipLeadingWhitespace(stream);
                        await stream.CopyToAsync(fs);
                    }
                }
                else
                {
                    await httpResponse.Content.CopyToAsync(fs);
                }
            }
        }

        public async Task<string> DownloadActivity(string activityName, long activityId, string outputPath, string baseFileName, OutputFormat outputFormat)
        {
            var httpResponse = await InitiateDownload(activityId, baseFileName, outputFormat);
            var pathName = GetTargetPathname(outputPath, baseFileName, GetExtension(httpResponse, outputFormat));
            await SaveContent(httpResponse, activityName, pathName, outputFormat);
            return pathName;
        }

        static Dictionary<string, string> ExtractMetaTags(string html)
        {
            Regex metaTag = new Regex(@"<meta\s+name\s*=\s*""(.+?)""\s+content\s*=\s*""(.+?)""\s*/>");
            Dictionary<string, string> metaInformation = new Dictionary<string, string>();

            foreach (Match m in metaTag.Matches(html))
                metaInformation.Add(m.Groups[1].Value, m.Groups[2].Value);
            return metaInformation;
        }
    }
}
