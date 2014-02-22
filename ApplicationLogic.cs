using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yoteichi.Bbs;
using Jimaku2.Properties;

namespace Jimaku2
{
    /// <summary>
    /// アプリケーションの動作モード
    /// </summary>
    public enum OperationMode
    {
        // 再生中
        Playing,
        // レス待機中
        Waiting,
        // 現在のレスで一時停止
        Paused,
        // スレッドがストップしたため停止中
        Stopped,
        // 開かれているスレがない
        Idle,
    };

    interface IApplicationLogic
    {
        Message CurrentMessage { get; }
        int MessageListIndex { get; set; }
        void Attach(IApplicationView view);
        void Detach(IApplicationView view);
        OperationMode CurrentOperationMode { get; set; }
        void Update();
        void MoveIndex(int offset);
        void OpenThread(Uri uri);
        List<Message> MessageList { get; }
        string ThreadTitle { get; }
        IThread Thread { get; }
    }

    interface IApplicationView
    {
        void OnStateChange();
        void OnMoveForward();
    }


    class ApplicationLogic : IApplicationLogic
    {
        /// <summary>
        /// 残り表示時間。動作モードが Playing の時のみ意味を持つ？
        /// </summary>
        int m_DisplayMillisecondsRemaining = 0;

        /// <summary>
        /// 現在開かれているスレッド。Thread プロパティを通して公開される
        /// </summary>
        IThread m_Thread;
        public IThread Thread { get { return m_Thread; } }

        List<Message> m_MessageList;
        int m_MessageListIndex = 0;

        /// <summary>
        /// 動作モード。
        /// </summary>
        OperationMode m_OperationMode;
        List<IApplicationView> m_ApplicationViews;
        DateTime m_LastProviderUpdate;
        DateTime m_LastUpdate;

        private int ReloadInterval = 10;

        // コンストラクタ
        public ApplicationLogic()
        {
            m_ApplicationViews = new List<IApplicationView>();
            m_LastUpdate = DateTime.Now;
            m_LastProviderUpdate = DateTime.Now;
            // デフォルトの動作モードは Idle。
            m_OperationMode = OperationMode.Idle;
        }

        // オブザーバーパターン関連のメソッド
        // Attach, Detach, StateChange, MoveForward
        void IApplicationLogic.Attach(IApplicationView view)
        {
            m_ApplicationViews.Add(view);
        }
        void IApplicationLogic.Detach(IApplicationView view)
        {
            m_ApplicationViews.Remove(view);
        }
        private void NotifyStateChange()
        {
            foreach (IApplicationView i in m_ApplicationViews)
                i.OnStateChange();
        }
        private void NotifyMoveForward()
        {
            foreach (IApplicationView i in m_ApplicationViews)
                i.OnMoveForward();
        }

        public Message CurrentMessage
        {
            get
            {
                if (m_MessageList == null ||
                    m_MessageList.Count == 0 ||
                    m_MessageListIndex >= m_MessageList.Count)
                    return null;
                else
                    return m_MessageList[m_MessageListIndex];
            }
        }

        /// <summary>
        /// スレッドを開く。動作モードが何であっても意味を持つ。
        /// </summary>
        public void OpenThread(Uri uri)
        {
            try
            {
                m_Thread = BoardGetter.GetThread(uri);
                if (m_Thread.MessageList == null)
                    m_Thread.Reload();
                Debug.Assert(m_Thread.MessageList != null);
                m_MessageList = m_Thread.MessageList;
                // 最終メッセージの次に設定する。
                m_MessageListIndex = m_MessageList.Count;
                SwitchOperationMode(OperationMode.Waiting);
                NotifyStateChange();
            }
            catch (ApplicationException)
            {
                // 非対応URL
                SwitchOperationMode(OperationMode.Idle);
                NotifyStateChange();
            }
        }

        /// <summary>
        ///  現在のレスを一つ進めるあるいは戻す。
        /// </summary>
        /// <param name="offset">１あるいは-1</param>
        public void MoveIndex(int offset)
        {
            Debug.Assert(offset == 1 || offset == -1);

            int newIndex = m_MessageListIndex + offset;
            // 添え字が範囲内であれば
            if (newIndex <= m_MessageList.Count && 0 <= newIndex)
            {
                // 停止中でなければ停止中にする
                if (m_OperationMode != OperationMode.Paused)
                    SwitchOperationMode(OperationMode.Paused);

                // 設定する
                m_MessageListIndex = newIndex;
                if (CurrentMessage == null)
                    SwitchOperationMode(OperationMode.Waiting);

                NotifyStateChange();
            }
            Debug.WriteLine("message list index {0}/{1}", m_MessageListIndex, m_MessageList.Count - 1);
        }

        /// <summary>
        /// フォームからのタイマーのティックで定期的に呼び出される。
        /// Playing時、レスの表示送りなど。
        /// </summary>
        void IApplicationLogic.Update()
        {
            int delta = (DateTime.Now - m_LastUpdate).Milliseconds;
            // Debug.WriteLine("delta = {0}", delta);
            m_LastUpdate = DateTime.Now;

            //Debug.WriteLineIf(m_OperationMode == OperationMode.Playing,
            //    String.Format("あと {0}ミリ秒現在のレスを表示します", m_DisplayMillisecondsRemaining));
            //Debug.WriteLineIf(CurrentMessage == null, "現在のレスがありません。レス待機中");
            //Debug.WriteLineIf(m_OperationMode == OperationMode.Paused, "ポーズ中です。");

            switch (m_OperationMode)
            {
            case OperationMode.Idle:
                break;
            case OperationMode.Paused:
                ReloadThreadWhenTime();
                break;
            case OperationMode.Playing:
                ReloadThreadWhenTime();
                if (m_DisplayMillisecondsRemaining > 0)
                {
                    // Debug.WriteLine("残り表示時間を減算します。");
                    m_DisplayMillisecondsRemaining -= delta;
                }
                else
                {
                    Debug.Assert(CurrentMessage != null);
                    m_MessageListIndex++;
                    if (CurrentMessage != null)
                    {
                        m_DisplayMillisecondsRemaining = CalculateDisplayTime(CurrentMessage);
                        NotifyMoveForward();
                    }
                    else
                    {
                        SwitchOperationMode(OperationMode.Waiting);
                    }
                    NotifyStateChange();
                }
                break;
            case OperationMode.Stopped:
                ReloadThreadWhenTime();
                break;
            case OperationMode.Waiting:
                int newMessages = ReloadThreadWhenTime();
                if (newMessages > 0)
                {
                    SwitchOperationMode(OperationMode.Playing);
                    NotifyMoveForward();
                }
                break;
            default:
                throw new ApplicationException("logic error");
            }
        }

        /// <summary>
        /// 一定時間ごとにスレッドをリロードする。
        /// </summary>
        /// <returns>新着メッセージの数</returns>
        private int ReloadThreadWhenTime()
        {

            // １０秒ごとにスレをリロードする
            var diff = (DateTime.Now - m_LastProviderUpdate);
            if (diff.Seconds >= ReloadInterval)
            {
                var oldMessageCount = m_Thread.MessageList.Count;
                m_Thread.Reload();
                var newMessageCount = m_Thread.MessageList.Count;
                // 新着レスがあれば
                if (oldMessageCount < newMessageCount)
                {
                    // NotifyMoveForward();
                    NotifyStateChange();
                }
                m_LastProviderUpdate = DateTime.Now;
                return newMessageCount - oldMessageCount;
            }
            return 0;
        }

        /// <summary>
        /// レスを表示するのに必要な時間を計算してミリ秒単位で返す。
        /// </summary>
        private static int CalculateDisplayTime(Message Message)
        {
            Debug.Assert(Message != null);
            double charsPerSec = 11.6; // 7.25;
            string text = WebText.UnescapeHtml(Message.Body);

            double complexity = 0;
            char prevChar = '\0'; // これはたぶん絶対こない
            foreach (var c in text)
            {
                if (c != prevChar)
                {
                    if (Char.IsLetter(c))
                        complexity += 0.5;
                    else
                        complexity += 1;
                }
                prevChar = c;
            }
            var estimated = (int) (complexity / charsPerSec * 1000);

            estimated = Math.Max(estimated, Settings.Default.MinimumDisplayTime);
            estimated = Math.Min(estimated, Settings.Default.MaximumDisplayTime);
            return estimated;
        }

        /// <summary>
        /// 動作モードを切り替える
        /// </summary>
        private void SwitchOperationMode(OperationMode newOperationMode)
        {
            Debug.Print("Switching OperationMode from {0} to {1}", m_OperationMode, newOperationMode);
            m_OperationMode = newOperationMode;
            // 動作モードの切り替えを行う
            switch (newOperationMode)
            {
            case OperationMode.Paused:
                break;
            case OperationMode.Playing:
                Debug.Assert(CurrentMessage != null);
                m_DisplayMillisecondsRemaining = CalculateDisplayTime(CurrentMessage);
                break;
            case OperationMode.Idle:
                m_Thread = null;
                m_MessageList = null;
                break;
            case OperationMode.Stopped:
                break;
            case OperationMode.Waiting:
                break;
            default:
                Debug.WriteLine("pauseButton_Click: Unknown operation mode");
                break;
            }
            NotifyStateChange();
        }

        public string ThreadTitle
        {
            get
            {
                if (m_Thread != null)
                    return String.Format("{0}の{1}", m_Thread.Board.Name, m_Thread.Title);
                else
                    return "無題";
            }
        }

        public List<Message> MessageList
        {
            get
            {
                return m_MessageList;
            }
        }

        public int MessageListIndex
        {
            get
            {
                return m_MessageListIndex;
            }
            set
            {
                Debug.Assert(value >= 0 && value <= m_MessageList.Count);
                m_MessageListIndex = value;
                if (value == MessageList.Count)
                    SwitchOperationMode(OperationMode.Waiting);
                else
                    SwitchOperationMode(OperationMode.Paused);
                NotifyStateChange();
            }
        }

        public OperationMode CurrentOperationMode
        {
            get
            {
                return m_OperationMode;
            }
            set
            {
                SwitchOperationMode(value);
            }
        }

        //public void OnThreadStop(IMessageProvider sender)
        //{
        //    m_OperationMode = OperationMode.Stopped;
        //    NotifyStateChange();
        //}
    }
}
