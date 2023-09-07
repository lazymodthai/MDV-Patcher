using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Configuration;

namespace MDV_Patcher // Replace with your desired namespace
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        
        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "อัปเดตสำเร็จ-ปิด";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "อัปเดตไม่สำเร็จ-ลองใหม่";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "กำลังดาวน์โหลด";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "กำลังอัปเดต";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Medieval_Dynasty.jpg");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    string z_txt = ConfigurationManager.AppSettings["steam_txt"];
                    Version onlineVersion = new Version(webClient.DownloadString(z_txt));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, new Version(0, 0, 0));
            }
        }

        private void InstallGameFiles(bool isUpdate, Version onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    string z_txt = ConfigurationManager.AppSettings["steam_txt"];
                    onlineVersion = new Version(webClient.DownloadString(z_txt));
                }

                webClient.DownloadFileCompleted += DownloadGameCompletedCallback;
                string z_zip = ConfigurationManager.AppSettings["steam_zip"];
                webClient.DownloadFileAsync(new Uri(z_zip), gameZip, onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"ผิดพลาด: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
            else
            {
                Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public struct Version
    {
        public static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        public Version(short major, short minor, short subMinor)
        {
            this.major = major;
            this.minor = minor;
            this.subMinor = subMinor;
        }

        public Version(string version)
        {
            string[] versionStrings = version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        public bool IsDifferentThan(Version otherVersion)
        {
            if (major != otherVersion.major)
            {
                return true;
            }
            else if (minor != otherVersion.minor)
            {
                return true;
            }
            else if (subMinor != otherVersion.subMinor)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
