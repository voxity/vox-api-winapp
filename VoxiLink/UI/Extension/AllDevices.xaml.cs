using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VoxiLink.Services;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour AllDevices.xaml
    /// </summary>
    public partial class AllDevices : UserControl
    {
        private BackgroundWorker bw_loadDevices = new BackgroundWorker();

        List<Voxity.API.Models.Device> lad = Api.Session.Devices.DeviceList();

        public AllDevices()
        {
            InitializeComponent();


            refresh_devicesList();
        }

        #region Custom methods
        private List<Voxity.API.Models.Device> sort_device(List<Voxity.API.Models.Device> lad, bool sort_isAscendant = true)
        {
            try
            {
                if (sort_isAscendant == true)
                {
                    lad.Sort((x, y) => string.Compare(x.extension, y.extension));
                }
                else
                {
                    lad.Sort((y, x) => string.Compare(x.extension, y.extension));
                }
            }
            catch (Exception e)
            {
                 Console.WriteLine("List devices :" + e);
            }

            return lad;
        }

        private void refresh_devicesList()
        {
            bw_loadDevices.DoWork += (sender, e) =>
            {
                Thread.Sleep(5000);
                lad = Api.Session.Devices.DeviceList();
            };


            bw_loadDevices.RunWorkerCompleted += (sender, eventArgs) =>
            {
                lb_allDevices.ItemsSource = sort_device(lad, true);
                try
                {
                    CollectionViewSource.GetDefaultView(lb_allDevices.ItemsSource).Refresh();
                }
                catch (NullReferenceException)
                { }

                if (lad.Count == 0)
                    InitTelTb.Visibility = Visibility.Visible;
                else
                    InitTelTb.Visibility = Visibility.Collapsed;

                bw_loadDevices.RunWorkerAsync();
            };

            bw_loadDevices.RunWorkerAsync();
        }

        private void updateTime_devicesList()
        {
            List<Voxity.API.Models.Device> lad = Api.Session.Devices.DeviceList();
            lb_allDevices.ItemsSource = sort_device(lad, true);
            CollectionViewSource.GetDefaultView(lb_allDevices.ItemsSource).Refresh();
        }
        #endregion

        private void btn_miniPhoneDevice_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
            Voxity.API.Models.Device d = (Voxity.API.Models.Device)button.DataContext;
            Api.Session.Calls.CreateChannel(d.extension.ToString());
        }
    }

    public sealed class StatusDeviceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string hexColor = "#FFFFFFFF";

            if (value != null)
            {
                switch (value.ToString())
                {
                    case null:
                        {
                            hexColor = "#FFFFFFFF";
                        }
                        break;
                    // available
                    case "0":
                        {
                            hexColor = "#FF70BB2E";
                        }
                        break;

                    case "1":
                        {
                            hexColor = "#FFEDC240";
                        }
                        break;
                    // sonne
                    case "2":
                        {
                            hexColor = "#FF4CC5CD";
                        }
                        break;

                    // In-use
                    case "3":
                        {
                            hexColor = "#FFE56731";
                        }
                        break;

                    case "4":
                        {
                            hexColor = "Green";
                        }
                        break;

                    // Unavailable
                    case "5":
                        {
                            hexColor = "#FF4C4C4C";
                        }
                        break;

                    default:
                        {
                            hexColor = "#FFFF7777";
                        }
                        break;
                }
            }
            

            return hexColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class DeviceEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool available = false;

            if (value != null)
            {
                switch (value.ToString())
                {
                    // available
                    case "0":
                        {
                            available = true;
                        }
                        break;

                    case "1":
                        {
                            available = false;
                        }
                        break;

                    case "2":
                        {
                            available = false;
                        }
                        break;

                    // In-use
                    case "3":
                        {
                            available = false;
                        }
                        break;

                    case "4":
                        {
                            available = false;
                        }
                        break;

                    // Unavailable
                    case "5":
                        {
                            available = false;
                        }
                        break;

                    default:
                        {
                            available = false;
                        }
                        break;
                }
            }

            return available == false ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class StatusDeviceTranslationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string translate = "undefined";

            if (value != null)
            {
                switch (value.ToString())
                {
                    // available
                    case "unavailable":
                        {
                            translate = "Indisponible";
                        }
                        break;

                    case "available":
                        {
                            translate = "Disponible";
                        }
                        break;

                    case "ring":
                        {
                            translate = "En attente du corespondant";
                        }
                        break;

                    // In-use
                    case "ringing":
                        {
                            translate = "Sonne";
                        }
                        break;

                    case "in-use":
                        {
                            translate = "En communication";
                        }
                        break;

                    // Unavailable
                    case "unknow":
                        {
                            translate = "Inconnu";
                        }
                        break;

                    default:
                        {
                            translate = "undefined";
                        }
                        break;
                }
            }

            return translate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
