using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Badger
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), e.ExceptionObject?.ToString() ?? "Unknown exception");
                };

                Application.ThreadException += (sender, e) =>
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), e.Exception?.ToString() ?? "Unknown thread exception");
                };

                if (args.Length > 0)
                {
                    if (args[0] == "--install")
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new InstallerForm(false));
                        return;
                    }
                    else if (args[0] == "--uninstall")
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new InstallerForm(true));
                        return;
                    }
                    else if (args[0] == "--game-worker")
                    {
                        bool createdNew;
                        using (Mutex mutex = new Mutex(true, "BadgerGameWorkerMutex", out createdNew))
                        {
                            if (!createdNew) return;

                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            Application.Run(new GameWorkerForm());
                        }
                        return;
                    }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), "Fatal Main Error: " + ex.ToString());
                }
                catch { }
                MessageBox.Show("Erro crítico ao iniciar o Badger:\n\n" + ex.Message, "Badger - Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}