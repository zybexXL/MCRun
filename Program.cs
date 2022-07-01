using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MCRun
{
    static class Program
    {
        internal static string Protocol = "mcrun";

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Count() == 0)
                RegisterHandler();

            //args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            string uri = string.Join("/", args);

            var parsedArgs = uri.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            if (parsedArgs.Count() == 0 || parsedArgs[0].ToLower() == "test")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
                Execute(uri, out _);
        }

        internal static bool RegisterHandler()
        {
            try
            {
                string exe = Application.ExecutablePath;
                string handler = $"\"{exe}\" \"%1\"";
                bool registered = false;
                using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    using (var key = root.CreateSubKey($"Software\\Classes\\{Protocol}\\shell\\open\\command", true))
                    {
                        string currValue = key.GetValue("", null)?.ToString();
                        if (currValue != handler)
                        {
                            key.SetValue("", handler);
                            registered = true;
                        }
                    }
                }

                if (registered)
                {
                    // notify windows
                    SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                    MessageBox.Show($"{Protocol}:// handler is now registered", "Handler registered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register the {Protocol}:// handler!\n\n{ex}", "Handler registration failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        internal static bool Execute(string cmdline, out string error)
        {
            Program.Parse(cmdline, out var cmd, out var args);

            error = "could not parse command";
            if (string.IsNullOrEmpty(cmd)) 
                return false;

            try
            {
                using (Process process = new Process())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = cmd;
                    startInfo.Arguments = args;
                    startInfo.UseShellExecute = true;
                    process.StartInfo = startInfo;

                    bool ok = process.Start();
                    error = ok ? "" : "Process already running?";
                    return ok;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return false;
        }

        internal static void Parse(string line, out string cmd, out string arguments)
        {
            line = Regex.Replace(line, $"^{Protocol}:/*", "", RegexOptions.IgnoreCase);

            bool addSlash = false;
            List<string> args = new List<string>();
            foreach (string arg in line.Split(new char[] { '/' }))
            {
                if (string.IsNullOrEmpty(arg))
                    addSlash = true;
                else
                {
                    string value = addSlash ? $"/{arg}" : arg;
                    if (value.Contains(" ")) value = $"\"{value}\"";
                    args.Add(value);
                    addSlash = false;
                }
            }

            if (args.Count > 0 && args[0].ToLower() == "test")
                args = args.Skip(1).ToList();

            cmd = args.Count > 0 ? args[0] : "";
            arguments = args.Count > 1 ? string.Join(" ", args.Skip(1)) : "";
            if (cmd.ToLower() == "shell")
            {
                cmd = "cmd.exe";
                arguments = $"/c {arguments}";
            }
        }
    }
}
