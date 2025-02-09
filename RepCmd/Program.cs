using CommandLine;
using Replicate;
using Replicate.MetaData;
using Replicate.RPC;
using Replicate.Serialization;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RepCmd {
    class Program {
        public class Options {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            //[Option("json", Required = false, HelpText = "Use a Json instead of a BinarySerializer")]
            //public bool Json { get; set; }
            [Option('s', "socket", Required = false, HelpText = "Host:Port to connect a SocketChannel to.")]
            public string Socket { get; set; }
            [Option('m', "method", Required = false, HelpText = "Method to request from")]
            public string Method { get; set; }
            [Option('r', "request", Required = false, HelpText = "Request value")]
            public string Request { get; set; }
            [Option('l', "list", Required = false, HelpText = "List methods")]
            public bool ListMethods { get; set; }
        }
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options).GetAwaiter().GetResult())
                .WithNotParsed(errors => {
                    foreach (var e in errors) {
                        Console.WriteLine(e.ToString());
                    }
                });
        }
        static async Task Run(Options options) {
            var model = new ReplicationModel(false);
            var json = new JSONSerializer(model);
            if (options.Socket != null) {
                var r = new Regex(@"(.*):(\d*)");
                var match = r.Match(options.Socket);

                var channel = SocketChannel.Connect(match.Groups[1].Value, int.Parse(match.Groups[2].Value), new BinarySerializer());
                var reflection = channel.CreateProxy<IReflectionService>();
                model.LoadFrom(await reflection.Model());
                var services = await reflection.Services();
                if (options.ListMethods) {
                    foreach (var service in services) {
                        Console.WriteLine($"{service.Key.Type.Id}:{service.Key.Method}");
                    }
                    return;
                } else if (options.Method != null) {
                    var splitted = options.Method.Split(':');
                    var type = model.Types[splitted[0]];

                    var service = services.FirstOrDefault(s =>
                        s.Key.Type.Id.Name == splitted[0] && s.Key.Method.Name == splitted[1]);
                    var requestType = model.GetType(service.Request);
                    var responseType = model.GetType(service.Response);
                    var request = json.Deserialize(requestType, options.Request ?? "null");
                    var resultStream = await channel.Request(new RPCRequest() {
                        Request = request,
                        Contract = new RPCContract(requestType, responseType),
                        Endpoint = service.Key,
                    }, ReliabilityMode.Reliable);
                    var result = channel.Serializer.Deserialize(responseType, resultStream);
                    Console.WriteLine(json.SerializeString(responseType, result));
                }
            }
        }
    }
}
