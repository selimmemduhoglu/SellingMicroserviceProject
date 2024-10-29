using IdentityServer.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityServer.Application.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IConfiguration configuration;

        public IdentityService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<LoginResponseModel> Login(LoginRequestModel requestModel)
        {
            // DB Process will be here. Check if user information is valid and get details

            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, requestModel.UserName),
                new Claim(ClaimTypes.Name, "Selim Memduhoglu"),
                new Claim(ClaimTypes.DateOfBirth,"09/10/1997"),
                new Claim(ClaimTypes.GivenName,"Microservice-Project"),
                new Claim(ClaimTypes.Email,"testmail@email.com"),
            };

            string keyword = "TokenSecurityKey";
            string tokenSecurityKey = configuration.GetValue<string>(keyword);


            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecurityKey));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            DateTime expiry = DateTime.Now.AddDays(10);

            JwtSecurityToken token = new JwtSecurityToken(claims: claims, expires: expiry, signingCredentials: creds, notBefore: DateTime.Now);

            string encodedJwt = new JwtSecurityTokenHandler().WriteToken(token);

            LoginResponseModel response = new()
            {
                UserToken = encodedJwt,
                UserName = requestModel.UserName
            };

            return Task.FromResult(response);
        }
    }
}
