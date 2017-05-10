﻿using Astrodon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Astrodon.DataContracts.Maintenance;
using Desktop.Lib.Pervasive;
using System.Data;
using Astrodon.Data.MaintenanceData;
using System.Data.Entity;
using Astrodon.DataContracts;
using System.Globalization;

namespace Astrodon.DataProcessor
{
    public class MaintenanceProcessor
    {
        private DataContext _context;
        private int _buildingId;

        private tblBuilding _building;
        private List<BuildingMaintenanceConfiguration> _buildingConfig;

        public MaintenanceProcessor(DataContext context, int buildingId)
        {
            _context = context;
            _buildingId = buildingId;
            _building = context.tblBuildings.Where(a => a.id == buildingId).Single();
            _buildingConfig = context.BuildingMaintenanceConfigurationSet.Where(a => a.BuildingId == _buildingId).ToList();
        }

        public ICollection<PastelMaintenanceTransaction> MissingMaintenanceRecordsGet()
        {
            var result = new List<PastelMaintenanceTransaction>();
            if (_buildingConfig.Count <= 0) //nothing configured
                return result;

            //load pastel transaction list
            var pastelTransactions = FetchPastelMaintTransactions(false);

            if (pastelTransactions.Count <= 0)
                return result;

            if (pastelTransactions == null)
                pastelTransactions = new List<PastelMaintenanceTransaction>();

            //load requisitions
            var minDate = pastelTransactions.Min(a => a.TransactionDate).Date;
            var maxDate = pastelTransactions.Max(a => a.TransactionDate).Date.AddDays(1).AddSeconds(-1);

            var dbList = (from r in _context.tblRequisitions
                          where r.trnDate >= minDate && r.trnDate <= maxDate
                          && r.building == _buildingId
                          select r).ToList();

            //remove all non maintenance transactions
            List<tblRequisition> reqList = new List<tblRequisition>();
            foreach (var config in _buildingConfig)
            {
                reqList.AddRange(dbList.Where(a => a.LedgerAccountNumber == config.PastelAccountNumber));
            }

            //remove already matched transactions
            foreach (var req in reqList.Where(a => a.PastelLedgerAutoNumber != null))
            {
                var matched = pastelTransactions.Where(a => a.AutoNumber == req.PastelLedgerAutoNumber && a.DataPath == req.PastelDataPath).SingleOrDefault();
                if (matched != null)
                    pastelTransactions.Remove(matched);
            }

            //remove matched requisitions
            pastelTransactions = CalculateMatches(pastelTransactions, reqList);
           


            return pastelTransactions.OrderBy(a => a.TransactionDate).ToList();
        }

        private List<PastelMaintenanceTransaction> FetchPastelMaintTransactions(bool isTrustAccount)
        {
            List<PastelMaintenanceTransaction> result = new List<PastelMaintenanceTransaction>();

            string accountList = string.Empty;

            foreach (var config in _buildingConfig)
            {
                if (!string.IsNullOrWhiteSpace(accountList))
                    accountList = accountList + " or LinkAcc = '" + config.PastelAccountNumber + "'";
                else
                    accountList = "LinkAcc = '" + config.PastelAccountNumber + "'";
            }

            string dataPath = _building.DataPath;

            if (isTrustAccount)//use the trust account
            {
                dataPath = _context.tblSettings.First().trust;
                accountList = "LinkAcc = '" + _building.AccNumber + "'";
            }

            string sqlMaintenanceRecords = PervasiveSqlUtilities.ReadResourceScript("Astrodon.DataProcessor.Scripts.MaintenanceRecordList.sql");
            sqlMaintenanceRecords = PervasiveSqlUtilities.SetDataSource(sqlMaintenanceRecords, dataPath);
            sqlMaintenanceRecords = sqlMaintenanceRecords.Replace("[AccountList]", accountList);

            foreach (DataRow row in PervasiveSqlUtilities.FetchPervasiveData(sqlMaintenanceRecords).Rows)
                result.Add(new PastelMaintenanceTransaction(row,isTrustAccount, dataPath));

            return result;
        }

      
        public List<PastelMaintenanceTransaction> FetchAndLinkMaintenanceTransactions(DateTime fromDate, DateTime toDate)
        {
            List<PastelMaintenanceTransaction> pastelTransactions = new List<PastelMaintenanceTransaction>();

            string accountList = string.Empty;

            foreach (var config in _buildingConfig)
            {
                if (!string.IsNullOrWhiteSpace(accountList))
                    accountList = accountList + " or LinkAcc = '" + config.PastelAccountNumber + "'";
                else
                    accountList = "LinkAcc = '" + config.PastelAccountNumber + "'";
            }

            string dataPath = _building.DataPath;

            string sqlMaintenanceRecords = PervasiveSqlUtilities.ReadResourceScript("Astrodon.DataProcessor.Scripts.MaintenanceRecordListBetweenDates.sql");
            sqlMaintenanceRecords = PervasiveSqlUtilities.SetDataSource(sqlMaintenanceRecords, dataPath);
            sqlMaintenanceRecords = sqlMaintenanceRecords.Replace("[AccountList]", accountList);
            sqlMaintenanceRecords = sqlMaintenanceRecords.Replace("[FromDate]", "'" + fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'");
            sqlMaintenanceRecords = sqlMaintenanceRecords.Replace("[ToDate]", "'" + toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'");

            foreach (DataRow row in PervasiveSqlUtilities.FetchPervasiveData(sqlMaintenanceRecords).Rows)
                pastelTransactions.Add(new PastelMaintenanceTransaction(row, false, dataPath));


            var minDate = pastelTransactions.Min(a => a.TransactionDate).Date.AddDays(-7);
            var maxDate = pastelTransactions.Max(a => a.TransactionDate).Date.AddDays(8).AddSeconds(-1);

            var reqList = (from r in _context.tblRequisitions
                           where r.trnDate >= minDate && r.trnDate <= maxDate
                           && r.building == _buildingId
                           select r).ToList();


            //match requisition transactions to pastel transactions and update changes
            return CalculateMatches(pastelTransactions, reqList);
        }

        private List<PastelMaintenanceTransaction> CalculateMatches(List<PastelMaintenanceTransaction> pastelTransactions, List<tblRequisition> reqList)
        {
            reqList = reqList.Except(reqList.Where(a => a.PastelLedgerAutoNumber != null)).ToList();
            foreach (var req in reqList.Where(a => a.PastelLedgerAutoNumber == null))
            {
                var matched = pastelTransactions.Where(a => a.LedgerAccount == req.LedgerAccountNumber
                                                         && Math.Abs(a.Amount) == Math.Abs(req.amount))
                                                         .OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();
                var potential = matched.FirstOrDefault();
                //find items where reference number is in here
                matched = matched.Where(a => req.payreference.Contains(a.Reference)
                                           || a.Reference.Contains(req.payreference)).OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();
                if (matched.Count > 0)
                    potential = matched.FirstOrDefault();

                if (potential != null)
                {
                    req.PastelLedgerAutoNumber = potential.AutoNumber;
                    req.PastelDataPath = potential.DataPath;
                    pastelTransactions.Remove(potential);
                }
            }
            _context.SaveChanges();

            return pastelTransactions;
        }
    }
}
