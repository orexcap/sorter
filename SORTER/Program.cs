using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
namespace SORTER
{
    class Program
    {
        //string SourceFolderPath = @"/Volumes/EOS_DIGITAL/DCIM";
        public static string SourceFolderPath = "/Users/joseph/Desktop/TEST_UNSORTED";
        public static string PhotosFileExtensions = "jpg,jpeg,cr2,cr3,png,gif,heif";
        public static string VideosFileExtensions = "mp4,mov,avi,mkv";
        public static StringBuilder Logs;

        static void Main(string[] args)
        {
            string PhotosDestinationFolderPath = "/Users/joseph/Desktop/TEST_PHOTOS_SORTED";
            string[] validPhotosFileExtensions = PhotosFileExtensions.Split(',');
             
            string VideosDestinationFolderPath = "/Users/joseph/Desktop/TEST_VIDEOS_SORTED";
            string[] validVideosFileExtensions = VideosFileExtensions.Split(',');

            Sort("Photos", PhotosDestinationFolderPath, validPhotosFileExtensions);
            Sort("Videos", VideosDestinationFolderPath, validVideosFileExtensions);
        }

        static void Sort(string Mode, string DestinationFolderPath,string[] ValidFileExtentions )
        {
            Logs = new StringBuilder();
            ConsoleLog(String.Format("Sorting in {0} Mode.",Mode));
            string FileListJsonFilePath = String.Format("{0}/FileList.json", DestinationFolderPath);
            List<FileIndexInfo> FileListCache = new List<FileIndexInfo>();

            // Destination Folder
            if (File.Exists(FileListJsonFilePath))
            {
                ConsoleLog(String.Format("Reading {0}' FileList.json...",Mode));
                var fileCache = File.ReadAllText(FileListJsonFilePath);
                FileListCache = JsonConvert.DeserializeObject<List<FileIndexInfo>>(fileCache);
            }
            else
            {
                FileListCache = GenerateFileListJson(
                    DestinationFolderPath,
                    FileListJsonFilePath);
            }

            // Source Folder
            ConsoleLog(String.Format("Reading files from {0}.", SourceFolderPath));

            var Files = Directory.GetFiles(SourceFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in Files)
            {
                var f = ConvertToFileIndex(file);

                var folderDate = f.CreatedOnDate < f.UpdatedOnDate ? f.CreatedOnDate : f.UpdatedOnDate;

                if (ValidFileExtentions.Contains(f.FileExtension.ToLower()))
                {
                    string destinationFolderPath = String.Format(@"{0}/{1:yyyy}/{1:MM}/{1:yyyyMMdd}/{2}"
                        , DestinationFolderPath, folderDate, f.FileExtension.ToUpper());
                    string destinationFilePath = String.Format("{0}/{1}"
                        , destinationFolderPath, f.FileName);

                    if (Directory.Exists(destinationFolderPath) == false)
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                    }
                     
                    int fileCount = FileListCache.Where(fi => fi.MD5Checksum == f.MD5Checksum).Count();

                    if (fileCount != 0)
                    {
                        ConsoleLog(String.Format("File {0}({1}) already exists.", f.FileName, f.MD5Checksum));
                    }
                    else
                    {
                        File.Move(file, destinationFilePath);
                        FileListCache.Add(f);
                        ConsoleLog(String.Format("File {0}({1}) was moved to {2}.", f.FileName, f.MD5Checksum, destinationFolderPath));
                    }
                }    
            }
            SaveFileListJson(FileListJsonFilePath, FileListCache, DestinationFolderPath);
            ConsoleLog(String.Format("Finished Sorting {0}.", Mode));
            SaveLogs(DestinationFolderPath);
        }

        static List<FileIndexInfo> GenerateFileListJson(string DestinationFolderPath, string FileListJsonFilePath)
        {
            Console.Write("Indexing Destination Folder {0}.", DestinationFolderPath);
            var rFileListCache = new List<FileIndexInfo>();
            var PhotoDestinationFiles = Directory.GetFiles(DestinationFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in PhotoDestinationFiles)
            {
                string fileName = Path.GetFileName(file);

                if (fileName.StartsWith('.') == false)
                {
                    var fileIndex = ConvertToFileIndex(file);
                    rFileListCache.Add(fileIndex);
                }
            }
            SaveFileListJson(FileListJsonFilePath, rFileListCache,DestinationFolderPath);
            return rFileListCache;
        }

        static void SaveFileListJson(string FileListJsonFilePath, List<FileIndexInfo> FileListCache, string DestinationFolderPath)
        {
            File.WriteAllText(FileListJsonFilePath, JsonConvert.SerializeObject(FileListCache));
            ConsoleLog(String.Format("Done Indexing Destination Folder {0}. FileList.json was updated.", DestinationFolderPath));
        }

        static FileIndexInfo ConvertToFileIndex(string FilePath){
            FileIndexInfo rFileIndexInfo = new FileIndexInfo();
            rFileIndexInfo.MD5Checksum = CalculateMD5(FilePath);
            rFileIndexInfo.FileName = Path.GetFileName(FilePath);
            rFileIndexInfo.FileExtension  = Path.GetExtension(FilePath).Substring(1);
            rFileIndexInfo.FilePath = FilePath;
            rFileIndexInfo.CreatedOnDate   = File.GetCreationTime(FilePath);
            rFileIndexInfo.UpdatedOnDate = File.GetLastWriteTime(FilePath);
            return rFileIndexInfo;
        }

        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static void ConsoleLog(String Text)
        {
            Logs.AppendFormat(Text + "\n");
            Console.WriteLine(Text);
        }

        static void SaveLogs(string DestinationFolderPath)
        { 
            string ReportPath = String.Format("{0}/Reports", DestinationFolderPath);

            if(Directory.Exists(ReportPath)== false)
            {
                Directory.CreateDirectory(ReportPath);
            }

            File.WriteAllText(String.Format("{0}/{1:yyyyMMddhhmmss}-Logs.log",ReportPath,DateTime.Now), Logs.ToString());
        }

     
    }
}
