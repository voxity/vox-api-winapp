using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VoxiLink.UI
{
    class PanelVisibilityManager
    {
        protected static List<UserControl> main_panels = new List<UserControl>();

        public static void register_panel(UserControl uc)
        {
            main_panels.Add(uc);
        }

        public static void unregister_panel(UserControl uc)
        {
            main_panels.Remove(uc);
        }

        public static void show_panel(UserControl uc)
        {
            foreach(UserControl panel in main_panels)
            {
                if (panel == uc)
                {
                    panel.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    panel.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }
}
