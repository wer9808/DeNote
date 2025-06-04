using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DeNote.Models.DeNote.Models;
using DeNote.Utils;
using SkiaSharp;

namespace DeNote.Models.Drawing
{

    // 입력 데이터 구조
    public class QIDrawingInputData
    {
        public QIDrawingInputType Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Pressure { get; set; } = 1.0f;
    }

    public enum QIDrawingInputType
    {
        Down,
        Move,
        Up
    }

    public enum QIDrawingToolType
    {
        Pen,
        Eraser,
        Highlighter,
        Shape,
        Text,
        Image,
    }

    public interface IQIDrawingTool
    {
        QIDrawingToolType Type { get; }
        QIDrawingObject CreateDrawingObject();
        Task HandleInput(QIDrawingInputData input, QIDrawingContext context);
        void ApplySettings(QIDrawingToolSettings settings);
        QIDrawingToolSettings GetSettings();
        void CancelDrawing(QIDrawingContext context);
    }

    public class QIPenTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Pen;
        private QIPenToolSettings Settings { get; set; } = new QIPenToolSettings();
        private QIPenStroke? CurrentStroke { get; set; }

        public QIDrawingObject CreateDrawingObject()
        {
            return new QIPenStroke
            {
                StrokeColor = Settings.Color,
                StrokeWidth = Settings.StrokeWidth,
                StrokeCap = Settings.StrokeCap,
                StrokeJoin = Settings.StrokeJoin,
                Points = new List<QIPoint>(),
                PressureOption = Settings.PressureEnabled,
            };
        }

        public async Task HandleInput(QIDrawingInputData input, QIDrawingContext context)
        {
            // Handle input for pen tool

            switch (input.Type)
            {
                case QIDrawingInputType.Down:
                    // 새 스트로크 시작
                    CurrentStroke = (QIPenStroke)CreateDrawingObject();
                    AddPoint(CurrentStroke, input);
                    context.ActiveObject = CurrentStroke;
                    break;

                case QIDrawingInputType.Move:
                    // 포인트 추가
                    if (CurrentStroke != null)
                    {
                        AddPoint(CurrentStroke, input);
                        context.InvalidateVisual();
                    }
                    break;

                case QIDrawingInputType.Up:
                    // 스트로크 완료
                    if (CurrentStroke != null)
                    {
                        AddPoint(CurrentStroke, input);
                        FinalizeStroke(CurrentStroke);
                        await context.AddDrawingObject(CurrentStroke);
                        CurrentStroke = null;
                        context.ActiveObject = null;
                    }
                    break;
            }
        }


        private void AddPoint(QIPenStroke stroke, QIDrawingInputData input)
        {
            stroke.AddPoint(new QIPoint
            {
                X = input.X,
                Y = input.Y,
                Pressure = input.Pressure
            });
        }

        private void FinalizeStroke(QIPenStroke stroke)
        {
            // 스트로크 최적화 및 최종 경로 생성
            // (베지에 곡선 등 더 복잡한 경로 생성이 가능)
            stroke.UpdateEnvelope();
        }

        public void ApplySettings(QIDrawingToolSettings settings)
        {
            // Apply settings for pen tool
            if (settings is QIPenToolSettings penSettings)
            {
                Settings = penSettings;
            }
        }

        public QIDrawingToolSettings GetSettings()
        {
            return Settings.Clone();
        }

        public void CancelDrawing(QIDrawingContext context)
        {
            // 현재 스트로크 취소
            if (CurrentStroke != null)
            {
                context.ActiveObject = null;
                CurrentStroke = null;
                context.InvalidateVisual();
            }
        }
    }


    // 형광펜 도구
    public class QIHighlighterTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Highlighter;
        private QIHighlighterToolSettings Settings { get; set; } = new QIHighlighterToolSettings();
        private QIHighlighter? CurrentStroke { get; set; }

        // 펜 도구와 유사한 구현...

        public QIDrawingObject CreateDrawingObject()
        {
            return new QIHighlighter
            {
                StrokeColor = Settings.Color,
                StrokeWidth = Settings.StrokeWidth,
                StrokeCap = Settings.StrokeCap,
                StrokeJoin = Settings.StrokeJoin,
                BlendMode = Settings.BlendMode
            };
        }

        // 나머지 메서드는 PenTool과 유사...



        public async Task HandleInput(QIDrawingInputData input, QIDrawingContext context)
        {
            // Handle input for pen tool

            switch (input.Type)
            {
                case QIDrawingInputType.Down:
                    // 새 스트로크 시작
                    CurrentStroke = (QIHighlighter)CreateDrawingObject();
                    AddPoint(CurrentStroke, input);
                    context.ActiveObject = CurrentStroke;
                    break;

                case QIDrawingInputType.Move:
                    // 포인트 추가
                    if (CurrentStroke != null)
                    {
                        AddPoint(CurrentStroke, input);
                        context.InvalidateVisual();
                    }
                    break;

                case QIDrawingInputType.Up:
                    // 스트로크 완료
                    if (CurrentStroke != null)
                    {
                        AddPoint(CurrentStroke, input);
                        FinalizeStroke(CurrentStroke);
                        await context.AddDrawingObject(CurrentStroke);
                        CurrentStroke = null;
                        context.ActiveObject = null;
                    }
                    break;
            }
        }


        private void AddPoint(QIHighlighter stroke, QIDrawingInputData input)
        {
            stroke.AddPoint(new QIPoint
            {
                X = input.X,
                Y = input.Y,
                Pressure = input.Pressure
            });
        }

        private void FinalizeStroke(QIHighlighter stroke)
        {
            // 스트로크 최적화 및 최종 경로 생성
            // (베지에 곡선 등 더 복잡한 경로 생성이 가능)
            stroke.UpdateEnvelope();
        }

        public void ApplySettings(QIDrawingToolSettings settings)
        {
            // Apply settings for pen tool
            if (settings is QIHighlighterToolSettings highlighterSettings)
            {
                Settings = highlighterSettings;
            }
        }

        public QIDrawingToolSettings GetSettings()
        {
            return Settings.Clone();
        }

        public void CancelDrawing(QIDrawingContext context)
        {
            // 현재 스트로크 취소
            if (CurrentStroke != null)
            {
                context.ActiveObject = null;
                CurrentStroke = null;
                context.InvalidateVisual();
            }
        }
    }


    // 도형 도구
    public class QIShapeTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Shape;
        private QIShapeToolSettings Settings { get; set; } = new QIShapeToolSettings();
        private QIShape? CurrentShape { get; set; }
        private SKPoint StartPoint { get; set; }
        private SKPoint EndPoint { get; set; }

        public QIDrawingObject CreateDrawingObject()
        {
            return new QIShape
            {
                StrokeColor = Settings.Color,
                StrokeWidth = Settings.StrokeWidth,
                FillColor = Settings.FillColor,
                FillOption = Settings.FillOption,
                ShapeType = Settings.Type
            };
        }

        public async Task HandleInput(QIDrawingInputData input, QIDrawingContext context)
        {
            switch (input.Type)
            {
                case QIDrawingInputType.Down:
                    // 도형 시작
                    CurrentShape = (QIShape)CreateDrawingObject();
                    StartPoint = new SKPoint(input.X, input.Y);
                    EndPoint = StartPoint;
                    UpdateShapePath();
                    context.ActiveObject = CurrentShape;
                    break;

                case QIDrawingInputType.Move:
                    // 도형 크기 조절
                    if (CurrentShape != null)
                    {
                        EndPoint = new SKPoint(input.X, input.Y);
                        UpdateShapePath();
                        context.InvalidateVisual();
                    }
                    break;

                case QIDrawingInputType.Up:
                    // 도형 완료
                    if (CurrentShape != null)
                    {
                        EndPoint = new SKPoint(input.X, input.Y);
                        UpdateShapePath();
                        await context.AddDrawingObject(CurrentShape);
                        CurrentShape = null;
                        context.ActiveObject = null;
                    }
                    break;
            }
        }

        private void UpdateShapePath()
        {
            var shapePath = new SKPath();

            switch (Settings.Type)
            {
                case QIShapeType.Rectangle:
                    shapePath.AddRect(SKRect.Create(
                        Math.Min(StartPoint.X, EndPoint.X),
                        Math.Min(StartPoint.Y, EndPoint.Y),
                        Math.Abs(EndPoint.X - StartPoint.X),
                        Math.Abs(EndPoint.Y - StartPoint.Y)));
                    shapePath.Close();
                    break;

                case QIShapeType.Ellipse:
                    shapePath.AddOval(SKRect.Create(
                        Math.Min(StartPoint.X, EndPoint.X),
                        Math.Min(StartPoint.Y, EndPoint.Y),
                        Math.Abs(EndPoint.X - StartPoint.X),
                        Math.Abs(EndPoint.Y - StartPoint.Y)));
                    shapePath.Close();
                    break;
                case QIShapeType.Triangle:
                    // 삼각형 경로 생성
                    shapePath.MoveTo(
                        (StartPoint.X + EndPoint.X) / 2,
                        Math.Min(StartPoint.Y, EndPoint.Y));
                    shapePath.LineTo(
                        Math.Min(StartPoint.X, EndPoint.X),
                        Math.Max(StartPoint.Y, EndPoint.Y));
                    shapePath.LineTo(
                        Math.Max(StartPoint.X, EndPoint.X),
                        Math.Max(StartPoint.Y, EndPoint.Y));
                    shapePath.LineTo(
                        (StartPoint.X + EndPoint.X) / 2,
                        Math.Min(StartPoint.Y, EndPoint.Y));
                    shapePath.Close();
                    break;
                case QIShapeType.Line:
                    shapePath.MoveTo(StartPoint);
                    shapePath.LineTo(EndPoint);
                    break;
            }

            CurrentShape?.UpdateShape(shapePath);
        }

        public void ApplySettings(QIDrawingToolSettings settings)
        {
            if (settings is QIShapeToolSettings shapeSettings)
            {
                Settings = shapeSettings;
            }
        }

        public QIDrawingToolSettings GetSettings()
        {
            return Settings.Clone();
        }

        public void CancelDrawing(QIDrawingContext context)
        {
            // 현재 스트로크 취소
            if (CurrentShape != null)
            {
                context.ActiveObject = null;
                CurrentShape = null;
                context.InvalidateVisual();
            }
        }
    }

    public class QIEraserTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Eraser;
        private QIEraserToolSettings Settings { get; set; } = new QIEraserToolSettings();

        public QIDrawingObject CreateDrawingObject()
        {
            throw new NotImplementedException();
        }

        public async Task HandleInput(QIDrawingInputData input, QIDrawingContext context)
        {
            var point = new QIPoint
            {
                X = input.X,
                Y = input.Y,
                Pressure = input.Pressure,
            };

            switch (input.Type)
            {
                case QIDrawingInputType.Down:
                    context.StartErasing(point, Settings.StrokeWidth);
                    break;
                case QIDrawingInputType.Move:
                    context.UpdateErasing(point);
                    break;
                case QIDrawingInputType.Up:
                    await context.EraseActualObjects();
                    break;
            }
        }

        public void ApplySettings(QIDrawingToolSettings settings)
        {
            if (settings is QIEraserToolSettings eraseToolSettings)
            {
                Settings = eraseToolSettings;
            }
        }

        public QIDrawingToolSettings GetSettings()
        {
            return Settings.Clone();
        }

        public void CancelDrawing(QIDrawingContext context)
        {
            // 현재 지우기 작업 취소
            context.CancelErasing();
        }
    }
}
