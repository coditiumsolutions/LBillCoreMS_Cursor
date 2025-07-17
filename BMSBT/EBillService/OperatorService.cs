namespace BMSBT.EBillService
{
    public class OperatorService : IOperatorService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OperatorService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string BillingMonth
        {
            get
            {
                // Assuming you store the operator's month in session
                return _httpContextAccessor.HttpContext.Session.GetString("BillingMonth");
            }
        }

        public string BillingYear
        {
            get
            {
                // Assuming you store the operator's year in session
                return _httpContextAccessor.HttpContext.Session.GetString("BillingYear");
            }
        }

        public string OperatorId
        {
            get
            {
                return _httpContextAccessor.HttpContext.Session.GetString("OperatorId");
            }
        }

        public string ReadingDate
        {
            get
            {
                return _httpContextAccessor.HttpContext.Session.GetString("OperatorId");
            }
        }

        public string DueDate
        {
            get
            {
                return _httpContextAccessor.HttpContext.Session.GetString("OperatorId");
            }
        }

    }
}
