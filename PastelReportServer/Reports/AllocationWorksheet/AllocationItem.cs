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
                if (ReasonDate != null)
                    return Reason + " " + ReasonDate.Value.ToString("yyyy/MM/dd");
                else
                    return Reason;
            }
        }
    }
}
