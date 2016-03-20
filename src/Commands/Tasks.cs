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
    [Verb("activetasks", HelpText = "Get's all ACTIVE tasks")]
    class ActiveTasks : OutputCommandBase<Data.Task>, IExecutable
    {
        public void Run(ILog log)
        {
            Log = log;
            base.ExecuteLogin();

            var tasks = new List<Data.Task>();

            WithCookieHttpClient(client =>
            {
                var url = WithTenantUrl("zeiterfassung/aufgaben?f=record_state_active");

                while (url != null)
                {
                    Log.Verbose("HTTP GET: " + url);
                    var response = client.GetStringAsync(url);
                    var responseString = response.Result;

                    var parser = new HtmlParser();
                    var doc = parser.Parse(responseString);

                    foreach (var task in doc.QuerySelectorAll("[data-record-type=tracker_task]"))
                    {
                        tasks.Add(TaskParser(task));
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

                Log.Info("Found " + tasks.Count() + " tasks.");

                base.WriteOutput(tasks);
            });
        }

        protected override void WriteCSVHeader()
        {
            AppendLineToOutput("Task ID;Task Name;Task Due;Project ID;Project Name;Customer ID;Customer Name".Replace(';', SeperatorChar));
        }

        protected override void WriteCSVLine(Data.Task i)
        {
            AppendLineToOutput(string.Format("{0};{1};{2};{3};\"{4}\";{5};\"{6}\"".Replace(';', SeperatorChar),
                i.ID,
                i.Name,
                i.Due.ToString("yyyy-MM-dd"),
                i.Project == null ? string.Empty : i.Project.ID.ToString(),
                i.Project == null ? string.Empty : i.Project.Name,
                i.Customer == null ? string.Empty : i.Customer.ID.ToString(),
                i.Customer == null ? string.Empty : i.Customer.Name
                ));
        }
        
        protected Data.Task TaskParser(IElement row)
        {
            var task = new Data.Task();

            task.ID = Int32.Parse(row.GetAttribute("data-record-id"));

            var cells = row.QuerySelectorAll("td");

            var nameAndCustomer = cells[3];
            task.Name = nameAndCustomer.QuerySelectorAll("a").First().InnerHtmlDecoded();

            var projectAndCustomer = cells[4];
            var projectNode = projectAndCustomer.QuerySelectorAll("a").First();

            task.Project = new Project();
            task.Project.ID = Int32.Parse(projectNode.GetAttribute("href").Split('/').Last());
            task.Project.Name = projectNode.InnerHtmlDecoded();


            var customerNode = projectAndCustomer.QuerySelectorAll("div>a").FirstOrDefault();
            if (customerNode != null)
            {
                task.Project.Customer = new Customer();
                task.Project.Customer.ID = Int32.Parse(customerNode.GetAttribute("href").Split('/').Last());
                task.Project.Customer.Name = customerNode.InnerHtmlDecoded();
                task.Customer = task.Project.Customer;

                Log.Info("Found task {0}: {3}({2}) {5}({4}) - {1}", task.ID, task.Name, task.Project.ID, task.Project.Name, task.Project.Customer.ID, task.Project.Customer.Name);

            }
            else
            {
                task.Customer = new Customer();
                task.Customer.ID = task.Project.ID;
                task.Customer.Name = task.Project.Name;

                task.Project = null;

                Log.Info("Found task {0}: {3}({2}) - {1}", task.ID, task.Name, task.Customer.ID, task.Customer.Name);

            }

            var DE_CULTURE = CultureInfo.GetCultureInfo("de");

            var d = cells[6].TextContent.Trim().TrimStart('-');
            if (!string.IsNullOrWhiteSpace(d))
            {
                task.Due = DateTime.ParseExact(d, "dd.MM.yyyy", DE_CULTURE);
            }


            return task;
        }
    }
}
