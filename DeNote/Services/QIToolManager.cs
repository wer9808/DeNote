using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using DeNote.Models.DeNote.Models;
using DeNote.Models.Drawing;

namespace DeNote.Services
{
    public class QIToolManager
    {

        private Dictionary<QIDrawingToolType, IQIDrawingTool> _tools = new Dictionary<QIDrawingToolType, IQIDrawingTool>();
        private Dictionary<QIDrawingToolType, QIDrawingToolSettings> _settingsStore = new Dictionary<QIDrawingToolType, QIDrawingToolSettings>();

        private IQIDrawingTool _activeTool = null;
        public IQIDrawingTool ActiveTool => _activeTool;

        private QIDrawingToolType _activeToolType = QIDrawingToolType.Pen;
        public QIDrawingToolType ActiveToolType
        {
            get => _activeToolType;
            set => SwitchTool(value);
        }
        // 입력 데이터 컨텍스트
        public QIDrawingContext Context { get; set; }

        public QIToolManager(QIDrawingContext context)
        {
            Context = context;

            // 도구 초기화
            _tools[QIDrawingToolType.Pen] = new QIPenTool();
            _tools[QIDrawingToolType.Highlighter] = new QIHighlighterTool();
            _tools[QIDrawingToolType.Shape] = new QIShapeTool();
            // 기타 도구...

            // 초기 도구 설정
            _settingsStore[QIDrawingToolType.Pen] = new QIPenToolSettings();
            _settingsStore[QIDrawingToolType.Highlighter] = new QIHighlighterToolSettings();
            _settingsStore[QIDrawingToolType.Shape] = new QIShapeToolSettings();

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
        public void HandleInput(QIDrawingInputData input)
        {
            if (_activeTool != null)
            {
                _activeTool.HandleInput(input, Context);
            }
        }

        internal QIDrawingToolSettings GetCurrentSettings()
        {
            return _activeTool.GetSettings();
        }

        // 이벤트
        public event EventHandler<ToolChangedEventArgs> ToolChanged;
        public event EventHandler<ToolSettingsChangedEventArgs> ToolSettingsChanged;
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
}
