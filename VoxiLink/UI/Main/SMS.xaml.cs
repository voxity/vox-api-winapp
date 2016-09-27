using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using VoxiLink.Services;
using VoxiLink.UI;

namespace VoxiLink
{
    public class Sms
    {
        public List<string> phone_number { get; set; }
        public DateTime send_date { get; set; }
        public string content { get; set; }
        public bool is_send { get; set; }
        public bool is_draft { get; set; }

        public Sms()
        {
            phone_number = new List<string>();
        }
    }

    public class Conversation
    {
        public List<Sms> conv { get; set; }

        public Conversation()
        {
            this.conv = new List<Sms>();
        }

        public static int GetHashListPhone<T>(IEnumerable<T> source)
        {
            int hash = 0;
            foreach (T element in source)
            {
                hash = unchecked(hash +
                    EqualityComparer<T>.Default.GetHashCode(element));
            }
            return hash;
        }
    }

    /// <summary>
    /// Logique d'interaction pour SMS.xaml
    /// </summary>
    public partial class SMS : UserControl, ICommand
    {
        #region Setters
        public static List<ContactModel> contact_list = new List<ContactModel>();

        public List<Voxity.API.Models.EmitSms> sms_list = new List<Voxity.API.Models.EmitSms>();
        public List<Voxity.API.Models.SmsResponses> response_list = new List<Voxity.API.Models.SmsResponses>();

        // dic for body of conversation, key is hashcode of list of dest, value is messages of the conv selected
        public Dictionary<int, Conversation> conv_dict { get; set; }

        public string resp = null;

        private int last_msgLenght = 0;
        #endregion

        #region  Events
        public event EventHandler CanExecuteChanged;
        #endregion

        // Constructor
        public SMS()
        {
            InitializeComponent();
            PanelVisibilityManager.register_panel(this);

            sms_list = Api.Session.Sms.EmitMessageList();
            response_list = Api.Session.Sms.ResponsesMessagesList();

            conv_dict = new Dictionary<int, Conversation>();

            refresh_logsList();

            lbl_titleConv.DataContext = Tokenizer.get_tokens();

            Window parentWindow = Window.GetWindow(Application.Current.MainWindow);

            AllContacts ac = new AllContacts();

            ac.ContactUpdated += new EventHandler(PopulateList_ContactUpdated);

            Tokenizer.TokenMatcher = text =>
            {
                if (text.EndsWith(";"))
                {
                    return text.Substring(0, text.Length - 1).Trim();
                }

                return null;
            };
        }

        #region Custom methods
        // Event fire when the request for the list contact of the API is send
        public void PopulateList_ContactUpdated(object sender, EventArgs e)
        {
            contact_list = sender as List<ContactModel>;

            lb_selContact.ItemsSource = contact_list;

            CollectionViewSource.GetDefaultView(lb_selContact.ItemsSource).Refresh();

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lb_selContact.ItemsSource);

            view.Filter = ContactFilter;
        }

        // Autocompletion contact filter
        private bool ContactFilter(object item)
        {
            if (String.IsNullOrEmpty(resp))
                return true;
            else
                return ((item as ContactModel).Contact.cn.IndexOf(resp, StringComparison.OrdinalIgnoreCase) >= 0);

        }

        // Generating SMS view lists
        public void refresh_logsList()
        {
            List<string> dest = new List<string>(); 

            // add sms emit to the dictionary
            foreach (Voxity.API.Models.EmitSms sms in sms_list)
            {
                dest.Add(sms.phone_number);

                try
                {
                    conv_dict[Conversation.GetHashListPhone(dest)].conv.Add(new Sms()
                    {
                        phone_number = new List<string> { sms.phone_number },
                        send_date = Convert.ToDateTime(sms.send_date),
                        content = sms.content,
                        is_send = true,
                        is_draft = false
                    });
                }
                catch (KeyNotFoundException)
                {
                    Conversation new_conv = new Conversation();
                    new_conv.conv.Add(new Sms()
                    {
                        phone_number = new List<string> { sms.phone_number },
                        send_date = Convert.ToDateTime(sms.send_date),
                        content = sms.content,
                        is_send = true,
                        is_draft = false
                    });

                    conv_dict.Add(Conversation.GetHashListPhone(dest), new_conv);
                }

                dest.Remove(sms.phone_number);
            }

            // add sms receive to the dictionary
            foreach (Voxity.API.Models.SmsResponses sms in response_list)
            {
                dest.Add(sms.phone_number);

                try
                {
                    conv_dict[Conversation.GetHashListPhone(dest)].conv.Add(new Sms()
                    {
                        phone_number = new List<string> { sms.phone_number },
                        send_date = Convert.ToDateTime(sms.send_date),
                        content = sms.content,
                        is_send = false,
                        is_draft = false
                    });
                }
                catch (KeyNotFoundException)
                {
                    Conversation new_conv = new Conversation();
                    new_conv.conv.Add(new Sms()
                    {
                        phone_number = new List<string> { sms.phone_number },
                        send_date = Convert.ToDateTime(sms.send_date),
                        content = sms.content,
                        is_send = false,
                        is_draft = false
                    });

                    conv_dict.Add(Conversation.GetHashListPhone(dest), new_conv);
                }

                dest.Remove(sms.phone_number);
            }

            // sort conv by date
            foreach (KeyValuePair<int, Conversation> conversation in conv_dict)
            {
                List<Sms> conv = conversation.Value.conv as List<Sms>;
                conv = sort_conv(conv, true);
            }

            // sort all conv by date
            IOrderedEnumerable<KeyValuePair<int, Conversation>> conv_tri = from conv in conv_dict
                                                                           orderby Convert.ToDateTime(conv.Value.conv.Last().send_date) descending
                                                                           select conv;

            conv_dict = conv_tri.ToDictionary(pair => pair.Key, pair => pair.Value);

            lb_smsList.ItemsSource = conv_dict;

            CollectionViewSource.GetDefaultView(lb_smsList.ItemsSource);

            /* 
             * Better way, use only ICollectionView, but hard to get the last message of a conv...
             * 
            ICollectionView view = CollectionViewSource.GetDefaultView(lb_smsList.ItemsSource);

            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Value.conv[0].send_date", ListSortDirection.Descending));
            }
            */

            lb_smsList.Visibility = Visibility.Visible;

            if (conv_dict.Count == 0)
                InitCtcTb.Visibility = Visibility.Visible;
            else
                InitCtcTb.Visibility = Visibility.Collapsed;
        }

        // Sort by date list of sms
        private List<Sms> sort_conv(List<Sms> list_conv, bool sort_isAscendant = true)
        {

            if (sort_isAscendant == true)
            {
                list_conv.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.send_date), Convert.ToDateTime(y.send_date)));
            }
            else
            {
                list_conv.Sort((y, x) => DateTime.Compare(Convert.ToDateTime(x.send_date), Convert.ToDateTime(y.send_date)));
            }

            return list_conv;
        }
        #endregion

        #region View methods

        #region Textbox
        private void tb_search_log_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void tbx_emit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbx_emit.Text.Length > 0)
                tb_emitInfo.Visibility = Visibility.Visible;
            else
                tb_emitInfo.Visibility = Visibility.Collapsed;
        }

        private void tbx_dest_TextChanged(object sender, TextChangedEventArgs e)
        {
            TokenizingControl tc = sender as TokenizingControl;
            string text = new TextRange(tc.Document.ContentStart, tc.Document.ContentEnd).Text;
            text = text.Trim(new Char[] { '\r', '\n', ' ' });

            if (!string.IsNullOrEmpty(text))
            {
                resp = text;
                CollectionViewSource.GetDefaultView(lb_selContact.ItemsSource).Refresh();
                lb_selContact.Visibility = Visibility.Visible;
            }
            else
            {
                if (lb_selContact != null)
                    lb_selContact.Visibility = Visibility.Collapsed;
            }
        }

        private void tbx_msg_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (lb_smsList.SelectedItem == null)
            {
                try
                {
                    if (tbx_msg.Text.Length == 1 && conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().is_draft == false)
                    {
                        conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Add(new Sms()
                        {
                            phone_number = Tokenizer.get_tokens(),
                            send_date = DateTime.Now,
                            content = tbx_msg.Text,
                            is_send = true,
                            is_draft = true
                        });
                    }
                    conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().is_draft = true;
                    conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().content = tbx_msg.Text;
                    conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().send_date = DateTime.Now;

                    lbl_titleConv.DataContext = conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.First().phone_number;
                }
                catch (KeyNotFoundException)
                {
                    Conversation new_conv = new Conversation();

                    new_conv.conv.Add(new Sms()
                    {
                        phone_number = Tokenizer.get_tokens(),
                        send_date = DateTime.Now,
                        content = tbx_msg.Text,
                        is_send = true,
                        is_draft = true
                    });

                    conv_dict.Add(Conversation.GetHashListPhone(Tokenizer.get_tokens()), new_conv);

                    conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().is_draft = true;
                    conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().content = tbx_msg.Text;
                    conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last().send_date = DateTime.Now;

                    lbl_titleConv.DataContext = conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.First().phone_number;
                }

                lb_smsConv.ItemsSource = conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv as List<Sms>;

                CollectionViewSource.GetDefaultView(lb_smsConv.ItemsSource).Refresh();

                last_msgLenght = tbx_msg.Text.Length;
            }
            else
            {
                if (!string.IsNullOrEmpty(tbx_msg.Text))
                {
                    KeyValuePair<int, Conversation> item = (KeyValuePair<int, Conversation>)lb_smsList.SelectedItem;

                    lb_smsConv.ItemsSource = item.Value.conv as List<Sms>;

                    lbl_titleConv.DataContext = item.Value.conv;

                    try
                    {
                        if (tbx_msg.Text.Length == 1 && conv_dict[item.Key].conv.Last().is_draft == false)
                        {
                            conv_dict[item.Key].conv.Add(new Sms()
                            {
                                phone_number = conv_dict[item.Key].conv.First().phone_number,
                                //send_date = DateTime.Now.AddHours(-2),
                                send_date = DateTime.Now,
                                content = tbx_msg.Text,
                                is_send = true,
                                is_draft = true
                            });
                        }
                        else
                        {
                            conv_dict[item.Key].conv.Last().is_draft = true;
                            conv_dict[item.Key].conv.Last().content = tbx_msg.Text;
                            conv_dict[item.Key].conv.Last().send_date = DateTime.Now;
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        Conversation new_conv = new Conversation();

                        new_conv.conv.Add(new Sms()
                        {
                            phone_number = conv_dict[item.Key].conv.First().phone_number,
                            send_date = DateTime.Now,
                            content = tbx_msg.Text,
                            is_send = true,
                            is_draft = true
                        });

                        conv_dict.Add(Conversation.GetHashListPhone(conv_dict[item.Key].conv.First().phone_number), new_conv);

                        conv_dict[item.Key].conv.Last().is_draft = true;
                        conv_dict[item.Key].conv.Last().content = tbx_msg.Text;
                        conv_dict[item.Key].conv.Last().send_date = DateTime.Now;

                        lbl_titleConv.DataContext = conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.First().phone_number;

                        last_msgLenght = tbx_msg.Text.Length;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {
                    var conv = conv_dict.Last(kvp => kvp.Value.conv.Last().is_draft == true);

                    if (Tokenizer.get_tokens() != null)
                        conv_dict[conv.Key].conv.Remove(conv.Value.conv.Last());
                    else
                        conv_dict.Remove(conv.Key);
                }

                CollectionViewSource.GetDefaultView(lb_smsConv.ItemsSource).Refresh();
            }

            IOrderedEnumerable<KeyValuePair<int, Conversation>> conv_tri = from conv in conv_dict
                                                                            orderby Convert.ToDateTime(conv.Value.conv.Last().send_date) descending
                                                                            select conv;

            conv_dict = conv_tri.ToDictionary(pair => pair.Key, pair => pair.Value);

            lb_smsList.ItemsSource = conv_dict;

            CollectionViewSource.GetDefaultView(lb_smsList.ItemsSource).Refresh();


            if (tbx_msg.Text.Length == 0)
            {
                grid_dest.Visibility = Visibility.Visible;
            }

        }
        #endregion

        #region Button

        private void btn_sendSmsContact_Click(object sender, RoutedEventArgs e)
        {
            Sms sms = new Sms();

            if (lb_smsList.SelectedItem == null)
            {
                sms = conv_dict[Conversation.GetHashListPhone(Tokenizer.get_tokens())].conv.Last();
            }
            else
            {

                KeyValuePair<int, Conversation> item = (KeyValuePair<int, Conversation>)lb_smsList.SelectedItem;

                lb_smsConv.ItemsSource = item.Value.conv as List<Sms>;

                sms = conv_dict[item.Key].conv.Last();
            }

            foreach (string phone in sms.phone_number)
            {
                try
                {
                    if (tbx_emit.Text.Length == 0)
                        Api.Session.Sms.SendMessage(tbx_msg.Text, phone);
                    else
                        Api.Session.Sms.SendMessage(tbx_msg.Text, phone, emitter: tbx_emit.Text);
                    conv_dict[Conversation.GetHashListPhone(sms.phone_number)].conv.Last().is_draft = false;
                    tbx_msg.TextChanged -= tbx_msg_TextChanged;
                    tbx_msg.Text = null;
                    tbx_msg.TextChanged += tbx_msg_TextChanged;
                }
                catch (Voxity.API.ApiSessionException ase)
                {
                    MessageBox.Show(ase.ToString());
                }
            }

            CollectionViewSource.GetDefaultView(lb_smsConv.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lb_smsList.ItemsSource).Refresh();
        }

        private void btn_newSms_Click(object sender, RoutedEventArgs e)
        {
            lb_smsConv.ItemsSource = null;

            lb_smsList.SelectionChanged -= lb_smsList_SelectionChanged;
            lb_smsList.SelectedItem = null;
            lb_smsList.SelectionChanged += lb_smsList_SelectionChanged;

            Tokenizer.Document.Blocks.Clear();

            tbx_msg.Text = null;

            lbl_titleConv.DataContext = Tokenizer.get_tokens();

            grid_dest.Visibility = Visibility.Visible;
        }
        #endregion

        #region ListBox
        private void lb_smsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KeyValuePair<int, Conversation> item = (KeyValuePair<int, Conversation>)lb_smsList.SelectedItem;

            lb_smsConv.ItemsSource = item.Value.conv as List<Sms>;

            lbl_titleConv.DataContext = item.Value.conv;

            if (item.Value.conv.First().phone_number == null)
            {
                grid_dest.Visibility = Visibility.Visible;
            }
            else
            {
                grid_dest.Visibility = Visibility.Collapsed;
            }

            if (item.Value.conv.Last().is_draft)
            {
                tbx_msg.TextChanged -= tbx_msg_TextChanged;
                tbx_msg.Text = item.Value.conv.Last().content;
                tbx_msg.TextChanged += tbx_msg_TextChanged;
            }
            else
            {
                tbx_msg.TextChanged -= tbx_msg_TextChanged;
                tbx_msg.Text = null;
                tbx_msg.TextChanged += tbx_msg_TextChanged;
            }

            sv_convBublle.ScrollToBottom();
        }

        private void lb_selContact_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lb_selContact.Visibility = Visibility.Collapsed;
            ContactModel contact = (ContactModel)lb_selContact.SelectedItem;

            if (contact != null)
            {
                Tokenizer.CaretPosition = Tokenizer.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                Tokenizer.CaretPosition.DeleteTextInRun(-(new TextRange(Tokenizer.Document.ContentStart, Tokenizer.Document.ContentEnd).Text.Length));
                try
                {
                    if (!string.IsNullOrWhiteSpace(contact.Contact.mobile))
                        Tokenizer.CaretPosition.InsertTextInRun(Voxity.API.Utils.Converter.ConvertPhone(contact.Contact.mobile) + ";");
                    else
                        throw new NullReferenceException();
                }
                catch (NullReferenceException)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(contact.Contact.telephoneNumber))
                            Tokenizer.CaretPosition.InsertTextInRun(Voxity.API.Utils.Converter.ConvertPhone(contact.Contact.telephoneNumber) + ";");
                        else
                            throw new NullReferenceException();
                    }
                    catch (NullReferenceException ex)
                    {
                        throw ex;
                    }
                }
            }
        }
        #endregion

        #endregion

        #region Events methods
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PanelVisibilityManager.show_panel(this);
        }
        #endregion

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            sms_list = Api.Session.Sms.EmitMessageList();
            response_list = Api.Session.Sms.ResponsesMessagesList();
            conv_dict = new Dictionary<int, Conversation>();
            refresh_logsList();
        }
    }

    #region Converter class
    public sealed class TranslationSmsDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dt = System.Convert.ToDateTime(value);

            DateTimeKind dateKind = dt.Kind;

            DateTime date = dt.ToLocalTime();

            if ((string)parameter == "long")
            {
                if (date.Date == DateTime.Now.Date)
                    return date.ToString(date.Hour.ToString("HH") + ":" + date.Minute.ToString("mm"));
                else
                    return date.ToString(date.Day.ToString("dd") + "/" + date.Month.ToString("MM") + ", " + date.Hour.ToString("HH") + ":" + date.Minute.ToString("mm"));
            }
            else
            {
                if (date.Date == DateTime.Now.Date)
                    return date.ToString(date.Hour.ToString("HH") + ":" + date.Minute.ToString("mm"));
                else
                    return date.ToString(date.Day.ToString("dd") + "/" + date.Month.ToString("MM"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class LastSmsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<Sms> conv = value as List<Sms>;
            string sms = null;

            if (conv.Count != 0)
            {
                switch (parameter.ToString())
                {
                    case "content":
                        if (conv.Last().is_draft == true)
                        {
                            sms = "[Brouillon] " + conv.Last().content;

                            if (string.IsNullOrEmpty(conv.Last().content))
                                sms = "[Nouveau message]";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(conv.Last().content))
                                sms = conv.Last().content;
                            else
                                sms = "[Nouveau message]";
                        }
                        break;
                    case "send_date":
                        TranslationSmsDateConverter smsDate = new TranslationSmsDateConverter();
                        sms = smsDate.Convert(conv.Last().send_date, targetType, null, culture).ToString();
                        break;
                    default:
                        sms = null;
                        break;
                }
            }
            

            return sms;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SearchSize : MarkupExtension, IValueConverter
    {
        private static SearchSize _instance;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var a = (System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter));
            return a;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new SearchSize());
        }
    }

    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((string)parameter == "send")
            {
                if ((bool)value == true)
                    return Visibility.Visible;
            }
            else
            {
                if ((bool)value == false)
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class TelToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ContactModel ctc = (ContactModel)parameter as ContactModel;

            string tel = null;

            if (value is List<Sms>)
            {
                List<Sms> dest = (List<Sms>)value as List<Sms>;

                if (dest.Count != 0)
                {
                    if (dest.Last().phone_number.Count == 1)
                    {
                        if (ctc == null)
                        {
                            foreach (ContactModel item in SMS.contact_list)
                            {
                                if (!string.IsNullOrEmpty(item.Contact.mobile))
                                {
                                    tel = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.mobile);

                                    if (tel.Contains(Voxity.API.Utils.Converter.ConvertPhone(dest.Last().phone_number[0])))
                                        return item.Contact.cn;
                                }

                                if (!string.IsNullOrEmpty(item.Contact.telephoneNumber))
                                {
                                    tel = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.telephoneNumber);
                                    if (tel.Contains(Voxity.API.Utils.Converter.ConvertPhone(dest.Last().phone_number[0])))
                                        return item.Contact.cn;
                                }
                            }
                        }
                        else
                        {
                            return ctc.Contact.cn;
                        }

                        return dest.Last().phone_number[0];

                    }
                    else
                    {
                        if (dest.Last().phone_number.Count != 0)
                            return "Message groupé (" + dest.Last().phone_number.Count + " destinataires)";
                        else
                            return "Nouveau message";
                    }
                }
            }
            else
            {
                if (value is string)
                {
                    string dest = (string)value as string;

                    if (!string.IsNullOrEmpty(dest))
                    {
                        if (ctc == null)
                        {
                            foreach (ContactModel item in SMS.contact_list)
                            {

                                if (!string.IsNullOrEmpty(item.Contact.mobile))
                                {
                                    tel = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.mobile);
                                    if (tel.Contains(Voxity.API.Utils.Converter.ConvertPhone(dest)))
                                        return item.Contact.cn;
                                }

                                if (!string.IsNullOrEmpty(item.Contact.telephoneNumber))
                                {
                                    tel = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.telephoneNumber);
                                    if (tel.Contains(Voxity.API.Utils.Converter.ConvertPhone(dest)))
                                        return item.Contact.cn;
                                }
                            }
                        }
                        else
                        {
                            return ctc.Contact.cn;
                        }

                        return dest;
                    }
                    else
                    {
                        return "Nouveau message";
                    }
                }
                else
                {
                    if (value is List<string>)
                    {
                        List<string> dest = (List<string>)value as List<string>;

                        if (dest.Count == 1)
                        {
                            if (ctc == null)
                            {
                                foreach (ContactModel item in SMS.contact_list)
                                {

                                    if (!string.IsNullOrEmpty(item.Contact.mobile))
                                    {
                                        tel = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.mobile);
                                        if (tel.Contains(Voxity.API.Utils.Converter.ConvertPhone(dest[0])))
                                            return item.Contact.cn;
                                    }

                                    if (!string.IsNullOrEmpty(item.Contact.telephoneNumber))
                                    {
                                        tel = Voxity.API.Utils.Converter.ConvertPhone(item.Contact.telephoneNumber);
                                        if (tel.Contains(Voxity.API.Utils.Converter.ConvertPhone(dest[0])))
                                            return item.Contact.cn;
                                    }
                                }
                            }
                            else
                            {
                                return ctc.Contact.cn;
                            }

                            return dest;
                        }
                        else
                        {
                            if (dest.Count != 0)
                                return "Message groupé (" + dest.Count + " destinataires)";
                            else
                                return "Nouveau message";
                        }
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
