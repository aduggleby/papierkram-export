using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Commands
{
    [Verb("testlogin", HelpText = "Test if credentials work when logging in to papierkram.de.")]
    class TestLogin : CommandBase, IExecutable
    {
        public void Run(ILog log)
        {
            Log = log;
            log.Verbose("Using FQDN: " + base.TenantUrl);
            base.ExecuteLogin();
        }
    }
}
