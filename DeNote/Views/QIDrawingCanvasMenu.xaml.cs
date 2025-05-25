using DeNote.Services;
using DeNote.Views.Settings;
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

namespace DeNote.Views
{
    /// <summary>
    /// QIMenuToolbar.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QIDrawingCanvasMenu : UserControl, IDisposable
    {
        private QIDrawingCanvasView _canvasView;

        private ScreenRecorder screenRecorder = new ScreenRecorder();
        private AppConfigWindow? _appConfigWindow;
        public QIDrawingCanvasMenu()
        {
            InitializeComponent();
        }

        public void RegisterCanvasControl(QIDrawingCanvasView canvasView)
        {
            if (_canvasView != null)
            {
                // 이미 등록된 경우, 기존 이벤트 핸들러 제거
            }

            _canvasView = canvasView;
        }


        private async void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            await _canvasView.Undo();
        }

        private async void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            await _canvasView.Redo();
        }
        private async void ClearDrawingBtn_Click(object sender, RoutedEventArgs e)
        {
            await _canvasView.Clear();
        }

        private void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (screenRecorder.IsRecording)
            {
                screenRecorder.StopRecording();
                RecordBtn.Content = "🎦";
            }
            else
            {
                screenRecorder.StartRecording();
                RecordBtn.Content = "⏹️";
            }
        }
        private void ToggleBackgroundBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            _canvasView.ToggleBackgroundOption();
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            await _canvasView.SaveCapture();
        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseAppConfigWindow();

            _appConfigWindow = new AppConfigWindow();
            _appConfigWindow.Show();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CloseAppConfigWindow()
        {
            if (_appConfigWindow != null)
            {
                _appConfigWindow.Close();
                _appConfigWindow = null;
            }
        }

        public void Dispose()
        {
            screenRecorder.Dispose();
        }

        public event EventHandler? CloseRequested;
    }
}
