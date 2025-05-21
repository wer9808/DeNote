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
        void HandleInput(QIDrawingInputData input, QIDrawingContext context);
        void ApplySettings(QIDrawingToolSettings settings);
        QIDrawingToolSettings GetSettings();
    }

    public class QIPenTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Pen;
        private QIPenToolSettings Settings { get; set; } = new QIPenToolSettings();
        private QIPenStroke CurrentStroke { get; set; }

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

        public void HandleInput(QIDrawingInputData input, QIDrawingContext context)
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
                        context.AddDrawingObject(CurrentStroke);
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
            // QIDrawingPathUtils.OptimizePoints(stroke);

            // 최종 경로 생성
            stroke.CachePath();
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
    }


    // 형광펜 도구
    public class QIHighlighterTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Highlighter;
        private QIHighlighterToolSettings Settings { get; set; } = new QIHighlighterToolSettings();
        private QIHighlighter CurrentStroke { get; set; }

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



        public void HandleInput(QIDrawingInputData input, QIDrawingContext context)
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
                        UpdatePath(CurrentStroke);
                        context.InvalidateVisual();
                    }
                    break;

                case QIDrawingInputType.Up:
                    // 스트로크 완료
                    if (CurrentStroke != null)
                    {
                        AddPoint(CurrentStroke, input);
                        UpdatePath(CurrentStroke);
                        FinalizeStroke(CurrentStroke);
                        context.AddDrawingObject(CurrentStroke);
                        CurrentStroke = null;
                        context.ActiveObject = null;
                    }
                    break;
            }
        }


        private void AddPoint(QIHighlighter stroke, QIDrawingInputData input)
        {
            stroke.Points.Add(new QIPoint
            {
                X = input.X,
                Y = input.Y,
                Pressure = input.Pressure
            });
        }

        private void UpdatePath(QIHighlighter stroke)
        {
            // 간단한 경로 생성 (실시간 피드백용)
            stroke.Path = new SKPath();

            if (stroke.Points.Count == 0) return;

            stroke.Path.MoveTo(stroke.Points[0].X, stroke.Points[0].Y);

            for (int i = 1; i < stroke.Points.Count; i++)
            {
                stroke.Path.LineTo(stroke.Points[i].X, stroke.Points[i].Y);
            }
        }

        private void FinalizeStroke(QIHighlighter stroke)
        {
            // 스트로크 최적화 및 최종 경로 생성
            // (베지에 곡선 등 더 복잡한 경로 생성이 가능)
            // QIDrawingPathUtils.OptimizePoints(stroke);

            // 다양한 경로 생성 방법 중 선택

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
    }


    // 도형 도구
    public class QIShapeTool : IQIDrawingTool
    {
        public QIDrawingToolType Type => QIDrawingToolType.Shape;
        private QIShapeToolSettings Settings { get; set; } = new QIShapeToolSettings();
        private QIShape CurrentShape { get; set; }
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

        public void HandleInput(QIDrawingInputData input, QIDrawingContext context)
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
                        context.AddDrawingObject(CurrentShape);
                        CurrentShape = null;
                        context.ActiveObject = null;
                    }
                    break;
            }
        }

        private void UpdateShapePath()
        {
            CurrentShape.Path = new SKPath();

            switch (Settings.Type)
            {
                case QIShapeType.Rectangle:
                    CurrentShape.Path.AddRect(SKRect.Create(
                        Math.Min(StartPoint.X, EndPoint.X),
                        Math.Min(StartPoint.Y, EndPoint.Y),
                        Math.Abs(EndPoint.X - StartPoint.X),
                        Math.Abs(EndPoint.Y - StartPoint.Y)));
                    CurrentShape.Path.Close();
                    break;

                case QIShapeType.Ellipse:
                    CurrentShape.Path.AddOval(SKRect.Create(
                        Math.Min(StartPoint.X, EndPoint.X),
                        Math.Min(StartPoint.Y, EndPoint.Y),
                        Math.Abs(EndPoint.X - StartPoint.X),
                        Math.Abs(EndPoint.Y - StartPoint.Y)));
                    CurrentShape.Path.Close();
                    break;
                case QIShapeType.Triangle:
                    // 삼각형 경로 생성
                    CurrentShape.Path.MoveTo(
                        (StartPoint.X + EndPoint.X) / 2,
                        Math.Min(StartPoint.Y, EndPoint.Y));
                    CurrentShape.Path.LineTo(
                        Math.Min(StartPoint.X, EndPoint.X),
                        Math.Max(StartPoint.Y, EndPoint.Y));
                    CurrentShape.Path.LineTo(
                        Math.Max(StartPoint.X, EndPoint.X),
                        Math.Max(StartPoint.Y, EndPoint.Y));
                    CurrentShape.Path.Close();
                    break;
                case QIShapeType.Line:
                    CurrentShape.Path.MoveTo(StartPoint);
                    CurrentShape.Path.LineTo(EndPoint);
                    break;
            }
            
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
    }
}
