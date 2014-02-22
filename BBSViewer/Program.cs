using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace BBSViewer
{
    static class Program
    {
        /// <summary>
        /// Console と Debug 出力を蓄積する StringBuilder。
        /// </summary>
        static public StringBuilder LogText { get; private set; }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // コンソール出力とデバッグ出力を StringBuilder オブジェクト LogText にリダイレクトする。
            TextWriter textWriter;
            Console.SetOut(textWriter = new StringWriter(LogText = new StringBuilder()));
            Debug.Listeners.Add(new TextWriterTraceListener(textWriter));

            // 出力してみる。Release ビルドではデバッグ表示はない。
            Console.WriteLine("Logger started");
            Debug.WriteLine("First debug print", "INFO");

            // ボイラープレート。
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
