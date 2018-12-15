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
    public partial class frmLogin : Form
    {
        LogsDatabase db = new LogsDatabase();
        const string databaseName = "Mydata";
        public frmLogin()
        {
            InitializeComponent();
            try
            {
                db.ReadXml(databaseName);
            }
            catch (Exception ex)
            {
                string user = "Admin";
                string password = "123";
                db.Users.AddUsersRow(user, password);
                db.WriteXml(databaseName);
            }
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
          

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var r = db.Users.Select("UserName like '" + textBox1.Text + "' AND Password like '" + textBox2.Text + "'");
            if (r.Count() > 0)
            {
                Close();
                frmReport frm = new frmReport();
                frm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Wrong username or password !!");
            }
        }
    }
}
