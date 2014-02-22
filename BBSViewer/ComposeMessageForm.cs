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
    /// <summary>
    /// 新規レス作成フォーム。
    /// </summary>
    public partial class ComposeMessageForm : Form
    {
        public ComposeMessageForm()
        {
            InitializeComponent();
        }

        // 送信ボタンのクリック
        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // キャンセルボタンのクリック
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// メッセージの内容。エスケープはされていない。改行コードはCRLF。
        /// </summary>
        public string Message
        {
            get { return messageTextBox.Text; }
        }

        /// <summary>
        /// メールアドレス。
        /// </summary>
        public string Mail
        {
            get { return mailTextBox.Text; }
        }

        /// <summary>
        /// 投稿者名。
        /// </summary>
        public string SenderName
        {
            get { return nameTextBox.Text; }
        }

        /// <summary>
        /// レスを投稿するスレのタイトル。
        /// クライアントがこのフォームを ShowDialog() する前に設定すべき。
        /// </summary>
        public string ThreadTitle
        {
            get
            {
                return threadTitleLabel.Text;
            }
            set
            {
                threadTitleLabel.Text = value;
            }
             
        }

        private void ComposeMessageForm_Load(object sender, EventArgs e)
        {
            nameTextBox.Text = "";
            mailTextBox.Text = "sage";
            messageTextBox.Text = "";
        }
    }
}
