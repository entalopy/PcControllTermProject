using OpenCvSharp;

namespace MidTermproject_21100210
{
    public sealed class ShapeFeatures
    {
        public Point2f Center { get; set; }
        public Point2f AxisX { get; set; }
        public Point2f AxisY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Rect BoundingBox { get; set; }
        public int Area { get; set; }
        public double MainComponentRatio { get; set; }
        public double QualityScore { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public int FrontSign { get; set; }
        public double FrontConfidence { get; set; }
        public int PredFrontSign { get; set; }
        public double PositiveEndWidth { get; set; }
        public double NegativeEndWidth { get; set; }
        public double FrontWidth { get; set; }
        public double RearWidth { get; set; }
        public int InnerSign { get; set; }
        public double InnerConfidence { get; set; }
        public string ShoeSide { get; set; }
        public double RotationAngleDegrees { get; set; }
        public double FrontBallWidth { get; set; }
        public double RearBallWidth { get; set; }
        public Point2f CenterLineStart { get; set; }
        public Point2f CenterLineEnd { get; set; }
        public Point2f FrontBallLineStart { get; set; }
        public Point2f FrontBallLineEnd { get; set; }
        public Point2f RearBallLineStart { get; set; }
        public Point2f RearBallLineEnd { get; set; }
        public int IslandCount { get; set; }
        public int IslandThreshold { get; set; }
        public double IslandScore { get; set; }
        public double DistortionScore { get; set; }
        public double DistortionDefectScore { get; set; }
        public double LocalDefectScore { get; set; }
        public double DefectScore { get; set; }
        public int DefectThreshold { get; set; }
        public string Label { get; set; }
        public bool IsBad { get; set; }
        public string BadReason { get; set; }
        public Mat BinaryMask { get; set; }

        public ShapeFeatures()
        {
            AxisX = new Point2f(1, 0);
            AxisY = new Point2f(0, 1);
            ShoeSide = "UNKNOWN";
            BadReason = string.Empty;
        }
    }
}

