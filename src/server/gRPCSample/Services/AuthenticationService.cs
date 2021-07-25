using Auth;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace gRPCSample.Services
{
    public class AuthenticationService : Auth.Authenticate.AuthenticateBase
    {
        private readonly ILogger<AuthenticationService> logger;

        public AuthenticationService(ILogger<AuthenticationService> logger)
        {
            this.logger = logger;
        }
        public override async Task<Auth.Empty> Login(Auth.LoginRequest request, ServerCallContext context)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("a sample secret key for my jwt token");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, request.Username) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            string jwtToken = tokenHandler.WriteToken(token);

            context.ResponseTrailers.Add("Authorization", jwtToken);
            return await Task.FromResult(new Auth.Empty());
        }

        [Authorize]
        public override async Task<Auth.AuthorizeResponse> RestrictedSource(Auth.Empty request, ServerCallContext context)
        {
            var user = context.GetHttpContext().User;
            return await Task.FromResult(new Auth.AuthorizeResponse() { Message = $"Welcome {user.Identity.Name}" });
        }
        public override async Task<AuthorizeResponse> Reflection(Empty request, ServerCallContext context)
        {
            var response = new AuthorizeResponse() { Message = "Hello reflection" };
            logger.LogDebug(response.Message);
            return await Task.FromResult(response);
        }
    }
}
