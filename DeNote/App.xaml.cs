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

            var splashWindow = new SplashWindow();

            splashWindow.MediaEnded += (s, e) =>
            {
                Thread.Sleep(2000); // Optional delay for better UX
                splashWindow.Close();
                _overlayWindow.Show();
                _overlayWindow.PositionWindow(); // Position the overlay window
            };

            splashWindow.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Clean up resources if needed
            _overlayWindow?.Dispose();
        }
    }

}
