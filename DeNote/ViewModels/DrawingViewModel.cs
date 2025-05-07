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
using DeNote.Services;

namespace DeNote.ViewModels
{

    public partial class DrawingViewModel : ObservableObject
    {

        private readonly DrawingCommandManager _commandManager = new DrawingCommandManager();

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
        private PenStrokeDrawingAttribute _normalPenAttribute = new PenStrokeDrawingAttribute(PenType.Normal);

        [ObservableProperty]
        private PenStrokeDrawingAttribute _highlighterAttribute = new PenStrokeDrawingAttribute(PenType.Highlighter);

        [ObservableProperty]
        private ShapeDrawingAttribute _shapeDrawingAttribute = new ShapeDrawingAttribute();

        private Point _startPoint;
        private Point _endPoint;

        public DrawingViewModel()
        {
            
        }

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
                    ShapeDrawingAttribute.ShapeType = CurrentShapeDrawingType;
                    switch (ShapeDrawingAttribute.ShapeType)
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
            var command = new AddDrawingObjectCommand(DrawingObjects, CurrentDrawingObject);
            ExecuteCommand(command);
            CurrentDrawingObject = null;

            DrawingUpdated = true;
        }

        [RelayCommand]
        private void ClearDrawing()
        {
            var command = new ClearDrawingObjectsCommand(DrawingObjects);
            ExecuteCommand(command);

            DrawingUpdated = true;
        }

        private IRelayCommand _undoCommand;
        public IRelayCommand UndoCommand => _undoCommand ??= new RelayCommand(
            () => {
                _commandManager.Undo();
                NotifyCanExecuteChanged();
                DrawingUpdated = true;
            },
            () => _commandManager.CanUndo);

        private IRelayCommand _redoCommand;
        public IRelayCommand RedoCommand => _redoCommand ??= new RelayCommand(
            () => {
                _commandManager.Redo();
                NotifyCanExecuteChanged();
                DrawingUpdated = true;
            },
            () => _commandManager.CanRedo);

        private void ExecuteCommand(MemCommand command)
        {
            _commandManager.ExecuteCommand(command);
            NotifyCanExecuteChanged();
        }

        private void NotifyCanExecuteChanged()
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void ChangeToPen(PenType penType)
        {
            if (IsDrawing) return;

            CurrentDrawingObjectType = DrawingObjectType.Pen;
            CurrentPenType = penType;
        }

        [RelayCommand]
        private void ChangeToShape(ShapeDrawingType shapeDrawingType)
        {
            if (IsDrawing) return;

            CurrentDrawingObjectType = DrawingObjectType.Shape;
            CurrentShapeDrawingType = shapeDrawingType;
        }
    }
}
