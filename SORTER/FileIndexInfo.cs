using System;
using System.Collections.Generic;
namespace SORTER
{
    public class FileIndexInfo
    {
        public string MD5Checksum { get; set; }
        public string OriginalFileName { get; set; }
        public string FileExtension {get; set; }
        public string OriginalFilePath { get; set; }
        public DateTime CreatedOnDate {get; set;}
        public DateTime UpdatedOnDate {get; set;}
        public string Sorter_FileName { get; set; }
        public DateTime Sorter_Date {get; set;}
        public string Sorter_DateSource { get; set; }
        public string Sorter_FilePath { get; set; }

        public FileIndexInfo()
        {
        }
    }
}