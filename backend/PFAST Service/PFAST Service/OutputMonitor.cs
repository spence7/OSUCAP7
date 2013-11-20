using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;


namespace PFAST_Service
{
    class OutputMonitor
    {
        public bool MonitorForOutput(string outputfile)
        {
        //FileSystemWatcher outputFileWatcher = new FileSystemWatcher(); 

        //outputFileWatcher.Path = "C:\\PFast\\PFast_Results";
        //outputFileWatcher.Filter = outputfile;
        //outputFileWatcher.Created += new FileSystemEventHandler(OnCreate);
        //outputFileWatcher.EnableRaisingEvents = true;

        //Logging
            System.Threading.Thread.Sleep(15000);
            DatabaseConnection log = new DatabaseConnection();

            log.WriteToLogFile("Looking for file at location C:\\PFast\\PFast_Result\\" + outputfile);
        for (int i = 0; i < 30; i++)
        {
            log.WriteToLogFile("Searching for output file " + i + "th time");

            if (File.Exists("C:\\PFast\\PFast_Result\\" + outputfile))
            {
                log.WriteToLogFile("Found output file");    
                return true;
            }
             System.Threading.Thread.Sleep(1000);
        }
            return false;
        }

       
          
        //void OnCreate(object sender, FileSystemEventArgs e)
        //{
        //    // output file detected
        //    Console.WriteLine("A new *.txt file has been created!");
        //}

   }
}
