using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MySql.Data.MySqlClient;


namespace PFAST_Service
{
    public partial class PFASTService : ServiceBase
    {
        Timer timer = new Timer();
        DatabaseConnection dbc = new DatabaseConnection();

        public PFASTService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 10000;
            timer.Enabled = true;

        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {

            //stoping the timer temporarily while the query is done
            timer.Enabled = false;

            //query job input table for work
            dbc.QueryForNewJobs();

            //re-enable the timer
            timer.Enabled = true;
        }
        

        protected override void OnStop()
        {
            System.Console.WriteLine("Service Terminating...");
        }

        public void WriteToLogFile()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Public\\Test.txt", true))
            {
                file.WriteLine("{0}  - Service Loop", System.DateTime.Now);
            }

        }

    }
}
