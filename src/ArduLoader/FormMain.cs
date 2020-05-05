using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ArduboyUploader.Properties;
using System.Text.RegularExpressions;

namespace ArduboyUploader
{
    public partial class FormMain : Form
    {
        private bool _cancelNow, _alreadyFailed;

        public FormMain()
        {
            InitializeComponent();
            tableLayoutPanelContents.BorderStyle = BorderStyle.FixedSingle;

            // Resize based on DPI
            using (var g = CreateGraphics())
            {
                ResizeFactor((int) Math.Floor(g.DpiX / 48));
            }
        }

        internal string InputFile { get; set; }

        private void ResizeFactor(int factor)
        {
            Width = 128 * factor + Width - pictureBoxStatus.Width;
            Height = 64 * factor + Height - pictureBoxStatus.Height;
            CenterToScreen();
        }

        /// <summary>
        ///     Checks if the provided path is local or remote
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <param name="clean">Return the path clean if needed</param>
        /// <returns>True if the path is local</returns>
        private static bool IsLocalPath(string path, out string clean)
        {
            var u = new Uri(path);
            clean = path;

            if (u.IsFile)
                return true;
            const string arduboyProtocol = "arduboy";
            if (u.Scheme == arduboyProtocol)
                clean = path.Substring(arduboyProtocol.Length + 1); // Remove the custom protocol

            return false;
        }

        private void backgroundWorkerUploader_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorkerUploader.ReportProgress((int) UploadStatus.Searching);

            try
            {
                string hex;
                try
                {
                    var input = InputFile;

                    if (!IsLocalPath(input, out input))
                    {
                        var tmp = Path.GetTempFileName();

                        // Download file
                        File.Delete(tmp);
                        tmp = Path.ChangeExtension(tmp,
                            ".hex"); // Program is going to try to decompress it without checking the extension
                        new WebDownload().DownloadFile(input, tmp);
                        input = tmp;
                    }

                    // Decompress (if possible)
                    try
                    {
                        var tmpFolder = Path.GetTempFileName();
                        File.Delete(tmpFolder);
                        ZipFile.ExtractToDirectory(input, Directory.CreateDirectory(tmpFolder).FullName);

                        foreach (var f in Directory.GetFiles(tmpFolder, Resources.FileFilterHex,
                            SearchOption.AllDirectories))
                        {
                            input = f; // Only use the first one
                            break;
                        }
                    }
                    catch
                    {
                        // Not a zip file
                    }

                    // Check the hex file (maximum size and extension just to be sure)
                    if (!File.Exists(input) || Path.GetExtension(input).ToLower() != ".hex" ||
                        Path.GetFileName(input).Contains(Settings.Default.PreventUploadIfFilenameContains) ||
                        new FileInfo(input).Length > Settings.Default.MaximumHexFilesizeKB * 1024 ||
                        !string.IsNullOrEmpty(Settings.Default.PreventUploadIfFileContains) && 
                        Regex.IsMatch(File.ReadAllText(input),Settings.Default.PreventUploadIfFileContains))
                        throw new Exception("Invalid hex file");

                    hex = input;
                }
                catch (Exception)
                {
                    backgroundWorkerUploader.ReportProgress((int) UploadStatus.ErrorFile);
                    return;
                }

                var s = Stopwatch.StartNew();

                // Search for Arduboy
                string port;
                while (!GetArduboyPort(out port))
                {
                    if (_cancelNow)
                        return;

                    if (s.ElapsedMilliseconds > Resources.WaitSearchingMs)
                    {
                        LogError("Timeout waiting for the Arduboy");
                        backgroundWorkerUploader.ReportProgress((int) UploadStatus.ErrorTransfering);
                        return;
                    }
                    Thread.Sleep(Resources.WaitIdleSlowMs);
                }

                if (_cancelNow)
                    return;

                // Reset it
                try
                {
                    SendReset(port);
                }
                catch (Exception ex)
                {
                    LogError("Error putting the Arduboy in bootloader: " + ex.Message);
                    backgroundWorkerUploader.ReportProgress((int) UploadStatus.ErrorTransfering);
                    //return;
                }

                s.Restart();

                // Prepare AvrDude
                ExtractAvrDudeFromResources();

                while (s.ElapsedMilliseconds < Resources.WaitAfterResetMs) // Wait 1s
                    Thread.Sleep(Resources.WaitIdleFastMs);

                // Search again
                while (!GetArduboyPort(out port))
                {
                    if (s.ElapsedMilliseconds > Resources.WaitBootloaderMs
                    ) // Maximum the bootloader waits so it is pointless to wait more
                    {
                        LogError("Timeout waiting for the Arduboy to appear again");
                        backgroundWorkerUploader.ReportProgress((int) UploadStatus.ErrorTransfering);
                        return;
                    }
                    Thread.Sleep(Resources.WaitIdleSlowMs);
                }

                if (_cancelNow)
                    return;

                backgroundWorkerUploader.ReportProgress((int) UploadStatus.Transfering);
                var status = UploadViaAvrDude(port, hex);
                backgroundWorkerUploader.ReportProgress((int) status);

                if (status != UploadStatus.Done) return; // Nothing more to do

                backgroundWorkerUploader.ReportProgress((int) status);
                Thread.Sleep(Resources.WaitSuccessMs);
                _cancelNow = true;
            }
            catch (Exception ex)
            {
                LogError("General error: " + ex.Message);
                backgroundWorkerUploader.ReportProgress((int) UploadStatus.ErrorTransfering);
            }
        }

        /// <summary>
        ///     Check and prepares AvrDude from embedded resources. Also sets Environment.CurrentDirectory
        /// </summary>
        private static void ExtractAvrDudeFromResources()
        {
            var appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Application.ProductName, Resources.ParamAvrDudeFolder);

            if (!Directory.Exists(appPath))
                Directory.CreateDirectory(appPath);

            Environment.CurrentDirectory = appPath;

            // Prepare AvrDude
            if (!File.Exists("avrdude.exe"))
                File.WriteAllBytes("avrdude.exe", Resources.avrdude);

            if (!File.Exists("custom.conf"))
                File.WriteAllBytes("custom.conf", Resources.custom);

            if (!File.Exists("libusb0.dll"))
                File.WriteAllBytes("libusb0.dll", Resources.libusb0);
        }

        /// <summary>
        ///     Send reset signal via opening and closing the port at 1200 bauds
        /// </summary>
        /// <param name="port">COM port</param>
        private static void SendReset(string port)
        {
            var arduboy = new SerialPort
            {
                BaudRate = 1200,
                PortName = port,
                DtrEnable = true
            };
            arduboy.Open();

            while (!arduboy.IsOpen)
                Thread.Sleep(Resources.WaitIdleFastMs);

            arduboy.DtrEnable = false;
            arduboy.Close();
            arduboy.Dispose();
        }

        /// <summary>
        ///     Uploads an hex file via AvrDude
        /// </summary>
        /// <param name="port">COM port</param>
        /// <param name="hex">Full path to the file</param>
        /// <returns>The result of the operation</returns>
        private static UploadStatus UploadViaAvrDude(string port, string hex)
        {
            // Send Hex
            var processStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = "avrdude.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                //Arguments = $"-C avrdude.conf -v -p atmega32u4 -c avr109 -P {port} -b 57600 -D -U flash:w:\"{hex}\":i"
                Arguments =
                    $"-C custom.conf -p atmega32u4 -V -q -c avr109 -P {port} -b 115200 -D -U flash:w:\"{hex}\":i"
            };

            var avrdude = new Process {StartInfo = processStartInfo};
            var output = new StringBuilder();
            var error = new StringBuilder();

            avrdude.OutputDataReceived += (sender1, e1) =>
            {
                if (e1.Data != null)
                    output.AppendLine(e1.Data);
            };
            avrdude.ErrorDataReceived += (sender1, e1) =>
            {
                if (e1.Data != null)
                    error.AppendLine(e1.Data);
            };

            avrdude.Start();
            avrdude.BeginErrorReadLine();
            avrdude.BeginOutputReadLine();
            avrdude.WaitForExit(6000);
            var standardOutput = output.Append(error).ToString();

            var status = UploadStatus.ErrorAvrDude;

            // Check the output to see if it was successful
            if (standardOutput.Contains("bytes of flash written") && standardOutput.Contains("Fuses OK") &&
                standardOutput.Contains("AVR device initialized") && standardOutput.Contains("done"))
            {
                status = UploadStatus.Done;
            }
            else
            {
                LogError("Error uploading the file to the Arduboy: " + standardOutput);

                try
                {
                    avrdude.Kill(); // Stop missing instances, if any
                }
                catch
                {
                }
            }
            return status;
        }

        /// <summary>
        ///     Logs the errors in the Windows log, under the Application branch (to avoid permission issues)
        /// </summary>
        /// <param name="msg">Message to log</param>
        private static void LogError(string msg)
        {
            try
            {
                using (var eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(msg, EventLogEntryType.Warning);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Looks up for a connected device
        /// </summary>
        /// <param name="port">COM port of the first device found (if any)</param>
        /// <returns>True if a device was found</returns>
        private static bool GetArduboyPort(out string port)
        {
            port = "";
            using (var s = new ManagementObjectSearcher(
                "SELECT Name, DeviceID, PNPDeviceID FROM Win32_SerialPort WHERE" +
                "(PNPDeviceID LIKE '%VID_2341%PID_8036%') OR " + "(PNPDeviceID LIKE '%VID_2341%PID_0036%') OR " +
                "(PNPDeviceID LIKE '%VID_1B4F%PID_9205%') OR " + "(PNPDeviceID LIKE '%VID_1B4F%PID_9206%') OR " +
                "(PNPDeviceID LIKE '%VID_2A03%PID_0036%')")
            ) // SparkFun Pro Micro
            {
                foreach (var p in s.Get().Cast<ManagementBaseObject>().ToList())
                {
                    port = p.GetPropertyValue("DeviceID").ToString();
                    return true;
                }
            }
            return false;
        }

        private void backgroundWorkerUploader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var status = (UploadStatus) e.ProgressPercentage;

            switch (status)
            {
                case UploadStatus.Searching:
                    pictureBoxStatus.Image = _alreadyFailed? Resources.retry: Resources.searching;
                    break;

                case UploadStatus.Transfering:
                    pictureBoxStatus.Image = Resources.transfer;
                    break;

                case UploadStatus.Done:
                    pictureBoxStatus.Image = Resources.done;
                    buttonCancel.Enabled = false;
                    break;

                case UploadStatus.ErrorAvrDude:
                case UploadStatus.ErrorTransfering:
                    pictureBoxStatus.Image = status == UploadStatus.ErrorAvrDude ? Resources.error3 : Resources.error;
                    buttonCancel.Text = "&Close";
                    buttonRetry.Enabled = true;
                    buttonRetry.Visible = true;
                    break;

                case UploadStatus.ErrorFile:
                    pictureBoxStatus.Image = Resources.error2;
                    buttonCancel.Text = "&Close";
                    break;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (backgroundWorkerUploader.IsBusy)
                _cancelNow = true;
            else
                Application.Exit();
            buttonCancel.Enabled = false;
        }

        private void backgroundWorkerUploader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_cancelNow)
                Application.Exit();
        }

        private void buttonRetry_Click(object sender, EventArgs e)
        {
            if (backgroundWorkerUploader.IsBusy) return;
            buttonRetry.Enabled = false;
            _alreadyFailed = true;
            backgroundWorkerUploader.RunWorkerAsync();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            backgroundWorkerUploader.RunWorkerAsync();
        }

        /// <summary>
        ///     Set a custom color for show that the embedded file is being use
        /// </summary>
        public void SetAlternativeColor()
        {
            BackColor = Color.Yellow;
            buttonCancel.BackColor = BackColor;
            buttonRetry.BackColor = BackColor;
        }
    }

    internal enum UploadStatus
    {
        Transfering,
        Done,
        ErrorTransfering,
        ErrorFile,
        Searching,
        ErrorAvrDude
    }
}