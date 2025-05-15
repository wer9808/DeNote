using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeNote.Services;

namespace DeNote.Models.Drawing
{
    public class QIDrawingContext
    {
        // 드로잉 객체 컬렉션

        // 명령 관리자 추가
        public QIDrawingCommandManager CommandManager { get; }
        public ObservableCollection<QIDrawingObject> Objects { get; } = new ObservableCollection<QIDrawingObject>();

        // 현재 활성 객체 (생성/편집 중)
        private QIDrawingObject activeObject;
        public QIDrawingObject ActiveObject
        {
            get => activeObject;
            set
            {
                activeObject = value;
                ActiveObjectChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public QIDrawingContext()
        {
            CommandManager = new QIDrawingCommandManager(this);
            CommandManager.CommandExecuted += (s, e) => InvalidateVisual();
        }

        public void AddDrawingObject(QIDrawingObject obj)
        {
            CommandManager.AddObject(obj);
            ObjectAdded?.Invoke(this, new DrawingObjectEventArgs(obj));
        }

        public void ClearObjects()
        {
            var clearCommand = new ClearDrawingObjectsCommand(this);
            CommandManager.ExecuteCommand(clearCommand);
        }

        // 화면 갱신 요청
        public void InvalidateVisual()
        {
            VisualInvalidated?.Invoke(this, EventArgs.Empty);
        }

        // 이벤트
        public event EventHandler<DrawingObjectEventArgs> ObjectAdded;
        public event EventHandler ActiveObjectChanged;
        public event EventHandler VisualInvalidated;
    }

    public class DrawingObjectEventArgs : EventArgs
    {
        public QIDrawingObject Object { get; }

        public DrawingObjectEventArgs(QIDrawingObject obj)
        {
            Object = obj;
        }
    }
}
