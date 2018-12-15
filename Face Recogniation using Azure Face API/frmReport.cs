using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Face_Recogniation_using_Azure_Face_API
{
    public partial class frmReport : Form
    {
        const string databaseName = "Mydata";
        public frmReport()
        {
            InitializeComponent();
        }

        private void frmReport_Load(object sender, EventArgs e)
        {
            logsDatabase1.ReadXml(databaseName);
            for (int i = 0; i < logsDatabase1.Logs.Rows.Count; i++)
            {
                if (logsDatabase1.Logs.Rows[i]["Logout_date"].ToString().Length <= 0 || logsDatabase1.Logs.Rows[i]["Logout_date"] == null)
                {
                    logsDatabase1.Logs.Rows[i].Delete();
                }
            }
            dataGridView1.DataSource = logsDatabase1.Logs;
        }
    }
}
