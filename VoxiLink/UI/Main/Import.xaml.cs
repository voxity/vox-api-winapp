using CsvHelper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using VoxiLink.Services;
using VoxiLink.UI;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour Import.xaml
    /// </summary>

    public partial class Import : UserControl, ICommand, INotifyPropertyChanged
    {
        private int _contact_csv;
        private int _contact_queue;
        private int _contact_success;
        private int _contact_fail;

        //private string csv_file;
        CsvReader csv;
        DataTable dt;

        private BackgroundWorker bw_import = new BackgroundWorker();

        public Import()
        {
            InitializeComponent();
            PanelVisibilityManager.register_panel(this);

            sp_contactError.DataContext = this;

            bw_import.WorkerReportsProgress = true;
            bw_import.ProgressChanged += ProgressChanged;
            bw_import.DoWork += DoWork;
            bw_import.RunWorkerCompleted += bw_import_RunWorkerCompleted;

        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // This is called on the UI thread when ReportProgress method is called
            pb_import.Value = e.ProgressPercentage;
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            int all_Contact = ContactCsv;
            int i_nom = -1;
            int i_prenom = -1;
            int i_tel = -1;
            int i_mob = -1;
            int i_mail = -1;

            string cn = null;
            string tel = null;
            string mobile = null;
            string mail = null;

            for (int i = 0; i < all_Contact; i++)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    i_nom = cb_nom.SelectedIndex;
                    i_prenom = cb_prenom.SelectedIndex;
                    i_tel = cb_tel.SelectedIndex;
                    i_mob = cb_mob.SelectedIndex;
                    i_mail = cb_mail.SelectedIndex;
                }));
                if (i_nom != -1)
                {
                    if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_nom].ToString()))
                    {
                        cn = dt.Rows[i][i_nom].ToString();
                        if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_prenom].ToString()))
                        {
                            cn += " " + dt.Rows[i][i_prenom].ToString();
                        }
                    }
                    else
                    {
                        if (i_prenom != -1)
                        {
                            if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_prenom].ToString()))
                            {
                                cn = dt.Rows[i][i_prenom].ToString();
                            }
                        }
                    }
                }
                else
                {
                    if (i_prenom != -1)
                    {
                        if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_prenom].ToString()))
                        {
                            cn = dt.Rows[i][i_prenom].ToString();
                        }
                    }
                }

                if (i_tel != -1)
                {
                    if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_tel].ToString()))
                    {
                        if (Voxity.API.Utils.Filter.ValidPhone(dt.Rows[i][i_tel].ToString()))
                        {
                            tel = dt.Rows[i][i_tel].ToString();
                        }
                    }
                }

                if (i_mob != -1)
                {
                    if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_mob].ToString()))
                    {
                        if (Voxity.API.Utils.Filter.ValidPhone(dt.Rows[i][i_mob].ToString()))
                        {
                            mobile = dt.Rows[i][i_mob].ToString();
                        }
                    }
                }

                if (i_mail != -1)
                {
                    if (!string.IsNullOrWhiteSpace(dt.Rows[i][i_mail].ToString()))
                    {
                        if (Voxity.API.Utils.Filter.ValidMail(dt.Rows[i][i_mail].ToString()))
                        {
                            mail = dt.Rows[i][i_mail].ToString();
                        }
                    }
                }

                try 
                {
                    Api.Session.Contacts.CreateContact(cn: cn, telNum: tel, mobile: mobile, mail: mail);
                    ContactSuccess += 1;
                    ContactCsv -= 1;
                    bw_import.ReportProgress(i);
                }
                catch
                {
                    ContactFail += 1;
                    ContactCsv -= 1;
                }                
            }
        }

        private void bw_import_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PanelVisibilityManager.show_panel(this);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(String property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        public int ContactCsv
        {
            get
            {
                return _contact_csv;
            }
            set
            {
                _contact_csv = value;
                OnPropertyChanged("ContactCsv");
            }
        }

        public int ContactQueue
        {
            get
            {
                return _contact_queue;
            }
            set
            {
                _contact_queue = value;
                OnPropertyChanged("ContactQueue");
            }
        }

        public int ContactSuccess
        {
            get
            {
                return _contact_success;
            }
            set
            {
                _contact_success = value;
                OnPropertyChanged("ContactSuccess");
            }
        }

        public int ContactFail
        {
            get
            {
                return _contact_fail;
            }
            set
            {
                _contact_fail = value;
                OnPropertyChanged("ContactFail");
            }
        }

        private void load_csv()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichiers CSV (*.csv)|*.csv";

            if (openFileDialog.ShowDialog() == true)
            {
                List<string> header = new List<string>();
                int i;
                bool max_field = false;

                //csv_file = openFileDialog.FileName;
                tb_fileName.Text = openFileDialog.SafeFileName;

                csv = new CsvReader(new StreamReader(@openFileDialog.FileName, Encoding.GetEncoding("ISO-8859-1")));

                csv.Configuration.HasHeaderRecord = false;

                dt = new DataTable();


                if (csv.Read())
                {
                    i = 0;

                    // fix me
                    while (!max_field)
                    {
                        try
                        {
                            header.Add(csv.GetField<string>(i));
                            dt.Columns.Add(i.ToString());
                            i++;
                        }
                        catch (CsvMissingFieldException)
                        {
                            max_field = true;
                        }
                    }

                    while (csv.Read())
                    {
                        var row = dt.NewRow();
                        foreach (DataColumn column in dt.Columns)
                        {
                            row[column.ColumnName] = csv.GetField<string>(int.Parse(column.ColumnName));
                        }
                        dt.Rows.Add(row);
                    }
                }

                cb_nom.ItemsSource = header;
                cb_prenom.ItemsSource = header;
                cb_tel.ItemsSource = header;
                cb_mob.ItemsSource = header;
                cb_mail.ItemsSource = header;

                cb_nom.SelectionChanged += Cb_nom_SelectionChanged;
                cb_prenom.SelectionChanged += Cb_prenom_SelectionChanged;
                cb_tel.SelectionChanged += Cb_tel_SelectionChanged;
                cb_mob.SelectionChanged += Cb_mob_SelectionChanged;
                cb_mail.SelectionChanged += Cb_mail_SelectionChanged;
            }
        }

        private void Cb_nom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex != -1)
            {
                lbl_name.Content = dt.Rows[1][cb_nom.SelectedIndex].ToString();
            }
        }

        private void Cb_prenom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex != -1)
            {
                lbl_surname.Content = dt.Rows[1][cb_prenom.SelectedIndex].ToString();
            }
        }

        private void Cb_tel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex != -1)
            {
                lbl_tel.Content = dt.Rows[1][cb_tel.SelectedIndex].ToString();
            }
        }

        private void Cb_mob_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex != -1)
            {
                lbl_mob.Content = dt.Rows[1][cb_mob.SelectedIndex].ToString();
            }
        }

        private void Cb_mail_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex != -1)
            {
                lbl_mail.Content = dt.Rows[1][cb_mail.SelectedIndex].ToString();
            }
        }


        private void btn_other_Click(object sender, RoutedEventArgs e)
        {
            load_csv();

            grid_file.Visibility = Visibility.Visible;
        }

        private void btn_outlook_Click(object sender, RoutedEventArgs e)
        {
            load_csv();

            cb_nom.SelectedIndex = 3;
            cb_prenom.SelectedIndex = 1;
            cb_tel.SelectedIndex = 37;
            cb_mob.SelectedIndex = 40;
            cb_mail.SelectedIndex = 48;


            grid_file.Visibility = Visibility.Visible;
        }

        private void btn_google_Click(object sender, RoutedEventArgs e)
        {
            load_csv();

            cb_nom.SelectedIndex = 2;
            cb_prenom.SelectedIndex = 0;
            cb_tel.SelectedIndex = 17;
            cb_mob.SelectedIndex = 20;
            cb_mail.SelectedIndex = 14;


            grid_file.Visibility = Visibility.Visible;
        }

        private void btn_sync_Click(object sender, RoutedEventArgs e)
        {
            if (csv == null)
                MessageBox.Show("Aucun fichier n'a été selectionné. Veuillez sélectionner un fichier CSV afin d'importer vos contacts.", "Voxity Client");
            else if (cb_nom.SelectedIndex == -1)
            {
                MessageBox.Show("Vous devez selectionner un champ pour le nom.", "Voxity Client");
            }

            else
            {
                grid_selectImport.Visibility = Visibility.Collapsed;
                grid_import.Visibility = Visibility.Visible;

                ContactCsv = dt.Rows.Count;
                pb_import.Maximum = ContactCsv - 1;

                bw_import.RunWorkerAsync();
            }
        }

    }

    public sealed class HideOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value < 1 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
