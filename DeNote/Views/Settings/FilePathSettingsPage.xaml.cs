using DeNote.ViewModels.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeNote.Views.Settings
{
    /// <summary>
    /// FilePathSettingsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FilePathSettingsPage : UserControl
    {
        public FilePathSettingsPage()
        {
            InitializeComponent();
            DataContext = new FilePathSettingsPageViewModel();
        }

    }
}
