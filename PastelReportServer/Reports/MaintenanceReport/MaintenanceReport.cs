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
using Astrodon.Data.MaintenanceData;

namespace Astrodon.Reports.MaintenanceReport
{
    public class MaintenanceReport
    {
        private DataContext _dataContext;
        public MaintenanceReport(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public byte[] RunReport(MaintenanceReportType reportType, DateTime fromDate,DateTime toDate,int buildingId, string buildingName, string dataPath)
        {
            DateTime startDate = fromDate;
            DateTime endDate = toDate;

            //maintenance records
            var q = from m in _dataContext.MaintenanceSet
                                          .Include(c => c.BuildingMaintenanceConfiguration)
                                          .Include(d => d.Requisition)
                                          .Include(e => e.Requisition.Supplier)
                    where m.Requisition.trnDate >= startDate 
                       && m.Requisition.trnDate <= endDate
                       && m.Requisition.building == buildingId
                    //   && m.Requisition.paid == true
                    select new MaintenanceReportDataItem()
                    {
                        MaintenanceClassificationType = m.BuildingMaintenanceConfiguration.MaintenanceClassificationType,
                        Unit = m.IsForBodyCorporate ? "Body Corporate" : m.CustomerAccount,
                        MaintenanceType = m.BuildingMaintenanceConfiguration.Name,
                        PastelAccountNumber = m.BuildingMaintenanceConfiguration.PastelAccountNumber,
                        PastelAccountName = m.BuildingMaintenanceConfiguration.PastelAccountName,
                        MaintenanceDate = m.Requisition.trnDate,
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
                        Amount = m.Requisition.amount,
                        Paid = m.Requisition.paid ? "Y":"N"
                    };

            var reportData = q.ToList();  

            //now select all requisitions not already in the list

            var dbList = (from r in _dataContext.tblRequisitions
                     join m in _dataContext.MaintenanceSet on r.id equals m.RequisitionId into maint
                     from g in maint.DefaultIfEmpty()
                     where r.trnDate >= startDate
                           && r.trnDate <= endDate
                           && r.building == buildingId
                           && g == null
                     select r).ToList();

            //filter q2 to maintenance only ledger accounts
            //remove all non maintenance transactions
            List<UnlinkedRequisitions> reqList = new List<UnlinkedRequisitions>();
            foreach (var config in _dataContext.BuildingMaintenanceConfigurationSet.Where(a => a.BuildingId == buildingId).ToList())
            {
                foreach(var itm in dbList.Where(a => a.LedgerAccountNumber == config.PastelAccountNumber))
                {
                    reqList.Add(new UnlinkedRequisitions() { BuildingMaintenanceConfiguration = config, Requisition = itm });
                }
            }

            //union the first query with the second one

            reportData.AddRange(reqList.Select(r => new MaintenanceReportDataItem()
            {
                MaintenanceClassificationType = r.BuildingMaintenanceConfiguration.MaintenanceClassificationType,
                Unit = string.Empty,
                MaintenanceType = r.BuildingMaintenanceConfiguration.Name,
                PastelAccountNumber = r.BuildingMaintenanceConfiguration.PastelAccountNumber,
                PastelAccountName = r.BuildingMaintenanceConfiguration.PastelAccountName,
                MaintenanceDate = r.Requisition.trnDate,
                Description = string.Empty,
                Supplier = string.Empty,
                CompanyReg = string.Empty,
                VatNumber = string.Empty,
                ContactPerson = string.Empty,
                EmailAddress = string.Empty,
                ContactNumber = string.Empty,
                Bank = r.Requisition.BankName,
                Branch = r.Requisition.BranchName,
                BranchCode = r.Requisition.BranchCode,
                AccountNumber = r.Requisition.AccountNumber,
                InvoiceNumber = string.Empty,
                WarrantyDuration =null,
                WarrantyType =null,
                WarrantyNotes = string.Empty,
                WarrantyExpires = null,
                SerialNumber = string.Empty,
                Amount = r.Requisition.amount,
                Paid = r.Requisition.paid ? "Y" : "N"
            }));

            if (reportData.Count <= 0)
                return null;

            reportData = reportData.OrderBy(a => a.MaintenanceClassificationType).ThenBy(a => a.Unit).ThenBy(a => a.MaintenanceType).ThenBy(a => a.MaintenanceDate).ThenBy(a => a.Supplier).ToList();

            var accountNumbers = reportData.Select(a => a.PastelAccountNumber).Distinct().ToArray();
            var accountList = LoadAccountValues(startDate,endDate, dataPath, accountNumbers);

            //merge account data with account list
            string currentLedgerAccount = "";

            decimal balance = 0;
            foreach (var dataItem in reportData)
            {

                if (dataItem.PastelAccountNumber != currentLedgerAccount)
                {
                    balance = dataItem.Amount;
                    currentLedgerAccount = dataItem.PastelAccountNumber;
                }
                else
                    balance = balance + dataItem.Amount;

                var itm = accountList.FirstOrDefault(a => a.AccNumber == dataItem.PastelAccountNumber && a.PeriodMonth == new DateTime(dataItem.MaintenanceDate.Year,dataItem.MaintenanceDate.Month,1));
                if (itm != null)
                {
                    dataItem.Budget = itm.Budget;
                    dataItem.BudgetAvailable = itm.BudgetAvailable;
                }

                dataItem.Balance = balance;
            }

            return RunReportToPdf(reportData, startDate,endDate, buildingName, reportType == MaintenanceReportType.SummaryReport);
        }

        private List<PervasiveAccount> LoadAccountValues(DateTime startDate,DateTime endDate, string dataPath, string[] accountNumbers)
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
            var accountData = PervasiveSqlUtilities.FetchPervasiveData( qry, null);
            List<PervasiveAccount> accountList = new List<PervasiveAccount>();
            foreach (DataRow row in accountData.Rows)
            {
                var dt = startDate;
                while (dt <= endDate)
                {
                    accountList.Add(new PervasiveAccount(row, GetBuildingPeriod(dt, dataPath),dt));
                    dt = dt.AddMonths(1);
                }
            }

            return accountList;
        }

        PeriodDataItem _periodItem = null;
        private int GetBuildingPeriod(DateTime startDate, string dataPath)
        {
            DateTime dDate = new DateTime(startDate.Year, startDate.Month, 1);
            if (_periodItem == null)
            {
                string sqlPeriodConfig = PervasiveSqlUtilities.ReadResourceScript("Astrodon.Reports.Scripts.PeriodParameters.sql");
                sqlPeriodConfig = SetDataSource(sqlPeriodConfig, dataPath);
                var periodData = PervasiveSqlUtilities.FetchPervasiveData(sqlPeriodConfig, null);
                foreach (DataRow row in periodData.Rows)
                {
                    _periodItem = new PeriodDataItem(row);
                    break;
                }
            }
            int period = 0;
            try
            {
                period = _periodItem.PeriodNumberLookup(dDate);
            }
            catch (Exception err)
            {
                throw err;
            }
            return period;
        }

        private string SetDataSource(string sqlQuery, string dataPath)
        {
            return PervasiveSqlUtilities.SetDataSource(sqlQuery, dataPath);
        }


        private byte[] RunReportToPdf(List<MaintenanceReportDataItem> data, DateTime startDate, DateTime endDate, string buildingName, bool detailedReport)
        {
            string rdlcPath = "Astrodon.Reports.MaintenanceReport.MaintenanceReport.rdlc";
            byte[] report = null;

            Dictionary<string, IEnumerable> reportData = new Dictionary<string, IEnumerable>();
            Dictionary<string, string> reportParams = new Dictionary<string, string>();

            string period = startDate.ToString("MMM yyyy") + " - " + endDate.ToString("MMM yyyy");

            reportParams.Add("Period", period);
            reportParams.Add("BuildingName", buildingName);
            reportParams.Add("DetailedReport", detailedReport ? "true" : "false");

            reportData.Add("MaintenanceReport", data);

            using (RdlcHelper rdlcHelper = new RdlcHelper(rdlcPath, reportData, reportParams))
            {
                rdlcHelper.Report.EnableExternalImages = true;
                report = rdlcHelper.GetReportAsFile();
            }
            return report;
        }
    }

    class UnlinkedRequisitions
    {
        public tblRequisition Requisition { get; set; }
        public BuildingMaintenanceConfiguration BuildingMaintenanceConfiguration { get; set; }
    }
}
