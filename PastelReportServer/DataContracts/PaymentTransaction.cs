﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Astrodon.DataContracts
{
    public class PaymentTransaction : PervasiveItem
    {
        public PaymentTransaction()
        {

        }

        public PaymentTransaction(DataRow row, bool isTrustAccount, string dataPath)
        {
            AutoNumber = ReadInt(row, "AutoNumber");
            TransactionDate = ReadDate(row, "TransactionDate");
            LedgerAccount = ReadString(row, "LedgerAccount");
            Account = ReadString(row, "Account");
            AccountName = ReadString(row, "AccountName");
            LedgerAccountName = ReadString(row, "LedgerAccountName");
            Reference = ReadString(row, "Reference");
            Description = ReadString(row, "Description");
            Amount = ReadDecimal(row, "Amount");
            TrustAccount = isTrustAccount;
            DataPath = dataPath;
            AccountType = isTrustAccount ? "TRUST" : "OWN";
        }

        public int AutoNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public string LedgerAccount { get; set; }
        public string LedgerAccountName { get; set; }
        public string Account { get; set; }
        public string AccountName { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public bool TrustAccount { get; set; }
        public string AccountType { get; set; }
        public string DataPath { get; set; }
    }
}
