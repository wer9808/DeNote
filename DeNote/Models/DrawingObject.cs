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

namespace DeNote.Models
{

    public enum DrawingObjectType
    {
        Pen,
        Shape,
        Text,
        Image,
    }

    public class DrawingObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the object
        public DrawingObjectType Type { get; set; } // e.g., "Rectangle", "Circle", etc.
        public DateTime CreationTime { get; set; } = DateTime.Now; // Timestamp of when the object was created
        public DateTime LastModifiedTime { get; set; } = DateTime.Now; // Timestamp of the last modification

        public Point Position { get; set; } = new Point(0, 0); // Position of the object on the canvas
        public Size Size { get; set; } = new Size(100, 100); // Size of the object (width and height)
        public double Rotation { get; set; } = 0; // Rotation angle of the object in degrees
        public Vector Scale { get; set; } = new Vector(1, 1); // Scale factor for the object (X and Y scale)
        public int ZIndex { get; set; } = 0; // Z-index for layering objects on the canvas


        public BaseDrawingAttribute DrawingAttribute { get; set; } = new BaseDrawingAttribute();
        public Color Stroke { get => DrawingAttribute.Stroke; set => DrawingAttribute.Stroke = value; } // Stroke color for the object
        public double StrokeThickness { get => DrawingAttribute.StrokeThickness; set => DrawingAttribute.StrokeThickness = value; } // Thickness of the stroke
        public Color Fill { get => DrawingAttribute.Fill; set => DrawingAttribute.Fill = value; } // Fill color for the object
        public double Opacity { get => DrawingAttribute.Opacity; set => DrawingAttribute.Opacity = value; } // Opacity of the object (0 to 1)

        public bool IsVisible { get; set; } = true; // Indicates if the object is visible
        public bool IsSelected { get; set; } = false; // Indicates if the object is selected
        public bool IsLocked { get; set; } = false; // Indicates if the object is locked (not editable)

        public static void DrawObject(SKCanvas canvas, DrawingObject drawingObject)
        {
            switch (drawingObject.Type)
            {
                case DrawingObjectType.Pen:
                    var penStroke = drawingObject as PenStroke;
                    DrawStroke(canvas, penStroke);
                    // DrawStroke(canvas, penStroke);
                    break;
                case DrawingObjectType.Shape:
                    DrawShape(canvas, drawingObject as ShapeDrawingObject);
                    break;
                default:
                    throw new NotSupportedException($"Drawing object type '{drawingObject.Type}' is not supported.");
            }
        }

        // Draw without pressure
        public static void DrawStroke(SKCanvas canvas, PenStroke penStroke)
        {
            var pathFitter = new PathFitter(1.0);

            var skPoints = penStroke.Points.Select(p => new SKPoint((float)p.X, (float)p.Y)).ToList();
            var smoothedPath = pathFitter.CreateCatmullRomPath(skPoints);

            var skColor = penStroke.Stroke.ToSKColor();

            if (penStroke.PenType == PenType.Highlighter)
            {
                skColor = skColor.WithAlpha((byte)(skColor.Alpha * penStroke.Opacity));

                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = skColor,
                    StrokeWidth = (float)penStroke.StrokeThickness,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    IsAntialias = true,
                    BlendMode = SKBlendMode.SrcOver,
                })
                {
                    canvas.DrawPath(smoothedPath, paint);
                }
            }
            else
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = skColor,
                    StrokeWidth = (float)penStroke.StrokeThickness,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    IsAntialias = true,
                })
                {
                    canvas.DrawPath(smoothedPath, paint);
                }
            }
        }

        // Draw with pressure
        private static void DrawHighDensityStrokeWithPressure(SKCanvas canvas, PenStroke penStroke)
        {
            if (penStroke.Points.Count < 2) return;

            var skPoints = penStroke.Points.Select(p => new SKPoint((float)p.X, (float)p.Y)).ToList();
            var skColor = penStroke.Stroke.ToSKColor();

            // 먼저 원본 포인트로 Catmull-Rom 스플라인 생성 (높은 세그먼트로)
            List<SKPoint> highDensityPoints = GenerateHighDensityPoints(skPoints, 5);
            List<float> pressures = penStroke.Points.Select(p => (float)p.Pressure).ToList();

            // 고밀도 포인트에 대한 압력 보간
            List<float> highDensityPressures = InterpolatePressures(skPoints, pressures, highDensityPoints);

            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = skColor,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            })
            {
                // 고밀도 포인트들을 LineSegment로 그리기
                for (int i = 0; i < highDensityPoints.Count - 1; i++)
                {
                    float pressure = highDensityPressures[i];
                    if (float.IsNaN(pressure) || pressure <= 0) pressure = 1.0f;

                    paint.StrokeWidth = (float)penStroke.StrokeThickness * pressure;
                    canvas.DrawLine(highDensityPoints[i], highDensityPoints[i + 1], paint);
                }
            }
        }

        private static List<SKPoint> GenerateHighDensityPoints(List<SKPoint> originalPoints, int densityFactor)
        {
            var pathFitter = new PathFitter(1.0);

            SKPath catmullRomPath = pathFitter.CreateCatmullRomPath(originalPoints, 0.5f, densityFactor);

            // SKPath에서 포인트 추출 (매우 간단한 구현)
            List<SKPoint> pathPoints = new List<SKPoint>();

            // 기존 CreateCatmullRomPath 함수를 수정하여 생성된 모든 포인트를 반환하도록 변경
            // 또는 더 간단하게 아래와 같이 세분화
            for (int i = 0; i < originalPoints.Count - 1; i++)
            {
                SKPoint start = originalPoints[i];
                SKPoint end = originalPoints[i + 1];

                // 두 점 사이에 densityFactor+1 개의 포인트 생성
                for (int j = 0; j <= densityFactor; j++)
                {
                    float t = (float)j / densityFactor;
                    float x = start.X + (end.X - start.X) * t;
                    float y = start.Y + (end.Y - start.Y) * t;
                    pathPoints.Add(new SKPoint(x, y));
                }
            }

            return pathPoints;
        }

        private static List<float> InterpolatePressures(List<SKPoint> originalPoints, List<float> originalPressures, List<SKPoint> highDensityPoints)
        {
            List<float> result = new List<float>(highDensityPoints.Count);

            // 각 고밀도 포인트에 대해 가장 가까운 원본 포인트의 인덱스와 거리를 찾음
            for (int i = 0; i < highDensityPoints.Count; i++)
            {
                SKPoint currentPoint = highDensityPoints[i];

                // 원본 점들 중 가장 가까운 두 점 찾기
                float minDist1 = float.MaxValue;
                float minDist2 = float.MaxValue;
                int minIdx1 = 0;
                int minIdx2 = 0;

                for (int j = 0; j < originalPoints.Count; j++)
                {
                    float dist = SKPoint.Distance(currentPoint, originalPoints[j]);

                    if (dist < minDist1)
                    {
                        minDist2 = minDist1;
                        minIdx2 = minIdx1;
                        minDist1 = dist;
                        minIdx1 = j;
                    }
                    else if (dist < minDist2)
                    {
                        minDist2 = dist;
                        minIdx2 = j;
                    }
                }

                // 두 압력값 사이를 거리에 따라 보간
                float totalDist = minDist1 + minDist2;
                if (totalDist <= 0.001f)
                {
                    // 거의 원본 포인트와 일치
                    result.Add(originalPressures[minIdx1]);
                }
                else
                {
                    // 거리에 따른 가중 평균
                    float weight1 = 1 - (minDist1 / totalDist);
                    float weight2 = 1 - (minDist2 / totalDist);
                    float normalizedWeight1 = weight1 / (weight1 + weight2);
                    float normalizedWeight2 = weight2 / (weight1 + weight2);

                    float interpolatedPressure = originalPressures[minIdx1] * normalizedWeight1 +
                                                originalPressures[minIdx2] * normalizedWeight2;
                    result.Add(interpolatedPressure);
                }
            }

            return result;
        }

        public static void DrawShape(SKCanvas canvas, ShapeDrawingObject shapeDrawingObject)
        {
            var shapeType = shapeDrawingObject.ShapeType;

            if (shapeType == ShapeDrawingType.Rectangle)
            {
                var rectangle = shapeDrawingObject as RectangleShape;
                var rect = new SKRect((float)rectangle.Position.X, (float)rectangle.Position.Y,
                    (float)(rectangle.Position.X + rectangle.Size.Width), (float)(rectangle.Position.Y + rectangle.Size.Height));

                using (var paint = new SKPaint
                {
                    Style = rectangle.IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                    Color = rectangle.Stroke.ToSKColor(),
                    StrokeWidth = (float)rectangle.StrokeThickness,
                    IsAntialias = true,
                })
                {
                    canvas.DrawRect(rect, paint);
                }
            }
            else if (shapeType == ShapeDrawingType.Ellipse)
            {
                var ellipse = shapeDrawingObject as EllipseShape;
                var centerX = (float)(ellipse.Position.X + ellipse.RadiusX);
                var centerY = (float)(ellipse.Position.Y + ellipse.RadiusY);
                var radiusX = (float)ellipse.RadiusX;
                var radiusY = (float)ellipse.RadiusY;
                using (var paint = new SKPaint
                {
                    Style = ellipse.IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                    Color = ellipse.Stroke.ToSKColor(),
                    StrokeWidth = (float)ellipse.StrokeThickness,
                    IsAntialias = true,
                })
                {
                    canvas.DrawOval(centerX, centerY, radiusX, radiusY, paint);
                }
            }
            else
            {
                throw new NotSupportedException($"Shape type '{shapeType}' is not supported.");
            }
        }
    }

    public class QIPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Pressure { get; set; } = 1.0;
        public QIPoint(double x, double y, double pressure)
        {
            X = x;
            Y = y;
            Pressure = pressure;
        }

        public QIPoint(StylusPoint stylusPoint)
        {
            X = stylusPoint.X;
            Y = stylusPoint.Y;
            Pressure = stylusPoint.PressureFactor;
        }

    }

    public class PenStroke : DrawingObject
    {
        public List<QIPoint> Points { get; set; } = []; // List of points that make up the stroke
        public PenStrokeDrawingAttribute PenAttribute { get => base.DrawingAttribute as PenStrokeDrawingAttribute; }
        public PenType PenType { get => PenAttribute.PenType; }
        public double Smoothness { get => PenAttribute.Smoothness; }
        public bool PressureEnabled { get => PenAttribute.PressureEnabled; set => PenAttribute.PressureEnabled = value; } // Indicates if pressure sensitivity is enabled

        public PenStroke(PenType penType = PenType.Normal)
        {
            base.Type = DrawingObjectType.Pen;
            base.DrawingAttribute = new PenStrokeDrawingAttribute(penType);
        }

        public PenStroke(PenStrokeDrawingAttribute penStrokeDrawingAttribute)
        {
            base.Type = DrawingObjectType.Pen;
            base.DrawingAttribute = penStrokeDrawingAttribute;
        }

    }

    // 베지어 곡선을 이용한 경로 보간 클래스
    public class PathFitter
    {
        private double _error;

        public PathFitter(double error)
        {
            _error = error;
        }

        public SKPath FitPath(SKPoint[] points)
        {
            var path = new SKPath();

            if (points.Length < 2)
                return path;

            path.MoveTo(points[0]);

            if (points.Length == 2)
            {
                path.LineTo(points[1]);
                return path;
            }

            // 베지어 곡선 피팅 알고리즘
            FitCubic(path, points, 0, points.Length - 1);

            return path;
        }

        private void FitCubic(SKPath path, SKPoint[] points, int first, int last)
        {
            if (last - first < 2)
            {
                path.LineTo(points[last]);
                return;
            }

            // 시작점과 끝점 설정
            var startPoint = points[first];
            var endPoint = points[last];

            // 제어점 계산
            var tan1 = GetTangent(points, first, 0);
            var tan2 = GetTangent(points, last, 1);

            // 제어점 거리 계산 (1/3 규칙)
            float segmentLength = GetLength(startPoint, endPoint) / 3;

            // 제어점 설정
            var control1 = new SKPoint(
                startPoint.X + tan1.X * segmentLength,
                startPoint.Y + tan1.Y * segmentLength);

            var control2 = new SKPoint(
                endPoint.X - tan2.X * segmentLength,
                endPoint.Y - tan2.Y * segmentLength);

            // 베지어 곡선 추가
            path.CubicTo(control1.X, control1.Y, control2.X, control2.Y, endPoint.X, endPoint.Y);
        }

        private SKPoint GetTangent(SKPoint[] points, int pointIndex, int endPointType)
        {
            // 끝점 유형: 0 = 시작점, 1 = 끝점
            int prev = Math.Max(pointIndex - 1, 0);
            int next = Math.Min(pointIndex + 1, points.Length - 1);

            // 접선 벡터 계산
            float dx, dy;

            if (endPointType == 0)
            {
                // 시작점의 접선
                dx = points[next].X - points[prev].X;
                dy = points[next].Y - points[prev].Y;
            }
            else
            {
                // 끝점의 접선
                dx = points[pointIndex].X - points[prev].X;
                dy = points[pointIndex].Y - points[prev].Y;
            }

            // 단위 벡터로 정규화
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length > 0)
            {
                dx /= length;
                dy /= length;
            }

            return new SKPoint(dx, dy);
        }

        private float GetLength(SKPoint p1, SKPoint p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public SKPath CreateCatmullRomPath(List<SKPoint> points, float tension = 0.5f, int segments = 10)
        {
            if (points.Count < 2)
                return new SKPath();

            SKPath path = new SKPath();
            path.MoveTo(points[0]);

            if (points.Count == 2)
            {
                path.LineTo(points[1]);
                return path;
            }

            // 첫 번째와 마지막 포인트 복제하여 경계 처리
            List<SKPoint> tempPoints = new List<SKPoint>();
            tempPoints.Add(points[0]); // 첫 포인트 복제
            tempPoints.AddRange(points);
            tempPoints.Add(points[points.Count - 1]); // 마지막 포인트 복제

            for (int i = 0; i < tempPoints.Count - 3; i++)
            {
                SKPoint p0 = tempPoints[i];
                SKPoint p1 = tempPoints[i + 1];
                SKPoint p2 = tempPoints[i + 2];
                SKPoint p3 = tempPoints[i + 3];

                // 세그먼트 사이에 더 많은 포인트 추가
                for (int j = 0; j <= segments; j++)
                {
                    float t = (float)j / segments;

                    // Catmull-Rom 보간 공식
                    float t2 = t * t;
                    float t3 = t2 * t;

                    float x = 0.5f * ((2 * p1.X) +
                            (-p0.X + p2.X) * t +
                            (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                            (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);

                    float y = 0.5f * ((2 * p1.Y) +
                            (-p0.Y + p2.Y) * t +
                            (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                            (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);

                    if (i > 0 || j > 0)
                        path.LineTo(x, y);
                    else
                        path.MoveTo(x, y);
                }
            }

            return path;
        }
    }


    public abstract class ShapeDrawingObject : DrawingObject
    {
        public ShapeDrawingAttribute ShapeDrawingAttribute { get => base.DrawingAttribute as ShapeDrawingAttribute; }
        public ShapeDrawingType ShapeType { get => ShapeDrawingAttribute.ShapeType; } // Type of shape (e.g., "Rectangle", "Circle", etc.)
        public bool IsFilled { get => ShapeDrawingAttribute.IsFilled; set => ShapeDrawingAttribute.IsFilled = value; } // Indicates if the shape is filled or not
        public bool IsStroked { get => ShapeDrawingAttribute.IsStroked; set => ShapeDrawingAttribute.IsStroked = value; } // Indicates if the shape has a stroke or not

        public abstract Geometry CreateGeometry(); // Method to create the geometry of the shape
    }

    public class RectangleShape : ShapeDrawingObject
    {

        public RectangleShape(ShapeDrawingAttribute? shapeDrawingAttribute = null)
        {
            base.Type = DrawingObjectType.Shape;
            base.DrawingAttribute = shapeDrawingAttribute ?? new ShapeDrawingAttribute();
        }

        public CornerRadius CornerRadius { get => this.ShapeDrawingAttribute.CornerRadius; }

        public override Geometry CreateGeometry()
        {

            // Create a rectangle geometry with the specified properties
            RectangleGeometry rectangleGeometry = new RectangleGeometry(new Rect(Position, Size), CornerRadius.TopLeft, CornerRadius.BottomRight);

            return rectangleGeometry;
        }
    }

    public class EllipseShape : ShapeDrawingObject
    {
        public EllipseShape(ShapeDrawingAttribute? shapeDrawingAttribute = null)
        {
            base.Type = DrawingObjectType.Shape;
            base.DrawingAttribute = shapeDrawingAttribute ?? new ShapeDrawingAttribute();
        }

        public double RadiusX
        {
            get { return Size.Width / 2; } // X radius of the ellipse
            set { Size = new Size(value * 2, Size.Height); } // Set the width based on the X radius
        }

        public double RadiusY
        {
            get { return Size.Height / 2; } // Y radius of the ellipse
            set { Size = new Size(Size.Width, value * 2); } // Set the height based on the Y radius
        }

        public Point Center
        {
            get { return new Point(Position.X + RadiusX, Position.Y + RadiusY); } // Center point of the ellipse
            set { Position = new Point(value.X - RadiusX, value.Y - RadiusY); } // Set the position based on the center point
        }

        public override Geometry CreateGeometry()
        {
            // Create an ellipse geometry with the specified properties
            EllipseGeometry ellipseGeometry = new EllipseGeometry(Center, RadiusX, RadiusY);
            return ellipseGeometry;
        }
    }

}
