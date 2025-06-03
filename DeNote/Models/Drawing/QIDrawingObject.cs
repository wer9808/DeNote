using DeNote.Utils;
using RBush;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static OpenTK.Graphics.OpenGL.GL;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace DeNote.Models.Drawing
{

    public abstract class QIDrawingObject : ISpatialData, IDisposable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the object
        public DateTime CreationTime { get; set; } = DateTime.Now; // Timestamp of when the object was created
        public DateTime LastModifiedTime { get; set; } = DateTime.Now; // Timestamp of the last modification

        public Point Position { get; set; } = new Point(0, 0); // Position of the object on the canvas
        public double Rotation { get; set; } = 0; // Rotation angle of the object in degrees
        public Vector Scale { get; set; } = new Vector(1, 1); // Scale factor for the object (X and Y scale)

        public bool IsDirty { get; set; } = true; // Indicates if the object has been modified since last render
        public bool IsPathCached { get; set; } = false; // Indicates if the path is cached for performance optimization
        public SKPath Path { get; set; } = new SKPath(); // Path for the object (used for drawing shapes)
        public SKRect Bounds => Path.ComputeTightBounds(); // Bounding box of the object

        public SKColor StrokeColor { get; set; } = SKColors.Black; // Stroke color for the object

        public bool BitmapCacheOption { get; set; } = false; // Indicates if the object should be cached for performance optimization
        public SKBitmap CachedBitmap { get; set; }
        public virtual float BitmapPadding { get => 2.0f; }
        public SKRect CacheBounds { get => new SKRect(Bounds.Left - BitmapPadding, Bounds.Top - BitmapPadding, Bounds.Right + BitmapPadding, Bounds.Bottom + BitmapPadding); }

        public abstract void Render(SKCanvas canvas); // Method to render the object on the canvas
        public abstract QIDrawingObject Clone(); // Method to create a copy of the object

        public virtual void Erase(SKPath eraserPath)
        {
            Path = Path.Op(eraserPath, SKPathOp.Difference);
        }

        public void UpdateEnvelope()
        {
            this.envelope = new Envelope(Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Bottom);
        }

        public virtual void UpdatePath(SKPath path)
        {
            Path = new SKPath(path);
            UpdateEnvelope();
            IsDirty = true;
        }

        public void Dispose()
        {
            Path?.Dispose();
            CachedBitmap?.Dispose();
        }

        public virtual bool IsEmpty => Path == null || Path.IsEmpty;
        protected Envelope envelope = new Envelope(0, 0, 0, 0);

        public ref readonly Envelope Envelope => ref envelope; // Spatial data envelope for the object
    }

    public class QIPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Pressure { get; set; } = 1.0f;

        public QIPoint() { }
        public QIPoint(float x, float y, float pressure = 1.0f)
        {
            X = x;
            Y = y;
            Pressure = pressure;
        }

        public QIPoint(StylusPoint stylusPoint)
        {
            X = (float)stylusPoint.X;
            Y = (float)stylusPoint.Y;
            Pressure = stylusPoint.PressureFactor;
        }

        public double DistanceTo(QIPoint other)
        {
            return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
        }

        public static double Distance(QIPoint p1, QIPoint p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static double Distance(float x1, float y1, float x2, float y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

    }

    public abstract class QIStrokeObject : QIDrawingObject
    {
        public float StrokeWidth { get; set; } = 2.0f; // Width of the stroke
        public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round; // Cap style for the stroke (e.g., round, square, butt)
        public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round; // Join style for the stroke (e.g., round, bevel, miter)
        public List<QIPoint> Points { get; set; } = []; // List of points that make up the stroke
        public override float BitmapPadding => StrokeWidth + base.BitmapPadding;
        public bool PressureOption { get; set; } = false;
    }

    public class QIPenStroke : QIStrokeObject
    {
        public QIPenStroke()
        {
            StrokeCap = SKStrokeCap.Round;
            StrokeJoin = SKStrokeJoin.Round;
            StrokeWidth = 4.0f;
            BitmapCacheOption = false;
        }

        public override void Render(SKCanvas canvas)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = StrokeColor;
                paint.StrokeWidth = StrokeWidth;
                paint.StrokeCap = StrokeCap;
                paint.StrokeJoin = StrokeJoin;
                paint.Style = SKPaintStyle.Fill;
                paint.IsAntialias = true;
                canvas.DrawPath(Path, paint);
            }
        }

        public void AddPoint(QIPoint point)
        {
            if (Points.Count == 0)
            {
                Points.Add(point);
                var pointMesh = CreatePointMesh(point);
                Path.AddPath(pointMesh);
            }
            else
            {
                var lastPoint = Points.Last();
                if (lastPoint.DistanceTo(point) < 0.01f)
                    return; // 너무 가까운 점은 무시
                var connectionMesh = CreateConnectionMesh(lastPoint, point);
                Path.AddPath(connectionMesh);
                var pointMesh = CreatePointMesh(point);
                Points.Add(point);
                Path.AddPath(pointMesh);
            }
        }

        private SKPath CreateConnectionMesh(QIPoint p1, QIPoint p2)
        {
            // 필압에 기반한 두께 계산
            float width1 = StrokeWidth * p1.Pressure;
            float width2 = StrokeWidth * p2.Pressure;

            // 두 점 사이의 거리 계산
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            // 두 점 사이의 단위 벡터 계산
            float nx = -dy / distance;
            float ny = dx / distance;

            // 두께에 따른 오프셋 계산
            float offset1 = width1 / 2;
            float offset2 = width2 / 2;

            // 사다리꼴의 네 꼭짓점 계산
            SKPoint v_p1_left = new SKPoint(p1.X + nx * offset1, p1.Y + ny * offset1);
            SKPoint v_p2_left = new SKPoint(p2.X + nx * offset2, p2.Y + ny * offset2);
            SKPoint v_p1_right = new SKPoint(p1.X - nx * offset1, p1.Y - ny * offset1);
            SKPoint v_p2_right = new SKPoint(p2.X - nx * offset2, p2.Y - ny * offset2);

            // 시계 방향 (CW)으로 경로 만들기 (Y-down 좌표계 기준)
            var path = new SKPath();
            path.MoveTo(v_p1_left);     // 시작점 (p1의 "왼쪽")
            path.LineTo(v_p1_right);    // p1의 "오른쪽"으로 이동
            path.LineTo(v_p2_right);    // p2의 "오른쪽"으로 이동
            path.LineTo(v_p2_left);     // p2의 "왼쪽"으로 이동
            path.Close();               // 경로 닫기 (v_p2_left에서 v_p1_left로)

            return path;
        }

        private SKPath CreatePointMesh(QIPoint point)
        {
            var path = new SKPath();

            float width = StrokeWidth * point.Pressure;
            var radius = width / 2;

            path.AddCircle(point.X, point.Y, radius);
            path.Close();
            return path;
        }

        public override void Erase(SKPath eraserPath)
        {
            base.Erase(eraserPath);
        }

        public override QIDrawingObject Clone()
        {
            return new QIPenStroke
            {
                Id = Id,
                CreationTime = CreationTime,
                LastModifiedTime = LastModifiedTime,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                IsPathCached = IsPathCached,
                Path = Path,
                StrokeColor = StrokeColor,
                BitmapCacheOption = BitmapCacheOption,
                CachedBitmap = CachedBitmap,
                StrokeWidth = StrokeWidth,
                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
                Points = new List<QIPoint>(Points),
                PressureOption = PressureOption,
                IsDirty = IsDirty,
            };
        }
    }

    public class QIHighlighter : QIStrokeObject
    {
        private const byte Alpha = 255;
        public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver; // Blend mode for the highlighter (e.g., normal, multiply, screen)
        private SKPath OriginalPath { get; set; } = new SKPath(); // Original path before any modifications

        public QIHighlighter()
        {
            StrokeCap = SKStrokeCap.Round;
            StrokeJoin = SKStrokeJoin.Round;
            StrokeWidth = 15.0f;
            PressureOption = false;
        }

        public override void Render(SKCanvas canvas)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = StrokeColor.WithAlpha(128);
                paint.StrokeWidth = StrokeWidth;
                paint.StrokeCap = StrokeCap;
                paint.StrokeJoin = StrokeJoin;
                paint.Style = SKPaintStyle.Fill;
                paint.BlendMode = BlendMode;
                paint.IsAntialias = true;
                canvas.DrawPath(Path, paint);
            }
        }

        public void AddPoint(QIPoint point)
        {
            if (Points.Count == 0)
            {
                Points.Add(point);
                OriginalPath.MoveTo(point.X, point.Y);
            }
            else
            {
                var lastPoint = Points.Last();
                if (lastPoint.DistanceTo(point) < 0.01f)
                    return; // 너무 가까운 점은 무시
                OriginalPath.LineTo(point.X, point.Y);
            }
            using var paint = new SKPaint
            {
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
            };
            Path = paint.GetFillPath(OriginalPath);
        }

        public override QIDrawingObject Clone()
        {
            return new QIHighlighter
            {
                Id = Id,
                CreationTime = CreationTime,
                LastModifiedTime = LastModifiedTime,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                IsPathCached = IsPathCached,
                Path = Path,
                StrokeColor = StrokeColor,
                BitmapCacheOption = BitmapCacheOption,
                CachedBitmap = CachedBitmap,
                StrokeWidth = StrokeWidth,
                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
                Points = new List<QIPoint>(Points),
                IsDirty = IsDirty,
            };
        }
    }

    public enum QIShapeType
    {
        Rectangle,
        Ellipse,
        Triangle,
        Line,
        Polygon,
    }

    public class QIShape : QIDrawingObject
    {
        public QIShapeType ShapeType { get; set; } = QIShapeType.Rectangle; // Type of shape (e.g., "Rectangle", "Circle", etc.)
        public float StrokeWidth { get; set; } = 2.0f; // Width of the stroke
        public bool FillOption { get; set; } = false; // Indicates if the shape is filled or not
        public SKColor FillColor { get; set; } = SKColors.Transparent; // Fill color for the shape
        public override float BitmapPadding => StrokeWidth + base.BitmapPadding;

        public override void Render(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = StrokeColor,
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
                Style = FillOption ? SKPaintStyle.StrokeAndFill : SKPaintStyle.Stroke
            };
            canvas.DrawPath(Path, paint);
        }

        public override QIDrawingObject Clone()
        {
            return new QIShape
            {
                Id = Id,
                CreationTime = CreationTime,
                LastModifiedTime = LastModifiedTime,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                IsPathCached = IsPathCached,
                Path = Path,
                StrokeColor = StrokeColor,
                BitmapCacheOption = BitmapCacheOption,
                CachedBitmap = CachedBitmap,
                ShapeType = ShapeType,
                StrokeWidth = StrokeWidth,
                FillOption = FillOption,
                FillColor = FillColor
            };
        }

        public void UpdateShape(SKPath path)
        {
            using var paint = new SKPaint
            {
                Color = StrokeColor,
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
                Style = FillOption ? SKPaintStyle.StrokeAndFill : SKPaintStyle.Stroke
            };
            Path = paint.GetFillPath(path);
            UpdateEnvelope();
        }

    }

}
