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
    internal class File_Name
    {
        public string FilePathValid { get; set; }
        public string FileInputValid { get; set; }
        public string checkFilePath(string FilePath,string FileInput)
        {
            FilePathValid = FilePath;
            FileInputValid = FileInput;
            string path = "";
            int nameLen = FileInputValid.Length;
            FileInputValid = "";
            for(int i = nameLen+1; i < FilePathValid.Length; i++)
            {
                FileInputValid += FilePathValid[i];      
            }
            foreach (var ch in FileInputValid)
            {
                if(ch == '.' )
                {
                    break;
                }
                else                        
                {
                    path += ch;
                }
            }
            return path;
        }
    }
}