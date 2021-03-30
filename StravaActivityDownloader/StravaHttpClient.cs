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

namespace StravaExporter
{
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
            private string outputPath;
            private string baseFileName;
            private OutputFormat outputFormat;
            public DownloadableActivity(HttpResponseMessage response, string outputPath, string baseFileName, OutputFormat outputFormat)
            {
                this.httpResponse = response;
                this.outputPath = outputPath;
                this.baseFileName = baseFileName;
                this.outputFormat = outputFormat;
            }
            public async Task Download()
            {
                using (FileStream fs = File.Create(GetTargetPathname()))
                    await httpResponse.Content.CopyToAsync(fs);
            }

            public string GetTargetPathname()
            {
                return StravaHttpClient.GetTargetPathname(outputPath, baseFileName, StravaHttpClient.GetExtension(httpResponse, outputFormat));
            }
        }

        public async Task<DownloadableActivity> BeginDownloadActivity(long activityId, string outputPath, string baseFileName, OutputFormat outputFormat)
        {
            string url = string.Format("/activities/{0}/export_{1}", activityId, outputFormat.ToString().ToLower());

            var response = await GetAsync(url);
            response.EnsureSuccessStatusCode();

            return new DownloadableActivity(response, outputPath, baseFileName, outputFormat);
        }

        public async Task<string> DownloadActivity(long activityId, string outputPath, string baseFileName, OutputFormat outputFormat)
        {
            string url = string.Format("/activities/{0}/export_{1}", activityId, outputFormat.ToString().ToLower());

            var httpResponse = await GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            var pathName = GetTargetPathname(outputPath, baseFileName, GetExtension(httpResponse, outputFormat));
            using (FileStream fs = File.Create(pathName))
                await httpResponse.Content.CopyToAsync(fs);
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
