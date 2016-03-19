using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Data
{
    class Invoice
    {
        public Customer Customer { get; set; }
        public Project Project { get; set; }

        public int ID { get; set; }

        public string Number { get; set; }
        public string Name { get; set; }

        public bool Paid { get; set; }

        public DateTime Date { get; set; }

        public DateTime Due { get; set; }

        public decimal Total { get; set; }

        public decimal Net { get; set; }

    }
}
