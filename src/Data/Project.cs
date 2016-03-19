using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Data
{
    class Project
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public Customer Customer { get; set; }
    }
}
