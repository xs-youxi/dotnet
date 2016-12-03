using CommandLine;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDomains
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = CommandLine.Parser.Default.ParseArguments<Options>(args);
            if (arguments.Errors.Any())
                return;
            var argsBucket = arguments.Value;
            try
            {
                var task = UpdateGoogleDomainsAsync(argsBucket.hostname, argsBucket.username, argsBucket.password, argsBucket.myip, argsBucket.offline);
                task.ContinueWith(r =>
                   {
                       Console.WriteLine("");
                       Console.WriteLine(r.Result);
                   });

                var autoEvent = new AutoResetEvent(false);
                var timer = new Timer((c) =>
                {
                    Console.Write($">");
                    if (task.IsCompleted)
                        autoEvent.Set();
                }, null, 0, 100);
                autoEvent.WaitOne();
                timer.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        class Options
        {
            [Value(0, Required = true)]
            public string hostname { get; set; }
            [Option('u', "username", Required = true, HelpText = "Input username.")]
            public string username { get; set; }
            [Option('p', "password", Required = true, HelpText = "Input password.")]
            public string password { get; set; }
            [Option('i', "myip", HelpText = "Input myip.")]
            public string myip { get; set; }
            [Option('o', "offline", HelpText = "yes or no")]
            public string offline { get; set; }
        }
        async public static Task<string> UpdateGoogleDomainsAsync(string hostname, string username, string password, string myip = null, string offline = null)
        {
            // ref https://support.google.com/domains/answer/6147083
            var uri = new Uri($"https://domains.google.com/nic/update");
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(username, password);

                var reqParams = new NameValueCollection();
                reqParams.Add("hostname", hostname);
                if (myip != null)
                    reqParams.Add("myip", myip);
                if (offline != null)
                    reqParams.Add("offline", offline);

                var responseArray = await client.UploadValuesTaskAsync(uri, "POST", reqParams);
                return Encoding.UTF8.GetString(responseArray);
            }
        }
    }
}
