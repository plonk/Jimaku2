using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Yoteichi.Bbs;

namespace Jimaku2
{
    public partial class OpenThreadDialog : Form
    {
        public Uri Uri { get; set; }

        public OpenThreadDialog()
        {
            InitializeComponent();
        }

        private void uriTextBox_TextChanged(object sender, EventArgs e)
        {
            string description = null;

            if (uriTextBox.Text == "")
            {
                description = "";
                okButton.Enabled = false;
            }
            else if (Uri.IsWellFormedUriString(uriTextBox.Text, UriKind.Absolute))
            {
                description = "";
                okButton.Enabled = true;
            }
            else
            {
                description = "URLではありません";
                okButton.Enabled = false;
            }
            descriptionLabel.Text = description;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            IThread thread = null;
            try
            {
                 thread = BoardGetter.GetThread(uriTextBox.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("開けないです。{0}", ex.Message));
                return;
            }
            if (thread == null)
            {
                MessageBox.Show("そんなスレないです");
                return;
            }
            Uri = new Uri(uriTextBox.Text);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenDialog_Load(object sender, EventArgs e)
        {
            descriptionLabel.Text = "";
        }

        private void OpenThreadDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            descriptionLabel.Text = "";
        }
    }
}
