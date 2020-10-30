using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace GYTECH_UPLOADER
{
    public partial class Form1 : Form
    {
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
                lbl_fieldname.Text = openFileDialog1.FileName;

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
            }
            else
            {
                btn_start.Tag = "STOP";
                btn_start.Text = "시작(&S)";
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
    }
}
