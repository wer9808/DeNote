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
using DeNote.Models.DeNote.Models;
using DeNote.Models.Drawing;
using DeNote.Services;
using SkiaSharp;

namespace DeNote.Views
{
    /// <summary>
    /// QIDrawingToolbar.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QIDrawingToolbar : UserControl, IDisposable
    {
        private QIDrawingCanvasView _canvasView;

        public QIDrawingToolbar()
        {
            InitializeComponent();
        }

        public void RegisterCanvasControl(QIDrawingCanvasView canvasView)
        {
            if (_canvasView != null)
            {
                        // 이미 등록된 경우, 기존 이벤트 핸들러 제거
                _canvasView.ToolChanged -= (s, e) => UpdateToolButtonStates();
                _canvasView.MenuRequested -= (s, e) => OnMenuRequested(s, e);
            }

            _canvasView = canvasView;

            _canvasView.ToolChanged += (s, e) => UpdateToolButtonStates();
            _canvasView.MenuRequested += (s, e) => OnMenuRequested(s, e);
                  
                  // 초기 도구 설정 업데이트
            UpdateColorPreview();
            UpdateToolButtonStates();
        }

        private void UpdateShapeButtonsState()
        {
            if (_canvasView == null) return;
            var settings = _canvasView.GetCurrentToolSettings() as QIShapeToolSettings;

            RectangleBtn.Background = Brushes.White;
            EllipseBtn.Background = Brushes.White;
            TriangleBtn.Background = Brushes.White;
            LineBtn.Background = Brushes.White;

            var shapeType = settings!.Type;
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
            if (_canvasView == null) return;
            var toolType = _canvasView.GetCurrentToolType();

            if (toolType != QIDrawingToolType.Shape)
            {
                return;
            }

            UpdateShapeButtonsState();
        }

        private void FillOptionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            var settings = _canvasView.GetCurrentToolSettings() as QIShapeToolSettings;

            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var fillOption = checkBox.IsChecked ?? false;
                settings.FillOption = fillOption;
                settings.FillColor = settings.Color;
                _canvasView.UpdateToolSettings(settings);
            }
        }

        private void ShapeTypeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            var button = sender as Button;

            var settings = _canvasView.GetCurrentToolSettings() as QIShapeToolSettings;

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

                _canvasView.UpdateToolSettings(settings);
                UpdateShapeButtonsState();
            }

        }

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            var toolType = _canvasView.GetCurrentToolType();

            if (toolType != QIDrawingToolType.Shape) return;

            // 이미 도형 도구가 선택된 경우
            // 도형 선택 팝업 열기
            InitializeShapePickerPopup();
            ShapePickerPopup.IsOpen = !ShapePickerPopup.IsOpen;
        }

        private void UpdateToolButtonStates()
        {
            if (_canvasView == null) return;
            // 모든 버튼의 배경을 초기화
            var toolType = _canvasView.GetCurrentToolType();
            var selectedColor = Brushes.Orange;

            var buttonTextBlock = DrawingToolPickerBtn.Content as TextBlock;


            // 선택된 버튼의 배경만 변경
            switch (toolType)
            {
                case QIDrawingToolType.Pen:
                    buttonTextBlock.Text = "✏️";
                    break;
                case QIDrawingToolType.Highlighter:
                    buttonTextBlock.Text = "🖍️";
                    break;
                case QIDrawingToolType.Shape:
                    buttonTextBlock.Text = "🔲";
                    break;
                case QIDrawingToolType.Eraser:
                    buttonTextBlock.Text = "🧽";
                    break;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            ToolbarPopup.IsOpen = false;
        }

        private void UpdateColorPreview()
        {
            if (_canvasView == null) return;
            var toolType = _canvasView.GetCurrentToolType();

            if (toolType == QIDrawingToolType.Eraser)
            {
                ColorPreviewCircle.Fill = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIDrawingToolSettings;
                if (settings != null)
                {
                    ColorPreviewCircle.Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)(settings.Color.Alpha),
                        (byte)(settings.Color.Red),
                        (byte)(settings.Color.Green),
                        (byte)(settings.Color.Blue)));
                }
            }
        }

        private void ColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            var toolType = _canvasView.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Eraser)
            {
                ColorPickerPopup.IsOpen = false;
                return;
            }
            UpdateColorPreview();
            ColorPickerPopup.IsOpen = !ColorPickerPopup.IsOpen;
        }

        private void ThicknessBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            var toolType = _canvasView.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Pen)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIPenToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }
            else if (toolType == QIDrawingToolType.Highlighter)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIHighlighterToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }
            else if (toolType == QIDrawingToolType.Shape)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIShapeToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }
            else if (toolType == QIDrawingToolType.Eraser)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIEraserToolSettings;
                ThicknessSlider.Value = settings.StrokeWidth;
            }

            ThicknessPopup.IsOpen = !ThicknessPopup.IsOpen;
        }


        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_canvasView == null) return;
            var toolType = _canvasView.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Pen)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIPenToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                _canvasView.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Highlighter)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIHighlighterToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                _canvasView.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Shape)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIShapeToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                _canvasView.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Eraser)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIEraserToolSettings;
                settings.StrokeWidth = (float)e.NewValue;
                _canvasView.UpdateToolSettings(settings);
            }
        }


        private void OnMenuRequested(object? sender, QIDrawingCanvasView.MenuRequestedEventArgs e)
        {

            var requestType = e.MenuRequestType;

            if (requestType == QIDrawingCanvasView.MenuRequestType.Open)
            {
                ToolbarPopup.IsOpen = true;
            }
            else if (requestType == QIDrawingCanvasView.MenuRequestType.Close)
            {
                ToolbarPopup.IsOpen = false;
            }

        }

        public void Dispose()
        {

        }

        private void ColorPickerPopup_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            // 색상 선택 팝업 열기
            var color = ColorPickerPopup.SelectedColor;
            var toolType = _canvasView.GetCurrentToolType();
            if (toolType == QIDrawingToolType.Pen)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIPenToolSettings;
                settings.Color = SKColor.Parse(color.ToString());
                _canvasView.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Highlighter)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIHighlighterToolSettings;
                settings.Color = SKColor.Parse(color.ToString());
                _canvasView.UpdateToolSettings(settings);
            }
            else if (toolType == QIDrawingToolType.Shape)
            {
                var settings = _canvasView.GetCurrentToolSettings() as QIShapeToolSettings;
                settings.Color = SKColor.Parse(color.ToString());
                _canvasView.UpdateToolSettings(settings);
            }

            UpdateColorPreview();
        }

        private void DrawingToolPickerPopup_SelectedToolChanged(object sender, RoutedPropertyChangedEventArgs<QIDrawingToolType> e)
        {
            if (_canvasView == null) return;
            _canvasView.ChangeTool(e.NewValue);
            UpdateToolButtonStates();
            UpdateColorPreview();
        }

        private void DrawingToolPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_canvasView == null) return;
            DrawingToolPickerPopup.SelectedTool = _canvasView.GetCurrentToolType();
            DrawingToolPickerPopup.IsOpen = !DrawingToolPickerPopup.IsOpen;
        }
    }
}
