using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Badger
{
    public class GameWorkerForm : Form
    {
        public GameWorkerForm()
        {
            this.Text = "Stardew Valley";
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Win32Native.SetWindowText(this.Handle, "Stardew Valley");
        }
    }

    internal static class Win32Native
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);
    }
}