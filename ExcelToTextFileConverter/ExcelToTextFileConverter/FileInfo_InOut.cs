using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToTextFileConverter
{

    internal class FileInfo_InOut
    {
        internal string FolderInput { get; set; }
        public string FolderOutput { get; set; }
        public string Prefix { get; set; }

        public string Format = "xlsx";
        public string FileName { get; set; }
        public string CsvFilePath { get; set; }
        public string TextFilePath { get; set; }

    }
}






