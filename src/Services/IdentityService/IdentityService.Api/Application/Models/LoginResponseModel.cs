namespace IdentityServer.Application.Models
{
    public class LoginResponseModel
    {
        public string UserName { get; set; } = default!;
        public string UserToken { get; set; } = default!;
    }
}
