using Astrodon.DataContracts;
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
        byte[] LevyRollReport(DateTime processMonth, string buildingName, string dataPath);

        [OperationContract]
        byte[] SupplierReport(string sqlConnectionString, DateTime processMonth);

        [OperationContract]
        byte[] MaintenanceReport(string sqlConnectionString, MaintenanceReportType reportType, DateTime processMonth, int buildingId, string buildingName, string dataPath);

    }
}
