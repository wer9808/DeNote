using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DeNote.Models.DeNote.Models;
using DeNote.Models.Drawing;
using SkiaSharp;

namespace DeNote.Services
{
    public class QIToolManager
    {

        private Dictionary<QIDrawingToolType, IQIDrawingTool> _tools = new Dictionary<QIDrawingToolType, IQIDrawingTool>();
        private Dictionary<QIDrawingToolType, QIDrawingToolSettings> _settingsStore = new Dictionary<QIDrawingToolType, QIDrawingToolSettings>();

        private IQIDrawingTool? _activeTool = null;
        public IQIDrawingTool? ActiveTool => _activeTool;

        private QIDrawingToolType _activeToolType = QIDrawingToolType.Pen;
        public QIDrawingToolType ActiveToolType
        {
            get => _activeToolType;
            set => SwitchTool(value);
        }

        private bool _isGestureCapturing = false;
        private SKPoint? _holdStartPoint = null;
        public DispatcherTimer HoldGestureTimer { get; private set; }

        // 입력 데이터 컨텍스트
        public QIDrawingContext Context { get; set; }

        public QIToolManager(QIDrawingContext context)
        {
            Context = context;

            // 도구 초기화
            _tools[QIDrawingToolType.Pen] = new QIPenTool();
            _tools[QIDrawingToolType.Highlighter] = new QIHighlighterTool();
            _tools[QIDrawingToolType.Shape] = new QIShapeTool();
            _tools[QIDrawingToolType.Eraser] = new QIEraserTool();
            // 기타 도구...

            // 초기 도구 설정
            _settingsStore[QIDrawingToolType.Pen] = new QIPenToolSettings();
            _settingsStore[QIDrawingToolType.Highlighter] = new QIHighlighterToolSettings();
            _settingsStore[QIDrawingToolType.Shape] = new QIShapeToolSettings();
            _settingsStore[QIDrawingToolType.Eraser] = new QIEraserToolSettings();

            HoldGestureTimer = new DispatcherTimer();
            HoldGestureTimer.Interval = TimeSpan.FromMilliseconds(500); // 0.5초 간격
            HoldGestureTimer.Tick += (s, e) =>
            {
                CaptureHoldGesture();
                HoldGestureTimer.Stop(); // 타이머 중지
            };

            // 기본 도구 설정
            SwitchTool(QIDrawingToolType.Pen);
        }



        // 도구 전환
        public void SwitchTool(QIDrawingToolType toolType)
        {
            if (!_tools.ContainsKey(toolType))
                return;

            // 현재 도구의 설정 저장
            if (_activeTool != null)
            {
                _settingsStore[_activeToolType] = _activeTool.GetSettings();
            }

            // 새 도구로 전환
            _activeToolType = toolType;
            _activeTool = _tools[toolType];

            // 저장된 설정 적용
            if (_settingsStore.ContainsKey(toolType))
            {
                _activeTool.ApplySettings(_settingsStore[toolType]);
            }

            // 도구 변경 이벤트 발생
            ToolChanged?.Invoke(this, new ToolChangedEventArgs(_activeTool, toolType));
        }

        // 현재 도구 설정 업데이트
        public void UpdateToolSettings(QIDrawingToolSettings settings)
        {
            if (_activeTool != null)
            {
                _activeTool.ApplySettings(settings);

                // 설정 저장소에도 업데이트
                _settingsStore[_activeToolType] = settings.Clone();

                // 설정 변경 이벤트 발생
                ToolSettingsChanged?.Invoke(this, new ToolSettingsChangedEventArgs(settings));
            }
        }

        // 입력 처리
        public async Task HandleInput(QIDrawingInputData input)
        {
            CaptureGesture(input);
            if (_activeTool != null)
            {
                await _activeTool.HandleInput(input, Context);
            }
        }

        private void CaptureGesture(QIDrawingInputData input)
        {
            if (input.Type == QIDrawingInputType.Down)
            {
                _holdStartPoint = new SKPoint(input.X, input.Y);
                StartCapturingHoldGesture();
            }
            else if (input.Type == QIDrawingInputType.Move)
            {
                if (_isGestureCapturing)
                {
                    var currentPoint = new SKPoint(input.X, input.Y);
                    if (_holdStartPoint == null ||
                        (currentPoint - _holdStartPoint.Value).Length > 10)
                    {
                        StopCapturingHoldGesture();
                    }
                }
            }
            else if (input.Type == QIDrawingInputType.Up)
            {
                if (_isGestureCapturing)
                {
                    StopCapturingHoldGesture();
                }
            }
        }

        private void StartCapturingHoldGesture()
        {
            if (!_isGestureCapturing)
            {
                _isGestureCapturing = true;
                GestureCapturingStarted?.Invoke(this, EventArgs.Empty);
                HoldGestureTimer.Start();
            }
        }

        private void CaptureHoldGesture()
        {
            if (_isGestureCapturing)
            {
                _isGestureCapturing = false;
                CancelDrawing();
                GestureCapturingEnded?.Invoke(this, new GestureCapturedEventArgs(QIDrawingGesture.Hold));
            }
        }

        private void StopCapturingHoldGesture()
        {
            if (_isGestureCapturing)
            {
                _isGestureCapturing = false;
                HoldGestureTimer.Stop();
                GestureCapturingEnded?.Invoke(this, new GestureCapturedEventArgs(QIDrawingGesture.None)); // restore event for gesture captured
            }
        }

        private void CancelDrawing()
        {
            if (_activeTool != null)
            {
                _activeTool.CancelDrawing(Context);
            }
        }

        public QIDrawingToolSettings? GetCurrentSettings()
        {
            return _activeTool?.GetSettings();
        }

        // 이벤트
        public event EventHandler<ToolChangedEventArgs> ToolChanged;
        public event EventHandler<ToolSettingsChangedEventArgs> ToolSettingsChanged;
        public event EventHandler<EventArgs> GestureCapturingStarted;
        public event EventHandler<GestureCapturedEventArgs> GestureCapturingEnded;
    }


    // 이벤트 인자 클래스
    public class ToolChangedEventArgs : EventArgs
    {
        public IQIDrawingTool Tool { get; }
        public QIDrawingToolType ToolType { get; }

        public ToolChangedEventArgs(IQIDrawingTool tool, QIDrawingToolType toolType)
        {
            Tool = tool;
            ToolType = toolType;
        }
    }

    public class ToolSettingsChangedEventArgs : EventArgs
    {
        public QIDrawingToolSettings Settings { get; }

        public ToolSettingsChangedEventArgs(QIDrawingToolSettings settings)
        {
            Settings = settings;
        }
    }

    public enum QIDrawingGesture
    {
        None,
        Hold,
    }

    public class GestureCapturedEventArgs : EventArgs
    {
        public QIDrawingGesture Gesture { get; }
        public GestureCapturedEventArgs(QIDrawingGesture gesture)
        {
            Gesture = gesture;
        }
    }
}
