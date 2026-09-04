using System;
using System.Windows.Forms;

namespace Badger
{
    public class GameWorkerForm : Form
    {
        public GameWorkerForm()
        {
            this.Text = "Stardew Valley";
            this.Width = 250;
            this.Height = 250;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.ShowIcon = false;

            System.Windows.Forms.Timer keepAlive = new System.Windows.Forms.Timer();
            keepAlive.Interval = 10000;
            keepAlive.Tick += (s, e) => { this.Text = "Stardew Valley"; };
            keepAlive.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            NativeMethods.SetWindowText(this.Handle, "Stardew Valley");
        }
    }
}