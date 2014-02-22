using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Media;
using Jimaku2.Properties;
using Yoteichi.Bbs;

namespace Jimaku2
{
    public partial class MainWindow : Form, IApplicationView
    {
        Point m_WindowDecorationOffset;
        Point m_MousePoint;
        Color m_TransparencyKey;
        OpenThreadDialog m_OpenThreadDialog;
        JimakuRenderer m_JimakuRenderer;
        IApplicationLogic m_ApplicationLogic;
        SoundPlayer m_SoundEffect;

        // コンストラクタ
        public MainWindow()
        {
            var drawArea = this.ClientSize;
            drawArea.Width -= 45;
            m_JimakuRenderer = new JimakuRenderer(this.ClientSize);

            m_ApplicationLogic = new ApplicationLogic();
            m_ApplicationLogic.Attach(this);

            m_SoundEffect = new SoundPlayer();
            m_SoundEffect = new SoundPlayer(@"C:\Tools\nicocast108\newnico.wav");

            InitializeComponent(); // 画面のDPIによっては　Resize とか Paint を飛ばしてくるので最後。
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            // ダブルバファリングを有効にする
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            // 前回終了時の位置と大きさにする
            // TODO: 画面をはみ出さないようにする
            // Suggestion: [0, 1) にノーマライズしてはどうか？
            if (Settings.Default.WindowLocation.X != -1)
            {
                this.Location = Settings.Default.WindowLocation;
            }
            if (Settings.Default.WindowSize.Width != -1)
            {
                this.Size = Settings.Default.WindowSize;
            }

            m_WindowDecorationOffset.X = (Size.Width - ClientSize.Width) / 2;
            int borderWidth = m_WindowDecorationOffset.X;
            m_WindowDecorationOffset.Y = Size.Height - ClientSize.Height - borderWidth;

            UpdateWindowText();

            stayOnTopToolStripMenuItem.Checked = Settings.Default.StayOnTop;
            this.TopMost = Settings.Default.StayOnTop;

            var uri = new Uri(Settings.Default.LastThread);
            m_ApplicationLogic.OpenThread(uri);

            m_TransparencyKey = this.BackColor;
            m_OpenThreadDialog = new OpenThreadDialog();

            displayTimer.Enabled = true;

            // デザイナーに MouseWheel イベントが出ないので手動で登録
            this.MouseWheel += new MouseEventHandler(this.MainWindow_MouseWheel);
        }

        private void UpdateWindowText()
        {
            var appName = Settings.Default.ProgramName;
            var dimension = String.Format("{0}x{1}", this.ClientSize.Width, this.ClientSize.Height);
            var threadName = m_ApplicationLogic.ThreadTitle == null ? "(無題)" : m_ApplicationLogic.ThreadTitle;
            var titleText = String.Format("{0} - {1} {2}", threadName, appName, dimension);
            this.Text = titleText;
        }

        /// <summary>
        ///  アプリケーションロジックの状態変更通知コールバック
        /// </summary>
        void IApplicationView.OnStateChange()
        {
            UpdateWindowText();

            // トラックバーの状態を更新
            if (m_ApplicationLogic.CurrentOperationMode != OperationMode.Idle)
            {
                trackBar.Enabled = true;
                trackBar.Minimum = 0;
                trackBar.Maximum = m_ApplicationLogic.MessageList.Count;
                trackBar.Value = m_ApplicationLogic.MessageListIndex;
            }
            else
            {
                trackBar.Enabled = false;
            }

            switch (m_ApplicationLogic.CurrentOperationMode)
            {
            case OperationMode.Playing:
                pauseButton.Text = "〓";
                break;
            case OperationMode.Paused:
                pauseButton.Text = "▶";
                break;
            }

            if (m_ApplicationLogic.CurrentOperationMode == OperationMode.Waiting)
            {
                // message = "（レス待機中）";
                m_JimakuRenderer.Text = "";
            }
            else
            {
                if (m_ApplicationLogic.CurrentMessage != null)
                {
                    m_JimakuRenderer.Text = WebText.UnescapeHtml(m_ApplicationLogic.CurrentMessage.Body);
                }
            }

            this.Invalidate();
        }

        void IApplicationView.OnMoveForward()
        {
            Debug.WriteLine("IApplicationView.OnMoveFoward");
            m_SoundEffect.Play();
        }

        private void ToggleControlVisibility(bool isVisible)
        {
            menuStrip.Visible = isVisible;
            pauseButton.Visible = isVisible;
            prevButton.Visible = isVisible;
            nextButton.Visible = isVisible;
            trackBar.Visible = isVisible;
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {

            ToggleControlVisibility(true);

            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Top -= m_WindowDecorationOffset.Y;
            Left -= m_WindowDecorationOffset.X;

            this.AllowTransparency = false;
        }

        private void MainWindow_Deactivate(object sender, EventArgs e)
        {

            ToggleControlVisibility(false);

            this.FormBorderStyle = FormBorderStyle.None;
            Top += m_WindowDecorationOffset.Y;
            Left += m_WindowDecorationOffset.X;

            this.AllowTransparency = true;
            this.TransparencyKey = m_TransparencyKey;
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            Debug.WriteLine("MainWindow_Paint");
            // g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            DrawIndicator(e.Graphics);
            m_JimakuRenderer.Draw(e.Graphics);
        }

        private void DrawIndicator(Graphics g)
        {
            OperationMode operationMode = m_ApplicationLogic.CurrentOperationMode;

            // 背景の描画
            Brush backBrush;
            if (operationMode == OperationMode.Playing ||
                operationMode == OperationMode.Waiting)
                backBrush = new SolidBrush(Color.Green);
            else
                backBrush = new SolidBrush(Color.Red);
            g.FillRectangle(backBrush, 0, 0, 68 + 6, 12 * 1);
            backBrush.Dispose();

            string labelText;
            if (m_ApplicationLogic.CurrentOperationMode == OperationMode.Playing)
            {
                labelText = String.Format("{0}/{1}",
                    m_ApplicationLogic.CurrentMessage.Number, // ここどうするか考える透明あぼーんで数があわなくなるはず
                    m_ApplicationLogic.MessageList.Last().Number);
            }
            else
            {
                string state;
                string number;

                switch (operationMode)
                {
                case OperationMode.Paused:
                    state = "停止中";
                    number = m_ApplicationLogic.CurrentMessage.Number.ToString();
                    break;
                case OperationMode.Waiting:
                    state = "レス待ち";
                    number = m_ApplicationLogic.MessageList.Last().Number.ToString();
                    break;
                case OperationMode.Idle:
                    state = "空";
                    number = "(空)";
                    break;
                case OperationMode.Stopped:
                    state = "スレスト";
                    number = m_ApplicationLogic.MessageList.Last().Number.ToString();
                    break;
                default:
                    state = "なんか";
                    number = "おかしいです";
                    break;
                }
                labelText = string.Format("{0} {1}", state, number);
            }

            // 文字の描画
            using (Brush foreBrush = new SolidBrush(Color.White),
                         shadowBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(labelText, SystemFonts.DefaultFont, foreBrush, new Point(0, 0));
                g.DrawString(m_ApplicationLogic.ThreadTitle, SystemFonts.DefaultFont, shadowBrush, new Point(0 + 1, 12 + 1));
                g.DrawString(m_ApplicationLogic.ThreadTitle, SystemFonts.DefaultFont, foreBrush, new Point(0, 12));
            }
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 設定を保存する
            Settings.Default.WindowLocation = this.Location;
            Settings.Default.WindowSize = this.Size;
            if (m_ApplicationLogic.CurrentOperationMode != OperationMode.Idle)
                Settings.Default.LastThread = m_ApplicationLogic.Thread.Uri.ToString();
            Settings.Default.StayOnTop = this.TopMost;
            Settings.Default.Save();

            m_ApplicationLogic.Detach(this);
            m_SoundEffect.Dispose();
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                //位置を記憶する
                m_MousePoint = new Point(e.X, e.Y);
            }
        }

        private void MainWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            Debug.Print("MouseWheel Delta = {0}", e.Delta);
            int amount = Math.Abs(e.Delta / 120);
            if (e.Delta > 0)
            {
                for (var i = 0; i < amount; i++)
                    m_ApplicationLogic.MoveIndex(-1);
            }
            else
            {
                for (var i = 0; i < amount; i++)
                    m_ApplicationLogic.MoveIndex(+1);
            }
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.Left += e.X - m_MousePoint.X;
                this.Top += e.Y - m_MousePoint.Y;
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fontDialog.Font = Settings.Default.JimakuFont;
            var result = fontDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Default.JimakuFont = fontDialog.Font;
                Settings.Default.Save();
            }
        }

        private void colorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colorDialog.Color = Settings.Default.JimakuColor;
            var result = colorDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                Settings.Default.JimakuColor = colorDialog.Color;
                Settings.Default.Save();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("バージョンとかないです");
        }

        private void displayTimer_Tick(object sender, EventArgs e)
        {
            Timer timer = sender as Timer;
            timer.Stop();
            m_ApplicationLogic.Update();
            timer.Start();
        }

        private void pauseButton_Click(object sender, EventArgs e)
        {
            OperationMode newOperationMode;

            if (m_ApplicationLogic.CurrentOperationMode == OperationMode.Paused)
            {
                newOperationMode = OperationMode.Playing;
                m_ApplicationLogic.CurrentOperationMode = newOperationMode;
            }
            else if (m_ApplicationLogic.CurrentOperationMode == OperationMode.Playing)
            {
                newOperationMode = OperationMode.Paused;
                m_ApplicationLogic.CurrentOperationMode = newOperationMode;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            m_ApplicationLogic.MoveIndex(+1);
        }

        private void prevButton_Click(object sender, EventArgs e)
        {
            m_ApplicationLogic.MoveIndex(-1);
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            var drawArea = this.ClientSize;
            drawArea.Width -= 45;
            m_JimakuRenderer.DrawArea = drawArea;

            UpdateWindowText();
            this.Invalidate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = m_OpenThreadDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                m_ApplicationLogic.OpenThread(m_OpenThreadDialog.Uri);
            }
        }

        private void firstToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_ApplicationLogic.MessageListIndex = 0;
        }

        private void lastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_ApplicationLogic.MessageListIndex = m_ApplicationLogic.MessageList.Count;
        }

        private void pauseButton_MouseEnter(object sender, EventArgs e)
        {
            switch (m_ApplicationLogic.CurrentOperationMode)
            {
            case OperationMode.Playing:
                pauseButton.Text = "〓";
                pauseButton.BackColor = Color.Red;
                pauseButton.ForeColor = Color.White;
                break;
            case OperationMode.Paused:
                pauseButton.Text = "▶";
                pauseButton.BackColor = Color.Green;
                pauseButton.ForeColor = Color.White;
                break;
            }
        }

        private void pauseButton_MouseLeave(object sender, EventArgs e)
        {
            pauseButton.BackColor = SystemColors.ControlDark;
            pauseButton.ForeColor = Color.White;
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            m_ApplicationLogic.MessageListIndex = trackBar.Value;
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (m_ApplicationLogic.Thread != null)
            {
                otherThreadsToolStripMenuItem.Enabled = true;
                ToolStripItemCollection items = otherThreadsToolStripMenuItem.DropDownItems;
                items.Clear();
                var board = m_ApplicationLogic.Thread.Board;
                if (board.ThreadList == null) // 必要ないと思うけれども
                    board.Reload();

                Debug.WriteLine("{0}のスレッドがあります。", board.ThreadList.Count);
                IEnumerable<ToolStripItem> newItems = from thread in board.ThreadList
                                                      where thread.LatestMessageNumber != 1001
                                                      select
                                                      new ToolStripMenuItem(thread.Title, null, (_sender, _e) =>
                                                      {
                                                          m_ApplicationLogic.OpenThread(thread.Uri);
                                                      });
                items.AddRange(newItems.ToArray());
                Debug.WriteLine("メニューの生成が終わりました。");
            }
            else
            {
                otherThreadsToolStripMenuItem.Enabled = false;
            }

        }

        private void stayOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
        }

        private void viewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            stayOnTopToolStripMenuItem.Checked = this.TopMost;
        }
    }
}
