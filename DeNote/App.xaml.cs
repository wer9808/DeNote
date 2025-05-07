using System.Configuration;
using System.Data;
using System.Windows;
using DeNote.ViewModels;
using DeNote.Views;

namespace DeNote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DrawingViewModel _drawingViewModel;
        private ToolbarWindow _toolbarWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _drawingViewModel = new DrawingViewModel();

            _toolbarWindow = new ToolbarWindow(_drawingViewModel);
            _toolbarWindow.Show();
        }
    }

}
