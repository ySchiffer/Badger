using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Badger
{
    public static class ThemeHelper
    {
        public static bool IsDarkTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value is int val)
                        {
                            return val == 0;
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public static void ApplyTheme(Form form)
        {
            bool isDark = IsDarkTheme();
            form.BackColor = isDark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
            form.ForeColor = isDark ? Color.White : SystemColors.ControlText;

            foreach (Control ctrl in form.Controls)
            {
                ApplyThemeRecursive(ctrl, isDark);
            }
        }

        private static void ApplyThemeRecursive(Control ctrl, bool isDark)
        {
            if (ctrl is Button btn)
            {
                btn.BackColor = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Control;
                btn.ForeColor = isDark ? Color.White : SystemColors.ControlText;
                btn.FlatStyle = FlatStyle.Standard;
            }
            else if (ctrl is CheckBox chk)
            {
                chk.ForeColor = isDark ? Color.White : SystemColors.ControlText;
            }
            else if (ctrl is Label lbl)
            {
                if (lbl.ForeColor != Color.Gray && lbl.ForeColor != Color.Green && lbl.ForeColor != Color.DarkRed && lbl.ForeColor != Color.Red)
                {
                    lbl.ForeColor = isDark ? Color.White : SystemColors.ControlText;
                }
            }

            foreach (Control child in ctrl.Controls)
            {
                ApplyThemeRecursive(child, isDark);
            }
        }
    }
}