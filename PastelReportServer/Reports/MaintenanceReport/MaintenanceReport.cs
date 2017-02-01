using Astrodon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Astrodon.DataContracts;
using System.Data.Entity;
using Desktop.Lib.Pervasive;
using Astrodon.Reports.LevyRoll;
using System.Data;
using System.Diagnostics;
using System.Collections;

namespace Astrodon.Reports.MaintenanceReport
{
    public class MaintenanceReport
    {
        private DataContext _dataContext;
        public MaintenanceReport(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public byte[] RunReport(MaintenanceReportType reportType, DateTime processMonth,int buildingId, string buildingName, string dataPath)
        {
            DateTime startDate = new DateTime(processMonth.Year, processMonth.Month, 1);
            DateTime endDate = startDate.AddMonths(1).AddSeconds(-1);

            var q = from m in _dataContext.MaintenanceSet
                                          .Include(c => c.BuildingMaintenanceConfiguration)
                                          .Include(d => d.Requisition)
                                          .Include(e => e.Requisition.Supplier)
                    where m.DateLogged >= startDate && m.DateLogged <= endDate
                    select new MaintenanceReportDataItem()
                    {
                        MaintenanceClassificationType = m.BuildingMaintenanceConfiguration.MaintenanceClassificationType,
                        Unit = m.IsForBodyCorporate ? "Body Corporate" : m.CustomerAccount,
                        MaintenanceType = m.BuildingMaintenanceConfiguration.Name,
                        PastelAccountNumber = m.BuildingMaintenanceConfiguration.PastelAccountNumber,
                        PastelAccountName = m.BuildingMaintenanceConfiguration.PastelAccountName,
                        MaintenanceDate = m.DateLogged,
                        Description = m.Description,
                        Supplier = m.Requisition.Supplier.CompanyName,
                        CompanyReg = m.Requisition.Supplier.CompanyRegistration,
                        VatNumber = m.Requisition.Supplier.VATNumber,
                        ContactPerson = m.Requisition.Supplier.ContactPerson,
                        EmailAddress = m.Requisition.Supplier.EmailAddress,
                        ContactNumber = m.Requisition.Supplier.ContactNumber,
                        Bank = m.Requisition.BankName,
                        Branch = m.Requisition.BranchName,
                        BranchCode = m.Requisition.BranchCode,
                        AccountNumber = m.Requisition.AccountNumber,
                        InvoiceNumber = m.InvoiceNumber,
                        WarrantyDuration = m.WarrantyDuration,
                        WarrantyType = m.WarrantyDurationType,
                        WarrantyNotes = m.WarrantyNotes,
                        WarrantyExpires = m.WarrentyExpires,
                        SerialNumber = m.WarrantySerialNumber,
                        Amount = m.TotalAmount
                    };

            var reportData = q.OrderBy(a => a.MaintenanceClassificationType).ThenBy(a => a.Unit).ThenBy(a => a.MaintenanceType).ThenBy(a => a.Supplier).ToList();

            if (reportData.Count <= 0)
                return null;

            var accountNumbers = reportData.Select(a => a.PastelAccountNumber).Distinct().ToArray();
            var periodNumber = GetBuildingPeriod(processMonth, dataPath);
            var accountList = LoadAccountValues(periodNumber,dataPath, accountNumbers);

            //merge account data with account list

            foreach(var dataItem in reportData)
                dataItem.LoadBudget(accountList);

            return RunReportToPdf(reportData, startDate, buildingName, reportType == MaintenanceReportType.SummaryReport);
        }

        private List<PervasiveAccount> LoadAccountValues(int periodNumber, string dataPath, string[] accountNumbers)
        {
            string accQry = "";


            if (accountNumbers.Length == 1)
                accQry = " = '" + accountNumbers[0] + "'";
            else
            {
                accQry = " in (";
                foreach(var s in accountNumbers)
                {
                    if(accQry.EndsWith("("))
                      accQry = accQry + "'" + s + "'";
                    else
                        accQry = accQry + ",'" + s + "'";
                }

                accQry = accQry + ")";
            }

            string qry = "select * from [DataSet].LedgerMaster where AccNumber " + accQry;

            qry = SetDataSource(qry, dataPath);
            var accountData = PervasiveSqlUtilities.FetchPervasiveData("", qry, null);
            List<PervasiveAccount> accountList = new List<PervasiveAccount>();
            foreach (DataRow row in accountData.Rows)
            {
                accountList.Add(new PervasiveAccount(row, periodNumber));
            }

            return accountList;
        }

        private int GetBuildingPeriod(DateTime processMonth, string dataPath)
        {
            DateTime dDate = new DateTime(processMonth.Year, processMonth.Month, 1);

            string sqlPeriodConfig = PervasiveSqlUtilities.ReadResourceScript("Astrodon.Reports.Scripts.PeriodParameters.sql");
            sqlPeriodConfig = SetDataSource(sqlPeriodConfig, dataPath);
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
            return period;
        }

        private string SetDataSource(string sqlQuery, string dataPath)
        {
            if (!Debugger.IsAttached)
                return sqlQuery.Replace("[DataSet].", "PAS11" + dataPath + ".");
            else
                return sqlQuery = sqlQuery.Replace("[DataSet].", "");
        }


        private byte[] RunReportToPdf(List<MaintenanceReportDataItem> data, DateTime dDate, string buildingName, bool detailedReport)
        {
            string rdlcPath = "Astrodon.Reports.MaintenanceReport.MaintenanceReport.rdlc";
            byte[] report = null;

            Dictionary<string, IEnumerable> reportData = new Dictionary<string, IEnumerable>();
            Dictionary<string, string> reportParams = new Dictionary<string, string>();

            string period = dDate.ToString("MMM yyyy");

            reportParams.Add("Period", period);
            reportParams.Add("BuildingName", buildingName);
            reportParams.Add("DetailedReport", detailedReport ? "true" : "false");

            reportData.Add("MaintenanceReport", data);

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
