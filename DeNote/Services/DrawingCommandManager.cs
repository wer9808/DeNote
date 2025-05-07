using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DeNote.Models;

namespace DeNote.Services
{
    public interface MemCommand
    {
        void Execute();
        void Undo();
    }

    public class AddDrawingObjectCommand : MemCommand
    {
        private readonly ObservableCollection<DrawingObject> _drawingObjects;
        private readonly DrawingObject _drawingObject;

        public AddDrawingObjectCommand(ObservableCollection<DrawingObject> drawingObjects, DrawingObject drawingObject)
        {
            _drawingObjects = drawingObjects;
            _drawingObject = drawingObject;
        }

        public void Execute()
        {
            _drawingObjects.Add(_drawingObject);
        }

        public void Undo()
        {
            _drawingObjects.Remove(_drawingObject);
        }
    }

    public class RemoveDrawingObjectCommand : MemCommand
    {
        private readonly ObservableCollection<DrawingObject> _drawingObjects;
        private readonly DrawingObject _drawingObject;
        private int _index;

        public RemoveDrawingObjectCommand(ObservableCollection<DrawingObject> drawingObjects, DrawingObject drawingObject)
        {
            _drawingObjects = drawingObjects;
            _drawingObject = drawingObject;
        }

        public void Execute()
        {
            _index = _drawingObjects.IndexOf(_drawingObject);
            if (_index != -1)
            {
                _drawingObjects.RemoveAt(_index);
            }
        }

        public void Undo()
        {
            if (_index != -1)
            {
                _drawingObjects.Insert(_index, _drawingObject);
            }
        }
    }

    public class ClearDrawingObjectsCommand : MemCommand
    {
        private readonly ObservableCollection<DrawingObject> _drawingObjects;
        private List<DrawingObject> _removedObjects;

        public ClearDrawingObjectsCommand(ObservableCollection<DrawingObject> drawingObjects)
        {
            _drawingObjects = drawingObjects;
        }

        public void Execute()
        {
            // 제거되기 전에 현재 객체들의 복사본을 저장
            _removedObjects = _drawingObjects.ToList();

            // 모든 객체 제거
            _drawingObjects.Clear();
        }

        public void Undo()
        {
            // 제거되었던 모든 객체를 다시 추가
            foreach (var obj in _removedObjects)
            {
                _drawingObjects.Add(obj);
            }
        }
    }

    public class DrawingCommandManager
    {
        private readonly Stack<MemCommand> _undoStack = new Stack<MemCommand>();
        private readonly Stack<MemCommand> _redoStack = new Stack<MemCommand>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void ExecuteCommand(MemCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // 새 명령을 실행하면 redo 스택은 지워집니다
        }

        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }

        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }
    }
}
