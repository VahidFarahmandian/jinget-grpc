using Grpc.Core;
using MyNameSpace;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace gRPCSample_Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var grpc = new grpcHelper();
            Console.WriteLine(await grpc.GetRestrictedResource());
            do
            {
                Console.WriteLine("1: Unary, 2: Server Streaming, 3: Client Streaming, 4: Bidi Streaming, q: Exit");
                var input = Console.ReadLine();
                if (input == "q")
                    break;
                switch (input)
                {
                    case "1":
                        var country = await grpc.UnarySample();
                        Console.WriteLine($"DateTime: {DateTime.Now.ToString("HH:mm:ss.fff")} Name: {country.Name}({country.TwoCharName}-{country.ThreeCharName})");
                        break;
                    case "2":
                        try
                        {
                            await foreach (var responseStreamData in grpc.ServerStreamingSample().ResponseStream.ReadAllAsync())
                            {
                                PrintCountryResponse(responseStreamData);
                            }
                        }
                        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
                        {
                            Console.WriteLine("Deadline exceeded...");
                        }
                        break;
                    case "3":
                        try
                        {
                            PrintCountryResponse(await grpc.ClientStreamingSampleAsync());
                        }
                        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                        {
                            Console.WriteLine("Request cancelled...");
                        }
                        break;
                    case "4":
                        await grpc.BidiStreamingSampleAsync();
                        break;
                    default:
                        Console.WriteLine("Only 1-4 please");
                        break;
                }
                Console.WriteLine("Press any key restart...");
                Console.ReadKey();
                Console.Clear();
            } while (true);



            void PrintCountryResponse(CountryCollectionResponse response)
            {
                foreach (CountryResponse country in response.Countries)
                {
                    Console.WriteLine($"Req: {country.RequestTime}, Res: {DateTime.Now:HH:mm:ss.fff}, Name: {country.Name}({country.TwoCharName}-{country.ThreeCharName})");
                }
            }

            Console.Read();
        }
    }
}
