using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            thread.IsBackground = true;
            thread.Start();

            Console.WriteLine("Launching a browser to log in to Strava and authorize StravaExporter to download your data");

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
            // wait for the http server thread to complete for a while then we'll fail; 
            // need to assume failure at some point, and if the client id was bad we can't recover otherwise
            int timeoutSecs = 60;
            Console.Write("Waiting for authorization from browser. Press CTRL+C to cancel. Will time out in... ");
            int row = Console.CursorTop;
            int col = Console.CursorLeft;
            Console.Write("{0,2}", timeoutSecs);
            while (--timeoutSecs >= 0 && !thread.Join(1000))
            {
                Console.SetCursorPosition(col, row);
                Console.Write("{0,2}", timeoutSecs);
            }
            Console.WriteLine();
            Console.WriteLine();

            string accessCode = httpServer.AccessCode;

            if (accessCode == null)
                throw new AuthorizationException("StravaExporter was not authorized to access Strava data");

            // now we need to POST to https://www.strava.com/oauth/token with the access code
            // client_id: your application’s ID, obtained during registration
            // client_secret: your application’s secret, obtained during registration
            // code: authorization code from last step
            // grant_type: the grant type for the request. For initial authentication, must always be "authorization_code".

            // https://www.strava.com/oauth/token?client_id=12345&client_secret=xxxxx&code=&grant_type=authorization_code
            url = string.Format("https://www.strava.com/oauth/token?client_id={0}&client_secret={1}&code={2}&grant_type=authorization_code",
                client_id, client_secret, accessCode);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.PostAsync(url, null).GetAwaiter().GetResult();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new AuthorizationException("Authorization failed. Please check your API keys (client id and secret)");
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
