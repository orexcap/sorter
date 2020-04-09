using System;
namespace SORTER
{
    public class FileIndexInfo
    {
        public string MD5Checksum { get; set; }
        public string FileName { get; set; }
        public string FileExtension {get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedOnDate {get; set;}
        public DateTime UpdatedOnDate {get; set;}
        public FileIndexInfo()
        {
        }
    }
}