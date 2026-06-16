using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace MidTermproject_21100210
{
    public sealed class MaskAnalyzer
    {
        // UI??Island box ?몃옓諛?媛믪씠 ?ш린???ㅼ뼱?⑤떎.
        // 媛????蹂몄껜 component瑜??쒖쇅?섍퀬, bounding box ?ш린媛 ??媛??댁긽???ъ씠 ?섎굹?쇰룄 ?덉쑝硫?BAD濡?蹂????덈떎.
        public int DefectThreshold { get; set; } = 16;

        public ShapeFeatures Analyze(string imagePath)
        {
            return Analyze(imagePath, false);
        }

        public ShapeFeatures Analyze(string imagePath, bool repairIslands)
        {
            Mat source = Cv2.ImRead(imagePath, ImreadModes.Unchanged);
            if (source.Empty())
            {
                return Bad(Path.GetFileName(imagePath), "file read failed");
            }

            Mat gray = ToBinary(source);
            IslandInfo islandInfo = AnalyzeIslandComponents(gray, DefectThreshold);
            Mat cleaned = new Mat();
            // ??대굹???뉗? 遺遺꾩씠 ?덈Т 留롮씠/?곴쾶 ?쒓굅?섎㈃ ??而ㅻ꼸 ?ш린瑜?議곗젅?섎㈃ ?쒕떎.
            // Open? ?뉗? ?뚯텧遺? ?몄씠利덈? ?쒓굅?섍퀬, Close???쒓굅 ???묒? ?딄??대굹 援щ찉??硫붿슫??
            Mat spikeKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(9, 9));
            Mat smoothKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(7, 7));
            Cv2.MorphologyEx(gray, cleaned, MorphTypes.Open, spikeKernel);
            Cv2.MorphologyEx(cleaned, cleaned, MorphTypes.Close, smoothKernel);

            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            int count = Cv2.ConnectedComponentsWithStats(cleaned, labels, stats, centroids, PixelConnectivity.Connectivity8, MatType.CV_32S);

            if (count <= 1)
            {
                source.Dispose();
                gray.Dispose();
                cleaned.Dispose();
                labels.Dispose();
                stats.Dispose();
                centroids.Dispose();
                spikeKernel.Dispose();
                smoothKernel.Dispose();
                return Bad(Path.GetFileName(imagePath), "no component");
            }

            int bestLabel = 1;
            int bestArea = 0;
            int totalArea = 0;
            for (int i = 1; i < count; i++)
            {
                int area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);
                totalArea += area;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestLabel = i;
                }
            }

            Mat component = new Mat();
            Cv2.Compare(labels, bestLabel, component, CmpTypes.EQ);
            var points = new List<Point2f>(bestArea);
            for (int y = 0; y < component.Rows; y++)
            {
                for (int x = 0; x < component.Cols; x++)
                {
                    if (component.At<byte>(y, x) != 0)
                    {
                        points.Add(new Point2f(x, y));
                    }
                }
            }

            ShapeFeatures features = BuildFeatures(points, component, Path.GetFileName(imagePath));
            features.ImageWidth = source.Width;
            features.ImageHeight = source.Height;
            features.MainComponentRatio = totalArea > 0 ? (double)bestArea / totalArea : 0;
            features.IslandCount = islandInfo.Count;
            features.IslandThreshold = DefectThreshold;
            features.IslandScore = islandInfo.Score;
            features.DistortionScore = CalculateDistortionScore(component);
            features.DistortionDefectScore = NormalizeDistortionScore(features.DistortionScore);
            features.LocalDefectScore = CalculateLocalDefectScore(component);
            features.DefectScore = Math.Max(features.IslandScore, Math.Max(features.DistortionDefectScore, features.LocalDefectScore));
            features.DefectThreshold = DefectThreshold;
            if (repairIslands)
            {
                features.MainComponentRatio = 1.0;
                features.IslandCount = 0;
                features.IslandScore = 0;
                features.DefectScore = Math.Max(features.DistortionDefectScore, features.LocalDefectScore);
                features.BadReason = "repaired: largest component only";
            }
            else
            {
                ApplyBadMaskRules(features, source.Width, source.Height);
            }
            features.QualityScore = CalculateQualityScore(features, source.Width, source.Height);

            source.Dispose();
            gray.Dispose();
            cleaned.Dispose();
            labels.Dispose();
            stats.Dispose();
            centroids.Dispose();
            spikeKernel.Dispose();
            smoothKernel.Dispose();
            return features;
        }

        private static Mat ToBinary(Mat source)
        {
            Mat gray = new Mat();
            if (source.Channels() == 4)
            {
                Mat[] channels = Cv2.Split(source);
                Mat alpha = channels[3];
                Mat bgr = new Mat();
                Mat alphaMask = new Mat();
                Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(gray, gray, 30, 255, ThresholdTypes.Binary);
                Cv2.Threshold(alpha, alphaMask, 30, 255, ThresholdTypes.Binary);
                if (Cv2.CountNonZero(alphaMask) < alphaMask.Rows * alphaMask.Cols)
                {
                    Cv2.BitwiseAnd(gray, alphaMask, gray);
                }
                foreach (Mat c in channels)
                {
                    if (!ReferenceEquals(c, alpha))
                    {
                        c.Dispose();
                    }
                }
                alpha.Dispose();
                alphaMask.Dispose();
                bgr.Dispose();
            }
            else if (source.Channels() == 3)
            {
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(gray, gray, 30, 255, ThresholdTypes.Binary);
            }
            else
            {
                Cv2.Threshold(source, gray, 30, 255, ThresholdTypes.Binary);
            }

            return gray;
        }

        private static ShapeFeatures BuildFeatures(List<Point2f> points, Mat mask, string label)
        {
            // 留덉뒪?ъ쓽 ?뺤긽???섏튂?뷀븯???듭떖 遺遺꾩씠??
            // 以묒떖, PCA 異? ???믪씠, bounding box, ?욎퐫 諛⑺뼢 異붿젙媛믪쓣 ?ш린??怨꾩궛?쒕떎.
            double cx = 0;
            double cy = 0;
            foreach (Point2f p in points)
            {
                cx += p.X;
                cy += p.Y;
            }
            cx /= Math.Max(points.Count, 1);
            cy /= Math.Max(points.Count, 1);

            double sxx = 0;
            double syy = 0;
            double sxy = 0;
            int minImageX = int.MaxValue;
            int maxImageX = int.MinValue;
            int minImageY = int.MaxValue;
            int maxImageY = int.MinValue;

            foreach (Point2f p in points)
            {
                double dx = p.X - cx;
                double dy = p.Y - cy;
                sxx += dx * dx;
                syy += dy * dy;
                sxy += dx * dy;
                minImageX = Math.Min(minImageX, (int)p.X);
                maxImageX = Math.Max(maxImageX, (int)p.X);
                minImageY = Math.Min(minImageY, (int)p.Y);
                maxImageY = Math.Max(maxImageY, (int)p.Y);
            }

            double theta = 0.5 * Math.Atan2(2.0 * sxy, sxx - syy);
            var axisX = new Point2f((float)Math.Cos(theta), (float)Math.Sin(theta));
            var axisY = new Point2f(-axisX.Y, axisX.X);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (Point2f p in points)
            {
                var relative = new Point2f((float)(p.X - cx), (float)(p.Y - cy));
                float px = ShapeModel.Dot(relative, axisX);
                float py = ShapeModel.Dot(relative, axisY);
                minX = Math.Min(minX, px);
                maxX = Math.Max(maxX, px);
                minY = Math.Min(minY, py);
                maxY = Math.Max(maxY, py);
            }

            var center = new Point2f((float)cx, (float)cy);
            EndProfile positiveEnd = EstimateEndProfile(points, center, axisX, axisY, 1);
            EndProfile negativeEnd = EstimateEndProfile(points, center, axisX, axisY, -1);
            double positiveEndWidth = positiveEnd.Width;
            double negativeEndWidth = negativeEnd.Width;

            double axisAngle = NormalizeAngleDegrees(Math.Atan2(axisX.Y, axisX.X) * 180.0 / Math.PI);
            bool sidewaysForPerspective = Math.Abs(Math.Abs(axisAngle) - 90.0) > 30.0;
            double positiveCompareWidth = positiveEnd.CompareWidth;
            double negativeCompareWidth = negativeEnd.CompareWidth;
            if (sidewaysForPerspective)
            {
                positiveCompareWidth = CorrectWidthForPerspective(positiveCompareWidth, positiveEnd.CenterY, minImageY, maxImageY);
                negativeCompareWidth = CorrectWidthForPerspective(negativeCompareWidth, negativeEnd.CenterY, minImageY, maxImageY);
            }

            int rawFrontSign = positiveCompareWidth >= negativeCompareWidth ? 1 : -1;
            double frontConfidence = Math.Abs(positiveCompareWidth - negativeCompareWidth) / Math.Max(Math.Max(positiveCompareWidth, negativeCompareWidth), 1.0);

            // The front side is decided from ball width, not from user point numbers.
            // ShapeModel uses T=0 near the toe and T=1 near the heel, so AxisX is normalized toe-to-heel.
            axisX = Multiply(axisX, -rawFrontSign);
            axisY = Multiply(axisY, -rawFrontSign);
            const int frontSignOnAxis = -1;
            Point2f frontAxis = Multiply(axisX, frontSignOnAxis);

            Tuple<int, double> innerSide = EstimateInnerSide(points, center, axisX, axisY, frontSignOnAxis);
            // Keep the pure rotation coordinate system for RIGHT masks.
            // Flipping AxisY here acts like a mirror and places the reference right-foot points like a left foot.
            AxisLineSet axisLines = BuildAxisLines(points, center, axisX, axisY, frontSignOnAxis);
            double rotationAngle = NormalizeAngleDegrees(Math.Atan2(frontAxis.Y, frontAxis.X) * 180.0 / Math.PI);
            string shoeSide = ClassifyShoeSideWithFallback(frontAxis, axisY, innerSide.Item1, innerSide.Item2, frontConfidence, rotationAngle);

            return new ShapeFeatures
            {
                Label = label,
                Center = new Point2f((float)cx, (float)cy),
                AxisX = axisX,
                AxisY = axisY,
                Width = Math.Max(maxX - minX, 1f),
                Height = Math.Max(maxY - minY, 1f),
                BoundingBox = new Rect(minImageX, minImageY, Math.Max(maxImageX - minImageX + 1, 1), Math.Max(maxImageY - minImageY + 1, 1)),
                Area = points.Count,
                BinaryMask = mask.Clone(),
                FrontSign = frontSignOnAxis,
                FrontConfidence = frontConfidence,
                PredFrontSign = rawFrontSign,
                PositiveEndWidth = positiveEndWidth,
                NegativeEndWidth = negativeEndWidth,
                FrontWidth = Math.Max(positiveEndWidth, negativeEndWidth),
                RearWidth = Math.Min(positiveEndWidth, negativeEndWidth),
                InnerSign = innerSide.Item1,
                InnerConfidence = innerSide.Item2,
                ShoeSide = shoeSide,
                RotationAngleDegrees = rotationAngle,
                FrontBallWidth = axisLines.FrontWidth,
                RearBallWidth = axisLines.RearWidth,
                CenterLineStart = axisLines.CenterStart,
                CenterLineEnd = axisLines.CenterEnd,
                FrontBallLineStart = axisLines.FrontStart,
                FrontBallLineEnd = axisLines.FrontEnd,
                RearBallLineStart = axisLines.RearStart,
                RearBallLineEnd = axisLines.RearEnd
            };
        }

        private static IslandInfo AnalyzeIslandComponents(Mat binary, int defectThreshold)
        {
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            int count = Cv2.ConnectedComponentsWithStats(binary, labels, stats, centroids, PixelConnectivity.Connectivity8, MatType.CV_32S);

            int bestLabel = -1;
            int bestArea = 0;
            for (int i = 1; i < count; i++)
            {
                int area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);
                if (area > bestArea)
                {
                    bestArea = area;
                    bestLabel = i;
                }
            }

            int islands = 0;
            double maxIslandBox = 0;
            for (int i = 1; i < count; i++)
            {
                if (i == bestLabel)
                {
                    continue;
                }

                int componentWidth = stats.Get<int>(i, (int)ConnectedComponentsTypes.Width);
                int componentHeight = stats.Get<int>(i, (int)ConnectedComponentsTypes.Height);
                int box = Math.Max(componentWidth, componentHeight);
                maxIslandBox = Math.Max(maxIslandBox, box);
                if (box >= defectThreshold)
                {
                    islands++;
                }
            }

            labels.Dispose();
            stats.Dispose();
            centroids.Dispose();
            return new IslandInfo
            {
                Count = islands,
                Score = maxIslandBox
            };
        }

        private static double CalculateDistortionScore(Mat component)
        {
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(component, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            if (contours == null || contours.Length == 0)
            {
                return 100.0;
            }

            Point[] bestContour = contours
                .OrderByDescending(c => Math.Abs(Cv2.ContourArea(c)))
                .First();

            double contourArea = Math.Abs(Cv2.ContourArea(bestContour));
            if (contourArea <= 1.0)
            {
                return 100.0;
            }

            Point[] hull = Cv2.ConvexHull(bestContour);
            double hullArea = Math.Abs(Cv2.ContourArea(hull));
            if (hullArea <= 1.0)
            {
                return 100.0;
            }

            double distortion = (1.0 - contourArea / hullArea) * 100.0;
            return Math.Max(0.0, Math.Min(100.0, distortion));
        }

        private static double NormalizeDistortionScore(double rawDistortionScore)
        {
            // Normal shoe silhouettes already have natural concavity, so raw hull difference is not a defect by itself.
            double excess = rawDistortionScore - 22.0;
            if (excess <= 0)
            {
                return 0.0;
            }

            return Math.Min(100.0, excess * 3.0);
        }

        private static double CalculateLocalDefectScore(Mat component)
        {
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(15, 15));
            Mat closed = new Mat();
            Mat defectMask = new Mat();
            Cv2.MorphologyEx(component, closed, MorphTypes.Close, kernel);
            Cv2.Subtract(closed, component, defectMask);

            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            int count = Cv2.ConnectedComponentsWithStats(defectMask, labels, stats, centroids, PixelConnectivity.Connectivity8, MatType.CV_32S);

            double bestScore = 0.0;
            for (int i = 1; i < count; i++)
            {
                int area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);
                int width = stats.Get<int>(i, (int)ConnectedComponentsTypes.Width);
                int height = stats.Get<int>(i, (int)ConnectedComponentsTypes.Height);
                double box = Math.Max(width, height);
                if (area < 8)
                {
                    continue;
                }

                double score = box + Math.Sqrt(Math.Max(area, 0)) * 0.5;
                if (score > bestScore)
                {
                    bestScore = score;
                }
            }

            kernel.Dispose();
            closed.Dispose();
            defectMask.Dispose();
            labels.Dispose();
            stats.Dispose();
            centroids.Dispose();
            return Math.Min(100.0, bestScore);
        }

        private static AxisLineSet BuildAxisLines(List<Point2f> points, Point2f center, Point2f axisLong, Point2f axisCross, int frontSign)
        {
            var orientedLongs = new List<float>();
            foreach (Point2f p in points)
            {
                var relative = new Point2f(p.X - center.X, p.Y - center.Y);
                orientedLongs.Add(ShapeModel.Dot(relative, axisLong) * frontSign);
            }

            orientedLongs.Sort();
            float minO = orientedLongs.First();
            float maxO = orientedLongs.Last();
            float span = Math.Max(maxO - minO, 1f);

            CrossLine front = BuildCrossLine(points, center, axisLong, axisCross, frontSign, minO + span * 0.68f, span * 0.045f);
            CrossLine rear = BuildCrossLine(points, center, axisLong, axisCross, frontSign, minO + span * 0.34f, span * 0.045f);

            Point2f centerStart = Add(center, Multiply(axisLong, minO * frontSign));
            Point2f centerEnd = Add(center, Multiply(axisLong, maxO * frontSign));

            return new AxisLineSet
            {
                CenterStart = centerStart,
                CenterEnd = centerEnd,
                FrontStart = front.Start,
                FrontEnd = front.End,
                RearStart = rear.Start,
                RearEnd = rear.End,
                FrontWidth = front.Width,
                RearWidth = rear.Width
            };
        }

        private static CrossLine BuildCrossLine(List<Point2f> points, Point2f center, Point2f axisLong, Point2f axisCross, int frontSign, float targetO, float tolerance)
        {
            float minSide = float.MaxValue;
            float maxSide = float.MinValue;
            foreach (Point2f p in points)
            {
                var relative = new Point2f(p.X - center.X, p.Y - center.Y);
                float orientedLong = ShapeModel.Dot(relative, axisLong) * frontSign;
                if (Math.Abs(orientedLong - targetO) > tolerance)
                {
                    continue;
                }

                float side = ShapeModel.Dot(relative, axisCross);
                minSide = Math.Min(minSide, side);
                maxSide = Math.Max(maxSide, side);
            }

            if (minSide == float.MaxValue)
            {
                minSide = -1;
                maxSide = 1;
            }

            Point2f lineCenter = Add(center, Multiply(axisLong, targetO * frontSign));
            return new CrossLine
            {
                Start = Add(lineCenter, Multiply(axisCross, minSide)),
                End = Add(lineCenter, Multiply(axisCross, maxSide)),
                Width = Math.Max(maxSide - minSide, 0)
            };
        }

        private static Point2f Add(Point2f a, Point2f b)
        {
            return new Point2f(a.X + b.X, a.Y + b.Y);
        }

        private static Point2f Multiply(Point2f a, float scale)
        {
            return new Point2f(a.X * scale, a.Y * scale);
        }

        private static EndProfile EstimateEndProfile(List<Point2f> points, Point2f center, Point2f axisLong, Point2f axisCross, int sign)
        {
            var longValues = new List<float>();
            foreach (Point2f p in points)
            {
                var relative = new Point2f(p.X - center.X, p.Y - center.Y);
                longValues.Add(ShapeModel.Dot(relative, axisLong));
            }

            if (longValues.Count == 0)
            {
                return new EndProfile();
            }

            longValues.Sort();
            int terminalIndex = sign > 0 ? (int)(longValues.Count * 0.86) : (int)(longValues.Count * 0.14);
            int ballStartIndex = sign > 0 ? (int)(longValues.Count * 0.58) : (int)(longValues.Count * 0.26);
            int ballEndIndex = sign > 0 ? (int)(longValues.Count * 0.78) : (int)(longValues.Count * 0.46);
            terminalIndex = Math.Max(0, Math.Min(longValues.Count - 1, terminalIndex));
            ballStartIndex = Math.Max(0, Math.Min(longValues.Count - 1, ballStartIndex));
            ballEndIndex = Math.Max(0, Math.Min(longValues.Count - 1, ballEndIndex));

            float terminalThreshold = longValues[terminalIndex];
            float ballMin = Math.Min(longValues[ballStartIndex], longValues[ballEndIndex]);
            float ballMax = Math.Max(longValues[ballStartIndex], longValues[ballEndIndex]);
            var terminalCrossValues = new List<float>();
            var ballCrossValues = new List<float>();
            double ySum = 0;
            int yCount = 0;

            foreach (Point2f p in points)
            {
                var relative = new Point2f(p.X - center.X, p.Y - center.Y);
                float longProjection = ShapeModel.Dot(relative, axisLong);
                if ((sign > 0 && longProjection >= terminalThreshold) || (sign < 0 && longProjection <= terminalThreshold))
                {
                    terminalCrossValues.Add(ShapeModel.Dot(relative, axisCross));
                    ySum += p.Y;
                    yCount++;
                }

                if (longProjection >= ballMin && longProjection <= ballMax)
                {
                    ballCrossValues.Add(ShapeModel.Dot(relative, axisCross));
                }
            }

            if (terminalCrossValues.Count == 0)
            {
                return new EndProfile();
            }

            double terminalWidth = terminalCrossValues.Max() - terminalCrossValues.Min();
            double ballWidth = ballCrossValues.Count > 0 ? ballCrossValues.Max() - ballCrossValues.Min() : terminalWidth;

            return new EndProfile
            {
                Width = terminalWidth,
                BallWidth = ballWidth,
                CompareWidth = terminalWidth * 0.75 + ballWidth * 0.25,
                CenterY = yCount > 0 ? ySum / yCount : center.Y
            };
        }

        private static double CorrectWidthForPerspective(double width, double centerY, int minImageY, int maxImageY)
        {
            double span = Math.Max(maxImageY - minImageY, 1);
            double yRatio = (centerY - minImageY) / span;
            yRatio = Math.Max(0.0, Math.Min(1.0, yRatio));
            const double perspectiveStrength = 0.35;
            return width / (1.0 + perspectiveStrength * yRatio);
        }

        private static Tuple<int, double> EstimateInnerSide(List<Point2f> points, Point2f center, Point2f axisLong, Point2f axisCross, int frontSign)
        {
            // ?좊컻 ?덉そ? 蹂댄넻 以묎컙 ?덈━ 遺遺꾩씠 ???ㅼ뼱媛꾨떎.
            // 洹몃옒???욎そ/?ㅼそ ?鍮?以묎컙 side distance媛 ??以꾩뼱?쒕뒗 履쎌쓣 ?덉そ?쇰줈 蹂몃떎.
            double positiveIndent = EstimateArchIndent(points, center, axisLong, axisCross, frontSign, 1);
            double negativeIndent = EstimateArchIndent(points, center, axisLong, axisCross, frontSign, -1);
            int innerSign = positiveIndent >= negativeIndent ? 1 : -1;
            double confidence = Math.Abs(positiveIndent - negativeIndent) / Math.Max(Math.Max(positiveIndent, negativeIndent), 1.0);
            return Tuple.Create(innerSign, confidence);
        }

        private static double EstimateArchIndent(List<Point2f> points, Point2f center, Point2f axisLong, Point2f axisCross, int frontSign, int sideSign)
        {
            var longValues = new List<float>();
            foreach (Point2f p in points)
            {
                var relative = new Point2f(p.X - center.X, p.Y - center.Y);
                longValues.Add(ShapeModel.Dot(relative, axisLong) * frontSign);
            }

            if (longValues.Count == 0)
            {
                return 0;
            }

            longValues.Sort();
            float minLong = longValues.First();
            float maxLong = longValues.Last();
            float span = Math.Max(maxLong - minLong, 1f);

            double frontWidth = EstimateSideDistance(points, center, axisLong, axisCross, frontSign, sideSign, minLong + span * 0.62f, minLong + span * 0.90f);
            double midWidth = EstimateSideDistance(points, center, axisLong, axisCross, frontSign, sideSign, minLong + span * 0.36f, minLong + span * 0.58f);
            double heelWidth = EstimateSideDistance(points, center, axisLong, axisCross, frontSign, sideSign, minLong + span * 0.08f, minLong + span * 0.30f);
            return Math.Max(frontWidth, heelWidth) - midWidth;
        }

        private static double EstimateSideDistance(List<Point2f> points, Point2f center, Point2f axisLong, Point2f axisCross, int frontSign, int sideSign, float minLong, float maxLong)
        {
            double maxSide = 0;
            foreach (Point2f p in points)
            {
                var relative = new Point2f(p.X - center.X, p.Y - center.Y);
                float longProjection = ShapeModel.Dot(relative, axisLong) * frontSign;
                if (longProjection < minLong || longProjection > maxLong)
                {
                    continue;
                }

                double sideProjection = ShapeModel.Dot(relative, axisCross) * sideSign;
                if (sideProjection > maxSide)
                {
                    maxSide = sideProjection;
                }
            }

            return maxSide;
        }

        private static string ClassifyShoeSideWithFallback(Point2f frontAxis, Point2f sideAxis, int innerSign, double innerConfidence, double frontConfidence, double rotationAngle)
        {
            const double minFrontConfidence = 0.20;
            string frontBased = ClassifyShoeSide(frontAxis, sideAxis, innerSign, innerConfidence);

            // When the shoe is almost upright, the original front/inner relation is more reliable.
            // The outer-bulge fallback is only for lying/sideways masks where end-width front estimation is weak.
            double uprightDelta = Math.Abs(Math.Abs(rotationAngle) - 90.0);
            bool sidewaysLike = uprightDelta > 20.0;
            if (frontConfidence < minFrontConfidence && sidewaysLike)
            {
                string bulgeBased = ClassifyShoeSideByOuterBulge(sideAxis, innerSign, innerConfidence);
                if (!string.Equals(bulgeBased, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
                {
                    return bulgeBased;
                }
            }

            return frontBased;
        }

        private static string ClassifyShoeSide(Point2f frontAxis, Point2f sideAxis, int innerSign, double innerConfidence)
        {
            if (innerConfidence < 0.05)
            {
                return "UNKNOWN";
            }

            // Once front/rear is decided, classify by the visible concave inner side.
            // In the mask review view, right-side indentation means RIGHT shoe, left-side indentation means LEFT shoe.
            Point2f innerDirection = Multiply(sideAxis, innerSign);
            if (Math.Abs(innerDirection.X) < 0.08)
            {
                return "UNKNOWN";
            }

            return innerDirection.X > 0 ? "RIGHT" : "LEFT";
        }

        private static string ClassifyShoeSideByOuterBulge(Point2f sideAxis, int innerSign, double innerConfidence)
        {
            if (innerConfidence < 0.05)
            {
                return "UNKNOWN";
            }

            // The outer bulge is opposite the concave inner side.
            // Outer bulge on the left means the concavity is on the right => RIGHT shoe.
            Point2f outerDirection = Multiply(sideAxis, -innerSign);
            if (Math.Abs(outerDirection.X) < 0.08)
            {
                return "UNKNOWN";
            }

            return outerDirection.X < 0 ? "RIGHT" : "LEFT";
        }

        private static double NormalizeAngleDegrees(double degrees)
        {
            while (degrees <= -180.0)
            {
                degrees += 360.0;
            }
            while (degrees > 180.0)
            {
                degrees -= 360.0;
            }
            return degrees;
        }

        private static void ApplyBadMaskRules(ShapeFeatures f, int width, int height)
        {
            // BAD ?먯젙 湲곗???諛붽씀怨??띠쑝硫??ш린???섏젙?섎㈃ ?쒕떎.
            // ?뚯쟾, 醫뚯슦 諛섏쟾, ?욌뮘 諛⑺뼢 臾몄젣??BAD媛 ?꾨땲??ShapeMatcher?먯꽌 泥섎━?섎뒗 ???臾몄젣濡??붾떎.
            if (f.DefectScore >= f.DefectThreshold)
            {
                f.IsBad = true;
                f.BadReason = GetDominantDefectReason(f);
            }
        }

        private static string GetDominantDefectReason(ShapeFeatures f)
        {
            if (f.IslandScore >= f.DistortionScore && f.IslandScore >= f.LocalDefectScore)
            {
                return "large island";
            }
            if (f.LocalDefectScore >= f.DistortionScore)
            {
                return "local defect";
            }
            return "shape distorted";
        }

        private static double CalculateQualityScore(ShapeFeatures f, int width, int height)
        {
            if (f.IsBad)
            {
                return 0;
            }

            double imageArea = width * height;
            double areaRatio = f.Area / imageArea;
            double areaScore = Clamp01(1.0 - Math.Abs(areaRatio - 0.10) / 0.12);
            double componentScore = Clamp01((f.MainComponentRatio - 0.82) / 0.18);
            double boxRatio = f.BoundingBox.Height > 0 ? (double)f.BoundingBox.Width / f.BoundingBox.Height : 0;
            double ratioScore = Clamp01(1.0 - Math.Abs(boxRatio - 0.75) / 1.25);

            return 100.0 * (0.45 * areaScore + 0.35 * componentScore + 0.20 * ratioScore);
        }

        private static double Clamp01(double value)
        {
            if (value < 0)
            {
                return 0;
            }
            if (value > 1)
            {
                return 1;
            }
            return value;
        }

        private static ShapeFeatures Bad(string label, string reason)
        {
            return new ShapeFeatures
            {
                Label = label,
                IsBad = true,
                BadReason = reason,
                QualityScore = 0
            };
        }

        private sealed class EndProfile
        {
            public double Width { get; set; }
            public double BallWidth { get; set; }
            public double CompareWidth { get; set; }
            public double CenterY { get; set; }
        }

        private sealed class AxisLineSet
        {
            public Point2f CenterStart { get; set; }
            public Point2f CenterEnd { get; set; }
            public Point2f FrontStart { get; set; }
            public Point2f FrontEnd { get; set; }
            public Point2f RearStart { get; set; }
            public Point2f RearEnd { get; set; }
            public double FrontWidth { get; set; }
            public double RearWidth { get; set; }
        }

        private sealed class IslandInfo
        {
            public int Count { get; set; }
            public double Score { get; set; }
        }

        private sealed class CrossLine
        {
            public Point2f Start { get; set; }
            public Point2f End { get; set; }
            public double Width { get; set; }
        }
    }
}








