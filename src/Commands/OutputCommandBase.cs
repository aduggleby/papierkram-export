using CommandLine;
using PapierkramExport.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace PapierkramExport
{
    class OutputCommandBase<T> : CommandBase
    {
        readonly static JsonSerializerSettings s_serializerSettings;

        static OutputCommandBase()
        {
            s_serializerSettings = new JsonSerializerSettings();
            s_serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            s_serializerSettings.TypeNameHandling = TypeNameHandling.None;
            s_serializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;

            JsonConvert.DefaultSettings = () => s_serializerSettings;
        }

        public OutputCommandBase()
        {
        }

        private FileInfo m_outputFileInfo;
        protected FileInfo OutputFileInfo
        {
            get
            {
                if (m_outputFileInfo == null)
                {
                    FileInfo fi;
                    try
                    {
                        fi = new FileInfo(OutputFile);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Invalid file name format: '{0}' - Error: {1}",
                            OutputFile,
                            ex.Message));
                    }

                    if (!Overwrite && fi.Exists)
                    {
                        throw new Exception(string.Format("File {0} already exists. Use -x to overwrite file.",
                            fi.FullName));
                    }

                    if (Overwrite && fi.Exists)
                    {
                        try
                        {
                            fi.Delete();
                            Log.Warn("Overwrite was specified, so deleting file: " + fi.FullName);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Could not delete file: {0}. Maybe the file is open in another program? Error: {1}",
                                fi.FullName,
                                ex.Message));
                        }
                    }

                    m_outputFileInfo = fi;


                }
                return m_outputFileInfo;
            }
        }

        [Option('o', "output", Required = true,
         HelpText = "Output file to be written to.")]
        public string OutputFile { get; set; }

        [Option('f', "format", Required = false,
         HelpText = "Either csv (default) or json.")]
        public FormatType Format { get; set; }

        [Option('s', "seperator", Required = false,
            HelpText = "Write the CSV seperator as the first line to the output")]
        public bool WriteSeperator { get; set; }

        [Option('c', "char", Required = false, Default = 's',
            HelpText = "Define the seperator s(emicolon) (default), c(comma) or t(ab)")]
        public char SeperatorCharDef { get; set; }

        protected char SeperatorChar
        {
            get
            {
                switch (SeperatorCharDef)
                {
                    case 'c':
                        return ';';
                        break;
                    case 't':
                        return '\t';
                        break;
                    case 's':
                    default:
                        return ',';
                        break;
                }
            }
        }

        protected string SeperatorCharDescription
        {
            get
            {
                switch (SeperatorCharDef)
                {
                    case 'c':
                        return "comma";
                    case 't':
                        return "tab";
                    case 's':
                    default:
                        return "semicolon";
                }
            }
        }

        [Option('x', "overwrite", Required = false,
            HelpText = "Overwrite if file exists")]
        public bool Overwrite { get; set; }

        private int m_csvCounter = 0;

        public virtual void WriteOutput(IEnumerable<T> ts)
        {

            Log.Verbose("Writing output file {0} (Format: {1}{2}) ", 
                OutputFileInfo.FullName, 
                Format.ToString(),
                Format == FormatType.csv ? " , Seperator: " + SeperatorCharDescription : string.Empty);
            switch (Format)
            {
                case FormatType.csv:

                    if (WriteSeperator)
                    {
                        Log.Warn("Excel currently does not support UTF8 BOM and the seperator line at the same time.");
                        Log.Warn("It is recommended to use parameter -c to define the list seperator used in your culture/regional settings.");
                    }
                    WriteCSV(ts);
                    break;

                case FormatType.json:
                    if (WriteSeperator)
                    {
                        Log.Warn("You have specified the -s parameter, which is not applicable in your chosen output format json.");
                    }
                    if (SeperatorCharDef != 'c')
                    {
                        Log.Warn("You have specified the -c parameter, which is not applicable in your chosen output format json.");
                    }
                    WriteJSON(ts);
                    break;
            }

            Log.Verbose("File {0} (Format: {1}{2}){3} written.", 
                OutputFileInfo.FullName, 
                Format.ToString(), 
                Format == FormatType.csv ? " , Seperator: " + SeperatorCharDescription : string.Empty,
                Format == FormatType.csv ? string.Format(" with {0} lines", m_csvCounter) : string.Empty);
        }

        protected virtual void AppendLineToOutput(string o)
        {
            using (StreamWriter sw = new StreamWriter(OutputFileInfo.FullName, true, new UTF8Encoding(true)))
            {
                sw.WriteLine(o);
            }
        }

        protected virtual void WriteCSV(IEnumerable<T> ts)
        {

            if (WriteSeperator)
            {
                AppendLineToOutput("sep=" + SeperatorChar);
            }
            m_csvCounter++;
            WriteCSVHeader();
            ts.ToList().ForEach(WriteCSVLineWrapper);
        }


        protected virtual void WriteCSVHeader()
        {

        }


        private void WriteCSVLineWrapper(T t)
        {
            m_csvCounter++;
            Log.Ping();
            WriteCSVLine(t);
        }

        protected virtual void WriteCSVLine(T t)
        {

        }

        protected virtual void WriteJSON(IEnumerable<T> ts)
        {
            AppendLineToOutput(JsonConvert.SerializeObject(ts));
        }
    }

    public enum FormatType
    {
        csv,
        json
    }
}
