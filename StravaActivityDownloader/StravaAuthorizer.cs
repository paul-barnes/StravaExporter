using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StravaExporter
{
    class StravaAuthorizer
    {
        public StravaAuthorizer()
        {

        }
        public void Authorize(string client_id, string client_secret, int port = 8080)
        {
            // the strava oath/authorize endpoint expects a redirect url so that after the user logs in 
            // to strava in the browser and authorizes the app to access his data, it redirects the browser 
            // to the redirect url. We will start an http server on the specified port in order to receive 
            // that redirect. The redirect url they redirect to will include a parameter named "code" which 
            // we need for the next step.

            // start the http server 
            MyHttpServer httpServer = new MyHttpServer(port);
            Thread thread = new Thread(new ThreadStart(httpServer.listen));
            thread.Start();

            Console.WriteLine("Launching a browser to log in to Strava and authorize StravaExporter to download your data...");

            // build the initial authorization request url and launch a browser for user to 
            // log in and grant us access
            string redirect_uri = string.Format("http://localhost:{0}/exchange_token", port);
            //string scope = "read,read_all,profile:read_all,profile:write,activity:read,activity:read_all,activity:write";
            string scope = "read,activity:read,activity:read_all";
            string url = string.Format("http://www.strava.com/oauth/authorize?client_id={0}&response_type=code&redirect_uri={1}&approval_prompt=force&scope={2}", 
                client_id, redirect_uri, scope);

            // launch the browser
            System.Diagnostics.Process.Start(url);

            // after the user completes the authorization in the browser, it will redirect to our http server.
            // wait for the http server thread to complete
            thread.Join();

            string accessCode = httpServer.AccessCode;

            if (accessCode == null)
                throw new Exception("StravaExporter was not authorized to access Strava data");

            // now we need to POST to https://www.strava.com/oauth/token with the access code
            // client_id: your application’s ID, obtained during registration
            // client_secret: your application’s secret, obtained during registration
            // code: authorization code from last step
            // grant_type: the grant type for the request. For initial authentication, must always be "authorization_code".

            // https://www.strava.com/oauth/token?client_id=36425&client_secret=ee2979ecc9afe77bff701ed5fd5ff192632fea88&code=&grant_type=authorization_code
            url = string.Format("https://www.strava.com/oauth/token?client_id={0}&client_secret={1}&code={2}&grant_type=authorization_code",
                client_id, client_secret, accessCode);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.PostAsync(url, null).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            dynamic json = JValue.Parse(responseBody);
            string access_token = json.access_token;
            string refresh_token = json.refresh_token;

            Console.WriteLine("Access Token = {0}", access_token);
            Console.WriteLine("Refresh Token = {0}", refresh_token);

            Properties.Settings.Default.RefreshToken = refresh_token;

            Console.WriteLine();
            Console.WriteLine("Authorization succeeded");
        }
    }
}
