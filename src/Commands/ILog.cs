using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Commands
{
    interface ILog
    {
        void Ping();

        void Verbose(string txt);
        void Verbose(string txt, params object[] arr);

        void Warn(string txt);
        void Warn(string txt, params object[] arr);

        void Info(string txt);
        void Info(string txt, params object[] arr);

        void Error(string txt);
        void Error(string txt, params object[] arr);
    }
}
