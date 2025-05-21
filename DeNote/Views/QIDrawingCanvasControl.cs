using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeNote.Models.DeNote.Models;
using DeNote.Models.Drawing;
using DeNote.Services;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using SkiaSharp;
using System.Windows.Input;
using System.Windows.Media;
using DeNote.Utils;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;

namespace DeNote.Views
{ /// <summary>
  /// QIDrawingCanvasControl.xaml에 대한 상호 작용 논리
  /// </summary>

    public enum BackgroundOption
    {
        Capture,
        SolidColor
    }

    public class QIDrawingCanvasControl : SKGLElement
    {
        private float defaultPressure = 1.0f; // 기본 압력 값

        private BackgroundOption backgroundOption = BackgroundOption.Capture;
        private SKBitmap _backgroundBitmap;
        private ScreenCaptureService _captureService;
        private bool _isBackgroundCaptured = false;
        public bool IsBackgroundCaptured => _isBackgroundCaptured;
        public bool IsCapturing { get; private set; }
        public event EventHandler BackgroundCaptureCompleted;

        private SKColor backgroundColor = SKColors.White;

        private QIDrawingContext drawingContext = new QIDrawingContext();
        private QIToolManager toolManager;
        private QIDrawingRenderer renderer = new QIDrawingRenderer();

        public QIDrawingCanvasControl()
        {
            toolManager = new QIToolManager(drawingContext);

            _captureService = new ScreenCaptureService();

            // 컨트롤이 로드될 때 초기 캡처 수행
            this.Loaded += QIDrawingCanvasControl_Loaded;
            this.Unloaded += QIDrawingCanvasControl_Unloaded;

            // 이벤트 연결
            drawingContext.VisualInvalidated += (s, e) => InvalidateVisual();
            toolManager.ToolChanged += OnToolChanged;
            drawingContext.ObjectAdded += OnObjectAdded;

            PaintSurface += OnPaintSurface;
        }

        private async void QIDrawingCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CaptureBackgroundAsync();
        }

        private void QIDrawingCanvasControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _backgroundBitmap?.Dispose();
        }

        protected void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (backgroundOption == BackgroundOption.Capture)
            {
                // 배경 그리기 (배경이 있는 경우)
                if (_backgroundBitmap != null)
                {
                    // SKBitmap을 캔버스 크기에 맞게 그리기
                    SKRect destRect = new SKRect(0, 0, e.Info.Width, e.Info.Height);
                    canvas.DrawBitmap(_backgroundBitmap, destRect);
                }
            }
            else
            {
                canvas.Clear(backgroundColor);
            }

            // 완성된 객체 렌더링
            foreach (var obj in drawingContext.Objects)
            {
                renderer.Render(canvas, obj);
            }

            // 활성 객체 렌더링 (생성/편집 중)
            if (drawingContext.ActiveObject != null)
            {
                drawingContext.ActiveObject.Render(canvas);
            }
        }

        private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (backgroundOption == BackgroundOption.Capture)
            {
                // 배경 그리기 (배경이 있는 경우)
                if (_backgroundBitmap != null)
                {
                    // SKBitmap을 캔버스 크기에 맞게 그리기
                    SKRect destRect = new SKRect(0, 0, e.Info.Width, e.Info.Height);
                    canvas.DrawBitmap(_backgroundBitmap, destRect);
                }
            }
            else
            {
                canvas.Clear(backgroundColor);
            }

            // 완성된 객체 렌더링
            foreach (var obj in drawingContext.Objects)
            {
                renderer.Render(canvas, obj);
            }

            // 활성 객체 렌더링 (생성/편집 중)
            if (drawingContext.ActiveObject != null)
            {
                drawingContext.ActiveObject.Render(canvas);
            }
        }

        /// <summary>
        /// 배경을 캡처합니다 (초기화 또는 수동 갱신용)
        /// </summary>
        public async Task CaptureBackgroundAsync()
        {
            if (IsCapturing)
                return;

            IsCapturing = true;

            try
            {
                // 현재 창의 핸들 가져오기
                Window currentWindow = Window.GetWindow(this);
                if (currentWindow == null)
                {
                    IsCapturing = false;
                    return;
                }

                IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(currentWindow).Handle;

                // 캡처 서비스를 통해 화면 캡처
                var newBitmap = await _captureService.CaptureScreenWithoutWindowAsync(windowHandle);

                // UI 스레드에서 비트맵 업데이트
                Dispatcher.Invoke(() =>
                {
                    if (newBitmap != null)
                    {
                        _backgroundBitmap?.Dispose();
                        _backgroundBitmap = newBitmap;
                        _isBackgroundCaptured = true;
                        InvalidateVisual();

                        // 캡처 완료 이벤트 발생
                        BackgroundCaptureCompleted?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Background capture error: {ex.Message}");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        /// <summary>
        /// 배경을 완전히 지웁니다
        /// </summary>
        public void ClearBackground()
        {
            Dispatcher.Invoke(() =>
            {
                _backgroundBitmap?.Dispose();
                _backgroundBitmap = null;
                _isBackgroundCaptured = false;
                InvalidateVisual();
            });
        }

        public async Task ToggleBackgroundOption()
        {
            if (this.backgroundOption == BackgroundOption.Capture)
            {
                this.backgroundOption = BackgroundOption.SolidColor;
                InvalidateVisual();
            }
            else
            {
                this.backgroundOption = BackgroundOption.Capture;
                await CaptureBackgroundAsync();
            }
        }

        public void ChangeBackgroundColor(SKColor color)
        {
            this.backgroundColor = color;
            InvalidateVisual();
        }

        // 객체 추가 시 처리
        private void OnObjectAdded(object sender, DrawingObjectEventArgs e)
        {
            // 필요한 경우 캐싱 등 처리
        }

        // 도구 변경 처리
        private void OnToolChanged(object sender, ToolChangedEventArgs e)
        {
            ToolChanged?.Invoke(this, e);
        }

        public QIDrawingToolType GetCurrentToolType()
        {
            return toolManager.ActiveToolType;
        }

        public void ChangeTool(QIDrawingToolType toolType)
        {
            toolManager.ActiveToolType = toolType;
        }

        // 현재 도구 설정 가져오기
        public QIDrawingToolSettings GetCurrentToolSettings()
        {
            return toolManager.GetCurrentSettings();
        }

        // 도구 설정 업데이트
        public void UpdateToolSettings(QIDrawingToolSettings settings)
        {
            toolManager.UpdateToolSettings(settings);
        }

        // 입력 이벤트 처리
        protected override void OnTouchDown(TouchEventArgs e)
        {
            if (toolManager.ActiveToolType == QIDrawingToolType.Pen)
            {
                var penToolSettings = toolManager.GetCurrentSettings() as QIPenToolSettings;
                if (penToolSettings != null)
                {
                    penToolSettings.PressureEnabled = false;
                    toolManager.UpdateToolSettings(penToolSettings);
                }
            }

            var point = e.GetTouchPoint(this).Position;

            toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Down,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure
            });

            e.Handled = true;
        }

        protected override void OnTouchMove(TouchEventArgs e)
        {
            var point = e.GetTouchPoint(this).Position;

            toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Move,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure
            });

            e.Handled = true;
        }

        protected override void OnTouchUp(TouchEventArgs e)
        {
            var point = e.GetTouchPoint(this).Position;

            toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Up,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure
            });

            e.Handled = true;
        }

        // 마우스 이벤트도 처리
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;

            if (toolManager.ActiveToolType == QIDrawingToolType.Pen)
            {
                var penToolSettings = toolManager.GetCurrentSettings() as QIPenToolSettings;
                if (penToolSettings != null)
                {
                    penToolSettings.PressureEnabled = false;
                    toolManager.UpdateToolSettings(penToolSettings);
                }
            }

            var point = e.GetPosition(this);

            toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Down,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure  // 마우스는 필압 정보 없음
            });

            e.Handled = true;
            CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.StylusDevice != null) return;

                var point = e.GetPosition(this);

                toolManager.HandleInput(new QIDrawingInputData
                {
                    Type = QIDrawingInputType.Move,
                    X = (float)point.X,
                    Y = (float)point.Y,
                    Pressure = defaultPressure
                });
            }

            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;

            var point = e.GetPosition(this);

            toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Up,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure
            });

            e.Handled = true;
            ReleaseMouseCapture();
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            if (toolManager.ActiveToolType == QIDrawingToolType.Pen)
            {
                var penToolSettings = toolManager.GetCurrentSettings() as QIPenToolSettings;
                if (penToolSettings != null)
                {
                    penToolSettings.PressureEnabled = true;
                    toolManager.UpdateToolSettings(penToolSettings);
                }
            }

            var points = e.GetStylusPoints(this);

            if (points.Count == 0)
                return;

            foreach (var point in points)
            {
                var input = new QIDrawingInputData
                {
                    Type = QIDrawingInputType.Down,
                    X = (float)point.X,
                    Y = (float)point.Y,
                    Pressure = point.PressureFactor
                };

                toolManager.HandleInput(input);
            }


            e.Handled = true;
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            var points = e.GetStylusPoints(this);
            if (points.Count == 0)
                return;
            foreach (var point in points)
            {
                var input = new QIDrawingInputData
                {
                    Type = QIDrawingInputType.Move,
                    X = (float)point.X,
                    Y = (float)point.Y,
                    Pressure = point.PressureFactor
                };

                toolManager.HandleInput(input);
            }

            e.Handled = true;
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            var points = e.GetStylusPoints(this);
            if (points.Count == 0)
                return;

            foreach (var point in points)
            {
                var input = new QIDrawingInputData
                {
                    Type = QIDrawingInputType.Up,
                    X = (float)point.X,
                    Y = (float)point.Y,
                    Pressure = point.PressureFactor
                };

                toolManager.HandleInput(input);
            }

            e.Handled = true;
        }

        public void Clear()
        {
            drawingContext.ClearObjects();
        }

        public void Undo()
        {
            drawingContext.CommandManager.Undo();
        }

        public void Redo()
        {
            drawingContext.CommandManager.Redo();
        }

        // 외부 이벤트
        public event EventHandler<ToolChangedEventArgs> ToolChanged;

    }
}
