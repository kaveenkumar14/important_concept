using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToTextFileConverter
{
    internal class InOutFolder
    {

        protected string folderPath(string pass)
        {
            string folder = "";
            folder = ConfigurationSettings.AppSettings[pass].ToString();
            
            if (pass == "Input")
            {
                if (folder == "")
                {
                    Console.WriteLine("please Enter the Input folder in config settings\n Because it is in empty ");
                }
                else
                {
                    Console.WriteLine("Input Folder : " + folder);
                    Console.WriteLine(" ");
                }
            }
            if (pass == "Output")
            {
                if (folder == "")
                {
                    Console.WriteLine("please Enter the Output folder in config settings\n Because it is in empty ");
                }
                else
                {
                    Console.WriteLine("Output Folder : " + folder);
                    Console.WriteLine(" ");
                }
            }
            if (pass == "Prefix" )
            {
                if (folder != "")
                {
                    Console.WriteLine("Prefix Name : " + folder);
                    Console.WriteLine("\n");
                }
            }
            return folder;

        }
        public FileInfo_InOut config_Folder(FileInfo_InOut pass)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            pass.FolderInput = folderPath("Input");
            pass.FolderOutput = folderPath("Output");
            pass.Prefix = folderPath("Prefix");
            Console.ResetColor();
            return pass;
        
        }
       

    }
}
