using BMSBT.Models;

namespace BMSBT.Services
{
    public static class CustomerAuditHelper
    {
        public const string ECustomerTable = "CustomersDetail";
        public const string ECustomerModule = "E-Customer Management";
        public const string MCustomerTable = "CustomersMaintenance";
        public const string MCustomerModule = "M-Customer Management";

        public static Dictionary<string, object?> CreateElectricitySnapshot(CustomersDetail customer)
        {
            return new Dictionary<string, object?>
            {
                ["CustomerNo"] = customer.CustomerNo,
                ["Btno"] = customer.Btno,
                ["CustomerName"] = customer.CustomerName,
                ["Project"] = customer.Project,
                ["SubProject"] = customer.SubProject,
                ["TariffName"] = customer.TariffName,
                ["Category"] = customer.Category,
                ["Block"] = customer.Block,
                ["Sector"] = customer.Sector,
                ["PloNo"] = customer.PloNo,
                ["City"] = customer.City,
                ["MobileNo"] = customer.MobileNo,
                ["TelephoneNo"] = customer.TelephoneNo,
                ["MeterNo"] = customer.MeterNo,
                ["MeterType"] = customer.MeterType,
                ["BillStatus"] = customer.BillStatus,
                ["BillGenerationStatus"] = customer.BillGenerationStatus
            };
        }

        public static Dictionary<string, object?> CreateMaintenanceSnapshot(CustomersMaintenance customer)
        {
            return new Dictionary<string, object?>
            {
                ["CustomerNo"] = customer.CustomerNo,
                ["BTNo"] = customer.BTNo,
                ["CustomerName"] = customer.CustomerName,
                ["Project"] = customer.Project,
                ["SubProject"] = customer.SubProject,
                ["TariffName"] = customer.TariffName,
                ["Category"] = customer.Category,
                ["Block"] = customer.Block,
                ["Sector"] = customer.Sector,
                ["PloNo"] = customer.PloNo,
                ["City"] = customer.City,
                ["MobileNo"] = customer.MobileNo,
                ["MeterNo"] = customer.MeterNo,
                ["BTNoMaintenance"] = customer.BTNoMaintenance,
                ["BillStatusMaint"] = customer.BillStatusMaint
            };
        }

        public static string GetElectricityRecordId(CustomersDetail customer)
        {
            return string.IsNullOrWhiteSpace(customer.Btno) ? customer.Uid.ToString() : customer.Btno;
        }

        public static string GetMaintenanceRecordId(CustomersMaintenance customer)
        {
            return string.IsNullOrWhiteSpace(customer.BTNo) ? customer.Uid.ToString() : customer.BTNo;
        }
    }
}
