using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Challenge_3
{
    class Program
    {
        private static RackspaceCloudIdentity auth = null;
        private static string Username = null;
        private static string Password = null;
        private static string APIKey = null;
        private static string AccountRegion = null;

        private static string Source = null;
        private static string Container = null;

        static void Main(string[] args)
        {
            Console.WriteLine();
            if (ParseArguments(args))
            {
                DirectoryInfo directory = null;

                try
                {
                    directory = new DirectoryInfo(Source);
                    if (!directory.Exists)
                    {
                        throw new Exception(String.Format("Source directory does not exist\n{0}", Source));
                    }
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                }

                Console.WriteLine(String.Format("Source directory: {0}", Source));
                Console.WriteLine(String.Format("Destination container: {0}", Container));
                Console.WriteLine("Logging in...");
                Console.WriteLine();

                if (Login())
                {
                    var cloudFiles = new CloudFilesProvider(auth);

                    try
                    {
                        switch (cloudFiles.CreateContainer(Container))
                        {
                            case ObjectStore.ContainerCreated:
                            case ObjectStore.ContainerExists:
                                foreach (var file in directory.GetFiles())
                                {
                                    Console.WriteLine(String.Format("{0,3:0}% Uploading: {1}", 0, file.Name));
                                    cloudFiles.CreateObjectFromFile(Container, file.FullName, progressUpdated: delegate(long p)
                                    {
                                        Console.SetCursorPosition(0, Console.CursorTop -1);
                                        Console.WriteLine(String.Format("{0,3:0}% Uploading: {1}", ((float)p / (float)file.Length) * 100, file.Name));
                                    });
                                }
                                break;
                            default:
                                throw new Exception(String.Format("Unknown error when creating container {0}", Container));
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
            Console.WriteLine("challenge3 user= [password=] [apikey=] [accountregion=] container= source=");
            Console.WriteLine();

            Console.WriteLine("user\t\tCloud Identity username");
            Console.WriteLine("pass\t\tCloud Identity password");
            Console.WriteLine("apikey\t\tAPI key if password is not specified");
            Console.WriteLine("accountregion\tSpecify LON if using a UK account");
            Console.WriteLine("container\tThe destination container");
            Console.WriteLine("source\t\tThe source folder to upload to cloud files");

            Console.WriteLine();

            Console.WriteLine("Examples:");
            Console.WriteLine("challenge3 user=user apikey=abc12 accountregion=LON container=test source=c:\\temp");
            Console.WriteLine("challenge3 user=user pass=hello container=test source=c:\\temp");
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

                    case "source":
                        Source = args[index].Split('=')[1];
                        break;

                    case "container":
                        Container = args[index].Split('=')[1];
                        break;

                    default:
                        PrintHelp(args[index]);
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

            if (String.IsNullOrWhiteSpace(Username) || (String.IsNullOrWhiteSpace(Password) && String.IsNullOrWhiteSpace(APIKey)) || String.IsNullOrWhiteSpace(Source) || String.IsNullOrWhiteSpace(Container))
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
