# Photo and Video Sorter

Sort and move your Photos and Videos to a target directory based on media's create or modified date.

- Organize your media in /TARGET_LIBRARY/yyyy/MM/yyyyMMdd/FILEFORMAT/ structure
- Checks if duplicate photos / videos in your target directory
- Review the sort operation thru yyyyMMddhhmmss-SORTER.log file report
- Runs on Windows / Mac / Raspbian with .NET Core 3.1 installed

## Sample (Windows)

### Source SD Card :
<pre>
D:\DCIM\103___03\IMG_0887.JPG <-- Created on March 31, 2020
D:\DCIM\103___04\IMG_0888.JPG <-- Created on April 1, 2020
D:\DCIM\103___04\IMG_0889.JPG <-- Created on April 2, 2020
D:\DCIM\103___04\IMG_0889.CR3 <-- Created on April 2, 2020
</pre>

### Photos will be sorted in your target folder as :
<pre>
X:\PHOTOS_LIBRARY\2020\03\20200331\JPG\IMG_0887.JPG
X:\PHOTOS_LIBRARY\2020\04\20200401\JPG\IMG_0888.JPG
X:\PHOTOS_LIBRARY\2020\04\20200401\JPG\IMG_0889.JPG
X:\PHOTOS_LIBRARY\2020\04\20200401\CR3\IMG_0889.CR3
</pre>

## Installation (Windows)

1. [Install .NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) in your system
2. [Download](https://github.com/orexcap/sorter) or clone (git clone https://github.com/orexcap/sorter.git) SORTER
3. Go to your /sorter/SORTER/ directory (E.g. C:\Users\Joseph\Desktop\sorter\SORTER )
4. Run the application for the first time to generate your Sorter.Config.json file
    <pre>sorter\SORTER\>dotnet run</pre>
5. Configure Sorter.Config.json
    <pre>{
        "SourceFolderPath":"D:\DCIM",
        "PhotosFileExtensions":"jpg,jpeg,cr2,cr3,png,gif,heif",
        "VideosFileExtensions":"mp4,mov,avi,mkv",
        "PhotosDestinationFolderPath":"X:\\",
        "VideosDestinationFolderPath":"Y:\\"
    }</pre>
6. Run the application again to begin sorting!
    <pre>sorter\SORTER\>dotnet run</pre>

## File Structure

### On your target folders the following files and folder will be created:
#### FileList.json - Index / list of files in your target folder. Used by the application to check if the file already exists in target folder.
<pre>
    X:\PHOTOS_LIBRARY\FileList.json
</pre>

#### Reports\yyyyMMddhhmmss-SORTER.log
<pre>
    X:\PHOTOS_LIBRARY\Reports\20200401010000-SORTER.log
</pre>