# Papierkram.de CLI Export

_Note: This projected is not affiliated with or endorsed by odacer finanzsoftware GmbH  (makers of papierkram.de). Use at your own risk._

A command line tool for exporting projects, tasks and invoices from your papierkram.de account. Written in C# with a Visual Studio 2015 project.

[Download binary for Windows here](https://github.com/aduggleby/papierkram-export/raw/master/dist/PapierkramExport.zip)

## Usage

### Login

Being by testing login if your credentials are correct.

    PapierkramExport.exe testlogin  -u your@email.com -p secretpassword -d companyname
    
Parameter -d is your companies domain (the part in front of .papierkram.de) when you login.
If login is successfull a message can be seen otherwise an error will be output.

### Invoices, Projects, Tasks, Active Tasks

You can export a CSV of all invoices, all projects and all tasks.

    PapierkramExport.exe invoices  -u your@email.com -p secretpassword -d companyname -o invoices.csv
    PapierkramExport.exe projects  -u your@email.com -p secretpassword -d companyname -o projects.csv
    PapierkramExport.exe tasks  -u your@email.com -p secretpassword -d companyname -o tasks.csv
    
### Additional options

There are a couple of extra options:

**Overwrite the output file if it exists**

    -x 
    
**Wait for <enter> after finishing processing**

    -w
    
    

#### Format 

**Format CSV (default)**

	-f csv

**Format JSON**

	-f json


#### CSV Options

**CSV Separator character: comma**

    -c c
    
**CSV Separator character: semicolon**

    -c s
    
**CSV Separator character: tab**

    -c t
    
**Write seperator line to file**

Tells Excels which is the seperator char if it does not match your regional settings. WARNING: Excel does not read UTF8 characters correctly when the seperator line is defined (see [here](http://stackoverflow.com/questions/20395699/sep-statement-breaks-utf8-bom-in-csv-file-which-is-generated-by-xsl)).

    -w 
    
## Troubleshooting

If you run in to any problems, you can attach the --verbose parameter to get further information or register an issue in this repository.

    PapierkramExport.exe invoices  -u your@email.com -p secretpassword -d companyname -o projects.csv --verbose


