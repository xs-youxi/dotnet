namespace GoogleDomains
{
    public class ArgsParser
    {
#if false
        public static void NDeskParser(string[] args)
        {
            string username = null, password = null, hostname = null, myip = null, offline = null;

            var p = new OptionSet()
                          .Add("host|hostname=", $"hostname is {hostname}", (v) => hostname = v)
                          .Add("u|username=", $"username is {username}", (v) => username = v)
                          .Add("p|password=", $"username is {password}", (v) => password = v)
                          .Add("ip|myip=", $"myip is {myip}", (v) => myip = v)
                          .Add("o|offline=", $"myip is {offline}", (v) => offline = v)
                          .Add("h|?|help", (v) => { Console.WriteLine("help"); });
            try
            {
                var extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }
#endif
    }
}
