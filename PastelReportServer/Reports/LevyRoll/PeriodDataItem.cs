using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Astrodon.Reports.LevyRoll
{
    public class PeriodDataItem
    {

        private List<PeriodItem> _ItemList;
           
        public PeriodDataItem(DataRow row)
        {
            int maxPeriodThis = ReadInt(row["NumberPeriodsThis"],12);
            int maxPeriodLast = ReadInt(row["NumberPeriodsLast"], 12);
            _ItemList = new List<PeriodItem>();
            //Period this
            for (int x=1; x<= 13; x++)
            {
                var itm = new PeriodItem();

                if (x <= maxPeriodThis)
                {
                    itm.PeriodNumber = 100 + x;
                    itm.Start = ReadDate(row["PerStartThis" + x.ToString().PadLeft(2, '0')]);
                    itm.End = ReadDate(row["PerEndThis" + x.ToString().PadLeft(2, '0')]);
                    _ItemList.Add(itm);
                }

                if (x <= maxPeriodLast)
                {
                    itm = new PeriodItem();
                    itm.PeriodNumber = x;
                    itm.Start = ReadDate(row["PerStartLast" + x.ToString().PadLeft(2, '0')]);
                    itm.End = ReadDate(row["PerEndLast" + x.ToString().PadLeft(2, '0')]);
                    _ItemList.Add(itm);
                }

            }
        }

        private int ReadInt(object cell, int def)
        {
            try
            {
                return (int)cell;
            }
            catch
            {
                return def;
            }
        }

        private DateTime? ReadDate(object value)
        {
            if (value == null || value is DBNull)
                return null;
            return (DateTime)value;
        }

        public int PeriodNumberLookup(DateTime date)
        {
            var x = _ItemList.Where(a => a.Start == date).FirstOrDefault();
            if (x == null)
                throw new Exception("No period found in Ledger Parameters for " + date.ToString("yyyyMMdd"));
            return x.PeriodNumber;
        }
    }

    public class PeriodItem
    {
        public int PeriodNumber { get; set; }

        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }
}
