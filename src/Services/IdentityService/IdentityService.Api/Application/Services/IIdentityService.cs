using IdentityServer.Application.Models;

namespace IdentityServer.Application.Services
{
    public interface IIdentityService
    {
        Task<LoginResponseModel> Login(LoginRequestModel requestModel);
    }
}
