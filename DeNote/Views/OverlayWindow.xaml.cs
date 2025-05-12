using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DeNote.Models;
using DeNote.ViewModels;
using SkiaSharp;
using System.ComponentModel;
using Wpf.Ui.Tray.Controls;

namespace DeNote.Views
{
    /// <summary>
    /// OverlayWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class OverlayWindow : Window
    {
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        // Win32 API 선언
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_F = 0x46;  // F 키의 가상 키 코드

        private IntPtr _windowHandle;
        private HwndSource _source;

        private DrawingViewModel viewModel { get => DataContext as DrawingViewModel; }

        public OverlayWindow(DrawingViewModel viewModel)
        {
            InitializeComponent();

            if (viewModel != null)
            {
                viewModel.CaptureScreenCommand.Execute(null);
            }

            DataContext = viewModel;

            this.Loaded += OverlayWindow_Loaded;
            this.Closing += OverlayWindow_Closing;
            this.Activated += OverlayWindow_Activated;
            this.KeyDown += OverlayWindow_KeyDown;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 창 핸들 가져오기
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            // 글로벌 핫키 등록 (Ctrl+Shift+F)
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_F);
        }
        private void OverlayWindow_Closing(object? sender, CancelEventArgs e)
        {
            // 핫키 등록 해제
            UnregisterHotKey(_windowHandle, HOTKEY_ID);

            // HwndSource 정리
            _source?.RemoveHook(HwndHook);
            _source?.Dispose();
        }

        private void OverlayWindow_Activated(object? sender, EventArgs e)
        {

            if (viewModel != null)
            {
                viewModel.CaptureScreenCommand.Execute(null);
                DrawingCanvas.InvalidateVisual();
            }

        }

        private void OverlayWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Shift+F 키 조합 확인
            if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                ToggleWindowVisibility();
                e.Handled = true;
            }
        }

        private void ToggleWindowVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                HideToTray();
            }
            else
            {
                ShowFromTray();
            }
        }

        private void HideToTray()
        {
            // 창 숨기기
            Hide();
        }

        private void ShowFromTray()
        {
            // 창 보이기
            Show();
            WindowState = WindowState.Maximized;
            Activate();
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            if (viewModel != null)
            {
                var startPoint = new StylusPoint(e.GetPosition(DrawingCanvas).X, e.GetPosition(DrawingCanvas).Y, 1.0f);
                viewModel.StartDrawingCommand.Execute(startPoint);
                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
            }
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null) return;

            if (e.LeftButton == MouseButtonState.Pressed && viewModel != null && viewModel.IsDrawing)
            {
                var currentPoint = new StylusPoint(e.GetPosition(DrawingCanvas).X, e.GetPosition(DrawingCanvas).Y, 1.0f);
                if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Pen && IsPointOutsideCanvas(currentPoint))
                {
                    viewModel.EndDrawingCommand.Execute(currentPoint);
                }
                else
                {
                    viewModel.UpdateDrawingCommand.Execute(currentPoint);
                }

                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
            }
        }

        private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;

            if (viewModel != null && viewModel.IsDrawing)
            {
                var endPoint = new StylusPoint(e.GetPosition(DrawingCanvas).X, e.GetPosition(DrawingCanvas).Y, 1.0f);
                viewModel.EndDrawingCommand.Execute(endPoint);
                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
            }
        }

        private void DrawingCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {

            if (viewModel != null)
            {
                var stylusPoints = e.GetStylusPoints(DrawingCanvas);
                var startPoint = stylusPoints[0];

                viewModel.StartDrawingCommand.Execute(startPoint);
                if (viewModel.CurrentDrawingObjectType == Models.DrawingObjectType.Pen)
                {
                    for (int i = 1; i < stylusPoints.Count; i++)
                    {
                        var currentPoint = stylusPoints[i];
                        viewModel.UpdateDrawingCommand.Execute(currentPoint);
                    }
                }
                else
                {
                    var currentPoint = stylusPoints.Last();
                    viewModel.UpdateDrawingCommand.Execute(currentPoint);
                }
                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
            }

        }

        private void DrawingCanvas_StylusMove(object sender, StylusEventArgs e)
        {

            if (viewModel != null && viewModel.IsDrawing)
            {
                var stylusPoints = e.GetStylusPoints(DrawingCanvas);

                if (viewModel.CurrentDrawingObjectType == Models.DrawingObjectType.Pen)
                {
                    for (int i = 0; i < stylusPoints.Count; i++)
                    {
                        if (i + 1 < stylusPoints.Count && IsPointOutsideCanvas(stylusPoints[i + 1]))
                        {
                            var endPoint = stylusPoints[i];
                            viewModel.EndDrawingCommand.Execute(endPoint);
                        }
                        else
                        {
                            var currentPoint = stylusPoints[i];
                            viewModel.UpdateDrawingCommand.Execute(currentPoint);
                        }
                    }
                }
                else
                {
                    var currentPoint = stylusPoints.Last();
                    viewModel.UpdateDrawingCommand.Execute(currentPoint);
                }
                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
            }
        }

        private void DrawingCanvas_StylusUp(object sender, StylusEventArgs e)
        {

            if (viewModel != null && viewModel.IsDrawing)
            {
                var stylusPoints = e.GetStylusPoints(DrawingCanvas);

                if (viewModel.CurrentDrawingObjectType == Models.DrawingObjectType.Pen)
                {
                    for (int i = 0; i < stylusPoints.Count - 1; i++)
                    {
                        var currentPoint = stylusPoints[i];
                        viewModel.UpdateDrawingCommand.Execute(currentPoint);
                    }
                    var endPoint = stylusPoints.Last();
                    viewModel.EndDrawingCommand.Execute(endPoint);
                }
                else
                {
                    var endPoint = stylusPoints.Last();
                    viewModel.EndDrawingCommand.Execute(endPoint);
                }

                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
            }

        }
        private bool IsPointOutsideCanvas(StylusPoint point)
        {
            // 캔버스 경계 확인
            double x = point.X;
            double y = point.Y;

            return (x < 0 || x > DrawingCanvas.ActualWidth ||
                    y < 0 || y > DrawingCanvas.ActualHeight);
        }

        // 윈도우 위치 설정 (예: 화면 오른쪽 상단)
        public void PositionWindow()
        {
            this.Left = 0;
            this.Top = 0;

            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        private void DrawingCanvas_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.Transparent);

            if (viewModel == null) return;

            // 캡처한 화면을 그립니다.
            if (viewModel.CanvasScreenshotBitmap != null)
            {
                var screenshotBitmap = viewModel.CanvasScreenshotBitmap;

                // 전체 화면 크기 가져오기
                int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                SKRect destRect = new SKRect(0, 0, screenWidth, screenHeight);
                canvas.DrawBitmap(screenshotBitmap, destRect);
            }

            foreach (var drawingObject in viewModel.DrawingObjects)
            {
                DrawingObject.DrawObject(canvas, drawingObject);
            }

            var currentDrawingObject = viewModel.CurrentDrawingObject;
            if (currentDrawingObject != null)
            {
                DrawingObject.DrawObject(canvas, currentDrawingObject);
            }

        }

        private void PenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.ChangeToPenCommand.Execute(PenType.Normal);
            }
        }

        private void HighlighterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.ChangeToPenCommand.Execute(PenType.Highlighter);
            }
        }

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.ChangeToShapeCommand.Execute(ShapeDrawingType.Ellipse);
            }
        }

        private void ClearDrawingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.ClearDrawingCommand.Execute(null);
                DrawingCanvas.InvalidateVisual();
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.UndoCommand.Execute(null);
                DrawingCanvas.InvalidateVisual();
            }
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.RedoCommand.Execute(null);
                DrawingCanvas.InvalidateVisual();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        private void NotifyIcon_Show(object sender, RoutedEventArgs e)
        {
            ShowFromTray();
        }

        private void NotifyIcon_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 핫키 메시지 처리
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleWindowVisibility();
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
