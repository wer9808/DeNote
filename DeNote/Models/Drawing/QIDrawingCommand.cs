using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace DeNote.Models.Drawing
{
    public interface QIDrawingCommand
    {
        void Execute();
        void Undo();
    }

    public class AddDrawingObjectCommand : QIDrawingCommand
    {
        private readonly QIDrawingContext context;
        private readonly QIDrawingObject drawingObject;

        public AddDrawingObjectCommand(QIDrawingContext context, QIDrawingObject drawingObject)
        {
            this.context = context;
            this.drawingObject = drawingObject;
        }

        public void Execute()
        {
            context.Objects.Add(drawingObject);
        }

        public void Undo()
        {
            context.Objects.Remove(drawingObject);
        }
    }

    public class RemoveDrawingObjectCommand : QIDrawingCommand
    {
        private readonly QIDrawingContext context;
        private readonly QIDrawingObject drawingObject;
        private readonly int originalIndex;

        public RemoveDrawingObjectCommand(QIDrawingContext context, QIDrawingObject drawingObject)
        {
            this.context = context;
            this.drawingObject = drawingObject;
            this.originalIndex = context.Objects.IndexOf(drawingObject);
        }

        public void Execute()
        {
            context.Objects.Remove(drawingObject);
        }

        public void Undo()
        {
            if (originalIndex >= 0 && originalIndex <= context.Objects.Count)
                context.Objects.Insert(originalIndex, drawingObject);
            else
                context.Objects.Add(drawingObject);
        }
    }

    public class ClearDrawingObjectsCommand : QIDrawingCommand
    {
        private readonly QIDrawingContext context;
        private List<QIDrawingObject> removedObjects;

        public ClearDrawingObjectsCommand(QIDrawingContext context)
        {
            this.context = context;
        }

        public void Execute()
        {
            // 제거되기 전에 현재 객체들의 복사본을 저장
            removedObjects = context.Objects.ToList();

            // 모든 객체 제거
            context.Objects.Clear();
        }

        public void Undo()
        {
                   // 제거되었던 모든 객체를 다시 추가
            foreach (var obj in removedObjects)
            {
                context.Objects.Add(obj);
            }
        }
    }

    public class EraseCommand : QIDrawingCommand
    {

        private readonly QIDrawingContext context;
        private QIDrawingObject original;
        private int originalIndex;
        private QIDrawingObject target;
        private SKPath eraserPath;

        public EraseCommand(QIDrawingContext context, QIDrawingObject original, SKPath eraserPath)
        {
            this.context = context;
            this.original = original;
            this.eraserPath = eraserPath;
        }

        public void Execute()
        {
            var sameObject = context.Objects.FirstOrDefault(o => o.Id == original.Id);
            if (sameObject != null)
            {
                original = sameObject;
            }
            this.originalIndex = context.Objects.IndexOf(original);
            this.target = original.Clone();
            target.Erase(eraserPath);
            if (target.IsEmpty)
            {
                context.Objects.Remove(original);
            }
            else
            {
                context.Objects[originalIndex] = target;
            }
        }

        public void Undo()
        {
            var targetIndex = context.Objects.IndexOf(target);
            if (targetIndex >= 0)
            {
                context.Objects[targetIndex] = original;
                originalIndex = targetIndex;
            }
            else
            {
                context.Objects.Insert(originalIndex, original);
            }
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
            AddCommand(new AddDrawingObjectCommand(context, obj));
        }

        public void RemoveObject(QIDrawingObject obj)
        {
            AddCommand(new RemoveDrawingObjectCommand(context, obj));
        }
    }

}
