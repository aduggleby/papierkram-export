using CommandLine;
using PapierkramExport.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = CommandLine.Parser.Default.ParseArguments<CommandBase, TestLogin, Invoices, Projects, ActiveTasks>(args);
            bool wait = false;
            ILog log = null;
            try
            {
                r.WithParsed<CommandBase>(b =>
                {
                    log = new Log(b.Verbose);
                    wait = b.WaitForEnter;
                });
                r.WithParsed<TestLogin>(o => o.Run(log));
                r.WithParsed<Invoices>(o => o.Run(log));
                r.WithParsed<Projects>(o => o.Run(log));
                r.WithParsed<ActiveTasks>(o => o.Run(log));

            }
            catch (Exception ex)
            {
                var saved = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = saved;
            }

            if (wait)
            {
                Console.WriteLine("Press <enter> to exit.");
                Console.ReadKey();
            }
        }
    }

    class Log : ILog
    {
        public enum Level
        {
            Verbose, Info, Warn, Error
        }

        public Dictionary<Level, ConsoleColor> Colors = new Dictionary<Level, ConsoleColor>();
        private bool m_verbose = false;

        public Log(bool verbose = false)
        {
            m_verbose = verbose;
            Colors.Add(Level.Verbose, ConsoleColor.Gray);
            Colors.Add(Level.Warn, ConsoleColor.Magenta);
            Colors.Add(Level.Info, ConsoleColor.White);
            Colors.Add(Level.Error, ConsoleColor.Red);
        }

        private void Output(Level l, string txt, params object[] arr)
        {
            if (l != Level.Verbose || m_verbose)
            {
                Console.ResetColor();
                Console.ForegroundColor = Colors[l];
                Console.WriteLine(string.Format("[{0}] {1}: {2}",
                    string.Join("", l.ToString().ToUpper().Take(3)).PadRight(3),
                    DateTime.Now.ToString("HH:mm:ss"), 
                    
                    string.Format(txt, arr)));

                Console.ResetColor();
            }
        }

        public void Error(string txt)
        {
            Error(txt, new string[0]);
        }

        public void Error(string txt, params object[] arr)
        {
            Output(Level.Error, txt, arr);
        }

        public void Info(string txt)
        {
            Info(txt, new string[0]);
        }

        public void Info(string txt, params object[] arr)
        {
            Output(Level.Info, txt, arr);
        }

        public void Verbose(string txt)
        {
            Verbose(txt, new string[0]);
        }

        public void Verbose(string txt, params object[] arr)
        {
            Output(Level.Verbose, txt, arr);
        }

        public void Warn(string txt)
        {
            Warn(txt, new string[0]);
        }

        public void Warn(string txt, params object[] arr)
        {
            Output(Level.Warn, txt, arr);
        }
    }
}
