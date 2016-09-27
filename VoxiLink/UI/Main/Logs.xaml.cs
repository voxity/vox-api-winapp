using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using VoxiLink.Services;
using VoxiLink.UI;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour CallLog.xaml
    /// </summary>
    public partial class Logs : UserControl, ICommand
    {
        public List<Voxity.API.Models.Log> all_logs { get; set; }
        public List<Voxity.API.Models.Log> emit_logs { get; set; }
        public List<Voxity.API.Models.Log> rcv_logs { get; set; }

        //private List<Voxity.API.Models.Log> list_userLog;

        private BackgroundWorker bw_loadLogs = new BackgroundWorker();

        public string TextBoxLogSearch
        {
            get { return tb_search_log.Text; }
            set { tb_search_log.Text = value; }
        }

        public Logs()
        {
            RefreshLogs();
        }

        public void RefreshLogs()
        {
            all_logs = new List<Voxity.API.Models.Log>();
            emit_logs = new List<Voxity.API.Models.Log>();
            rcv_logs = new List<Voxity.API.Models.Log>();

            InitializeComponent();
            PanelVisibilityManager.register_panel(this);

            DataContext = this;

            bw_loadLogs.DoWork += (sender, e) =>
            {
                StringCollection filter_src = new StringCollection();
                StringCollection filter_dest = new StringCollection();

                // Outcalling
                String[] src = new String[] { "1015" };
                filter_src.AddRange(src);

                // Incalling
                String[] dest = new String[] { "1015" };
                filter_dest.AddRange(dest);

                List<Voxity.API.Models.Log> list_userLog = Api.Session.Calls.Logs();

                // Outcalling
                all_logs = list_userLog.Where(s => s.source == Api.User.internalExtension).ToList();
                // Incalling
                all_logs.AddRange(list_userLog.Where(d => d.destination == Api.User.internalExtension && d.source != Api.User.internalExtension).ToList());

                emit_logs = list_userLog.Where(s => s.source == Api.User.internalExtension).ToList();

                rcv_logs = list_userLog.Where(s => s.destination == Api.User.internalExtension).ToList();
            };

            bw_loadLogs.RunWorkerCompleted += (sender, eventArgs) =>
            {
                all.log_list = all_logs;
                all.refresh_logsList();

                emit.log_list = emit_logs;
                emit.refresh_logsList();

                rcv.log_list = rcv_logs;
                rcv.refresh_logsList();

                // Fix me, use PropertyChangedEventArgs to refresh
                DataContext = null;
                DataContext = this;
            };

            bw_loadLogs.RunWorkerAsync();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            //this.Visibility = (this.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed);

            PanelVisibilityManager.show_panel(this);
        }

        private bool LogFilter(object item)
        {
            if (String.IsNullOrEmpty(tb_search_log.Text))
                return true;
            else
                return ((item as Voxity.API.Models.Log).source.IndexOf(tb_search_log.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void tb_search_log_TextChanged(object sender, TextChangedEventArgs e)
        {
            all.search = tb_search_log.Text;
            emit.search = tb_search_log.Text;
            rcv.search = tb_search_log.Text;
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogs();
        }
    }

    public sealed class MyBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == false ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
