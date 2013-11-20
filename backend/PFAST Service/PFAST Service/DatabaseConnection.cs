using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.IO;
using System.Diagnostics;

namespace PFAST_Service
{
    class DatabaseConnection
   {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        private void Initialize()
        {
            //Test DB info

            //server = "localhost";
            //database = "irani";
            //uid = "root";
            //password = "OSUCap7";
            //string connectionString;
            //connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            //database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            //Live DB info
            server = "localhost";
            database = "irani";
            uid = "root";
            password = "w3m1$$IGOR";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        public DatabaseConnection()
        {
            this.Initialize();
        }

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //Error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        WriteToErrorFile("Cannot connect to server.  Contact administrator");
                        break;


                    case 1045:
                        WriteToErrorFile("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        private bool CloseConnection()
        {
         try
             {
            connection.Close();
            return true;
             }
            catch (MySqlException ex)
            {
            WriteToErrorFile(ex.Message);
            return false;
            }
         }

        public void UpdateResultsTable()
        {
            string query = "INSERT INTO RESULTS (pfast_input_jobs) VALUES('" + "Test" + "')";

            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
            }
        }

        public void QueryForNewJobs()
        {
            string query = "SELECT * FROM pfast_input_jobs ORDER BY upload_date";
                //open connection
                if (this.OpenConnection() == true)
                {

                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    //execute the query statement on the pfast_input_jobs table
                    MySqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        //at least one record was returned, now check that the input file exists in the returned directory

                        //store information returned from the database
                        string user_name = dr["user_name"].ToString();
                        DateTime upload_date = (DateTime)dr["upload_date"];
                        string MySQLFormat_upload_date = upload_date.ToString("yyyy-MM-dd HH:mm:ss");
                        string input_file_path = dr["input_file_path"].ToString();
                        string input_file_name = dr["input_file_name"].ToString();
                        string job_retry = dr["job_retry"].ToString();

                        string inputfile = input_file_path + input_file_name;

                        dr.Close();

                        if (File.Exists(inputfile))
                        {
                            WriteToLogFile("File found: " + inputfile + " , beginning input validation.");

                            InputValidator validator = new InputValidator();
                            string resultstext;

                            if (validator.ValidateInputFileForErrors(inputfile, out resultstext))
                            {
                                WriteToLogFile("Input passed validation, starting PFast processing of the input file.");

                                string outputfile = "PFast_Results_" + user_name + ".xls";

                                WriteToLogFile("The output file is: " + outputfile);
                                outputfile = outputfile.Replace(" ", "_");

                                WriteToLogFile("The updated output file is: " + outputfile);

                                //Execute PFast
                                Process PFastProcess = new Process();

                                PFastProcess.StartInfo.FileName = @"C:\PFast\PFast.exe";
                                PFastProcess.StartInfo.Arguments = "input.xls C:\\PFast\\Pfast_Result\\" + outputfile;
                                PFastProcess.StartInfo.UseShellExecute = false;
                                PFastProcess.StartInfo.CreateNoWindow = true;
                                PFastProcess.Start();

                                //Monitor the results folder for new output file
                                WriteToLogFile("Checking for output file");
                                OutputMonitor monitor = new OutputMonitor();

                                //If the output file is found in the output directory
                                if (monitor.MonitorForOutput(outputfile))
                                {
                                    WriteToLogFile("Found output file" + outputfile);
                                    //retrieve the output file directory and user email address for storage

                                    string userdataquery = "SELECT * FROM pfast_user_files WHERE user_name = '" + user_name + "' AND upload_date = '" + MySQLFormat_upload_date + "'";
                                    cmd = new MySqlCommand(userdataquery, connection);

                                    WriteToLogFile("Set command to get outputdir and email, query is: " + userdataquery);
                                    //execute the query statement on the pfast_input_jobs table

                                    WriteToLogFile("Before Execute");
                                    MySqlDataReader userdatadr = cmd.ExecuteReader();

                                    WriteToLogFile("SAfter Execute");
                                    userdatadr.Read();

                                    WriteToLogFile("After Read");
                                    string outputdir = userdatadr["output_file_path"].ToString() + outputfile;
                                    outputdir = outputdir.Replace(@"\", @"\\");
                                    string user_email = userdatadr["user_email"].ToString();
                                    WriteToLogFile("Retrieved output directory: " + outputdir);

                                    //close the connection 
                                    userdatadr.Close();

                                    //Move the file to the proper directory
                                    try
                                    {
                                        WriteToLogFile("Attempting to move the file");
                                        File.Move(@"C:\PFAST\PFast_Result\" + outputfile, outputdir); // Try to move the file
                                        WriteToLogFile("File Moved");
                                    }

                                    catch (IOException ex)
                                    {
                                        WriteToLogFile("Caught Exception when attempting to move file"); // Write error
                                    }

                                    //add the outputfile name and directory to pfast_user_jobs
                                    string userfilesquery = "UPDATE pfast_user_files SET output_file_path = '" + outputdir + "', output_file_name = '" + outputfile + "' WHERE user_name = '" + user_name + "' AND upload_date = '" + MySQLFormat_upload_date + "'";
                                    cmd = new MySqlCommand(userfilesquery, connection);
                                    cmd.ExecuteNonQuery();

                                    //write success to results table
                                    //make sure record does not already exist

                                    string existsquery = "SELECT count(*) FROM pfast_results WHERE user_name = '" + user_name + "' AND upload_date = '" + MySQLFormat_upload_date + "'";
                                    cmd = new MySqlCommand(existsquery,connection);
                                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                                    if (count == 0)
                                    {
                                        string resultsquery = "INSERT INTO pfast_results VALUES('" + user_name + "','" + MySQLFormat_upload_date + "','" + user_email + "','" + "Your input files have been processed.  Please login to your lean and flexible account to purchase your results.'" + ",1)";
                                        cmd = new MySqlCommand(resultsquery, connection);
                                        cmd.ExecuteNonQuery();
                                    }

                                    else
                                        WriteToLogFile("Result record already exists in database, insertion failed!");

                                    //delete job from queue
                                    string deletejobquery = "DELETE FROM pfast_input_jobs WHERE (user_name = '" + user_name + "') AND upload_date = ('" + MySQLFormat_upload_date + "')";
                                    cmd= new MySqlCommand(deletejobquery, connection);
                                    cmd.ExecuteNonQuery();
                                }
                                else
                                {
                                    WriteToLogFile("Failed to locate output file " + outputfile);
                                    // string resultsquery = "INSERT INTO pfast_results VALUES('" + dr["user_name"].ToString() + " 
                                    //put back on queue and update job count
                                }

                            }

                            else
                            {
                                WriteToLogFile("Input validation failed, aborting PFast processing of the input file.");
                                //write error to results table
                            }
                        }

                        else
                        {
                            WriteToLogFile("ERROR: Input file not found at : " + inputfile + "\nJob moved back to queue for reprocess. " + job_retry + "attempts remain");
                            //code to update job retry interval
                        }
                    }

                    else
                    {
                        //No records returned, close the datareader connection
                        dr.Close();
                    }

                       this.CloseConnection();
                    }
                    //close connection
                
                
                }
        


        public void WriteToErrorFile(String str)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Public\\DatabaseError.txt", true))
            {
                file.WriteLine("{0}  - {1}",str, System.DateTime.Now);
            }

        }

        public void WriteToLogFile(String str)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Public\\Pfast_Log.txt", true))
            {
                file.WriteLine(str);
            }

        }


    }
}
