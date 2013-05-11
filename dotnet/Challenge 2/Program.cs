using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Challenge_2
{
    class Program
    {
        private static RackspaceCloudIdentity auth = null;
        private static string Username = null;
        private static string Password = null;
        private static string APIKey = null;
        private static string AccountRegion = null;

        private static string ServerRegion = null;

        static void Main(string[] args)
        {
            /* Login
             * List servers
             * select server to clone
             * create image of server
             * create server using image
             * delete image
             */



            Console.WriteLine();
            if (ParseArguments(args))
            {
                Console.WriteLine("Logging in...");

                if (Login())
                {
                    var cloudServers = new CloudServersProvider(auth);
                    
                    Console.WriteLine("Listing servers...");

                    try
                    {
                        var servers = cloudServers.ListServers(status: ServerState.ACTIVE, limit: 9, region: ServerRegion).ToList();

                        if (servers.Count == 0)
                            throw new Exception("No active servers on account");

                        Console.WriteLine();
                    
                        for (int i = 0; i < servers.Count(); i++)
                        {
                            Console.WriteLine(String.Format("{0,2} {1}", i+1, servers[i].Name));
                        }

                        Console.WriteLine();
                    
                        var validSelection = false;
                        int index = -1;
                        do
                        {
                            Console.Write(String.Format("Select server to clone [1-{0}]:", Math.Min(9, servers.Count)));
                            var selection = Console.ReadLine();
                            if (Int32.TryParse(selection, out index))
                            {
                                index--;
                                validSelection = (index >= 0 && index < servers.Count);
                            }
                        } while (!validSelection);                        

                        var imageName = String.Format("clone{0}", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"));
                        Console.Write("Enter new server name:");
                        var serverName = Console.ReadLine();

                        // get detailed information of the selected server, this allows us to access the server's flavor ID
                        var server = cloudServers.GetDetails(servers[index].Id, ServerRegion);





                        Console.WriteLine("Creating image...");
                        if (cloudServers.CreateImage(servers[index].Id, imageName, region: ServerRegion))
                        {
                            SimpleServerImage image = null;
                            // find the image we just created so we can monitor the progress.
                            image = cloudServers.ListImages(server: servers[index].Id, region: ServerRegion).Where(result => result.Name == imageName).Single();
                            if (image != null)
                            {
                                try
                                {
                                    image.WaitForActive(progressUpdatedCallback: delegate(int p)
                                    {
                                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                                        Console.WriteLine(String.Format("{0} {1}%", "Creating image...", p));
                                    });

                                    Console.WriteLine("Creating Server...");
                                    var newServer = cloudServers.CreateServer(serverName, image.Id, server.Flavor.Id, region: ServerRegion);

                                    Console.WriteLine(String.Format(" {0,-2} {1,-10} {2,-14} {3,-15} {4,-10} {5,-21}", "%", "Name", "Password", "IPv4", "Status", "Task"));
                                    Console.WriteLine();

                                    newServer.WaitForActive(progressUpdatedCallback: delegate(int p)
                                    {
                                        server = newServer.GetDetails();

                                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                                        Console.WriteLine(String.Format("{0,-3} {1,-10} {2,-14} {3,-15} {4,-10} {5,-21}", p, server.Name, newServer.AdminPassword, server.AccessIPv4, server.VMState, server.TaskState));
                                    });

                                    // output the server details one last time so we capture the IPv4 address.
                                    server = newServer.GetDetails();
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine(String.Format("100 {0,-10} {1,-14} {2,-15} {3,-10} {4,-21}", server.Name, newServer.AdminPassword, server.AccessIPv4, server.VMState, server.TaskState));
                                }
                                finally
                                {
                                    Console.WriteLine("Cleaning up temporary image...");
                                    Console.WriteLine();
                                    if (image.Delete())
                                    {
                                        image.WaitForDelete(progressUpdatedCallback: delegate(int p)
                                        {
                                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                                            Console.WriteLine(String.Format("Cleaning up temporary image... {0}", p));
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Failed to create image");
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex);
                    }
                }
            }

            Console.WriteLine();
            Console.Write("Press any key to exit");
            Console.ReadKey();
        }


        #region Helper Functions
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

            Console.WriteLine();

            Console.WriteLine("Examples:");
            Console.WriteLine("challenge1 user=user apikey=abc12 accountregion=LON");
            Console.WriteLine("challenge1 user=user pass=hello");
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

            if (String.IsNullOrWhiteSpace(Username) || (String.IsNullOrWhiteSpace(Password) && String.IsNullOrWhiteSpace(APIKey)))
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
        #endregion
    }
}
