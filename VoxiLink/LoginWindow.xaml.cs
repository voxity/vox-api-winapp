using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Diagnostics;
using VoxiLink.Services;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly BackgroundWorker bgw_http_listen = new BackgroundWorker();
        private RedirectHttpListener rhl = new RedirectHttpListener();

        public LoginWindow()
        {
            bgw_http_listen.DoWork += worker_DoWork;
            bgw_http_listen.RunWorkerCompleted += worker_RunWorkerCompleted;
            bgw_http_listen.RunWorkerAsync();
            InitializeComponent();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            rhl.Listen(Api.Session.UriRedirect); //Listen on the Redirect Uri
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = null;
            this.Close();
            ((App)Application.Current).Login_Success();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Api.Session.AskAuthUri);
        }
    }
}
