using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DeNote.Models
{
    public class BaseDrawingAttribute
    {
        public Brush Stroke { get; set; } = Brushes.Black; // Stroke color for the object
        public double StrokeThickness { get; set; } = 1; // Thickness of the stroke

        protected double MaxStrokeThickness { get; set; } = 20;
        protected double MinStrokeThickness { get; set; } = 0.2;

        public Brush Fill { get; set; } = Brushes.Transparent; // Fill color for the object
        public double Opacity { get; set; } = 1.0; // Opacity of the object (0 to 1)

        public virtual BaseDrawingAttribute Clone()
        {
            return new BaseDrawingAttribute()
            {
                Stroke = Stroke,
                StrokeThickness = StrokeThickness,
                Fill = Fill,
                Opacity = Opacity,
            };
        }
    }

    public enum PenType
    {
        Normal,
        Highlighter,
    }

    public class PenStrokeDrawingAttribute : BaseDrawingAttribute
    {
        public PenType PenType { get; set; } = PenType.Normal;
        public double Smoothness { get; set; } = 0.2;

        public PenStrokeDrawingAttribute(PenType penType = PenType.Normal)
        {
            this.PenType = penType;

            switch (penType)
            {
                case PenType.Highlighter:
                    base.Stroke = Brushes.Yellow;
                    base.Opacity = 0.5;
                    base.StrokeThickness = 10;
                    base.MinStrokeThickness = 5;
                    break;
                default:
                    base.Stroke = Brushes.Red;
                    break;
            }
        }

        public override PenStrokeDrawingAttribute Clone()
        {
            return new PenStrokeDrawingAttribute()
            {
                PenType = PenType,
                Smoothness = Smoothness,
                Stroke = Stroke,
                StrokeThickness = StrokeThickness,
                Fill = Fill,
                Opacity = Opacity,
            };
        }

    }

    public enum ShapeDrawingType
    {
        Rectangle,
        Ellipse,
    }

    public class ShapeDrawingAttribute : BaseDrawingAttribute
    {
        public ShapeDrawingType ShapeType { get; set; } = ShapeDrawingType.Rectangle; // Type of shape (e.g., "Rectangle", "Circle", etc.)
        public bool IsStroked { get; set; } = true; // Indicates if the shape has a stroke or not
        public bool IsFilled { get; set; } = false; // Indicates if the shape is filled or not
        public CornerRadius CornerRadius { get; set; } = new CornerRadius(0); // Corner radius for rounded corners

        public override ShapeDrawingAttribute Clone()
        {
            return new ShapeDrawingAttribute()
            {
                ShapeType = ShapeType,
                IsFilled = IsFilled,
                IsStroked = IsStroked,
                CornerRadius = new CornerRadius(CornerRadius.TopLeft),
                Stroke = Stroke,
                StrokeThickness = StrokeThickness,
                Fill = Fill,
                Opacity = Opacity,
            };
        }
    }

}
