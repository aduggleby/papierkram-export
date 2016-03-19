using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Data
{
    class Task
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public Project Project { get; set; }
        public Customer Customer { get; set; }

        public DateTime Due { get; set; }

    }
}
