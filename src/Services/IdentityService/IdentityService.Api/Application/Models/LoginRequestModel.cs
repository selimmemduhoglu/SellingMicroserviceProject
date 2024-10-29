namespace IdentityServer.Application.Models
{
    public class LoginRequestModel
    {
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;

    }
}
