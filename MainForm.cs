using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Badger
{
    public partial class MainForm : Form
    {
        [DllImport("kernel32.dll")]
        private static extern int UnregisterApplicationRestart();

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Label lblStatusLabel;
        private StatusDot pnlIndicator;
        private Label lblStatusValue;
        private Button btnToggle;
        private Button btnConfig;
        private CircularResetButton btnReset;
        private Label lblTimer;
        private ToolTip toolTip;
        private CustomDropdownMenu configMenu;
        private ToolStripMenuItem? trayToggleMenuItem;
        private System.Windows.Forms.Timer activityTimer;

        private bool isRunning = false;
        private Process? gameProcess = null;
        private long lastMenuCloseTick = 0;
        private readonly long startupTick = Environment.TickCount64;
        private bool allowShowDisplay = false;
        private EventWaitHandle? showWindowEvent;
        private long totalSeconds = 0;

        public bool HideToTrayOnClose { get; private set; } = false;
        public bool StartWithWindows { get; private set; } = false;
        public bool StartInTray { get; private set; } = false;

        private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "BadgerGameActivity";
        private const string RegistryAppKey = @"SOFTWARE\Badger";

        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Badger",
            "settings.ini");

        public MainForm()
        {
            // Impede que o Windows Restart Manager reabra o Badger automaticamente após reiniciar o PC
            try { UnregisterApplicationRestart(); } catch { }

            LoadSettings();
            allowShowDisplay = !StartInTray;

            trayMenu = new ContextMenuStrip();
            trayIcon = new NotifyIcon();
            lblStatusLabel = new Label();
            pnlIndicator = new StatusDot();
            lblStatusValue = new Label();
            btnToggle = new Button();
            btnConfig = new Button();
            btnReset = new CircularResetButton();
            lblTimer = new Label();
            toolTip = new ToolTip();
            activityTimer = new System.Windows.Forms.Timer();

            InitializeComponentCustom();
            configMenu = new CustomDropdownMenu(this);
            ThemeHelper.ApplyTheme(this);
            ThemeHelper.ApplyContextMenuTheme(trayMenu);
            UpdateTimerDisplay();

            activityTimer.Interval = 1000;
            activityTimer.Tick += ActivityTimer_Tick;

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;

            if (StartInTray && !this.IsHandleCreated)
            {
                this.CreateHandle();
            }

            StartSingleInstanceListener();
            StartActivity();
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowShowDisplay ? value : false);
        }

        private void StartSingleInstanceListener()
        {
            try
            {
                showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowWindowEventName);
                Thread listenerThread = new Thread(() =>
                {
                    while (showWindowEvent != null)
                    {
                        try
                        {
                            if (showWindowEvent.WaitOne() && !this.IsDisposed && this.IsHandleCreated)
                            {
                                this.BeginInvoke(new Action(ShowForm));
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                })
                {
                    IsBackground = true
                };
                listenerThread.Start();
            }
            catch
            {
            }
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Badger";
            this.Width = 265;
            this.Height = 148;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

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

            btnConfig.Text = "⚙";
            btnConfig.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            btnConfig.Left = 210;
            btnConfig.Top = 12;
            btnConfig.Width = 28;
            btnConfig.Height = 26;
            btnConfig.Click += BtnConfig_Click;
            this.Controls.Add(btnConfig);

            lblStatusLabel.Text = "Status:";
            lblStatusLabel.Left = 14;
            lblStatusLabel.Top = 15;
            lblStatusLabel.Width = 45;
            lblStatusLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            this.Controls.Add(lblStatusLabel);

            pnlIndicator.Left = 58;
            pnlIndicator.Top = 19;
            pnlIndicator.Width = 9;
            pnlIndicator.Height = 9;
            this.Controls.Add(pnlIndicator);

            lblStatusValue.Left = 71;
            lblStatusValue.Top = 15;
            lblStatusValue.Width = 138;
            lblStatusValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            this.Controls.Add(lblStatusValue);

            btnToggle.Left = 14;
            btnToggle.Top = 48;
            btnToggle.Width = 224;
            btnToggle.Height = 28;
            btnToggle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnToggle.Click += BtnToggle_Click;
            this.Controls.Add(btnToggle);

            // Ícone circular de Resetar Contador (canto inferior esquerdo)
            btnReset.Left = 14;
            btnReset.Top = 81;
            btnReset.Width = 20;
            btnReset.Height = 20;
            btnReset.Click += BtnReset_Click;
            toolTip.SetToolTip(btnReset, "Resetar contador");
            this.Controls.Add(btnReset);

            // Contador de tempo diretamente à direita do ícone circular
            lblTimer.Text = "0h 00m";
            lblTimer.Left = 40;
            lblTimer.Top = 83;
            lblTimer.Width = 115;
            lblTimer.Height = 16;
            lblTimer.TextAlign = ContentAlignment.MiddleLeft;
            lblTimer.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            this.Controls.Add(lblTimer);

            // Créditos no canto inferior direito
            Label lblCredits = new Label()
            {
                Text = "Schiffer",
                Left = 168,
                Top = 83,
                Width = 70,
                Height = 16,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic)
            };
            this.Controls.Add(lblCredits);

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

        private string FormatPlayTime()
        {
            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            return $"{hours}h {minutes:D2}m";
        }

        private void UpdateTimerDisplay()
        {
            string formatted = FormatPlayTime();
            lblTimer.Text = formatted;
            string stateLabel = isRunning ? "Ativo" : "Parado";
            trayIcon.Text = $"Badger ({stateLabel} - {formatted})";
        }

        private void ActivityTimer_Tick(object? sender, EventArgs e)
        {
            if (!isRunning) return;

            totalSeconds++;
            UpdateTimerDisplay();

            if (totalSeconds % 30 == 0)
            {
                SavePlayTime();
            }
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Deseja zerar o contador de tempo de reprodução?",
                "Resetar Contador",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                totalSeconds = 0;
                SavePlayTime();
                UpdateTimerDisplay();
            }
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            SaveSettings();
            SavePlayTime();
        }

        private void ShowForm()
        {
            allowShowDisplay = true;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (HideToTrayOnClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else if (!e.Cancel)
            {
                SaveSettings();
                SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
                StopActivity();
                trayIcon.Visible = false;
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
                    if (File.Exists(workerExe))
                    {
                        File.SetAttributes(workerExe, FileAttributes.Normal);
                    }

                    File.Copy(currentExe, workerExe, true);
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
                activityTimer.Start();
                lblStatusValue.Text = "Reproduzindo";
                lblStatusValue.ForeColor = Color.Green;
                pnlIndicator.IndicatorColor = Color.Green;
                btnToggle.Text = "⏸ Parar";
                UpdateTimerDisplay();
                if (trayToggleMenuItem != null) trayToggleMenuItem.Text = "Parar";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar atividade: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatusValue.Text = "Parado";
                lblStatusValue.ForeColor = Color.Red;
                pnlIndicator.IndicatorColor = Color.Red;
            }
        }

        private void StopActivity()
        {
            activityTimer.Stop();
            SavePlayTime();

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
            pnlIndicator.IndicatorColor = Color.Red;
            btnToggle.Text = "▶ Iniciar";
            UpdateTimerDisplay();
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

        public void SetStartInTray(bool value)
        {
            StartInTray = value;
            SaveSettings();
        }

        private void LoadSettings()
        {
            // 1. Lê primeiro do arquivo local em AppData (fallback seguro)
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    foreach (string line in File.ReadAllLines(SettingsFilePath))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;

                        if (parts[0] == "HideToTrayOnClose" && bool.TryParse(parts[1], out bool hideVal))
                            HideToTrayOnClose = hideVal;
                        else if (parts[0] == "StartInTray" && bool.TryParse(parts[1], out bool trayVal))
                            StartInTray = trayVal;
                        else if (parts[0] == "TotalSeconds" && long.TryParse(parts[1], out long secVal))
                            totalSeconds = secVal;
                    }
                }
            }
            catch
            {
            }

            // 2. Lê e sincroniza com o Registro do Windows (garantindo --autostart atualizado)
            try
            {
                using (var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
                {
                    if (runKey != null)
                    {
                        object? currentVal = runKey.GetValue(AppRegistryName);
                        StartWithWindows = currentVal != null;
                        if (StartWithWindows)
                        {
                            string expectedVal = $"\"{Application.ExecutablePath}\" --autostart";
                            if (!string.Equals(currentVal?.ToString(), expectedVal, StringComparison.OrdinalIgnoreCase))
                            {
                                runKey.SetValue(AppRegistryName, expectedVal);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                using (var appKey = Registry.CurrentUser.OpenSubKey(RegistryAppKey, false))
                {
                    if (appKey != null)
                    {
                        if (appKey.GetValue("HideToTrayOnClose") is int hideVal)
                        {
                            HideToTrayOnClose = hideVal == 1;
                        }

                        if (appKey.GetValue("StartInTray") is int trayVal)
                        {
                            StartInTray = trayVal == 1;
                        }

                        object? savedSeconds = appKey.GetValue("TotalSeconds");
                        if (savedSeconds is long lVal)
                        {
                            totalSeconds = lVal;
                        }
                        else if (savedSeconds is int iVal)
                        {
                            totalSeconds = iVal;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveLocalFile()
        {
            try
            {
                string? dir = Path.GetDirectoryName(SettingsFilePath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllLines(SettingsFilePath, new[]
                {
                    $"HideToTrayOnClose={HideToTrayOnClose}",
                    $"StartInTray={StartInTray}",
                    $"TotalSeconds={totalSeconds}"
                });
            }
            catch
            {
            }
        }

        private void SavePlayTime()
        {
            SaveLocalFile();

            try
            {
                using (var appKey = Registry.CurrentUser.CreateSubKey(RegistryAppKey, true))
                {
                    if (appKey != null)
                    {
                        appKey.SetValue("TotalSeconds", totalSeconds, RegistryValueKind.QWord);
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            SaveLocalFile();

            try
            {
                using (var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
                {
                    if (runKey != null)
                    {
                        if (StartWithWindows)
                        {
                            string? exePath = Application.ExecutablePath;
                            if (!string.IsNullOrEmpty(exePath))
                            {
                                runKey.SetValue(AppRegistryName, $"\"{exePath}\" --autostart");
                            }
                        }
                        else
                        {
                            if (runKey.GetValue(AppRegistryName) != null)
                            {
                                runKey.DeleteValue(AppRegistryName);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                using (var appKey = Registry.CurrentUser.CreateSubKey(RegistryAppKey, true))
                {
                    if (appKey != null)
                    {
                        appKey.SetValue("HideToTrayOnClose", HideToTrayOnClose ? 1 : 0, RegistryValueKind.DWord);
                        appKey.SetValue("StartInTray", StartInTray ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch
            {
            }
        }

        private void ExitApp()
        {
            SaveSettings();
            SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
            StopActivity();
            trayIcon.Visible = false;
            showWindowEvent?.Dispose();
            showWindowEvent = null;
            Application.Exit();
        }
    }

    internal sealed class StatusDot : Control
    {
        private Color indicatorColor = Color.Red;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color IndicatorColor
        {
            get => indicatorColor;
            set
            {
                if (indicatorColor != value)
                {
                    indicatorColor = value;
                    this.Invalidate();
                }
            }
        }

        public StatusDot()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            this.BackColor = Color.Transparent;
            this.TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float size = Math.Min(this.Width, this.Height) - 1.5f;
            using (var brush = new SolidBrush(indicatorColor))
            {
                e.Graphics.FillEllipse(brush, 0.5f, 0.5f, size, size);
            }
        }
    }

    internal sealed class CircularResetButton : Control
    {
        private bool isHovered = false;
        private bool isPressed = false;

        public CircularResetButton()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPressed = false;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color circleColor = isPressed
                ? Color.FromArgb(28, 25, 95)
                : isHovered
                    ? Color.FromArgb(55, 50, 165)
                    : Color.FromArgb(38, 34, 125);

            float size = Math.Min(this.Width, this.Height) - 1f;
            RectangleF circleRect = new RectangleF(0.5f, 0.5f, size, size);

            using (var circleBrush = new SolidBrush(circleColor))
            {
                g.FillEllipse(circleBrush, circleRect);
            }

            float inset = size * 0.25f;
            RectangleF arcRect = new RectangleF(
                circleRect.X + inset,
                circleRect.Y + inset,
                size - inset * 2f,
                size - inset * 2f);

            float strokeWidth = Math.Max(1.8f, size * 0.115f);
            using (var arcPen = new Pen(Color.White, strokeWidth))
            {
                arcPen.StartCap = LineCap.Flat;
                arcPen.EndCap = LineCap.Flat;
                g.DrawArc(arcPen, arcRect, 30f, 285f);
            }

            float cx = circleRect.X + size / 2f;
            float cy = circleRect.Y + size / 2f;
            float r = arcRect.Width / 2f;
            double endRad = 315.0 * Math.PI / 180.0;
            float ex = cx + r * (float)Math.Cos(endRad);
            float ey = cy + r * (float)Math.Sin(endRad);

            float arrowSize = size * 0.19f;
            PointF[] arrowHead = new PointF[]
            {
                new PointF(ex + arrowSize * 0.45f, ey - arrowSize * 0.45f),
                new PointF(ex + arrowSize * 0.45f, ey + arrowSize * 0.55f),
                new PointF(ex - arrowSize * 0.55f, ey + arrowSize * 0.55f)
            };

            using (var arrowBrush = new SolidBrush(Color.White))
            {
                g.FillPolygon(arrowBrush, arrowHead);
            }
        }
    }
}