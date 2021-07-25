using Grpc.Core;
using gRPCSample.Data;
using MyNameSpace;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace gRPCSample.Services
{
    public class SampleService : MyNameSpace.Samples.SamplesBase
    {
        private readonly SampleContext _dataContext;

        public SampleService(SampleContext dataContext) => _dataContext = dataContext;

        public override async Task<CountryResponse> UnarySample(Request request, ServerCallContext context)
        {
            var country = _dataContext.Countries.FirstOrDefault(x => x.CountryID == request.Id);

            return await Task.FromResult(new CountryResponse
            {
                Name = country.CountryName,
                TwoCharName = country.TwoCharCountryCode,
                ThreeCharName = country.ThreeCharCountryCode
            });
        }

        public override async Task ServerStreamingSample(PaginatedRequest request, IServerStreamWriter<CountryCollectionResponse> responseStream, ServerCallContext context)
        {
            var pageIndex = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var countries = _dataContext.Countries.Skip((pageIndex++) * request.PageSize).Take(request.PageSize).ToList();
                if (!countries.Any())
                {
                    break;
                }

                var reply = new CountryCollectionResponse();
                reply.Countries.AddRange(
                    countries.Select(x => new CountryResponse
                    {
                        Name = x.CountryName,
                        TwoCharName = x.TwoCharCountryCode,
                        ThreeCharName = x.ThreeCharCountryCode,
                        RequestTime = request.RequestTime
                    }));
                await responseStream.WriteAsync(reply);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            if (context.CancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("Request cancelled");
            }
        }

        public override async Task<CountryCollectionResponse> ClientStreamingSample(IAsyncStreamReader<CountryCollectionRequest> requestStream, ServerCallContext context)
        {
            CountryCollectionResponse responses = new();
            await foreach (var message in requestStream.ReadAllAsync())
            {
                var countries = _dataContext.Countries.Where(x => message.Requests.Select(x => x.Id).Contains(x.CountryID)).ToList();
                responses.Countries.AddRange(
                    countries.Select(x => new CountryResponse
                    {
                        Name = x.CountryName,
                        TwoCharName = x.TwoCharCountryCode,
                        ThreeCharName = x.ThreeCharCountryCode,
                        RequestTime = message.Requests.First(r => r.Id == x.CountryID).RequestTime
                    }));
            }
            return responses;
        }

        public override async Task BidiStreamingSample(IAsyncStreamReader<CountryCollectionRequest> requestStream, IServerStreamWriter<CountryCollectionResponse> responseStream, ServerCallContext context)
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                var countries = _dataContext.Countries.Where(x => message.Requests.Select(x => x.Id).Contains(x.CountryID)).ToList();

                var reply = new CountryCollectionResponse();
                reply.Countries.AddRange(
                   countries.Select(x => new CountryResponse
                   {
                       Name = x.CountryName,
                       TwoCharName = x.TwoCharCountryCode,
                       ThreeCharName = x.ThreeCharCountryCode,
                       RequestTime = message.Requests.First(r => r.Id == x.CountryID).RequestTime
                   }));
                await responseStream.WriteAsync(reply);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
