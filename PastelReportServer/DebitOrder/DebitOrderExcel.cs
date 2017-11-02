using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Astrodon.Data;
using Astrodon.Data.DebitOrder;
using Astrodon.Reports.LevyRoll;
using OfficeOpenXml;
using System.Globalization;
using System.IO;

namespace Astrodon.DebitOrder
{
    public class DebitOrderExcel
    {
        private DataContext _DataContext;

        public DebitOrderExcel(DataContext dataContext)
        {
            this._DataContext = dataContext;
        }

        public List<DebitOrderItem> RunDebitOrderForBuilding(int buildingId, DateTime processMonth, bool showFeeBreakdown)
        {
            int period;
            processMonth = new DateTime(processMonth.Year, processMonth.Month, 1);
            var query = _DataContext.CustomerDebitOrderSet
                                    .Where(a => a.BuildingId == buildingId)
                                    .Select(b => new DebitOrderItem()
                                    {
                                        BuildingId = b.BuildingId,
                                        CustomerCode = b.CustomerCode,
                                        BranchCode = b.BranceCode,
                                        AccountTypeId = b.AccountType,
                                        AccountNumber = b.AccountNumber,
                                        DebitOrderCollectionDay = b.DebitOrderCollectionDay,
                                        DebitOrderFeeDisabled = b.IsDebitOrderFeeDisabled //disabled on unit level
                                    });

            var debitOrderItems = query.ToList();


            DateTime collectionDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
            var building = _DataContext.tblBuildings.Single(a => a.id == buildingId);
            var buildingSettings = _DataContext.tblBuildingSettings.SingleOrDefault(a => a.buildingID == buildingId);
            var levyRollData = new List<LevyRollDataItem>();

            if (debitOrderItems.Count > 0)
            {
                var customers = debitOrderItems.Select(a => a.CustomerCode).Distinct().ToList();
                levyRollData = new LevyRollReport().LoadReportData(processMonth, building.DataPath, customers, out period);
            }

            decimal debitOrderFee = 0;
            if (buildingSettings != null)
            {
                debitOrderFee = buildingSettings.DebitOrderFee;
                if (building.IsDebitOrderFeeDisabled)
                    debitOrderFee = 0; //disabled on building level
            }
            foreach (var item in debitOrderItems)
            {
                item.DebitOrderFee = debitOrderFee;
                item.IsDebitOrderFeeDisabledOnBuilding = building.IsDebitOrderFeeDisabled;
                var levyRollItem = levyRollData.SingleOrDefault(a => a.CustomerCode.Trim().ToLower() == item.CustomerCode.Trim().ToLower());
                if (levyRollItem != null)
                {
                    item.AmountDue = levyRollItem.Due;
                    item.CustomerName = levyRollItem.CustomerDesc;
                }
                if (item.DebitOrderCollectionDay == DebitOrderDayType.One)
                    item.CollectionDay = collectionDay;
                else
                    item.CollectionDay = new DateTime(collectionDay.Year, collectionDay.Month, 15);
            }

            return debitOrderItems.Where(a => a.AmountDue > 0).ToList();
        }

        public byte[] RunDebitOrder(List<DebitOrderItem> debitOrderItems, bool showFeeBreakdown)
        {
            byte[] result = null;
            using (var memStream = new MemoryStream())
            {
                using (ExcelPackage excelPkg = new ExcelPackage())
                {

                    using (ExcelWorksheet wsSheet1 = excelPkg.Workbook.Worksheets.Add("Debtors"))
                    {

                        wsSheet1.Cells["A1"].Value = "SUPPLIER ID";
                        wsSheet1.Cells["B1"].Value = "REFERENCE";
                        wsSheet1.Cells["C1"].Value = "SUPPLIER NAME";
                        wsSheet1.Cells["D1"].Value = "HOLNES";
                        wsSheet1.Cells["E1"].Value = "ACCOUNT NAME";
                        wsSheet1.Cells["F1"].Value = "DESCRIPTION";
                        wsSheet1.Cells["G1"].Value = "BRANCH CODE";
                        wsSheet1.Cells["H1"].Value = "ACCOUNT TYPE";
                        wsSheet1.Cells["I1"].Value = "ACCOUNT NO";
                        wsSheet1.Cells["J1"].Value = "COLLECTION DAY";
                        wsSheet1.Cells["K1"].Value = "COLLECTION AMOUNT";
                        if (showFeeBreakdown)
                        {
                            wsSheet1.Cells["L1"].Value = "DEBIT ORDER FEE";
                            wsSheet1.Cells["M1"].Value = "AMOUNT DUE";
                            wsSheet1.Cells["N1"].Value = "COMMENT";

                        }
                        int rowNum = 1;
                        foreach (var row in debitOrderItems.Where(a => a.AmountDue > 0).OrderBy(a => a.BuildingId).ThenBy(a => a.CustomerCode))
                        {
                            rowNum++;
                            wsSheet1.Cells["A" + rowNum.ToString()].Value = "";
                            wsSheet1.Cells["B" + rowNum.ToString()].Value = row.Reference;
                            wsSheet1.Cells["C" + rowNum.ToString()].Value = row.SupplierName;
                            wsSheet1.Cells["D" + rowNum.ToString()].Value = row.Holnes;
                            wsSheet1.Cells["E" + rowNum.ToString()].Value = row.CustomerName;
                            wsSheet1.Cells["F" + rowNum.ToString()].Value = row.Description;
                            wsSheet1.Cells["G" + rowNum.ToString()].Value = row.BranchCode;
                            wsSheet1.Cells["H" + rowNum.ToString()].Value = row.AccountType;
                            wsSheet1.Cells["I" + rowNum.ToString()].Value = row.AccountNumber;
                            wsSheet1.Cells["J" + rowNum.ToString()].Value = row.CollectionDay.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

                            wsSheet1.Cells["K" + rowNum.ToString()].Value = row.CollectionAmount.ToString("###,##0.00", CultureInfo.InvariantCulture);

                            if (showFeeBreakdown)
                            {
                                wsSheet1.Cells["L" + rowNum.ToString()].Value = row.DebitOrderFee.ToString("###,##0.00", CultureInfo.InvariantCulture);
                                wsSheet1.Cells["M" + rowNum.ToString()].Value = row.AmountDue.ToString("###,##0.00", CultureInfo.InvariantCulture);
                                string debitOrderComment = "";
                                if (row.IsDebitOrderFeeDisabledOnBuilding)
                                {
                                    if (row.DebitOrderFeeDisabled)
                                        debitOrderComment = "Fee disabled on building and unit.";
                                    else
                                        debitOrderComment = "Fee disabled on building.";
                                }
                                else
                                {
                                    if (row.DebitOrderFeeDisabled)
                                        debitOrderComment = "Fee disabled on unit.";
                                }

                                wsSheet1.Cells["N" + rowNum.ToString()].Value = debitOrderComment;
                            }
                        }

                        wsSheet1.Protection.IsProtected = false;
                        wsSheet1.Protection.AllowSelectLockedCells = false;
                        wsSheet1.Cells.AutoFitColumns();

                        excelPkg.SaveAs(memStream);
                        memStream.Flush();
                        result = memStream.ToArray();
                    }
                }
            }
            return result;
        }
    }
}
