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
    [Verb("projects", HelpText = "Get's all projects")]
    class Projects : OutputCommandBase<Project>, IExecutable
    {
        public void Run(ILog log)
        {
            Log = log;
            base.ExecuteLogin();

            var projects = new List<Project>();

            WithCookieHttpClient(client =>
            {
                var url = WithTenantUrl("projekte?f=record_state_all");

                while (url != null)
                {
                    Log.Verbose("HTTP GET: " + url);
                    var response = client.GetStringAsync(url);
                    var responseString = response.Result;

                    var parser = new HtmlParser();
                    var doc = parser.Parse(responseString);

                    foreach (var project in doc.QuerySelectorAll("[data-record-type=project]"))
                    {
                        projects.Add(ProjectParser(project));
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

                Log.Info("Found " + projects.Count() + " projects.");

                base.WriteOutput(projects);
            });
        }

        protected override void WriteCSVHeader()
        {
            AppendLineToOutput("Project ID;Project Name;Customer ID;Customer Name".Replace(';', SeperatorChar));
        }

        protected override void WriteCSVLine(Project p)
        {
            AppendLineToOutput(string.Format("{0};\"{1}\";{2};\"{3}\"".Replace(';', SeperatorChar),
                p.ID,
                p.Name,
                p.Customer.ID,
                p.Customer.Name
                ));
        }

        protected Project ProjectParser(IElement row)
        {
            var project = new Project();

            project.ID = Int32.Parse(row.GetAttribute("data-record-id"));

            var cells = row.QuerySelectorAll("td");

            var nameAndCustomer = cells[4];
            project.Name = nameAndCustomer.QuerySelectorAll("a").First().InnerHtmlDecoded();
            var customer = nameAndCustomer.QuerySelectorAll("div>a").First();
            project.Customer = new Customer();
            project.Customer.Name = customer.InnerHtmlDecoded();
            project.Customer.ID = Int32.Parse(customer.GetAttribute("href").Split('/').Last());

            Log.Info("Found project {0}: {2} {3} - {1}", project.ID, project.Name, project.Customer.Name, project.Customer.ID);

            return project;
        }
    }
}
