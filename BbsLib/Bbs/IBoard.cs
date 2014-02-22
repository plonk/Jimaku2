using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// 板クラスのインターフェース
    /// </summary>
    public interface IBoard
    {
        /// <summary>
        /// 板名
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 板トップ
        /// </summary>
        Uri TopUri { get; }
        /// <summary>
        /// スレッドリスト。実際には ThreadProxy のリスト
        /// </summary>
        List<ThreadProxy> ThreadList { get; }
        /// <summary>
        /// レスを投稿する
        /// </summary>
        void Post(Message message, int threadId);
        /// <summary>
        /// スレッドリストをリロードする
        /// </summary>
        void Reload();
        /// <summary>
        /// 対応する具象スレッドクラスをインスタンス化する
        /// </summary>
        IThread CreateThread(int threadId, string title, int latestResNumber);
    }
}
