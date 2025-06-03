using DeNote.Services;
using DeNote.Views.Settings;
using ScreenRecorderLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private ScreenRecorder _screenRecorder = new ScreenRecorder();
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
            _screenRecorder = new ScreenRecorder();
            _screenRecorder.RecordingFailed += ScreenRecorder_RecordingFailed;
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

        private void ScreenRecorder_RecordingFailed(object? sender, RecordingFailedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RecordBtn.Content = "🎦";
                MessageBox.Show($"녹화에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_screenRecorder.IsRecording)
            {
                _screenRecorder.StopRecording();
                RecordBtn.Content = "🎦";
            }
            else
            {
                try
                {
                    _screenRecorder.StartRecording();
                    RecordBtn.Content = "⏹️";
                }
                catch (UnauthorizedAccessException ex)
                {
                    RecordBtn.Content = "🎦";
                    MessageBox.Show($"녹화를 시작할 수 없습니다. 녹화 영상 저장 경로에 대한 접근 권한이 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    RecordBtn.Content = "🎦";
                    MessageBox.Show($"녹화를 시작할 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            _screenRecorder.Dispose();
        }

        public event EventHandler? CloseRequested;
    }
}
