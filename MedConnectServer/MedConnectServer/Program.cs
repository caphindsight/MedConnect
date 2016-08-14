using System;
using System.Threading;

using Nancy;
using Nancy.Hosting.Self;

using MedConnectMongoLib;

namespace MedConnectServer {
    public sealed class MyModule : NancyModule {
        public MyModule() {
            Get["/"] = _ => $"Salt: {Program.Salt}\n";
        }
    }

    public static class Program {
        private static readonly MongoCtl Mongo_;
        public static string Salt { get; private set; }

        static Program() {
            Mongo_ = new MongoCtl(ServerConfig.Data.Mongo.ConnectionString, ServerConfig.Data.Mongo.Database);
        }

        public static void Main(string[] args) {
            Console.WriteLine("Checking mongo connection..");
            Salt = Mongo_.GetSalt().Result;

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
