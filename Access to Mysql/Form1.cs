using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace Access_to_Mysql
{
    public partial class Form1 : Form
    {
        OleDbConnection conn = new OleDbConnection();
        string pass = "OPENIT";
        List<string> tableName = new List<string>();
        MySqlConnection connection;
        OpenFileDialog ofd = new OpenFileDialog();
        string access = null;
        string host_name = null;
        string user_name = null;
        string pass_of_mysql = null;
        string db_name = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            host_name = host.Text;
            user_name = username.Text;
            pass_of_mysql = password.Text;
            db_name = db.Text;
            pass = access_pass.Text;


            if (!(String.IsNullOrEmpty(access) || String.IsNullOrEmpty(host_name) || String.IsNullOrEmpty(user_name) || String.IsNullOrEmpty(db_name)))
            {
                conn.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;" +
                @"Data source=" + access + ";"
                + "Jet OLEDB:Database Password=" + pass + ";";

                open_conn_access();
                get_all_table();
            }
            else
            {
                MessageBox.Show("Please Select All data First");
            }
        }

        public void open_conn_access()
        {

            try
            {
                conn.Open();
                //MessageBox.Show("Connected success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Access Connection is failed. Check if there is password or not");
            }

        } // Access connection

        public void close_conn_access()
        {
            conn.Close();
        } //Access connection close

        public void open_conn_mysql()
        {
            MySqlConnectionStringBuilder conn_str = new MySqlConnectionStringBuilder();
            conn_str.Server = host_name;
            conn_str.UserID = user_name;
            conn_str.Password = pass_of_mysql;
            conn_str.Database = db_name;
            connection = new MySqlConnection(conn_str.GetConnectionString(true));
            try
            {
                connection.Open();
            }catch(Exception ex)
            {
                MessageBox.Show("Mysql Database Connection Failed");
            }
        } //open mysql connection 

        public void get_all_table()
        {
            DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,new object[] {null,null,null, "TABLE" });
            open_conn_mysql();
            List<string> allrows = new List<string>();
            List<string> list_table = new List<string>();
            int progress = 0;
            int number_of_tables = dt.Rows.Count;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = number_of_tables;
            try
            {
                foreach (DataRow tr in dt.Rows)
                {
                    progressBar1.Value = progress;
                    string tbName = tr[2].ToString();
                    Console.WriteLine("*********************  " + tbName + "  ******************");
                    if (!tbName.Contains("MSys"))
                    {
                        string[] rest = new string[] { null, null, tbName, null };
                        DataTable rows = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, rest);
                        List<string> datatype_list = new List<string>();
                        allrows.Clear();
                        foreach (DataRow rr in rows.Rows)
                        {
                            string tbrow = rr["COLUMN_NAME"].ToString();
                            int colnum_type = Int32.Parse(rr["DATA_TYPE"].ToString());
                            string datatype = get_dataType(colnum_type);
                            datatype_list.Add(datatype);
                            if (!(Boolean)rr["IS_NULLABLE"])
                            {
                                allrows.Add(tbrow + " " + datatype);
                            }
                            else
                            {
                                allrows.Add(tbrow + " " + datatype + " NULL");
                            }
                        }
                        string columns = String.Join(",", allrows);
                        int sizeofrow = allrows.Count;
                        string query = "CREATE TABLE " + tbName + "(" + columns + ")";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine(query);
                        add_primary_key(tbName);
                        string[] temp = datatype_list.ToArray();
                        transfar_data(tbName, sizeofrow, temp);
                        list_table.Add(tbName);
                    }
                    progress++;
                }
            }catch(Exception e)
            {

            }
            add_forign_key();
            progressBar1.Value = number_of_tables;
            MessageBox.Show("Ok!!");
        } //close mysql connection



        public void add_primary_key(string tbName)
        {
            DataTable primarykeyTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Primary_Keys, new Object[] { null, null, tbName });
            int columnOrdinalForname = primarykeyTable.Columns["COLUMN_NAME"].Ordinal;
            string query_pk = "ALTER TABLE " + tbName + " ADD PRIMARY KEY(";
            int count = primarykeyTable.Rows.Count;
            int i = 0;
            foreach (DataRow r in primarykeyTable.Rows)
            {
                string pk = r.ItemArray[columnOrdinalForname].ToString();
                if (count > i + 1)
                {
                    query_pk = String.Concat(query_pk, pk + ",");
                }
                else
                {
                    query_pk = String.Concat(query_pk, pk);
                }
                i++;
            }

            query_pk = query_pk + ")";
            if (i > 0)
            {
                Console.WriteLine(query_pk);
                MySqlCommand cmd_pk = new MySqlCommand(query_pk, connection);
                cmd_pk.ExecuteNonQuery();
            }
        }// for adding primary keys

        public void transfar_data(string tbName,int size,string[] type)
        {
            OleDbCommand access_cmd = new OleDbCommand("SELECT * FROM " + tbName,conn);
            OleDbDataReader access_reader = access_cmd.ExecuteReader();
            List<string> data_holder = new List<string>();
            List<string> data_holder_rows = new List<string>();
            List<string> data_type = new List<string>();
            int j = 0;
            while(access_reader.Read())
            {
                data_holder.Clear();
             for(int i=0; i<size; i++)
                {
                    if(j==0)
                    {
                        data_type.Add(access_reader.GetName(i));
                    }

                    if(access_reader.GetDataTypeName(i).Equals("DBTYPE_DATE"))
                    {
                        string[] temp_date = access_reader.GetValue(i).ToString().Split(' ');
                        if (!String.IsNullOrEmpty(temp_date[0]))
                        {
                            string[] part_date = temp_date[0].Split('/');
                            string month = get_month(part_date[1]);
                            data_holder.Add("'" + part_date[2] + "-" + month + "-" + part_date[0] + "'");
                        }
                        else
                        {
                            data_holder.Add("'" + "" + "'");
                        }
                    }
                    else
                    {
                        string s = access_reader.GetValue(i).ToString();
                        if(!String.IsNullOrEmpty(s))
                        {
                            data_holder.Add("'"+s.ToString().Replace("'","")+"'");
                        }
                        else
                        {
                            data_holder.Add("NULL");
                        }
                    }
                }
                string temp = "(" + String.Join(",", data_holder) + ")";
                data_holder_rows.Add(temp);
                j++;
            }

            string query = "INSERT INTO " + tbName + " (" + String.Join(",",data_type) + ") " + " VALUES " + String.Join(",", data_holder_rows);
            if(data_holder_rows.Count > 0)
            {
                MySqlCommand insrtcmd = new MySqlCommand(query,connection);
                insrtcmd.ExecuteNonQuery();
            }
        }//for transfaring the data from access to mysql

        public void add_forign_key()
        {
            DataTable forignkeyTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, new Object[] { null });
            string query = null;
            foreach(DataRow row in forignkeyTable.Rows)
            {
                string Name = (string)row["FK_NAME"];
                string PrimaryTable = (string)row["PK_TABLE_NAME"];
                string PrimaryField = (string)row["PK_COLUMN_NAME"];
                string PrimaryIndex = (string)row["PK_NAME"];
                string ForeignTable = (string)row["FK_TABLE_NAME"];
                string ForeignField = (string)row["FK_COLUMN_NAME"];

                query = "ALTER TABLE " + ForeignTable + " ADD FOREIGN KEY (" + ForeignField + ") REFERENCES " + PrimaryTable + "(" + PrimaryField + ");";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                }catch(Exception e)
                {
                    Console.WriteLine("There is some problem with database please check");
                }
            }
        }



        public string get_month(string month)
        {
            switch(month)
            {
                case "Jan":
                    return "01";
                case "Feb":
                    return "02";
                case "Mar":
                    return "03";
                case "Apr":
                    return "04";
                case "May":
                    return "05";
                case "Jun":
                    return "06";
                case "Jul":
                    return "07";
                case "Aug":
                    return "08";
                case "Sep":
                    return "09";
                case "Oct":
                    return "10";
                case "Nov":
                    return "11";
                case "Dec":
                    return "12";
            }
            return null;
        } //convert month form text to number

        public string get_dataType(int i)
        {
            switch ((OleDbType)(i))
            {
                case OleDbType.WChar:
                    return "VARCHAR(600)";
                case OleDbType.Double:
                    return "DOUBLE";
                case OleDbType.Date:
                    return "DATE";
                case OleDbType.Boolean:
                    return "VARCHAR(10)";
                case OleDbType.Integer:
                    return "INT";
            }
            return null;
        }// for getting datatypes from the specific field


        private void button2_Click(object sender, EventArgs e)
        {
            if(ofd.ShowDialog() == DialogResult.OK)
            {
                access = ofd.FileName;
                access_file.Text = access;
            }
        }

        
    }
}
