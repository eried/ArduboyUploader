using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArduboyUploader;

namespace ArduLoader
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
            var f = new FormMain();

            if (Environment.GetCommandLineArgs().Length <= 1)
            {
                var openFile = new OpenFileDialog
                {
                    Filter = "Compatible files|*.hex;*.arduboy|Hex File|*.hex|Arduboy File|*.arduboy",
                    Title = "Select a file to upload to Arduboy",
                };
                if (openFile.ShowDialog(f) == DialogResult.OK)
                    f.InputFile = openFile.FileName;
            }
            else
            {
                var c = Environment.GetCommandLineArgs()[1];

                if (c.ToLower().Trim() == "-register")
                    CheckAssociations();
                else
                    f.InputFile = Environment.GetCommandLineArgs()[1];
            }

            if (!String.IsNullOrEmpty(f.InputFile))
                Application.Run(f);
        }

        private static void CheckAssociations()
        {
            try
            {
                var l = Process.GetCurrentProcess().MainModule.FileName;

                if (MessageBox.Show("Would you like to associate the '.hex' files with this program?", "Associations", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    var k = "HKEY_CURRENT_USER\\Software\\Classes\\hexfile";
                    Registry.SetValue(k, "", "Hex file");
                    Registry.SetValue(k, "FriendlyTypeName", l);
                    Registry.SetValue(k+ "\\shell\\open\\command", "", l + " \"%1\"");
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\.hex", "", "hexfile");
                }

                if (MessageBox.Show("Would you like to associate the '.arduboy' files with this program?", "Associations", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    var k = "HKEY_CURRENT_USER\\Software\\Classes\\arduboyfile";
                    Registry.SetValue(k, "", "Arduboy file");
                    Registry.SetValue(k, "FriendlyTypeName", l);
                    Registry.SetValue(k+ "\\shell\\open\\command", "", l + " \"%1\"");
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\.arduboy", "", "arduboyfile");
                }

                if (MessageBox.Show("Would you like to associate the 'arduboy:' protocol with this program?", "Associations", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    RegisterMyProtocol(l);

                SHChangeNotify(134217728, 8192, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Associations", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Application.Exit();
        }

        private static void RegisterMyProtocol(string app)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("arduboy");
            key = Registry.ClassesRoot.CreateSubKey("arduboy");
            key.SetValue(string.Empty, "URL: arduboy Protocol");
            key.SetValue("URL Protocol", string.Empty);
            key = key.CreateSubKey(@"shell\open\command");
            key.SetValue(string.Empty, app + " " + "%1");
            key.Close();
        }

        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
    }
}
