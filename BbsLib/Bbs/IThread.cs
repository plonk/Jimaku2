using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// スレッドクラスのインターフェース
    /// </summary>
    public interface IThread
    {
        /// <summary>
        /// メッセージリスト
        /// </summary>
        List<Message> MessageList { get; }
        /// <summary>
        /// 最新レス番号？
        /// </summary>
        int LatestMessageNumber { get; }
        /// <summary>
        /// スレタイ
        /// </summary>
        string Title { get; set; }
        /// <summary>
        /// スレッドID番号
        /// </summary>
        int Id { get; }
        /// <summary>
        /// スレッドのURI。read.cgi
        /// </summary>
        Uri Uri { get; }
        /// <summary>
        /// スレッドストップしているか
        /// </summary>
        bool IsStopped { get; }
        /// <summary>
        /// 新着レスを取得する
        /// </summary>
        void Reload();
        IBoard Board { get; }
    }
}
