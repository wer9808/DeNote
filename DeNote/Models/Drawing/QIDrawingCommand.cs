using RBush;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // 완전히 지워진 객체: (지워진 객체, 지워지기 전 Path, 컬렉션 내 원래 인덱스)
        // 이제 originalPath는 지워지기 전의 최종 Path가 됩니다.
        private readonly List<(QIDrawingObject obj, SKPath oldPath, int originalIndex)> _completelyRemovedObjects;

        // Path가 변경된 객체: (변경될 객체, 변경 전 Path)
        private readonly List<(QIDrawingObject obj, SKPath oldPath)> _partiallyModifiedObjects;

        public EraseCommand(QIDrawingContext context, SKPath eraserPath)
        {
            this.context = context;
            _eraserPath = eraserPath;
            _completelyRemovedObjects = new List<(QIDrawingObject obj, SKPath oldPath, int originalIndex)>();
            _partiallyModifiedObjects = new List<(QIDrawingObject obj, SKPath oldPath)>();
        }

        public void Execute()
        {
            _completelyRemovedObjects.Clear();
            _partiallyModifiedObjects.Clear();

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

            foreach (var item in sortedTargets)
            {
                var drawingObject = item.Obj;
                int originalIndex = item.OriginalIndex;

                // 지우개 Path와 DrawingObject의 Path가 겹치는지 다시 한번 정확히 확인 (Bounding Box 외에 실제 Path 겹침)
                // if (drawingObject.Path.Bounds.IntersectsWith(_eraserPath.Bounds)) // 이 검사는 R-tree에서 이미 걸러졌지만, 더 정확한 교차 확인을 위해 유지
                {
                    SKPath oldPath = new SKPath(drawingObject.Path);
                    var newPath = new SKPath();

                    if (drawingObject.Path.Op(_eraserPath, SKPathOp.Difference, newPath))
                    {
                        if (newPath.IsEmpty)
                        {
                            // 완전히 지워진 경우: 리스트에서 제거 및 R-tree에서도 제거
                            _completelyRemovedObjects.Add((drawingObject, oldPath, originalIndex));
                            _drawingObjects.RemoveAt(originalIndex); // ObservableCollection에서 제거 -> CollectionChanged 이벤트 발생 -> R-tree에서도 제거
                                                                     // _spatialIndex.Delete(drawingObject); // CollectionChanged 이벤트 핸들러에서 처리되므로 여기서는 불필요
                        }
                        else
                        {
                            // 부분적으로 지워진 경우: Path 업데이트 및 R-tree 업데이트
                            _partiallyModifiedObjects.Add((drawingObject, oldPath));
                            drawingObject.UpdatePath(newPath);
                            context.UpdateIndex(drawingObject); // R-tree에서 업데이트
                        }
                    }
                }
            }
            context.InvalidateVisual(); // UI 갱신
        }

        public void Undo()
        {
            // 1. 부분적으로 지워졌던 객체 복원 (최근 변경된 객체부터)
            foreach (var item in _partiallyModifiedObjects.AsEnumerable().Reverse())
            {
                item.obj.UpdatePath(item.oldPath); // 이전 Path로 복원
                context.UpdateIndex(item.obj);
            }
            _partiallyModifiedObjects.Clear();

            // 2. 완전히 지워졌던 객체 복원 (원래 인덱스 순서대로)
            foreach (var item in _completelyRemovedObjects.OrderBy(x => x.originalIndex))
            {
                item.obj.UpdatePath(item.oldPath); // 지워지기 전의 Path로 복원
                _drawingObjects.Insert(item.originalIndex, item.obj); // 리스트에 다시 삽입 -> CollectionChanged 이벤트 발생 -> R-tree에도 추가
                                                                      // _spatialIndex.Insert(item.obj); // CollectionChanged 이벤트 핸들러에서 처리되므로 여기서는 불필요
            }
            _completelyRemovedObjects.Clear();

            context.InvalidateVisual(); // UI 갱신
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
