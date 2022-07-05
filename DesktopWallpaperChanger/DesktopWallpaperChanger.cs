using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NLog;

namespace DesktopWallpaperChanger
{
    class WallpaperChanger
    {
        //stolen
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);


        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;
        //endstolen

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        
        public string CurrentMapArtist;
        public string CurrentMapTitle;
        public string CurrentMapMapper;
        
        public void ChangeWallpaper(object songsFolder)
        {
            var osuDirectory = new DirectoryInfo((string)songsFolder);
            var directories = osuDirectory.GetDirectories();
            var isUpdated = false;
            try
            {
                while (!isUpdated)
                {
                    FileInfo[] osuFiles = GetMapFiles(directories);
                    if (osuFiles.Length == 0)
                        continue;
                    var bg = GetMapBackground(osuFiles);
                    if (bg == null)
                        continue;
                    var result = SetWallpaper(bg);
                    isUpdated = result == 1;
                    if (isUpdated)
                        FillMapData(osuFiles);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.StackTrace);
            }
            
        }
        
        //stolen
        private static int SetWallpaper(FileInfo file)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key.SetValue(@"WallpaperStyle", 1.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());
            return SystemParametersInfo(
                SPI_SETDESKWALLPAPER, 
                0,
                file.FullName, 
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                );
        }
        //endstolen
        
        private static FileInfo? GetMapBackground(FileInfo[] osuFiles)
        {
            var rand = new Random();
            var pointer = rand.Next(0, osuFiles.Length);
            var osuFile = osuFiles[pointer];
            return GetBackgroundPath(osuFile);
        }

        private static DirectoryInfo PickMap(DirectoryInfo[] directories)
        {
            var dirCount = directories.Length;
            var rnd = new Random();
            return directories[rnd.Next(0, dirCount)];
        }

        private static FileInfo? GetBackgroundPath(FileInfo osuFile)
        {
            using (StreamReader sr = osuFile.OpenText())
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    var match = Regex.Match(line, @"""(.+\.(?:png|jpg|PNG|JPG))""");
                    if (match.Success)
                        return new FileInfo($"{osuFile.DirectoryName}/{match.Groups[1].Value}");
                }
            }

            return null;
        }

        private static FileInfo[] GetMapFiles(DirectoryInfo[] directories)
        {
            var map = PickMap(directories);
            var mapFiles =  map.GetFiles();
            return mapFiles.Where(file => file.Extension == ".osu").ToArray();
        }

        private void FillMapData(FileInfo[] mapFiles)
        {
            var pattern = @"(.+) - (.+) \((.+)\) \[(.+)\].*";
            var fileName = mapFiles.First(file => file.Extension == ".osu").Name;
            foreach (Match match in Regex.Matches(fileName, pattern))
            {
                CurrentMapArtist = match.Groups[1].Value;
                CurrentMapTitle = match.Groups[2].Value;
                CurrentMapMapper = match.Groups[3].Value;
            }
        }
    }
}