using Astrodon.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Astrodon.DataProcessor
{
    public class ProcessorThread
    {
        private static String connStringDefault = "Data Source=SERVER2\\SQLEXPRESS;Initial Catalog=Astrodon;Persist Security Info=True;User ID=sa;Password=sa"; //Astrodon
        private static String connStringL = "Data Source=STEPHEN-PC\\MTDNDSQL;Initial Catalog=Astrodon;Persist Security Info=True;User ID=sa;Password=m3t@p@$$"; //Local
        private static String connStringD = "Data Source=DEVELOPERPC\\SQLEXPRESS;Initial Catalog=Astrodon;Persist Security Info=True;User ID=sa;Password=$DEVELOPER$"; //Astrodon
        private static String connStringLocal = "Data Source=.;Initial Catalog=Astrodon;Persist Security Info=True;User ID=sa;Password=1q2w#E$R"; //LamaDev

        private DateTime _NextRun = DateTime.Now.AddMinutes(1);

        public ProcessorThread()
        {
            _NextRun = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 22, 0, 0);
            if (System.Diagnostics.Debugger.IsAttached)
                _NextRun = DateTime.Now.AddSeconds(5);

            Terminated = false;
            new Thread(Run).Start();
        }

        public bool Terminated { get; set; }


        public void Run()
        {
            while (!Terminated)
            {
                try
                {
                    if (DateTime.Now > _NextRun)
                    {
                        _NextRun = _NextRun.AddDays(1);
                        ProcessBuildingMatches();
                        DataContext.HouseKeepSystemLog();
                    }

                }
                catch (Exception e)
                {
                    LogException(e);
                }

                Thread.Sleep(1000);
            }
        }



        private void LogException(Exception e)
        {
            using (var context = new DataContext(GetConnectionString()))
            {
                context.SystemLogSet.Add(new Data.Log.SystemLog()
                {
                    EventTime = DateTime.Now,
                    Message = e.Message,
                    StackTrace = e.StackTrace
                });
            }
        }


        private static string GetConnectionString()
        {
            if (Environment.MachineName == "STEPHEN-PC")
            {
                return connStringL;
            }
            else if (Environment.MachineName == "DEVELOPERPC")
            {
                return connStringD;
            }
            else if (Environment.MachineName == "PASTELPARTNER")
                return connStringLocal;
            return connStringDefault;
        }



        private void ProcessBuildingMatches()
        {
            List<int> buildingList;
            using (var context = new DataContext(GetConnectionString()))
            {
                buildingList = context.tblBuildings.Select(a => a.id).ToList();
            }

            foreach (var buildingId in buildingList)
            {
                if (Terminated)
                    return;

                try
                {
                    using (var context = new DataContext(GetConnectionString()))
                    {
                        context.SystemLogSet.Add(new Data.Log.SystemLog()
                        {
                            EventTime = DateTime.Now,
                            Message = "Processing Building " + buildingId.ToString()
                        });
                        context.SaveChanges();
                        if (Debugger.IsAttached)
                            Console.WriteLine("Processing Building " + buildingId.ToString());

                        var processor = new RequisitionProcessor(context, buildingId);
                        var linked = processor.LinkPayments();

                        context.SystemLogSet.Add(new Data.Log.SystemLog()
                        {
                            EventTime = DateTime.Now,
                            Message = "Linked  " + linked.ToString() + " payments for " + buildingId.ToString()
                        });
                        if (Debugger.IsAttached)
                            Console.WriteLine("Linked  " + linked.ToString() + " payments for " + buildingId.ToString());
                        context.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }
        }

    }
}
