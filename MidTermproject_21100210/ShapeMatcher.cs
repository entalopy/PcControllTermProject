using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;

namespace MidTermproject_21100210
{
    public sealed class ShapeMatcher
    {
        public ShapeFeatures LastOrientedFeatures { get; private set; }
        public string LastDebugInfo { get; private set; }

        private sealed class BoundarySample
        {
            public float T { get; set; }
            public PointF Left { get; set; }
            public PointF Right { get; set; }
            public PointF Center { get; set; }
            public float Width { get; set; }
        }

        private sealed class ProjectedBoundaryPoint
        {
            public float T { get; set; }
            public float Side { get; set; }
            public PointF Point { get; set; }
        }

        private sealed class ContourSample
        {
            public float T { get; set; }
            public float Side { get; set; }
            public PointF Point { get; set; }
        }

        public PointF[] Match(ShapeModel model, ShapeFeatures maskFeatures)
        {
            // 대응점 계산은 여기서 시작한다.
            // 먼저 마스크에 가장 잘 맞는 회전 후보를 고르고, 그 후보로 빨간점 6개를 마스크 픽셀 좌표로 변환한다.
            ShapeFeatures oriented = ChooseBestOrientation(model, maskFeatures);
            List<BoundarySample> boundarySamples = BuildBoundarySamples(oriented);
            oriented = ApplyMaxPairBallLines(oriented, boundarySamples);
            LastOrientedFeatures = oriented;
            var debug = new StringBuilder();
            debug.AppendLine("=== Boundary-based point matching ===");
            debug.AppendFormat(CultureInfo.InvariantCulture, "boundary samples: {0}", boundarySamples.Count);
            debug.AppendLine();
            if (boundarySamples.Count >= 2)
            {
                debug.AppendLine("ball lines: max1-left to max1-right, max2-left to max2-right");
            }

            var result = new PointF[model.MeasurementPoints.Length];
            int boundarySuccessCount = 0;
            int fallbackCount = 0;
            for (int i = 0; i < model.MeasurementPoints.Length; i++)
            {
                ShapePointLocation loc = model.ToShapeLocation(model.MeasurementPoints[i]);
                bool usedBoundary;
                result[i] = TransformByBoundaryLocation(oriented, loc, boundarySamples, out usedBoundary);
                if (usedBoundary)
                {
                    boundarySuccessCount++;
                }
                else
                {
                    fallbackCount++;
                }

                debug.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "P{0}: T={1:0.000}, SideRatio={2:0.000}, boundary={3}, fallback={4}",
                    i + 1,
                    loc.T,
                    loc.SideRatio,
                    usedBoundary ? "success" : "failed",
                    usedBoundary ? "no" : "yes");
                debug.AppendLine();
            }

            debug.AppendFormat(CultureInfo.InvariantCulture, "boundary success: {0}/{1}, fallback: {2}", boundarySuccessCount, model.MeasurementPoints.Length, fallbackCount);
            LastDebugInfo = debug.ToString();
            return result;
        }

        private ShapeFeatures ChooseBestOrientation(ShapeModel model, ShapeFeatures maskFeatures)
        {
            // 앞뒤 방향이 이상하면 가장 먼저 여기부터 보면 된다.
            // 실제 입력은 회전만 있고 mirror는 없으므로 정상/180도 회전 후보만 비교한다.
            // 기준 파란 윤곽 155개를 마스크로 변환했을 때 실제 마스크 외곽선과 가장 잘 맞는 후보를 선택한다.
            ShapeFeatures best = maskFeatures;
            double bestScore = double.MinValue;
            List<CvPoint> boundaryPoints = GetBoundaryPoints(maskFeatures.BinaryMask);
            int[,] signs = { { 1, 1 } };
            for (int i = 0; i < signs.GetLength(0); i++)
            {
                ShapeFeatures candidate = WithAxisSigns(maskFeatures, signs[i, 0], signs[i, 1]);
                double outlineScore = ScoreOutlineFit(model, candidate, boundaryPoints);
                // 빨간점 번호는 사용자가 드래그해서 의미가 바뀔 수 있으므로
                // 회전 후보 선택에 사용하지 않는다.
                // outlineScore는 외곽선 거리만 보므로 앞코 방향이 애매한 이미지에서는 위험할 수 있다.
                // 180도 뒤집힘이 계속 보이면 FrontSign/FrontConfidence 기반 penalty를 여기에 추가한다.
                double score = outlineScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private double ScoreOutlineFit(ShapeModel model, ShapeFeatures candidate, List<CvPoint> boundaryPoints)
        {
            // 회전/좌우반전 후보 중 어느 것이 가장 좋은지 점수화하는 함수이다.
            // 잘못된 mirror를 선택하면 여기서 외곽선 거리 점수나 inside 점수 가중치를 조절하면 된다.
            if (candidate.BinaryMask == null || boundaryPoints.Count == 0)
            {
                return 0;
            }

            double totalDistance = 0;
            int usableCount = 0;
            int insideCount = 0;
            foreach (PointF outlinePoint in model.OutlinePoints)
            {
                PointF mapped = TransformPoint(model, candidate, outlinePoint);
                int x = (int)Math.Round(mapped.X);
                int y = (int)Math.Round(mapped.Y);
                if (x < 0 || y < 0 || x >= candidate.BinaryMask.Cols || y >= candidate.BinaryMask.Rows)
                {
                    totalDistance += 80;
                    usableCount++;
                    continue;
                }

                if (IsInside(candidate.BinaryMask, x, y))
                {
                    insideCount++;
                }

                totalDistance += NearestBoundaryDistance(mapped, boundaryPoints);
                usableCount++;
            }

            if (usableCount == 0)
            {
                return double.MinValue;
            }

            double averageDistance = totalDistance / usableCount;
            return -averageDistance * 20.0 + insideCount * 0.8;
        }

        private static List<CvPoint> GetBoundaryPoints(Mat mask)
        {
            var result = new List<CvPoint>();
            if (mask == null)
            {
                return result;
            }

            for (int y = 1; y < mask.Rows - 1; y++)
            {
                for (int x = 1; x < mask.Cols - 1; x++)
                {
                    if (mask.At<byte>(y, x) == 0)
                    {
                        continue;
                    }

                    bool boundary =
                        mask.At<byte>(y - 1, x) == 0 ||
                        mask.At<byte>(y + 1, x) == 0 ||
                        mask.At<byte>(y, x - 1) == 0 ||
                        mask.At<byte>(y, x + 1) == 0;
                    if (boundary && ((x + y) % 2 == 0))
                    {
                        result.Add(new CvPoint(x, y));
                    }
                }
            }

            return result;
        }

        private static double NearestBoundaryDistance(PointF p, List<CvPoint> boundaryPoints)
        {
            double best = 6400;
            foreach (CvPoint boundary in boundaryPoints)
            {
                double dx = p.X - boundary.X;
                double dy = p.Y - boundary.Y;
                double d = dx * dx + dy * dy;
                if (d < best)
                {
                    best = d;
                }
            }

            return Math.Sqrt(best);
        }

        private static ShapeFeatures WithAxisSigns(ShapeFeatures source, int sx, int sy)
        {
            return new ShapeFeatures
            {
                Center = source.Center,
                AxisX = new Point2f(source.AxisX.X * sx, source.AxisX.Y * sx),
                AxisY = new Point2f(source.AxisY.X * sy, source.AxisY.Y * sy),
                Width = source.Width,
                Height = source.Height,
                BoundingBox = source.BoundingBox,
                Area = source.Area,
                MainComponentRatio = source.MainComponentRatio,
                QualityScore = source.QualityScore,
                FrontSign = source.FrontSign,
                FrontConfidence = source.FrontConfidence,
                InnerSign = source.InnerSign,
                InnerConfidence = source.InnerConfidence,
                ShoeSide = source.ShoeSide,
                RotationAngleDegrees = source.RotationAngleDegrees,
                IslandCount = source.IslandCount,
                IslandThreshold = source.IslandThreshold,
                FrontBallWidth = source.FrontBallWidth,
                RearBallWidth = source.RearBallWidth,
                CenterLineStart = source.CenterLineStart,
                CenterLineEnd = source.CenterLineEnd,
                FrontBallLineStart = source.FrontBallLineStart,
                FrontBallLineEnd = source.FrontBallLineEnd,
                RearBallLineStart = source.RearBallLineStart,
                RearBallLineEnd = source.RearBallLineEnd,
                Label = source.Label,
                IsBad = source.IsBad,
                BadReason = source.BadReason,
                BinaryMask = source.BinaryMask
            };
        }

        private double CountInside(Mat mask, ShapeModel model, ShapeFeatures features)
        {
            if (mask == null)
            {
                return 0;
            }

            double count = 0;
            foreach (PointF p in model.MeasurementPoints)
            {
                PointF q = TransformPoint(model, features, p);
                int x = (int)Math.Round(q.X);
                int y = (int)Math.Round(q.Y);
                if (IsInside(mask, x, y))
                {
                    count++;
                }
            }

            return count;
        }

        public PointF TransformPoint(ShapeModel model, ShapeFeatures maskFeatures, PointF modelPoint)
        {
            // 실제 선형변환 공식이 들어있는 핵심 함수이다.
            // 기준 중심 -> 기준 PCA 축 좌표 -> 크기 스케일 -> 마스크 PCA 축 좌표 -> 마스크 중심 순서로 변환한다.
            // 대응점 공식 자체를 바꾸고 싶을 때만 이 부분을 수정하면 된다.
            ShapeFeatures reference = model.Features;
            var relative = new Point2f(modelPoint.X - reference.Center.X, modelPoint.Y - reference.Center.Y);
            float localX = ShapeModel.Dot(relative, reference.AxisX);
            float localY = ShapeModel.Dot(relative, reference.AxisY);
            float sx = maskFeatures.Width / Math.Max(reference.Width, 1f);
            float sy = maskFeatures.Height / Math.Max(reference.Height, 1f);

            Point2f transformed = new Point2f(
                maskFeatures.Center.X + maskFeatures.AxisX.X * localX * sx + maskFeatures.AxisY.X * localY * sy,
                maskFeatures.Center.Y + maskFeatures.AxisX.Y * localX * sx + maskFeatures.AxisY.Y * localY * sy);

            return new PointF(transformed.X, transformed.Y);
        }

        private PointF TransformByBoundaryLocation(ShapeFeatures maskFeatures, ShapePointLocation loc)
        {
            bool usedBoundary;
            return TransformByBoundaryLocation(maskFeatures, loc, BuildBoundarySamples(maskFeatures), out usedBoundary);
        }

        private PointF TransformByBoundaryLocation(
            ShapeFeatures maskFeatures,
            ShapePointLocation loc,
            List<BoundarySample> samples,
            out bool usedBoundary)
        {
            usedBoundary = false;
            BoundarySample sample;
            if (TryGetBoundarySampleAtT(samples, loc.T, out sample))
            {
                float sideRatio = Clamp(GetPointSideRatioForShoe(maskFeatures, loc.SideRatio), -1.2f, 1.2f);
                float u = (sideRatio + 1f) / 2f;
                PointF mapped = Lerp(sample.Left, sample.Right, u);

                if (maskFeatures.BinaryMask == null || maskFeatures.BinaryMask.Empty())
                {
                    usedBoundary = true;
                    return mapped;
                }

                PointF inside = SnapInside(maskFeatures.BinaryMask, mapped);
                int x = (int)Math.Round(inside.X);
                int y = (int)Math.Round(inside.Y);
                if (IsInside(maskFeatures.BinaryMask, x, y))
                {
                    usedBoundary = true;
                    return inside;
                }
            }

            return TransformByShapeLocation(maskFeatures, loc);
        }

        private static List<BoundarySample> BuildBoundarySamples(ShapeFeatures features)
        {
            var samples = new List<BoundarySample>();
            Mat mask = features.BinaryMask;
            if (mask == null || mask.Empty())
            {
                return samples;
            }

            const int sampleCount = 100;
            List<ProjectedBoundaryPoint> boundaryPoints = ProjectBoundaryPoints(features, GetBoundaryPoints(mask));
            if (boundaryPoints.Count == 0)
            {
                return samples;
            }

            var leftPoints = new List<ProjectedBoundaryPoint>();
            var rightPoints = new List<ProjectedBoundaryPoint>();
            foreach (ProjectedBoundaryPoint point in boundaryPoints)
            {
                if (point.Side < 0f)
                {
                    leftPoints.Add(point);
                }
                else
                {
                    rightPoints.Add(point);
                }
            }

            List<ContourSample> leftContour = BuildContourSamples(leftPoints, true, sampleCount);
            List<ContourSample> rightContour = BuildContourSamples(rightPoints, false, sampleCount);
            if (leftContour.Count < 2 || rightContour.Count < 2)
            {
                return samples;
            }

            return BuildMaxPairSamples(leftContour, rightContour);
        }

        private static List<ProjectedBoundaryPoint> ProjectBoundaryPoints(ShapeFeatures features, List<CvPoint> boundaryPoints)
        {
            var result = new List<ProjectedBoundaryPoint>();
            float minAlong = -features.Width / 2f;
            float span = Math.Max(features.Width, 1f);

            foreach (CvPoint point in boundaryPoints)
            {
                var rel = new Point2f(
                    point.X - features.Center.X,
                    point.Y - features.Center.Y);
                float along = ShapeModel.Dot(rel, features.AxisX);
                float side = ShapeModel.Dot(rel, features.AxisY);
                float t = Clamp((along - minAlong) / span, 0f, 1f);

                result.Add(new ProjectedBoundaryPoint
                {
                    T = t,
                    Side = side,
                    Point = new PointF(point.X, point.Y)
                });
            }

            return result;
        }

        private static List<ContourSample> BuildContourSamples(List<ProjectedBoundaryPoint> contourPoints, bool isLeft, int sampleCount)
        {
            var result = new List<ContourSample>();
            if (contourPoints == null || contourPoints.Count == 0)
            {
                return result;
            }

            contourPoints.Sort((a, b) => a.T.CompareTo(b.T));
            float[] tolerances = { 0.015f, 0.025f, 0.04f, 0.06f, 0.09f };
            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount == 1 ? 0f : i / (float)(sampleCount - 1);
                ContourSample sample;
                if (TryBuildContourSample(contourPoints, t, tolerances, isLeft, out sample))
                {
                    result.Add(sample);
                }
            }

            return result;
        }

        private static bool TryBuildContourSample(
            List<ProjectedBoundaryPoint> contourPoints,
            float targetT,
            float[] tolerances,
            bool isLeft,
            out ContourSample sample)
        {
            foreach (float tolerance in tolerances)
            {
                ProjectedBoundaryPoint best = null;
                foreach (ProjectedBoundaryPoint point in contourPoints)
                {
                    if (Math.Abs(point.T - targetT) > tolerance)
                    {
                        continue;
                    }

                    if (best == null ||
                        (isLeft && point.Side < best.Side) ||
                        (!isLeft && point.Side > best.Side))
                    {
                        best = point;
                    }
                }

                if (best != null)
                {
                    sample = new ContourSample
                    {
                        T = targetT,
                        Side = best.Side,
                        Point = best.Point
                    };
                    return true;
                }
            }

            sample = null;
            return false;
        }

        private static bool TryGetContourSampleAtT(List<ContourSample> samples, float t, out ContourSample sample)
        {
            sample = null;
            if (samples == null || samples.Count < 2)
            {
                return false;
            }

            if (t <= samples[0].T)
            {
                sample = samples[0];
                return true;
            }

            ContourSample last = samples[samples.Count - 1];
            if (t >= last.T)
            {
                sample = last;
                return true;
            }

            for (int i = 0; i < samples.Count - 1; i++)
            {
                ContourSample a = samples[i];
                ContourSample b = samples[i + 1];
                if (t < a.T || t > b.T)
                {
                    continue;
                }

                float denom = Math.Max(b.T - a.T, 0.0001f);
                float u = (t - a.T) / denom;
                sample = new ContourSample
                {
                    T = t,
                    Side = a.Side + (b.Side - a.Side) * u,
                    Point = Lerp(a.Point, b.Point, u)
                };
                return true;
            }

            return false;
        }

        private static List<BoundarySample> BuildMaxPairSamples(List<ContourSample> leftContour, List<ContourSample> rightContour)
        {
            var result = new List<BoundarySample>();
            List<ContourSample> leftMaxes = FindContourMaxes(leftContour, true);
            List<ContourSample> rightMaxes = FindContourMaxes(rightContour, false);
            int pairCount = Math.Min(leftMaxes.Count, rightMaxes.Count);
            if (pairCount < 2)
            {
                return result;
            }

            leftMaxes.Sort((a, b) => a.T.CompareTo(b.T));
            rightMaxes.Sort((a, b) => a.T.CompareTo(b.T));
            for (int i = 0; i < pairCount; i++)
            {
                ContourSample left = leftMaxes[i];
                ContourSample right = rightMaxes[i];
                result.Add(new BoundarySample
                {
                    T = (left.T + right.T) / 2f,
                    Left = left.Point,
                    Right = right.Point,
                    Center = MidPoint(left.Point, right.Point),
                    Width = Distance(left.Point, right.Point)
                });
            }

            result.Sort((a, b) => a.T.CompareTo(b.T));
            return result;
        }

        private static List<ContourSample> FindContourMaxes(List<ContourSample> contour, bool isLeft)
        {
            var candidates = new List<ContourSample>();
            if (contour == null || contour.Count == 0)
            {
                return candidates;
            }

            for (int i = 1; i < contour.Count - 1; i++)
            {
                ContourSample prev = contour[i - 1];
                ContourSample current = contour[i];
                ContourSample next = contour[i + 1];
                bool isMax = isLeft
                    ? current.Side <= prev.Side && current.Side <= next.Side
                    : current.Side >= prev.Side && current.Side >= next.Side;
                if (isMax)
                {
                    AddSeparatedMax(candidates, current);
                }
            }

            if (candidates.Count < 2)
            {
                foreach (ContourSample sample in contour)
                {
                    AddSeparatedMax(candidates, sample);
                }
            }

            candidates.Sort((a, b) => Math.Abs(b.Side).CompareTo(Math.Abs(a.Side)));
            if (candidates.Count > 2)
            {
                candidates.RemoveRange(2, candidates.Count - 2);
            }

            return candidates;
        }

        private static void AddSeparatedMax(List<ContourSample> candidates, ContourSample sample)
        {
            const float minTGap = 0.18f;
            for (int i = 0; i < candidates.Count; i++)
            {
                ContourSample existing = candidates[i];
                if (Math.Abs(existing.T - sample.T) > minTGap)
                {
                    continue;
                }

                if (Math.Abs(sample.Side) > Math.Abs(existing.Side))
                {
                    candidates[i] = sample;
                }

                return;
            }

            candidates.Add(sample);
        }

        private static bool TryGetBoundarySampleAtT(List<BoundarySample> samples, float t, out BoundarySample sample)
        {
            sample = null;
            if (samples == null || samples.Count < 2)
            {
                return false;
            }

            if (t < samples[0].T)
            {
                return false;
            }

            BoundarySample last = samples[samples.Count - 1];
            if (t > last.T)
            {
                return false;
            }

            for (int i = 0; i < samples.Count - 1; i++)
            {
                BoundarySample a = samples[i];
                BoundarySample b = samples[i + 1];
                if (t < a.T || t > b.T)
                {
                    continue;
                }

                float denom = Math.Max(b.T - a.T, 0.0001f);
                float u = (t - a.T) / denom;
                sample = new BoundarySample
                {
                    T = t,
                    Left = Lerp(a.Left, b.Left, u),
                    Right = Lerp(a.Right, b.Right, u),
                    Center = Lerp(a.Center, b.Center, u),
                    Width = a.Width + (b.Width - a.Width) * u
                };
                return true;
            }

            return false;
        }

        private static ShapeFeatures ApplyMaxPairBallLines(ShapeFeatures source, List<BoundarySample> maxPairSamples)
        {
            if (maxPairSamples == null || maxPairSamples.Count < 2)
            {
                return source;
            }

            maxPairSamples.Sort((a, b) => a.T.CompareTo(b.T));
            BoundarySample front = maxPairSamples[0];
            BoundarySample rear = maxPairSamples[1];
            ShapeFeatures result = WithAxisSigns(source, 1, 1);
            result.FrontBallLineStart = ToPoint2f(front.Left);
            result.FrontBallLineEnd = ToPoint2f(front.Right);
            result.RearBallLineStart = ToPoint2f(rear.Left);
            result.RearBallLineEnd = ToPoint2f(rear.Right);
            result.FrontBallWidth = front.Width;
            result.RearBallWidth = rear.Width;
            return result;
        }

        private static Point2f ToPoint2f(PointF point)
        {
            return new Point2f(point.X, point.Y);
        }

        private PointF TransformByShapeLocation(ShapeFeatures maskFeatures, ShapePointLocation loc)
        {
            float minAlong = -maskFeatures.Width / 2f;
            float maxAlong = maskFeatures.Width / 2f;
            float targetAlong = minAlong + loc.T * (maxAlong - minAlong);
            float sideRatio = GetPointSideRatioForShoe(maskFeatures, loc.SideRatio);

            float minSide;
            float maxSide;
            float targetSide;
            if (FindMaskCrossSectionRangeRobust(maskFeatures, targetAlong, out minSide, out maxSide))
            {
                float centerSide = (minSide + maxSide) / 2f;
                float halfSide = Math.Max((maxSide - minSide) / 2f, 1f);
                targetSide = centerSide + sideRatio * halfSide;
            }
            else
            {
                targetSide = sideRatio * Math.Max(maskFeatures.Height / 2f, 1f);
            }

            PointF mapped = new PointF(
                maskFeatures.Center.X + maskFeatures.AxisX.X * targetAlong + maskFeatures.AxisY.X * targetSide,
                maskFeatures.Center.Y + maskFeatures.AxisX.Y * targetAlong + maskFeatures.AxisY.Y * targetSide);

            if (maskFeatures.BinaryMask != null)
            {
                mapped = SnapInside(maskFeatures.BinaryMask, mapped);
            }

            return mapped;
        }

        private static float GetPointSideRatioForShoe(ShapeFeatures maskFeatures, float referenceSideRatio)
        {
            // The reference shape is a right-foot drawing. For RIGHT masks we want visual rotation only;
            // because image Y grows downward, the cross-axis sign is opposite when projected on the mask.
            if (string.Equals(maskFeatures.ShoeSide, "RIGHT", StringComparison.OrdinalIgnoreCase))
            {
                return -referenceSideRatio;
            }

            return referenceSideRatio;
        }

        private static bool FindMaskCrossSectionRangeRobust(ShapeFeatures features, float targetAlong, out float minSide, out float maxSide)
        {
            float[] tolerances =
            {
                Math.Max(features.Width * 0.015f, 2f),
                Math.Max(features.Width * 0.025f, 3f),
                Math.Max(features.Width * 0.040f, 4f),
                Math.Max(features.Width * 0.060f, 5f)
            };

            foreach (float tolerance in tolerances)
            {
                if (FindMaskCrossSectionRange(features, targetAlong, tolerance, out minSide, out maxSide))
                {
                    return true;
                }
            }

            minSide = 0f;
            maxSide = 0f;
            return false;
        }

        private static bool FindMaskCrossSectionRange(ShapeFeatures features, float targetAlong, float tolerance, out float minSide, out float maxSide)
        {
            minSide = float.MaxValue;
            maxSide = float.MinValue;

            Mat mask = features.BinaryMask;
            if (mask == null || mask.Empty())
            {
                return false;
            }

            for (int y = 0; y < mask.Rows; y++)
            {
                for (int x = 0; x < mask.Cols; x++)
                {
                    if (mask.At<byte>(y, x) == 0)
                    {
                        continue;
                    }

                    var rel = new Point2f(
                        x - features.Center.X,
                        y - features.Center.Y);

                    float along = ShapeModel.Dot(rel, features.AxisX);
                    if (Math.Abs(along - targetAlong) > tolerance)
                    {
                        continue;
                    }

                    float side = ShapeModel.Dot(rel, features.AxisY);
                    minSide = Math.Min(minSide, side);
                    maxSide = Math.Max(maxSide, side);
                }
            }

            return minSide < maxSide;
        }

        private static PointF Lerp(PointF a, PointF b, float u)
        {
            return new PointF(
                a.X + (b.X - a.X) * u,
                a.Y + (b.Y - a.Y) * u);
        }

        private static PointF MidPoint(PointF a, PointF b)
        {
            return new PointF(
                (a.X + b.X) / 2f,
                (a.Y + b.Y) / 2f);
        }

        private static float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static PointF SnapInside(Mat mask, PointF p)
        {
            // 계산된 대응점이 흰색 마스크 밖으로 나가면 가장 가까운 마스크 내부 픽셀로 끌어오는 보정 함수이다.
            // 보정 범위를 늘리거나 줄이고 싶으면 아래 radius 최대값을 조절하면 된다.
            int x = (int)Math.Round(p.X);
            int y = (int)Math.Round(p.Y);
            if (IsInside(mask, x, y))
            {
                return p;
            }

            int bestX = x;
            int bestY = y;
            int bestDistance = int.MaxValue;
            for (int radius = 1; radius <= 80; radius++)
            {
                int minX = Math.Max(0, x - radius);
                int maxX = Math.Min(mask.Cols - 1, x + radius);
                int minY = Math.Max(0, y - radius);
                int maxY = Math.Min(mask.Rows - 1, y + radius);

                for (int yy = minY; yy <= maxY; yy++)
                {
                    for (int xx = minX; xx <= maxX; xx++)
                    {
                        if (xx != minX && xx != maxX && yy != minY && yy != maxY)
                        {
                            continue;
                        }

                        if (!IsInside(mask, xx, yy))
                        {
                            continue;
                        }

                        int dx = xx - x;
                        int dy = yy - y;
                        int distance = dx * dx + dy * dy;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestX = xx;
                            bestY = yy;
                        }
                    }
                }

                if (bestDistance != int.MaxValue)
                {
                    return new PointF(bestX, bestY);
                }
            }

            return p;
        }

        private static bool IsInside(Mat mask, int x, int y)
        {
            return x >= 0 && y >= 0 && x < mask.Cols && y < mask.Rows && mask.At<byte>(y, x) != 0;
        }
    }
}
