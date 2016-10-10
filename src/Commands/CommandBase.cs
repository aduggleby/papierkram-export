using CommandLine;
using PapierkramExport.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using System.Net;

namespace PapierkramExport
{
    class CommandBase
    {
        static CommandBase()
        {
            CookieContainer = CookieContainer ?? new CookieContainer();
        }

        public CommandBase()
        {
            WaitForEnter = false;
        }

        // Omitting long name, default --verbose
        [Option(
          HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option('w', "wait",
          HelpText = "Wait for enter key after finishing.")]
        public bool WaitForEnter { get; set; }

        [Option('d', "domain", Required = true,
            HelpText = "The domain associated with your account (without .papierkram.de).")]
        public string Domain { get; set; }

        [Option('u', "user", Required = true,
            HelpText = "The user account to use to log on to Papierkram.de with.")]
        public string User { get; set; }

        [Option('p', "password", Required = true,
            HelpText = "The password associated with the user account.")]
        public string Password { get; set; }

        
        protected Uri TenantUrl
        {
            get
            {
                return new Uri(string.Format("https://{0}.papierkram.de", Domain));
            }
        }

        protected string WithTenantUrl(string subpage)
        {
            
            return string.Format("{0}{1}", TenantUrl.ToString(), subpage.TrimStart('/'));
        }

        public ILog Log { get; set; }

        public static CookieContainer CookieContainer { get; set; }

        protected void WithCookieHttpClient(Action<HttpClient> c)
        {
            using (var handler = new HttpClientHandler()
            {
                CookieContainer = CookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip
            })
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.87 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html");

                client.DefaultRequestHeaders.Add("Origin", TenantUrl.ToString().TrimEnd('/'));
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.Add("Accept-Language", "en");
                client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

                client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.ExpectContinue = false;

                c(client);
            }
        }

        protected void ExecuteLogin()
        {
            var loginUrl = WithTenantUrl("login");

            WithCookieHttpClient(client =>
            {
                Log.Verbose("HTTP GET: " + loginUrl);
                var response = client.GetStringAsync(loginUrl);


                var responseString = response.Result;
                Log.Verbose("Received: " + responseString.Length + " chars");

                var parser = new HtmlParser();
                var doc = parser.Parse(responseString);

                var values = GetCSRFParams(doc);

                var cookies = CookieContainer.GetCookies(TenantUrl);

                foreach (Cookie c in cookies)
                {
                    Log.Verbose(string.Format("Received Cookie: [{0}]=[{1}]", c.Name, c.Value));
                }

                values.Add("user[subdomain]", Domain.ToLower());
                values.Add("user[email]", User);
                values.Add("user[password]", Password);
                values.Add("user[remember_me]", "0");
                values.Add("commit", "Anmelden");

                client.DefaultRequestHeaders.Add("Referer", loginUrl);


                Log.Verbose("HTTP POST: " + loginUrl);

                var content = new FormUrlEncodedContent(values);

                var loginResponse = client.PostAsync(loginUrl, content).Result;

                var loginResponseString = loginResponse.Content.ReadAsStringAsync();

                if (loginResponseString.Result.Contains("pkError"))
                {
                    throw new Exception("Could not login, found pkError in result. Check domain, user and password.");
                }
                else
                {
                    Log.Info("Login successful as " + User + " for " + TenantUrl);
                }
            }); // Encounter a "'iso-2022-cn' is not a supported encoding name" exception here? Continue, it is handled by trying again.
        }

        protected Dictionary<string, string> GetCSRFParams(AngleSharp.Dom.Html.IHtmlDocument doc)
        {
            var csrfParam = doc.QuerySelectorAll("meta[name='csrf-param']").FirstOrDefault().GetAttribute("content");
            Log.Verbose("Found CSRF Param: " + csrfParam);

            var csrfToken = doc.QuerySelectorAll("meta[name='csrf-token']").FirstOrDefault().GetAttribute("content");
            Log.Verbose("Found CSRF Token: " + csrfToken);

            var values = new Dictionary<string, string>
                {
                    { "utf8", "✓" },
                    { csrfParam, csrfToken }
                };
            return values;
        }
    }
}
