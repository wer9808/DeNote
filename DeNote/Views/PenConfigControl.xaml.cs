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
using DeNote.ViewModels;

namespace DeNote.Views
{
    /// <summary>
    /// PenConfigControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PenConfigControl : UserControl
    {

        const double MaxStrokeThickness = 20;
        const double MinStrokeThickness = 0.5;

        public PenConfigControl(DrawingViewModel drawingViewModel)
        {
            InitializeComponent();
            this.DataContext = drawingViewModel;
        }
    }
}
