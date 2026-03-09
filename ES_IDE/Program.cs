using EventScriptIDE.Dialogs;
using System;
using System.Threading;
using System.Windows.Forms;

namespace EventScriptIDE
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            //Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.NoneEnabled;
            Application.SetCompatibleTextRenderingDefault(true);
            Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //Application.Run(new IDE(args));
            using (SplashForm splash = new SplashForm())
            {
				if (args.Length > 0)
				{
					splash.label1.Text = "Loading Project";
				}


				splash.Show();
                Application.DoEvents();
                Thread.Sleep(1000);
            }

            Application.Run(new IDE(args));
        }

        static void UIThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                "RunTime Error :\n" + e.Exception.ToString(),
                "ES IDE CRASH",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            MessageBox.Show(
                "Fatal Error:\n" + ex.ToString(),
                "ES IDE CRASH",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
