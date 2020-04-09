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
        public static StringBuilder Log;

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
            Log = new StringBuilder();
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
                string fileExtension = Path.GetExtension(file).Substring(1);
                string fileName = Path.GetFileName(file);

                var createDate = File.GetCreationTime(file);
                var updateDate = File.GetLastWriteTime(file);

                var folderDate = createDate < updateDate ? createDate : updateDate;


                if (ValidFileExtentions.Contains(fileExtension.ToLower()))
                {
                    string destinationFolderPath = String.Format(@"{0}/{1:yyyy}/{1:MM}/{1:yyyyMMdd}/{2}"
                        , DestinationFolderPath, folderDate, fileExtension.ToUpper());
                    string destinationFilePath = String.Format("{0}/{1}"
                        , destinationFolderPath, fileName);

                    if (Directory.Exists(destinationFolderPath) == false)
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                    }

                    string fileMD5 = CalculateMD5(file);

                    int fileCount = FileListCache.Where(fi => fi.MD5Checksum == fileMD5).Count();

                    if (fileCount != 0)
                    {
                        ConsoleLog(String.Format("File {0}({1}) already exists.", fileName, fileMD5));
                    }
                    else
                    {
                        File.Move(file, destinationFilePath);
                        ConsoleLog(String.Format("File {0}({1}) was moved to {2}.", fileName, fileMD5, destinationFolderPath));
                    }
                }
                 
            }

            // Refresh FileListJson File
            // Append Only
            // FileListCache = GenerateFileListJson(DestinationFolderPath,FileListJsonFilePath);
            // 


            ConsoleLog(String.Format("Finished Sorting {0}.", Mode));
            SaveLogs(DestinationFolderPath);
        }

        static List<FileIndexInfo> GenerateFileListJson(string DestinationFolderPath, string FileListJsonPath)
        {
            Console.Write("Indexing Destination Folder {0}.", DestinationFolderPath);
            var rFileListCache = new List<FileIndexInfo>();
            var PhotoDestinationFiles = Directory.GetFiles(DestinationFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in PhotoDestinationFiles)
            {
                string fileName = Path.GetFileName(file);

                if (fileName.StartsWith('.') == false)
                {
                    var fileIndex = new FileIndexInfo();
                    fileIndex.MD5Checksum = CalculateMD5(file);
                    fileIndex.FileName = fileName;
                    fileIndex.FilePath = file;
                    rFileListCache.Add(fileIndex);
                }
            }
            File.WriteAllText(FileListJsonPath, JsonConvert.SerializeObject(rFileListCache));
            ConsoleLog(String.Format("Done Indexing Destination Folder {0}. FileList.json was updated.", DestinationFolderPath));
            return rFileListCache;
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
            Log.AppendFormat(Text + "\n");
            Console.WriteLine(Text);
        }

        static void SaveLogs(string DestinationFolderPath)
        { 
            string ReportPath = String.Format("{0}/Reports", DestinationFolderPath);

            if(Directory.Exists(ReportPath)== false)
            {
                Directory.CreateDirectory(ReportPath);
            }

            File.WriteAllText(String.Format("{0}/{1:yyyyMMddhhmmss}-Logs.log",ReportPath,DateTime.Now), Log.ToString());
        }

     
    }
}
