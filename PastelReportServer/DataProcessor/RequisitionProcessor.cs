using Astrodon.Data;
using Astrodon.DataContracts;
using Desktop.Lib.Pervasive;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Astrodon.DataProcessor
{
    public class RequisitionProcessor
    {
        private DataContext _context;
        private int _buildingId;

        private tblBuilding _building;

        public RequisitionProcessor(DataContext context, int buildingId)
        {
            _context = context;
            _buildingId = buildingId;
            _building = context.tblBuildings.Where(a => a.id == buildingId).Single();
        }

        private List<PaymentTransaction> FetchPaymentTransactions(bool isTrustAccount)
        {
            List<PaymentTransaction> result = new List<PaymentTransaction>();
            string dataPath = _building.DataPath;

            string sqlPaymentRecords = string.Empty;

            if (isTrustAccount)//use the trust account
            {
                dataPath = _context.tblSettings.First().trust;
                //accountList = "LinkAcc = '" + _building.AccNumber + "'";
                sqlPaymentRecords = PervasiveSqlUtilities.ReadResourceScript("Astrodon.DataProcessor.Scripts.TrustPaymentTransactionList.sql");
                sqlPaymentRecords = sqlPaymentRecords.Replace("[TRUSTACCOUNT]", _building.AccNumber);
            }
            else
            {
               sqlPaymentRecords = PervasiveSqlUtilities.ReadResourceScript("Astrodon.DataProcessor.Scripts.PaymentTransactionList.sql");
            }

            sqlPaymentRecords = PervasiveSqlUtilities.SetDataSource(sqlPaymentRecords, dataPath);

            foreach (DataRow row in PervasiveSqlUtilities.FetchPervasiveData(sqlPaymentRecords).Rows)
                result.Add(new PaymentTransaction(row, isTrustAccount, dataPath));

            return result;
        }

        public int LinkPayments()
        {
           

            int result = 0;
            //step 1 find all payments in pastel
            var pastelTransactions = FetchPaymentTransactions(false);
            var trustTransactions = FetchPaymentTransactions(true);

            if (pastelTransactions.Count <= 0 && trustTransactions.Count <= 0)
                return 0;

            if (pastelTransactions == null)
                pastelTransactions = new List<PaymentTransaction>();

            pastelTransactions.AddRange(trustTransactions);

            //load requisitions
            var minDate = pastelTransactions.Min(a => a.TransactionDate).Date;
            var maxDate = pastelTransactions.Max(a => a.TransactionDate).Date.AddDays(1).AddSeconds(-1);
            minDate = minDate.AddDays(-7);
            maxDate = maxDate.AddDays(7);

            var reqList = (from r in _context.tblRequisitions
                           where r.trnDate >= minDate && r.trnDate <= maxDate
                           && r.building == _buildingId
                           && r.processed == true
                           select r).ToList();

            //filter all items already matched

            //remove already matched transactions
            foreach (var req in reqList.Where(a => a.PaymentLedgerAutoNumber != null))
            {
                var matched = pastelTransactions.Where(a => a.AutoNumber == req.PaymentLedgerAutoNumber && a.DataPath == req.PaymentDataPath).SingleOrDefault();
                if (matched != null)
                    pastelTransactions.Remove(matched);
            }

            //remove matched requisitions
            reqList = reqList.Except(reqList.Where(a => a.PaymentLedgerAutoNumber != null)).ToList();

            //now match pastel with requisition payment

            foreach (var req in reqList)
            {
                var minD = req.trnDate.Date.AddDays(-5);
                var maxD = req.trnDate.Date.AddDays(2);

                var matched = pastelTransactions.Where(a => a.TransactionDate >= minD
                                                         && a.TransactionDate <= maxD
                                                         && Math.Abs(a.Amount) == Math.Abs(req.amount)
                                                         && a.AccountType == req.account) //OWN or TRUST
                                                         .OrderBy(a => a.TransactionDate - req.trnDate.Date).FirstOrDefault();

                if (matched != null)
                {
                    req.PaymentLedgerAutoNumber = matched.AutoNumber;
                    req.PaymentDataPath = matched.DataPath;
                    if (_buildingId == 364) //only for testing maintenance 
                    {
                        req.paid = true;
                    }
                    result++;
                    pastelTransactions.Remove(matched);
                }

            }
            _context.SaveChanges();

            return result;
        }
    }
}
