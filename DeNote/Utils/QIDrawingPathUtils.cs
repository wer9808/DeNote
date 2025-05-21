using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeNote.Models.Drawing;
using SkiaSharp;

namespace DeNote.Utils
{
    public class QIDrawingPathUtils
    {
        // 포인트 최적화 설정값
        public static float DefaultDistanceThreshold { get; set; } = 2.0f; // 포인트 간 최소 거리 (이보다 가까우면 제거됨)
        public static float DefaultAngleThreshold { get; set; } = 10.0f; // 각도 변화 임계값 (각도 변화가 작으면 중간 포인트 제거)
        public static float DefaultPressureThreshold { get; set; } = 0.05f; // 필압 변화 임계값

        public static void OptimizePoints(QIStrokeObject strokeObject)
        {
            // 스트로크 최적화 로직
            // 예: Ramer-Douglas-Peucker 알고리즘을 사용하여 점을 줄임
            // 이 부분은 실제 구현에 따라 다를 수 있습니다.
            // qIStrokeObject.Points = Optimize(qIStrokeObject.Points);
            if (strokeObject == null || strokeObject.Points == null || strokeObject.Points.Count <= 2)
                return; // 최적화할 필요가 없음

            var inputPoints = strokeObject.Points;
            var result = new List<QIPoint>();

            // 첫 번째 포인트는 항상 포함
            result.Add(inputPoints[0]);

            // 필압 변화 패턴을 감지하기 위한 변수들
            float lastPressure = inputPoints[0].Pressure;
            bool pressureIncreasing = false;
            bool pressureDecreasing = false;

            for (int i = 1; i < inputPoints.Count - 1; i++)
            {
                var prev = inputPoints[i - 1];
                var curr = inputPoints[i];
                var next = inputPoints[i + 1];

                // 필압 변화 방향 감지
                bool currentIncreasing = curr.Pressure > prev.Pressure;
                bool currentDecreasing = curr.Pressure < prev.Pressure;

                // 필압 변화 방향이 바뀌는 지점 감지 (중요 포인트)
                bool pressureDirectionChange = (pressureIncreasing && currentDecreasing) ||
                                              (pressureDecreasing && currentIncreasing);

                // 필압 변화량 계산
                float pressureDiff = Math.Abs(curr.Pressure - lastPressure);

                // 현재 포인트와 이전 포인트 사이의 거리 계산
                float distance = (float)Math.Sqrt(Math.Pow(curr.X - prev.X, 2) + Math.Pow(curr.Y - prev.Y, 2));

                // 각도 계산 (이전-현재-다음 포인트가 형성하는 각도)
                var v1x = curr.X - prev.X;
                var v1y = curr.Y - prev.Y;
                var v2x = next.X - curr.X;
                var v2y = next.Y - curr.Y;

                // 두 벡터의 내적을 이용한 각도 계산
                var dot = v1x * v2x + v1y * v2y;
                var mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                var mag2 = Math.Sqrt(v2x * v2x + v2y * v2y);

                // 0으로 나누기 방지
                if (mag1 * mag2 == 0)
                    continue;

                var cosAngle = dot / (mag1 * mag2);
                cosAngle = Math.Max(-1, Math.Min(1, cosAngle)); // 값 범위 -1 ~ 1로 제한
                var angle = Math.Acos(cosAngle) * 180 / Math.PI;

                // 포인트 유지 여부 결정
                bool keepPoint = false;

                // 1. 거리가 충분히 멀면 유지
                if (distance >= DefaultDistanceThreshold)
                    keepPoint = true;

                // 2. 각도 변화가 크면 유지
                if (angle > DefaultAngleThreshold)
                    keepPoint = true;

                // 3. 필압 변화가 임계값보다 크면 유지
                if (pressureDiff > DefaultPressureThreshold)
                    keepPoint = true;

                // 4. 필압 변화 방향이 바뀌는 점이면 유지
                if (pressureDirectionChange)
                    keepPoint = true;

                // 5. 필압 최대 또는 최소 지점이면 유지
                if ((curr.Pressure > prev.Pressure && curr.Pressure > next.Pressure) ||
                    (curr.Pressure < prev.Pressure && curr.Pressure < next.Pressure))
                    keepPoint = true;

                if (keepPoint)
                {
                    result.Add(curr);
                    lastPressure = curr.Pressure;
                    pressureIncreasing = currentIncreasing;
                    pressureDecreasing = currentDecreasing;
                }
            }

            // 마지막 포인트는 항상 포함
            result.Add(inputPoints[inputPoints.Count - 1]);

            // 너무 많은 포인트가 제거된 경우 보간 추가
            if (result.Count < inputPoints.Count * 0.1 && result.Count > 2)
            {
                var expandedResult = new List<QIPoint>();
                expandedResult.Add(result[0]);

                // 인접한 포인트 사이에 보간 포인트 추가
                for (int i = 0; i < result.Count - 1; i++)
                {
                    var p1 = result[i];
                    var p2 = result[i + 1];

                    // 두 점 사이의 거리가 멀면 보간 추가
                    float dist = (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

                    if (dist > DefaultDistanceThreshold * 5)
                    {
                        // 적절한 개수의 보간 포인트 결정 (거리에 따라)
                        int interpolationCount = (int)(dist / (DefaultDistanceThreshold * 2));
                        interpolationCount = Math.Min(interpolationCount, 5); // 최대 5개로 제한

                        for (int j = 1; j <= interpolationCount; j++)
                        {
                            float ratio = (float)j / (interpolationCount + 1);
                            var interpolatedPoint = new QIPoint
                            {
                                X = p1.X + (p2.X - p1.X) * ratio,
                                Y = p1.Y + (p2.Y - p1.Y) * ratio,
                                // 선형 보간으로 필압 유지
                                Pressure = p1.Pressure + (p2.Pressure - p1.Pressure) * ratio
                            };
                            expandedResult.Add(interpolatedPoint);
                        }
                    }

                    expandedResult.Add(p2);
                }

                result = expandedResult;
            }

            // 원본 Points 컬렉션 업데이트
            strokeObject.Points.Clear();
            strokeObject.Points.AddRange(result);
        }


        /// <summary>
        /// 베지에 곡선을 생성하여 Path 객체를 업데이트합니다.
        /// </summary>
        /// <param name="strokeObject">경로를 업데이트할 스트로크 객체</param>
        public static void CreateBezierPath(QIStrokeObject strokeObject)
        {
            if (strokeObject == null || strokeObject.Points == null || strokeObject.Points.Count == 0)
                return;

            var points = strokeObject.Points;
            strokeObject.Path.Reset();

            if (points.Count == 1)
            {
                // 포인트가 하나만 있으면 원을 그림
                var p = points[0];
                strokeObject.Path.AddCircle(p.X, p.Y, strokeObject.StrokeWidth / 2, SKPathDirection.Clockwise);
                return;
            }

            if (points.Count == 2)
            {
                // 포인트가 두 개면 직선을 그림
                strokeObject.Path.MoveTo(points[0].X, points[0].Y);
                strokeObject.Path.LineTo(points[1].X, points[1].Y);
                return;
            }

            // 3개 이상의 포인트가 있는 경우 베지에 곡선 생성
            strokeObject.Path.MoveTo(points[0].X, points[0].Y);

            for (int i = 0; i < points.Count - 2; i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];
                var p2 = points[i + 2];

                // 컨트롤 포인트 계산 (p1을 중심으로 부드러운 곡선 형성)
                float cpx1 = p0.X + (p1.X - p0.X) * 0.5f;
                float cpy1 = p0.Y + (p1.Y - p0.Y) * 0.5f;
                float cpx2 = p1.X + (p2.X - p1.X) * 0.5f;
                float cpy2 = p1.Y + (p2.Y - p1.Y) * 0.5f;

                // 첫 번째 세그먼트는 이전 포인트에서 첫 번째 컨트롤 포인트까지 쿼드 곡선
                if (i == 0)
                {
                    strokeObject.Path.QuadTo(p0.X, p0.Y, cpx1, cpy1);
                }

                // p1을 중심으로 베지에 곡선 그리기
                strokeObject.Path.QuadTo(p1.X, p1.Y, cpx2, cpy2);

                // 마지막 세그먼트는 마지막 컨트롤 포인트에서 마지막 포인트까지
                if (i == points.Count - 3)
                {
                    strokeObject.Path.LineTo(p2.X, p2.Y);
                }
            }
        }


        /// <summary>
        /// Ramer-Douglas-Peucker 알고리즘의 재귀적 구현
        /// </summary>
        private static List<QIPoint> RamerDouglasPeucker(List<QIPoint> points, int startIndex, int endIndex, float epsilon)
        {
            if (endIndex <= startIndex + 1)
            {
                // 시작과 끝 포인트만 있는 경우 그대로 반환
                return new List<QIPoint> { points[startIndex], points[endIndex] };
            }

            // 시작과 끝 포인트를 연결하는 직선
            float dMax = 0;
            int index = startIndex;

            // 시작과 끝 포인트
            QIPoint start = points[startIndex];
            QIPoint end = points[endIndex];

            // 선분의 길이 계산 (시작점부터 끝점까지)
            float lineLength = (float)Math.Sqrt(
                Math.Pow(end.X - start.X, 2) +
                Math.Pow(end.Y - start.Y, 2)
            );

            // 각 점에서 직선까지의 수직 거리 계산
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                float distance;

                if (lineLength == 0)
                {
                    // 시작점과 끝점이 같은 경우 (드문 경우)
                    distance = (float)Math.Sqrt(
                        Math.Pow(points[i].X - start.X, 2) +
                        Math.Pow(points[i].Y - start.Y, 2)
                    );
                }
                else
                {
                    // 점에서 직선까지의 수직 거리 계산
                    float u = ((points[i].X - start.X) * (end.X - start.X) +
                               (points[i].Y - start.Y) * (end.Y - start.Y)) /
                              (lineLength * lineLength);

                    // 직선 위의 가장 가까운 점 계산
                    QIPoint closestPoint;
                    if (u < 0)
                        closestPoint = start;
                    else if (u > 1)
                        closestPoint = end;
                    else
                        closestPoint = new QIPoint(
                            start.X + u * (end.X - start.X),
                            start.Y + u * (end.Y - start.Y)
                        );

                    // 거리 계산
                    distance = (float)Math.Sqrt(
                        Math.Pow(points[i].X - closestPoint.X, 2) +
                        Math.Pow(points[i].Y - closestPoint.Y, 2)
                    );
                }

                // 가장 먼 거리와 해당 포인트 인덱스 업데이트
                if (distance > dMax)
                {
                    index = i;
                    dMax = distance;
                }
            }

            // 가장 먼 거리가 임계값보다 크면 해당 지점을 기준으로 재귀적으로 분할
            List<QIPoint> result = new List<QIPoint>();
            if (dMax > epsilon)
            {
                // 재귀적으로 양쪽 부분을 처리
                List<QIPoint> recResults1 = RamerDouglasPeucker(points, startIndex, index, epsilon);
                List<QIPoint> recResults2 = RamerDouglasPeucker(points, index, endIndex, epsilon);

                // 결과 병합 (중복 포인트 제거)
                result.AddRange(recResults1.Take(recResults1.Count - 1));
                result.AddRange(recResults2);
            }
            else
            {
                // 임계값보다 작으면 시작과 끝점만 유지
                result.Add(start);
                result.Add(end);
            }

            return result;
        }

    }


}
