using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace gRPCSample.Middlewares
{
    public class AuthorizationHandler : IAuthorizationHandler
    {
        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            try
            {
                bool hasToken = ((DefaultHttpContext)context.Resource).Request.Headers.TryGetValue("Authorization", out StringValues token);

                if (hasToken is not true)
                {
                    context.Fail();
                }
                if (token.First().StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
                    token = token.First().Substring(7);
                var jwt = new JwtSecurityTokenHandler().ReadToken(token.ToString()) as JwtSecurityToken;

                ((DefaultHttpContext)context.Resource).User = new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims, "jwt"));
                context.Succeed(context.Requirements.First(x => x.GetType() == typeof(DenyAnonymousAuthorizationRequirement)));
            }
            catch
            {
                context.Fail();
            }
        }

    }
}
