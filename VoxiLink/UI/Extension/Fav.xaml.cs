using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using VoxiLink.Services;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour AllContact.xaml
    /// </summary>
    public partial class Fav : UserControl
    {
        public List<Voxity.API.Models.Contact> lfav = new List<Voxity.API.Models.Contact>();

        private BackgroundWorker bw_loadContacts = new BackgroundWorker();

        public event EventHandler FavUpdated;

        public Fav()
        {
            InitializeComponent();

            Voxity.API.Models.Contact ctx_addContact = new Voxity.API.Models.Contact();

            refresh_contactList();
        }

        private bool ContactFilter(object item)
        {
            if (string.IsNullOrEmpty(tb_searchContact.Text))
                return true;
            else
                return ((item as Voxity.API.Models.Contact).cn.IndexOf(tb_searchContact.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private List<Voxity.API.Models.Contact> sort_contact(List<Voxity.API.Models.Contact> lac, bool sort_isAscendant = true)
        {
            if (sort_isAscendant == true)
            {
                lac.Sort((x, y) => string.Compare(x.cn, y.cn));
            }
            else
            {
                lac.Sort((y, x) => string.Compare(x.cn, y.cn));
            }

            return lac;
        }

        public void refresh_contactList()
        {
            bw_loadContacts.DoWork += (sender, e) =>
            {
                lfav = new List<Voxity.API.Models.Contact>();

                if (Properties.Settings.Default.Fav != null)
                    lfav.AddRange(Properties.Settings.Default.Fav);
            };

            bw_loadContacts.RunWorkerCompleted += (sender, eventArgs) =>
            {
                lb_allContact.ItemsSource = sort_contact(lfav, true);
                CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource).Refresh();

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource);
                view.Filter = ContactFilter;

                lb_allContact.Visibility = Visibility.Visible;

                if (this.FavUpdated != null)
                    FavUpdated(lfav, new EventArgs());

                if (lfav.Count == 0)
                    InitFavTb.Visibility = Visibility.Visible;
                else
                    InitFavTb.Visibility = Visibility.Collapsed;
            };

            bw_loadContacts.RunWorkerAsync();
        }

        private void btn_miniPhone_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Voxity.API.Models.Contact ci = (Voxity.API.Models.Contact)button.DataContext;
            Api.Session.Calls.CreateChannel(ci.telephoneNumber.ToString());
        }

        private void btn_miniMobile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Voxity.API.Models.Contact ci = (Voxity.API.Models.Contact)button.DataContext;
            Api.Session.Calls.CreateChannel(ci.mobile.ToString());
        }

        private void btn_sort_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btn_sort.CommandParameter == "Ascending")
            {
                btn_sort.CommandParameter = "Descending";
                lb_allContact.ItemsSource = sort_contact(lfav, false);
                CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource).Refresh();

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource);
                view.Filter = ContactFilter;
            }
            else
            {
                btn_sort.CommandParameter = "Ascending";
                lb_allContact.ItemsSource = sort_contact(lfav, true);
                CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource).Refresh();

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource);
                view.Filter = ContactFilter;

            }
        }

        private void tb_searchContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource).Refresh();
        }

        private void btn_deleteContact_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Voxity.API.Models.Contact ci = (Voxity.API.Models.Contact)button.DataContext;

            string sMessageBoxText = "Etes-vous sûr de vouloir retirer '" + ci.cn + "' de vos favoris ?";
            string sCaption = "Voxity Client - Suppression de favoris";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

            switch (rsltMessageBox)
            {
                case MessageBoxResult.Yes:
                    Properties.Settings.Default.Fav.Remove(ci);
                    refresh_contactList();

                    break;

                case MessageBoxResult.No:
                    /* ... */
                    break;

                case MessageBoxResult.Cancel:
                    /* ... */
                    break;
            }
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            refresh_contactList();
        }

        private void btn_editContact_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            StackPanel sp = (StackPanel)button.Parent;

            Grid grid = (Grid)sp.Parent;

            StackPanel sp_parent = (StackPanel)grid.Parent;

            ((Grid)sp_parent.FindName("grid_contactRead")).Visibility = Visibility.Collapsed;

            ((StackPanel)sp_parent.FindName("sp_contactEdit")).Visibility = Visibility.Visible;
        }

        private void btn_validContact_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Voxity.API.Models.Contact ci = (Voxity.API.Models.Contact)button.DataContext;

            StackPanel sp = (StackPanel)button.Parent;

            StackPanel sp_parent = (StackPanel)sp.Parent;

            TextBox tbx_nameEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_nameEdit"));
            TextBox tbx_phoneEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_phoneEdit"));
            TextBox tbx_phoneRacEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_phoneRacEdit"));
            TextBox tbx_mobileEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_mobileEdit"));
            TextBox tbx_mobileRacEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_mobileRacEdit"));
            TextBox tbx_mailEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_mailEdit"));


            Api.Session.Contacts.UpdateContact(
                ci.uid,
                tbx_nameEdit.Text,
                tbx_phoneEdit.Text,
                tbx_phoneRacEdit.Text,
                tbx_mobileEdit.Text,
                tbx_mobileRacEdit.Text,
                tbx_mailEdit.Text
            );

            refresh_contactList();
        }

        private void btn_cancelContact_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            StackPanel sp = (StackPanel)button.Parent;

            StackPanel sp_parent = (StackPanel)sp.Parent;

            ((StackPanel)sp_parent.FindName("sp_contactEdit")).Visibility = Visibility.Collapsed;

            ((Grid)sp_parent.FindName("grid_contactRead")).Visibility = Visibility.Visible;

            refresh_contactList();
        }

        private void btn_importContacts_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Grid grid = (Grid)button.Parent;
            Grid grid_parent = (Grid)grid.Parent;
            UserControl usrCtrl = (UserControl)grid_parent.Parent;
            TabItem tbItm = (TabItem)usrCtrl.Parent;
            TabControl tbCtrl = (TabControl)tbItm.Parent;
            Grid grid_tb = (Grid)tbCtrl.Parent;
            Grid gridTb_parent = (Grid)grid_tb.Parent;

            Grid grid_ctMn = (Grid)gridTb_parent.FindName("grid_mainContent");
            Grid grid_ct = (Grid)grid_ctMn.FindName("grid_content");
            UserControl ctr_import = (UserControl)grid_ct.FindName("import");

            Import cur = ctr_import as Import;
            cur.Execute(null);

        }

        private void OnNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
