using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// 適切な種類の板クラスをインスタンス化して返すGetメソッドを提供する。
    /// </summary>
    public class BoardGetter
    {
        static private List<IBoard> BoardPool;

        // 静的コンストラクタ
        static BoardGetter()
        {
            BoardPool = new List<IBoard>();
        }

        /// <summary>
        /// 板を取得。２度め以降は同じオブジェクトが返る。
        /// </summary>
        public static IBoard GetBoard(Uri uri)
        {
            IBoard board = CreateBoard(uri);
            Uri normalizedUri = board.TopUri;
            IEnumerable<IBoard> result = from i in BoardPool
                                         where i.TopUri == normalizedUri
                                         select i;
            Debug.Assert(result.Count() == 0 || result.Count() == 1);
            bool found = (result.Count() == 1);
            if (found)
            {
                return result.First();
            }
            else
            {
                BoardPool.Add(board);
                return board;
            }
        }

        /// <summary>
        /// string 版
        /// </summary>
        public static IBoard GetBoard(string uriString)
        {
            return GetBoard(new Uri(uriString));
        }

        /// <summary>
        /// スレッド一覧がロードされた状態の板を取得
        /// </summary>
        public static IBoard GetBoardLoaded(Uri uri)
        {
            var board = GetBoard(uri);
            if (board.ThreadList == null)
                board.Reload();
            return board;
        }

        // string版
        public static IBoard GetBoardLoaded(string uriString)
        {
            return GetBoardLoaded(new Uri(uriString));
        }

        // 板オブジェクトを作成する
        private static IBoard CreateBoard(Uri uri)
        {
            // 適切な板クラスを選択して、実体化する
            IBoard board = null;
            if (ShitarabaBoard.IsBoardUri(uri))
            {
                board = new ShitarabaBoard(uri);
            }
            else if (WaiwaiBoard.IsBoardUri(uri))
            {
                board = new WaiwaiBoard(uri);
            }
            else if (BintanBoard.IsBoardUri(uri))
            {
                board = new BintanBoard(uri);
            }
            else
                throw new ApplicationException("URLは対応した掲示板ではありません");

            return board;
        }


        // URIからスレッド番号を抜き出す
        public static int? GetThreadId(Uri uri)
        {
            // 適切な板クラスを選択して、実体化する
            int? id = null;
            if (ShitarabaBoard.IsBoardUri(uri))
            {
                id = ShitarabaBoard.GetThreadId(uri);
            }
            else if (WaiwaiBoard.IsBoardUri(uri))
            {
                id = WaiwaiBoard.GetThreadId(uri);
            }
            else if (BintanBoard.IsBoardUri(uri))
            {
                id = BintanBoard.GetThreadId(uri);
            }
            else
                throw new ApplicationException("URLは対応した掲示板のスレッドではありません");

            return id;
        }

        /// <summary>
        /// スレッドだけを取得。
        /// </summary>
        public static IThread GetThread(Uri uri)
        {
            int? id;

            id = GetThreadId(uri);

            if (id == null)
            {
                return null;
            }
            else
            {
                IBoard b = GetBoard(uri);
                if (b.ThreadList == null)
                    b.Reload();
                foreach (var t in b.ThreadList)
                {
                    if (t.Id == id)
                        return t;
                }
                return null;
            }
            throw new ApplicationException("LogicError");
        }

        // string版
        public static IThread GetThread(string uri)
        {
            return GetThread(new Uri(uri));
        }
    }
}
