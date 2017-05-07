using ArduLoader.Properties;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
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

        public FormMain(string input)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            InitializeComponent();
            tableLayoutPanelContents.BorderStyle = BorderStyle.FixedSingle;

            backgroundWorkerUploader.RunWorkerAsync(input);
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

            try
            {
                var input = e.Argument as string;

                if (IsLocalPath(input,out input))
                {
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
                Thread.Sleep(1000);
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
                //57600 
                //Arguments = $"-C avrdude.conf -v -p atmega32u4 -c avr109 -P {port} -b 115200 -D -U flash:w:\"{hex}\":i"
                Arguments = $"-C custom.conf -p atmega32u4 -V -q -q -c avr109 -P {port} -b 115200 -D -U flash:w:\"{hex}\":i"
            };

            backgroundWorkerUploader.ReportProgress((int)UploadStatus.Transfering);

            var avrdude = new Process { StartInfo = processStartInfo };
            avrdude.Start();

            if (avrdude.WaitForExit(10000))
            {
                backgroundWorkerUploader.ReportProgress((int)UploadStatus.Done);
                Thread.Sleep(1000);
                _cancelNow = true;
            }
            else
            {
                backgroundWorkerUploader.ReportProgress((int)UploadStatus.ErrorTransfering);
                try
                {
                    avrdude.Kill();
                }
                catch (Exception)
                {

                }
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
    }

    internal enum UploadStatus
    {
        Transfering=1,Done,ErrorTransfering,ErrorFile,
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
