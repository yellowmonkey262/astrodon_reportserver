using Astrodon.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Astrodon.Reports.SupplierReport
{
    public class SupplierReport 
    {
        private DataContext _dataContext;
        public SupplierReport(DataContext dataContext)
        {
            _dataContext = dataContext;
        }


        internal byte[] RunReport(DateTime processMonth)
        {
            //setup query

            DateTime startDate = new DateTime(processMonth.Year, processMonth.Month, processMonth.Day);
            DateTime endDate = startDate.AddMonths(1).AddSeconds(-1);

            var q = from s in _dataContext.SupplierSet
                    from m in s.Maintenance
                    where m.DateLogged >= startDate && m.DateLogged <= endDate
                    group m by new
                    {
                        s.id,
                        s.CompanyName,
                        s.CompanyRegistration,
                        s.ContactPerson,
                        s.EmailAddress,
                        s.ContactNumber,
                        s.BlackListed,
                        m.BuildingMaintenanceConfiguration.Building.Building
                    } into grouped
                    select new SupplierReportDataItem
                    {
                        SupplierId = grouped.Key.id,
                        CompanyName = grouped.Key.CompanyName,
                        Registration = grouped.Key.CompanyRegistration,
                        ContactPerson = grouped.Key.ContactPerson,
                        Email = grouped.Key.EmailAddress,
                        Phone = grouped.Key.ContactNumber,
                        BlackList = grouped.Key.BlackListed ? "Yes" : "No",
                        Building = grouped.Key.Building,
                        LastUsed = grouped.Max(m => m.DateLogged),
                        Projects = grouped.Count()
                    };

            var reportData = q.OrderBy(a => a.CompanyName).ToList();
            if (reportData.Count == 0)
                return null;

            return RunReportToPdf(reportData,processMonth);
        }

        private byte[] RunReportToPdf(List<SupplierReportDataItem> data,  DateTime dDate)
        {
            string rdlcPath = "Astrodon.Reports.SupplierReport.SupplierReport.rdlc";
            byte[] report = null;

            Dictionary<string, IEnumerable> reportData = new Dictionary<string, IEnumerable>();
            Dictionary<string, string> reportParams = new Dictionary<string, string>();

            string period = dDate.ToString("MMM yyyy");

            reportParams.Add("Period", period);

            reportData.Add("SupplierData", data);

            using (RdlcHelper rdlcHelper = new RdlcHelper(rdlcPath,
                                                        reportData,
                                                        reportParams))
            {

                rdlcHelper.Report.EnableExternalImages = true;
                report = rdlcHelper.GetReportAsFile();
            }
            return report;
        }
    }
}
