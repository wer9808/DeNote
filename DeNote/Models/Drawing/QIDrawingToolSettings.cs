using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNote.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using SkiaSharp;

    namespace DeNote.Models
    {
        // 도구 설정 기본 클래스
        public abstract class QIDrawingToolSettings
        {
            public SKColor Color { get; set; } = SKColors.Red;

            public abstract QIDrawingToolSettings Clone();
        }

        // 펜 도구 설정 클래스
        public class QIPenToolSettings : QIDrawingToolSettings
        {
            public float StrokeWidth { get; set; } = 5.0f;
            public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round;
            public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round;
            public float Opacity { get; set; } = 1.0f;
            public bool PressureEnabled { get; set; } = false;

            public override QIDrawingToolSettings Clone()
            {
                return new QIPenToolSettings
                {
                    Color = this.Color,
                    StrokeWidth = this.StrokeWidth,
                    StrokeCap = this.StrokeCap,
                    StrokeJoin = this.StrokeJoin,
                    Opacity = this.Opacity,
                    PressureEnabled = this.PressureEnabled,
                };
            }
        }

        // 펜 도구 설정 클래스
        public class QIHighlighterToolSettings : QIDrawingToolSettings
        {
            public float StrokeWidth { get; set; } = 15.0f;
            public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round;
            public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round;
            public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;

            public override QIDrawingToolSettings Clone()
            {
                return new QIHighlighterToolSettings
                {
                    Color = this.Color,
                    StrokeWidth = this.StrokeWidth,
                    StrokeCap = this.StrokeCap,
                    StrokeJoin = this.StrokeJoin,
                    BlendMode = this.BlendMode
                };
            }
        }


        // 도형 도구 설정
        public class QIShapeToolSettings : QIDrawingToolSettings
        {
            public float StrokeWidth { get; set; } = 2.0f;
            public SKColor FillColor { get; set; } = SKColors.Transparent;
            public bool FillOption { get; set; }
            public Drawing.QIShapeType Type { get; set; } = Drawing.QIShapeType.Rectangle;

            public override QIDrawingToolSettings Clone()
            {
                return new QIShapeToolSettings
                {
                    Color = this.Color,
                    StrokeWidth = this.StrokeWidth,
                    FillColor = this.FillColor,
                    FillOption = this.FillOption,
                    Type = this.Type
                };
            }
        }


        public class QIEraserToolSettings : QIDrawingToolSettings
        {
            public float StrokeWidth { get; set; } = 10.0f;

            public override QIDrawingToolSettings Clone()
            {
                return new QIEraserToolSettings
                {
                    StrokeWidth = StrokeWidth,
                };
            }
        }
    }

}
