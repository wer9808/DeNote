using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeNote.Services;
using SkiaSharp;
using RBush;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeNote.Models.Drawing
{
    public class QIDrawingContext: ObservableObject
    {
        // 드로잉 객체 컬렉션

        // 명령 관리자 추가
        public QIDrawingCommandManager CommandManager { get; }
        public bool CanExecute => CommandManager.CanExecute;

        public ObservableCollection<QIDrawingObject> Objects { get; } = new ObservableCollection<QIDrawingObject>();
        public RBush<QIDrawingObject> SpatialIndex;

        public bool IsDrawing { get; set; } = false;
        public bool IsErasing { get; set; } = false;
        public SKBitmap EraserBitmap { get; set; }
        private SKPath eraserPath;
        private SKPaint eraserPaint;
        private float eraserWidth;

        private float canvasWidth;
        private float canvasHeight;

        // 현재 활성 객체 (생성/편집 중)
        private QIDrawingObject? activeObject;
        public QIDrawingObject? ActiveObject
        {
            get => activeObject;
            set
            {
                activeObject = value;
                ActiveObjectChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public QIDrawingContext(float canvasWidth = 3840, float canvasHeight = 2160)
        {
            this.canvasWidth = canvasWidth;
            this.canvasHeight = canvasHeight;
            SpatialIndex = new RBush<QIDrawingObject>();

            CommandManager = new QIDrawingCommandManager(this);
            CommandManager.CommandExecuted += (s, e) => InvalidateVisual();
            CommandManager.CommandUndone += (s, e) => InvalidateVisual();
            CommandManager.CommandRedone += (s, e) => InvalidateVisual();
            CommandManager.CanExecuteChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanExecute));
            };

            this.ObjectAdded += OnObjectAdded;
            this.ObjectRemoved += OnObjectRemoved;
            Objects.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    if (e.NewItems == null) return;
                    foreach (QIDrawingObject obj in e.NewItems)
                    {
                        ObjectAdded.Invoke(this, new DrawingObjectEventArgs(obj));
                    }
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldItems == null) return;
                    foreach (QIDrawingObject obj in e.OldItems)
                    {
                        ObjectRemoved.Invoke(this, new DrawingObjectEventArgs(obj));
                    }
                }
            };
        }

        private void OnObjectAdded(object? sender, DrawingObjectEventArgs e)
        {
            // 객체가 추가될 때 QuadTree에 추가
            var newObject = e.Object;
            SpatialIndex.Insert(newObject);
            InvalidateVisual();
        }

        private void OnObjectRemoved(object? sender, DrawingObjectEventArgs e)
        {
            // 객체가 제거될 때 QuadTree에서 제거
            var removedObject = e.Object;
            SpatialIndex.Delete(removedObject);
            InvalidateVisual();
        }

        public void UpdateIndex(QIDrawingObject obj)
        {
            SpatialIndex.Delete(obj);
            SpatialIndex.Insert(obj);
        }

        public async Task AddDrawingObject(QIDrawingObject obj)
        {
            var addCommand = new AddDrawingCommand(this, obj);
            await CommandManager.Execute(addCommand);
        }

        public async Task ClearObjects()
        {
            var clearCommand = new ClearCommand(this);
            await CommandManager.Execute(clearCommand);
        }

        // 화면 갱신 요청
        public void InvalidateVisual()
        {
            VisualInvalidated?.Invoke(this, EventArgs.Empty);
        }

        internal void StartErasing(QIPoint point, float eraserWidth)
        {
            CacheBitmapBeforeErasing();
            IsErasing = true;
            // 지우개용 페인트 설정
            this.eraserWidth = eraserWidth;
            eraserPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
                StrokeWidth = this.eraserWidth,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                BlendMode = SKBlendMode.Clear, // 중요: 투명하게 지우는 모드
                IsAntialias = true
            };
            eraserPath = new SKPath();
            eraserPath.MoveTo(point.X, point.Y);
        }

        private void CacheBitmapBeforeErasing()
        {
            var bitmap = new SKBitmap((int)canvasWidth, (int)canvasHeight);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear();

                foreach (var obj in Objects)
                {
                    obj.Render(canvas);
                }
            }

            EraserBitmap = bitmap;
        }

        public void UpdateErasing(QIPoint point)
        {
            if (IsErasing)
            {
                eraserPath.LineTo(point.X, point.Y);
                using (var canvas = new SKCanvas(EraserBitmap))
                {
                    canvas.DrawPath(eraserPath, eraserPaint);
                }
                InvalidateVisual();
            }
        }

        public async Task EndErasing()
        {
            if (IsErasing)
            {
                await EraseActualObjects().ContinueWith(x =>
                {
                    IsErasing = false;
                    EraserBitmap.Dispose();
                    InvalidateVisual();
                });
            }
        }

        private async Task EraseActualObjects()
        {
            var actualEraserPath = eraserPaint.GetFillPath(eraserPath);
            var eraserBounds = actualEraserPath.ComputeTightBounds();

            // QuadTree를 사용하여 교차하는 객체 찾기
            // var intersectingObjects = QuadTree.Retrieve(Objects.ToList(), eraserBounds);

            var eraseCommand = new EraseCommand(this, actualEraserPath);
            await CommandManager.Execute(eraseCommand);
        }

        // 이벤트
        public event EventHandler<DrawingObjectEventArgs> ObjectAdded;
        public event EventHandler<DrawingObjectEventArgs> ObjectRemoved;

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
