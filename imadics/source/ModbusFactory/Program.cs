using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SoftControllers;
using CsvHelper;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;

namespace SampleScenes
{
    class Program 
    {    
        static async Task Main (string[] args) 
        {
            Console.WriteLine ("Factory I/O Sample Scenes Modbus Controllers");
            if (args.Length != 2 
            || !TryParseConnectionString (args[0], out var connectionInformation)
            || !TryGetSceneType(args[1], out var sceneType))
            {
                Console.WriteLine ("Missing or invalid arguments. Please provide valid connection information and scene configuration.");
                Console.WriteLine ("Usage: SortingStation IP-ADDRESS:PORT/DEVICE-ID SCENE CONFIG.CSV [WAIT|AUTOSTART]");
                Console.WriteLine ("");
                Console.WriteLine ("       IP-ADDRESS:PORT/DEVICE-ID  the IP address and port of the MODBUS TCP server, DEVICE-ID accessible on that server.");
                Console.WriteLine ("       SCENE name of the scene");
                return;
            }
            var tagsFile = $"Tags_{args[1]}_Modbus.csv";
            if (!TryLoadTagsFile(tagsFile, out var registerMap))
            {
                throw new ArgumentException("Cannot load Tags file.");
            }
            var screen = Screen.CreateScreen(out var ncurses);
            if (ncurses == false)
            {
                Console.WriteLine("NCurses screen not supported in the current terminal.");
            }

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(connectionInformation);
            serviceCollection.AddSingleton(registerMap);
            serviceCollection.AddSingleton(screen);

            RegisterControllers(serviceCollection);
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
    
            var cts = new CancellationTokenSource ();
            Console.CancelKeyPress += new ConsoleCancelEventHandler ((sender, args) => { cts.Cancel (); });
            
            screen.MoveAddString (1, 2, $"FACTORY");

            screen.MoveAddString (2, 2, $"Waiting for simulation to start...                     ");
            screen.Refresh ();
            
            using var factory = new SoftControllers.ModbusServerFactoryManager(connectionInformation.endPoint, connectionInformation.deviceId, registerMap);
            await factory.WaitForStart();
            
            using var sceneController = serviceProvider.GetService(sceneType) as ModbusController;
        
            screen.MoveAddString (2, 2, $"Factory is alive. Running controllers...                     ");
            screen.Refresh ();
            
            var token = cts.Token;
            var sortingTask = sceneController.Run(token);
            
            await Task.WhenAll(sortingTask);
            screen.MoveAddString (2, 2, $"The simulation has been terminated. ");
            screen.Refresh ();
        }

        private static bool TryGetStartOption(string arg, out bool autoStart)
        {
            autoStart = String.Equals(arg, "AUTOSTART", StringComparison.InvariantCultureIgnoreCase);
            return true;
        }

        private static void RegisterControllers(IServiceCollection services)
        {
            foreach(var controllerType in typeof(Program).Assembly.GetTypes().Where(t=>t.IsSubclassOf(typeof(Controller))))
            {
                services.AddTransient(controllerType);
            }
        }

        static bool TryGetSceneType(string sceneName, out Type sceneType)
        {
            var typeName = $"{sceneName}Controller";
            sceneType = typeof(Program).Assembly.GetTypes().FirstOrDefault(t => String.Equals(t.Name, typeName, StringComparison.InvariantCultureIgnoreCase));
            return sceneType != null;
        }

        static bool TryLoadTagsFile(string tagFilename, out RegisterMap tags)
        {
            var r = new Regex (@"^(?<register>([^\d]+))(?<index>\d+)", RegexOptions.None, TimeSpan.FromMilliseconds (150));
            RegisterMap.TagRecord GetRecord(dynamic input)
            {
                var m = r.Match (input.Address);
                if (m.Success)
                {
                    var regType = Enum.Parse<RegisterType>(m.Result("${register}").Replace(" ",""));
                    var address = ushort.Parse(m.Result ("${index}"));
                    return new RegisterMap.TagRecord(input.Name, regType, address); 
                }
                else
                    throw new ArgumentException("Input dynamic object does not contain correct data.");
            }

            try
            {
                var binRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var fullname = Path.Combine(binRootPath, "Scenes", tagFilename);
                using (var reader = new StreamReader(fullname))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>();
                    tags = new RegisterMap(records.Select(GetRecord)); 
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Cannot read tags file: {e.Message}");
                tags = null;
                return false;
            }
        }

        private static bool TryParseConnectionString (string str, out ConnectionInformation connection) 
        {
            var r = new Regex (@"^(?<address>([^/]+))/(?<device>\d+)", RegexOptions.None, TimeSpan.FromMilliseconds (150));
            Match m = r.Match (str);
            if ((m.Success) 
                && IPEndPoint.TryParse (m.Result ("${address}"), out var endPoint)
                && Byte.TryParse (m.Result ("${device}"), out var deviceId))
                {
                    connection = new ConnectionInformation(endPoint, deviceId);
                    return true;
                }
                connection = default;
            return false;
        }
    }
}