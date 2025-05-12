using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
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
using SkiaSharp;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

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
        private SKBitmap _canvasScreenshotBitmap;

        [ObservableProperty]
        private DrawingObject? _currentDrawingObject = null;

        [ObservableProperty]
        private DrawingObjectType _currentDrawingObjectType = DrawingObjectType.Pen;

        [ObservableProperty]
        private PenType _currentPenType = PenType.Normal;

        [ObservableProperty]
        private StylusPointDescription _currentPointDescription;

        [ObservableProperty]
        private ShapeDrawingType _currentShapeDrawingType = ShapeDrawingType.Rectangle;

        [ObservableProperty]
        private bool _isDrawing = false;

        [ObservableProperty]
        private bool _isAttributeChanging = false;

        [ObservableProperty]
        private PenStrokeDrawingAttribute _normalPenAttribute = new PenStrokeDrawingAttribute(PenType.Normal);

        [ObservableProperty]
        private PenStrokeDrawingAttribute _highlighterAttribute = new PenStrokeDrawingAttribute(PenType.Highlighter);

        [ObservableProperty]
        private ShapeDrawingAttribute _shapeDrawingAttribute = new ShapeDrawingAttribute();

        [ObservableProperty]
        private Visibility _toolBarVisibility = Visibility.Visible;

        private QIPoint _startPoint;
        private QIPoint _endPoint;

        public DrawingViewModel()
        {
            
        }

        [RelayCommand]
        private void StartDrawing(StylusPoint startPoint)
        {
            if (IsAttributeChanging) return;
            if (IsDrawing)
                EndDrawing();
            IsDrawing = true;
            ToolBarVisibility = Visibility.Collapsed;

            var startQIPoint = new QIPoint(startPoint);

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

                    penStroke.Points.Add(startQIPoint);
                    CurrentDrawingObject = penStroke;
                    break;
                case DrawingObjectType.Shape:
                    _startPoint = startQIPoint;
                    ShapeDrawingAttribute.ShapeType = CurrentShapeDrawingType;
                    switch (ShapeDrawingAttribute.ShapeType)
                    {
                        case ShapeDrawingType.Rectangle:
                            var rectangle = new RectangleShape(ShapeDrawingAttribute.Clone());
                            rectangle.Position = new Point(startQIPoint.X, startQIPoint.Y);
                            rectangle.Size = new Size(0, 0);

                            CurrentDrawingObject = rectangle;
                            break;
                        case ShapeDrawingType.Ellipse:
                            var ellipse = new EllipseShape(ShapeDrawingAttribute.Clone());
                            ellipse.Position = new Point(startQIPoint.X, startQIPoint.Y);
                            ellipse.Size = new Size(0, 0);
                            CurrentDrawingObject = ellipse;
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

            var currentQIPoint = new QIPoint(currentPoint);

            switch (CurrentDrawingObjectType)
            {
                case DrawingObjectType.Pen:
                    var penStroke = CurrentDrawingObject as PenStroke;
                    penStroke.Points.Add(currentQIPoint);
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
            ToolBarVisibility = Visibility.Visible;
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
            if (IsAttributeChanging) return;
            if (IsDrawing) return;

            CurrentDrawingObjectType = DrawingObjectType.Pen;
            CurrentPenType = penType;
        }

        [RelayCommand]
        private void ChangeToShape(ShapeDrawingType shapeDrawingType)
        {
            if (IsAttributeChanging) return;
            if (IsDrawing) return;

            CurrentDrawingObjectType = DrawingObjectType.Shape;
            CurrentShapeDrawingType = shapeDrawingType;
        }

        [RelayCommand]
        private void CaptureScreen()
        {
            // 전체 화면 크기 가져오기
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // 비트맵 생성
            using (Bitmap bitmap = new Bitmap(screenWidth, screenHeight))
            {
                // 화면 캡처
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));
                }

                // Bitmap을 SKBitmap으로 변환
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Position = 0;

                    CanvasScreenshotBitmap = SKBitmap.Decode(memoryStream);
                }
            }
        }
    }
}
