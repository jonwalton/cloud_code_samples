using System;
using System.Collections.Generic;
using net.openstack.Core.Domain;
using net.openstack.Core.Exceptions.Response;
using net.openstack.Providers.Rackspace;
using System.Threading;

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
            Console.WriteLine();
            if (ParseArguments(args))
            {
                Console.WriteLine("Logging in...");

                if (Login())
                {
                    var cloudServers = new CloudServersProvider(auth);

                    var newServerList = new List<NewServer>();
                    var serversToBuild = 3;

                    var completedServers = 0;
                    var cursorPos = 0;
                    
                    // keep looping until all servers are finished building, or have returned an error state.
                    while (completedServers < serversToBuild)
                    {
                        try
                        {
                            if (newServerList.Count < serversToBuild)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    Console.WriteLine(String.Format("Creating server: {0}{1}...", ServerNamePrefix, i+1));

                                    // Create a 512mb cloud server instance using centos 6.0
                                    newServerList.Add(cloudServers.CreateServer(String.Format("{0}{1}", ServerNamePrefix, i+1), "a3a2c42f-575f-4381-9c6d-fcd3b7d07d17", "2", region: ServerRegion));
                                }

                                Console.WriteLine();
                                Console.WriteLine(String.Format(" {0,-2} {1,-10} {2,-14} {3,-15} {4,-10} {5,-21}", "%", "Name", "Password", "IPv4", "Status", "Task"));
                                cursorPos = Console.CursorTop;
                                
                                for (int i = 0; i < newServerList.Count; i++)
                                {
                                    Console.WriteLine();
                                }
                            }

                            completedServers = 0; // avoid duplicate counting
                            for (int i = 0; i < newServerList.Count; i++)
                            {
                                var server = newServerList[i].GetDetails();
                                
                                Console.SetCursorPosition(0, cursorPos +i);
                                Console.WriteLine(String.Format("{0,-3} {1,-10} {2,-14} {3,-15} {4,-10} {5,-21}", server.Progress, server.Name, newServerList[i].AdminPassword, server.AccessIPv4, server.VMState, server.TaskState));

                                if (server.Status == ServerState.ACTIVE || server.Status == ServerState.ERROR)
                                {
                                    completedServers++;
                                }

                                Thread.Sleep(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            PrintException(ex);
                            return;
                        }

                        Thread.Sleep(1000);
                    }
                }
            }

            Console.WriteLine();
            Console.Write("Press any key to exit");
            Console.ReadKey();
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
            catch (Exception ex)
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
            Console.WriteLine("challenge1 user= [password=] [apikey=] [accountregion=] [serverregion=] serverprefix=");
            Console.WriteLine();

            Console.WriteLine("user\t\tCloud Identity username");
            Console.WriteLine("pass\t\tCloud Identity password");
            Console.WriteLine("apikey\t\tAPI key if password is not specified");
            Console.WriteLine("accountregion\tSpecify LON if using a UK account");
            Console.WriteLine("serverregion\tRegion to build servers in.");
            Console.WriteLine("\t\t  If not specified, will build in default region");
            Console.WriteLine("serverprefix\tPrefix for server names");
            
            Console.WriteLine();

            Console.WriteLine("Examples:");
            Console.WriteLine("challenge1 user=user apikey=abc12 accountregion=LON serverprefix=db");
            Console.WriteLine("challenge1 user=user pass=hello serverprefix=web");
        }

        static string ReadIniValue(string Key)
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
                    case "user":
                        Username = args[index].Split('=')[1];
                        break;

                    case "pass":
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

                    case "serverprefix":
                        ServerNamePrefix = args[index].Split('=')[1];
                        break;

                    default:
                        PrintHelp();
                        return false;
                }

                index++;
            }

            // if the values aren't passed into the command line, read from %appdata%/.something

            if (String.IsNullOrWhiteSpace(Username))
                Username = ReadIniValue("username");

            if (String.IsNullOrWhiteSpace(Password) && String.IsNullOrWhiteSpace(APIKey))
            {
                Password = ReadIniValue("password");
                APIKey = ReadIniValue("apikey");
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
            Console.ReadKey();
            Environment.Exit(-1);
        }
    }
}
