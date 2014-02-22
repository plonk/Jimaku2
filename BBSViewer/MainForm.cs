using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Yoteichi;

namespace BBSViewer
{
    /// <summary>
    /// ユーザインターフェース。
    /// メインフォーム。
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// フォームはこのオブジェクトの状態を表示することを使命とし、
        /// また、ユーザーの操作は主としてこのオブジェクトに入力する。
        /// </summary>
        ApplicationLogic m_ApplicationLogic;
        /// <summary>
        /// レス投稿フォーム。
        /// </summary>
        ComposeMessageForm m_ComposeMessageForm;
        /// <summary>
        /// ログ表示フォーム。
        /// </summary>
        LogForm m_LogForm;

        // コンストラクタ
        public MainForm()
        {
            InitializeComponent();

            m_ApplicationLogic = new ApplicationLogic();
            m_ApplicationLogic.ErrorOccured += this.OnError;
            m_ApplicationLogic.PropertyChanged += this.UpdateRepresentation;

            // 利用する別のフォームを作成する。

            m_ComposeMessageForm = new ComposeMessageForm();

            m_LogForm = new LogForm();
            m_LogForm.Show();

            foreach (var url in new string[] { "http://jbbs.shitaraba.net/game/48538/", "http://yy25.60.kg/peercastjikkyou/", "http://katsu.ula.cc/yoteichi/" })
            {
                var item = new ToolStripMenuItem();
                item.Text = url;
                item.Click += async (object sender, EventArgs e) =>
                {
                    await m_ApplicationLogic.OpenBoardAsync(url);
                };
                bookmarksToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        /// <summary>
        /// ウィンドウタイトルを選択された板の名前にもとづいて更新する。
        /// </summary>
        void UpdateTitle()
        {
            if (m_ApplicationLogic.Board != null)
            {
                this.Text = m_ApplicationLogic.Board.Name + " - BBSViewer";
            }
            else
            {
                this.Text = "BBSViewer";
            }
        }

        /// <summary>
        /// スレッド一覧表示部を選択された板にもとづいて更新する。
        /// </summary>
        void UpdateThreadList()
        {
            if (m_ApplicationLogic.Board != null)
            {
                // 消して追加しなおし
                threadListView.Items.Clear();
                foreach (var thread in m_ApplicationLogic.Board.ThreadList)
                {
                    var f1 = thread.Id.ToString();
                    var f2 = thread.Title;
                    var f3 = thread.LatestMessageNumber.ToString();
                    var f4 = "N/A"; // thread.IsStopped ? "YES" : "NO";
                    var row = new string[] { f1, f2, f3, f4 };
                    var item = new ListViewItem(row);
                    threadListView.Items.Add(item);
                }
                threadListView.Enabled = true;
            }
            else
            {
                threadListView.Items.Clear();
                threadListView.Enabled = false;
            }
        }

        /// <summary>
        /// レス一覧表示部を選択されたスレにもとづいて更新する。
        /// </summary>
        void UpdateMessageList()
        {
            if (m_ApplicationLogic.Thread != null)
            {
                // 消して追加しなおし
                messageListView.Items.Clear();
                foreach (var res in m_ApplicationLogic.Thread.MessageList)
                {
                    var f1 = res.Number.ToString();
                    string tmp = res.Body.UnescapeHtml();
                    string f2;
                    if (tmp.Length > 30)
                        f2 = tmp.Substring(0, 30) + "…";
                    else
                        f2 = tmp;
                    var row = new string[] { f1, f2 };
                    var item = new ListViewItem(row);
                    messageListView.Items.Add(item);
                }
                messageListView.Enabled = true;
            }
            else
            {
                messageListView.Items.Clear();
                messageListView.Enabled = false;
            }
        }

        /// <summary>
        /// スレッドのURIを表示するラベルを更新する。（そんなものなかった）
        /// </summary>
        void UpdateThreadUriLabel()
        {
        }

        /// <summary>
        /// レス表示部を更新する。
        /// </summary>
        void UpdateMessageText()
        {

            if (m_ApplicationLogic.Message != null)
            {
                var documentText = new StringBuilder(1024);
                var msg = m_ApplicationLogic.Message;
                var text = String.Format("{0} {1} {2} ID:{3}<br><br>{4}",
                   msg.Number, msg.Name, msg.Date, msg.Id, WebText.LinkifyUri(msg.Body));

                documentText
                    .Append("<html><head>")
                    .Append("<style>")
                    .Append("<!--\n" + Properties.Resources.StyleSheet + "\n-->")
                    .Append("</style>")
                    .Append("<title></title>")
                    .Append(String.Format("<base href=\"http://{0}/\">", m_ApplicationLogic.Board.TopUri.Host))
                    .Append("</head>")
                    .Append("<body>")
                    .Append(text)
                    .Append("</body>")
                    .Append("</html>");

                messageWebBrowser.DocumentText = documentText.ToString();
            }
            else
            {
                messageWebBrowser.DocumentText = "";
            }
        }

        /// <summary>
        /// URI 入力欄を選択された板の URI で更新する。
        /// </summary>
        private void UpdateUriTextBox()
        {
            if (m_ApplicationLogic.Board != null)
                uriTextBox.Text = m_ApplicationLogic.Board.TopUri.ToString();
        }

        /// <summary>
        /// PropertyChangedイベントに対するコールバック関数。表示を更新する。
        /// </summary>
        public void UpdateRepresentation(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Property {0} changed", (object) e.PropertyName);
            switch (e.PropertyName)
            {
            case "Board":
                UpdateTitle();
                UpdateUriTextBox();
                UpdateThreadList();
                break;
            case "Thread":
                UpdateMessageList();
                UpdateThreadUriLabel();
                break;
            case "Message":
                UpdateMessageText();
                break;
            default:
                Console.WriteLine("Unknown PropertyName \"{0}\"", e.PropertyName);
                break;
            }
        }

        /// <summary>
        /// 移動ボタンがクリック。URI入力欄にもとづいて板を開く。
        /// </summary>
        private async void goButton_Click(object sender, EventArgs e)
        {
            await m_ApplicationLogic.OpenBoardAsync(uriTextBox.Text);
        }

        /// <summary>
        /// ErrorOccured イベントに対するコールバック。「開けませんでした」
        /// </summary>
        public void OnError(object sender, ErrorOccuredEventArgs e)
        {
            MessageBox.Show(e.Text, e.Caption);
        }

        /// <summary>
        /// 終了メニュー項目
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // メッセージリストで選択が変更された
        private void messageListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var items = messageListView.SelectedItems;
            if (items.Count == 0)
                return;
            var resNumber = items[0].SubItems[0].Text;
            m_ApplicationLogic.SelectMessage(resNumber);
        }

        // スレッドリストで選択が変更された
        private void threadListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var items = threadListView.SelectedItems;
            if (items.Count == 0)
                return;
            var threadNumber = items[0].SubItems[0].Text;
            m_ApplicationLogic.SelectThread(threadNumber);
        }

        // ツール　リロード
        private async void reloadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            await m_ApplicationLogic.ReloadThread();
        }

        // 新規　メッセージ
        private void messageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_ApplicationLogic.Thread == null)
            {
                return;
            }

            m_ComposeMessageForm.ThreadTitle = m_ApplicationLogic.Thread.Title;
            var result = m_ComposeMessageForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                m_ApplicationLogic.PostMessage(
                    m_ComposeMessageForm.SenderName,
                    m_ComposeMessageForm.Mail,
                    m_ComposeMessageForm.Message.Replace("\r\n", "\n"));
            }
        }

        // 新規
        private void newToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            threadToolStripMenuItem.Enabled = (m_ApplicationLogic.Board != null);
            messageToolStripMenuItem.Enabled = (m_ApplicationLogic.Thread != null);
        }

        // ログウィンドウの表示切り替え
        private void logWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_LogForm.Visible)
            {
                m_LogForm.Hide();
            }
            else
            {
                m_LogForm.Show();
            }
        }

        // ツールメニューが開く
        private void toolsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            logWindowToolStripMenuItem.Checked = m_LogForm.Visible;
        }

    }
}
