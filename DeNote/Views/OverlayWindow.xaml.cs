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
using SkiaSharp;
using System.ComponentModel;
using Wpf.Ui.Tray.Controls;
using DeNote.Services;
using Brushes = System.Windows.Media.Brushes;
using DeNote.Models.Drawing;
using DeNote.Models.DeNote.Models;
using Color = System.Windows.Media.Color;

namespace DeNote.Views
{
    /// <summary>
    /// OverlayWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class OverlayWindow : Window, IDisposable
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

        private ScreenRecorder screenRecorder = new ScreenRecorder();

        public OverlayWindow()
        {
            InitializeComponent();

            this.Loaded += OverlayWindow_Loaded;
            this.Closing += OverlayWindow_Closing;
            this.Activated += OverlayWindow_Activated;

            UpdateColorPreview();
            UpdateToolButtonStates();
            DrawingCanvas.ToolChanged += (s, e) => UpdateToolButtonStates();
            DrawingCanvas.MenuRequested += (s, e) => OnMenuRequested(s, e);
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

            Dispatcher.InvokeAsync(DrawingCanvas.CaptureBackgroundAsync);

            WindowState = WindowState.Maximized;
            Activate();
        }

        // 윈도우 위치 설정 (예: 화면 오른쪽 상단)
        public void PositionWindow()
        {
            this.Left = 0;
            this.Top = 0;

            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }


        private void PenBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ChangeTool(QIDrawingToolType.Pen);
            UpdateColorPreview();
        }

        private void HighlighterBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ChangeTool(QIDrawingToolType.Highlighter);
            UpdateColorPreview();
        }


        private void UpdateShapeButtonsState()
        {

            var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;

            RectangleBtn.Background = Brushes.White;
            EllipseBtn.Background = Brushes.White;
            TriangleBtn.Background = Brushes.White;
            LineBtn.Background = Brushes.White;

            var shapeType = settings.Type;
            switch (shapeType)
            {
                case QIShapeType.Rectangle:
                    RectangleBtn.Background = Brushes.Orange;
                    break;
                case QIShapeType.Ellipse:
                    EllipseBtn.Background = Brushes.Orange;
                    break;
                case QIShapeType.Triangle:
                    TriangleBtn.Background = Brushes.Orange;
                    break;
                case QIShapeType.Line:
                    LineBtn.Background = Brushes.Orange;
                    break;
            }
        }

        private void InitializeShapePickerPopup()
        {
            var toolType = DrawingCanvas.GetCurrentToolType();

            if (toolType != QIDrawingToolType.Shape)
            {
                return;
            }

            UpdateShapeButtonsState();
        }

        private void FillOptionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;

            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var fillOption = checkBox.IsChecked ?? false;
                settings.FillOption = fillOption;
                settings.FillColor = settings.Color;
                DrawingCanvas.UpdateToolSettings(settings);
            }
        }

        private void ShapeTypeBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;

            if (button != null && settings != null)
            {
                switch (button.Tag.ToString())
                {
                    case "Rectangle":
                        settings.Type = QIShapeType.Rectangle;
                        break;
                    case "Ellipse":
                        settings.Type = QIShapeType.Ellipse;
                        break;
                    case "Triangle":
                        settings.Type = QIShapeType.Triangle;
                        break;
                    case "Line":
                        settings.Type = QIShapeType.Line;
                        break;
                }

                DrawingCanvas.UpdateToolSettings(settings);
                UpdateShapeButtonsState();
            }

        }

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            var toolType = DrawingCanvas.GetCurrentToolType();

            if (toolType == QIDrawingToolType.Shape)
            {
                // 이미 도형 도구가 선택된 경우
                // 도형 선택 팝업 열기
                InitializeShapePickerPopup();
                ShapePickerPopup.IsOpen = !ShapePickerPopup.IsOpen;
            }
            else
            {
                // 도형 도구로 변경
                DrawingCanvas.ChangeTool(QIDrawingToolType.Shape);

                UpdateColorPreview();
            }
        }

        private void EraserBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.ChangeTool(QIDrawingToolType.Eraser);
            UpdateColorPreview();
        }


        private void UpdateToolButtonStates()
        {
            // 모든 버튼의 배경을 초기화
            PenBtn.Background = Brushes.Transparent;
            HighlighterBtn.Background = Brushes.Transparent;
            ShapeBtn.Background = Brushes.Transparent;
            EraserBtn.Background = Brushes.Transparent;

            var toolType = DrawingCanvas.GetCurrentToolType();
            var selectedColor = Brushes.Orange;

            // 선택된 버튼의 배경만 변경
            switch (toolType)
            {
                case QIDrawingToolType.Pen:
                    PenBtn.Background = selectedColor;
                    break;
                case QIDrawingToolType.Highlighter:
                    HighlighterBtn.Background = selectedColor;
                    break;
                case QIDrawingToolType.Shape:
                    ShapeBtn.Background = selectedColor;
                    break;
                case QIDrawingToolType.Eraser:
                    EraserBtn.Background = selectedColor;
                    break;
            }
        }


        private void ClearDrawingBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Clear();
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Undo();
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Redo();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
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

        public void Dispose()
        {
            screenRecorder.Dispose();
            // 핫키 등록 해제
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            // HwndSource 정리
            _source?.RemoveHook(HwndHook);
            _source?.Dispose();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateColorPreview()
        {
            var toolType = DrawingCanvas.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Pen)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIPenToolSettings;
                ColorPreviewCircle.Fill = new SolidColorBrush(Color.FromArgb(settings.Color.Alpha, settings.Color.Red, settings.Color.Green, settings.Color.Blue));
            }
            else if (toolType == QIDrawingToolType.Highlighter)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIHighlighterToolSettings;
                ColorPreviewCircle.Fill = new SolidColorBrush(Color.FromArgb(settings.Color.Alpha, settings.Color.Red, settings.Color.Green, settings.Color.Blue));
            }
            else if (toolType == QIDrawingToolType.Shape)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;
                ColorPreviewCircle.Fill = new SolidColorBrush(Color.FromArgb(settings.Color.Alpha, settings.Color.Red, settings.Color.Green, settings.Color.Blue));
            }
            else if (toolType == QIDrawingToolType.Eraser)
            {
                ColorPreviewCircle.Fill = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void ColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            var toolType = DrawingCanvas.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Eraser)
            {
                ColorPickerPopup.IsOpen = false;
                return;
            }
            ColorPickerPopup.IsOpen = !ColorPickerPopup.IsOpen;
        }

        private void ThicknessBtn_Click(object sender, RoutedEventArgs e)
        {
            var toolType = DrawingCanvas.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Pen)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIPenToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }
            else if (toolType == QIDrawingToolType.Highlighter)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIHighlighterToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }
            else if (toolType == QIDrawingToolType.Shape)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }
            else if (toolType == QIDrawingToolType.Eraser)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIEraserToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }

            ThicknessPopup.IsOpen = !ThicknessPopup.IsOpen;
        }


        private void ColorBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                        // 색상 선택 팝업 열기
                var color = (Color)button.Background.GetValue(SolidColorBrush.ColorProperty);

                var toolType = DrawingCanvas.GetCurrentToolType();
                if (toolType == QIDrawingToolType.Pen)
                {
                    var settings = DrawingCanvas.GetCurrentToolSettings() as QIPenToolSettings;
                    settings.Color = SKColor.Parse(color.ToString());
                    DrawingCanvas.UpdateToolSettings(settings);
                }
                else if (toolType == QIDrawingToolType.Highlighter)
                {
                    var settings = DrawingCanvas.GetCurrentToolSettings() as QIHighlighterToolSettings;
                    settings.Color = SKColor.Parse(color.ToString());
                    DrawingCanvas.UpdateToolSettings(settings);
                }
                else if (toolType == QIDrawingToolType.Shape)
                {
                    var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;
                    settings.Color = SKColor.Parse(color.ToString());
                    DrawingCanvas.UpdateToolSettings(settings);
                }

                        // 선택된 색상으로 도구 설정 업데이트
                UpdateColorPreview();
            }
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var toolType = DrawingCanvas.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Pen)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIPenToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                DrawingCanvas.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Highlighter)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIHighlighterToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                DrawingCanvas.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Shape)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIShapeToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                DrawingCanvas.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Eraser)
            {
                var settings = DrawingCanvas.GetCurrentToolSettings() as QIEraserToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                DrawingCanvas.UpdateToolSettings(settings);
            }
        }

        private async void ToggleBackgroundBtn_Click(object sender, RoutedEventArgs e)
        {
            await DrawingCanvas.ToggleBackgroundOption();
        }

        private void OnMenuRequested(object sender, QIDrawingCanvasControl.MenuRequestedEventArgs e)
        {

            var requestType = e.MenuRequestType;

            if (requestType == QIDrawingCanvasControl.MenuRequestType.Open)
            {

            }

        }
    }
}
