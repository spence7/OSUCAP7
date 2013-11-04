using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

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
            server = "localhost";
            database = "captest";
            uid = "root";
            password = "OSUCap7";
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

        public void Insert()
        {
            string query = "INSERT INTO FileURL (URL) VALUES('" + "Test" + "')";

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



        public void Query()
        {
            string query = "SELECT URL FROM fileurl";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Public\\QueryResults.txt", true))
            {
                file.WriteLine("Outside of db connection");
                //open connection
                if (this.OpenConnection() == true)
                {
                    file.WriteLine("Inside of db connection");
                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    MySqlDataReader dr = cmd.ExecuteReader();


                    while (dr.Read())
                    {
                        file.WriteLine("{0}", dr["URL"].ToString());
                    }


                    //close connection
                    this.CloseConnection();
                }
            }
        }


        public void WriteToErrorFile(String str)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\Public\\DatabaseError.txt", true))
            {
                file.WriteLine("{0}  - {1}",str, System.DateTime.Now);
            }

        }


    }
}
