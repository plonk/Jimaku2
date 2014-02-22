using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Yoteichi;
using Yoteichi.Bbs;

namespace BBSViewer
{
    // アプリケーションの挙動を記述するクラス。フォームがメンバーとして持つ。
    // プロパティの変更を通知するために、INotifyPropertyChanged インターフェースを実装する。
    class ApplicationLogic : INotifyPropertyChanged
    {
        /// <summary>
        /// 現在開かれている板。
        /// </summary>
        public IBoard Board
        {
            get { return m_Board; }
            set { m_Board = value; NotifyPropertyChanged(); }
        }
        private IBoard m_Board;
        /// <summary>
        /// 現在開かれているスレ。
        /// </summary>
        public IThread Thread
        {
            get { return m_Thread; }
            set { m_Thread = value; NotifyPropertyChanged(); }
        }
        private IThread m_Thread;
        /// <summary>
        /// 現在開かれているレス。
        /// </summary>
        public Message Message
        {
            get { return m_Message; }
            set { m_Message = value; NotifyPropertyChanged(); }
        }
        private Message m_Message;

        // コンストラクタ
        public ApplicationLogic()
        {
        }

        // エラーを通知する。フォームはこのイベントにコールバックを設定して、
        // メッセージボックスを表示するだろう。
        void NotifyError(string message, string caption)
        {
            if (ErrorOccured != null)
                ErrorOccured(this, new ErrorOccuredEventArgs(message, caption));
        }

        // 板を開く
        public async Task OpenBoardAsync(string boardUri)
        {
            try
            {
                Board = null;
                Thread = null;
                Message = null;

                var board = BoardGetter.GetBoardLoaded(boardUri);
                Board = board;
            }
            catch (Exception e)
            {
                var message = String.Format("板が開けません。{0}", e.Message);
                NotifyError(message, "エラー");
            }
        }

        // スレを選択する
        public async void SelectThread(string threadId)
        {
            Console.WriteLine("SelectThread: {0}", threadId);

            Thread = null;
            Message = null;
            foreach (var thread in Board.ThreadList)
            {
                if (thread.Id == threadId.ToInt())
                {
                    // thread.Reload();
                    Thread = thread;
                    return;
                }
            }
        }

        /// <summary>
        /// レスを選択する
        /// </summary>
        public void SelectMessage(string messageNumber)
        {
            foreach (var msg in Thread.MessageList)
            {
                if (msg.Number == messageNumber.ToInt())
                {
                    Message = msg;
                    return;
                }
            }
        }

        /// <summary>
        /// プロパティの変更を通知する。
        /// 呼び出し元の setter の名前が propertyName に入る。
        /// </summary>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // スレをリロードする。
        public async Task ReloadThread()
        {
            Thread.Reload();
            NotifyPropertyChanged("Thread");
        }

        // レスを選択されたスレに投下する
        public void PostMessage(string name, string mail, string message)
        {
            // struct をフィルアップする。このやり方はあんまり良くないな
            var msg = new Message(-1, // レス番
                name, // 投稿者名
                mail, // メールアドレス
                null, // 投稿者ID
                null, // 日付
                message); // メッセージ
            try
            {
                m_Board.Post(msg, Thread.Id);
            }
            catch (ApplicationException e)
            {
                NotifyError(e.Message, "エラー");
            }
        }


        /// <summary>
        /// エラーが起こった時に発生するイベント
        /// 板、スレを開くことやレスの投稿が失敗した時に発生します。
        /// （こんなの要るのか・・・？）
        /// </summary>
        public event ErrorOccuredEventHandler ErrorOccured;
        /// <summary>
        /// プロパティが変更された時に発生するイベント。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
