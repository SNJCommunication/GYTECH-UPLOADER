using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace GYTECH_UPLOADER
{
    public partial class SettingForm : Form
    {
        List<JObject> Fields = new List<JObject>();
        int[] UploadIntervals = new int[6];
        public SettingForm()
        {
            InitializeComponent();
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            string response = GLOBAL.HttpWebRequest("http://198.13.51.149/gytech/getfieldlist.php");

            JObject resobj = JObject.Parse(response);
            JArray arr_fieldlist = (JArray)resobj["FieldList"];

            Fields.Clear();

            foreach(JObject field in arr_fieldlist)
            {
                listBox1.Items.Add(field["FieldName"].ToString());
                Fields.Add(field);
            }

            UploadIntervals[0] = 300;
            UploadIntervals[1] = 600;
            UploadIntervals[2] = 900;
            UploadIntervals[3] = 1800;
            UploadIntervals[4] = 3600;
            UploadIntervals[5] = -1;
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("업로드 할 현장을 선택해주세요.", "현장선택오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (listBox2.SelectedIndex < 0)
            {
                MessageBox.Show("자동업로드 주기 또는 수동업로드를 선택해주세요.", "업로드 설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            IniFile iniFile = new IniFile();
            iniFile.Load("Settings.ini");

            iniFile["Setting"]["FieldNo"] = Fields[listBox1.SelectedIndex]["No"].ToString();
            iniFile["Setting"]["Interval"] = UploadIntervals[listBox2.SelectedIndex];
            iniFile.Save("Settings.ini");

            GLOBAL.FieldName = Fields[listBox1.SelectedIndex]["FieldName"].ToString();
            GLOBAL.FieldNo = Fields[listBox1.SelectedIndex]["No"].ToString();
            GLOBAL.Interval = UploadIntervals[listBox2.SelectedIndex];

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
