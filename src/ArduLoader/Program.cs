using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            var hex = "";
            if (Environment.GetCommandLineArgs().Length <= 1)
            {
                var openFile = new OpenFileDialog();
                openFile.Filter = "Hex File (*.hex)|*.hex";
                openFile.Title = "Select a file to upload to Arduboy";

                if (openFile.ShowDialog() == DialogResult.OK)
                    hex = openFile.FileName;
                else
                    Application.Exit();
            }
            else
            {
                var c = Environment.GetCommandLineArgs()[1];

                if (c.ToLower().Trim() == "-register")
                    CheckAssociations();
                else
                    hex = Environment.GetCommandLineArgs()[1];
            }

            if(hex.Length>0)
                Application.Run(new FormMain(hex));
        }

        private static void CheckAssociations()
        {
            try
            {
                var l = Process.GetCurrentProcess().MainModule.FileName;

                if (MessageBox.Show("Would you like to associate the '.hex' files with this program?", "Associations", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\hexfile", "", "Hex file");
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\hexfile", "FriendlyTypeName", l);
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\hexfile\\shell\\open\\command", "", l + " \"%1\"");
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\.hex", "", "hexfile");
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

            //if (key == null)
            {
                key = Registry.ClassesRoot.CreateSubKey("arduboy");
                key.SetValue(string.Empty, "URL: arduboy Protocol");
                key.SetValue("URL Protocol", string.Empty);
                key = key.CreateSubKey(@"shell\open\command");
                key.SetValue(string.Empty, app + " " + "%1");
            }

            key.Close();
        }

        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
    }
}
