using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExcelToTextFileConverter
{
    internal partial class Program
    {
        public static ConsoleColor Green { get; private set; }

        [Obsolete]
        static void Main(string[] args)
        {
           
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n\t\t\t\t\t\tExcel file Conversion ");
            Console.Write("\t\t\t\t\t\t\t\b---------------------\n\n\n");
            Console.ResetColor();
            Console.WriteLine("\n\n");

                FileInfo_InOut Fileinfo = new FileInfo_InOut();
                InOutFolder IOF = new InOutFolder();
                var FileInfoValues = IOF.config_Folder(Fileinfo);
                if ((FileInfoValues.FolderInput != "")&&(FileInfoValues.FolderOutput!=""))
                {
                ExcelConvert EC = new ExcelConvert(FileInfoValues.FolderInput, FileInfoValues.FolderOutput, FileInfoValues.Prefix);
                EC.Excel_To_Csv_And_Txt();
                }
            Console.ReadLine();
        }
    }
}






