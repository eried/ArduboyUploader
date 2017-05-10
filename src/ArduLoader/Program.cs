using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ArduboyUploader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
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
            var k = Registry.ClassesRoot.OpenSubKey("arduboy");
            k = Registry.ClassesRoot.CreateSubKey("arduboy");
            k.SetValue(string.Empty, "URL: arduboy Protocol");
            k.SetValue("URL Protocol", string.Empty);
            k = k.CreateSubKey(@"shell\open\command");
            k.SetValue(string.Empty, app + " " + "%1");
            k.Close();
        }

        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
    }
}
