using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Badger
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Label lblStatusLabel;
        private Panel pnlIndicator;
        private Label lblStatusValue;
        private Button btnToggle;
        private Button btnConfig;
        private CustomDropdownMenu configMenu;
        private ToolStripMenuItem? trayToggleMenuItem;
        private bool isRunning = false;
        private Process? gameProcess = null;
        private long lastMenuCloseTick = 0;
        
        public bool HideToTrayOnClose { get; private set; } = false;
        public bool StartWithWindows { get; private set; } = false;

        private const string RegistryRunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppRegistryName = "BadgerGameActivity";

        public MainForm()
        {
            trayMenu = new ContextMenuStrip();
            trayIcon = new NotifyIcon();
            lblStatusLabel = new Label();
            pnlIndicator = new Panel();
            lblStatusValue = new Label();
            btnToggle = new Button();
            btnConfig = new Button();

            InitializeComponentCustom();
            configMenu = new CustomDropdownMenu(this);
            LoadSettings();
            ThemeHelper.ApplyTheme(this);
            StartActivity();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Badger";
            this.Width = 265;
            this.Height = 148;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Carregamento direto do arquivo app.ico da pasta do aplicativo (garantido pelo instalador)
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    Icon appIcon = new Icon(iconPath);
                    this.Icon = appIcon;
                    trayIcon.Icon = appIcon;
                }
                else
                {
                    // Fallback caso rode de outro diretório de desenvolvimento
                    Icon fallbackIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
                    this.Icon = fallbackIcon;
                    trayIcon.Icon = fallbackIcon;
                }
            }
            catch
            {
                this.Icon = SystemIcons.Application;
                trayIcon.Icon = SystemIcons.Application;
            }

            // Botão de Configuração
            btnConfig.Text = "⚙";
            btnConfig.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            btnConfig.Left = 210;
            btnConfig.Top = 12;
            btnConfig.Width = 28;
            btnConfig.Height = 26;
            btnConfig.Click += BtnConfig_Click;
            this.Controls.Add(btnConfig);

            // Status Layout
            lblStatusLabel.Text = "Status:";
            lblStatusLabel.Left = 14;
            lblStatusLabel.Top = 15;
            lblStatusLabel.Width = 45;
            lblStatusLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            this.Controls.Add(lblStatusLabel);

            pnlIndicator.Left = 58;
            pnlIndicator.Top = 19;
            pnlIndicator.Width = 8;
            pnlIndicator.Height = 8;
            this.Controls.Add(pnlIndicator);

            lblStatusValue.Left = 70;
            lblStatusValue.Top = 15;
            lblStatusValue.Width = 140;
            lblStatusValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            this.Controls.Add(lblStatusValue);

            // Botão compacto centralizado
            btnToggle.Left = 14;
            btnToggle.Top = 48;
            btnToggle.Width = 224;
            btnToggle.Height = 28;
            btnToggle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnToggle.Click += BtnToggle_Click;
            this.Controls.Add(btnToggle);

            // Créditos
            Label lblCredits = new Label()
            {
                Text = "Schiffer",
                Left = 14,
                Top = 84,
                Width = 224,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic)
            };
            this.Controls.Add(lblCredits);

            // Tray Menu
            trayMenu.Items.Add("Abrir", null, (s, e) => ShowForm());
            trayToggleMenuItem = (ToolStripMenuItem)trayMenu.Items.Add("Parar", null, (s, e) => ToggleActivity());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Sair", null, (s, e) => ExitApp());

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.Text = "Badger";
            trayIcon.DoubleClick += (s, e) => ShowForm();

            this.FormClosing += MainForm_FormClosing;
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (HideToTrayOnClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void BtnToggle_Click(object? sender, EventArgs e)
        {
            ToggleActivity();
        }

        private void ToggleActivity()
        {
            if (isRunning)
            {
                StopActivity();
            }
            else
            {
                StartActivity();
            }
        }

        private void StartActivity()
        {
            try
            {
                string currentExe = Application.ExecutablePath;
                string currentDir = Path.GetDirectoryName(currentExe) ?? AppDomain.CurrentDomain.BaseDirectory;
                string exeNameWithoutExt = Path.GetFileNameWithoutExtension(currentExe);
                
                string workerExe = Path.Combine(currentDir, "Stardew Valley.exe");
                
                if (currentExe.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Você está rodando diretamente como .DLL. Compile o projeto como executável (.exe) para que o Windows consiga gerenciar o processo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // Se o executável do worker já existir, remove o atributo de oculto temporariamente para permitir a sobrescrita
                    if (File.Exists(workerExe))
                    {
                        File.SetAttributes(workerExe, FileAttributes.Normal);
                    }

                    File.Copy(currentExe, workerExe, true);
                    // Oculta o executável do worker na pasta
                    File.SetAttributes(workerExe, FileAttributes.Hidden);

                    string[] extensions = { ".dll", ".runtimeconfig.json", ".deps.json" };
                    foreach (var ext in extensions)
                    {
                        string original = Path.Combine(currentDir, exeNameWithoutExt + ext);
                        string target = Path.Combine(currentDir, "Stardew Valley" + ext);
                        if (File.Exists(original))
                        {
                            if (File.Exists(target))
                            {
                                File.SetAttributes(target, FileAttributes.Normal);
                            }
                            File.Copy(original, target, true);
                            // Oculta as dependências do worker
                            File.SetAttributes(target, FileAttributes.Hidden);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (!File.Exists(workerExe)) throw;
                }
                catch (IOException)
                {
                    if (!File.Exists(workerExe)) throw;
                }

                if (File.Exists(workerExe))
                {
                    gameProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = workerExe,
                        Arguments = "--game-worker",
                        UseShellExecute = true,
                        WorkingDirectory = currentDir
                    });
                }
                else
                {
                    throw new FileNotFoundException("Executável do jogo não pôde ser criado.");
                }

                isRunning = true;
                lblStatusValue.Text = "Reproduzindo";
                lblStatusValue.ForeColor = Color.Green;
                pnlIndicator.BackColor = Color.Green;
                btnToggle.Text = "⏸ Parar";
                trayIcon.Text = "Badger (Ativo)";
                if (trayToggleMenuItem != null) trayToggleMenuItem.Text = "Parar";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar atividade: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatusValue.Text = "Parado";
                lblStatusValue.ForeColor = Color.Red;
                pnlIndicator.BackColor = Color.Red;
            }
        }

        private void StopActivity()
        {
            try
            {
                if (gameProcess != null && !gameProcess.HasExited)
                {
                    gameProcess.Kill();
                    gameProcess.Dispose();
                    gameProcess = null;
                }

                foreach (var p in Process.GetProcessesByName("Stardew Valley"))
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch
            {
            }

            isRunning = false;
            lblStatusValue.Text = "Parado";
            lblStatusValue.ForeColor = Color.Red;
            pnlIndicator.BackColor = Color.Red;
            btnToggle.Text = "▶ Iniciar";
            trayIcon.Text = "Badger (Parado)";
            if (trayToggleMenuItem != null) trayToggleMenuItem.Text = "Iniciar";
        }

        private void BtnConfig_Click(object? sender, EventArgs e)
        {
            if (Environment.TickCount - lastMenuCloseTick < 250)
                return;

            if (configMenu.Visible)
            {
                configMenu.Hide();
                lastMenuCloseTick = Environment.TickCount;
            }
            else
            {
                configMenu.UpdateStates();
                
                Point rightEdgeButton = btnConfig.PointToScreen(new Point(btnConfig.Width, btnConfig.Height + 4));
                configMenu.Location = new Point(rightEdgeButton.X, rightEdgeButton.Y);
                
                configMenu.Show(this);
            }
        }

        public void NotifyMenuClosed()
        {
            lastMenuCloseTick = Environment.TickCount;
        }

        public void SetHideToTray(bool value)
        {
            HideToTrayOnClose = value;
            SaveSettings();
        }

        public void SetStartup(bool value)
        {
            StartWithWindows = value;
            SaveSettings();
        }

        private void LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, false))
                {
                    if (key != null)
                    {
                        var val = key.GetValue(AppRegistryName);
                        StartWithWindows = val != null;
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
                {
                    if (key != null)
                    {
                        if (StartWithWindows)
                        {
                            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                            if (exePath != null)
                            {
                                key.SetValue(AppRegistryName, $"\"{exePath}\"");
                            }
                        }
                        else
                        {
                            if (key.GetValue(AppRegistryName) != null)
                            {
                                key.DeleteValue(AppRegistryName);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void ExitApp()
        {
            StopActivity();
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}