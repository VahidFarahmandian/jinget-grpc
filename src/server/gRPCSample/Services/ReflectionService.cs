using Grpc.Core;
using System.Threading.Tasks;

namespace gRPCSample.Services
{
    public class ReflectionService : Reflection.ReflectionService.ReflectionServiceBase
    {
        public override async Task<Reflection.Response> Reflection(Reflection.Empty request, ServerCallContext context)
        {
            var response = new Reflection.Response() { Message = "Hello reflection" };
            return await Task.FromResult(response);
        }
    }
}
