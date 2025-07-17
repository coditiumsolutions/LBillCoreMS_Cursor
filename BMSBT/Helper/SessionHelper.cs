namespace BMSBT.Helper
{
    public class SessionHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetOperatorId()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            return session?.GetString("OperatorId");
        }
    }

}
