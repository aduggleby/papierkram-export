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
    [Verb("invoices", HelpText = "Get's all invoices")]
    class Invoices : OutputCommandBase<Invoice>, IExecutable
    {

        public void Run(ILog log)
        {
            Log = log;
            base.ExecuteLogin();

            var invoices = new List<Invoice>();

            WithCookieHttpClient(client =>
            {
                var url = WithTenantUrl("einnahmen/rechnungen?f=record_state_all,year_all");

                while (url != null)
                {
                    Log.Verbose("HTTP GET: " + url);
                    var response = client.GetStringAsync(url);
                    var responseString = response.Result;

                    var parser = new HtmlParser();
                    var doc = parser.Parse(responseString);

                    foreach (var invoice in doc.QuerySelectorAll("[data-record-type=income_invoice]"))
                    {
                        invoices.Add(InvoiceParser(invoice));
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

                Log.Info("Found " + invoices.Count() + " invoices.");

                base.WriteOutput(invoices);
            });
        }

        protected override void WriteCSVHeader()
        {
            AppendLineToOutput("Invoice ID;Invoice Number;Invoice Date;Invoice Due;Invoice Total;Invoice Net;Invoice Subject;Project ID;Project Name;Customer ID;Customer Name".Replace(';', SeperatorChar));

        }

        protected override void WriteCSVLine(Invoice i)
        {
            AppendLineToOutput(string.Format("{0};{1};{2};{3};{4};{5};\"{6}\";{7};\"{8}\";{9};\"{10}\"".Replace(';', SeperatorChar),
                i.ID,
                i.Number,
                i.Date.ToString("yyyy-MM-dd"),
                i.Due.ToString("yyyy-MM-dd"),
                i.Total.ToString(CultureInfo.GetCultureInfo("en")),
                i.Net.ToString(CultureInfo.GetCultureInfo("en")),
                i.Name,
                i.Project == null ? string.Empty : i.Project.ID.ToString(),
                i.Project == null ? string.Empty : i.Project.Name,
                i.Customer == null ? string.Empty : i.Customer.ID.ToString(),
                i.Customer == null ? string.Empty : i.Customer.Name
                ));
        }

        protected Invoice InvoiceParser(IElement row)
        {
            var invoice = new Invoice();

            invoice.ID = Int32.Parse(row.GetAttribute("data-record-id"));

            var cells = row.QuerySelectorAll("td");

            var numberAndSubject = cells[3];
            invoice.Number = numberAndSubject.QuerySelectorAll("a").First().InnerHtmlDecoded();
            invoice.Name = numberAndSubject.QuerySelectorAll("div>em").First().InnerHtmlDecoded();

            var projectAndCustomer = cells[4];
            var customerNode = projectAndCustomer.QuerySelectorAll("a").First();
            var customerName = customerNode.InnerHtmlDecoded();
            var customerID = Int32.Parse(customerNode.GetAttribute("href").Split('/').Last());

            string projectName;
            int projectID;

            Log.Info("Found Invoice {0}: [{1}] {2}", invoice.ID, invoice.Number.PadRight(15), invoice.Name);

            var secondNode = projectAndCustomer.QuerySelectorAll("div>a");
            if (secondNode.Any())
            {

                // invoice has a project assigned.
                var projectNode = secondNode.First();
                projectName = projectNode.InnerHtmlDecoded();
                projectID = Int32.Parse(projectNode.GetAttribute("href").Split('/').Last());

                Log.Verbose("for customer {0} ({1}) and project {2} ({3})", customerName, customerID, projectName, projectID);

                var project = new Project()
                {
                    Name = projectName,
                    ID = projectID
                };

                var customer = new Customer()
                {
                    Name = customerName,
                    ID = customerID
                };

                invoice.Project = project;
                invoice.Customer = customer;
            }
            else
            {
                //// the first node was the customer not the project, so correct it.
                //customerName = projectName;
                //projectName = null;
                //customerID = projectID;
                //projectID = 0;

                Log.Verbose("for customer {0} ({1}) (no project assigned)", customerName, customerID);

                var customer = new Customer()
                {
                    Name = customerName,
                    ID = customerID
                };

                invoice.Customer = customer;

            }
            var DE_CULTURE = CultureInfo.GetCultureInfo("de");

            var paid = cells[5];
            invoice.Paid = projectAndCustomer.QuerySelectorAll("span.label-success").Any();

            var d = cells[6].InnerHtmlDecoded().Trim();
            if (!string.IsNullOrWhiteSpace(d))
            {
                invoice.Date = DateTime.ParseExact(d, "dd.MM.yyyy", DE_CULTURE);
            }

            var d2 = cells[7].InnerHtmlDecoded().Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).First().Trim();
            if (!string.IsNullOrWhiteSpace(d2))
            {
                invoice.Due = DateTime.ParseExact(d2, "dd.MM.yyyy", DE_CULTURE);
            }

            var euros = cells[8].TextContent.Replace("€", string.Empty).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            invoice.Total = decimal.Parse(euros[0].Trim(), DE_CULTURE);
            invoice.Net = decimal.Parse(euros[1].Trim(), DE_CULTURE);

            Log.Verbose("Invoiced on {0}. Due on {1} for EUR {2} (Net: EUR {3})",
                invoice.Date.ToShortDateString(),
                invoice.Due.ToShortDateString(),
                invoice.Total,
                invoice.Net);


            return invoice;
        }
    }
}
