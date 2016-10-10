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
    [Verb("tasks", HelpText = "Get's all tasks")]
    class Tasks : OutputCommandBase<Data.Task>, IExecutable
    {
        protected virtual string TaskUrl
        {
            get
            {
                return "zeiterfassung/aufgaben?f=record_state_all";
            }
        }

        protected virtual string ActiveTaskUrl
        {
            get
            {
                return "zeiterfassung/aufgaben?f=record_state_active";
            }
        }

        public void Run(ILog log)
        {
            Log = log;
            base.ExecuteLogin();

            var tasks = GetTasks(TaskUrl);

            // there is no archived indicator on the task view so we have to get
            // the active view and do a delta
            var activeTasks = GetTasks(ActiveTaskUrl);

            foreach(var task in tasks)
            {
                task.Archived = true;
                if (activeTasks.Any(x => x.ID == task.ID))
                {
                    task.Archived = false;
                }
            }

            Log.Info("Found " + tasks.Count() + " tasks.");

            base.WriteOutput(tasks);
        }

        private List<Data.Task> GetTasks(string u)
        {
            var tasks = new List<Data.Task>();

            WithCookieHttpClient(client =>
            {
                var url = WithTenantUrl(u);

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


            });
            return tasks;
        }

        protected override void WriteCSVHeader()
        {
            AppendLineToOutput("Task ID;Task Name;Task Due;Task Archived;Project ID;Project Name;Customer ID;Customer Name".Replace(';', SeperatorChar));
        }

        protected override void WriteCSVLine(Data.Task i)
        {
            AppendLineToOutput(string.Format("{0};{1};{2};{3};{4};\"{5}\";{6};\"{7}\"".Replace(';', SeperatorChar),
                i.ID,
                i.Name,
                i.Due==null?string.Empty :i.Due.Value.ToString("yyyy-MM-dd"),
                i.Archived.ToString().ToUpper(),
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

            var debug = row.InnerHtml;

            var cells = row.QuerySelectorAll("td");

            var nameAndCustomer = cells[3];
            task.Name = nameAndCustomer.QuerySelectorAll("a").First().InnerHtmlDecoded();

            var projectAndCustomer = cells[4];
            var customerNode = projectAndCustomer.QuerySelectorAll("a").First();
            
            task.Customer = new Customer();
            task.Customer.ID = Int32.Parse(customerNode.GetAttribute("href").Split('/').Last());
            task.Customer.Name = customerNode.InnerHtmlDecoded();

            var projectNode = projectAndCustomer.QuerySelectorAll("div>a").FirstOrDefault();
            if (projectNode != null)
            {
                task.Project = new Project();
                task.Project.ID = Int32.Parse(projectNode.GetAttribute("href").Split('/').Last());
                task.Project.Name = projectNode.InnerHtmlDecoded();

                task.Project.Customer = task.Customer;

                Log.Info("Found task {0}: {3}({2}) {5}({4}) - {1}", task.ID, task.Name, task.Project.ID, task.Project.Name, task.Project.Customer.ID, task.Project.Customer.Name);
            }
            else
            {
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
