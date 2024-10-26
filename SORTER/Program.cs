using MetadataExtractor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace SORTER
{
    class Program
    {
        public static char SEP = Path.DirectorySeparatorChar;
        public static Settings SETTINGS;
        public static StringBuilder LOGS;
        public static string ConfigJsonFilePath = $"{Environment.CurrentDirectory}{SEP}Sorter.Config.json";

        static void Main(string[] args)
        {
            LoadConfigJson();
            Sort("Photos", SETTINGS.SourceFolderPath, SETTINGS.PhotosDestinationFolderPath,
                SETTINGS.PhotosFileExtensions);
            Sort("Videos", SETTINGS.SourceFolderPath, SETTINGS.VideosDestinationFolderPath,
                SETTINGS.VideosFileExtensions);
        }

        static void LoadConfigJson()
        {
            string newSourceFolderPath = $"SOURCE{SEP}FOLDER";
            string newPhotosDestinationFolderPath = $"PHOTOS{SEP}DESTINATION{SEP}FOLDER";
            string newVideosDestinationFolderPath = $"VIDEOS{SEP}DESTINATION{SEP}FOLDER";

            SETTINGS = new Settings();
            SETTINGS.SourceFolderPath = newSourceFolderPath;
            SETTINGS.PhotosDestinationFolderPath = newPhotosDestinationFolderPath;
            SETTINGS.VideosDestinationFolderPath = newVideosDestinationFolderPath;
            SETTINGS.PhotosFileExtensions = "jpg,jpeg,cr2,cr3,png,gif,heif,heic";
            SETTINGS.VideosFileExtensions = "mp4,mov,avi,mkv,360";
            SETTINGS.RenameFiles = false;

            if (File.Exists(ConfigJsonFilePath))
            {
                //Load Config.json
                Console.WriteLine("Reading Sorter.Config.json...");
                var sorterConfig = File.ReadAllText(ConfigJsonFilePath);
                var LoadSettings = JsonConvert.DeserializeObject<Settings>(sorterConfig);

                if (CheckConfigJson(LoadSettings, SETTINGS) == true)
                {
                    SETTINGS = LoadSettings;
                }
            }
            else
            {
                //Write new Config.json File
                File.WriteAllText(ConfigJsonFilePath, JsonConvert.SerializeObject(SETTINGS));
                CheckConfigJson(SETTINGS, SETTINGS);
            }
        }

        static bool CheckConfigJson(Settings LoadSettings, Settings NewSettings)
        {
            bool rConfigJson = false;
            if (LoadSettings.SourceFolderPath == NewSettings.SourceFolderPath ||
                LoadSettings.PhotosDestinationFolderPath == NewSettings.PhotosDestinationFolderPath ||
                LoadSettings.VideosDestinationFolderPath == NewSettings.VideosDestinationFolderPath)
            {
                Console.WriteLine("Please setup {0} first.", ConfigJsonFilePath);
                Environment.Exit(0);
            }
            else
            {
                rConfigJson = true;
            }
            return rConfigJson;
        }

        static void Sort(string Mode, string SourceFolderPath, string DestinationFolderPath,
            string FileExtensions)
        {
            LOGS = new StringBuilder();

            DateTime sDateTime = DateTime.Now;
            ConsoleLog($"Sorting {Mode}. Current time is {sDateTime}.");
            ConsoleLog($"Rename files is {SETTINGS.RenameFiles}.");

            string FileListJsonFilePath = $"{DestinationFolderPath}{SEP}FileList.json";
            List<FileIndexInfo> FileListCache = new List<FileIndexInfo>();
            bool updateFileListJsonFile = false;
            string[] ValidFileExtentions = FileExtensions.Split(',');

            // Destination Folder
            if (File.Exists(FileListJsonFilePath))
            {
                ConsoleLog($"Reading {Mode}' FileList.json...");
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
            ConsoleLog($"Reading files from {SourceFolderPath}.");

            var ValidFiles = new List<string>();
            foreach (var vfe in ValidFileExtentions)
            {
                string searchPattern = $"*.{vfe}";
                var validFiles = Directory.GetFiles(SourceFolderPath, searchPattern,
                    new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true });
                ValidFiles.AddRange(validFiles);
            }

            Parallel.ForEach(ValidFiles, file =>
            {
                var f = ConvertToFileIndex(file);

                if (f.OriginalFileName.StartsWith('.') == false &&
                    ValidFileExtentions.Contains(f.FileExtension.ToLower()))
                {
                    AppendMetaData(f);
                    var folderDate = f.Sorter_Date;
                    try
                    {
                        string destinationFolderPath = $"{DestinationFolderPath}{SEP}{folderDate:yyyy}{SEP}{folderDate:MM}{SEP}{folderDate:yyyyMMdd}{SEP}{f.FileExtension.ToUpper()}";
                        string destinationFilePath = $"{destinationFolderPath}{SEP}{f.OriginalFileName}";

                        if (SETTINGS.RenameFiles)
                        {
                            destinationFilePath = $"{destinationFolderPath}{SEP}{f.Sorter_FileName}";
                        }

                        if (Directory.Exists(destinationFolderPath) == false)
                        {
                            Directory.CreateDirectory(destinationFolderPath);
                        }

                        int fileCount = FileListCache.Where(fi => fi.MD5Checksum == f.MD5Checksum).Count();

                        if (fileCount != 0)
                        {
                            ConsoleLog($"File {f.OriginalFileName}({f.MD5Checksum}) already exists.");
                        }
                        else if (File.Exists(destinationFilePath))
                        {
                            ConsoleLog($"File {f.OriginalFileName}({f.MD5Checksum}) already exists.");
                            // Retest MD5 Checksum of DestinationFilePath
                            string ReMD5Checksum = CalculateMD5(destinationFilePath);
                            if (ReMD5Checksum != f.MD5Checksum)
                            {
                                // Move the file with MD5 as prefix (DUP+First 8 Character)
                                f.OriginalFileName = $"DUP{f.MD5Checksum.Substring(0, 8)}-{f.OriginalFileName}";
                                destinationFilePath = $"{destinationFolderPath}{SEP}{f.OriginalFileName}";
                                ConsoleLog($"MD5 Checksum Mismatch Source's MD5 {f.MD5Checksum} and Destination MD5 (Recalculated) {ReMD5Checksum}. Moving File as {f.OriginalFileName}.");
                                updateFileListJsonFile = MoveFile(FileListCache, file, f, destinationFolderPath, destinationFilePath);
                            }
                        }
                        else
                        {
                            updateFileListJsonFile = MoveFile(FileListCache, file, f, destinationFolderPath, destinationFilePath);
                        }
                    }
                    catch (Exception exc)
                    {
                        ConsoleLog($"Failed to process {f.OriginalFilePath}.");
                        ConsoleLog(exc.Message);
                    }
                }
            });

            if (updateFileListJsonFile)
            {
                SaveFileListJson("Re-index", FileListJsonFilePath, FileListCache, DestinationFolderPath);
            }

            DateTime fDateTime = DateTime.Now;
            ConsoleLog($"Finished Sorting {Mode}. Current time is {fDateTime}.");
            TimeSpan ts = fDateTime - sDateTime;
            ConsoleLog($"Runtime {ts.Hours}:{ts.Minutes}:{ts.Seconds}:{ts.Milliseconds / 10}");
            SaveLogs(DestinationFolderPath);
        }

        static bool MoveFile(List<FileIndexInfo> FileListCache, string file,
            FileIndexInfo f, string destinationFolderPath, string destinationFilePath)
        {
            bool updateFileListJsonFile;
            ConsoleLog($"Moving File {f.OriginalFileName}({f.MD5Checksum}) to {destinationFolderPath}. Source Date : {f.Sorter_DateSource}.");

            if (SETTINGS.RenameFiles)
            {
                ConsoleLog($"{f.OriginalFileName} will be renamed to {f.Sorter_FileName}.");
            }

            File.Move(file, destinationFilePath);
            f.Sorter_FilePath = $"{destinationFolderPath}{SEP}{f.Sorter_FileName}";
            FileListCache.Add(f);
            updateFileListJsonFile = true;
            ConsoleLog($"Done {f.OriginalFileName} was moved to {f.Sorter_FilePath}.");
            return updateFileListJsonFile;
        }

        // TODO: Compare Source and Destination Folder's file travesal method. Line 120.

        static List<FileIndexInfo> GenerateFileListJson(string DestinationFolderPath,
         string FileListJsonFilePath, string[] ValidFileExtentions)
        {
            ConsoleLog($"Indexing Destination Folder {DestinationFolderPath}.");
            var rFileListCache = new List<FileIndexInfo>();
            var destinationFiles = Directory.GetFiles(DestinationFolderPath, "*.*", SearchOption.AllDirectories);
            Parallel.ForEach(destinationFiles, file =>
            {
                var f = ConvertToFileIndex(file);
                if (f.OriginalFileName.StartsWith('.') == false &&
                    ValidFileExtentions.Contains(f.FileExtension.ToLower()))
                {
                    AppendMetaData(f);
                    f.Sorter_FilePath = $"{DestinationFolderPath}{SEP}{f.OriginalFileName}";
                    rFileListCache.Add(f);
                    ConsoleLog($"Adding {f.OriginalFilePath} to Index file.");
                }
            });
            SaveFileListJson("Index", FileListJsonFilePath, rFileListCache, DestinationFolderPath);
            return rFileListCache;
        }

        static void SaveFileListJson(string IndexMode, string FileListJsonFilePath,
            List<FileIndexInfo> FileListCache, string DestinationFolderPath)
        {
            File.WriteAllText(FileListJsonFilePath, JsonConvert.SerializeObject(FileListCache));
            ConsoleLog($"Done {IndexMode}ing Destination Folder {DestinationFolderPath}. FileList.json was updated.");
        }

        static FileIndexInfo ConvertToFileIndex(string FilePath)
        {
            string fileName = Path.GetFileName(FilePath);
            string fileExtension = "";

            if (Path.GetExtension(FilePath).StartsWith('.'))
            {
                fileExtension = Path.GetExtension(FilePath).Substring(1);
            }

            FileIndexInfo rFileIndexInfo = new FileIndexInfo();
            rFileIndexInfo.MD5Checksum = CalculateMD5(FilePath);
            rFileIndexInfo.OriginalFileName = fileName;
            rFileIndexInfo.FileExtension = fileExtension;
            rFileIndexInfo.OriginalFilePath = FilePath;
            rFileIndexInfo.CreatedOnDate = File.GetCreationTime(FilePath);
            rFileIndexInfo.UpdatedOnDate = File.GetLastWriteTime(FilePath);

            return rFileIndexInfo;
        }

        static void AppendMetaData(FileIndexInfo FileInfo)
        {
            DateTime sorter_Date = FileInfo.CreatedOnDate;
            String sorter_DateSource = "CreatedOnDate";

            if (FileInfo.UpdatedOnDate < sorter_Date)
            {
                sorter_Date = FileInfo.UpdatedOnDate;
                sorter_DateSource = "UpdatedOnDate";
            }

            try
            {
                IEnumerable<MetadataExtractor.Directory> directories
                    = ImageMetadataReader.ReadMetadata(FileInfo.OriginalFilePath);

                foreach (var directory in directories)
                    foreach (var tag in directory.Tags)
                    {
                        if (tag.Name.Contains("Created")
                                || tag.Name.Contains("Modified")
                                || tag.Name.Contains("Original")
                                || tag.Name.Contains("Digitized")
                                || tag.Name.Contains("Creation"))
                        {
                            if (tag.Description != null)
                            {
                                DateTime metadata_Date = sorter_Date;
                                string dateString = tag.Description;

                                // Parse a string.
                                if (DateTime.TryParseExact(dateString, "ddd MMM dd HH:mm:ss yyyy",
                                        CultureInfo.InvariantCulture, DateTimeStyles.None,
                                        out metadata_Date))
                                {
                                    if (metadata_Date <= sorter_Date)
                                    {
                                        sorter_Date = metadata_Date;
                                        sorter_DateSource = ($"{directory.Name} - {tag.Name} = {tag.Description}");
                                    }
                                }

                                // Parse a string with time zone information. 
                                if (DateTime.TryParseExact(dateString, "ddd MMM dd HH:mm:ss zzz yyyy",
                                        CultureInfo.InvariantCulture, DateTimeStyles.None,
                                        out metadata_Date))
                                {
                                    if (metadata_Date <= sorter_Date)
                                    {
                                        sorter_Date = metadata_Date;
                                        sorter_DateSource = ($"{directory.Name} - {tag.Name} = {tag.Description}");
                                    }
                                }

                                // Parse a string date from MAC OS Photos E.g. 2003:05:15 17:18:38
                                if (DateTime.TryParseExact(dateString, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture,
                                                     DateTimeStyles.None, out metadata_Date))
                                {
                                    if (metadata_Date < sorter_Date)
                                    {
                                        sorter_Date = metadata_Date;
                                        sorter_DateSource = ($"{directory.Name} - {tag.Name} = {tag.Description}");
                                    }
                                }
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                ConsoleLog($"Failed to use MetadataExtractor on {FileInfo.OriginalFilePath}.");
                ConsoleLog(ex.Message);
            }

            FileInfo.Sorter_FileName = FileInfo.OriginalFileName;
            FileInfo.Sorter_Date = sorter_Date;
            FileInfo.Sorter_DateSource = sorter_DateSource;

            if (SETTINGS.RenameFiles)
            {
                FileInfo.Sorter_FileName = $"{FileInfo.Sorter_Date:yyyyMMddHHmmss}_{FileInfo.Sorter_FileName}";
            }
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
            LOGS.AppendFormat(Text + "\n");
            Console.WriteLine(Text);
        }

        static void SaveLogs(string DestinationFolderPath)
        {
            string ReportPath = $"{DestinationFolderPath}{SEP}Reports";

            if (Directory.Exists(ReportPath) == false)
            {
                Directory.CreateDirectory(ReportPath);
            }

            File.WriteAllText($"{ReportPath}{SEP}{DateTime.Now:yyyyMMddhhmmss}-SORTER.log", LOGS.ToString());
        }

    }
}