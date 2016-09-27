using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VoxiLink.Services;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour AllLogs.xaml
    /// </summary>
    public partial class ListLog : UserControl
    {
        public List<Voxity.API.Models.Log> log_list = new List<Voxity.API.Models.Log>();

        private string _search;
        public string search
        {
            get { return _search; }
            set
            {
                _search = value;
                CollectionViewSource.GetDefaultView(lb_allLogs.ItemsSource).Refresh();
            }
        }

        public ListLog()
        {
            InitializeComponent();

            //refresh_logsSms();
        }

        #region Custom methods
        private List<Voxity.API.Models.Log> sort_logs(List<Voxity.API.Models.Log> list_userLog, bool sort_isAscendant = true)
        {

            if (sort_isAscendant == true)
            {
                list_userLog.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.calldate), Convert.ToDateTime(y.calldate)));
            }
            else
            {
                list_userLog.Sort((y, x) => DateTime.Compare(Convert.ToDateTime(x.calldate), Convert.ToDateTime(y.calldate)));
            }

            return list_userLog;
        }

        public void refresh_logsList()
        {
            log_list = sort_logs(log_list, false);

            lb_allLogs.ItemsSource = log_list;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_allLogs.ItemsSource);

            view.Filter = LogFilter;

            lb_allLogs.Visibility = Visibility.Visible;

            if (log_list.Count == 0)
                InitCtcTb.Visibility = Visibility.Visible;
            else
                InitCtcTb.Visibility = Visibility.Collapsed;

        }


        private bool LogFilter(object item)
        {
            if (String.IsNullOrEmpty(search))
                return true;
            else
                return ((item as Voxity.API.Models.Log).source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || (item as Voxity.API.Models.Log).destination.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        #endregion

        private void btn_miniPhoneDevice_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Voxity.API.Models.Log d = (Voxity.API.Models.Log)button.DataContext;
            if (d.destination != Api.User.internalExtension)
                Api.Session.Calls.CreateChannel(d.destination.ToString());
            else
                Api.Session.Calls.CreateChannel(d.source.ToString());
        }

    }

    public sealed class SensLogConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string icon = "\uE8F5;";

            if (values[0].ToString() == Api.User.internalExtension)
                icon = "\uEA4C";

            if (values[1].ToString() == Api.User.internalExtension)
                icon = "\uE8F5";

            if (values[0].ToString() == values[1].ToString())
                icon = "\xE929";

            return icon;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class SourceDestConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string text = "Inconnu";

            if (values[0].ToString() == Api.User.internalExtension)
                text = values[1].ToString();

            if (values[1].ToString() == Api.User.internalExtension)
                text = values[0].ToString();

            TelToNameConverterLog ttn = new TelToNameConverterLog();
            text = ttn.Convert(text, targetType, null, culture).ToString();

            switch (text)
            {
                case "s":
                    text = "Appel interrompu";
                    break;
            }

            if (text.Length > 4) {
                if(text.Substring(0, 4).Equals("666*"))
                {
                    text = "Écoute : " + text.Substring(4);
                }
            }
            return text;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ColorLogConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Color hexColor = new Color() { A = 0xFF, R = 0x4C, G = 0x4C, B = 0x4C };

            if (values[0].ToString() == Api.User.internalExtension)
                hexColor = new Color() { A = 0xFF, R = 0x4C, G = 0xC5, B = 0xCD };

            if (values[1].ToString() == Api.User.internalExtension)
                hexColor = new Color() { A = 0xFF, R = 0xED, G = 0xC2, B = 0x40 };

            if (values[0].ToString() == values[1].ToString())
                hexColor = new Color() { A = 0xFF, R = 0xE5, G = 0x67, B = 0x31 };

            return new SolidColorBrush(hexColor);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class StatusLogConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string hexColor = "#FFFFFFFF";

            string parameterString = parameter as string;

            string[] parameters = parameterString.Split(new char[] { '|' });

            switch (parameters[0])
            {
                // available
                case "incoming":
                    {
                        if (parameters[1] == "answered")
                            hexColor = "#FF70BB2E";
                        //unanswered
                        else
                            hexColor = "#FFFF0000";
                    }
                    break;

                case "outgoing":
                    {
                        hexColor = "#FFFF7777";
                    }
                    break;

                case "N/A":
                    {
                        hexColor = "#FFFF2222";
                    }
                    break;
                default:
                    {
                        hexColor = "#FFFFFFFF";
                    }
                    break;
            }

            return hexColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToVisibilityNAConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (string)value == "N/A" ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class TranslationSecondConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan time = TimeSpan.FromSeconds(System.Convert.ToDouble(value));
            if(time.Hours == 0 && time.Minutes == 0)
                return time.Seconds.ToString() + " s";
            else if (time.Hours == 0 && time.Minutes != 0)
                return time.Minutes.ToString() + " min, " + time.Seconds.ToString() + " s";
            else
                return time.Hours.ToString() + "h, " + time.Minutes.ToString() + "min, " + time.Seconds.ToString() + "s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class UserDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dt = System.Convert.ToDateTime(value);

            DateTimeKind dateKind = dt.Kind;

            DateTime date = dt.ToLocalTime();

            if (date.Date == DateTime.Now.Date)
                return "Aujourd'hui à " + date.ToString(date.Hour.ToString("HH") + ":" + date.Minute.ToString("mm"));
            else
                return "Le " + date.ToString(date.Day.ToString("dd") + "/" + date.Month.ToString("MM") + " à " + date.Hour.ToString("HH") + ":" + date.Minute.ToString("mm"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class TelToNameConverterLog : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string tel = (string)value as string;
            string c = null;

            foreach (ContactModel item in SMS.contact_list)
            {
                if (!string.IsNullOrEmpty(item.Contact.mobile))
                {
                    c = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.mobile);

                    if (c.Contains(Voxity.API.Utils.Converter.ConvertPhone(tel)))
                        return item.Contact.cn;
                }

                if (!string.IsNullOrEmpty(item.Contact.telephoneNumber))
                {
                    c = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.telephoneNumber);
                    if (c.Contains(Voxity.API.Utils.Converter.ConvertPhone(tel)))
                        return item.Contact.cn;
                }
            }

            return tel;
        }
                
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
