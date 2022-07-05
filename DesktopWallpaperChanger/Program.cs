using System;
using System.IO;
using System.Windows.Forms;
using DesktopWallpaperChanger.Properties;
using IniParser;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;

namespace DesktopWallpaperChanger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppContext());
        }
    }

    public class AppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private System.Threading.Timer timer;
        private int sleepTime = 30*60*1000;
        private WallpaperChanger wc;
        public AppContext()
        {
            SetupLogging();
            wc = new WallpaperChanger();
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.favicon,
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items =
                    {
                        new ToolStripMenuItem("Change wallpaper", null, ChangeWallpaper),
                        new ToolStripMenuItem("Copy map data to clipboard", null, CopyMapInfoToClipboard),
                        new ToolStripMenuItem("Exit", null, Exit),
                    }
                },
                Text = "osu! Wallpaper Changer",
            };
            trayIcon.Click += ChangeWallpaper;
            var songsFolder = CheckCfg();
            timer = new System.Threading.Timer(wc.ChangeWallpaper, songsFolder, 0, sleepTime);
        }

        private string CheckCfg()
        {
            var parser = new FileIniDataParser();
            var dir = Directory.GetCurrentDirectory();
            if (!File.Exists( dir + "/cfg.ini"))
                CreateCgfFile(parser, dir);
            var cfgData = parser.ReadFile("cfg.ini");
            var songsFolder = cfgData["Config"]["SongsFolder"];
            return songsFolder;
        }

        private void CreateCgfFile(FileIniDataParser parser, string dir)
        {
            var file = File.Create(dir + "/cfg.ini");
            file.Close();
            var data = parser.ReadFile("cfg.ini");
            data.Sections.AddSection("Config");
            var songsFolderPath = GetSongsFolderPath();
            if (songsFolderPath == null)
            {
                TrueExit();
                return;
            }
            data["Config"].AddKey("SongsFolder", songsFolderPath);
            parser.WriteFile("cfg.ini", data);
        }
        
        private string? GetSongsFolderPath()
        {
            var fbd = new CommonOpenFileDialog();
            fbd.IsFolderPicker = true;
            var result = fbd.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
                return fbd.FileName;
            return null;
        }

        private void Exit(object sender, EventArgs e)
        {
            TrueExit();
        }

        private void TrueExit()
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        private void ChangeWallpaper(object sender, EventArgs e)
        {
            MouseEventArgs me;
            try
            {
                me = (MouseEventArgs) e;
            }
            catch(InvalidCastException)
            {
                timer.Change(0, sleepTime);
                return;
            }
            if ((me.Button & MouseButtons.Left) != 0)
                timer.Change(0, sleepTime);
        }

        private void CopyMapInfoToClipboard(object sender, EventArgs e)
        {
            Clipboard.SetText($"{wc.CurrentMapArtist} - {wc.CurrentMapTitle} by {wc.CurrentMapMapper}");
        }

        private void SetupLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") {FileName = $"{Directory.GetCurrentDirectory()}/logs.txt"};
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;
        }
    }
}