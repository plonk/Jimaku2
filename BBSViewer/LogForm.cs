using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BBSViewer
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
        }

        private void copyTimer_Tick(object sender, EventArgs e)
        {
            if (Program.LogText.Length > logTextBox.Text.Length)
            {
                var tail = Program.LogText.ToString().Substring(logTextBox.Text.Length, Program.LogText.Length - logTextBox.Text.Length);
                logTextBox.AppendText(tail);
            }
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

    }
}
