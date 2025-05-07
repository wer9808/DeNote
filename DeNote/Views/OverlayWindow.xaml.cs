using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DeNote.Views
{
    /// <summary>
    /// OverlayWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class OverlayWindow : Window
    {
        private DrawingViewModel viewModel { get => DataContext as DrawingViewModel; }

        public OverlayWindow(DrawingViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (viewModel != null && viewModel.DrawingUpdated)
            {
                viewModel.DrawingUpdated = false;
                RedrawCanvas();
            }
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            if (viewModel != null)
            {
                var startPoint = new StylusPoint(e.GetPosition(DrawingCanvas).X, e.GetPosition(DrawingCanvas).Y, 1.0f);
                viewModel.StartDrawingCommand.Execute(startPoint);
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
            }
        }

        private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;

            if (viewModel != null && viewModel.IsDrawing)
            {
                var endPoint = new StylusPoint(e.GetPosition(DrawingCanvas).X, e.GetPosition(DrawingCanvas).Y, 1.0f);
                viewModel.EndDrawingCommand.Execute(endPoint);
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


        private void RedrawCanvas()
        {
            if (viewModel != null)
            {
                DrawingCanvas.Children.Clear();
                foreach (var drawingObject in viewModel.DrawingObjects)
                {
                    var uiElement = DrawingObject.CreateVisualElement(DrawingCanvas, drawingObject);
                    if (uiElement != null) DrawingCanvas.Children.Add(uiElement);
                }

                var currentDrawingObject = viewModel.CurrentDrawingObject;
                if (currentDrawingObject != null)
                {
                    var uiElement = DrawingObject.CreateVisualElement(DrawingCanvas, currentDrawingObject);
                    if (uiElement != null) DrawingCanvas.Children.Add(uiElement);
                }

            }
        }


        // 윈도우 위치 설정 (예: 화면 오른쪽 상단)
        public void PositionWindow()
        {
            this.Left = 0;
            this.Top = 0;

            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }
    }
}
