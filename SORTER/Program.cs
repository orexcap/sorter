using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

namespace SORTER
{
    class Program
    { 
        public static Settings SETTINGS;
        public static StringBuilder LOGS;
        public static string ConfigJsonFilePath = String.Format(@"{0}/Sorter.Config.json",
            Environment.CurrentDirectory);

        static void Main(string[] args){ 
            LoadConfigJson();
            Sort("Photos", SETTINGS.SourceFolderPath, SETTINGS.PhotosDestinationFolderPath,
                SETTINGS.PhotosFileExtensions);
            Sort("Videos", SETTINGS.SourceFolderPath ,SETTINGS.VideosDestinationFolderPath,
                SETTINGS.VideosFileExtensions);
        }

        static void LoadConfigJson(){
            string newSourceFolderPath = @"SOURCE/FOLDER";
            string newPhotosDestinationFolderPath = @"PHOTOS/DESTINATION/FOLDER";
            string newVideosDestinationFolderPath = @"VIDEOS/DESTINATION/FOLDER";

            SETTINGS = new Settings();
            SETTINGS.SourceFolderPath = newSourceFolderPath;
            SETTINGS.PhotosDestinationFolderPath = newPhotosDestinationFolderPath;
            SETTINGS.VideosDestinationFolderPath = newVideosDestinationFolderPath;
            SETTINGS.PhotosFileExtensions = "jpg,jpeg,cr2,cr3,png,gif,heif,heic";
            SETTINGS.VideosFileExtensions = "mp4,mov,avi,mkv";

            if(File.Exists(ConfigJsonFilePath)){
                //Load Config.json
                Console.WriteLine("Reading Sorter.Config.json...");
                var sorterConfig = File.ReadAllText(ConfigJsonFilePath);
                var LoadSettings = JsonConvert.DeserializeObject<Settings>(sorterConfig);

                if(CheckConfigJson(LoadSettings,SETTINGS) == true){
                    SETTINGS = LoadSettings;
                }
            }
            else{
                //Write new Config.json File
                File.WriteAllText(ConfigJsonFilePath, JsonConvert.SerializeObject(SETTINGS));
                CheckConfigJson(SETTINGS,SETTINGS);
            }
        }

        static bool CheckConfigJson(Settings LoadSettings, Settings NewSettings){
            bool rConfigJson = false;
            if(LoadSettings.SourceFolderPath == NewSettings.SourceFolderPath ||
                LoadSettings.PhotosDestinationFolderPath == NewSettings.PhotosDestinationFolderPath ||
                LoadSettings.VideosDestinationFolderPath == NewSettings.VideosDestinationFolderPath ){
                Console.WriteLine("Please setup {0} first.",ConfigJsonFilePath);
                Environment.Exit(0);  
            }
            else{
                rConfigJson = true;
            }
            return rConfigJson;
        }

        static void Sort(string Mode, string SourceFolderPath, string DestinationFolderPath,
            string FileExtensions){
            LOGS = new StringBuilder();
            ConsoleLog(String.Format("Sorting {0}.",Mode));
            string FileListJsonFilePath = String.Format("{0}/FileList.json", DestinationFolderPath);
            List<FileIndexInfo> FileListCache = new List<FileIndexInfo>();
            bool updateFileListJsonFile = false;
            string[] ValidFileExtentions = FileExtensions.Split(',');

            // Destination Folder
            if (File.Exists(FileListJsonFilePath)){
                ConsoleLog(String.Format("Reading {0}' FileList.json...",Mode));
                var fileCache = File.ReadAllText(FileListJsonFilePath);
                FileListCache = JsonConvert.DeserializeObject<List<FileIndexInfo>>(fileCache);
            }
            else{
                FileListCache = GenerateFileListJson(
                    DestinationFolderPath,
                    FileListJsonFilePath,
                    ValidFileExtentions);
            }

            // Source Folder
            ConsoleLog(String.Format("Reading files from {0}.", SourceFolderPath));

            var ValidFiles = new List<string>();
            foreach(var vfe in ValidFileExtentions){
                string searchPattern = string.Format("*.{0}",vfe);
                var validFiles = Directory.GetFiles(SourceFolderPath, searchPattern, 
                    new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true });
                ValidFiles.AddRange(validFiles);
            }
 
            foreach (var file in ValidFiles){
                var f = ConvertToFileIndex(file);
                var folderDate = f.CreatedOnDate < f.UpdatedOnDate ? f.CreatedOnDate : f.UpdatedOnDate;

                if (f.FileName.StartsWith('.') == false && 
                    ValidFileExtentions.Contains(f.FileExtension.ToLower())){
                    string destinationFolderPath = String.Format(@"{0}/{1:yyyy}/{1:MM}/{1:yyyyMMdd}/{2}"
                        , DestinationFolderPath, folderDate, f.FileExtension.ToUpper());
                    string destinationFilePath = String.Format("{0}/{1}"
                        , destinationFolderPath, f.FileName);

                    if (Directory.Exists(destinationFolderPath) == false){
                        Directory.CreateDirectory(destinationFolderPath);
                    }
                     
                    int fileCount = FileListCache.Where(fi => fi.MD5Checksum == f.MD5Checksum).Count();
                    
                    if (fileCount != 0){
                        ConsoleLog(String.Format("File {0}({1}) already exists.", f.FileName, f.MD5Checksum));
                    }
                    else if(File.Exists(destinationFilePath)){
                        ConsoleLog(String.Format("File {0}({1}) already exists.", f.FileName, f.MD5Checksum));
                        // Retest MD5 Checksum of DestinationFilePath
                        string ReMD5Checksum = CalculateMD5(destinationFilePath);
                        if(ReMD5Checksum != f.MD5Checksum){
                            // Move the file with MD5 as prefix (DUP+First 8 Character)
                            f.FileName = String.Format("DUP{0}-{1}",f.MD5Checksum.Substring(0,8), f.FileName);
                            destinationFilePath = String.Format("{0}/{1}", destinationFolderPath, f.FileName);
                            string md5CheckSumErrorMessage =
                                "MD5 Checksum Mismatch Source's MD5 {0} and Destination MD5 (Recalculated) {1}. Moving File as {2}.";
                            ConsoleLog(String.Format(md5CheckSumErrorMessage, f.MD5Checksum, ReMD5Checksum, f.FileName));
                             updateFileListJsonFile = MoveFile(FileListCache, file, f, destinationFolderPath, destinationFilePath);
                        }
                    }
                    else{
                        updateFileListJsonFile = MoveFile(FileListCache, file, f, destinationFolderPath, destinationFilePath);
                    }

                }    
            }
            if (updateFileListJsonFile){
                SaveFileListJson("Re-index", FileListJsonFilePath, FileListCache, DestinationFolderPath);
            }
            ConsoleLog(String.Format("Finished Sorting {0}.", Mode));
            SaveLogs(DestinationFolderPath);
        }

        static bool MoveFile(List<FileIndexInfo> FileListCache, string file,
            FileIndexInfo f, string destinationFolderPath, string destinationFilePath){
            bool updateFileListJsonFile;
            ConsoleLog(String.Format("Moving File {0}({1}) to {2}.", f.FileName, f.MD5Checksum,
                destinationFolderPath));
            File.Move(file, destinationFilePath);
            FileListCache.Add(f);
            updateFileListJsonFile = true;
            ConsoleLog(String.Format("File {0}({1}) was moved to {2}.", f.FileName, f.MD5Checksum,
                destinationFolderPath));
            return updateFileListJsonFile;
        }

        static List<FileIndexInfo> GenerateFileListJson(string DestinationFolderPath,
         string FileListJsonFilePath, string[] ValidFileExtentions){
            ConsoleLog(String.Format("Indexing Destination Folder {0}.", DestinationFolderPath));
            var rFileListCache = new List<FileIndexInfo>();
            var destinationFiles = Directory.GetFiles(DestinationFolderPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in destinationFiles){
                var f = ConvertToFileIndex(file);
                Console.WriteLine(f.FilePath);
                if (f.FileName.StartsWith('.') == false && 
                    ValidFileExtentions.Contains(f.FileExtension.ToLower())){
                         rFileListCache.Add(f);
                    }
            }
            SaveFileListJson("Index",FileListJsonFilePath, rFileListCache,DestinationFolderPath);
            return rFileListCache;
        }

        static void SaveFileListJson(string IndexMode, string FileListJsonFilePath,
            List<FileIndexInfo> FileListCache, string DestinationFolderPath){
            File.WriteAllText(FileListJsonFilePath, JsonConvert.SerializeObject(FileListCache));
            ConsoleLog(String.Format("Done {0}ing Destination Folder {1}. FileList.json was updated.", IndexMode, DestinationFolderPath));
        }

        static FileIndexInfo ConvertToFileIndex(string FilePath){
            string fileName = Path.GetFileName(FilePath);
            string fileExtension = "";

            if(Path.GetExtension(FilePath).StartsWith('.')){
                fileExtension = Path.GetExtension(FilePath).Substring(1);
            }

            FileIndexInfo rFileIndexInfo = new FileIndexInfo();
            rFileIndexInfo.MD5Checksum = CalculateMD5(FilePath);
            rFileIndexInfo.FileName = fileName;
            rFileIndexInfo.FileExtension = fileExtension;
            rFileIndexInfo.FilePath = FilePath;
            rFileIndexInfo.CreatedOnDate   = File.GetCreationTime(FilePath);
            rFileIndexInfo.UpdatedOnDate = File.GetLastWriteTime(FilePath);

            ReadXMPFile(rFileIndexInfo);

            return rFileIndexInfo;
        }

        static string CalculateMD5(string filename){
            using (var md5 = MD5.Create()){
                using (var stream = File.OpenRead(filename)){
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static void ReadXMPFile(FileIndexInfo FileIndex){
           string directoryName = Path.GetDirectoryName(FileIndex.FilePath);
           string fileName = Path.GetFileNameWithoutExtension(FileIndex.FilePath);
           string xmpFileName = fileName + ".xmp";
           string xmpFilePath = String.Format("{0}/{1}",directoryName,xmpFileName);
  
           if(File.Exists(xmpFilePath))
           {
               FileIndex.CreatedOnDateFromXMP = new List<DateTime>();
               Console.WriteLine("Reading Created On Date from XMP : {0}",xmpFilePath);
               XDocument doc = XDocument.Load(xmpFilePath);

                //     XNamespace ns = XNamespace.Get("adobe:ns:meta/");
                //     var listOfNames = doc.Descendants(ns + "xmpmeta")
                //              .Select(x => x.Elements().First().Value).ToList();
                //    Console.WriteLine (JsonConvert.SerializeObject(listOfNames));
 
                var query = doc.Descendants()
                .Where(c => c.Name.LocalName.ToString() == "DateCreated")
                .ToArray();

                foreach (String item in query) {
                    FileIndex.CreatedOnDateFromXMP.Add(Convert.ToDateTime(item));
                    //Console.WriteLine(item);
                }

                //Apply the Changes got from the XMP File
                if(FileIndex.CreatedOnDateFromXMP.FirstOrDefault() != null)
                {
                    FileIndex.CreatedOnDateFromFile = FileIndex.CreatedOnDate;
                    FileIndex.CreatedOnDate = FileIndex.CreatedOnDateFromXMP.FirstOrDefault();
                    FileIndex.FileName = String.Format("{0:yyyyMMddhhmmss}-{1}",FileIndex.CreatedOnDate,FileIndex.FileName);
                }

           }
        }

        static void ConsoleLog(String Text){
            LOGS.AppendFormat(Text + "\n");
            Console.WriteLine(Text);
        }

        static void SaveLogs(string DestinationFolderPath){ 
            string ReportPath = String.Format("{0}/Reports", DestinationFolderPath);

            if(Directory.Exists(ReportPath)== false){
                Directory.CreateDirectory(ReportPath);
            }

            File.WriteAllText(String.Format("{0}/{1:yyyyMMddhhmmss}-SORTER.log",ReportPath,DateTime.Now), LOGS.ToString());
        }
    }
}