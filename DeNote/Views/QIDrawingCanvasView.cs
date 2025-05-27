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

    public class QIDrawingCanvasView : SKGLElement
    {
        private float defaultPressure = 1.0f; // 기본 압력 값

        private BackgroundOption backgroundOption = BackgroundOption.Capture;
        private SKBitmap? _backgroundBitmap;
        private bool _isBackgroundCaptured = false;
        public bool IsBackgroundCaptured => _isBackgroundCaptured;
        public bool IsCapturing { get; private set; }
        public event EventHandler BackgroundCaptureCompleted;

        private SKColor backgroundColor = SKColors.White;

        private QIDrawingContext drawingContext;
        private QIToolManager toolManager;
        private QIDrawingRenderer renderer = new QIDrawingRenderer();

        private DispatcherTimer _longPressTimer;
        private QIPoint _startPosition;
        private const double LongPressThresholdSeconds = 1.5;
        private const float MovementThreshold = 1.0f; // 긴 누름 모니터링 중 마우스 이동 허용 범위
        private const float StylusMovementThreshold = 5.0f; // 긴 누름 모니터링 중 마우스 이동 허용 범위
        // True if the stylus or mouse button is currently pressed down,
        // and a long press action has not yet been triggered or cancelled by movement.
        private bool _isMonitoringForLongPress = false;

        public QIDrawingCanvasView()
        {
            float primaryScreenWidth = (float)SystemParameters.PrimaryScreenWidth;
            float primaryScreenHeight = (float)SystemParameters.PrimaryScreenHeight;

            drawingContext = new QIDrawingContext(primaryScreenWidth, primaryScreenHeight);
            toolManager = new QIToolManager(drawingContext);

            // 컨트롤이 로드될 때 초기 캡처 수행
            this.Loaded += QIDrawingCanvasControl_Loaded;
            this.Unloaded += QIDrawingCanvasControl_Unloaded;

            // 이벤트 연결
            drawingContext.VisualInvalidated += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(() => InvalidateVisual());
            };
            toolManager.ToolChanged += OnToolChanged;

            PaintSurface += OnPaintSurface;

            _longPressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(LongPressThresholdSeconds)
            };
            _longPressTimer.Tick += _longPressTimer_Tick;
        }

        private void _longPressTimer_Tick(object? sender, EventArgs e)
        {
            if (_isMonitoringForLongPress)
            {
                _longPressTimer.Stop();
                _isMonitoringForLongPress = false;
                // 긴 누름 이벤트 발생
                var MenuRequestedEventArgs = new MenuRequestedEventArgs
                {
                    MousePosition = _startPosition,
                    MenuRequestType = MenuRequestType.Open
                };
                MenuRequested?.Invoke(this, MenuRequestedEventArgs);
            }
        }

        private void StartLongPressMonitoring(QIPoint position)
        {
            _startPosition = position;
            _isMonitoringForLongPress = true;
            _longPressTimer.Start();
        }

        private void StopLongPressMonitoring()
        {
            _isMonitoringForLongPress = false;
            _longPressTimer.Stop();
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

            float primaryScreenWidth = (float)SystemParameters.PrimaryScreenWidth;
            float primaryScreenHeight = (float)SystemParameters.PrimaryScreenHeight;
            SKRect destRect = new SKRect(0, 0, primaryScreenWidth, primaryScreenHeight);

            if (backgroundOption == BackgroundOption.Capture)
            {
                // 배경 그리기 (배경이 있는 경우)
                if (_backgroundBitmap != null)
                {
                    // SKBitmap을 캔버스 크기에 맞게 그리기
                    canvas.DrawBitmap(_backgroundBitmap, destRect);
                }
            }
            else
            {
                canvas.Clear(backgroundColor);
            }

            if (drawingContext.IsErasing)
            {
                canvas.DrawBitmap(drawingContext.EraserBitmap, destRect);
                return;
            }

            // 완성된 객체 렌더링
            foreach (var obj in drawingContext.OrderedObjects)
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

            float primaryScreenWidth = (float)SystemParameters.PrimaryScreenWidth;
            float primaryScreenHeight = (float)SystemParameters.PrimaryScreenHeight;
            SKRect destRect = new SKRect(0, 0, primaryScreenWidth, primaryScreenHeight);

            if (backgroundOption == BackgroundOption.Capture)
            {
                // 배경 그리기 (배경이 있는 경우)
                if (_backgroundBitmap != null)
                {
                    // SKBitmap을 캔버스 크기에 맞게 그리기
                    canvas.DrawBitmap(_backgroundBitmap, destRect);
                }
            }
            else
            {
                canvas.Clear(backgroundColor);
            }

            if (drawingContext.IsErasing)
            {
                canvas.DrawBitmap(drawingContext.EraserBitmap, destRect);
                return;
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
                var newBitmap = await ScreenCaptureService.CaptureScreenWithoutWindowAsync(windowHandle);

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

        public void ToggleBackgroundOption()
        {
            if (this.backgroundOption == BackgroundOption.Capture)
            {
                this.backgroundOption = BackgroundOption.SolidColor;
                InvalidateVisual();
            }
            else
            {
                this.backgroundOption = BackgroundOption.Capture;
                InvalidateVisual();
                // await CaptureBackgroundAsync();
            }
        }

        public void ChangeBackgroundColor(SKColor color)
        {
            this.backgroundColor = color;
            InvalidateVisual();
        }

        // 도구 변경 처리
        private void OnToolChanged(object? sender, ToolChangedEventArgs e)
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
        public QIDrawingToolSettings? GetCurrentToolSettings()
        {
            return toolManager?.GetCurrentSettings();
        }

        // 도구 설정 업데이트
        public void UpdateToolSettings(QIDrawingToolSettings settings)
        {
            toolManager.UpdateToolSettings(settings);
        }

        // 마우스 이벤트도 처리
        protected override async void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!drawingContext.CanExecute) return;
            if (e.StylusDevice != null) return;

            var point = e.GetPosition(this);
            var startPosition = new QIPoint
            {
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure // 마우스는 필압 정보 없음
            };

            var MenuRequestedEventArgs = new MenuRequestedEventArgs
            {
                MousePosition = startPosition,
                MenuRequestType = MenuRequestType.Close
            };
            MenuRequested?.Invoke(this, MenuRequestedEventArgs);

            if (!_isMonitoringForLongPress)
            {
                StartLongPressMonitoring(startPosition);
            }

            e.Handled = true;
        }

        private async Task StartDrawing(QIPoint point)
        {
            await toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Down,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = point.Pressure  // 마우스는 필압 정보 없음
            });

            drawingContext.IsDrawing = true;
        }

        protected override async void OnMouseMove(MouseEventArgs e)
        {
            if (e.StylusDevice != null) return;

            var point = e.GetPosition(this);
            var mousePosition = new QIPoint
            {
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure // 마우스는 필압 정보 없음
            };

            if (_isMonitoringForLongPress && _startPosition.DistanceTo(mousePosition) >= MovementThreshold)
            {
                // 마우스가 움직이면 긴 누름 모니터링 중지
                StopLongPressMonitoring();
                await StartDrawing(_startPosition);
                CaptureMouse();
            }

            if (drawingContext.IsDrawing)
            {
                if (!drawingContext.CanExecute) return;
                await toolManager.HandleInput(new QIDrawingInputData
                {
                    Type = QIDrawingInputType.Move,
                    X = (float)point.X,
                    Y = (float)point.Y,
                    Pressure = defaultPressure
                });

                e.Handled = true;
            }
        }

        protected override async void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            if (_isMonitoringForLongPress)
            {
                // 긴 누름 모니터링 중지
                StopLongPressMonitoring();
            }

            if (!drawingContext.IsDrawing) return;
            if (e.StylusDevice != null) return;

            var point = e.GetPosition(this);

            await toolManager.HandleInput(new QIDrawingInputData
            {
                Type = QIDrawingInputType.Up,
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = defaultPressure
            });

            drawingContext.IsDrawing = false;
            e.Handled = true;
            ReleaseMouseCapture();
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // 스타일러스 입력에 의해 발생한 마우스 이벤트는 무시
            if (e.StylusDevice != null)
            {
                return;
            }

            var point = e.GetPosition(this);
            var mousePosition = new QIPoint
            {
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = 1.0f // 마우스는 필압 정보 없음
            };
            MenuRequested?.Invoke(this, new MenuRequestedEventArgs { MousePosition = mousePosition, MenuRequestType = MenuRequestType.Open });
            e.Handled = true;
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            var points = e.GetStylusPoints(this);

            if (points.Count == 0)
                return;

            int i = 0;
            var point = points[i];
            // 긴 누름 모니터링 시작
            var startPosition = new QIPoint
            {
                X = (float)point.X,
                Y = (float)point.Y,
                Pressure = point.PressureFactor
            };

            var MenuRequestedEventArgs = new MenuRequestedEventArgs
            {
                MousePosition = startPosition,
                MenuRequestType = MenuRequestType.Close
            };
            MenuRequested?.Invoke(this, MenuRequestedEventArgs);

            if (!_isMonitoringForLongPress)
            {
                StartLongPressMonitoring(startPosition);
            }

            e.Handled = true;
        }

        protected override async void OnStylusMove(StylusEventArgs e)
        {
            var points = e.GetStylusPoints(this);
            if (points.Count == 0)
            {
                if (_isMonitoringForLongPress)
                {
                    // 스타일러스가 움직이지 않으면 긴 누름 모니터링 중지
                    StopLongPressMonitoring();
                }
            }
            var firstPoint = points.FirstOrDefault();
            var mousePosition = new QIPoint
            {
                X = (float)firstPoint.X,
                Y = (float)firstPoint.Y,
                Pressure = firstPoint.PressureFactor
            };

            if (_isMonitoringForLongPress && _startPosition.DistanceTo(mousePosition) >= StylusMovementThreshold)
            {
                // 스타일러스가 움직이면 긴 누름 모니터링 중지
                if (!drawingContext.CanExecute) return;
                StopLongPressMonitoring();
                await StartDrawing(_startPosition);
                CaptureStylus();
            }

            if (drawingContext.IsDrawing)
            {
                foreach (var point in points)
                {
                    var input = new QIDrawingInputData
                    {
                        Type = QIDrawingInputType.Move,
                        X = (float)point.X,
                        Y = (float)point.Y,
                        Pressure = point.PressureFactor
                    };

                    await toolManager.HandleInput(input);
                }

                e.Handled = true;
            }
        }

        protected override async void OnStylusUp(StylusEventArgs e)
        {
            if (_isMonitoringForLongPress)
            {
                // 긴 누름 모니터링 중지
                StopLongPressMonitoring();
            }

            if (!drawingContext.IsDrawing) return;

            var points = e.GetStylusPoints(this);
            if (points.Count == 0)
                return;

            int i;
            for (i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var input = new QIDrawingInputData
                {
                    Type = QIDrawingInputType.Move,
                    X = (float)point.X,
                    Y = (float)point.Y,
                    Pressure = point.PressureFactor
                };

                await toolManager.HandleInput(input);
            }

            var lastPoint = points.Last();
            var endInput = new QIDrawingInputData
            {
                Type = QIDrawingInputType.Up,
                X = (float)lastPoint.X,
                Y = (float)lastPoint.Y,
                Pressure = lastPoint.PressureFactor
            };

            await toolManager.HandleInput(endInput);

            ReleaseStylusCapture();
            drawingContext.IsDrawing = false;
            e.Handled = true;
        }

        public async Task Clear()
        {
            if (!drawingContext.CanExecute) return;
            await drawingContext.ClearObjects();
        }

        public async Task Undo()
        {
            if (!drawingContext.CanExecute) return;
            await drawingContext.CommandManager.Undo();
        }

        public async Task Redo()
        {
            if (!drawingContext.CanExecute) return;
            await drawingContext.CommandManager.Redo();
        }

        public async Task SaveCapture()
        {
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

                await ScreenCaptureService.SaveScreenShot(windowHandle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Background capture error: {ex.Message}");
            }
        }

        // 외부 이벤트
        public event EventHandler<ToolChangedEventArgs> ToolChanged;

        public event EventHandler<MenuRequestedEventArgs> MenuRequested;

        public enum MenuRequestType
        {
            Open,
            Close,

        }

        public class MenuRequestedEventArgs : EventArgs
        {
            public QIPoint MousePosition { get; set; }
            public required MenuRequestType MenuRequestType { get; set; }
        }
    }
}
