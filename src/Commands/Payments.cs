using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using CommandLine;
using CommandLine.Text;
using PapierkramExport.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PapierkramExport.Commands
{
    [Verb("payments", HelpText = "Get's all payments")]
    class Payments : OutputCommandBase<Payment>, IExecutable
    {
        public void Run(ILog log)
        {
            Log = log;
            base.ExecuteLogin();

            var payments = new List<Payment>();

            WithCookieHttpClient(client =>
            {
                var url = WithTenantUrl("einnahmen/zahlungseingaenge/gebucht");

                while (url != null)
                {
                    Log.Verbose("HTTP GET: " + url);
                    var response = client.GetStringAsync(url);
                    var responseString = response.Result;

                    var parser = new HtmlParser();
                    var doc = parser.Parse(responseString);

                    foreach (var payment in doc.QuerySelectorAll("[data-record-type=income_receipt]"))
                    {
                        payments.Add(PaymentParser(payment));
                    }

                    var more = doc.QuerySelectorAll("a[rel=next]");
                    if (more.Any())
                    {
                        url = WithTenantUrl(more.First().GetAttribute("href"));
                        Log.Verbose("Next Page: " + url);
                    }
                    else
                    {
                        url = null;
                    }
                }

                Log.Info("Found " + payments.Count() + " payments.");

                base.WriteOutput(payments);
            });
        }

        protected override void WriteCSVHeader()
        {
            AppendLineToOutput("Invoice ID,Payment Date,Amount Paid".Replace(';', SeperatorChar));
        }

        protected override void WriteCSVLine(Payment p)
        {
            AppendLineToOutput(string.Format("{0};\"{1}\";{2};\"{3}\"".Replace(';', SeperatorChar),
                p.InvoiceID,
                p.PaymentDate.ToString("yyyy-MM-dd"),
                p.Amount.ToString(CultureInfo.GetCultureInfo("en"))                
                ));
        }

        protected Payment PaymentParser(IElement row)
        {
            var DE_CULTURE = CultureInfo.GetCultureInfo("de");
            
            var payment = new Payment();
            
            var cells = row.QuerySelectorAll("td");

            var invoice = cells[1];
            payment.InvoiceID = Int32.Parse(invoice.QuerySelectorAll("a").First().GetAttribute("href").Split('/').Last());

            
            var d = cells[3].InnerHtmlDecoded().Trim();
            if (!string.IsNullOrWhiteSpace(d))
            {
                payment.PaymentDate = DateTime.ParseExact(d, "dd.MM.yyyy", DE_CULTURE);
            }

            var euros = cells[5].TextContent.Replace("€", string.Empty).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            payment.Amount = decimal.Parse(euros[0].Trim(), DE_CULTURE);
            
            Log.Info("Found payment on {0} for invoice {1}: {2}", payment.PaymentDate, payment.InvoiceID, payment.Amount);

            return payment;
        }
    }
}
