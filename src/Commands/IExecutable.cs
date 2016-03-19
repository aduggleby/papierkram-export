using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Commands
{
    interface IExecutable
    {
        void Run(ILog log);
    }
}
