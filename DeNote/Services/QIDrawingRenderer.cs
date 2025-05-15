using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeNote.Models.Drawing;
using SkiaSharp;
using System.Windows.Shapes;
using System.Windows.Ink;

namespace DeNote.Services
{
    public class QIDrawingRenderer
    {
        // 캐시 관리
        private Dictionary<Guid, SKBitmap> bitmapCache = new Dictionary<Guid, SKBitmap>();

        public void Render(SKCanvas canvas, QIDrawingObject obj)
        {
            if (obj.IsDirty)
            {
                // 캐싱 여부 동적 결정
                obj.BitmapCacheOption = ShouldCacheObject(obj);
                obj.IsDirty = false;
            }

            // 캐시 확인
            if (obj.BitmapCacheOption)
            {
                if (obj.CachedBitmap == null || obj.IsDirty)
                {
                    CacheObject(obj);
                    obj.IsDirty = false;
                }

                // SKSamplingOptions 사용
                // 고품질 Linear 필터링과 mipmap 사용
                SKSamplingOptions samplingOptions = new SKSamplingOptions(
                    SKFilterMode.Nearest,  // 선형 필터링
                    SKMipmapMode.None   // 선형 밉맵 필터링
                );

                // SKBitmap을 SKImage로 변환
                using (SKImage image = SKImage.FromBitmap(obj.CachedBitmap))
                {
                    // SKImage.DrawImage 메서드 사용 (일부 버전에서 지원)
                    // 버전에 따라 다음 형식 중 하나를 사용

                    // 방법 1: 페인트 사용
                    using (var paint = new SKPaint())
                    {
                        paint.IsAntialias = true;

                        canvas.DrawImage(
                            image,
                            obj.CacheBounds.Left,
                            obj.CacheBounds.Top,
                            samplingOptions,
                            paint
                        );
                    }
                }
                return;
            }

            // 캐시가 없거나 캐싱하지 않는 경우
            // 직접 렌더링
            obj.Render(canvas);
        }

        public bool ShouldCacheObject(QIDrawingObject obj)
        {
            // 스트로크 객체인 경우
            if (obj is QIPenStroke stroke)
            {
                // 포인트 개수에 따른 캐싱 결정
                int pointThreshold = 30; // 임계값, 테스트를 통해 조정 필요

                // 포인트 개수와 효과 여부로 결정
                return stroke.Points.Count > pointThreshold;
            }

            else if (obj is QIHighlighter)
            {
                return false;
            }

            // 다른 유형의 객체에 대한 규칙
            // 예: 이미지나 텍스트는 항상 캐싱
            // 기본적으로는 객체의 복잡도나 크기를 기준으로 결정
            double area = obj.Bounds.Width * obj.Bounds.Height;
            return area > 10000; // 일정 크기 이상이면 캐싱 (픽셀 단위)
        }

        private void CacheObject(QIDrawingObject obj)
        {
            // 렌더링 결과를 비트맵으로 캐싱
            // ...

            int bitmapWidth = (int)obj.CacheBounds.Width;
            int bitmapHeight = (int)obj.CacheBounds.Height;

            SKBitmap bitmap = new SKBitmap(bitmapWidth, bitmapHeight);
            using (SKCanvas bitmapCanvas = new SKCanvas(bitmap))
            {
                bitmapCanvas.Clear(SKColors.Transparent);

                // 캔버스의 원점을 객체의 Bounds 좌표만큼 이동
                bitmapCanvas.Translate(-obj.CacheBounds.Left, -obj.CacheBounds.Top);

                obj.Render(bitmapCanvas);
            }
            obj.CachedBitmap = bitmap;
        }

        public void InvalidateCache(Guid objId)
        {
            if (bitmapCache.ContainsKey(objId))
            {
                bitmapCache[objId]?.Dispose();
                bitmapCache.Remove(objId);
            }
        }
    }

}
