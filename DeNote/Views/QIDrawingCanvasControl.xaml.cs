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
    /// QIDrawingCanvasControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QIDrawingCanvasControl : UserControl, IDisposable
    {
        public QIDrawingCanvasControl()
        {
            InitializeComponent();

            // Initialize the canvas or any other components if needed
            DrawingCanvasToolbar.RegisterCanvasControl(DrawingCanvasView);
            DrawingCanvasMenu.RegisterCanvasControl(DrawingCanvasView);

            DrawingCanvasMenu.CloseRequested += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ReloadCanvas()
        {
            Dispatcher.InvokeAsync(DrawingCanvasView.CaptureBackgroundAsync);
        }

        public void Dispose()
        {
            DrawingCanvasToolbar.Dispose();
            DrawingCanvasView.Dispose();
        }

        public event EventHandler? CloseRequested;
    }
}
