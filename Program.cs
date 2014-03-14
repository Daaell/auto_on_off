using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using libLightpack;

namespace Lightpack_auto_on_off
{
    static class Program
    {
        private static NotifyIcon ni = new NotifyIcon();
        private static string appGUID = "8b13263b-891c-45d1-b414-da557c090ff4";
        private static Mutex app_instance = new Mutex(true, appGUID);
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!app_instance.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Lightpack Auto ON/OF is already running!");
                return;
            }

            if (getStatus())
            {
                ni.ContextMenuStrip = GetContext();
                ni.Icon = Properties.Resources.output;
                ni.Visible = true;
                ni.Text = "Turns Lightpack ON/OFF if you Resume/Suspend your machine.";
                SystemEvents.PowerModeChanged += PMC;
                SystemEvents.SessionEnding += SE;
                Application.Run();
            }
        }

        private static bool getStatus()
        {
            try
            {
                var api = new ApiLightpack("127.0.0.1", 3636);
                api.Connect();
                api.Lock();
                Debug.WriteLine("LP Auto ON/OFF: " + api.GetStatus().ToString());
                api.UnLock();
                api.Disconnect();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }



        private static void ChangeState(Status input)
        {
            var api = new ApiLightpack("127.0.0.1", 3636);
            api.Connect();
            api.Lock();
            api.SetStatus(input);
            api.UnLock();
            api.Disconnect();
        }

        private static ContextMenuStrip GetContext()
        {
            ContextMenuStrip CMS = new ContextMenuStrip();
            CMS.Items.Add("Exit", null, Exit_Click);
            return CMS;
        }

        private static void PMC(object sender, PowerModeChangedEventArgs e)
        {
            Debug.WriteLine("LP Auto ON/OFF: " + e.Mode.ToString());
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    ChangeState(Status.Off);
                    break;
                case PowerModes.Resume:
                    ChangeState(Status.On);
                    break;
            }
        }

        private static void SE(object sender, SessionEndingEventArgs e)
        {
            ChangeState(Status.Off);
        }


        private static void Exit_Click(object sender, EventArgs e)
        {
            SystemEvents.PowerModeChanged -= PMC;
            SystemEvents.SessionEnding -= SE;
            Application.Exit();
        }
    }
}
