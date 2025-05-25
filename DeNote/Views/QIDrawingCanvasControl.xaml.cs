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
    /// QIDrawingCanvasControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QIDrawingCanvasControl : UserControl, IDisposable
    {
        public QIDrawingCanvasControl()
        {
            InitializeComponent();

            this.Loaded += QIDrawingCanvasControl_Loaded;
        }

        private void QIDrawingCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the canvas or any other components if needed
            DrawingCanvasToolbar.RegisterCanvasControl(DrawingCanvasView);
            DrawingCanvasMenu.RegisterCanvasControl(DrawingCanvasView);

            DrawingCanvasMenu.CloseRequested += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public async void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            Debug.WriteLine($"CanvasControl Preview Key Down: {e.Key}");
            // Handle key down events if necessary
            // Ctrl+Z 확인
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+Shift+Z가 아닌 경우에만 처리
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                {
                    await DrawingCanvasView.Undo(); // Undo 작업 수행
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Ctrl+Shift+Z는 다른 곳에서 처리
                    await DrawingCanvasView.Redo(); // Redo 작업 수행
                }
            }

            else if (e.Key == Key.F5)
            {
                await DrawingCanvasView.Clear();
                await DrawingCanvasView.CaptureBackgroundAsync();
            }

            else if (e.Key == Key.F6)
            {
                await DrawingCanvasView.CaptureBackgroundAsync();
            }

            else if (e.Key == Key.F7)
            {
                DrawingCanvasView.ToggleBackgroundOption();
            }

            else if (e.Key == Key.PrintScreen)
            {
                await DrawingCanvasView.SaveCapture();
            }

            e.Handled = true; // 이벤트가 다른 곳으로 전파되는 것을 막음
        }

        public void ReloadCanvas()
        {
            Dispatcher.InvokeAsync(DrawingCanvasView.CaptureBackgroundAsync);
        }

        public void Dispose()
        {
            DrawingCanvasToolbar.Dispose();
            DrawingCanvasView.Dispose();
            DrawingCanvasMenu.Dispose();
        }

        public event EventHandler? CloseRequested;
    }
}
