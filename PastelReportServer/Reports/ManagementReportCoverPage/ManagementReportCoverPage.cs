using Astrodon.Reports.ManagementReportCoverPage;
using Desktop.Lib.Pervasive;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Astrodon.Reports.LevyRoll
{
    public class ManagementReportCoverPage
    {
        public byte[] RunReport(DateTime dDate, string buildingName, List<TOCDataItem> tocDataItems)
        {
            return RunReportToPdf(dDate, buildingName, tocDataItems);
        }

        private byte[] RunReportToPdf(DateTime dDate, string building, List<TOCDataItem> tocDataItems)
        {
            string rdlcPath = "Astrodon.Reports.ManagementReportCoverPage.ManagementReportCoverPageReport.rdlc";
            byte[] report = null;

            Dictionary<string, IEnumerable> reportData = new Dictionary<string, IEnumerable>();
            Dictionary<string, string> reportParams = new Dictionary<string, string>();

            string period = dDate.ToString("MMM yyyy");

            reportParams.Add("BuildingName", building);
            reportParams.Add("Period", period);

            reportData.Add("dsTocItems", tocDataItems);

            using (RdlcHelper rdlcHelper = new RdlcHelper(rdlcPath,
                                                        reportData,
                                                        reportParams))
            {

                rdlcHelper.Report.EnableExternalImages = true;
                report = rdlcHelper.GetReportAsFile();
            }
            return report;
        }

        private string SetDataSource(string sqlQuery, string dataPath)
        {
            return PervasiveSqlUtilities.SetDataSource(sqlQuery, dataPath);
        }
    }
}