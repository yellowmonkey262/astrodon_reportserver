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
    public class LevyRollReport
    {
        public byte[] RunReport(DateTime processMonth, string buildingName, string dataPath)
        {
            DateTime dDate = new DateTime( processMonth.Year,processMonth.Month,1);

            string sqlPeriodConfig = PervasiveSqlUtilities.ReadResourceScript("Astrodon.Reports.Scripts.PeriodParameters.sql");
            sqlPeriodConfig = SetDataSource(sqlPeriodConfig,dataPath);
            var periodData = PervasiveSqlUtilities.FetchPervasiveData("", sqlPeriodConfig, null);
            PeriodDataItem periodItem = null;
            foreach (DataRow row in periodData.Rows)
            {
                periodItem = new PeriodDataItem(row);
                break;
            }
            int period = 0;
            try
            {
                period = periodItem.PeriodNumberLookup(dDate);
            }
            catch (Exception err)
            {
                throw err;
            }


            //run the main report query
            string sqlQuery = PervasiveSqlUtilities.ReadResourceScript("Astrodon.Reports.Scripts.LevyRollAllCustomers.sql");
            sqlQuery = SetDataSource(sqlQuery,dataPath);

            var allMasterAccounts = PervasiveSqlUtilities.FetchPervasiveData("", sqlQuery, null);

            sqlQuery = PervasiveSqlUtilities.ReadResourceScript("Astrodon.Reports.Scripts.LevyRoll.sql");
            sqlQuery = SetDataSource(sqlQuery, dataPath);

            var reportDB = PervasiveSqlUtilities.FetchPervasiveData("", sqlQuery, new OdbcParameter("@PPeriod", period));

            List<LevyRollDataItem> data = new List<LevyRollDataItem>();
            foreach (DataRow row in reportDB.Rows)
            {
                data.Add(new LevyRollDataItem(row, period));
            }

            foreach (DataRow row in allMasterAccounts.Rows)
            {
                var rowItem = new LevyRollDataItem(row, period);
                var currentCustomer = data.Where(a => a.CustomerCode == rowItem.CustomerCode).FirstOrDefault();
                if (currentCustomer == null)
                {
                    data.Add(rowItem);
                }
            }

            string sundriesQry = PervasiveSqlUtilities.ReadResourceScript("Astrodon.Reports.Scripts.Sundries.sql");
            sundriesQry = SetDataSource(sundriesQry,dataPath);
            reportDB = PervasiveSqlUtilities.FetchPervasiveData("", sundriesQry, new OdbcParameter("@PPeriod", period));
            List<SundryDataItem> sundries = new List<SundryDataItem>();
            foreach (DataRow row in reportDB.Rows)
            {
                sundries.Add(new SundryDataItem(row));
            }

           return RunReportToPdf(data, sundries,dDate,buildingName);
        }

        private byte[] RunReportToPdf(List<LevyRollDataItem> data, List<SundryDataItem> sundries, DateTime dDate, string building)
        {
            string rdlcPath = "Astrodon.Reports.LevyRoll.LevyRollReport.rdlc";
            byte[] report = null;

            Dictionary<string, IEnumerable> reportData = new Dictionary<string, IEnumerable>();
            Dictionary<string, string> reportParams = new Dictionary<string, string>();

            string period = dDate.ToString("MMM yyyy");

            reportParams.Add("BuildingName", building);
            reportParams.Add("Period", period);

            reportData.Add("LevyRollMain", data);
            reportData.Add("dsLevySundries", sundries);

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
            if (!Debugger.IsAttached)
                return sqlQuery.Replace("[DataSet].", "PAS11" + dataPath + ".");
            else
                return sqlQuery = sqlQuery.Replace("[DataSet].", "");
        }
    }
}