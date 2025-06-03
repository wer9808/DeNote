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

        private bool _canExecute = true;
        public bool CanExecute => _canExecute;

        public QIDrawingCommandManager(QIDrawingContext context)
        {
            this.context = context;
        }

        // 명령 실행
        public async Task Execute(QIDrawingCommand command)
        {
            if (!CanExecute) return;
            await Task.Run(() => command.Execute(null));

            // 스택 크기 제한 적용
            if (undoList.Count >= maxStackSize)
            {
                // 맨 처음(가장 오래된) 명령 제거
                undoList.RemoveFirst();
            }

            undoList.AddLast(command);
            redoList.Clear(); // 새 명령이 실행되면 redo 스택은 초기화
        }

        // Undo 실행
        public bool CanUndo => undoList.Count > 0;

        public async Task Undo()
        {
            if (!CanExecute) return;
            if (!CanUndo) return;

            var command = undoList.Last!.Value;
            undoList.RemoveLast();

            await command.Undo();
            redoList.AddLast(command);

            CommandUndone?.Invoke(this, EventArgs.Empty);
        }

        // Redo 실행
        public bool CanRedo => redoList.Count > 0;
        public async Task Redo()
        {
            if (!CanExecute) return;
            if (!CanRedo) return;

            var command = redoList.Last!.Value;
            redoList.RemoveLast();

            await command.Redo();
            undoList.AddLast(command);

            CommandRedone?.Invoke(this, EventArgs.Empty);
        }

        // 이벤트
        public event EventHandler CommandExecuted;
        public event EventHandler CommandUndone;
        public event EventHandler CommandRedone;

        // UI에 CanUndo/CanRedo 상태 변경을 알리기 위한 이벤트
        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged(bool canExecute)
        {
            // CommandManager.InvalidateRequerySuggested(); 를 호출하여 WPF 바인딩 갱신
            _canExecute = canExecute;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty); // 직접 이벤트 발생 (옵션)
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
