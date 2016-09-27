using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoxiLink.UI;

namespace VoxiLink
{
    /// <summary>
    /// Logique d'interaction pour Settings.xaml
    /// </summary>
    public partial class Settings : UserControl, ICommand
    {
        public Settings()
        {
            InitializeComponent();
            PanelVisibilityManager.register_panel(this);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            PanelVisibilityManager.show_panel(this);
            ShortcutTextBox.Text = Properties.Settings.Default.ModifierKey.ToString() + " + " + Properties.Settings.Default.Key.ToString();
            hideCb.IsChecked = Properties.Settings.Default.Hide;
        }

        private void ShortcutTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // The text box grabs all input.
            e.Handled = true;

            // Fetch the actual shortcut key.
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Ignore modifier keys.
            if (key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin
                || key == Key.Back || key == Key.Delete)
            {
                return;
            }

            // Build the shortcut key name.
            StringBuilder shortcutText = new StringBuilder();
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
            {
                shortcutText.Append("Windows + ");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append("Ctrl + ");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append("Shift + ");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append("Alt + ");
            }
            shortcutText.Append(key.ToString());

            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                // Update the text box.
                ShortcutTextBox.Text = shortcutText.ToString();
                Properties.Settings.Default.ModifierKey = Keyboard.Modifiers;
                Properties.Settings.Default.Key = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(key);
            }
        }

        private void SaveParamsBtn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void ShortcutTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            Properties.Settings.Default.Save();
            MessageBox.Show("Les modifications apportée à l'application prendront effet au redémarrage de l'application.", "Modification", MessageBoxButton.OK);
        }

        private void hideCb_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Hide = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Hide = false;
        }
    }
}
