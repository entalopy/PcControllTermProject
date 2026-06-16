using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;

namespace MidTermproject_21100210
{
    public sealed class ResultRenderer
    {
        public void SaveResult(string maskPath, string outputPath, ShapeFeatures features, PointF[] points)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using (var bitmap = new Bitmap(maskPath))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawOverlay(g, features, points);

                bitmap.Save(outputPath, ImageFormat.Png);
            }
        }

        public static void DrawOverlay(Graphics g, ShapeFeatures features, PointF[] points)
        {
            DrawFeatureGuides(g, features);
            DrawQualityBadge(g, features);
            if (features.IsBad)
            {
                using (Font font = new Font("Arial", 24, FontStyle.Bold))
                using (Brush brush = new SolidBrush(Color.Red))
                {
                    g.DrawString("BAD MASK: " + features.BadReason, font, brush, new PointF(20, 88));
                }
            }

            DrawPoints(g, points);
        }

        private static void DrawFeatureGuides(Graphics g, ShapeFeatures features)
        {
            using (Pen boxPen = new Pen(Color.Cyan, 3))
            using (Pen axisXPen = new Pen(Color.Yellow, 3))
            using (Pen axisYPen = new Pen(Color.DeepPink, 3))
            using (Pen frontPen = new Pen(Color.LimeGreen, 4))
            using (Pen rearPen = new Pen(Color.Magenta, 4))
            using (Brush centerBrush = new SolidBrush(Color.Orange))
            {
                Rectangle box = new Rectangle(features.BoundingBox.X, features.BoundingBox.Y, features.BoundingBox.Width, features.BoundingBox.Height);
                g.DrawRectangle(boxPen, box);

                PointF center = new PointF(features.Center.X, features.Center.Y);
                g.FillEllipse(centerBrush, center.X - 5, center.Y - 5, 10, 10);

                g.DrawLine(axisXPen, features.CenterLineStart.X, features.CenterLineStart.Y, features.CenterLineEnd.X, features.CenterLineEnd.Y);
                g.DrawLine(frontPen, features.FrontBallLineStart.X, features.FrontBallLineStart.Y, features.FrontBallLineEnd.X, features.FrontBallLineEnd.Y);
                g.DrawLine(rearPen, features.RearBallLineStart.X, features.RearBallLineStart.Y, features.RearBallLineEnd.X, features.RearBallLineEnd.Y);

                DrawLineLabel(g, "center", Color.Yellow, MidPoint(features.CenterLineStart, features.CenterLineEnd));
                DrawLineLabel(g, "front ball", Color.LimeGreen, MidPoint(features.FrontBallLineStart, features.FrontBallLineEnd));
                DrawLineLabel(g, "rear ball", Color.Magenta, MidPoint(features.RearBallLineStart, features.RearBallLineEnd));
            }
        }

        private static void DrawQualityBadge(Graphics g, ShapeFeatures features)
        {
            Color color = features.IsBad ? Color.Red : features.QualityScore >= 70 ? Color.LimeGreen : features.QualityScore >= 45 ? Color.Orange : Color.Red;
            Color sideColor = GetShoeSideColor(features.ShoeSide);
            using (Brush fill = new SolidBrush(Color.FromArgb(210, color)))
            using (Brush sideFill = new SolidBrush(Color.FromArgb(230, sideColor)))
            using (Brush textBrush = new SolidBrush(Color.Black))
            using (Font font = new Font("Arial", 12, FontStyle.Bold))
            using (Font sideFont = new Font("Arial", 13, FontStyle.Bold))
            {
                RectangleF rect = new RectangleF(16, 14, 170, 28);
                g.FillRectangle(fill, rect);
                g.DrawString(string.Format("Q {0:0.0}  {1}", features.QualityScore, features.IsBad ? "BAD" : "OK"), font, textBrush, 22, 18);

                RectangleF sideRect = new RectangleF(16, 48, 230, 34);
                g.FillRectangle(sideFill, sideRect);
                g.DrawString(string.Format("{0}  angle {1:0} deg", features.ShoeSide, features.RotationAngleDegrees), sideFont, textBrush, 22, 54);
            }
        }

        private static PointF MidPoint(Point2f a, Point2f b)
        {
            return new PointF((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
        }

        private static void DrawLineLabel(Graphics g, string text, Color color, PointF p)
        {
            using (Font font = new Font("Arial", 10, FontStyle.Bold))
            using (Brush back = new SolidBrush(Color.FromArgb(215, Color.Black)))
            using (Brush fore = new SolidBrush(color))
            {
                SizeF size = g.MeasureString(text, font);
                RectangleF rect = new RectangleF(p.X + 8, p.Y - 18, size.Width + 8, size.Height + 4);
                g.FillRectangle(back, rect);
                g.DrawString(text, font, fore, rect.X + 4, rect.Y + 2);
            }
        }

        private static Color GetShoeSideColor(string shoeSide)
        {
            if (string.Equals(shoeSide, "LEFT", StringComparison.OrdinalIgnoreCase))
            {
                return Color.DeepSkyBlue;
            }
            if (string.Equals(shoeSide, "RIGHT", StringComparison.OrdinalIgnoreCase))
            {
                return Color.HotPink;
            }
            return Color.Gold;
        }

        private static void DrawPoints(Graphics g, PointF[] points)
        {
            using (Brush fill = new SolidBrush(Color.Lime))
            using (Pen outline = new Pen(Color.Black, 2))
            using (Font font = new Font("Arial", 12, FontStyle.Bold))
            using (Brush text = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < points.Length; i++)
                {
                    PointF p = points[i];
                    var rect = new RectangleF(p.X - 6, p.Y - 6, 12, 12);
                    g.FillEllipse(fill, rect);
                    g.DrawEllipse(outline, rect);
                    g.DrawString((i + 1).ToString(), font, text, p.X + 7, p.Y - 7);
                }
            }
        }
    }
}
