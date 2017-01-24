using Astrodon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Astrodon.DataContracts;

namespace Astrodon.Reports.MaintenanceReport
{
    public class MaintenanceReport
    {
        public MaintenanceReport(DataContext dataContext)
        {

        }

        internal byte[] RunReport(MaintenanceReportType reportType, DateTime processMonth, string buildingName, string dataPath)
        {
            throw new NotImplementedException();
        }
    }
}
