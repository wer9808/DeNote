using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using DeNote.Models;
using DeNote.Models.Drawing;

namespace DeNote.Services
{

    public class QIDrawingCommandManager
    {
        private readonly LinkedList<QIDrawingCommand> undoList = new LinkedList<QIDrawingCommand>();
        private readonly LinkedList<QIDrawingCommand> redoList = new LinkedList<QIDrawingCommand>();
        private readonly int maxStackSize = 30;

        private readonly QIDrawingContext context;

        public QIDrawingCommandManager(QIDrawingContext context)
        {
            this.context = context;
        }

        // 명령 실행
        public void ExecuteCommand(QIDrawingCommand command)
        {
            command.Execute();
            // 스택 크기 제한 적용
            if (undoList.Count >= maxStackSize)
            {
                // 맨 처음(가장 오래된) 명령 제거
                undoList.RemoveFirst();
            }

            undoList.AddLast(command);
            redoList.Clear(); // 새 명령이 실행되면 redo 스택은 초기화
            CommandExecuted?.Invoke(this, EventArgs.Empty);
        }

        // Undo 실행
        public bool CanUndo => undoList.Count > 0;

        public void Undo()
        {
            if (!CanUndo) return;

            var command = undoList.Last.Value;
            undoList.RemoveLast();

            command.Undo();
            redoList.AddLast(command);

            CommandUndone?.Invoke(this, EventArgs.Empty);
            context.InvalidateVisual();
        }

        // Redo 실행
        public bool CanRedo => redoList.Count > 0;
        public void Redo()
        {
            if (!CanRedo) return;

            var command = redoList.Last.Value;
            redoList.RemoveLast();

            command.Execute();
            undoList.AddLast(command);

            CommandRedone?.Invoke(this, EventArgs.Empty);
            context.InvalidateVisual();
        }

        // 이벤트
        public event EventHandler CommandExecuted;
        public event EventHandler CommandUndone;
        public event EventHandler CommandRedone;

        // 특정 타입의 객체만 처리하는 팩토리 메서드
        public void AddObject(QIDrawingObject obj)
        {
            ExecuteCommand(new AddDrawingObjectCommand(context, obj));
        }

        public void RemoveObject(QIDrawingObject obj)
        {
            ExecuteCommand(new RemoveDrawingObjectCommand(context, obj));
        }

        // 복합 명령 생성을 위한 메서드
        public CompositeCommand CreateCompositeCommand()
        {
            return new CompositeCommand(context);
        }

        // 모든 명령 스택 초기화
        public void Clear()
        {
            undoList.Clear();
            redoList.Clear();
            CommandExecuted?.Invoke(this, EventArgs.Empty);
        }
    }
}
