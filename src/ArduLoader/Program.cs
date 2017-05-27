using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ArduboyUploader.Properties;
using Microsoft.Win32;
using Mono.Cecil;

namespace ArduboyUploader
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var p = Process.GetCurrentProcess();
            var f = new FormMain();

            var mutex = new Mutex(true, p.ProcessName + Application.ProductName, out bool instance);

            if (!instance)
            {
                // Search for instances of this application
                var firstSeen = false;

                foreach (var pr in Process.GetProcessesByName(p.ProcessName))
                    if (pr.Id != p.Id)
                        if (!firstSeen)
                        {
                            firstSeen = true;
                            ShowWindow(pr.MainWindowHandle, 5);
                            SetForegroundWindow(pr.MainWindowHandle);
                        }
                        else
                            pr.Kill();
            }
            else
            {
                const string bundledPrefix = "embedded:";

                if (Environment.GetCommandLineArgs().Length <= 1)
                {
                    var input = "";

                    // Check if there is an embedded resource
                    var r = GetBundledFileName(bundledPrefix);

                    if (!string.IsNullOrEmpty(r))

                        try
                        {
                            var name = r.Substring(bundledPrefix.Length);

                            var path = Path.GetTempFileName();
                            File.Delete(path);
                            Directory.CreateDirectory(path);

                            var bundledDestinationPath = Path.Combine(path, name);
                            var outputStream = new FileStream(bundledDestinationPath, FileMode.Create);
                            Assembly.GetEntryAssembly().GetManifestResourceStream(r).CopyTo(outputStream);
                            outputStream.Close();

                            input = bundledDestinationPath;
                        }
                        catch
                        {
                        }

                    if (string.IsNullOrEmpty(input))
                        GetInputFile(f, out input);
                    else
                    {
                        // Special color for the background
                        f.SetAlternativeColor();
                    }
                    f.InputFile = input;
                }
                else
                {
                    var c = Environment.GetCommandLineArgs()[1];

                    switch (c.ToLower().Trim())
                    {
                        case "-register":
                            CheckAssociations();
                            break;

                        case "-bundle":
                        case "-package":
                        {
                            try
                            {
                                if (GetInputFile(f, out string input) &&
                                    GetOutputFile(f, Path.GetFileNameWithoutExtension(input) + "_Uploader",
                                        out string output))
                                {
                                    if (File.Exists(output))
                                        File.Delete(output);

                                    string customIcon = null;
                                    var iconDialog = new OpenFileDialog
                                    {
                                        Filter = "Icon file|*.ico",
                                        Title = "Select a new icon or Cancel to leave the current one"
                                    };
                                    if (iconDialog.ShowDialog(f) == DialogResult.OK)
                                        customIcon = iconDialog.FileName;


                                    var newResource = new EmbeddedResource(bundledPrefix + Path.GetFileName(input),
                                        ManifestResourceAttributes.Public, File.ReadAllBytes(input));
                                    AddResourcesToCurrentAssembly(newResource, bundledPrefix, output);

                                    if (!string.IsNullOrEmpty(customIcon))
                                    {
                                        var iconStream = new FileStream(customIcon, FileMode.Open);
                                        new IconChanger().ChangeIcon(output,
                                            new IconChanger.IconReader(iconStream).Icons);
                                    }

                                    MessageBox.Show(f,
                                        "Package created successfully in: " + Environment.NewLine + output,
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(f, "Error creating the package: " + ex.Message, "Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            }
                        }
                            break;

                        case "-clean":
                        case "-unpackage":
                        case "-unbundle":
                        {
                            try
                            {
                                if (GetOutputFile(f, "ArduboyUploader", out string output))
                                {
                                    AddResourcesToCurrentAssembly(null, bundledPrefix, output);

                                    MessageBox.Show(f,
                                        "Package created successfully in: " + Environment.NewLine + output,
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(f, "Error creating the package: " + ex.Message, "Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            }
                            break;
                        }

                        default:
                            f.InputFile = Environment.GetCommandLineArgs()[1];
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(f.InputFile))
                    Application.Run(f);

                GC.KeepAlive(mutex);
            }
        }

        /// <summary>
        /// Adds and removes resources to the current assembly and copies the result back
        /// </summary>
        /// <param name="add">Resource to add</param>
        /// <param name="removePrefix">Remove resources matching this prefix</param>
        /// <param name="assembly">Output path for the result</param>
        private static void AddResourcesToCurrentAssembly(Resource add, string removePrefix, string assembly)
        {
            File.Copy(Process.GetCurrentProcess().MainModule.FileName, assembly, true);
            var asm = AssemblyDefinition.ReadAssembly(assembly);

            if (!string.IsNullOrEmpty(removePrefix))
                for (var i = asm.MainModule.Resources.Count - 1; i >= 0; i--)
                    if (asm.MainModule.Resources[i].Name.StartsWith(removePrefix))
                        asm.MainModule.Resources.RemoveAt(i);

            if (add != null)
                asm.MainModule.Resources.Add(add);

            asm.Write(assembly);
        }

        private static string GetBundledFileName(string prefix)
        {
            foreach (var r in Assembly.GetEntryAssembly().GetManifestResourceNames())
                if (r.StartsWith(prefix))
                    return r;
            return "";
        }

        private static bool GetOutputFile(IWin32Window form, string tentativeName, out string outputFile)
        {
            var saveFile = new SaveFileDialog
            {
                Filter = "Executable|*.exe",
                Title = "Output for the new Uploader",
                FileName = tentativeName,
            };

            if (saveFile.ShowDialog(form) == DialogResult.OK)
            {
                outputFile = saveFile.FileName;
                return true;
            }

            outputFile = "";
            return false;
        }

        private static bool GetInputFile(IWin32Window form, out string selectedFile)
        {
            var openFile = new OpenFileDialog
            {
                Filter = "Compatible files|*.hex;*.arduboy|Hex File|*.hex|Arduboy File|*.arduboy",
                Title = "Choose the input file for the Arduboy",
            };

            if (openFile.ShowDialog(form) == DialogResult.OK)
            {
                selectedFile = openFile.FileName;
                return true;
            }

            selectedFile = "";
            return false;
        }

        private static void CheckAssociations()
        {
            try
            {
                var l = Process.GetCurrentProcess().MainModule.FileName;

                if (
                    MessageBox.Show("Would you like to associate the '.hex' files with this program?", "Associations",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) ==
                    DialogResult.Yes)
                {
                    var k = "HKEY_CURRENT_USER\\Software\\Classes\\hexfile";
                    Registry.SetValue(k, "", "Hex file");
                    Registry.SetValue(k, "FriendlyTypeName", l);
                    Registry.SetValue(k + "\\shell\\open\\command", "", l + " \"%1\"");
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\.hex", "", "hexfile");
                }

                if (
                    MessageBox.Show("Would you like to associate the '.arduboy' files with this program?",
                        "Associations", MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    var k = "HKEY_CURRENT_USER\\Software\\Classes\\arduboyfile";
                    Registry.SetValue(k, "", "Arduboy file");
                    Registry.SetValue(k, "FriendlyTypeName", l);
                    Registry.SetValue(k + "\\shell\\open\\command", "", l + " \"%1\"");
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\.arduboy", "", "arduboyfile");
                }

                if (
                    MessageBox.Show("Would you like to associate the 'arduboy:' protocol with this program?",
                        "Associations", MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button2) == DialogResult.Yes)
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