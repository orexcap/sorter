using System;
using System.Collections.Generic;
namespace SORTER
{
    public class FileIndexInfo
    {
        public string MD5Checksum { get; set; }
        public string FileName { get; set; }
        public string FileExtension {get; set; }
        public string OriginalFilePath { get; set; }
        public string SortedFilePath { get; set; }
        public DateTime CreatedOnDate {get; set;}
        public DateTime UpdatedOnDate {get; set;}
        public DateTime MediaTakenOnDate {get; set;}
        public string MediaTakenOnDateSource { get; set; }
        public FileIndexInfo()
        {
        }
    }
}