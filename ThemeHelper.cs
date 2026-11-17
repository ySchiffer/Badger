using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Badger
{
    public static class ThemeHelper
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

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
            Color bgColor = isDark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
            Color fgColor = isDark ? Color.White : SystemColors.ControlText;

            form.BackColor = bgColor;
            form.ForeColor = fgColor;

            if (form.IsHandleCreated)
            {
                ApplyTitleBarTheme(form.Handle, isDark, bgColor, fgColor);
            }
            else
            {
                form.HandleCreated += (s, e) => ApplyTitleBarTheme(form.Handle, isDark, bgColor, fgColor);
            }

            foreach (Control ctrl in form.Controls)
            {
                ApplyThemeRecursive(ctrl, isDark);
            }
        }

        public static void ApplyContextMenuTheme(ContextMenuStrip menu)
        {
            if (!IsDarkTheme()) return;

            Color menuBg = Color.FromArgb(40, 40, 40);
            menu.ShowImageMargin = false;
            menu.BackColor = menuBg;
            menu.ForeColor = Color.White;
            menu.Renderer = new DarkMenuRenderer();

            foreach (ToolStripItem item in menu.Items)
            {
                item.BackColor = menuBg;
                item.ForeColor = Color.White;
            }
        }

        private static void ApplyTitleBarTheme(IntPtr handle, bool isDark, Color bgColor, Color fgColor)
        {
            try
            {
                int useDarkMode = isDark ? 1 : 0;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));

                int captionColor = isDark ? ToColorRef(bgColor) : DWMWA_COLOR_DEFAULT;
                int textColor = isDark ? ToColorRef(fgColor) : DWMWA_COLOR_DEFAULT;

                DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
                DwmSetWindowAttribute(handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
            }
            catch
            {
            }
        }

        private static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
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

        private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkColorTable())
            {
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Color.White;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                using (var pen = new Pen(Color.FromArgb(70, 70, 70)))
                {
                    e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (var pen = new Pen(Color.FromArgb(65, 65, 65)))
                {
                    Rectangle rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            private static readonly Color BgColor = Color.FromArgb(40, 40, 40);
            private static readonly Color HoverColor = Color.FromArgb(62, 62, 62);
            private static readonly Color BorderColor = Color.FromArgb(65, 65, 65);

            public override Color ToolStripDropDownBackground => BgColor;
            public override Color MenuBorder => BorderColor;
            public override Color MenuItemBorder => HoverColor;
            public override Color MenuItemSelected => HoverColor;
            public override Color MenuItemSelectedGradientBegin => HoverColor;
            public override Color MenuItemSelectedGradientEnd => HoverColor;
            public override Color MenuItemPressedGradientBegin => HoverColor;
            public override Color MenuItemPressedGradientEnd => HoverColor;
            public override Color ImageMarginGradientBegin => BgColor;
            public override Color ImageMarginGradientMiddle => BgColor;
            public override Color ImageMarginGradientEnd => BgColor;
            public override Color SeparatorDark => Color.FromArgb(70, 70, 70);
            public override Color SeparatorLight => Color.FromArgb(70, 70, 70);
        }
    }
}