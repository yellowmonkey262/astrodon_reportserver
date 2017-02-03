using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Astrodon.Data.MaintenanceData;
using Astradon.Data.Utility;

namespace Astrodon.Reports.MaintenanceReport
{
    public class MaintenanceReportDataItem:ReportDataBase
    {
        public string AccountNumber { get;  set; }
        public decimal Amount { get;  set; }
        public string Bank { get;  set; }
        public string Branch { get;  set; }
        public string BranchCode { get;  set; }
        public MaintenanceClassificationType MaintenanceClassificationType { get; internal set; }
        public string Classification
        {
            get
            {
                return NameSplitting.SplitCamelCase(MaintenanceClassificationType);
            }
        }


        public string CompanyReg { get;  set; }
        public string ContactNumber { get;  set; }
        public string ContactPerson { get;  set; }
        public string Description { get;  set; }
        public string EmailAddress { get;  set; }
        public string InvoiceNumber { get;  set; }
        public DateTime MaintenanceDate { get;  set; }
        public string MaintenanceType { get;  set; }

        public string MaintenanceDisplayName { get { return MaintenanceType + " " + PastelAccountNumber + "-" + PastelAccountName; } }

        public string PastelAccountName { get;  set; }
        public string PastelAccountNumber { get;  set; }
        public string SerialNumber { get;  set; }
        public string Supplier { get;  set; }
        public string Unit { get;  set; }
        public string VatNumber { get;  set; }
        public int? WarrantyDuration { get;  set; }
        public DateTime? WarrantyExpires { get;  set; }
        public string WarrantyNotes { get;  set; }
        public DurationType? WarrantyType { get;  set; }

        public string WarrantyDescription
        {
            get
            {
                if (WarrantyDuration == null)
                    return null;
                if (WarrantyType == null)
                    return null;

                return WarrantyDuration.Value.ToString() + " " + WarrantyType.Value.ToString();
            }
        }

        public decimal Budget { get; set; }
        public decimal Balance { get; set; }
        public decimal BudgetAvailable { get; set; }

        public void LoadBudget(List<PervasiveAccount> accountList)
        {
            var itm = accountList.FirstOrDefault(a => a.AccNumber == PastelAccountNumber);
            if(itm != null)
            {
                Budget = itm.Budget;
                Balance = itm.ClosingBalance;
                BudgetAvailable = itm.BudgetAvailable;
            }
        }
    }
}
