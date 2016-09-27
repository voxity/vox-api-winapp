using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Voxity.API;
using VoxiLink.Services;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Setter
        private string _userName;
        private HotKey _hotkey { get; set; }

        //public event EventHandler CanExecuteChanged;

        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
            }
        }

        public class ListContact
        {
            public string ContactName { get; set; }
        }
        #endregion

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

            UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Api.User.name);

            InitializeComponent();

            Loaded += (s, e) =>
            {
                _hotkey = new HotKey(Properties.Settings.Default.ModifierKey, Properties.Settings.Default.Key, this);
                _hotkey.HotKeyPressed += (k) =>
                {
                    TextSelectionReader tsr = new TextSelectionReader();
                    string phone = tsr.TryGetSelectedTextFromActiveControl();

                    Console.WriteLine("Test appel : " + phone);
                    try
                    {
                        Api.Session.Calls.CreateChannel(phone);
                    }
                    catch (ApiInternalErrorException) { }
                    
                };
            };

            // API.Calls.logs();
            DataContext = this;
        }

        #region UI actions
        private void btn_call_Click(object sender, RoutedEventArgs e)
        {
            if (tbx_phone.Text != "")
                Api.Session.Calls.CreateChannel(tbx_phone.Text);
        }

        private void btn_sms_Click(object sender, RoutedEventArgs e)
        {
            if (grid_SMS.Visibility == Visibility.Visible)
            {
                grid_SMS.Visibility = Visibility.Collapsed;
            }
            else
            {
                grid_SMS.Visibility = Visibility.Visible;
            }
        }

        private void btn_sendSms_Click(object sender, RoutedEventArgs e)
        {
            if (tbx_emitter.Text != "")
            {
                Api.Session.Sms.SendMessage(tbx_sms.Text, tbx_phone.Text, emitter: tbx_emitter.Text);
            }
            else
            {
                Api.Session.Sms.SendMessage(tbx_sms.Text, tbx_phone.Text);
            }
            grid_SMS.Visibility = Visibility.Collapsed;
        }
    }
    #endregion

    #region UI customers
    public class ItemHeader : MarkupExtension, IValueConverter
    {
        private static ItemHeader _instance;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new ItemHeader());
        }
    }

    public class ContentList : MarkupExtension, IValueConverter
    {
        private static ContentList _instance;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter)) - 60.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new ContentList());
        }
    }

    public sealed class EditAdmin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Api.User.is_admin == 1)
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ThicknessEditAdmin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Api.User.is_admin == 1)
                return new Thickness(0, 0, 0, 1);
            else
                return new Thickness(0, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToVisibilityConverterIsAdmin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Api.User.is_admin == 1)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToVisibilityConverterAdmin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Api.User.is_admin == 1)
                return value == null || (string)value == " " ? Visibility.Collapsed : Visibility.Visible;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace((string)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToHideConverterAdmin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Api.User.is_admin == 1)
                return value == null || (string)value == " " ? Visibility.Visible : Visibility.Collapsed;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToHideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace((string)value) ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToDisabled : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (string)value != " ")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class TranslationDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            culture = new CultureInfo("fr-FR");
            string date = DateTime.Parse(value.ToString(), culture).ToString();
            return date;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BooleanToCollapsedVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //reverse conversion (false=>Visible, true=>collapsed) on any given parameter
            bool input = (null == parameter) ? (bool)value : !((bool)value);
            return (input) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    #endregion
}
