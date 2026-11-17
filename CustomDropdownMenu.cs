using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Badger
{
    public class CustomDropdownMenu : Form
    {
        private MainForm mainForm;
        private CheckBox chkHide;
        private CheckBox chkStartup;
        private CheckBox chkStartInTray;
        private bool isUpdatingStates = false;

        public CustomDropdownMenu(MainForm owner)
        {
            mainForm = owner;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Width = 310;
            this.Height = 170;

            bool isDark = ThemeHelper.IsDarkTheme();
            this.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.White;

            chkHide = new CheckBox()
            {
                Text = "O botão de fechar deve minimizar pra bandeja",
                Left = 15,
                Top = 15,
                Width = 280,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = isDark ? Color.White : Color.Black,
                Checked = mainForm.HideToTrayOnClose
            };
            chkHide.CheckedChanged += (s, e) => {
                if (!isUpdatingStates) mainForm.SetHideToTray(chkHide.Checked);
            };
            this.Controls.Add(chkHide);

            chkStartup = new CheckBox()
            {
                Text = "Iniciar junto com o Windows",
                Left = 15,
                Top = 50,
                Width = 280,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = isDark ? Color.White : Color.Black,
                Checked = mainForm.StartWithWindows
            };
            chkStartup.CheckedChanged += (s, e) => {
                if (!isUpdatingStates) mainForm.SetStartup(chkStartup.Checked);
            };
            this.Controls.Add(chkStartup);

            chkStartInTray = new CheckBox()
            {
                Text = "Inicializar na bandeja",
                Left = 15,
                Top = 85,
                Width = 280,
                Height = 24,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = isDark ? Color.White : Color.Black,
                Checked = mainForm.StartInTray
            };
            chkStartInTray.CheckedChanged += (s, e) => {
                if (!isUpdatingStates) mainForm.SetStartInTray(chkStartInTray.Checked);
            };
            this.Controls.Add(chkStartInTray);

            Panel separator = new Panel()
            {
                Left = 15,
                Top = 120,
                Width = 280,
                Height = 1,
                BackColor = isDark ? Color.FromArgb(70, 70, 70) : Color.Silver
            };
            this.Controls.Add(separator);

            LinkLabel lnkDiscord = new LinkLabel()
            {
                Text = "Suporte: Discord",
                Left = 15,
                Top = 133,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                LinkColor = Color.FromArgb(0, 120, 215)
            };
            lnkDiscord.LinkClicked += (s, e) => {
                try { Process.Start(new ProcessStartInfo("https://discord.gg/KQaEchPhcG") { UseShellExecute = true }); } catch { }
                this.Hide();
            };
            this.Controls.Add(lnkDiscord);

            Label lblSaved = new Label()
            {
                Text = "Salvo automaticamente",
                Left = 170,
                Top = 133,
                Width = 125,
                Height = 20,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular)
            };
            this.Controls.Add(lblSaved);
        }

        public void UpdateStates()
        {
            isUpdatingStates = true;
            try
            {
                if (chkHide != null) chkHide.Checked = mainForm.HideToTrayOnClose;
                if (chkStartup != null) chkStartup.Checked = mainForm.StartWithWindows;
                if (chkStartInTray != null) chkStartInTray.Checked = mainForm.StartInTray;
            }
            finally
            {
                isUpdatingStates = false;
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (this.Visible)
            {
                this.Hide();
            }
            mainForm.NotifyMenuClosed();
        }
    }
}