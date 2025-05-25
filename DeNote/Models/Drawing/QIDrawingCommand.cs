using RBush;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DeNote.Models.Drawing
{
    public interface QIDrawingCommand
    {
        void Execute();
        void Undo();
    }

    public class AddDrawingCommand : QIDrawingCommand
    {
        private readonly QIDrawingContext _context;
        private readonly QIDrawingObject _drawingObject;

        public AddDrawingCommand(QIDrawingContext context, QIDrawingObject drawingObject)
        {
            this._context = context;
            this._drawingObject = drawingObject;
        }

        public void Execute()
        {
            _context.Objects.Add(_drawingObject);
        }

        public void Undo()
        {
            _context.Objects.Remove(_drawingObject);
        }
    }

    public class ClearCommand : QIDrawingCommand
    {
        private readonly QIDrawingContext _context;
        private List<QIDrawingObject> _removedObjects;

        public ClearCommand(QIDrawingContext context)
        {
            this._context = context;
        }

        public void Execute()
        {
            // 제거되기 전에 현재 객체들의 복사본을 저장
            _removedObjects = _context.Objects.ToList();

            // 모든 객체 제거
            _context.Objects.Clear();
        }

        public void Undo()
        {
            // 제거되었던 모든 객체를 다시 추가
            foreach (var obj in _removedObjects)
            {
                _context.Objects.Add(obj);
            }
        }
    }

    public class EraseCommand : QIDrawingCommand
    {
        private readonly QIDrawingContext context;
        private ObservableCollection<QIDrawingObject> _drawingObjects => context.Objects;
        private readonly SKPath _eraserPath;

        private readonly Dispatcher _dispatcher; // UI 스레드 접근을 위한 Dispatcher

        public class EraseOperationResult : IDisposable
        {
            public QIDrawingObject TargetObject { get; }
            public SKPath OldPath { get; } // 이 Path는 Dispose() 되어야 함
            public SKPath NewPath { get; } // 이 Path는 Dispose() 되어야 함
            public int OriginalIndex { get; }
            public bool IsCompletelyErased => NewPath.IsEmpty;

            public EraseOperationResult(QIDrawingObject target, SKPath oldPath, SKPath newPath, int originalIndex)
            {
                TargetObject = target;
                OldPath = oldPath; // 생성된 Path를 받아서 보관
                NewPath = newPath; // 생성된 Path를 받아서 보관
                OriginalIndex = originalIndex;
            }

            // IDisposable 구현
            public void Dispose()
            {
                // 내부 SKPath 객체들을 안전하게 Dispose()
                OldPath?.Dispose();
                NewPath?.Dispose();
            }
        }

        // Command 실행 결과 (Undo를 위한)
        private readonly List<EraseOperationResult> _lastExecutedResults;

        public EraseCommand(QIDrawingContext context, SKPath eraserPath)
        {
            this.context = context;
            _eraserPath = eraserPath;
            _dispatcher = Dispatcher.CurrentDispatcher;
            _lastExecutedResults = new List<EraseOperationResult>();
        }

        public void Execute()
        {
            Task.Run(ExecuteAsync);
        }

        public async Task ExecuteAsync()
        {
            _lastExecutedResults.Clear(); // 새로운 Execute를 위해 이전 결과 클리어

            // 1. 지우개 Path의 Bounding Box를 사용하여 R-tree에서 겹치는 객체들만 쿼리
            SKRect eraserBounds = _eraserPath.Bounds;
            var queryEnvelope = new Envelope(eraserBounds.Left, eraserBounds.Top, eraserBounds.Right, eraserBounds.Bottom);
            IEnumerable<QIDrawingObject> potentialTargets = context.SpatialIndex.Search(queryEnvelope);

            // R-tree는 렌더링 순서를 보장하지 않으므로, _drawingObjects의 원래 순서를 기준으로 정렬하여 처리합니다.
            // 역순으로 처리해야 컬렉션에서 제거 시 인덱스 문제가 발생하지 않습니다.
            var sortedTargets = potentialTargets
                .Where(obj => _drawingObjects.Contains(obj)) // 아직 리스트에 있는 객체만 처리
                .Select(obj => new { Obj = obj, OriginalIndex = _drawingObjects.IndexOf(obj) })
                .OrderByDescending(x => x.OriginalIndex)
                .ToList();

            // UI 스레드에서 Path.Op 결과들을 저장할 임시 리스트
            var resultsToApply = new List<EraseOperationResult>();

            List<Task> opTasks = new List<Task>();

            // `lock` 객체는 비동기 작업 결과를 저장할 리스트 접근 시 사용
            object lockObject = new object();

            foreach (var item in sortedTargets)
            {
                var drawingObject = item.Obj;
                int originalIndex = item.OriginalIndex;

                Task opTask = Task.Run(() =>
                {
                    // **중요: 백그라운드 스레드에서 생성되는 모든 SKPath는 여기서 Dispose를 관리해야 합니다.**
                    using (SKPath currentPathCopy = new SKPath(drawingObject.Path)) // 복사본 생성 후 using
                    using (SKPath eraserPathCopy = new SKPath(_eraserPath))       // 복사본 생성 후 using
                    {
                        // 1단계: Intersection Op으로 실제 겹침 여부 확인
                        using (var intersectionResultPath = new SKPath())
                        {
                            if (!currentPathCopy.Op(eraserPathCopy, SKPathOp.Intersect, intersectionResultPath) || intersectionResultPath.IsEmpty)
                            {
                                return; // 겹치지 않으면 종료
                            }
                        }

                        // 2단계: Difference Op 연산 수행
                        // oldPath는 여기서 생성하고, EraseOperationResult에 전달 후 Dispose() 책임은 EraseOperationResult가 가짐
                        SKPath oldPathForResult = new SKPath(currentPathCopy);

                        using (var tempNewPath = new SKPath()) // Op 연산 결과로 받을 임시 Path
                        {
                            if (currentPathCopy.Op(eraserPathCopy, SKPathOp.Difference, tempNewPath))
                            {
                                // EraseOperationResult에 저장할 newPath는 tempNewPath의 복사본이어야 합니다.
                                // 그래야 tempNewPath가 using 블록을 벗어나도 EraseOperationResult가 유효한 Path를 가집니다.
                                SKPath newPathForResult = new SKPath(tempNewPath); // 복사본 생성

                                lock (lockObject)
                                {
                                    resultsToApply.Add(new EraseOperationResult(drawingObject, oldPathForResult, newPathForResult, originalIndex));
                                }
                            }
                            else
                            {
                                // Op 연산 실패 시, 생성된 oldPathForResult도 여기서 Dispose()
                                oldPathForResult?.Dispose();
                            }
                        }
                    } // currentPathCopy, eraserPathCopy는 여기서 Dispose됨
                });

                opTasks.Add(opTask);
            }

            await Task.WhenAll(opTasks);

            _dispatcher.Invoke(() =>
            {
                // 모든 연산 결과를 _lastExecutedResults에 저장 (Undo/Redo를 위한)
                _lastExecutedResults.AddRange(resultsToApply.OrderByDescending(r => r.OriginalIndex));

                // _drawingObjects 컬렉션에 변경 사항 반영
                foreach (var result in _lastExecutedResults) // 이미 역순으로 정렬됨
                {
                    if (result.IsCompletelyErased)
                    {
                        _drawingObjects.RemoveAt(result.OriginalIndex);
                    }
                    else
                    {
                        result.TargetObject.UpdatePath(result.NewPath); // NewPath는 이제 EraseOperationResult가 소유
                        context.UpdateIndex(result.TargetObject);
                    }
                }

                context.InvalidateVisual();
            });
        }

        public void Undo()
        {
            _dispatcher.Invoke(() =>
            {
                // Path가 변경된 객체부터 복원 (이전 상태로 되돌림)
                foreach (var result in _lastExecutedResults.Where(r => !r.IsCompletelyErased).Reverse())
                {
                    result.TargetObject.UpdatePath(result.OldPath); // OldPath는 이제 EraseOperationResult가 소유
                    context.UpdateIndex(result.TargetObject);
                }

                // 완전히 제거된 객체들을 복원 (원래 인덱스 오름차순으로 삽입)
                foreach (var result in _lastExecutedResults.Where(r => r.IsCompletelyErased).OrderBy(r => r.OriginalIndex))
                {
                    result.TargetObject.UpdatePath(result.OldPath); // OldPath는 이제 EraseOperationResult가 소유
                    _drawingObjects.Insert(result.OriginalIndex, result.TargetObject);
                }

                // Undo 완료 후, _lastExecutedResults에 저장된 SKPath 객체들을 Dispose
                // (실제 Undo/Redo 스택에서는 이 명령 객체가 Redo 스택으로 이동하면 Dispose하지 않고,
                // 완전히 스택에서 벗어날 때 Dispose하는 로직이 필요함)
                foreach (var result in _lastExecutedResults)
                {
                    result.Dispose();
                }
                _lastExecutedResults.Clear();
            });
        }


    }

    // 복합 명령 (여러 명령을 하나로 묶음)
    public class CompositeCommand : QIDrawingCommand
    {
        private readonly List<QIDrawingCommand> commands = new List<QIDrawingCommand>();
        private readonly QIDrawingContext context;
        public int Count => commands.Count;

        public CompositeCommand(QIDrawingContext context)
        {
            this.context = context;
        }

        public void AddCommand(QIDrawingCommand command)
        {
            commands.Add(command);
        }

        public void Execute()
        {
            foreach (var command in commands)
            {
                command.Execute();
            }
        }

        public void Undo()
        {
            // 역순으로 Undo 수행
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }

        // 추가 편의 메서드
        public void AddObject(QIDrawingObject obj)
        {
            AddCommand(new AddDrawingCommand(context, obj));
        }
    }

}
