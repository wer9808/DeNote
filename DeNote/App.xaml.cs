using System.Configuration;
using System.Data;
using System.Windows;
using DeNote.Views;

namespace DeNote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private OverlayWindow _overlayWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _overlayWindow = new OverlayWindow();
            _overlayWindow?.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Clean up resources if needed
            _overlayWindow?.Dispose();
        }
    }

}
