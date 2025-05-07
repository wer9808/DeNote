using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
        public Brush Stroke { get => DrawingAttribute.Stroke; set => DrawingAttribute.Stroke = value; } // Stroke color for the object
        public double StrokeThickness { get => DrawingAttribute.StrokeThickness; set => DrawingAttribute.StrokeThickness = value; } // Thickness of the stroke
        public Brush Fill { get => DrawingAttribute.Fill; set => DrawingAttribute.Fill = value; } // Fill color for the object
        public double Opacity { get => DrawingAttribute.Opacity; set => DrawingAttribute.Opacity = value; } // Opacity of the object (0 to 1)

        public bool IsVisible { get; set; } = true; // Indicates if the object is visible
        public bool IsSelected { get; set; } = false; // Indicates if the object is selected
        public bool IsLocked { get; set; } = false; // Indicates if the object is locked (not editable)


        public static UIElement CreateVisualElement(Canvas canvas, DrawingObject drawingObject)
        {
            // Create a visual element based on the type of drawing object

            UIElement uiElement;

            switch (drawingObject.Type)
            {
                case DrawingObjectType.Pen:
                    uiElement = CreateVisualElement(canvas, drawingObject as PenStroke);
                    break;
                case DrawingObjectType.Shape:
                    uiElement = CreateVisualElement(canvas, drawingObject as ShapeDrawingObject);
                    break;
                default:
                    throw new NotSupportedException($"Drawing object type '{drawingObject.Type}' is not supported.");
            }

            if (uiElement != null)
            {
                // Set common properties for the visual element
                uiElement.Opacity = drawingObject.Opacity;
                uiElement.IsHitTestVisible = drawingObject.IsVisible;
                Canvas.SetLeft(uiElement, drawingObject.Position.X);
                Canvas.SetTop(uiElement, drawingObject.Position.Y);
                Panel.SetZIndex(uiElement, drawingObject.ZIndex);

                uiElement.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(drawingObject.Scale.X, drawingObject.Scale.Y),
                        new RotateTransform(drawingObject.Rotation),
                    }
                };

                uiElement.RenderTransformOrigin = new Point(drawingObject.Size.Width / 2, drawingObject.Size.Height / 2);
            }

            return uiElement;
        }


        public static UIElement CreateVisualElement(Canvas canvas, PenStroke penStroke)
        {
            if (penStroke.Points == null || penStroke.Points.Count < 2)
                return null;

            // Path 객체 생성
            Path path = new Path();

            // 스트로크 스타일 설정
            path.Stroke = penStroke.Stroke;
            path.StrokeThickness = penStroke.StrokeThickness;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.StrokeLineJoin = PenLineJoin.Round;

            // 안티앨리어싱 설정
            RenderOptions.SetEdgeMode(path, EdgeMode.Aliased);

            // PathGeometry 생성
            PathGeometry pathGeometry = new PathGeometry();

            // StylusPointCollection에서 점 추출 및 베지어 곡선 생성
            StylusPointCollection points = penStroke.Points;

            if (points.Count == 2)
            {
                // 두 점만 있으면 직선 그리기
                PathFigure figure = new PathFigure();
                figure.StartPoint = new Point(points[0].X, points[0].Y);
                figure.Segments.Add(new LineSegment(new Point(points[1].X, points[1].Y), true));
                pathGeometry.Figures.Add(figure);
            }
            else
            {
                // 여러 점이 있으면 베지어 곡선 생성
                PathFigure figure = new PathFigure();
                figure.StartPoint = new Point(points[0].X, points[0].Y);

                // 베지어 곡선의 제어점 계산 헬퍼 메서드
                Point ComputeControlPoint(Point p0, Point p1, Point p2)
                {
                    // 이전 점과 다음 점 사이의 벡터 계산
                    double vx = p2.X - p0.X;
                    double vy = p2.Y - p0.Y;

                    // 제어점 위치 조정 (부드러움 조절 계수)
                    double smoothness = penStroke.Smoothness;

                    // 제어점 계산
                    return new Point(
                        p1.X + smoothness * vx,
                        p1.Y + smoothness * vy
                    );
                }

                // 베지어 곡선 생성을 위한 점 준비
                for (int i = 0; i < points.Count - 1; i += 3)
                {
                    if (i + 3 < points.Count)
                    {
                        // 4개의 점을 사용하여 베지어 곡선 생성
                        Point p0 = new Point(points[i].X, points[i].Y);
                        Point p1 = new Point(points[i + 1].X, points[i + 1].Y);
                        Point p2 = new Point(points[i + 2].X, points[i + 2].Y);
                        Point p3 = new Point(points[i + 3].X, points[i + 3].Y);

                        // 제어점 계산
                        Point ctrl1 = ComputeControlPoint(p0, p1, p2);
                        Point ctrl2 = ComputeControlPoint(p1, p2, p3);

                        // 베지어 세그먼트 추가
                        figure.Segments.Add(new BezierSegment(ctrl1, ctrl2, p2, true));
                    }
                    else if (i + 1 < points.Count)
                    {
                        // 남은 점은 직선으로 연결
                        figure.Segments.Add(new LineSegment(new Point(points[i + 1].X, points[i + 1].Y), true));
                    }
                }

                pathGeometry.Figures.Add(figure);
            }

            path.Data = pathGeometry;

            return path;
        }


        public static UIElement CreateVisualElement(Canvas canvas, ShapeDrawingObject shapeDrawingObject)
        {
            Shape uiShape = null;

            switch (shapeDrawingObject.ShapeType)
            {
                case ShapeDrawingType.Rectangle:
                    var rectangleShape = shapeDrawingObject as RectangleShape;
                    var rectangle = new Rectangle();
                    rectangle.RadiusX = rectangleShape.CornerRadius.TopLeft;
                    rectangle.RadiusY = rectangleShape.CornerRadius.TopLeft;
                    uiShape = rectangle;
                    break;
                case ShapeDrawingType.Ellipse:
                    var elipseShape = shapeDrawingObject as EllipseShape;
                    var ellipse = new Ellipse();
                    ellipse.Width = elipseShape.Size.Width;
                    ellipse.Height = elipseShape.Size.Height;
                    uiShape = ellipse;
                    break;
                default:
                    throw new NotSupportedException($"Drawing object type '{shapeDrawingObject.Type}' is not supported.");
            }

            if (uiShape != null)
            {
                uiShape.Fill = shapeDrawingObject.Fill;
                uiShape.Stroke = shapeDrawingObject.Stroke;
                uiShape.StrokeThickness = shapeDrawingObject.StrokeThickness;
                uiShape.Opacity = shapeDrawingObject.Opacity;
                uiShape.IsHitTestVisible = shapeDrawingObject.IsVisible;
                // Set the position and size of the shape
                uiShape.Width = shapeDrawingObject.Size.Width;
                uiShape.Height = shapeDrawingObject.Size.Height;
                uiShape.Opacity = shapeDrawingObject.Opacity;
            }

            return uiShape;
        }
    }

    public class PenStroke : DrawingObject
    {
        public StylusPointCollection Points { get; set; } = new StylusPointCollection(); // List of points that make up the stroke
        public PenStrokeDrawingAttribute PenAttribute { get => base.DrawingAttribute as PenStrokeDrawingAttribute; }
        public PenType PenType { get => PenAttribute.PenType; }
        public double Smoothness { get => PenAttribute.Smoothness; }

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
