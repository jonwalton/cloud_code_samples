using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using net.openstack.Core.Domain;
using net.openstack.Core.Exceptions.Response;
using net.openstack.Providers.Rackspace;

namespace Challenge1
{
    class Program
    {
        private static RackspaceCloudIdentity auth = null;
        private static string Username = null;
        private static string Password = null;
        private static string APIKey = null;
        private static string AccountRegion = null;

        private static string ServerNamePrefix = null;
        private static string ServerRegion = null;

        static void Main(string[] args)
        {
            if (ParseArguments(args))
            {
                if (Login())
                {
                    var cloudServers = new CloudServersProvider(auth);

                    var newServerList = new List<NewServer>();
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            // Create a 512mb cloud server instance using centos 6.0
                            newServerList.Add(cloudServers.CreateServer(String.Format("{0}{1}", ServerNamePrefix, i), "a3a2c42f-575f-4381-9c6d-fcd3b7d07d17", "2", region: ServerRegion));
                        }
                        catch (Exception ex)
                        {
                            PrintException(ex);
                        }
                    }
                }
            }
        }

        static bool Login()
        {
            auth = new RackspaceCloudIdentity();
            auth.Username = Username;
            auth.Password = Password;
            auth.APIKey = APIKey;
            auth.CloudInstance = AccountRegion == "LON" ? CloudInstance.UK : CloudInstance.Default;

            try
            {
                CloudIdentityProvider identityProvider = new CloudIdentityProvider();
                var userAccess = identityProvider.Authenticate(auth);
            }
            catch (ResponseException ex)
            {
                PrintException(ex);
                return false;
            }

            return true;
        }

        static void PrintHelp(string InvalidCommand = null)
        {
            if (!String.IsNullOrWhiteSpace(InvalidCommand))
            {
                Console.WriteLine(String.Format("Invalid command {0}", InvalidCommand));
                Console.WriteLine();
            }

            Console.WriteLine("Usage:");
            Console.WriteLine();

            Console.WriteLine("username\tCloud Identity username. If not defined, username will be read from %appdata%/.rackspace_cloud_credentials");
            Console.WriteLine("password\tCloud Identity password. If not defined, username will be read from %appdata%/.rackspace_cloud_credentials");
        }

        static string ReadIniValue(string Section, string Key)
        {
            return "";
        }

        private static bool ParseArguments(string[] args)
        {
            var index = 0;
            while (index < args.Length)
            {
                switch (args[index].ToLower().Split('=')[0])
                {
                    case "username":
                        Username = args[index].Split('=')[1];
                        break;

                    case "password":
                        Password = args[index].Split('=')[1];
                        break;

                    case "apikey":
                        APIKey = args[index].Split('=')[1];
                        break;

                    case "accountregion":
                        AccountRegion = args[index].Split('=')[1];
                        break;

                    case "serverregion":
                        ServerRegion = args[index].Split('=')[1];
                        break;

                    case "servernameprefix":
                        ServerNamePrefix = args[index].Split('=')[1];
                        break;

                    default:
                        PrintHelp();
                        return false;
                }

                index++;
            }

            if (String.IsNullOrWhiteSpace(Username) || (String.IsNullOrWhiteSpace(Password) && String.IsNullOrWhiteSpace(APIKey)) || String.IsNullOrWhiteSpace(ServerNamePrefix))
            {
                PrintHelp();
                return false;
            }

            return true;
        }

        private static void PrintException(Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.Read();
            Environment.Exit(-1);
        }
    }
}
