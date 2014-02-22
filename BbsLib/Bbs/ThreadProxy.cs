using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// Proxy
    /// </summary>
    public class ThreadProxy : IThread
    {
        int m_Id;
        string m_Title;
        int m_LatestMessageNumber;
        IBoard m_Board;
        IThread m_ThreadImp;

        // コンストラクタ
        public ThreadProxy(int threadNumber, string title, int latestMessageNumber, IBoard board)
        {
            m_Id = threadNumber;
            m_Title = title;
            m_LatestMessageNumber = latestMessageNumber;
            m_Board = board;
        }

        private IThread GetThreadImp()
        {
            return m_Board.CreateThread(m_Id, m_Title, m_LatestMessageNumber);
        }

        /// <summary>
        /// メッセージリスト
        /// </summary>
        public List<Message> MessageList
        {
            get
            {
                if (m_ThreadImp == null)
                    m_ThreadImp = GetThreadImp();
                return m_ThreadImp.MessageList;
            }
        }

        /// <summary>
        /// 最新レス番号？
        /// </summary>
        public int LatestMessageNumber
        {
            get
            {
                if (m_ThreadImp == null)
                    return m_LatestMessageNumber;
                else
                    return m_ThreadImp.LatestMessageNumber;
            }
        }

        /// <summary>
        /// スレタイ
        /// </summary>
        public string Title
        {
            get
            {
                if (m_ThreadImp == null)
                    return m_Title;
                else
                    return m_ThreadImp.Title;
            }
            set
            {
                if (m_ThreadImp == null)
                    m_ThreadImp = GetThreadImp();
                m_ThreadImp.Title = value;
            }
        }
        /// <summary>
        /// スレッドID番号
        /// </summary>
        public int Id
        {
            get
            {
                return m_Id;
            }
        }
        /// <summary>
        /// スレッドのURI。read.cgi
        /// </summary>
        public Uri Uri
        {
            // ここで実装を得るのはよくないかも。そうなるとコンストラクタにURIを渡してもらわないといけない
            get
            {
                if (m_ThreadImp == null)
                    m_ThreadImp = GetThreadImp();
                return m_ThreadImp.Uri;
            }
        }
        /// <summary>
        /// スレッドストップしているか
        /// </summary>
        public bool IsStopped
        {
            get
            {
                if (m_ThreadImp == null)
                    m_ThreadImp = GetThreadImp();
                return m_ThreadImp.IsStopped;
                //if (m_ThreadImp == null)
                //    return false;
                //else
                //    return m_ThreadImp.IsStopped;
            }
        }
        /// <summary>
        /// 新着レスを取得する
        /// </summary>
        public void Reload()
        {
            if (m_ThreadImp == null)
                m_ThreadImp = GetThreadImp();
            m_ThreadImp.Reload();
        }

        public IBoard Board
        {
            get
            {
                return m_Board;
            }
        }
    }
}
