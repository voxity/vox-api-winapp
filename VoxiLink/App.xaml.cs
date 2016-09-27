using Microsoft.Shell;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using VoxiLink.Services;
using System.Collections.Generic;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : System.Windows.Application, ISingleInstanceApp
    {
        private NotifyIcon _notifyIcon;
        private bool _isExit;

        private const string Unique = "VoxiLink";

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();

                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }

        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _notifyIcon = new NotifyIcon();
            _notifyIcon.DoubleClick += (s, args) => ShowCurrentWindow();
            _notifyIcon.Icon = VoxiLink.Properties.Resources.logo_voxity_client;
            _notifyIcon.Visible = true;

            // Handle errors
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                var ModifierKey = VoxiLink.Properties.Settings.Default.ModifierKey;
            }
            catch (NullReferenceException)
            {
                VoxiLink.Properties.Settings.Default.ModifierKey = System.Windows.Input.ModifierKeys.Windows;
            }

            try
            {
                var ModifierKey = (VoxiLink.Properties.Settings.Default.Key);
            }
            catch (NullReferenceException)
            {
                VoxiLink.Properties.Settings.Default.Key = Keys.W;
            }

            if (string.IsNullOrWhiteSpace(VoxityApi.Default.REFRESH_TOKEN))
            {
                new LoginWindow().ShowDialog();
            }
            else
            {
                Api.Session.RefreshToken = VoxityApi.Default.REFRESH_TOKEN;

                Login_Success();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            VoxiLink.Properties.Settings.Default.Save();
            _notifyIcon.Visible = false;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Exception ex = e.Exception as Exception;

            switch (ex.GetType().ToString())
            {
                case "Voxity.API.RefreshTokenException":
                    _notifyIcon.ShowBalloonTip(250, "Authentification VoxiLink expirée", "Votre session semble avoir expirée, merci de vous authentifier à nouveau au près de Voxity.", ToolTipIcon.Warning);
                    Login_Fail();
                    break;

                case "VoxiLink.Services.HotKeyException":
                    HotKeyException hke = ex as HotKeyException;
                    _notifyIcon.ShowBalloonTip(250, hke.MK + " + " + hke.K + " déjà utilisé.", "Pour changer le raccourcis VoxiLink rendez-vous dans les paramètres de l'application.", ToolTipIcon.Warning);
                    break;
                    
                case "System.NullReferenceException":
                    break;

                default:
                    _notifyIcon.ShowBalloonTip(250, "Erreur VoxiLink", "Détails : " + ex.Message, ToolTipIcon.Info);
                    break;
            }
        }
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            _notifyIcon.ShowBalloonTip(250, "Erreur VoxiLink", "Erreur critique VoxiLink :" + ex.Message, ToolTipIcon.Error);

            Environment.Exit(-1);
        }


        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("VoxiLink").Click += (s, e) => ShowCurrentWindow();
            _notifyIcon.ContextMenuStrip.Items.Add("Fermer").Click += (s, e) => ExitApplication();
        }

        private void ExitApplication()
        {
            _isExit = true;
            MainWindow.Close();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        private void ShowCurrentWindow()
        {
            if (MainWindow.IsVisible)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
            else
            {
                MainWindow.Show();
            }
        }

        private void CurrentWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExit)
            {
                if (VoxiLink.Properties.Settings.Default.Hide)
                {
                    e.Cancel = true;
                    MainWindow.Hide(); // A hidden window can be shown again, a closed one not
                    if (VoxiLink.Properties.Settings.Default.HideNotif)
                    {
                        _notifyIcon.ShowBalloonTip(250, "VoxiLink", "VoxiLink vous aide toujours. Pour fermer l'application cliquez droit sur l'icône > Fermer", ToolTipIcon.Info);
                        VoxiLink.Properties.Settings.Default.HideNotif = false;
                    }
                }
                else
                {
                    this.ExitApplication();
                }
            }
        }

        public void Login_Success()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Closing += CurrentWindow_Closing;

            _notifyIcon.ShowBalloonTip(250, "Connexion en cours...", "Veuillez patienter un instant, nous préparons tout pour vous.", ToolTipIcon.None);

            CreateContextMenu();

            ShowCurrentWindow();
        }

        public void Login_Fail()
        {
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Current.MainWindow = null;

            LoginWindow loginWindow = new LoginWindow();
            
            ShowCurrentWindow();
        }
    }
}

