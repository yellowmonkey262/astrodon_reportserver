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

        private List<PaymentTransaction> FetchPaymentTransactions()
        {
            List<PaymentTransaction> result = new List<PaymentTransaction>();
            string dataPath = _building.DataPath;

            string sqlPaymentRecords = string.Empty;


            sqlPaymentRecords = PervasiveSqlUtilities.ReadResourceScript("Astrodon.DataProcessor.Scripts.PaymentTransactionList.sql");

            sqlPaymentRecords = PervasiveSqlUtilities.SetDataSource(sqlPaymentRecords, dataPath);

            foreach (DataRow row in PervasiveSqlUtilities.FetchPervasiveData(sqlPaymentRecords).Rows)
                result.Add(new PaymentTransaction(row, dataPath));

            return result;
        }

        public int LinkPayments()
        {


            //step 1 find all payments in pastel
            var pastelTransactions = FetchPaymentTransactions();

            if (pastelTransactions == null)
                pastelTransactions = new List<PaymentTransaction>();

            if (pastelTransactions.Count <= 0)
                return 0;


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

            return CalculateMatches(pastelTransactions, reqList);

        }

        private int CalculateMatches(List<PaymentTransaction> pastelTransactions, List<tblRequisition> reqList)
        {
            int result = 0;
            reqList = reqList.Except(reqList.Where(a => a.PaymentLedgerAutoNumber != null)).ToList(); //remove already linked items

            foreach (var req in reqList.Where(a => a.PaymentLedgerAutoNumber == null))
            {
                var matched = pastelTransactions.Where(a => a.LedgerAccount == req.LedgerAccountNumber //match the requisition to the account, payments are matched to the LinkAccount
                                                         && Math.Abs(a.Amount) == Math.Abs(req.amount))
                                                         .OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();

                var potential = matched.FirstOrDefault(); //just amount

                //date and same reference
                matched = matched.Where(a => req.payreference == a.Reference && a.TransactionDate == req.trnDate)
                                 .OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();
                if (matched.Count > 0)
                {
                    potential = matched.FirstOrDefault();
                }
                else
                {
                    //date
                    matched = matched.Where(a => a.TransactionDate == req.trnDate)
                                 .OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();
                    if (matched.Count > 0)
                    {
                        potential = matched.FirstOrDefault();
                    }
                    else
                    {
                        //reference
                        matched = matched.Where(a => req.payreference == a.Reference)
                              .OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();
                        if (matched.Count > 0)
                        {
                            potential = matched.FirstOrDefault();
                        }
                        else
                        {
                            //reference like
                            matched = matched.Where(a => req.payreference.Contains(a.Reference) || a.Reference.Contains(req.payreference))
                                      .OrderByDescending(a => Math.Abs(DateTime.Compare(a.TransactionDate, req.trnDate))).ToList();
                            if (matched.Count > 0)
                                potential = matched.FirstOrDefault();
                        }
                    }
                }


                if (potential != null)
                {
                    req.PaymentLedgerAutoNumber = potential.AutoNumber;
                    req.PaymentDataPath = potential.DataPath;
                    req.paid = true;
                    result++;
                    pastelTransactions.Remove(potential);
                }
            }
            _context.SaveChanges();

            return result;
        }
    }
}
