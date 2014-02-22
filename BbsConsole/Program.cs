using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yoteichi.Bbs;

namespace BbsConsole
{
    class Program
    {
        static Board m_Board;
        static Thread m_Thread;

        static void Main(string[] args)
        {
            CommandLoop();
        }

        static void CommandLoop()
        {
            string line;

            while (true)
            {
                ShowPrompt();
                
                line = Console.ReadLine();
                if (line == null)
                    line = "bye\n";

                string commandName;
                string[] args;
                ParseCommandLine(line, out commandName, out args);
                var result = ExecuteCommand(commandName, args);
                if (result == Result.Terminate)
                    break;
            }
        }

        enum Result
        {
            Continue,
            Terminate
        }
        /// <summary>
        /// コマンドを実行する。
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="args"></param>
        /// <returns>継続するならば false?</returns>
        private static Result ExecuteCommand(string commandName, string[] args)
        {
            switch (commandName)
            {
            case "echo":
                Echo(args);
                break;
            case "open":
                Open(args);
                break;
            case "list":
                List(args);
                break;
            case "read":
                Read(args);
                break;
            case "select":
                Select(args);
                break;
            case "bye":
            case "quit":
            case "exit":
                Bye(args);
                return Result.Terminate;
            default:
                Console.WriteLine("そんなコマンド知りません。");
                break;
            }
            return Result.Continue;
        }

        static void Bye(string[] args)
        {
            Console.WriteLine("バイバイ ﾉｼ");
            System.Threading.Thread.Sleep(2000);
        }

        static void ShowPrompt()
        {
            var thread = m_Thread == null ? "(null)" : m_Thread.Title;
            var board = m_Board == null ? "(null)" : m_Board.Title;
            Console.Write("[{0}@{1}]$ ", thread, board);
        }

        static void ParseCommandLine(string commandLine, out string commandName, out string[] args)
        {
            commandLine = commandLine.TrimEnd(null); // null 入れるとどうなるん
            var argv = Regex.Split(commandLine, @"\s+");

            args = new string[argv.Length - 1];
            Array.Copy(argv, 1, args, 0, argv.Length - 1);
            commandName = argv[0].ToLower();

            return;
        }

        static void Echo(string[] args)
        {
            foreach (var arg in args)
            {
                Console.Write(arg);
            }
            Console.WriteLine("");
        }

        static void Usage(string message)
        {
            string output = String.Format("Usage: {0}", message);
            Console.WriteLine(output);
        }

        static void Open(string[] args)
        {
            if (args.Length != 1)
            {
                Usage("open [url]");
                return;
            }
            string url = args[0];
            Uri uri;
            try
            {
                uri = new Uri(url);
            } catch (UriFormatException)
            {
                Console.WriteLine("URL がおかしいです。");
                return;
            }

            if (Service.IsThread(uri))
            {
                m_Thread = Service.GetThread(uri);
                Console.WriteLine("スレッド{0}を開きました。", m_Thread.Title);
            }
            else
            {
                m_Thread = null;
            }

            if (Service.IsBoard(uri))
            {
                m_Board = Service.GetBoard(uri);
                Console.WriteLine("板「{0}」を開きました。", m_Board.Title);
            }
        }

        // スレ一覧を表示する
        static void List(string[] args)
        {
            if (args.Length != 0)
            {
                Usage("list");
                return;
            }

            if (m_Board == null)
            {
                Console.WriteLine("板を開いてください。");
                return;
            }

            if (!m_Board.IsLoaded)
            {
                m_Board.Load();
            }

            foreach (Thread t in m_Board.Threads)
            {
                Console.WriteLine("{0} {1}", t.Number, t.Title);
            }
        }

        // スレッドを選択する
        static void Select(string[] args)
        {
            if (args.Length != 1)
            {
                Usage("select <スレ番号>");
            }

            Thread selected = m_Board.Threads.Find((Thread t)=>
                 t.Title == args[0]); // 完全マッチ
            if (selected == null)
            {
                selected = m_Board.Threads.Find((Thread t) =>
                 t.Title.Contains(args[0])); // 部分マッチ
            }

            if (selected == null)
            {
                Console.WriteLine("そんなスレないです");
            }
            else
            {
                m_Thread = selected;
                Console.WriteLine("スレ「{0}」が選択されました。", m_Thread.Title);
            }
        }

        static void Read(string[] args)
        {
            int begin = 1;
            int end = -1;

            if (!(args.Length >= 0))
            {
                Usage("read [読むレス番号]");
                return;
            }
            else if (args.Length == 1)
            {
                begin = args[0].ToInt();
                end = args[0].ToInt();
            }

            if (m_Thread == null)
            {
                Console.WriteLine("スレッドを選択してください。");
                return;
            }

            if (!m_Thread.IsLoaded)
                m_Thread.Load();
            
            foreach (Message m in m_Thread.Messages)
            {
                if (m.Number >= begin)
                {
                    Console.WriteLine("{0} {1} {2} {3}", m.Number, m.Name.UnescapeHtml(), m.Mail, m.Date);
                    Console.WriteLine("");
                    Console.WriteLine("{0}", m.Body.UnescapeHtml());
                    Console.WriteLine("");
                }

                if (end != -1 && m.Number >= end)
                    break;
            }
        }
    }
}
