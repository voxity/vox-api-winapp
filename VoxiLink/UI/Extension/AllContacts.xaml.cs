using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using Voxity.API;
using VoxiLink.Services;

namespace VoxiLink
{
    public class ContactModel
    {
        public Voxity.API.Models.Contact Contact { get; set; }
        public bool IsFav { get; set; }
    }

    /// <summary>
    /// Logique d'interaction pour AllContact.xaml
    /// </summary>
    public partial class AllContacts : UserControl
    {
        public List<ContactModel> lac = new List<ContactModel>();

        List<Voxity.API.Models.Contact> voxContactList = new List<Voxity.API.Models.Contact>();

        private BackgroundWorker bw_loadContacts = new BackgroundWorker();

        public event EventHandler ContactUpdated;

        public AllContacts()
        {
            InitializeComponent();

            Voxity.API.Models.Contact ctx_addContact = new Voxity.API.Models.Contact();
            grid_formAddUser.DataContext = ctx_addContact;

            refresh_contactList();
        }

        private bool ContactFilter(object item)
        {
            if (String.IsNullOrEmpty(tb_searchContact.Text))
                return true;
            else
                return ((item as ContactModel).Contact.cn.IndexOf(tb_searchContact.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private List<ContactModel> sort_contact(List<ContactModel> lac, bool sort_isAscendant = true)
        {
            if (sort_isAscendant == true)
            {
                lac.Sort((x, y) => string.Compare(x.Contact.cn, y.Contact.cn));
            }
            else
            {
                lac.Sort((y, x) => string.Compare(x.Contact.cn, y.Contact.cn));
            }

            return lac;
        }

        public void refresh_contactList()
        {
            bw_loadContacts.DoWork += (sender, e) =>
            {
                ContactModel localContact;

                lac = new List<ContactModel>();

                List<Voxity.API.Models.Contact> voxContactList = Api.Session.Contacts.ContactList();
                
                foreach (Voxity.API.Models.Contact voxContact in voxContactList)
                {
                    localContact = new ContactModel();

                    localContact.Contact = voxContact;
                    localContact.IsFav = false;

                    if (Properties.Settings.Default.Fav != null) {
                        foreach (Voxity.API.Models.Contact c in Properties.Settings.Default.Fav)
                        {
                            if (c.uid == voxContact.uid)
                                localContact.IsFav = true;
                        }
                    }

                    lac.Add(localContact);
                }
            };

            bw_loadContacts.RunWorkerCompleted += (sender, eventArgs) =>
            {
                lb_allContact.ItemsSource = sort_contact(lac, true);
                CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource).Refresh();

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource);
                view.Filter = ContactFilter;

                lb_allContact.Visibility = Visibility.Visible;

                if (this.ContactUpdated != null)
                    ContactUpdated(lac, new EventArgs());

                if (lac.Count == 0)
                    InitCtcTb.Visibility = Visibility.Visible;
                else
                    InitCtcTb.Visibility = Visibility.Collapsed;
            };

            bw_loadContacts.RunWorkerAsync();
        }

        private void btn_miniPhone_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ContactModel ci = (ContactModel)button.DataContext;
            Api.Session.Calls.CreateChannel(ci.Contact.telephoneNumber.ToString());
        }

        private void btn_miniMobile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ContactModel ci = (ContactModel)button.DataContext;
            Api.Session.Calls.CreateChannel(ci.Contact.mobile.ToString());
        }

        private void btn_formAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (grid_formAddUser.Visibility == Visibility.Visible)
            {
                grid_formAddUser.Visibility = Visibility.Collapsed;
                tb_btnFormAddUser.Visibility = Visibility.Visible;

                refresh_contactList();
            }
            else
            {
                grid_formAddUser.Visibility = Visibility.Visible;
                tb_btnFormAddUser.Visibility = Visibility.Collapsed;
            }
        }

        private void btn_addUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbx_addContactName.Text))
            {
                tbx_addContactName.BorderBrush = System.Windows.Media.Brushes.Red;
                tbx_addContactPhone.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else
            {
                Api.Session.Contacts.CreateContact(tbx_addContactName.Text, tbx_addContactPhone.Text, tbx_addContactPhoneRac.Text, tbx_addContactMobile.Text, tbx_addContactMobileRac.Text, tbx_addContactMail.Text);

                grid_formAddUser.Visibility = Visibility.Collapsed;
                tb_btnFormAddUser.Visibility = Visibility.Visible;
                tbx_addContactMail.Text = "";
                tbx_addContactMobile.Text = "";
                tbx_addContactMobileRac.Text = "";
                tbx_addContactName.Text = "";
                tbx_addContactPhone.Text = "";
                tbx_addContactPhoneRac.Text = "";

                refresh_contactList();
            }
        }

        private void btn_sort_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btn_sort.CommandParameter == "Ascending")
            {
                btn_sort.CommandParameter = "Descending";
                lb_allContact.ItemsSource = sort_contact(lac, false);
                CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource).Refresh();

                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_allContact.ItemsSource);
                view.Filter = ContactFilter;
            }
            else
            {
                btn_sort.CommandParameter = "Ascending";
                lb_allContact.ItemsSource = sort_contact(lac, true);
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
            ContactModel ci = (ContactModel)button.DataContext;

            string sMessageBoxText = "Etes-vous sûr de vouloir retirer '" + ci.Contact.cn + "' de vos contact ?";
            string sCaption = "Voxity Client - Suppression de contact";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

            switch (rsltMessageBox)
            {
                case MessageBoxResult.Yes:
                    Api.Session.Contacts.DeleteContact(ci.Contact.uid);
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
            ContactModel ci = (ContactModel)button.DataContext;

            StackPanel sp = (StackPanel)button.Parent;

            StackPanel sp_parent = (StackPanel)sp.Parent;

            TextBox tbx_nameEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_nameEdit"));
            TextBox tbx_phoneEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_phoneEdit"));
            TextBox tbx_phoneRacEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_phoneRacEdit"));
            TextBox tbx_mobileEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_mobileEdit"));
            TextBox tbx_mobileRacEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_mobileRacEdit"));
            TextBox tbx_mailEdit = ((TextBox)((StackPanel)sp_parent.FindName("sp_contactEdit")).FindName("tbx_mailEdit"));


            Api.Session.Contacts.UpdateContact(
                ci.Contact.uid,
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

        private void btn_addFav_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ContactModel ci = (ContactModel)button.DataContext;

            Console.WriteLine(ci.Contact.telephoneNumber);

            if (ci.IsFav != true)
            {
                try
                {
                    Properties.Settings.Default.Fav.Add(ci.Contact);
                }
                catch (NullReferenceException)
                {
                    Properties.Settings.Default.Fav = new List<Voxity.API.Models.Contact>();
                    Properties.Settings.Default.Fav.Add(ci.Contact);
                }
            }
            else
            {
                try { 
                    int i = 0;
                    foreach (Voxity.API.Models.Contact c in Properties.Settings.Default.Fav)
                    {
                        if (c.uid == ci.Contact.uid)
                            Properties.Settings.Default.Fav.RemoveAt(i);
                        i++;
                    }
                }
                catch (InvalidOperationException) { }

            }

            refresh_contactList();
        }

        private void OnNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

    }

    #region Converter

    public sealed class FavIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isFav = value as bool?;
            string icon = "\uE886";

            if (isFav ?? true)
                icon = "\uE96C";

            return icon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FavColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isFav = value as bool?;
            string color = "#FF4C4C4C";

            if (isFav ?? true)
                color = "#FFE56731";

            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ValidMailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Uri uri = new Uri("mailto:" + value);
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Validation Rules
    public class OverThirteenValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value != null)
            {
                int age = 0;
                try
                {
                    age = Convert.ToInt32(value);
                }
                catch
                {
                    return new ValidationResult(false, "You must be older than 13!");
                }

                if (age > 13)
                    return ValidationResult.ValidResult;

            }
            return new ValidationResult(false, "You must be older than 13!");
        }
    }

    public class TextBoxNotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string str = value as string;
            if (str != null)
            {
                if (str.Length > 0)
                    return ValidationResult.ValidResult;
                if (str.Length == 0)
                    return new ValidationResult(false, Message);
            }
            return new ValidationResult(false, null);
        }

        public String Message { get; set; }
    }

    public class TextBoxNotEmptyPhoneValidationRule : ValidationRule
    {
        public String Message { get; set; }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            Message = null;
            string str = value as string;
            if (!string.IsNullOrEmpty(str))
            {
                if (str.Length > 0)
                {
                    if (Voxity.API.Utils.Filter.ValidPhone(str))
                        return ValidationResult.ValidResult;
                    Message = "Téléphone invalide.";
                }
                else
                {
                    Message = "Saisir un téléphone.";
                }
            }

            return new ValidationResult(false, Message);
        }
    }

    public class TextBoxPhoneValidationRule : ValidationRule
    {
        public String Message { get; set; }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            Message = null;
            string str = value as string;
            if (!string.IsNullOrEmpty(str))
            {
                if (str.Length > 0)
                {
                    if (Voxity.API.Utils.Filter.ValidPhone(str))
                        return ValidationResult.ValidResult;
                    Message = "Téléphone invalide.";
                }
            }
            else
                return ValidationResult.ValidResult;

            return new ValidationResult(false, Message);
        }
    }

    public class TextBoxRacPhoneValidationRule : ValidationRule
    {
        public String Message { get; set; }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            Message = null;
            string str = value as string;
            if (!String.IsNullOrEmpty(str))
            {
                if (str.Length > 0)
                {
                    if (Voxity.API.Utils.Filter.ValidPhone(str))
                        return ValidationResult.ValidResult;
                    Message = "Format invalide.";
                }
            }
            else
                return ValidationResult.ValidResult;

            return new ValidationResult(false, Message);
        }
    }

    public class TextBoxMailValidationRule : ValidationRule
    {
        public String Message { get; set; }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            Message = null;
            string str = value as string;
            if (!String.IsNullOrEmpty(str))
            {
                if (str.Length > 0)
                {
                    if (Voxity.API.Utils.Filter.ValidPhone(str))
                        return ValidationResult.ValidResult;
                    Message = "Mail invalide.";
                }
            }
            else
                return ValidationResult.ValidResult;

            return new ValidationResult(false, Message);
        }
    }

    #endregion
}
