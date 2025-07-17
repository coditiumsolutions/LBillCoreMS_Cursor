using BMSBT.Models;

namespace BMSBT.BillServices
{
    public class SetMonth
    {

      
        public void SetBillingMonth(string currentBillingMonth, string currentBillingYear)
        {
            // Map month numbers to their respective names
            var monthMap = new Dictionary<int, string>
    {
        { 1, "January" }, { 2, "February" }, { 3, "March" },
        { 4, "April" }, { 5, "May" }, { 6, "June" },
        { 7, "July" }, { 8, "August" }, { 9, "September" },
        { 10, "October" }, { 11, "November" }, { 12, "December" }
    };

            // Parse current month
            int currentMonth;
            if (!int.TryParse(currentBillingMonth, out currentMonth))
            {
                currentMonth = monthMap.FirstOrDefault(x => x.Value.Equals(currentBillingMonth, StringComparison.OrdinalIgnoreCase)).Key;
                if (currentMonth == 0)
                {
                    throw new ArgumentException($"Invalid month value: {currentBillingMonth}. Must be a valid integer or month name.");
                }
            }

            // Parse current year
            int currentYear;
            if (!int.TryParse(currentBillingYear, out currentYear))
            {
                throw new ArgumentException($"Invalid year value: {currentBillingYear}. Must be a valid integer.");
            }

            // Calculate the previous month and year
            int previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            int previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            // Convert to string format
            
            BillCreationState.PreviousMonth =  monthMap[previousMonth];
            BillCreationState.PreviousYear  =  previousYear.ToString();
            BillCreationState.CurrentMonth  =  currentBillingMonth;
            BillCreationState.CurrentYear   =  currentBillingYear;

        }


    }
}
