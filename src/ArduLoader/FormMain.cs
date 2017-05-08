using ArduLoader.Properties;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace ArduLoader
{
    public partial class FormMain : Form
    {
        private bool _cancelNow = false;
        private readonly string _input;

        public FormMain(string input)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            InitializeComponent();
            tableLayoutPanelContents.BorderStyle = BorderStyle.FixedSingle;

            _input = input;
            backgroundWorkerUploader.RunWorkerAsync();
        }

        private static bool IsLocalPath(string p, out string clean)
        {
            var u = new Uri(p);
            clean = p;

            if (u.IsFile)
                return true;
            else
            {
                const string arduboyProtocol = "arduboy";
                if (u.Scheme == arduboyProtocol)
                    clean = p.Substring(arduboyProtocol.Length + 1); // Remove the custom protocol

                return false;
            }
        }

        private void backgroundWorkerUploader_DoWork(object sender, DoWorkEventArgs e)
        {
            string hex = "";
            backgroundWorkerUploader.ReportProgress((int)UploadStatus.Searching);

            try
            {
                var input = _input;

                if (IsLocalPath(input,out input))
                {
                    // Decompress if needed
                    if(Path.GetExtension(input).ToLower()==".arduboy")
                    {
                        var tmpFolder = Path.GetTempFileName();
                        File.Delete(tmpFolder);
                        ZipFile.ExtractToDirectory(input, Directory.CreateDirectory(tmpFolder).FullName);

                        foreach(var f in Directory.GetFiles(tmpFolder, "*.hex", SearchOption.AllDirectories))
                        {
                            input = f; // Only use the first one
                            break;
                        }
                    }

                    // Check the hex file (maximum size 90 KB, just to be sure)
                    if (!File.Exists(input) || Path.GetExtension(input).ToLower() != ".hex" || new FileInfo(input).Length > 90 * 1024)
                    {
                        throw new Exception("Invalid hex file");
                    }
                    else
                        hex = input;
                }
                else
                {
                    var tmp = Path.GetTempFileName();

                    // Download file
                    new WebDownload().DownloadFile(input, tmp);
                    hex = tmp;
                }
            }
            catch (Exception)
            {
                backgroundWorkerUploader.ReportProgress((int)UploadStatus.ErrorFile);
                return;
            }

            var s = Stopwatch.StartNew();

            // Search for Arduboy
            string port;
            while (!GetArduboyPort(out port))
            {
                if (_cancelNow)
                    return;

                if (s.ElapsedMilliseconds > 15000)
                {
                    backgroundWorkerUploader.ReportProgress((int)UploadStatus.ErrorTransfering);
                    return;
                }
                Thread.Sleep(1000);
            }

            if (_cancelNow)
                return;

            // Reset it
            try
            {
                var arduboy = new SerialPort(port, 1200);
                arduboy.Open();
                Thread.Sleep(500);
                arduboy.DtrEnable = false;
                arduboy.Close();
            }
            catch
            {
                backgroundWorkerUploader.ReportProgress((int)UploadStatus.ErrorTransfering);
                return;
            }
            s.Restart();

            // Search again
            while (!GetArduboyPort(out port))
            {
                if (s.ElapsedMilliseconds > 6000)
                {
                    backgroundWorkerUploader.ReportProgress((int)UploadStatus.ErrorTransfering);
                    return;
                }
                Thread.Sleep(500);
            }

            if (_cancelNow)
                return;

            // Send Hex
            var processStartInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "avrdude.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                //Arguments = $"-C avrdude.conf -v -p atmega32u4 -c avr109 -P {port} -b 57600 -D -U flash:w:\"{hex}\":i"
                Arguments = $"-C custom.conf -p atmega32u4 -V -q -q -c avr109 -P {port} -b 115200 -D -U flash:w:\"{hex}\":i"
            };

            backgroundWorkerUploader.ReportProgress((int)UploadStatus.Transfering);

            var avrdude = new Process { StartInfo = processStartInfo };
            avrdude.Start();

            if (avrdude.WaitForExit(10000))
            {
                backgroundWorkerUploader.ReportProgress((int)UploadStatus.Done);
                Thread.Sleep(500);
                _cancelNow = true;
            }
            else
            {
                backgroundWorkerUploader.ReportProgress((int)UploadStatus.ErrorTransfering);
                try
                {
                    avrdude.Kill();
                }
                catch (Exception){  }
            }
        }

        private bool GetArduboyPort(out string port)
        {
            port = "";
            using (var s = new ManagementObjectSearcher("SELECT Name, DeviceID, PNPDeviceID FROM Win32_SerialPort WHERE(PNPDeviceID LIKE '%VID_2341%PID_8036%') OR(PNPDeviceID LIKE '%VID_2341%PID_0036%')"))
                foreach (ManagementBaseObject p in s.Get().Cast<ManagementBaseObject>().ToList())
                {
                    port = p.GetPropertyValue("DeviceID").ToString();
                    return true;
                }

            return false;
        }

        private void backgroundWorkerUploader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch((UploadStatus)e.ProgressPercentage)
            {
                case UploadStatus.Searching:
                    pictureBoxStatus.Image = Resources.searching;
                    break;

                case UploadStatus.Transfering:
                    pictureBoxStatus.Image = Resources.transfer;
                    break;

                case UploadStatus.Done:
                    pictureBoxStatus.Image = Resources.done;
                    buttonCancel.Enabled = false;
                    break;

                case UploadStatus.ErrorTransfering:
                    pictureBoxStatus.Image = Resources.error;
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
            if(_cancelNow)
                Application.Exit();
        }

        private void buttonRetry_Click(object sender, EventArgs e)
        {
            if(!backgroundWorkerUploader.IsBusy)
            {
                buttonRetry.Enabled = false;
                backgroundWorkerUploader.RunWorkerAsync();
            }
        }
    }

    internal enum UploadStatus
    {
        Transfering,Done,ErrorTransfering,ErrorFile,Searching,
    }
    public class WebDownload : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public WebDownload() : this(30000) { }

        public WebDownload(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }
}
