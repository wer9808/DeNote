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
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace DeNote.Models.Drawing
{

    public abstract class QIDrawingObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the object
        public DateTime CreationTime { get; set; } = DateTime.Now; // Timestamp of when the object was created
        public DateTime LastModifiedTime { get; set; } = DateTime.Now; // Timestamp of the last modification

        public Point Position { get; set; } = new Point(0, 0); // Position of the object on the canvas
        public double Rotation { get; set; } = 0; // Rotation angle of the object in degrees
        public Vector Scale { get; set; } = new Vector(1, 1); // Scale factor for the object (X and Y scale)

        public bool IsDirty { get; set; } = true; // Indicates if the object has been modified since last render
        public bool PathCacheOption { get; set; } = true; // Indicates if the path should be cached for performance optimization
        public SKPath Path { get; set; } = new SKPath(); // Path for the object (used for drawing shapes)
        public SKRect Bounds => Path.ComputeTightBounds(); // Bounding box of the object

        public SKColor StrokeColor { get; set; } = SKColors.Black; // Stroke color for the object

        public bool BitmapCacheOption { get; set; } = false; // Indicates if the object should be cached for performance optimization
        public SKBitmap CachedBitmap { get; set; }
        public virtual float BitmapPadding { get => 2.0f; }
        public SKRect CacheBounds { get => new SKRect(Bounds.Left - BitmapPadding, Bounds.Top - BitmapPadding, Bounds.Right + BitmapPadding, Bounds.Bottom + BitmapPadding); }

        public abstract void Render(SKCanvas canvas); // Method to render the object on the canvas
        public abstract QIDrawingObject Clone(); // Method to create a copy of the object
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
            PathCacheOption = true;
            BitmapCacheOption = true;
        }

        public override void Render(SKCanvas canvas)
        {
            if (PressureOption)
            {
                RenderVariableWidthPath(canvas);
            }
            else
            {
                if (PathCacheOption && Path.IsEmpty)
                {
                    Path = new SKPath();
                    Path.MoveTo((float)Position.X, (float)Position.Y);
                    foreach (var point in Points)
                    {
                        Path.LineTo((float)point.X, (float)point.Y);
                    }
                }

                using (var paint = new SKPaint())
                {
                    paint.Color = StrokeColor;
                    paint.StrokeWidth = StrokeWidth;
                    paint.StrokeCap = StrokeCap;
                    paint.StrokeJoin = StrokeJoin;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawPath(Path, paint);
                }
            }

        }

        // 필압을 고려한 가변 두께 경로 렌더링
        private void RenderVariableWidthPath(SKCanvas canvas)
        {
            if (Points.Count < 2)
                return;

            for (int i = 0; i < Points.Count - 1; i++)
            {
                var p1 = Points[i];
                var p2 = Points[i + 1];

                // 필압에 기반한 두께 계산
                float width1 = StrokeWidth * p1.Pressure;
                float width2 = StrokeWidth * p2.Pressure;

                // 두 점 사이의 거리 계산
                float dx = p2.X - p1.X;
                float dy = p2.Y - p1.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance < 0.01f)
                    continue;

                // 두 점 사이의 단위 벡터 계산
                float nx = -dy / distance;
                float ny = dx / distance;

                // 두께에 따른 오프셋 계산
                float offset1 = width1 / 2;
                float offset2 = width2 / 2;

                // 사다리꼴 형태로 경로 만들기
                var path = new SKPath();
                path.MoveTo(p1.X + nx * offset1, p1.Y + ny * offset1);
                path.LineTo(p2.X + nx * offset2, p2.Y + ny * offset2);
                path.LineTo(p2.X - nx * offset2, p2.Y - ny * offset2);
                path.LineTo(p1.X - nx * offset1, p1.Y - ny * offset1);
                path.Close();

                // 사다리꼴 그리기
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = StrokeColor,
                    IsAntialias = true
                })
                {
                    canvas.DrawPath(path, paint);
                }

                // 곡선 연결 부분에 원 그리기 (부드러운 연결을 위해)
                if (i > 0)
                {
                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Fill,
                        Color = StrokeColor,
                        IsAntialias = true
                    })
                    {
                        var radius = Math.Min(offset1, offset2);
                        canvas.DrawCircle(p1.X, p1.Y, radius, paint);
                    }
                }
            }
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
                PathCacheOption = PathCacheOption,
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
        public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver; // Blend mode for the highlighter (e.g., normal, multiply, screen)

        public QIHighlighter()
        {
            StrokeCap = SKStrokeCap.Round;
            StrokeJoin = SKStrokeJoin.Round;
            StrokeWidth = 15.0f;
            PressureOption = false;
        }

        public override void Render(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = StrokeColor.WithAlpha(128), // 반투명
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
                BlendMode = BlendMode
            };

            canvas.DrawPath(Path, paint);
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
                PathCacheOption = PathCacheOption,
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
            // 채우기 먼저 (있는 경우)
            if (FillOption)
            {
                using var fillPaint = new SKPaint
                {
                    Color = FillColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                canvas.DrawPath(Path, fillPaint);
            }

            // 테두리
            using var strokePaint = new SKPaint
            {
                Color = StrokeColor,
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawPath(Path, strokePaint);
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
                PathCacheOption = PathCacheOption,
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

    }

}
