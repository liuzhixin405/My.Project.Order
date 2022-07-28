namespace Ordering.API.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private IHttpContextAccessor _contextAccessor;
        public IdentityService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        public string GetUserIdentity()
        {
            return _contextAccessor.HttpContext?.User.FindFirst("sub")?.Value??String.Empty;
        }

        public string GetUserName()
        {
            return _contextAccessor.HttpContext?.User.Identity?.Name ?? String.Empty;
        }
    }
}
