using System;
namespace SORTER
{
    public class Settings
    {
        public string SourceFolderPath { get; set; }
        public string PhotosFileExtensions { get; set; }
        public string VideosFileExtensions {get; set; }
        public string PhotosDestinationFolderPath { get; set; }
        public string VideosDestinationFolderPath { get; set; }

        public Settings()
        {
        }
    }
}