using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.Win32;

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

        public string CurrentMapArtist;
        public string CurrentMapTitle;
        public string CurrentMapMapper;
        
        public void ChangeWallpaper(object songsFolder)
        {
            var osuDirectory = new DirectoryInfo((string)songsFolder);
            var directories = osuDirectory.GetDirectories();
            var isUpdated = false;
            FileInfo[] mapFiles = null;
            while (!isUpdated)
            {
                var map = PickMap(directories);
                mapFiles = map.GetFiles();
                var bg = GetMapBackground(mapFiles);
                if (bg == null)
                    continue;
                var result = SetWallpaper(bg);
                isUpdated = result == 1;
            }
            FillMapData(mapFiles);
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

        private static FileInfo GetMapBackground(FileInfo[] mapFiles)
        {
            var pics = mapFiles.Where(file => file.Extension == ".png" || file.Extension == ".jpg" || file.Extension == ".jpeg").ToList();
            
            if (!pics.Any() || pics.Exists(file => file.Extension == ".osu"))
            {
                Console.WriteLine("List is empty");
                return null;
            }
            
            var wallpaperFile = pics[0];
            foreach (var pic in pics)
            {
                if (pic.Length > wallpaperFile.Length)
                    wallpaperFile = pic;
            }
            return wallpaperFile;
        }

        private static DirectoryInfo PickMap(DirectoryInfo[] directories)
        {
            var dirCount = directories.Length;
            var rnd = new Random();
            return directories[rnd.Next(0, dirCount)];
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