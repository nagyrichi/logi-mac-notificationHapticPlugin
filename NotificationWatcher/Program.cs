using System;
using System.Threading;
using System.Windows.Forms;

namespace NotificationWatcher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Ensure single instance
            bool createdNew;
            using var mutex = new Mutex(true, "NotificationWatcherMutex", out createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show("Notification Watcher is already running!", "Info", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new NotificationWatcherContext());
        }
    }
}
