using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace PowerPointLabsInstallerUi
{
    public partial class Form1 : Form
    {
        private readonly string _installerZipAddress = Application.StartupPath + "\\data.zip";
        private const string TextButtonClose = "Close";
        private const string TextButtonRunning = "Running...";
        private const string ErrorWindowTitle = "BiomedPPTX Installer";
        private const string UrlForVstoRuntime = "https://aka.ms/VSTOInstaller";

        private readonly string _onlineInstallerZipAddress = Path.Combine(Path.GetTempPath(),
            @"BiomedPPTXInstaller\olInstaller.zip");

        private readonly string _targetInstallFolder;

        public Form1()
        {
            InitializeComponent();

            // handle special char case for EURO user
            _targetInstallFolder = Path.Combine(
                (IsSpecialCharPresentInInstallPath()
                    ? Path.GetPathRoot(Environment.SystemDirectory)
                    : Path.GetTempPath()),
                @"BiomedPPTXInstaller");
        }

        /// <summary>
        /// If there are special characters present in the install path,
        /// the offline installer (ClickOnce) will fail to install.
        /// </summary>
        private bool IsSpecialCharPresentInInstallPath()
        {
            return new Uri(Path.GetTempPath()).AbsolutePath.Replace("/", "\\") != Path.GetTempPath();
        }

        /// <summary>
        /// main behavior
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text != TextButtonClose)
            {
                //detection for VSTO runtime + its config
                if (!IsVstoRuntimeValid())
                {
                    var dialogResult = MessageBox.Show(
                        "For BiomedPPTX to work properly, your computer needs to have " +
                        "Visual Studio Tools for Office (VSTO) Runtime from Microsoft.\n\n" +
                        "Click Yes to download it, or click No to continue the installation anyway.",
                        ErrorWindowTitle,
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                    if (dialogResult == DialogResult.Yes)
                    {
                        Process.Start(UrlForVstoRuntime);
                        return;
                    }
                    else if (dialogResult == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                else if (!IsVstoConfigValid())
                {
                    var vstoConfigDir = GetVstoConfigDir();
                    if (MessageBox.Show(
                        "A corrupted system file is detected.\n" +
                        "In order to install the add-in, you may need to rename the file [VSTOInstaller.exe.Config] in the folder" +
                        "\n[" + vstoConfigDir + "]\n to the new filename [VSTOInstaller.exe.Config.backup]\n\n" +
                        "However, in some PCs, the corrupted system file won't affect the installation.\n" +
                        "Click OK to continue.",
                        ErrorWindowTitle,
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                        == DialogResult.Cancel)
                    {
                        return;
                    }
                }

                //run installation files
                button1.Enabled = false;
                button1.Text = TextButtonRunning;

                //normal offline installer
                Boolean isUnzipSuccessful = UnzipInstaller(_installerZipAddress);
                if (isUnzipSuccessful)
                {
                    RunInstaller();
                    DeploySmartLibraryAssets();
                    DeployBioArtIndex();
                    DeployTutorial();
                }
                button1.Enabled = true;
                button1.Text = TextButtonClose;
            }
            else
            {
                Close();
            }
        }

        private void AfterOnlineInstallerDownload()
        {
            var isUnzipSuccessful = UnzipInstaller(_onlineInstallerZipAddress);
            if (isUnzipSuccessful)
            {
                MessageBox.Show("In order to install the add-in, please click 'yes' to allow changes.",
                    ErrorWindowTitle);
                RunInstaller();
                DeploySmartLibraryAssets();
                DeployBioArtIndex();
                DeployTutorial();
            }
            button1.Enabled = true;
            button1.Text = TextButtonClose;
        }

        private void WhenDownloadFailure()
        {
            button1.Enabled = true;
            button1.Text = TextButtonClose;
        }

        private static bool IsVstoRuntimeValid()
        {
            var runtimeExistList = new List<bool>();
            Boolean result = false;

            var directoriesForProgramFiles = GetProgramFilesDirectories();
            foreach(string dir in directoriesForProgramFiles)
            {
                var directoryForVstoRuntime = Path.Combine(dir, @"Common Files\Microsoft Shared\VSTO\10.0");
                runtimeExistList.Add(Directory.Exists(directoryForVstoRuntime));
            }
            foreach (var isRuntimeExist in runtimeExistList)
            {
                result = result || isRuntimeExist;
            }
            return result;
        }

        private static bool IsVstoConfigValid()
        {
            var configExistList = new List<bool>();
            Boolean result = false;

            var directoriesForProgramFiles = GetProgramFilesDirectories();
            foreach (string dir in directoriesForProgramFiles)
            {
                var directoryForVstoConfig = Path.Combine(dir,
                    @"Common Files\Microsoft Shared\VSTO\10.0\VSTOInstaller.exe.Config");
                configExistList.Add(File.Exists(directoryForVstoConfig));
            }
            foreach (var isConfigExist in configExistList)
            {
                result = result || isConfigExist;
            }
            return !result;
        }

        private static string GetVstoConfigDir()
        {
            var directoriesForProgramFiles = GetProgramFilesDirectories();
            foreach (string dir in directoriesForProgramFiles)
            {
                var directoryForVstoConfig = Path.Combine(dir,
                    @"Common Files\Microsoft Shared\VSTO\10.0\VSTOInstaller.exe.Config");
                if (File.Exists(directoryForVstoConfig))
                {
                    return directoryForVstoConfig;
                }
            }
            return "";
        }

        private static List<string> GetProgramFilesDirectories()
        {
            var result = new List<string>();
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                result.Add(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
                result.Add(Environment.GetEnvironmentVariable("ProgramW6432"));
            }
            else
            {
                result.Add(Environment.GetEnvironmentVariable("ProgramFiles"));
            }
            return result;
        }

        private void RunInstaller()
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = Path.Combine(_targetInstallFolder, "setup.exe"),
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                PowerPointLabs.Views.ErrorDialogWrapper.ShowDialog("Failed to install",
                    "An error occurred while installing BiomedPPTX. You can right-click on the setup.exe file " +
                    "and select 'Run as Administrator' to try again.", e);
            }
        }

        private void DeploySmartLibraryAssets()
        {
            try
            {
                string assetsZipPath = Path.Combine(Application.StartupPath, "smart-assets.zip");
                if (!File.Exists(assetsZipPath))
                {
                    return;
                }

                string targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BiomedPPTX", "Assets");

                string dbPath = Path.Combine(targetDir, "SMART-Library", "illustrations.db");
                if (File.Exists(dbPath))
                {
                    return;
                }

                Directory.CreateDirectory(targetDir);

                label1.Text = "Deploying SMART-Library assets...";
                label1.Refresh();

                var zip = ZipStorer.Open(assetsZipPath, FileAccess.Read);
                var zipDir = zip.ReadCentralDir();
                int total = zipDir.Count;
                int current = 0;

                foreach (var file in zipDir)
                {
                    string outputPath = Path.Combine(targetDir, file.FilenameInZip);
                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    zip.ExtractFile(file, outputPath);
                    current++;

                    if (current % 100 == 0)
                    {
                        label1.Text = String.Format("Deploying SMART-Library assets... {0}/{1}", current, total);
                        label1.Refresh();
                    }
                }
                zip.Close();

                label1.Text = "SMART-Library assets deployed successfully.";
                label1.Refresh();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to deploy SMART-Library assets: " + e.Message,
                    ErrorWindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DeployBioArtIndex()
        {
            try
            {
                string bioArtPath = Path.Combine(Application.StartupPath, "bioart_index.json");
                if (!File.Exists(bioArtPath))
                {
                    return;
                }

                string targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BiomedPPTX", "Assets");

                Directory.CreateDirectory(targetDir);

                string targetPath = Path.Combine(targetDir, "bioart_index.json");
                if (!File.Exists(targetPath))
                {
                    File.Copy(bioArtPath, targetPath);
                }
            }
            catch (Exception)
            {
            }
        }

        private void DeployTutorial()
        {
            try
            {
                string tutorialSource = Path.Combine(Application.StartupPath, "Tutorial.pptx");
                if (!File.Exists(tutorialSource))
                {
                    return;
                }

                string targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BiomedPPTX");
                Directory.CreateDirectory(targetDir);

                string targetPath = Path.Combine(targetDir, "Tutorial.pptx");
                File.Copy(tutorialSource, targetPath, true);
            }
            catch (Exception)
            {
            }
        }

        private Boolean UnzipInstaller(String installerZipAddress)
        {
            try
            {
                var installerZip = ZipStorer.Open(installerZipAddress, FileAccess.Read);
                var zipDir = installerZip.ReadCentralDir();
                foreach (var file in zipDir)
                {
                    installerZip.ExtractFile(file,
                        Path.Combine(_targetInstallFolder, file.FilenameInZip));
                }
                installerZip.Close();
                return true;
            }
            catch (Exception e)
            {
                PowerPointLabs.Views.ErrorDialogWrapper.ShowDialog("Failed to install",
                    "An error occurred while installing BiomedPPTX", e);
            }
            return false;
        }
    }
}
