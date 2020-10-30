using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace GYTECH_UPLOADER
{
    public partial class Form1 : Form
    {

        BackgroundWorker worker = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void pic_setting_Click(object sender, EventArgs e)
        {            
            if(new SettingForm().ShowDialog() == DialogResult.OK)
            {
                lbl_fieldname.Text = "현장명 : " + GLOBAL.FieldName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btn_start.Tag = "STOP";

            if (!File.Exists("Settings.ini"))
            {
                FileStream fs = File.Create("Settings.ini");
                fs.Close();
                
            }

            IniFile iniFile = new IniFile();
            iniFile.Load("Settings.ini");

            
            if(iniFile["Setting"]["FieldNo"].ToString() == null || iniFile["Setting"]["Interval"].ToString() == null)
            {                
                MessageBox.Show("초기 환경설정이 필요합니다. 환경설정으로 이동합니다.", "환경설정 필요", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (new SettingForm().ShowDialog() != DialogResult.OK)
                {
                    this.Close();
                    return;
                }

                
            }
            else
            {
                GLOBAL.FieldNo = iniFile["Setting"]["FieldNo"].ToString();
                int.TryParse(iniFile["Setting"]["Interval"].ToString(), out GLOBAL.Interval);

                string response = GLOBAL.HttpWebRequest("http://198.13.51.149/gytech/getsensorlist.php");

                JObject resobj = JObject.Parse(response);
                JArray arr_fields = (JArray)resobj["FieldList"];

                foreach(JObject field in arr_fields)
                {
                    if(field["No"].ToString().Equals(GLOBAL.FieldNo.ToString()))
                    {
                        GLOBAL.FieldName = field["FieldName"].ToString();
                        break;
                    }
                }
            }

            lbl_fieldname.Text = "현장명 : " + GLOBAL.FieldName;
            lbl_filepath.Text = iniFile["Setting"]["FileName"].ToString();


            
        }

        private void btn_browse_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "데이터 파일 (*.dat)|*.dat";
            openFileDialog1.FileName = "";
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                lbl_filepath.Text = openFileDialog1.FileName;

                IniFile iniFile = new IniFile();
                iniFile.Load("Settings.ini");
                iniFile["Setting"]["FileName"] = lbl_fieldname.Text;
                iniFile.Save("Settings.ini");
            }

            
        }

        private void btn_start_Click(object sender, EventArgs e)
        {
            if(btn_start.Tag.ToString() == "STOP")
            {
                btn_start.Tag = "START";
                btn_start.Text = "정지(&S)";

                GLOBAL.Remaining = 0;
                timer1.Start();
            }
            else
            {
                btn_start.Tag = "STOP";
                btn_start.Text = "시작(&S)";

                timer1.Stop();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btn_start.Tag.ToString() == "START")
            {
                MessageBox.Show("업로드가 진행중입니다. 업로드를 정지한 후 종료해주세요.", "업로드 진행중", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
        }

        public void UploadData(string filename)
        {
            if(!File.Exists(filename))
            {
                MessageBox.Show("지정한 위치에 파일이 없습니다. 파일 이름이 변경되었거나 옮겨지지 않았는지 확인해주세요.", "파일오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection connection = new MySqlConnection("Server=198.13.51.149;Port=3306;Database=GYTECH;Uid=snjadmin;Pwd=07042124505"))
            {
                DateTime LastDateTime = DateTime.Now;
                DataTable FieldTable = null;

                // 1. Get Last Uploaded Data From Database;
                try
                {
                    connection.Open();
                    string sql = "SELECT * FROM `SensorValue_" + GLOBAL.FieldNo + "` ORDER BY `WrittenDate` DESC LIMIT 0, 1";

                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    MySqlDataReader table = cmd.ExecuteReader();

                    FieldTable = table.GetSchemaTable();
                    
                                        
                    if (table.Read())
                    {
                        LastDateTime = DateTime.Parse(table["WrittenDate"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                // 2. File Read

                string line = "";
                StreamReader sr = new StreamReader(filename);

                string sql_insert = "";
                string sql_values = "";
                List<string> ColumnNames = new List<string>();
                Dictionary<String, String> DataSets = new Dictionary<string, string>();

                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    DateTime CurrentDateTime = DateTime.Now;


                    parts[0] = parts[0].Replace("\"", "");

                    if(!DateTime.TryParse(parts[0], out CurrentDateTime))
                    {
                        if(parts[0].Contains("TIMESTAMP"))
                        {
                            for(int i=0; i < parts.Length; i++)
                            { 
                                if(parts[i].StartsWith("\"Displacement"))
                                {
                                    parts[i] += ",";
                                    parts[i] += parts[i + 1];
                                    ColumnNames.Add(parts[i]);
                                    i++;
                                }
                                else
                                {
                                    ColumnNames.Add(parts[i]);
                                }
                                
                            }                            
                        }
                        continue;
                    }

                    if(DateTime.Compare(CurrentDateTime, LastDateTime) <= 0)
                    {
                        continue;
                    }

                    //Console.WriteLine("시작일시 : " + CurrentDateTime.ToString("yyyy-mm-dd hh:ii:ss"));

                    //INSERT INTO `SensorValue_4` (`No`, `ddd`, `WrittenDate`, `Displacement(1, 1)`, `Displacement(1, 2)`, `Displacement(1, 3)`, `Displacement(1, 4)`, `Displacement(1, 5)`, `Displacement(1, 6)`, `Displacement(1, 7)`, `Displacement(1, 8)`, `Displacement(1, 9)`, `Displacement(1, 10)`, `Displacement(1, 11)`, `Displacement(1, 12)`, `Displacement(1, 13)`, `Displacement(1, 14)`, `Displacement(1, 15)`, `Displacement(1, 16)`, `Displacement(1, 17)`, `Displacement(1, 18)`, `Displacement(1, 19)`, `Displacement(1, 20)`, `Displacement(1, 21)`, `Displacement(1, 22)`, `Displacement(1, 23)`, `Displacement(1, 24)`, `Displacement(1, 25)`, `Displacement(1, 26)`, `Displacement(1, 27)`, `Displacement(1, 28)`, `Displacement(1, 29)`, `Displacement(1, 30)`, `Displacement(1, 31)`, `Displacement(1, 32)`, `Displacement(1, 33)`, `Displacement(1, 34)`, `Displacement(1, 35)`, `Displacement(1, 36)`, `Displacement(1, 37)`, `Displacement(1, 38)`, `Displacement(1, 39)`, `Displacement(1, 40)`, `Displacement(1, 41)`, `Displacement(1, 42)`, `Displacement(1, 43)`, `Displacement(1, 44)`, `Displacement(1, 45)`, `Displacement(1, 46)`, `Displacement(1, 47)`, `Displacement(1, 48)`, `Displacement(1, 49)`, `Displacement(1, 50)`, `Displacement(1, 51)`, `Displacement(1, 52)`, `EL_1`, `EL_2`, `EL_3`, `EL_4`, `EL_5`, `EL_6`, `EL_7`, `EL_8`, `EL_9`, `EL_10`, `EL_11`, `EL_12`, `EL_13`, `EL_14`, `EL_15`, `EL_16`, `EL_17`, `EL_18`, `EL_19`, `EL_20`, `EL_21`, `EL_22`, `EL_23`, `EL_24`, `EL_25`, `EL_26`, `EL_27`, `EL_28`, `TT_1X`, `TT_1Y`, `TT_2X`, `TT_2Y`, `TT_3X`, `TT_3Y`, `TT_4X`, `TT_4Y`, `TT_5X`, `TT_5Y`, `TT_6X`, `TT_6Y`) VALUES(NULL, '0', '2020-10-28 23:55:00', '-2.148132', '-22.95931', '-48.31509', '-72.3974', '-39.48508', '-41.95609', '-34.45265', '-29.77665', '-14.4368', '-22.15216', '-2.074059', '-3.096489', '0', '-17.79586', '-26.68397', '-42.49952', '-14.53712', '-33.53414', '-42.79342', '-6.696022', '-3.285118', '-17.8578', '-46.20357', '-33.36793', '-20.26176', '0.00000763', '-1.174587', '-2.951242', '-35.91781', '-26.32428', '-32.10583', '-35.1564', '-42.997', '-36.26576', '-34.08089', '-17.05228', '-14.2653', '-6.129105', '0', '-22.22998', '-52.20802', '-104.453', '-108.279', '-104.321', '-76.18083', '-58.0886', '-29.34669', '-24.99608', '-55.66338', '-48.76485', '-38.36747', '0', '79.34892', '78.67072', '245.5069', '92.91283', '36.62258', '124.788', '58.32485', '-38.65717', '-2.034588', '109.8677', '16.2767', '217.0227', '-10.17294', '67.81959', '229.2302', '250.2543', '75.95794', '266.531', '265.8528', '179.0437', '92.23464', '-18.31129', '127.5008', '177.6873', '213.6317', '75.95794', '124.1098', '200.746', '163.4452', '-761.614', '0', '-2.034588', '124.1098', '-1247.202', '-271.9565', '12.88572', '-102.4076', '-1626.314', '-101.0512', '-127.5008');
                    sql_insert = "INSERT INTO `SensorValue_" + GLOBAL.FieldNo + "` (";
                    sql_values = " VALUES(NULL, '0', ";
                    if (FieldTable.Rows.Count != (parts.Length - 9)) continue;

                    int cnt = 0;
                    String columnname = "";

                    foreach (DataRow row in FieldTable.Rows)
                    {
                        if (cnt > 0) sql_insert += ",";
                        sql_insert += "`" + row.Field<String>("ColumnName") + "`";
                        cnt++;

                        columnname = row.Field<String>("ColumnName") == "WrittenDate" ? "TIMESTAMP" : ("\"" + row.Field<String>("ColumnName") + "\"");
                        int idx = ColumnNames.IndexOf(columnname);
                        if (idx >= 0)
                        {
                            //sql_values += ColumnNames[idx];
                            DataSets.Add(row.Field<String>("ColumnName"), (parts[idx] == "NAN" ? "0" : parts[idx]));
                        }
                        
                    }

                    sql_insert += ") VALUES(NULL, '0', ";

                    cnt = 0;
                    foreach(string part in parts)
                    {
                        if (cnt == 1)
                        {
                            cnt++;
                            continue;
                        }
                        
                        if (cnt > 0) sql_insert += ",";


                        sql_insert += "'" + ((part == "NAN") ? "0" : part) + "'";
                        cnt++;
                    }

                    sql_insert += ");";

                    Console.WriteLine(sql_insert);


                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (worker != null && worker.IsBusy) return;

            GLOBAL.Remaining = (GLOBAL.Remaining == -1) ? GLOBAL.Interval : (GLOBAL.Remaining - 1);
            
            if(GLOBAL.Remaining < 0)
            {
                lbl_stat.Text = "데이터 업로드 중";
                //UploadData(lbl_filepath.Text);
                worker = new BackgroundWorker();
                worker.DoWork += Worker_DoWork;
                worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
                worker.RunWorkerAsync();

            }
            else
            {
                lbl_stat.Text = GLOBAL.Remaining + "초 후 업로드";
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            
            this.Invoke(new MethodInvoker(delegate () {
                txt_stat.Text += "데이터 업로드 시작\r\n";
            }));


            UploadData(lbl_filepath.Text);
            
            this.Invoke(new MethodInvoker(delegate () {
                txt_stat.Text += "데이터 업로드 종료\r\n";
            }));
            
        }
    }
}
