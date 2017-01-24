﻿using Astrodon;
using Astrodon.Reports.LevyRoll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Astrodon.DataContracts;
using Astrodon.Data;
using Astrodon.Reports.MaintenanceReport;
using Astrodon.Reports.SupplierReport;

namespace PastelDataService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ReportService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select ReportService.svc or ReportService.svc.cs at the Solution Explorer and start debugging.
    public class ReportService : IReportService
    {
        public byte[] LevyRollReport(DateTime processMonth, string buildingName, string dataPath)
        {
            var lr = new LevyRollReport();
            return lr.RunReport(processMonth, buildingName, dataPath);
        }

        public byte[] MaintenanceReport(string sqlConnectionString, MaintenanceReportType reportType, DateTime processMonth, int buildingId, string buildingName, string dataPath)
        {
            using (var dc = new DataContext(sqlConnectionString))
            {
                var rp = new MaintenanceReport(dc);

                return rp.RunReport(reportType, processMonth,buildingId, buildingName, dataPath);
                
            }
        }


        public byte[] SupplierReport(string sqlConnectionString, DateTime processMonth)
        {
            using (var dc = new DataContext(sqlConnectionString))
            {
                var rp = new SupplierReport(dc);

                return rp.RunReport(processMonth);
            }
        }
    }
}
