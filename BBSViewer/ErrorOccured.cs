using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBSViewer
{
    public class ErrorOccuredEventArgs : EventArgs
    {
        public ErrorOccuredEventArgs(string text, string caption)
        {
            Text = text;
            Caption = caption;
        }
        public string Text { get; set; }
        public string Caption { get; set; }
    }
    delegate void ErrorOccuredEventHandler(object sender, ErrorOccuredEventArgs e);
}
