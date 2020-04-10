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
        public static string SourceFolderPath = @"SOURCE_FOLDER_PATH";
        public static string PhotosFileExtensions = "jpg,jpeg,cr2,cr3,png,gif,heif";
        public static string VideosFileExtensions = "mp4,mov,avi,mkv";
        public static StringBuilder Logs;

        static void Main(string[] args)
        {
            string PhotosDestinationFolderPath = @"PHOTOS";
            string[] validPhotosFileExtensions = PhotosFileExtensions.Split(',');
             
            string VideosDestinationFolderPath = @"VIDEOS";
            string[] validVideosFileExtensions = VideosFileExtensions.Split(',');

            Sort("Photos", PhotosDestinationFolderPath, validPhotosFileExtensions);
            Sort("Videos", VideosDestinationFolderPath, validVideosFileExtensions);
        }

        static void Sort(string Mode, string DestinationFolderPath,string[] ValidFileExtentions )
        {
            Logs = new StringBuilder();
            ConsoleLog(String.Format("Sorting {0}.",Mode));
            string FileListJsonFilePath = String.Format("{0}/FileList.json", DestinationFolderPath);
            List<FileIndexInfo> FileListCache = new List<FileIndexInfo>();
            bool updateFileListJsonFile = false;


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
                    FileListJsonFilePath,
                    ValidFileExtentions);
            }

            // Source Folder
            ConsoleLog(String.Format("Reading files from {0}.", SourceFolderPath));

            var Files = Directory.GetFiles(SourceFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in Files)
            {
                var f = ConvertToFileIndex(file);

                var folderDate = f.CreatedOnDate < f.UpdatedOnDate ? f.CreatedOnDate : f.UpdatedOnDate;

                if (f.FileName.StartsWith('.') == false && 
                    ValidFileExtentions.Contains(f.FileExtension.ToLower()))
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
                        updateFileListJsonFile = true;
                        ConsoleLog(String.Format("File {0}({1}) was moved to {2}.", f.FileName, f.MD5Checksum, destinationFolderPath));
                    }
                }    
            }
            if (updateFileListJsonFile)
            {
                SaveFileListJson("Re-index", FileListJsonFilePath, FileListCache, DestinationFolderPath);
            }
            ConsoleLog(String.Format("Finished Sorting {0}.", Mode));
            SaveLogs(DestinationFolderPath);
        }

        static List<FileIndexInfo> GenerateFileListJson(string DestinationFolderPath,
         string FileListJsonFilePath, string[] ValidFileExtentions)
        {
            ConsoleLog(String.Format("Indexing Destination Folder {0}.", DestinationFolderPath));
            var rFileListCache = new List<FileIndexInfo>();
            var destinationFiles = Directory.GetFiles(DestinationFolderPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in destinationFiles)
            {
                var f = ConvertToFileIndex(file);
                Console.WriteLine(f.FilePath);
                if (f.FileName.StartsWith('.') == false && 
                    ValidFileExtentions.Contains(f.FileExtension.ToLower()))
                    {
                         rFileListCache.Add(f);
                    }
            }
            SaveFileListJson("Index",FileListJsonFilePath, rFileListCache,DestinationFolderPath);
            return rFileListCache;
        }

        static void SaveFileListJson(string IndexMode, string FileListJsonFilePath, List<FileIndexInfo> FileListCache, string DestinationFolderPath)
        {
            File.WriteAllText(FileListJsonFilePath, JsonConvert.SerializeObject(FileListCache));
            ConsoleLog(String.Format("Done {0}ing Destination Folder {1}. FileList.json was updated.", IndexMode, DestinationFolderPath));
        }

        static FileIndexInfo ConvertToFileIndex(string FilePath){

            string fileName = Path.GetFileName(FilePath);
            string fileExtension = "";

            if(Path.GetExtension(FilePath).StartsWith('.'))
            {
                fileExtension = Path.GetExtension(FilePath).Substring(1);
            }

            FileIndexInfo rFileIndexInfo = new FileIndexInfo();
            rFileIndexInfo.MD5Checksum = CalculateMD5(FilePath);
            rFileIndexInfo.FileName = fileName;
            rFileIndexInfo.FileExtension = fileExtension;
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

            File.WriteAllText(String.Format("{0}/{1:yyyyMMddhhmmss}-SORTER.log",ReportPath,DateTime.Now), Logs.ToString());
        }
    }
}
