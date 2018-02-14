using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Astrodon.Reports.AllocationWorksheet
{
    class AllocationItem
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public int Priority { get; set; }

        public string BuildingCode { get; set; }

        public int BuildingId { get; set; }

        public string BuildingName { get; set; }

        public DateTime OrderDate { get; set; }

        public string Reason { get; set; }

        public DateTime? ReasonDate { get; set; }

        public string AllocationReason
        {
            get
            {
                string result = "";
                if (ReasonDate != null)
                     result = Reason + " " + ReasonDate.Value.ToString("yyyy/MM/dd");
                else
                    result = Reason;

                if (FinancialStartDate != null)
                    result = result + " Financial Start: " + FinancialStartDate.Value.ToString("yyyy/MM/dd");


                if (FinancialEndDate != null)
                    result = result + " Financial End: " + FinancialEndDate.Value.ToString("yyyy/MM/dd");

                return result;
            }
        }

        public DateTime? FinancialStartDate { get; internal set; }
        public DateTime? FinancialEndDate { get; internal set; }
    }
}
