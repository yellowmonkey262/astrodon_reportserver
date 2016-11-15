using PastelReportServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SelfHosted
{
    class Program
    {
        static void Main(string[] args)
        {
            ReportServiceHost host = new ReportServiceHost();
            Console.ReadLine();
            host.Terminated = true;
        }
    }
}
