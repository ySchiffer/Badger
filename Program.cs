using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Badger
{
    static class Program
    {
        public const string MainMutexName = @"Local\Badger_MainInstance_Mutex";
        public const string ShowWindowEventName = @"Local\Badger_ShowWindow_Event";

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
                        using (Mutex workerMutex = new Mutex(true, @"Local\Badger_GameWorker_Mutex", out bool createdNewWorker))
                        {
                            if (!createdNewWorker) return;

                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            Application.Run(new GameWorkerForm());
                        }
                        return;
                    }
                }

                bool isAutoStart = args.Any(a => a.Equals("--autostart", StringComparison.OrdinalIgnoreCase));
                bool forceShow = args.Any(a => a.Equals("--show", StringComparison.OrdinalIgnoreCase));

                using (Mutex mainMutex = new Mutex(true, MainMutexName, out bool createdNewMain))
                {
                    if (!createdNewMain)
                    {
                        // Ignora disparos automáticos do boot/restauração de sessão; só exibe se for clique manual
                        bool isBootWindow = Environment.TickCount64 < 120_000;
                        if (!isAutoStart && (forceShow || !isBootWindow))
                        {
                            if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out EventWaitHandle? showEvent))
                            {
                                using (showEvent)
                                {
                                    showEvent.Set();
                                }
                            }
                        }
                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }
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