using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using MyNameSpace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace gRPCSample_Client
{
    class grpcHelper
    {
        Samples.SamplesClient client;
        Auth.Authenticate.AuthenticateClient authClient;
        GrpcChannel channel;

        public grpcHelper()
        {
            var channelMethodConfigs = new MethodConfig
            {
                Names = { MethodName.Default },

                RetryPolicy = new RetryPolicy
                {
                    //The maximum number of call attempts, including the original attempt
                    MaxAttempts = 5,

                    //determines when the next retry attempt is made. After each attempt, the current backoff is multiplied by BackoffMultiplier
                    InitialBackoff = TimeSpan.FromSeconds(10),

                    //The maximum backoff places an upper limit on exponential backoff growth
                    MaxBackoff = TimeSpan.FromSeconds(60),

                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            };
            channel = GrpcChannel.ForAddress("https://localhost:5001"
                , new GrpcChannelOptions
                {
                    ServiceConfig = new ServiceConfig
                    {
                        MethodConfigs = { channelMethodConfigs }
                    }
                }
                );
            client = new Samples.SamplesClient(channel);
            authClient = new Auth.Authenticate.AuthenticateClient(channel);
        }


        internal async Task<string> Login()
        {
            var response = authClient.LoginAsync(new Auth.LoginRequest { Username = "vahid", Password = "123" });
            await response;
            var trailers = response.GetTrailers();
            return trailers.GetValue("authorization").ToString();
        }

        internal async Task<string> GetRestrictedResource()
        {
            var task = await Task.Factory
                .StartNew(async () =>
                {
                    var response = authClient.LoginAsync(new Auth.LoginRequest { Username = "vahid", Password = "123" });
                    await response;
                    var trailers = response.GetTrailers();
                    return trailers.GetValue("authorization");
                })
                .ContinueWith(async (login) =>
                {
                    string token = await await login;
                    var headers = new Metadata
                    {
                        { "Authorization", $"Bearer {token}" }
                    };
                    return (await authClient.RestrictedSourceAsync(new Auth.Empty(), headers)).Message;
                });
            return await task;
        }

        internal async Task<CountryResponse> UnarySample() => await client.UnarySampleAsync(new Request { Id = 104 });

        internal AsyncServerStreamingCall<CountryCollectionResponse> ServerStreamingSample() =>
            client.ServerStreamingSample(new PaginatedRequest { PageSize = 10, RequestTime = DateTime.Now.ToString("HH:mm:ss.fff") }, deadline: DateTime.UtcNow.AddSeconds(10));

        internal async Task<CountryCollectionResponse> ClientStreamingSampleAsync()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            var call = client.ClientStreamingSample(cancellationToken: token);
            for (int i = 1; i <= 250; i += 30)
            {
                var request = new CountryCollectionRequest();
                request.Requests.AddRange(new List<Request> {
                    new Request {
                        Id = i,
                        RequestTime = DateTime.Now.ToString("HH:mm:ss.fff")
                    },
                    new Request {
                        Id = (i++) + 30,
                        RequestTime = DateTime.Now.ToString("HH:mm:ss.fff")
                    }
                });
                await call.RequestStream.WriteAsync(request);
                if (i > 100)
                    source.Cancel();
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            await call.RequestStream.CompleteAsync();

            return await call.ResponseAsync;
        }

        internal async Task<Task> BidiStreamingSampleAsync()
        {
            var call = client.BidiStreamingSample();

            var responseReaderTask = Task.Factory.StartNew(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    foreach (var country in response.Countries)
                    {
                        Console.WriteLine($"Req: {country.RequestTime}. Res:{DateTime.Now:HH:mm:ss.fff}, Name: {country.Name}({country.TwoCharName}-{country.ThreeCharName})");
                    }
                }
            });

            for (int i = 1; i <= 250; i += 30)
            {
                var request = new CountryCollectionRequest();
                request.Requests.AddRange(new List<Request> {
                    new Request {
                        Id = i,
                        RequestTime = DateTime.Now.ToString("HH:mm:ss.fff")
                    },
                    new Request {
                        Id = (i++) + 30,
                        RequestTime = DateTime.Now.ToString("HH:mm:ss.fff")
                    }
                });

                await call.RequestStream.WriteAsync(request);
                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(0, 5)));
            }

            await call.RequestStream.CompleteAsync();
            return await responseReaderTask;
        }
    }
}
