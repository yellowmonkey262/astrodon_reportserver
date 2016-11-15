using Astrodon;
using Astrodon.Reports.LevyRoll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;

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
    }
}
