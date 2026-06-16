using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MidTermproject_21100210
{
    public partial class Form1 : Form
    {
        private readonly ShapeModel shapeModel = new ShapeModel();
        private readonly MaskAnalyzer analyzer = new MaskAnalyzer();
        private readonly ShapeMatcher matcher = new ShapeMatcher();
        private readonly ResultRenderer renderer = new ResultRenderer();
        private readonly string defaultMaskRoot;

        private string currentMaskPath;
        private ShapeFeatures currentMaskFeatures;
        private PointF[] currentMatchedPoints;
        private int draggingPointIndex = -1;
        private List<string> resultFiles = new List<string>();
        private List<string> resultInfoTexts = new List<string>();
        private List<string> resultOriginalFiles = new List<string>();
        private List<string> goodResultFiles = new List<string>();
        private List<string> goodResultInfoTexts = new List<string>();
        private List<string> goodOriginalFiles = new List<string>();
        private List<string> badResultFiles = new List<string>();
        private List<string> badResultInfoTexts = new List<string>();
        private List<string> badOriginalFiles = new List<string>();
        private int resultFileIndex = -1;
        private int goodResultFileIndex = -1;
        private int badResultFileIndex = -1;
        private List<ExperimentResult> lastExperimentResults = new List<ExperimentResult>();
        private readonly Dictionary<string, ManualLabel> manualLabels = new Dictionary<string, ManualLabel>(StringComparer.OrdinalIgnoreCase);

        public Form1()
        {
            InitializeComponent();
            defaultMaskRoot = FindDefaultMaskRoot();
            txtMaskRoot.Text = defaultMaskRoot;
            txtPointInfo.Text = shapeModel.GetMeasurementInfo();
            txtResultInfo.Text = "Click Read File to load one mask, or Match All Folder Files to process a folder.";
            UpdateDefectThresholdLabel();
            SetNavigationState();
            btnLabelCurrent.Enabled = false;
            btnExportLabels.Enabled = false;
        }

        private void btnReadFile_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Mask PNG (*.png)|*.png|All files (*.*)|*.*";
                dialog.Multiselect = true;
                dialog.InitialDirectory = Directory.Exists(txtMaskRoot.Text) ? txtMaskRoot.Text : defaultMaskRoot;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (dialog.FileNames.Length == 1)
                {
                    LoadMask(dialog.FileName);
                }
                else
                {
                    string root = Path.GetDirectoryName(dialog.FileNames[0]);
                    txtMaskRoot.Text = root;
                    MatchFiles(dialog.FileNames.OrderBy(f => f).ToArray(), root, Path.Combine(root, "matched_selected"));
                }
            }
        }

        private void btnMatch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentMaskPath))
            {
                MessageBox.Show(this, "留덉뒪???뚯씪??癒쇱? ?쎌뼱二쇱꽭??", "Match", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MatchCurrentMask();
        }

        private void btnMatchAll_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "留덉뒪???대?吏 ?대뜑瑜??좏깮?섏꽭??";
                dialog.SelectedPath = Directory.Exists(txtMaskRoot.Text) ? txtMaskRoot.Text : defaultMaskRoot;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                txtMaskRoot.Text = dialog.SelectedPath;
                MatchAllFolderFiles(dialog.SelectedPath);
            }
        }

        private void btnResetPoints_Click(object sender, EventArgs e)
        {
            var fresh = new ShapeModel();
            for (int i = 0; i < shapeModel.MeasurementPoints.Length; i++)
            {
                shapeModel.SetMeasurementPoint(i, fresh.MeasurementPoints[i]);
            }

            currentMatchedPoints = null;
            txtPointInfo.Text = shapeModel.GetMeasurementInfo();
            pictureReference.Invalidate();
            pictureResult.Invalidate();
        }

        private void btnPrevResult_Click(object sender, EventArgs e)
        {
            ShowActiveResultAt(GetActiveResultIndex() - GetResultPageStep());
        }

        private void btnNextResult_Click(object sender, EventArgs e)
        {
            ShowActiveResultAt(GetActiveResultIndex() + GetResultPageStep());
        }

        private void rdoResultFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdoShowGood.Checked && !rdoShowBad.Checked)
            {
                return;
            }

            ShowActiveResultAt(0);
        }

        private void trackIslandBox_Scroll(object sender, EventArgs e)
        {
            UpdateDefectThresholdLabel();
        }

        private void btnLabelCurrent_Click(object sender, EventArgs e)
        {
            ExperimentResult current = GetCurrentExperimentResult();
            if (current == null)
            {
                MessageBox.Show(this, "Match All 결과에서 라벨을 입력할 이미지를 먼저 선택하세요.", "Manual Label", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ManualLabel existing;
            manualLabels.TryGetValue(current.FilePath, out existing);
            using (var dialog = new LabelInputDialog(current, existing))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                manualLabels[current.FilePath] = dialog.Label;
                txtResultInfo.Text = BuildImageReviewText(current.Index, lastExperimentResults.Count, current.FilePath, current.Features, current.Points)
                    + Environment.NewLine
                    + FormatManualLabel(dialog.Label);
                lblStatus.Text = string.Format("Labeled {0}/{1}: {2}", manualLabels.Count, lastExperimentResults.Count, Path.GetFileName(current.FilePath));
            }
        }

        private void btnExportLabels_Click(object sender, EventArgs e)
        {
            if (lastExperimentResults.Count == 0)
            {
                MessageBox.Show(this, "먼저 Match All Folder Files를 실행하세요.", "Export Labels", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string root = Directory.Exists(txtMaskRoot.Text) ? txtMaskRoot.Text : defaultMaskRoot;
            string outputRoot = Path.Combine(root, "output");
            string csvPath = SaveExperimentResults(lastExperimentResults, outputRoot);
            string xlsPath = SaveLabelComparisonWorkbook(lastExperimentResults, outputRoot);
            lblStatus.Text = "Exported: " + csvPath;
            txtResultInfo.Text = "Manual label export complete" + Environment.NewLine
                + "labeled: " + manualLabels.Count + " / " + lastExperimentResults.Count + Environment.NewLine
                + "csv: " + csvPath + Environment.NewLine
                + "excel: " + xlsPath;
            MessageBox.Show(this, "엑셀/CSV 내보내기 완료:\r\n" + csvPath + "\r\n" + xlsPath, "Export Labels", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadMask(string path)
        {
            currentMaskPath = path;
            currentMatchedPoints = null;
            SyncIslandThreshold();
            currentMaskFeatures = analyzer.Analyze(path);
            pictureMask.Image = LoadBitmapWithoutLock(path);
            pictureResult.Image = LoadBitmapWithoutLock(path);
            lblCurrentFile.Text = Path.GetFileName(path);
            txtResultInfo.Text = FormatMaskInfo(currentMaskFeatures);
            ResetResultLists();
            SetNavigationState();
        }

        private void MatchCurrentMask()
        {
            SyncIslandThreshold();
            currentMaskFeatures = analyzer.Analyze(currentMaskPath);
            if (CanMatch(currentMaskFeatures))
            {
                currentMatchedPoints = matcher.Match(shapeModel, currentMaskFeatures);
                currentMaskFeatures = matcher.LastOrientedFeatures ?? currentMaskFeatures;
            }
            else
            {
                currentMatchedPoints = new PointF[0];
            }

            txtResultInfo.Text = FormatMatchInfo(currentMaskFeatures, currentMatchedPoints);
            pictureResult.Image = RenderPreview(currentMaskPath, currentMaskFeatures, currentMatchedPoints);
        }

        private void MatchAllFolderFiles(string root)
        {
            string[] files = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories)
                .Where(f => f.IndexOf(Path.DirectorySeparatorChar + "matched_results" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0)
                .Where(f => f.IndexOf(Path.DirectorySeparatorChar + "output" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0)
                .OrderBy(f => f)
                .ToArray();
            if (files.Length == 0)
            {
                MessageBox.Show(this, "?좏깮???대뜑??PNG ?뚯씪???놁뒿?덈떎.", "Match All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MatchFiles(files, root, Path.Combine(root, "output"));
        }

        private void MatchFiles(string[] files, string root, string outputRoot)
        {
            Directory.CreateDirectory(outputRoot);
            string matchedImagesRoot = Path.Combine(outputRoot, "matched_images");
            Directory.CreateDirectory(matchedImagesRoot);
            SyncIslandThreshold();

            var log = new StringBuilder();
            var experimentResults = new List<ExperimentResult>();
            log.AppendLine("file,status,reason,shoe_side,rotation_angle,quality,area,component_ratio,islands,island_score,distortion_score,local_defect_score,defect_score,defect_threshold,width,height,front_ball_width,rear_ball_width,front_rear_width_delta,p1x,p1y,p2x,p2y,p3x,p3y,p4x,p4y,p5x,p5y,p6x,p6y");
            progressBar.Minimum = 0;
            progressBar.Maximum = files.Length;
            progressBar.Value = 0;
            ResetResultLists();
            manualLabels.Clear();

            int okCount = 0;
            int badCount = 0;
            double qualitySum = 0;
            string bestFile = string.Empty;
            string worstFile = string.Empty;
            double bestQuality = double.MinValue;
            double worstQuality = double.MaxValue;

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                ShapeFeatures features = analyzer.Analyze(file);
                PointF[] points = CanMatch(features) ? matcher.Match(shapeModel, features) : new PointF[0];
                if (CanMatch(features))
                {
                    features = matcher.LastOrientedFeatures ?? features;
                }

                string relative = MakeSafeRelativePath(root, file);
                string outputPath = Path.Combine(matchedImagesRoot, relative);
                renderer.SaveResult(file, outputPath, features, points);
                resultFiles.Add(outputPath);
                resultInfoTexts.Add(BuildImageReviewText(i + 1, files.Length, file, features, points));
                resultOriginalFiles.Add(file);
                log.AppendLine(BuildLogLine(file, features, points));
                experimentResults.Add(new ExperimentResult
                {
                    Index = i + 1,
                    FilePath = file,
                    ModelGroup = ExtractModelGroup(file),
                    Features = features,
                    Points = points,
                    MatchedImagePath = outputPath
                });

                if (features.IsBad)
                {
                    badCount++;
                    badResultFiles.Add(outputPath);
                    badResultInfoTexts.Add(BuildImageReviewText(badResultFiles.Count, badResultFiles.Count, file, features, points));
                    badOriginalFiles.Add(file);
                }
                else
                {
                    okCount++;
                    goodResultFiles.Add(outputPath);
                    goodResultInfoTexts.Add(BuildImageReviewText(goodResultFiles.Count, goodResultFiles.Count, file, features, points));
                    goodOriginalFiles.Add(file);
                    qualitySum += features.QualityScore;
                    if (features.QualityScore > bestQuality)
                    {
                        bestQuality = features.QualityScore;
                        bestFile = file;
                    }
                    if (features.QualityScore < worstQuality)
                    {
                        worstQuality = features.QualityScore;
                        worstFile = file;
                    }
                }

                progressBar.Value = i + 1;
                lblStatus.Text = string.Format("Processing {0}/{1}: {2}", i + 1, files.Length, Path.GetFileName(file));
                Application.DoEvents();
            }

            lastExperimentResults = experimentResults;
            string csvPath = Path.Combine(outputRoot, "experiment_results.csv");
            lblStatus.Text = "Done: " + outputRoot;
            txtResultInfo.Text = BuildBatchSummary(files.Length, okCount, badCount, qualitySum, bestQuality, worstQuality, bestFile, worstFile, outputRoot, csvPath)
                + Environment.NewLine
                + "정답 라벨은 아직 저장되지 않았습니다. Label Current로 입력한 뒤 Export Labels를 누르세요.";
            lblBatchSummary.Text = string.Format("OK {0} / BAD {1} / TOTAL {2}", okCount, badCount, files.Length);
            rdoShowGood.Checked = okCount > 0 || badCount == 0;
            rdoShowBad.Checked = okCount == 0 && badCount > 0;
            if (GetActiveResultFiles().Count > 0)
            {
                ShowActiveResultAt(0);
            }
            btnLabelCurrent.Enabled = lastExperimentResults.Count > 0;
            btnExportLabels.Enabled = lastExperimentResults.Count > 0;
            MessageBox.Show(this, "Match All complete.\r\n라벨 입력 후 Export Labels를 누르면 experiment_results.csv와 검수용 Excel 파일이 저장됩니다.", "Match All", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetResultLists()
        {
            resultFiles = new List<string>();
            resultInfoTexts = new List<string>();
            resultOriginalFiles = new List<string>();
            goodResultFiles = new List<string>();
            goodResultInfoTexts = new List<string>();
            goodOriginalFiles = new List<string>();
            badResultFiles = new List<string>();
            badResultInfoTexts = new List<string>();
            badOriginalFiles = new List<string>();
            lastExperimentResults = new List<ExperimentResult>();
            resultFileIndex = -1;
            goodResultFileIndex = -1;
            badResultFileIndex = -1;
            rdoShowGood.Checked = true;
            btnLabelCurrent.Enabled = false;
            btnExportLabels.Enabled = false;
        }

        private static string BuildLogLine(string file, ShapeFeatures features, PointF[] points)
        {
            var values = new List<string>
            {
                Quote(file),
                features.IsBad ? "BAD" : "OK",
                Quote(features.BadReason),
                features.ShoeSide,
                features.RotationAngleDegrees.ToString("0.###", CultureInfo.InvariantCulture),
                features.QualityScore.ToString("0.##", CultureInfo.InvariantCulture),
                features.Area.ToString(CultureInfo.InvariantCulture),
                features.MainComponentRatio.ToString("0.####", CultureInfo.InvariantCulture),
                features.IslandCount.ToString(CultureInfo.InvariantCulture),
                features.IslandScore.ToString("0.###", CultureInfo.InvariantCulture),
                features.DistortionScore.ToString("0.###", CultureInfo.InvariantCulture),
                features.LocalDefectScore.ToString("0.###", CultureInfo.InvariantCulture),
                features.DefectScore.ToString("0.###", CultureInfo.InvariantCulture),
                features.DefectThreshold.ToString(CultureInfo.InvariantCulture),
                features.Width.ToString("0.###", CultureInfo.InvariantCulture),
                features.Height.ToString("0.###", CultureInfo.InvariantCulture),
                features.FrontBallWidth.ToString("0.###", CultureInfo.InvariantCulture),
                features.RearBallWidth.ToString("0.###", CultureInfo.InvariantCulture),
                (features.FrontBallWidth - features.RearBallWidth).ToString("0.###", CultureInfo.InvariantCulture)
            };

            for (int i = 0; i < 6; i++)
            {
                if (i < points.Length)
                {
                    values.Add(points[i].X.ToString("0.###", CultureInfo.InvariantCulture));
                    values.Add(points[i].Y.ToString("0.###", CultureInfo.InvariantCulture));
                }
                else
                {
                    values.Add(string.Empty);
                    values.Add(string.Empty);
                }
            }

            return string.Join(",", values);
        }


        private string SaveExperimentResults(List<ExperimentResult> results, string outputRoot)
        {
            Directory.CreateDirectory(outputRoot);
            string csvPath = Path.Combine(outputRoot, "experiment_results.csv");
            var sb = new StringBuilder();
            sb.AppendLine("index,file_name,file_path,model_group,image_width,image_height,pred_front_sign,positive_end_width,negative_end_width,front_width,rear_width,front_confidence,pred_shoe_side,inner_sign,inner_confidence,rotation_angle_deg,defect_score,island_score,distortion_score,local_defect_score,defect_threshold,is_bad,bad_reason,p1_x,p1_y,p2_x,p2_y,p3_x,p3_y,p4_x,p4_y,p5_x,p5_y,p6_x,p6_y,true_p1_x,true_p1_y,true_p2_x,true_p2_y,true_p3_x,true_p3_y,true_p4_x,true_p4_y,true_p5_x,true_p5_y,true_p6_x,true_p6_y,p1_error_px,p2_error_px,p3_error_px,p4_error_px,p5_error_px,p6_error_px,point_mean_error_px,point_labeled_count,point_all_within_15px,point_all_within_20px,matched_image_path,true_front_sign,true_shoe_side,true_bad,memo");

            foreach (ExperimentResult result in results)
            {
                ManualLabel label = GetManualLabel(result.FilePath);
                sb.AppendLine(string.Join(",", BuildExperimentCsvValues(result, label).Select(CsvEscape)));
            }

            File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
            return csvPath;
        }

        private IEnumerable<object> BuildExperimentCsvValues(ExperimentResult result, ManualLabel label)
        {
            ShapeFeatures f = result.Features ?? new ShapeFeatures();
            var values = new List<object>
            {
                result.Index,
                Path.GetFileName(result.FilePath),
                result.FilePath,
                result.ModelGroup,
                f.ImageWidth,
                f.ImageHeight,
                f.PredFrontSign,
                FormatDouble(f.PositiveEndWidth),
                FormatDouble(f.NegativeEndWidth),
                FormatDouble(f.FrontWidth),
                FormatDouble(f.RearWidth),
                FormatDouble(f.FrontConfidence),
                f.ShoeSide,
                f.InnerSign,
                FormatDouble(f.InnerConfidence),
                FormatDouble(f.RotationAngleDegrees),
                FormatDouble(f.DefectScore),
                FormatDouble(f.IslandScore),
                FormatDouble(f.DistortionScore),
                FormatDouble(f.LocalDefectScore),
                f.DefectThreshold,
                f.IsBad ? "TRUE" : "FALSE",
                f.IsBad ? f.BadReason : "OK"
            };

            for (int i = 0; i < 6; i++)
            {
                if (result.Points != null && i < result.Points.Length)
                {
                    values.Add(FormatDouble(result.Points[i].X));
                    values.Add(FormatDouble(result.Points[i].Y));
                }
                else
                {
                    values.Add(string.Empty);
                    values.Add(string.Empty);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                values.Add(label.TruePointX[i]);
                values.Add(label.TruePointY[i]);
            }

            for (int i = 0; i < 6; i++)
            {
                double error;
                values.Add(TryCalculatePointError(result, label, i, out error) ? FormatDouble(error) : string.Empty);
            }

            double meanError;
            int labeledCount;
            values.Add(TryCalculateMeanPointError(result, label, out meanError, out labeledCount) ? FormatDouble(meanError) : string.Empty);
            values.Add(labeledCount);
            values.Add(labeledCount == 6 ? (AreAllPointErrorsWithin(result, label, 15.0) ? "TRUE" : "FALSE") : string.Empty);
            values.Add(labeledCount == 6 ? (AreAllPointErrorsWithin(result, label, 20.0) ? "TRUE" : "FALSE") : string.Empty);

            values.Add(result.MatchedImagePath);
            values.Add(label.TrueFrontSign);
            values.Add(label.TrueShoeSide);
            values.Add(label.TrueBad);
            values.Add(label.Memo);
            return values;
        }

        private static bool TryCalculatePointError(ExperimentResult result, ManualLabel label, int index, out double error)
        {
            error = 0;
            if (result == null || result.Points == null || index < 0 || index >= result.Points.Length || label == null)
            {
                return false;
            }

            double trueX;
            double trueY;
            if (!TryParseManualPoint(label, index, out trueX, out trueY))
            {
                return false;
            }

            double dx = result.Points[index].X - trueX;
            double dy = result.Points[index].Y - trueY;
            error = Math.Sqrt(dx * dx + dy * dy);
            return true;
        }

        private static bool TryCalculateMeanPointError(ExperimentResult result, ManualLabel label, out double meanError, out int labeledCount)
        {
            meanError = 0;
            labeledCount = 0;
            double sum = 0;
            for (int i = 0; i < 6; i++)
            {
                double error;
                if (TryCalculatePointError(result, label, i, out error))
                {
                    sum += error;
                    labeledCount++;
                }
            }

            if (labeledCount == 0)
            {
                return false;
            }

            meanError = sum / labeledCount;
            return true;
        }

        private static bool AreAllPointErrorsWithin(ExperimentResult result, ManualLabel label, double threshold)
        {
            for (int i = 0; i < 6; i++)
            {
                double error;
                if (!TryCalculatePointError(result, label, i, out error) || error > threshold)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseManualPoint(ManualLabel label, int index, out double x, out double y)
        {
            x = 0;
            y = 0;
            if (label == null || index < 0 || index >= 6)
            {
                return false;
            }

            return double.TryParse(label.TruePointX[index], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && double.TryParse(label.TruePointY[index], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
        }
        private string SaveLabelComparisonWorkbook(List<ExperimentResult> results, string outputRoot)
        {
            Directory.CreateDirectory(outputRoot);
            string xlsPath = Path.Combine(outputRoot, "experiment_label_comparison.xls");
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.AppendLine("<Styles><Style ss:ID=\"Header\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#D9EAF7\" ss:Pattern=\"Solid\"/></Style><Style ss:ID=\"Bad\"><Interior ss:Color=\"#F4CCCC\" ss:Pattern=\"Solid\"/></Style><Style ss:ID=\"Ok\"><Interior ss:Color=\"#D9EAD3\" ss:Pattern=\"Solid\"/></Style></Styles>");
            sb.AppendLine("<Worksheet ss:Name=\"LabelComparison\"><Table>");
            string[] headers =
            {
                "index", "file_name",
                "pred_shoe_side", "true_shoe_side", "shoe_side_match",
                "is_bad", "true_bad", "bad_match", "point_mean_error_px", "point_labeled_count", "all_points_15px", "all_points_20px", "matched_image_path", "memo"
            };
            sb.AppendLine(BuildXmlRow(headers, true, null));
            foreach (ExperimentResult result in results)
            {
                ManualLabel label = GetManualLabel(result.FilePath);
                ShapeFeatures f = result.Features ?? new ShapeFeatures();
                string sideMatch = CompareText(f.ShoeSide, label.TrueShoeSide);
                string badMatch = CompareBad(f.IsBad, label.TrueBad);
                string rowStyle = (sideMatch == "MISS" || badMatch == "MISS") ? "Bad" : (sideMatch == "" && badMatch == "" ? null : "Ok");
                double meanError;
                int labeledCount;
                bool hasMeanError = TryCalculateMeanPointError(result, label, out meanError, out labeledCount);
                object[] row =
                {
                    result.Index,
                    Path.GetFileName(result.FilePath),
                    f.ShoeSide,
                    label.TrueShoeSide,
                    sideMatch,
                    f.IsBad ? "BAD" : "GOOD",
                    label.TrueBad,
                    badMatch,
                    hasMeanError ? FormatDouble(meanError) : string.Empty,
                    labeledCount,
                    labeledCount == 6 ? (AreAllPointErrorsWithin(result, label, 15.0) ? "TRUE" : "FALSE") : string.Empty,
                    labeledCount == 6 ? (AreAllPointErrorsWithin(result, label, 20.0) ? "TRUE" : "FALSE") : string.Empty,
                    result.MatchedImagePath,
                    label.Memo
                };
                sb.AppendLine(BuildXmlRow(row, false, rowStyle));
            }
            sb.AppendLine("</Table><WorksheetOptions xmlns=\"urn:schemas-microsoft-com:office:excel\"><FreezePanes/><FrozenNoSplit/><SplitHorizontal>1</SplitHorizontal><TopRowBottomPane>1</TopRowBottomPane></WorksheetOptions></Worksheet></Workbook>");
            File.WriteAllText(xlsPath, sb.ToString(), new UTF8Encoding(true));
            return xlsPath;
        }

        private static string BuildXmlRow(IEnumerable<object> values, bool header, string rowStyle)
        {
            var sb = new StringBuilder();
            sb.Append(rowStyle == null ? "<Row>" : "<Row ss:StyleID=\"" + rowStyle + "\">");
            foreach (object value in values)
            {
                sb.Append(header ? "<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">" : "<Cell><Data ss:Type=\"String\">");
                sb.Append(XmlEscape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));
                sb.Append("</Data></Cell>");
            }
            sb.Append("</Row>");
            return sb.ToString();
        }

        private static string XmlEscape(string value)
        {
            return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private ManualLabel GetManualLabel(string filePath)
        {
            ManualLabel label;
            return manualLabels.TryGetValue(filePath, out label) ? label : new ManualLabel();
        }

        private static string CompareText(string pred, string truth)
        {
            if (string.IsNullOrWhiteSpace(truth))
            {
                return string.Empty;
            }
            return string.Equals((pred ?? string.Empty).Trim(), truth.Trim(), StringComparison.OrdinalIgnoreCase) ? "OK" : "MISS";
        }

        private static string CompareBad(bool predBad, string truth)
        {
            if (string.IsNullOrWhiteSpace(truth))
            {
                return string.Empty;
            }
            string normalized = NormalizeBadLabel(truth);
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }
            return string.Equals(predBad ? "BAD" : "GOOD", normalized, StringComparison.OrdinalIgnoreCase) ? "OK" : "MISS";
        }

        private static string NormalizeBadLabel(string truth)
        {
            string t = (truth ?? string.Empty).Trim().ToUpperInvariant();
            if (t == "1" || t == "TRUE" || t == "BAD") return "BAD";
            if (t == "0" || t == "FALSE" || t == "GOOD" || t == "OK") return "GOOD";
            return string.Empty;
        }

        private static string ExtractModelGroup(string file)
        {
            string folder = Path.GetFileName(Path.GetDirectoryName(file));
            return folder ?? string.Empty;
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string CsvEscape(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\r") || s.Contains("\n"))
            {
                s = "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            return s;
        }

        private sealed class ExperimentResult
        {
            public int Index { get; set; }
            public string FilePath { get; set; }
            public string ModelGroup { get; set; }
            public ShapeFeatures Features { get; set; }
            public PointF[] Points { get; set; }
            public string MatchedImagePath { get; set; }
        }
        private sealed class ManualLabel
        {
            public string TrueFrontSign { get; set; }
            public string TrueShoeSide { get; set; }
            public string TrueBad { get; set; }
            public string Memo { get; set; }
            public string[] TruePointX { get; private set; }
            public string[] TruePointY { get; private set; }

            public ManualLabel()
            {
                TrueFrontSign = string.Empty;
                TrueShoeSide = string.Empty;
                TrueBad = string.Empty;
                Memo = string.Empty;
                TruePointX = new string[6];
                TruePointY = new string[6];
                for (int i = 0; i < 6; i++)
                {
                    TruePointX[i] = string.Empty;
                    TruePointY[i] = string.Empty;
                }
            }
        }

        private sealed class LabelInputDialog : Form
        {
            private readonly ComboBox cboFront = new ComboBox();
            private readonly ComboBox cboSide = new ComboBox();
            private readonly ComboBox cboBad = new ComboBox();
            private readonly TextBox txtMemo = new TextBox();
            private readonly TextBox[] txtPointX = new TextBox[6];
            private readonly TextBox[] txtPointY = new TextBox[6];
            private readonly PictureBox picturePointLabel = new PictureBox();
            private readonly CheckBox chkSavePoints = new CheckBox();
            private readonly PointF[] manualPoints = new PointF[6];
            private Bitmap previewBitmap;
            private int draggingPointIndex = -1;
            public ManualLabel Label { get; private set; }

            public LabelInputDialog(ExperimentResult result, ManualLabel existing)
            {
                Text = "Manual Label";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(990, 585);
                MaximizeBox = false;
                MinimizeBox = false;

                ShapeFeatures f = result.Features ?? new ShapeFeatures();
                Controls.Add(new Label { Left = 12, Top = 12, Width = 520, Text = Path.GetFileName(result.FilePath) });
                Controls.Add(new Label { Left = 12, Top = 42, Width = 520, Text = string.Format(CultureInfo.InvariantCulture, "pred front={0}, side={1}, bad={2}", f.PredFrontSign, f.ShoeSide, f.IsBad ? "BAD" : "GOOD") });

                AddCombo("true_front_sign", cboFront, 72, new[] { "", "1", "-1" });
                AddCombo("true_shoe_side", cboSide, 107, new[] { "", "LEFT", "RIGHT" });
                AddCombo("true_bad", cboBad, 142, new[] { "", "GOOD", "BAD" });

                Controls.Add(new Label { Left = 12, Top = 180, Width = 530, Text = "manual reference points: drag P1~P6 on the image or type x/y" });
                chkSavePoints.Left = 12;
                chkSavePoints.Top = 204;
                chkSavePoints.Width = 250;
                chkSavePoints.Text = "save manual point labels";
                chkSavePoints.CheckedChanged += (s, e) => picturePointLabel.Invalidate();
                Controls.Add(chkSavePoints);

                Controls.Add(new Label { Left = 52, Top = 237, Width = 70, Text = "pred x" });
                Controls.Add(new Label { Left = 122, Top = 237, Width = 70, Text = "pred y" });
                Controls.Add(new Label { Left = 210, Top = 237, Width = 70, Text = "true x" });
                Controls.Add(new Label { Left = 290, Top = 237, Width = 70, Text = "true y" });

                for (int i = 0; i < 6; i++)
                {
                    int top = 262 + i * 28;
                    Controls.Add(new Label { Left = 12, Top = top + 3, Width = 35, Text = "P" + (i + 1).ToString(CultureInfo.InvariantCulture) });
                    string predX = result.Points != null && i < result.Points.Length ? result.Points[i].X.ToString("0.0", CultureInfo.InvariantCulture) : "";
                    string predY = result.Points != null && i < result.Points.Length ? result.Points[i].Y.ToString("0.0", CultureInfo.InvariantCulture) : "";
                    Controls.Add(new Label { Left = 52, Top = top + 3, Width = 65, Text = predX });
                    Controls.Add(new Label { Left = 122, Top = top + 3, Width = 65, Text = predY });
                    txtPointX[i] = new TextBox { Left = 210, Top = top, Width = 68 };
                    txtPointY[i] = new TextBox { Left = 290, Top = top, Width = 68 };
                    txtPointX[i].TextChanged += (s, e) => UpdatePreviewPointsFromText(false);
                    txtPointY[i].TextChanged += (s, e) => UpdatePreviewPointsFromText(false);
                    Controls.Add(txtPointX[i]);
                    Controls.Add(txtPointY[i]);
                }

                var copyPred = new Button { Left = 380, Top = 262, Width = 145, Height = 30, Text = "Copy Pred Points" };
                copyPred.Click += (s, e) => CopyPredictedPoints(result);
                Controls.Add(copyPred);
                Controls.Add(new Label { Left = 380, Top = 300, Width = 160, Height = 70, Text = "Drag points on the image. Leave unchecked if this sample is not used for point error." });

                Controls.Add(new Label { Left = 12, Top = 437, Width = 120, Text = "memo" });
                txtMemo.Left = 140;
                txtMemo.Top = 434;
                txtMemo.Width = 385;
                Controls.Add(txtMemo);

                picturePointLabel.Left = 550;
                picturePointLabel.Top = 72;
                picturePointLabel.Width = 420;
                picturePointLabel.Height = 420;
                picturePointLabel.BorderStyle = BorderStyle.FixedSingle;
                picturePointLabel.BackColor = Color.Black;
                picturePointLabel.SizeMode = PictureBoxSizeMode.Zoom;
                picturePointLabel.Paint += picturePointLabel_Paint;
                picturePointLabel.MouseDown += picturePointLabel_MouseDown;
                picturePointLabel.MouseMove += picturePointLabel_MouseMove;
                picturePointLabel.MouseUp += (s, e) => draggingPointIndex = -1;
                Controls.Add(picturePointLabel);
                Controls.Add(new Label { Left = 550, Top = 500, Width = 420, Height = 35, Text = "Mouse: drag red points. The text boxes update automatically." });

                var ok = new Button { Left = 780, Top = 540, Width = 85, Text = "OK", DialogResult = DialogResult.OK };
                var cancel = new Button { Left = 875, Top = 540, Width = 85, Text = "Cancel", DialogResult = DialogResult.Cancel };
                Controls.Add(ok);
                Controls.Add(cancel);
                AcceptButton = ok;
                CancelButton = cancel;

                LoadPreviewImage(result);
                LoadInitialPointState(result, existing);

                if (existing != null)
                {
                    cboFront.Text = existing.TrueFrontSign;
                    cboSide.Text = existing.TrueShoeSide;
                    cboBad.Text = existing.TrueBad;
                    txtMemo.Text = existing.Memo;
                    for (int i = 0; i < 6; i++)
                    {
                        txtPointX[i].Text = existing.TruePointX[i];
                        txtPointY[i].Text = existing.TruePointY[i];
                    }
                }

                ok.Click += (s, e) =>
                {
                    Label = new ManualLabel
                    {
                        TrueFrontSign = cboFront.Text.Trim(),
                        TrueShoeSide = cboSide.Text.Trim().ToUpperInvariant(),
                        TrueBad = cboBad.Text.Trim().ToUpperInvariant(),
                        Memo = txtMemo.Text.Trim()
                    };
                    if (chkSavePoints.Checked)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Label.TruePointX[i] = txtPointX[i].Text.Trim();
                            Label.TruePointY[i] = txtPointY[i].Text.Trim();
                        }
                    }
                };
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && previewBitmap != null)
                {
                    previewBitmap.Dispose();
                    previewBitmap = null;
                }
                base.Dispose(disposing);
            }

            private void LoadPreviewImage(ExperimentResult result)
            {
                string path = !string.IsNullOrEmpty(result.MatchedImagePath) && File.Exists(result.MatchedImagePath)
                    ? result.MatchedImagePath
                    : result.FilePath;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using (var temp = new Bitmap(path))
                    {
                        previewBitmap = new Bitmap(temp);
                    }
                    picturePointLabel.Image = previewBitmap;
                }
            }

            private void LoadInitialPointState(ExperimentResult result, ManualLabel existing)
            {
                bool hasExisting = false;
                if (existing != null)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double x;
                        double y;
                        if (TryParseManualPoint(existing, i, out x, out y))
                        {
                            manualPoints[i] = new PointF((float)x, (float)y);
                            hasExisting = true;
                        }
                    }
                }

                if (!hasExisting && result.Points != null)
                {
                    for (int i = 0; i < 6 && i < result.Points.Length; i++)
                    {
                        manualPoints[i] = result.Points[i];
                    }
                }

                chkSavePoints.Checked = hasExisting;
            }

            private void CopyPredictedPoints(ExperimentResult result)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (result.Points != null && i < result.Points.Length)
                    {
                        manualPoints[i] = result.Points[i];
                        txtPointX[i].Text = result.Points[i].X.ToString("0.###", CultureInfo.InvariantCulture);
                        txtPointY[i].Text = result.Points[i].Y.ToString("0.###", CultureInfo.InvariantCulture);
                    }
                }
                chkSavePoints.Checked = true;
                picturePointLabel.Invalidate();
            }

            private void UpdatePreviewPointsFromText(bool enableSave)
            {
                for (int i = 0; i < 6; i++)
                {
                    double x;
                    double y;
                    if (double.TryParse(txtPointX[i].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                        && double.TryParse(txtPointY[i].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    {
                        manualPoints[i] = new PointF((float)x, (float)y);
                    }
                }
                if (enableSave)
                {
                    chkSavePoints.Checked = true;
                }
                picturePointLabel.Invalidate();
            }

            private void picturePointLabel_Paint(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (previewBitmap == null)
                {
                    return;
                }

                bool active = chkSavePoints.Checked;
                for (int i = 0; i < 6; i++)
                {
                    PointF screen = ImageToPreviewPoint(manualPoints[i]);
                    Color fillColor = active ? Color.Red : Color.FromArgb(180, Color.Gold);
                    using (Brush brush = new SolidBrush(fillColor))
                    using (Pen pen = new Pen(Color.Black, 2))
                    using (Font font = new Font("Arial", 11, FontStyle.Bold))
                    using (Brush textBrush = new SolidBrush(Color.White))
                    {
                        e.Graphics.FillEllipse(brush, screen.X - 7, screen.Y - 7, 14, 14);
                        e.Graphics.DrawEllipse(pen, screen.X - 7, screen.Y - 7, 14, 14);
                        e.Graphics.DrawString((i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, screen.X + 8, screen.Y - 10);
                    }
                }
            }

            private void picturePointLabel_MouseDown(object sender, MouseEventArgs e)
            {
                draggingPointIndex = FindNearestPoint(e.Location);
                if (draggingPointIndex >= 0)
                {
                    chkSavePoints.Checked = true;
                    UpdatePointFromMouse(draggingPointIndex, e.Location);
                }
            }

            private void picturePointLabel_MouseMove(object sender, MouseEventArgs e)
            {
                if (draggingPointIndex >= 0 && e.Button == MouseButtons.Left)
                {
                    UpdatePointFromMouse(draggingPointIndex, e.Location);
                }
            }

            private int FindNearestPoint(Point location)
            {
                int nearest = -1;
                double best = 14.0;
                for (int i = 0; i < 6; i++)
                {
                    PointF screen = ImageToPreviewPoint(manualPoints[i]);
                    double dx = screen.X - location.X;
                    double dy = screen.Y - location.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = i;
                    }
                }
                return nearest;
            }

            private void UpdatePointFromMouse(int index, Point location)
            {
                PointF imagePoint = PreviewToImagePoint(location);
                manualPoints[index] = imagePoint;
                txtPointX[index].Text = imagePoint.X.ToString("0.###", CultureInfo.InvariantCulture);
                txtPointY[index].Text = imagePoint.Y.ToString("0.###", CultureInfo.InvariantCulture);
                picturePointLabel.Invalidate();
            }

            private RectangleF GetImageDisplayRectangle()
            {
                if (previewBitmap == null || previewBitmap.Width <= 0 || previewBitmap.Height <= 0)
                {
                    return new RectangleF(0, 0, picturePointLabel.Width, picturePointLabel.Height);
                }

                float scale = Math.Min((float)picturePointLabel.Width / previewBitmap.Width, (float)picturePointLabel.Height / previewBitmap.Height);
                float width = previewBitmap.Width * scale;
                float height = previewBitmap.Height * scale;
                return new RectangleF((picturePointLabel.Width - width) / 2f, (picturePointLabel.Height - height) / 2f, width, height);
            }

            private PointF ImageToPreviewPoint(PointF imagePoint)
            {
                RectangleF rect = GetImageDisplayRectangle();
                if (previewBitmap == null)
                {
                    return imagePoint;
                }

                return new PointF(rect.Left + imagePoint.X * rect.Width / previewBitmap.Width, rect.Top + imagePoint.Y * rect.Height / previewBitmap.Height);
            }

            private PointF PreviewToImagePoint(Point previewPoint)
            {
                RectangleF rect = GetImageDisplayRectangle();
                if (previewBitmap == null || rect.Width <= 0 || rect.Height <= 0)
                {
                    return previewPoint;
                }

                float x = (previewPoint.X - rect.Left) * previewBitmap.Width / rect.Width;
                float y = (previewPoint.Y - rect.Top) * previewBitmap.Height / rect.Height;
                x = Math.Max(0, Math.Min(previewBitmap.Width - 1, x));
                y = Math.Max(0, Math.Min(previewBitmap.Height - 1, y));
                return new PointF(x, y);
            }

            private void AddCombo(string caption, ComboBox combo, int top, string[] values)
            {
                Controls.Add(new Label { Left = 12, Top = top + 3, Width = 120, Text = caption });
                combo.Left = 140;
                combo.Top = top;
                combo.Width = 120;
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.Items.AddRange(values);
                combo.SelectedIndex = 0;
                Controls.Add(combo);
            }
        }
        private static double SafeBoxRatio(ShapeFeatures features)
        {
            return features.BoundingBox.Height > 0 ? (double)features.BoundingBox.Width / features.BoundingBox.Height : 0;
        }

        private static string Quote(string text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private static string MakeSafeRelativePath(string root, string file)
        {
            string relative = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folder = Path.GetDirectoryName(relative);
            string name = Path.GetFileNameWithoutExtension(relative) + "_matched.png";
            return string.IsNullOrEmpty(folder) ? name : Path.Combine(folder, name);
        }

        private Bitmap RenderPreview(string path, ShapeFeatures features, PointF[] points)
        {
            var bitmap = new Bitmap(path);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                ResultRenderer.DrawOverlay(g, features, points);
            }

            return bitmap;
        }

        private static Bitmap LoadBitmapWithoutLock(string path)
        {
            using (var temp = new Bitmap(path))
            {
                return new Bitmap(temp);
            }
        }

        private static bool CanMatch(ShapeFeatures features)
        {
            return features != null && features.BinaryMask != null && !features.BinaryMask.Empty() && features.Area > 0;
        }

        private string FormatMaskInfo(ShapeFeatures f)
        {
            if (f == null)
            {
                return string.Empty;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "=== Mask Analysis ===\r\nfile: {0}\r\nstatus: {8}\r\nshoe side: {12}\r\nrotation angle: {13:0.0} deg\r\nquality: {7:0.0}/100\r\n\r\n=== Shape Features ===\r\narea: {1}\r\nmain component ratio: {2:0.000}\r\nislands: {11}  island score: {14:0.0}\r\ndistortion: {15:0.0}\r\nlocal defect: {16:0.0}\r\ndefect score: {17:0.0} / threshold {18}\r\nbox ratio: {9:0.000}\r\ncenter: ({3:0.0}, {4:0.0})\r\nPCA width/height: {5:0.0} / {6:0.0}",
                f.Label, f.Area, f.MainComponentRatio, f.Center.X, f.Center.Y, f.Width, f.Height,
                f.QualityScore,
                f.IsBad ? "BAD - " + f.BadReason : "OK",
                SafeBoxRatio(f),
                f.IslandThreshold,
                f.IslandCount,
                f.ShoeSide,
                f.RotationAngleDegrees,
                f.IslandScore,
                f.DistortionScore,
                f.LocalDefectScore,
                f.DefectScore,
                f.DefectThreshold);
        }

        private static string BuildBatchSummary(int total, int okCount, int badCount, double qualitySum, double bestQuality, double worstQuality, string bestFile, string worstFile, string outputRoot, string csvPath)
        {
            double avgQuality = okCount > 0 ? qualitySum / okCount : 0;
            var sb = new StringBuilder();
            sb.AppendLine("Batch result");
            sb.AppendLine("------------");
            sb.AppendFormat(CultureInfo.InvariantCulture, "total: {0}\r\n", total);
            sb.AppendFormat(CultureInfo.InvariantCulture, "OK: {0}\r\nBAD: {1}\r\n", okCount, badCount);
            sb.AppendFormat(CultureInfo.InvariantCulture, "BAD rate: {0:0.0}%\r\n", total > 0 ? 100.0 * badCount / total : 0);
            sb.AppendFormat(CultureInfo.InvariantCulture, "avg quality: {0:0.0}/100\r\n", avgQuality);
            if (okCount > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "best: {0:0.0}  {1}\r\n", bestQuality, Path.GetFileName(bestFile));
                sb.AppendFormat(CultureInfo.InvariantCulture, "worst OK: {0:0.0}  {1}\r\n", worstQuality, Path.GetFileName(worstFile));
            }
            sb.AppendLine();
            sb.AppendLine("output: " + outputRoot);
            sb.AppendLine("csv: " + csvPath);
            return sb.ToString();
        }

        private string BuildImageReviewText(int index, int total, string file, ShapeFeatures f, PointF[] points)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "=== Review Image {0}/{1} ===\r\n", index, total);
            sb.AppendLine("file: " + Path.GetFileName(file));
            sb.AppendLine("folder: " + Path.GetFileName(Path.GetDirectoryName(file)));
            sb.AppendLine("status: " + (f.IsBad ? "BAD - " + f.BadReason : "OK"));
            sb.AppendFormat(CultureInfo.InvariantCulture, "shoe side: {0}\r\n", f.ShoeSide);
            sb.AppendFormat(CultureInfo.InvariantCulture, "rotation angle: {0:0.0} deg\r\n", f.RotationAngleDegrees);
            sb.AppendFormat(CultureInfo.InvariantCulture, "quality: {0:0.0}/100\r\n", f.QualityScore);
            sb.AppendLine();
            sb.AppendFormat(CultureInfo.InvariantCulture, "area: {0}\r\n", f.Area);
            sb.AppendFormat(CultureInfo.InvariantCulture, "main component ratio: {0:0.000}\r\n", f.MainComponentRatio);
            sb.AppendFormat(CultureInfo.InvariantCulture, "islands: {0}  island score: {1:0.0}\r\n", f.IslandCount, f.IslandScore);
            sb.AppendFormat(CultureInfo.InvariantCulture, "distortion: {0:0.0}\r\n", f.DistortionScore);
            sb.AppendFormat(CultureInfo.InvariantCulture, "local defect: {0:0.0}\r\n", f.LocalDefectScore);
            sb.AppendFormat(CultureInfo.InvariantCulture, "defect score: {0:0.0} / threshold {1}\r\n", f.DefectScore, f.DefectThreshold);
            sb.AppendFormat(CultureInfo.InvariantCulture, "box ratio: {0:0.000}\r\n", SafeBoxRatio(f));
            sb.AppendFormat(CultureInfo.InvariantCulture, "front/rear ball width: {0:0.0} / {1:0.0}\r\n", f.FrontBallWidth, f.RearBallWidth);
            sb.AppendFormat(CultureInfo.InvariantCulture, "PCA width/height: {0:0.0} / {1:0.0}\r\n", f.Width, f.Height);
            if (points != null && points.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== Matched Points ===");
                for (int i = 0; i < points.Length; i++)
                {
                    ShapePointLocation loc = shapeModel.ToShapeLocation(shapeModel.MeasurementPoints[i]);
                    sb.AppendFormat(CultureInfo.InvariantCulture, "P{0}: ({1:0.0}, {2:0.0})  T={3:0.000}, SideRatio={4:0.000}\r\n", i + 1, points[i].X, points[i].Y, loc.T, loc.SideRatio);
                }
            }

            AppendMatcherDebugInfo(sb);
            return sb.ToString();
        }

        private void ShowActiveResultAt(int index)
        {
            List<string> files = GetActiveResultFiles();
            List<string> infos = GetActiveResultInfos();
            List<string> originals = GetActiveOriginalFiles();
            if (files.Count == 0)
            {
                txtResultInfo.Text = rdoShowBad.Checked ? "No BAD masks in the last batch." : "No GOOD masks in the last batch.";
                SetNavigationState();
                return;
            }

            if (index < 0)
            {
                index = files.Count - 1;
            }
            if (index >= files.Count)
            {
                index = 0;
            }

            SetActiveResultIndex(index);
            string resultPath = files[index];
            pictureResult.Image = LoadBitmapWithoutLock(resultPath);
            if (index < originals.Count && File.Exists(originals[index]))
            {
                pictureMask.Image = LoadBitmapWithoutLock(originals[index]);
            }
            lblCurrentFile.Text = string.Format("{0} {1}/{2}: {3}", rdoShowBad.Checked ? "BAD" : "GOOD", index + 1, files.Count, Path.GetFileName(resultPath));
            lblStatus.Text = resultPath;
            if (index < infos.Count)
            {
                txtResultInfo.Text = infos[index];
            }
            SetNavigationState();
        }


        private ExperimentResult GetCurrentExperimentResult()
        {
            string currentOriginal = GetCurrentOriginalPath();
            if (string.IsNullOrEmpty(currentOriginal))
            {
                return null;
            }

            return lastExperimentResults.FirstOrDefault(r => string.Equals(r.FilePath, currentOriginal, StringComparison.OrdinalIgnoreCase));
        }

        private string GetCurrentOriginalPath()
        {
            List<string> originals = GetActiveOriginalFiles();
            int index = GetActiveResultIndex();
            if (index < 0 || index >= originals.Count)
            {
                return null;
            }
            return originals[index];
        }

        private static int CountManualPoints(ManualLabel label)
        {
            if (label == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < 6; i++)
            {
                double x;
                double y;
                if (TryParseManualPoint(label, i, out x, out y))
                {
                    count++;
                }
            }
            return count;
        }
        private static string FormatManualLabel(ManualLabel label)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "=== Manual Label ===\r\ntrue_front_sign: {0}\r\ntrue_shoe_side: {1}\r\ntrue_bad: {2}\r\nmanual point labels: {3}/6\r\nmemo: {4}",
                label.TrueFrontSign,
                label.TrueShoeSide,
                label.TrueBad,
                CountManualPoints(label),
                label.Memo);
        }
        private List<string> GetActiveResultFiles()
        {
            return rdoShowBad.Checked ? badResultFiles : goodResultFiles;
        }

        private List<string> GetActiveResultInfos()
        {
            return rdoShowBad.Checked ? badResultInfoTexts : goodResultInfoTexts;
        }

        private List<string> GetActiveOriginalFiles()
        {
            return rdoShowBad.Checked ? badOriginalFiles : goodOriginalFiles;
        }

        private int GetResultPageStep()
        {
            return rdoStep10.Checked ? 10 : 1;
        }

        private int GetActiveResultIndex()
        {
            return rdoShowBad.Checked ? badResultFileIndex : goodResultFileIndex;
        }

        private void SetActiveResultIndex(int index)
        {
            resultFileIndex = index;
            if (rdoShowBad.Checked)
            {
                badResultFileIndex = index;
            }
            else
            {
                goodResultFileIndex = index;
            }
        }

        private void SetNavigationState()
        {
            bool enabled = GetActiveResultFiles().Count > 0;
            btnPrevResult.Enabled = enabled;
            btnNextResult.Enabled = enabled;
            rdoShowGood.Enabled = goodResultFiles.Count > 0;
            rdoShowBad.Enabled = badResultFiles.Count > 0;
        }

        private void SyncIslandThreshold()
        {
            analyzer.DefectThreshold = trackIslandBox.Value;
        }

        private void UpdateDefectThresholdLabel()
        {
            lblIslandBox.Text = string.Format("Defect score >= {0} => BAD", trackIslandBox.Value);
            SyncIslandThreshold();
        }

        private static string FindDefaultMaskRoot()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string projectFolder = "\uAE30\uB9D0\uD140\uD504\uB85C\uC81D\uD2B8";
            string maskFolder = "\uB9C8\uC2A4\uD06C\uC774\uBBF8\uC9C0";
            string koreanDesktop = "\uBC14\uD0D5 \uD654\uBA74";
            string[] candidates =
            {
                Path.Combine(desktop, projectFolder, maskFolder),
                Path.Combine(user, "OneDrive", koreanDesktop, projectFolder, maskFolder)
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private string FormatMatchInfo(ShapeFeatures f, PointF[] points)
        {
            var sb = new StringBuilder(FormatMaskInfo(f));
            sb.AppendLine();
            if (points != null && points.Length > 0)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    ShapePointLocation loc = shapeModel.ToShapeLocation(shapeModel.MeasurementPoints[i]);
                    sb.AppendFormat(CultureInfo.InvariantCulture, "P{0}: ({1:0.0}, {2:0.0})  T={3:0.000}, SideRatio={4:0.000}", i + 1, points[i].X, points[i].Y, loc.T, loc.SideRatio);
                    sb.AppendLine();
                }
            }

            AppendMatcherDebugInfo(sb);
            return sb.ToString();
        }

        private void AppendMatcherDebugInfo(StringBuilder sb)
        {
            if (string.IsNullOrWhiteSpace(matcher.LastDebugInfo))
            {
                return;
            }

            sb.AppendLine();
            sb.Append(matcher.LastDebugInfo);
        }

        private void pictureReference_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            PointF[] outline = shapeModel.OutlinePoints.Select(ModelToReferenceView).ToArray();
            using (Pen pen = new Pen(Color.DodgerBlue, 2))
            {
                e.Graphics.DrawPolygon(pen, outline);
            }

            for (int i = 0; i < shapeModel.MeasurementPoints.Length; i++)
            {
                PointF p = ModelToReferenceView(shapeModel.MeasurementPoints[i]);
                using (Brush brush = new SolidBrush(Color.Red))
                using (Pen pen = new Pen(Color.Black, 1))
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                using (Brush text = new SolidBrush(Color.Black))
                {
                    e.Graphics.FillEllipse(brush, p.X - 6, p.Y - 6, 12, 12);
                    e.Graphics.DrawEllipse(pen, p.X - 6, p.Y - 6, 12, 12);
                    e.Graphics.DrawString((i + 1).ToString(), font, text, p.X + 7, p.Y - 7);
                }
            }
        }

        private void pictureReference_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < shapeModel.MeasurementPoints.Length; i++)
            {
                PointF p = ModelToReferenceView(shapeModel.MeasurementPoints[i]);
                float dx = p.X - e.X;
                float dy = p.Y - e.Y;
                if (dx * dx + dy * dy <= 100)
                {
                    draggingPointIndex = i;
                    return;
                }
            }
        }

        private void pictureReference_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingPointIndex < 0)
            {
                return;
            }

            PointF modelPoint = ReferenceViewToModel(new PointF(e.X, e.Y));
            shapeModel.SetMeasurementPoint(draggingPointIndex, modelPoint);
            txtPointInfo.Text = shapeModel.GetMeasurementInfo();
            pictureReference.Invalidate();
        }

        private void pictureReference_MouseUp(object sender, MouseEventArgs e)
        {
            draggingPointIndex = -1;
        }

        private PointF ModelToReferenceView(PointF p)
        {
            RectangleF bounds = GetModelBounds();
            float scale = Math.Min((pictureReference.Width - 40) / bounds.Width, (pictureReference.Height - 40) / bounds.Height);
            float x = 20 + (p.X - bounds.Left) * scale;
            float y = 20 + (bounds.Bottom - p.Y) * scale;
            return new PointF(x, y);
        }

        private PointF ReferenceViewToModel(PointF p)
        {
            RectangleF bounds = GetModelBounds();
            float scale = Math.Min((pictureReference.Width - 40) / bounds.Width, (pictureReference.Height - 40) / bounds.Height);
            float x = bounds.Left + (p.X - 20) / scale;
            float y = bounds.Bottom - (p.Y - 20) / scale;
            return new PointF(x, y);
        }

        private RectangleF GetModelBounds()
        {
            float left = shapeModel.OutlinePoints.Min(p => p.X);
            float right = shapeModel.OutlinePoints.Max(p => p.X);
            float top = shapeModel.OutlinePoints.Max(p => p.Y);
            float bottom = shapeModel.OutlinePoints.Min(p => p.Y);
            return RectangleF.FromLTRB(left, bottom, right, top);
        }
    }
}









