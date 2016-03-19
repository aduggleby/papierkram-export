# Papierkram.de CLI Export

_Note: This projected is not affiliated with or endorsed by odacer finanzsoftware GmbH  (makers of papierkram.de). Use at your own risk._

A command line tool for exporting projects, tasks and invoices from your papierkram.de account. Written in C# with a Visual Studio 2015 project.

## Usage

### Login

Being by testing login if your credentials are correct.

    PapierkramExport.exe testlogin  -u your@email.com -p secretpassword -d companyname
    
Parameter -d is your companies domain (the part in front of .papierkram.de) when you login.
If login is successfull a message can be seen otherwise an error will be output.

### Invoices, Projects, Tasks

You can export a CSV of all invoices, all projects and ACTIVE tasks (note: at the moment the tool only export ACTIVE tasks - a major different to projects and invoices where we export ALL items).

    PapierkramExport.exe invoices  -u your@email.com -p secretpassword -d companyname -o projects.csv
    PapierkramExport.exe projects  -u your@email.com -p secretpassword -d companyname -o projects.csv
    PapierkramExport.exe activetasks  -u your@email.com -p secretpassword -d companyname -o tasks.csv
    
## Troubleshooting

If you run in to any problems, you can attach the --verbose parameter to get further information or register an issue in this repository.

    PapierkramExport.exe invoices  -u your@email.com -p secretpassword -d companyname -o projects.csv --verbose


