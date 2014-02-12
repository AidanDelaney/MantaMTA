using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.ServiceProcess;

namespace MantaMtaReset
{
    class Program
    {
        private const string MANTA_SERVICE_NAME = "MantaMTA";
        private const string MANTA_SERVICE_PROCESS_NAME = "MantaService";

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // Get the service.
            ServiceController mantaService = new ServiceController(MANTA_SERVICE_NAME);

            try
            {
                if (mantaService.Status == ServiceControllerStatus.Stopped)
                {
                    WriteToLog("Manta Service already stopped.");
                }
                else
                {
                    WriteToLog("Attempting to stop the Manta Service.");
                    mantaService.Stop();
                    mantaService.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    WriteToLog("Stopped the Manta Service.");
                }
            }
            catch(Exception ex)
            {
                if(ex is System.ServiceProcess.TimeoutException)
                {
                    WriteToLog("Failed to stop the Manta Service; will attempt to kill the process.");
                    
                    // Get the Manta Process.
                    Process[] proc = Process.GetProcessesByName(MANTA_SERVICE_PROCESS_NAME);
                    if(proc.Length < 1)
                    {
                        WriteToLog("Cannot find the Manta Process!");
                        return;
                    }

                    // Kill the MantaMTA process.
                    proc[0].Kill();

                    WriteToLog("Killed the Manta Process; Cleaning database.");

                    using(SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
                    {
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = @"
BEGIN TRANSACTION

DECLARE @tmp table(mta_msg_id uniqueidentifier)

INSERT INTO @tmp
SELECT queue.mta_msg_id
FROM man_mta_queue as [queue]
WHERE [queue].mta_queue_isPickupLocked = 1
	AND [queue].mta_msg_id IN (SELECT [tran].mta_msg_id FROM man_mta_transaction as [tran] WHERE [tran].mta_msg_id = [queue].mta_msg_id AND [tran].mta_transactionStatus_id = 4)

DELETE
FROM man_mta_queue
WHERE mta_msg_id IN (SELECT * FROM @tmp)

UPDATE man_mta_queue
SET mta_queue_isPickupLocked = 0
WHERE mta_queue_isPickupLocked = 1

COMMIT TRANSACTION
";
                        cmd.CommandTimeout = 5 * 60;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    WriteToLog("Manta database cleaned.");
                }
                else
                {
                    WriteToLog(ex.Message + Environment.NewLine + ex.StackTrace);
                    return;
                }
            }


            try
            {
                WriteToLog("Starting the Manta Service.");
                mantaService.Start();
                mantaService.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                WriteToLog("Manta Service running.");
            }
            catch(Exception)
            {
                try
                {
                    mantaService.Start();
                    mantaService.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    WriteToLog("Manta Service running.");
                }
                catch (Exception)
                {
                    WriteToLog("Failed to start the Manta Service!!!");
                }
            }

            //System.Diagnostics.Trace.WriteLine("Press any key to continue.");
            //Console.ReadKey(true);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteToLog("Unhandled exception: " + (e.ExceptionObject as Exception).Message + Environment.NewLine + (e.ExceptionObject as Exception).StackTrace);
        }

        private static void WriteToLog(string message)
        {
            System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString() + " " + message);
            //System.Diagnostics.Trace.WriteLine("Cannot Continue; Press any key to exit.");
            //Console.ReadKey();
        }
    }
}
