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
    internal class ExcelConvert
    {

        public string[] FileDir;
        FileInfo_InOut Fileinfo = new FileInfo_InOut();
        public string TxtName, CsvName;
        public ExcelConvert(string FolderInput, string FolderOutput, string Prefix)
        {
                FileDir = Directory.GetFiles(FolderInput, "*." + Fileinfo.Format);
                Fileinfo.FolderInput = FolderInput;
                Fileinfo.FolderOutput = FolderOutput;
                Fileinfo.Prefix = Prefix;
        }
        public  int Excel_To_Csv(string filePath){
            
            Workbook workbook = new Workbook();                     //Load an Excel file
            workbook.LoadFromFile(filePath);
            int workbookcount = workbook.Worksheets.Count;
            for (int i = 0; i < workbookcount; i++)
            {
                Worksheet sheet = workbook.Worksheets[i];           //Get the first worksheet
                if (i >0)
                {
                    Fileinfo.CsvFilePath = Fileinfo.FolderOutput + "\\" + Fileinfo.Prefix + Fileinfo.FileName+"(sheet"+(i+1) + ").csv";
                    CsvName = Fileinfo.Prefix + Fileinfo.FileName + "(sheet" + (i + 1) + ").csv";
                }
                else
                {
                    Fileinfo.CsvFilePath = Fileinfo.FolderOutput + "\\" + Fileinfo.Prefix + Fileinfo.FileName + ".csv";
                    CsvName = Fileinfo.Prefix + Fileinfo.FileName + ".csv";
                }
                if (File.Exists(Fileinfo.CsvFilePath))                    
                {
                    Console.WriteLine("\t\tCSV File already exits in the same folder");
                }

                int SC = sheet.Columns.Count(); int SR = sheet.Rows.Count();
                using (StreamWriter streamWriter = new StreamWriter(Fileinfo.CsvFilePath))
                {
                    for (int row = 1; row <= SR; row++)
                    {
                        for (int col = 1; col <= SC; col++)
                        {
                            string item = sheet[row, col].Value.ToString();
                            string quotedItem = "\"" + item.Replace("\"", "\"\"") + "\"";
                            if (col < SC)
                            {
                                streamWriter.Write(quotedItem + "|");
                            }
                            else
                            {
                                streamWriter.Write(quotedItem);
                            }
                        }
                        streamWriter.WriteLine();
                    }
                }

                Console.WriteLine("\t\tConverted into csv file format \"" + CsvName + "\"");

            }
            return workbookcount;

        }
        public void Csv_to_Txt(int sheetCount)
        {
            for (int i = 0; i < sheetCount; i++)
            {
                if (i > 0)
                {
                    Fileinfo.CsvFilePath = Fileinfo.FolderOutput + "\\" + Fileinfo.Prefix + Fileinfo.FileName + "(sheet" + (i + 1) + ").csv";
                    Fileinfo.TextFilePath = Fileinfo.FolderOutput + "\\" + Fileinfo.Prefix + Fileinfo.FileName + "(sheet" + (i + 1) + ").txt";
                    TxtName = Fileinfo.Prefix + Fileinfo.FileName + "(sheet" + (i + 1) + ").txt";
                    CsvName = Fileinfo.Prefix + Fileinfo.FileName + "(sheet" + (i + 1) + ").csv";
                }
                else
                {
                    Fileinfo.CsvFilePath = Fileinfo.FolderOutput + "\\" + Fileinfo.Prefix + Fileinfo.FileName + ".csv";
                    Fileinfo.TextFilePath = Fileinfo.FolderOutput + "\\" + Fileinfo.Prefix + Fileinfo.FileName + ".txt";
                    TxtName= Fileinfo.Prefix + Fileinfo.FileName + ".txt";
                    CsvName = Fileinfo.Prefix + Fileinfo.FileName + ".csv";


                }
                if (File.Exists(Fileinfo.CsvFilePath))
                {
                    if (File.Exists(Fileinfo.TextFilePath))
                    {
                        Console.WriteLine("\t\tText File already exits in the same folder");
                    }
                    File.Copy(Fileinfo.CsvFilePath, Fileinfo.TextFilePath, true);
                    Console.WriteLine("\t\tConverted into txt file format \"" + TxtName + "\"");
                    File.Delete(Fileinfo.CsvFilePath);
                    Console.WriteLine($"\t\tCsv file deleted \"{CsvName}\"");

                }
                else
                {
                    Console.WriteLine("\t\tCSV file not founded... \n \t\tSo reloaded the process it automatically creates");
                }
            }
            
        }

        public void Excel_To_Csv_And_Txt()
        {
            float count = FileDir.Length, progress = 100 / count, progress_percent;
            string processor = "Processing";
            int sheetcount = 0;
            progress_percent = progress;

            if (FileDir.Any())
            {

            File_Name files = new File_Name();

            foreach (string filePath in FileDir)
            {
                    Fileinfo.FileName = files.checkFilePath(filePath, Fileinfo.FolderInput);

                    Console.WriteLine("\n----------------------------------------------------------------------------------------------");

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Excel File Name : " + Fileinfo.FileName + "." + Fileinfo.Format);
                    Console.ResetColor();
                    Console.WriteLine("\t\tExcel file into CSV file \n\t\t...... Process started ......");
                    sheetcount=Excel_To_Csv(filePath);
                    Console.WriteLine("\t\t...... Process Completed ......");

                    TxtName = Fileinfo.Prefix + Fileinfo.FileName + ".txt";
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("\nCSV File Name : " + Fileinfo.Prefix + Fileinfo.FileName+".csv");
                    Console.ResetColor();
                    Console.WriteLine("\t\tCSV file into Text file \n\t\t...... Process started......");
                    Csv_to_Txt(sheetcount);
                    Console.WriteLine("\t\t......Process Completed ......");
                    Console.WriteLine("\n\n");

                Console.ForegroundColor = ConsoleColor.Red;
                if (Math.Round(progress_percent) == 100)
                {
                    processor = "Completed";
                }
                Console.WriteLine("|||||||||||||||||||||||||| {0} : {1}% ||||||||||||||||||||||||||", processor, Math.Round(progress_percent));
                Console.ResetColor();

                progress_percent += progress;

                Thread.Sleep(2000);

                Console.WriteLine("----------------------------------------------------------------------------------------------");

            }
        }
            else
            {
                Console.WriteLine("Please provide excel file in the input folder ");
            }

        }
      
    }
}











