using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Badger
{
    public class InstallerForm : Form
    {
        private bool isUninstall;
        private Label lblTitle = null!;
        private Label lblDesc = null!;
        private Button btnAction = null!;
        private Button btnCancel = null!;

        public InstallerForm(bool uninstall)
        {
            isUninstall = uninstall;
            InitializeComponentCustom();
            ThemeHelper.ApplyTheme(this);
        }

        private void InitializeComponentCustom()
        {
            this.Text = isUninstall ? "Badger - Desinstalador" : "Instalador do Badger";
            this.Width = 380;
            this.Height = 220;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitle = new Label()
            {
                Text = isUninstall ? "Desinstalar Badger" : "Instalador do Badger",
                Left = 20,
                Top = 20,
                Width = 320,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);

            lblDesc = new Label()
            {
                Text = isUninstall 
                    ? "Deseja remover o Badger e seus atalhos do sistema?\nDesenvolvido por Schiffer." 
                    : "Este assistente irá instalar o Badger (Game Activity Spoofing).\nDesenvolvido por Schiffer.",
                Left = 20,
                Top = 60,
                Width = 320,
                Height = 40
            };
            this.Controls.Add(lblDesc);

            btnAction = new Button()
            {
                Text = isUninstall ? "Desinstalar" : "Instalar",
                Left = 170,
                Top = 125,
                Width = 90,
                Height = 30
            };
            btnAction.Click += BtnAction_Click;
            this.Controls.Add(btnAction);

            btnCancel = new Button()
            {
                Text = "Cancelar",
                Left = 270,
                Top = 125,
                Width = 80,
                Height = 30
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BtnAction_Click(object? sender, EventArgs e)
        {
            try
            {
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Badger");
                string exeDestination = Path.Combine(appDataDir, "Badger.exe");
                string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Badger.lnk");

                if (isUninstall)
                {
                    if (File.Exists(desktopPath)) File.Delete(desktopPath);
                    if (Directory.Exists(appDataDir)) Directory.Delete(appDataDir, true);

                    MessageBox.Show("Badger desinstalado com sucesso!", "Desinstalação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Directory.CreateDirectory(appDataDir);
                    string? currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(currentExe))
                    {
                        File.Copy(currentExe, exeDestination, true);
                        string? sourceDir = Path.GetDirectoryName(currentExe);
                        if (sourceDir != null)
                        {
                            string srcIcon = Path.Combine(sourceDir, "app.png");
                            if (File.Exists(srcIcon))
                            {
                                File.Copy(srcIcon, Path.Combine(appDataDir, "app.png"), true);
                            }
                        }
                    }

                    string vbsScript = Path.Combine(Path.GetTempPath(), "create_shortcut.vbs");
                    File.WriteAllText(vbsScript, 
                        $"Set wShell = CreateObject(\"WScript.Shell\")\n" +
                        $"Set shortcut = wShell.CreateShortcut(\"{desktopPath}\")\n" +
                        $"shortcut.TargetPath = \"{exeDestination}\"\n" +
                        $"shortcut.WorkingDirectory = \"{appDataDir}\"\n" +
                        $"shortcut.Save()");
                    
                    Process.Start("cscript.exe", $"//nologo \"{vbsScript}\"").WaitForExit();
                    if (File.Exists(vbsScript)) File.Delete(vbsScript);

                    MessageBox.Show("Instalação concluída com sucesso!\nO atalho foi criado na sua Área de Trabalho.", "Instalador", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.Close();
        }
    }
}