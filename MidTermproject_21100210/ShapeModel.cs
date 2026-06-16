using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using OpenCvSharp;

namespace MidTermproject_21100210
{
    public sealed class ShapeModel
    {
        public PointF[] OutlinePoints { get; private set; }
        public PointF[] MeasurementPoints { get; private set; }
        public ShapeFeatures Features { get; private set; }

        public ShapeModel()
        {
            OutlinePoints = CreateOutlinePoints();
            Features = ComputeFeatures(OutlinePoints, "Reference");
            MeasurementPoints = CreateDefaultMeasurementPoints();
        }

        public string GetMeasurementInfo()
        {
            var lines = new List<string>();
            for (int i = 0; i < MeasurementPoints.Length; i++)
            {
                PointF p = MeasurementPoints[i];
                PointF n = ToNormalizedReference(p);
                ShapePointLocation loc = ToShapeLocation(p);
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "P{0}: model=({1:0.0}, {2:0.0})  normalized=({3:0.000}, {4:0.000})  T={5:0.000}, SideRatio={6:0.000}",
                    i + 1, p.X, p.Y, n.X, n.Y, loc.T, loc.SideRatio));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public PointF ToNormalizedReference(PointF p)
        {
            Point2f center = Features.Center;
            Point2f relative = new Point2f(p.X - center.X, p.Y - center.Y);
            float nx = Dot(relative, Features.AxisX) / Math.Max(Features.Width / 2f, 1f);
            float ny = Dot(relative, Features.AxisY) / Math.Max(Features.Height / 2f, 1f);
            return new PointF(nx, ny);
        }

        public ShapePointLocation ToShapeLocation(PointF p)
        {
            ShapeFeatures reference = Features;
            var rel = new Point2f(
                p.X - reference.Center.X,
                p.Y - reference.Center.Y);

            float along = Dot(rel, reference.AxisX);
            float side = Dot(rel, reference.AxisY);

            float minAlong = -reference.Width / 2f;
            float maxAlong = reference.Width / 2f;
            float t = (along - minAlong) / Math.Max(maxAlong - minAlong, 1f);
            t = Clamp01(t);

            if (!FindReferenceCrossSectionRange(t, out float minSide, out float maxSide))
            {
                float fallbackRatio = side / Math.Max(reference.Height / 2f, 1f);
                fallbackRatio = Math.Max(-1.2f, Math.Min(1.2f, fallbackRatio));

                return new ShapePointLocation
                {
                    T = t,
                    SideRatio = fallbackRatio
                };
            }

            float sectionWidth = maxSide - minSide;
            if (sectionWidth < reference.Height * 0.15f)
            {
                float fallbackRatio = side / Math.Max(reference.Height / 2f, 1f);
                fallbackRatio = Math.Max(-1.2f, Math.Min(1.2f, fallbackRatio));

                return new ShapePointLocation
                {
                    T = t,
                    SideRatio = fallbackRatio
                };
            }

            float centerSide = (minSide + maxSide) / 2f;
            float halfSide = Math.Max(sectionWidth / 2f, 1f);
            float ratio = (side - centerSide) / halfSide;
            ratio = Math.Max(-1.2f, Math.Min(1.2f, ratio));

            return new ShapePointLocation
            {
                T = t,
                SideRatio = ratio
            };
        }

        private PointF[] CreateDefaultMeasurementPoints()
        {
            // 기본 빨간점 6개의 시작 위치를 바꾸고 싶으면 여기 좌표를 수정하면 된다.
            // 이 좌표는 화면 픽셀이 아니라 기준 형상 좌표계의 좌표이다.
            return new[]
            {
                new PointF(0f, 170f),
                new PointF(-58f, 75f),
                new PointF(58f, 75f),
                new PointF(-45f, -100f),
                new PointF(45f, -100f),
                new PointF(0f, -215f)
            };
        }

        private static ShapeFeatures ComputeFeatures(PointF[] points, string label)
        {
            double cx = points.Average(p => p.X);
            double cy = points.Average(p => p.Y);
            double sxx = 0;
            double syy = 0;
            double sxy = 0;

            foreach (PointF p in points)
            {
                double dx = p.X - cx;
                double dy = p.Y - cy;
                sxx += dx * dx;
                syy += dy * dy;
                sxy += dx * dy;
            }

            double theta = 0.5 * Math.Atan2(2.0 * sxy, sxx - syy);
            var axisX = new Point2f((float)Math.Cos(theta), (float)Math.Sin(theta));
            var axisY = new Point2f(-axisX.Y, axisX.X);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float left = points.Min(p => p.X);
            float top = points.Min(p => p.Y);
            float right = points.Max(p => p.X);
            float bottom = points.Max(p => p.Y);

            foreach (PointF p in points)
            {
                var relative = new Point2f((float)(p.X - cx), (float)(p.Y - cy));
                float px = Dot(relative, axisX);
                float py = Dot(relative, axisY);
                minX = Math.Min(minX, px);
                maxX = Math.Max(maxX, px);
                minY = Math.Min(minY, py);
                maxY = Math.Max(maxY, py);
            }

            return new ShapeFeatures
            {
                Label = label,
                Center = new Point2f((float)cx, (float)cy),
                AxisX = axisX,
                AxisY = axisY,
                Width = Math.Max(maxX - minX, 1f),
                Height = Math.Max(maxY - minY, 1f),
                BoundingBox = new Rect((int)Math.Floor(left), (int)Math.Floor(top), (int)Math.Ceiling(right - left), (int)Math.Ceiling(bottom - top)),
                Area = points.Length,
                MainComponentRatio = 1.0
            };
        }

        public static float Dot(Point2f a, Point2f b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private bool FindReferenceCrossSectionRange(float t, out float minSide, out float maxSide)
        {
            ShapeFeatures reference = Features;
            float[] tolerances =
            {
                Math.Max(reference.Width * 0.015f, 2f),
                Math.Max(reference.Width * 0.025f, 3f),
                Math.Max(reference.Width * 0.040f, 4f),
                Math.Max(reference.Width * 0.060f, 5f)
            };

            foreach (float tolerance in tolerances)
            {
                if (FindReferenceCrossSectionRange(t, tolerance, out minSide, out maxSide))
                {
                    return true;
                }
            }

            minSide = 0f;
            maxSide = 0f;
            return false;
        }

        private bool FindReferenceCrossSectionRange(float t, float tolerance, out float minSide, out float maxSide)
        {
            minSide = float.MaxValue;
            maxSide = float.MinValue;

            ShapeFeatures reference = Features;
            float minAlong = -reference.Width / 2f;
            float maxAlong = reference.Width / 2f;
            float targetAlong = minAlong + t * (maxAlong - minAlong);

            foreach (PointF op in OutlinePoints)
            {
                var rel = new Point2f(
                    op.X - reference.Center.X,
                    op.Y - reference.Center.Y);

                float along = Dot(rel, reference.AxisX);
                if (Math.Abs(along - targetAlong) > tolerance)
                {
                    continue;
                }

                float side = Dot(rel, reference.AxisY);
                minSide = Math.Min(minSide, side);
                maxSide = Math.Max(maxSide, side);
            }

            return minSide < maxSide;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }
            if (value > 1f)
            {
                return 1f;
            }
            return value;
        }

        public void SetMeasurementPoint(int index, PointF point)
        {
            if (index < 0 || index >= MeasurementPoints.Length)
            {
                return;
            }

            MeasurementPoints[index] = point;
        }

        private static PointF[] CreateOutlinePoints()
        {
            return new[]
            {
                new PointF(-10.221f, 262.377f), new PointF(-16.148f, 261.365f), new PointF(-22.076f, 260.953f),
                new PointF(-27.57f, 259.507f), new PointF(-33.064f, 258.061f), new PointF(-38.702f, 255.971f),
                new PointF(-43.184f, 253.702f), new PointF(-47.666f, 251.434f), new PointF(-50.847f, 249.076f),
                new PointF(-54.462f, 245.895f), new PointF(-58.076f, 242.714f), new PointF(-61.69f, 238.522f),
                new PointF(-64.871f, 234.618f), new PointF(-68.052f, 230.714f), new PointF(-71.133f, 226.277f),
                new PointF(-73.546f, 222.474f), new PointF(-75.959f, 218.67f), new PointF(-77.327f, 215.989f),
                new PointF(-79.351f, 211.796f), new PointF(-81.375f, 207.604f), new PointF(-83.622f, 203.345f),
                new PointF(-85.69f, 197.317f), new PointF(-87.759f, 191.289f), new PointF(-89.883f, 184.449f),
                new PointF(-91.763f, 175.63f), new PointF(-93.642f, 166.811f), new PointF(-95.956f, 153.221f),
                new PointF(-96.968f, 144.401f), new PointF(-97.98f, 135.582f), new PointF(-97.98f, 132.112f),
                new PointF(-97.835f, 122.714f), new PointF(-97.69f, 113.317f), new PointF(-96.823f, 97.269f),
                new PointF(-96.1f, 88.016f), new PointF(-95.377f, 78.763f), new PointF(-94.799f, 74.281f),
                new PointF(-93.498f, 67.196f), new PointF(-92.196f, 60.112f), new PointF(-90.451f, 52.594f),
                new PointF(-88.293f, 45.51f), new PointF(-86.135f, 38.425f), new PointF(-83.009f, 32.353f),
                new PointF(-80.551f, 24.69f), new PointF(-78.093f, 17.028f), new PointF(-75.581f, 8.498f),
                new PointF(-73.546f, -0.466f), new PointF(-71.511f, -9.43f), new PointF(-69.498f, -19.551f),
                new PointF(-68.341f, -29.093f), new PointF(-67.184f, -38.635f), new PointF(-66.895f, -49.045f),
                new PointF(-66.606f, -57.719f), new PointF(-66.317f, -66.394f), new PointF(-66.028f, -71.599f),
                new PointF(-66.606f, -81.141f), new PointF(-67.184f, -90.683f), new PointF(-69.209f, -104.418f),
                new PointF(-70.076f, -114.972f), new PointF(-70.943f, -125.527f), new PointF(-71.522f, -135.647f),
                new PointF(-71.811f, -144.466f), new PointF(-72.1f, -153.286f), new PointF(-72.1f, -159.792f),
                new PointF(-71.811f, -167.888f), new PointF(-71.522f, -175.984f), new PointF(-71.322f, -185.093f),
                new PointF(-70.076f, -193.045f), new PointF(-68.83f, -200.997f), new PointF(-66.939f, -208.948f),
                new PointF(-64.336f, -215.599f), new PointF(-61.734f, -222.249f), new PointF(-58.565f, -227.599f),
                new PointF(-54.462f, -232.948f), new PointF(-50.358f, -238.298f), new PointF(-45.787f, -243.502f),
                new PointF(-39.715f, -247.695f), new PointF(-33.642f, -251.888f), new PointF(-25.257f, -255.647f),
                new PointF(-18.028f, -258.105f), new PointF(-10.799f, -260.563f), new PointF(-4.293f, -262.442f),
                new PointF(3.659f, -262.442f), new PointF(11.611f, -262.442f), new PointF(21.876f, -261.286f),
                new PointF(29.683f, -258.105f), new PointF(37.49f, -254.924f), new PointF(44.719f, -248.997f),
                new PointF(50.502f, -243.358f), new PointF(56.286f, -237.719f), new PointF(60.578f, -231.169f),
                new PointF(64.382f, -224.274f), new PointF(68.186f, -217.378f), new PointF(71.011f, -212.541f),
                new PointF(73.324f, -201.987f), new PointF(75.637f, -191.433f), new PointF(77.583f, -173.571f),
                new PointF(78.261f, -160.948f), new PointF(78.94f, -148.325f), new PointF(77.828f, -135.358f),
                new PointF(77.394f, -126.25f), new PointF(76.96f, -117.141f), new PointF(76.182f, -111.647f),
                new PointF(75.659f, -106.298f), new PointF(75.136f, -100.948f), new PointF(74.59f, -99.368f),
                new PointF(74.257f, -94.153f), new PointF(73.923f, -88.939f), new PointF(73.567f, -81.027f),
                new PointF(73.657f, -75.011f), new PointF(73.746f, -68.995f), new PointF(74.069f, -65.067f),
                new PointF(74.791f, -58.059f), new PointF(75.514f, -51.051f), new PointF(76.648f, -41.547f),
                new PointF(77.994f, -32.96f), new PointF(79.34f, -24.373f), new PointF(81.231f, -15.569f),
                new PointF(82.866f, -6.539f), new PointF(84.501f, 2.492f), new PointF(86.113f, 12.112f),
                new PointF(87.804f, 21.221f), new PointF(89.494f, 30.329f), new PointF(91.418f, 37.413f),
                new PointF(93.008f, 48.112f), new PointF(97.346f, 85.413f), new PointF(97.98f, 95.823f),
                new PointF(97.367f, 102.973f), new PointF(96.811f, 110.57f), new PointF(96.254f, 118.166f),
                new PointF(95.652f, 123.473f), new PointF(94.006f, 130.992f), new PointF(92.36f, 138.51f),
                new PointF(89.449f, 147.626f), new PointF(86.936f, 155.678f), new PointF(84.423f, 163.73f),
                new PointF(82.296f, 170.872f), new PointF(78.926f, 179.302f), new PointF(75.557f, 187.733f),
                new PointF(70.831f, 198.196f), new PointF(66.717f, 206.259f), new PointF(62.602f, 214.322f),
                new PointF(58.966f, 221.361f), new PointF(54.24f, 227.678f), new PointF(49.513f, 233.995f),
                new PointF(43.174f, 239.823f), new PointF(38.358f, 244.16f), new PointF(33.542f, 248.498f),
                new PointF(30.307f, 250.945f), new PointF(25.346f, 253.702f), new PointF(20.384f, 256.46f),
                new PointF(15.673f, 259.551f), new PointF(8.589f, 260.707f), new PointF(1.505f, 261.864f),
                new PointF(1.172f, 262.153f), new PointF(-3.96f, 262.442f)
            };
        }
    }
}
