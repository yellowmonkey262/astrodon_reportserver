using Astrodon.DataContracts;
using Astrodon.DataContracts.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace PastelDataService
{
    [ServiceContract]
    public interface IReportService
    {
        [OperationContract]
        byte[] LevyRollReport(DateTime processMonth, string buildingName, string dataPath, bool includeSundries);

        [OperationContract]
        byte[] SupplierReport(string sqlConnectionString, DateTime fromDate, DateTime toDate, int buildingId);

        [OperationContract]
        byte[] MaintenanceReport(string sqlConnectionString, MaintenanceReportType reportType, DateTime processMonth, int buildingId, string buildingName, string dataPath);

        [OperationContract]
        ICollection<PastelMaintenanceTransaction> MissingMaintenanceRecordsGet(string sqlConnectionString, int buildingId);

    }
}
