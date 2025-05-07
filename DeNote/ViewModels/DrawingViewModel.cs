using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeNote.Models;

namespace DeNote.ViewModels
{
    public partial class DrawingViewModel : ObservableObject
    {

        [ObservableProperty]
        private ObservableCollection<DrawingObject> _drawingObjects = new ObservableCollection<DrawingObject>();

        [ObservableProperty]
        private bool _drawingUpdated = false;

        [ObservableProperty]
        private DrawingObject? _currentDrawingObject = null;

        [ObservableProperty]
        private DrawingObjectType _currentDrawingObjectType = DrawingObjectType.Pen;

        [ObservableProperty]
        private PenType _currentPenType = PenType.Normal;

        [ObservableProperty]
        private ShapeDrawingType _currentShapeDrawingType = ShapeDrawingType.Rectangle;

        [ObservableProperty]
        private bool _isDrawing = false;

        [ObservableProperty]
        private Brush _currentStrokeColor = Brushes.Red;

        [ObservableProperty]
        private double _currentStrokeThickness = 1.0;

        [ObservableProperty]
        private Brush _currentFillColor = Brushes.Transparent;

        [ObservableProperty]
        private bool _isFilled = false;
        [ObservableProperty]
        private bool _isStroked = false;

        [ObservableProperty]
        private PenStrokeDrawingAttribute _normalPenAttribute = new PenStrokeDrawingAttribute(PenType.Normal);

        [ObservableProperty]
        private PenStrokeDrawingAttribute _highlighterAttribute = new PenStrokeDrawingAttribute(PenType.Highlighter);

        [ObservableProperty]
        private ShapeDrawingAttribute _shapeDrawingAttribute = new ShapeDrawingAttribute();

        private Point _startPoint;
        private Point _endPoint;

        [RelayCommand]
        private void StartDrawing(StylusPoint startPoint)
        {
            if (IsDrawing)
                EndDrawing();
            IsDrawing = true;

            switch (CurrentDrawingObjectType)
            {
                case DrawingObjectType.Pen:
                    PenStroke penStroke;
                    switch (CurrentPenType)
                    {
                        case PenType.Highlighter:
                            penStroke = new PenStroke(HighlighterAttribute.Clone());
                            break;
                        default:
                            penStroke = new PenStroke(NormalPenAttribute.Clone());
                            break;
                    }
                    penStroke.Points.Add(startPoint);
                    CurrentDrawingObject = penStroke;
                    break;
                case DrawingObjectType.Shape:
                    _startPoint = startPoint.ToPoint();
                    switch (CurrentShapeDrawingType)
                    {
                        case ShapeDrawingType.Rectangle:
                            CurrentDrawingObject = new RectangleShape(ShapeDrawingAttribute.Clone());
                            break;
                        case ShapeDrawingType.Ellipse:
                            CurrentDrawingObject = new EllipseShape(ShapeDrawingAttribute.Clone());
                            break;
                    }
                    break;
            }

            DrawingUpdated = true;
        }

        [RelayCommand]
        private void UpdateDrawing(StylusPoint currentPoint)
        {
            if (!IsDrawing || CurrentDrawingObject == null)
                return;
            switch (CurrentDrawingObjectType)
            {
                case DrawingObjectType.Pen:
                    var penStroke = CurrentDrawingObject as PenStroke;
                    penStroke.Points.Add(currentPoint);
                    break;
                case DrawingObjectType.Shape:
                    switch (CurrentShapeDrawingType)
                    {
                        case ShapeDrawingType.Rectangle:
                            var rectangle = CurrentDrawingObject as RectangleShape;
                            
                            var x = Math.Min(_startPoint.X, currentPoint.X);
                            var y = Math.Min(_startPoint.Y, currentPoint.Y);
                            var width = Math.Abs(_startPoint.X - currentPoint.X);
                            var height = Math.Abs(_startPoint.Y - currentPoint.Y);

                            rectangle.Position = new Point(x, y);
                            rectangle.Size = new Size(width, height);

                            break;
                        case ShapeDrawingType.Ellipse:
                            var ellipse = CurrentDrawingObject as EllipseShape;

                            var ellipseX = Math.Min(_startPoint.X, currentPoint.X);
                            var ellipseY = Math.Min(_startPoint.Y, currentPoint.Y);
                            var ellipseWidth = Math.Abs(_startPoint.X - currentPoint.X);
                            var ellipseHeight = Math.Abs(_startPoint.Y - currentPoint.Y);

                            ellipse.Position = new Point(ellipseX, ellipseY);
                            ellipse.Size = new Size(ellipseWidth, ellipseHeight);

                            break;
                    }
                    break;
            }

            DrawingUpdated = true;
        }

        [RelayCommand]
        private void EndDrawing(StylusPoint? endPoint = null)
        {
            IsDrawing = false;
            if (CurrentDrawingObject == null)
                return;
            DrawingObjects.Add(CurrentDrawingObject);
            CurrentDrawingObject = null;

            DrawingUpdated = true;
        }

        [RelayCommand]
        private void ClearDrawing() 
        { 
            DrawingObjects.Clear();

            DrawingUpdated = true;
        }

        private void SavePrevDrawingObjectTypeSetting()
        {

            switch (CurrentDrawingObjectType)
            {
                case DrawingObjectType.Pen:
                    if (CurrentPenType == PenType.Normal)
                    {
                        NormalPenAttribute.Stroke = CurrentStrokeColor;
                        NormalPenAttribute.StrokeThickness = CurrentStrokeThickness;
                    }
                    else
                    {
                        HighlighterAttribute.Stroke = CurrentStrokeColor;
                        HighlighterAttribute.StrokeThickness = CurrentStrokeThickness;
                    }
                    break;
                case DrawingObjectType.Shape:
                    ShapeDrawingAttribute.Stroke = CurrentStrokeColor;
                    ShapeDrawingAttribute.StrokeThickness = CurrentStrokeThickness;
                    ShapeDrawingAttribute.IsFilled = IsFilled;
                    ShapeDrawingAttribute.IsStroked = IsStroked;
                    break;
                default:
                    break;
            }

        }

        [RelayCommand]
        private void ChangeToPen(PenType penType)
        {
            if (IsDrawing) return;

            SavePrevDrawingObjectTypeSetting();

            CurrentDrawingObjectType = DrawingObjectType.Pen;
            CurrentPenType = penType;
        }

        [RelayCommand]
        private void ChangeToShape(ShapeDrawingType shapeDrawingType)
        {
            if (IsDrawing) return;

            SavePrevDrawingObjectTypeSetting();

            CurrentDrawingObjectType = DrawingObjectType.Shape;
            CurrentShapeDrawingType = shapeDrawingType;
        }
    }
}
