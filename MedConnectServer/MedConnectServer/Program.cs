using System;
using System.Threading;

using Nancy;
using Nancy.Hosting.Self;

using MedConnectMongoLib;

namespace MedConnectServer {
    public static class Program {
        public static string Salt { get; private set; }

        static Program() {
            StaticConfiguration.DisableErrorTraces = !ServerConfig.Data.Http.DebugMode;
        }

        public static void Main(string[] args) {
            Console.WriteLine("Checking mongo connection..");
            Salt = MongoConnection.MongoCtl.GetSalt().Result;

            if (Salt != ServerConfig.Data.Mongo.Salt) {
                throw new Exception("Invalid salt in mongo configuration");
            }

            using (var host = new NancyHost(new Uri(ServerConfig.Data.Http.Url))) {
                host.Start();
                Console.WriteLine($"Listening on {ServerConfig.Data.Http.Url}..");
                Console.WriteLine("Press ESC to quit..");
    
                for (;;) {
                    while (Console.KeyAvailable) {
                        if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                            goto abort;
                    }

                    Thread.Sleep(100);
                }

                abort:
                Console.WriteLine("Bye.");
            }
        }
    }
}
