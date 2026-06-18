namespace MidTermproject_21100210
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pictureReference = new System.Windows.Forms.PictureBox();
            this.pictureMask = new System.Windows.Forms.PictureBox();
            this.pictureResult = new System.Windows.Forms.PictureBox();
            this.txtPointInfo = new System.Windows.Forms.TextBox();
            this.txtResultInfo = new System.Windows.Forms.TextBox();
            this.btnReadFile = new System.Windows.Forms.Button();
            this.btnMatch = new System.Windows.Forms.Button();
            this.btnMatchAll = new System.Windows.Forms.Button();
            this.btnResetPoints = new System.Windows.Forms.Button();
            this.btnPrevResult = new System.Windows.Forms.Button();
            this.btnNextResult = new System.Windows.Forms.Button();
            this.panelStep = new System.Windows.Forms.Panel();
            this.rdoStep1 = new System.Windows.Forms.RadioButton();
            this.rdoStep10 = new System.Windows.Forms.RadioButton();
            this.panelFilter = new System.Windows.Forms.Panel();
            this.rdoShowGood = new System.Windows.Forms.RadioButton();
            this.rdoShowBad = new System.Windows.Forms.RadioButton();
            this.panelOrientationMode = new System.Windows.Forms.Panel();
            this.rdoAutoOrientation = new System.Windows.Forms.RadioButton();
            this.rdoManualOrientation = new System.Windows.Forms.RadioButton();
            this.panelShoeSide = new System.Windows.Forms.Panel();
            this.rdoLeftFoot = new System.Windows.Forms.RadioButton();
            this.rdoRightFoot = new System.Windows.Forms.RadioButton();
            this.panelFrontDirection = new System.Windows.Forms.Panel();
            this.rdoFrontUp = new System.Windows.Forms.RadioButton();
            this.rdoFrontDown = new System.Windows.Forms.RadioButton();
            this.lblOrientationMode = new System.Windows.Forms.Label();
            this.lblShoeSide = new System.Windows.Forms.Label();
            this.lblFrontDirection = new System.Windows.Forms.Label();
            this.txtMaskRoot = new System.Windows.Forms.TextBox();
            this.lblCurrentFile = new System.Windows.Forms.Label();
            this.lblBatchSummary = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.trackIslandBox = new System.Windows.Forms.TrackBar();
            this.lblIslandBox = new System.Windows.Forms.Label();
            this.labelReference = new System.Windows.Forms.Label();
            this.labelMask = new System.Windows.Forms.Label();
            this.labelResult = new System.Windows.Forms.Label();
            this.btnLabelCurrent = new System.Windows.Forms.Button();
            this.btnExportLabels = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureReference)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureMask)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureResult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackIslandBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureReference
            // 
            this.pictureReference.BackColor = System.Drawing.Color.White;
            this.pictureReference.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureReference.Location = new System.Drawing.Point(17, 112);
            this.pictureReference.Margin = new System.Windows.Forms.Padding(4);
            this.pictureReference.Name = "pictureReference";
            this.pictureReference.Size = new System.Drawing.Size(442, 779);
            this.pictureReference.TabIndex = 0;
            this.pictureReference.TabStop = false;
            this.pictureReference.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureReference_Paint);
            this.pictureReference.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureReference_MouseDown);
            this.pictureReference.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureReference_MouseMove);
            this.pictureReference.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureReference_MouseUp);
            // 
            // pictureMask
            // 
            this.pictureMask.BackColor = System.Drawing.Color.DimGray;
            this.pictureMask.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureMask.Location = new System.Drawing.Point(483, 112);
            this.pictureMask.Margin = new System.Windows.Forms.Padding(4);
            this.pictureMask.Name = "pictureMask";
            this.pictureMask.Size = new System.Drawing.Size(599, 472);
            this.pictureMask.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureMask.TabIndex = 1;
            this.pictureMask.TabStop = false;
            // 
            // pictureResult
            // 
            this.pictureResult.BackColor = System.Drawing.Color.DimGray;
            this.pictureResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureResult.Location = new System.Drawing.Point(1106, 112);
            this.pictureResult.Margin = new System.Windows.Forms.Padding(4);
            this.pictureResult.Name = "pictureResult";
            this.pictureResult.Size = new System.Drawing.Size(599, 472);
            this.pictureResult.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureResult.TabIndex = 2;
            this.pictureResult.TabStop = false;
            // 
            // txtPointInfo
            // 
            this.txtPointInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtPointInfo.Location = new System.Drawing.Point(483, 674);
            this.txtPointInfo.Margin = new System.Windows.Forms.Padding(4);
            this.txtPointInfo.Multiline = true;
            this.txtPointInfo.Name = "txtPointInfo";
            this.txtPointInfo.ReadOnly = true;
            this.txtPointInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPointInfo.Size = new System.Drawing.Size(598, 216);
            this.txtPointInfo.TabIndex = 3;
            // 
            // txtResultInfo
            // 
            this.txtResultInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtResultInfo.Location = new System.Drawing.Point(1106, 674);
            this.txtResultInfo.Margin = new System.Windows.Forms.Padding(4);
            this.txtResultInfo.Multiline = true;
            this.txtResultInfo.Name = "txtResultInfo";
            this.txtResultInfo.ReadOnly = true;
            this.txtResultInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResultInfo.Size = new System.Drawing.Size(598, 216);
            this.txtResultInfo.TabIndex = 4;
            // 
            // btnReadFile
            // 
            this.btnReadFile.Location = new System.Drawing.Point(483, 18);
            this.btnReadFile.Margin = new System.Windows.Forms.Padding(4);
            this.btnReadFile.Name = "btnReadFile";
            this.btnReadFile.Size = new System.Drawing.Size(136, 42);
            this.btnReadFile.TabIndex = 5;
            this.btnReadFile.Text = "Read File";
            this.btnReadFile.UseVisualStyleBackColor = true;
            this.btnReadFile.Click += new System.EventHandler(this.btnReadFile_Click);
            // 
            // btnMatch
            // 
            this.btnMatch.Location = new System.Drawing.Point(627, 18);
            this.btnMatch.Margin = new System.Windows.Forms.Padding(4);
            this.btnMatch.Name = "btnMatch";
            this.btnMatch.Size = new System.Drawing.Size(136, 42);
            this.btnMatch.TabIndex = 6;
            this.btnMatch.Text = "Match";
            this.btnMatch.UseVisualStyleBackColor = true;
            this.btnMatch.Click += new System.EventHandler(this.btnMatch_Click);
            // 
            // btnMatchAll
            // 
            this.btnMatchAll.Location = new System.Drawing.Point(771, 18);
            this.btnMatchAll.Margin = new System.Windows.Forms.Padding(4);
            this.btnMatchAll.Name = "btnMatchAll";
            this.btnMatchAll.Size = new System.Drawing.Size(214, 42);
            this.btnMatchAll.TabIndex = 7;
            this.btnMatchAll.Text = "Match All Folder Files";
            this.btnMatchAll.UseVisualStyleBackColor = true;
            this.btnMatchAll.Click += new System.EventHandler(this.btnMatchAll_Click);
            // 
            // btnResetPoints
            // 
            this.btnResetPoints.Location = new System.Drawing.Point(994, 18);
            this.btnResetPoints.Margin = new System.Windows.Forms.Padding(4);
            this.btnResetPoints.Name = "btnResetPoints";
            this.btnResetPoints.Size = new System.Drawing.Size(136, 42);
            this.btnResetPoints.TabIndex = 8;
            this.btnResetPoints.Text = "Reset Points";
            this.btnResetPoints.UseVisualStyleBackColor = true;
            this.btnResetPoints.Click += new System.EventHandler(this.btnResetPoints_Click);
            // 
            // btnPrevResult
            // 
            this.btnPrevResult.Location = new System.Drawing.Point(1139, 18);
            this.btnPrevResult.Margin = new System.Windows.Forms.Padding(4);
            this.btnPrevResult.Name = "btnPrevResult";
            this.btnPrevResult.Size = new System.Drawing.Size(107, 42);
            this.btnPrevResult.TabIndex = 16;
            this.btnPrevResult.Text = "Prev";
            this.btnPrevResult.UseVisualStyleBackColor = true;
            this.btnPrevResult.Click += new System.EventHandler(this.btnPrevResult_Click);
            // 
            // btnNextResult
            // 
            this.btnNextResult.Location = new System.Drawing.Point(1254, 18);
            this.btnNextResult.Margin = new System.Windows.Forms.Padding(4);
            this.btnNextResult.Name = "btnNextResult";
            this.btnNextResult.Size = new System.Drawing.Size(107, 42);
            this.btnNextResult.TabIndex = 17;
            this.btnNextResult.Text = "Next";
            this.btnNextResult.UseVisualStyleBackColor = true;
            this.btnNextResult.Click += new System.EventHandler(this.btnNextResult_Click);
            // 
            // panelStep
            // 
            this.panelStep.Controls.Add(this.rdoStep10);
            this.panelStep.Controls.Add(this.rdoStep1);
            this.panelStep.Location = new System.Drawing.Point(1368, 18);
            this.panelStep.Margin = new System.Windows.Forms.Padding(4);
            this.panelStep.Name = "panelStep";
            this.panelStep.Size = new System.Drawing.Size(112, 42);
            this.panelStep.TabIndex = 29;
            // 
            // rdoStep1
            // 
            this.rdoStep1.AutoSize = true;
            this.rdoStep1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoStep1.Location = new System.Drawing.Point(0, 9);
            this.rdoStep1.Margin = new System.Windows.Forms.Padding(4);
            this.rdoStep1.Name = "rdoStep1";
            this.rdoStep1.Size = new System.Drawing.Size(41, 25);
            this.rdoStep1.TabIndex = 27;
            this.rdoStep1.Text = "1";
            this.rdoStep1.UseVisualStyleBackColor = true;
            // 
            // rdoStep10
            // 
            this.rdoStep10.AutoSize = true;
            this.rdoStep10.Checked = true;
            this.rdoStep10.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoStep10.Location = new System.Drawing.Point(50, 9);
            this.rdoStep10.Margin = new System.Windows.Forms.Padding(4);
            this.rdoStep10.Name = "rdoStep10";
            this.rdoStep10.Size = new System.Drawing.Size(52, 25);
            this.rdoStep10.TabIndex = 28;
            this.rdoStep10.TabStop = true;
            this.rdoStep10.Text = "10";
            this.rdoStep10.UseVisualStyleBackColor = true;
            // 
            // panelFilter
            // 
            this.panelFilter.Controls.Add(this.rdoShowBad);
            this.panelFilter.Controls.Add(this.rdoShowGood);
            this.panelFilter.Location = new System.Drawing.Point(1485, 18);
            this.panelFilter.Margin = new System.Windows.Forms.Padding(4);
            this.panelFilter.Name = "panelFilter";
            this.panelFilter.Size = new System.Drawing.Size(220, 42);
            this.panelFilter.TabIndex = 30;
            // 
            // rdoShowGood
            // 
            this.rdoShowGood.AutoSize = true;
            this.rdoShowGood.Checked = true;
            this.rdoShowGood.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoShowGood.Location = new System.Drawing.Point(0, 7);
            this.rdoShowGood.Margin = new System.Windows.Forms.Padding(4);
            this.rdoShowGood.Name = "rdoShowGood";
            this.rdoShowGood.Size = new System.Drawing.Size(90, 25);
            this.rdoShowGood.TabIndex = 25;
            this.rdoShowGood.TabStop = true;
            this.rdoShowGood.Text = "GOOD";
            this.rdoShowGood.UseVisualStyleBackColor = true;
            this.rdoShowGood.CheckedChanged += new System.EventHandler(this.rdoResultFilter_CheckedChanged);
            // 
            // rdoShowBad
            // 
            this.rdoShowBad.AutoSize = true;
            this.rdoShowBad.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoShowBad.Location = new System.Drawing.Point(103, 7);
            this.rdoShowBad.Margin = new System.Windows.Forms.Padding(4);
            this.rdoShowBad.Name = "rdoShowBad";
            this.rdoShowBad.Size = new System.Drawing.Size(74, 25);
            this.rdoShowBad.TabIndex = 26;
            this.rdoShowBad.Text = "BAD";
            this.rdoShowBad.UseVisualStyleBackColor = true;
            this.rdoShowBad.CheckedChanged += new System.EventHandler(this.rdoResultFilter_CheckedChanged);
            // 
            // panelOrientationMode
            // 
            this.panelOrientationMode.Controls.Add(this.rdoManualOrientation);
            this.panelOrientationMode.Controls.Add(this.rdoAutoOrientation);
            this.panelOrientationMode.Location = new System.Drawing.Point(620, 67);
            this.panelOrientationMode.Margin = new System.Windows.Forms.Padding(4);
            this.panelOrientationMode.Name = "panelOrientationMode";
            this.panelOrientationMode.Size = new System.Drawing.Size(205, 35);
            this.panelOrientationMode.TabIndex = 36;
            // 
            // rdoAutoOrientation
            // 
            this.rdoAutoOrientation.AutoSize = true;
            this.rdoAutoOrientation.Checked = true;
            this.rdoAutoOrientation.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoAutoOrientation.Location = new System.Drawing.Point(0, 5);
            this.rdoAutoOrientation.Margin = new System.Windows.Forms.Padding(4);
            this.rdoAutoOrientation.Name = "rdoAutoOrientation";
            this.rdoAutoOrientation.Size = new System.Drawing.Size(73, 25);
            this.rdoAutoOrientation.TabIndex = 33;
            this.rdoAutoOrientation.TabStop = true;
            this.rdoAutoOrientation.Text = "Auto";
            this.rdoAutoOrientation.UseVisualStyleBackColor = true;
            this.rdoAutoOrientation.CheckedChanged += new System.EventHandler(this.rdoOrientationMode_CheckedChanged);
            // 
            // rdoManualOrientation
            // 
            this.rdoManualOrientation.AutoSize = true;
            this.rdoManualOrientation.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoManualOrientation.Location = new System.Drawing.Point(94, 5);
            this.rdoManualOrientation.Margin = new System.Windows.Forms.Padding(4);
            this.rdoManualOrientation.Name = "rdoManualOrientation";
            this.rdoManualOrientation.Size = new System.Drawing.Size(94, 25);
            this.rdoManualOrientation.TabIndex = 34;
            this.rdoManualOrientation.Text = "Manual";
            this.rdoManualOrientation.UseVisualStyleBackColor = true;
            this.rdoManualOrientation.CheckedChanged += new System.EventHandler(this.rdoOrientationMode_CheckedChanged);
            // 
            // panelShoeSide
            // 
            this.panelShoeSide.Controls.Add(this.rdoRightFoot);
            this.panelShoeSide.Controls.Add(this.rdoLeftFoot);
            this.panelShoeSide.Location = new System.Drawing.Point(956, 67);
            this.panelShoeSide.Margin = new System.Windows.Forms.Padding(4);
            this.panelShoeSide.Name = "panelShoeSide";
            this.panelShoeSide.Size = new System.Drawing.Size(190, 35);
            this.panelShoeSide.TabIndex = 37;
            // 
            // rdoLeftFoot
            // 
            this.rdoLeftFoot.AutoSize = true;
            this.rdoLeftFoot.Checked = true;
            this.rdoLeftFoot.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoLeftFoot.Location = new System.Drawing.Point(0, 5);
            this.rdoLeftFoot.Margin = new System.Windows.Forms.Padding(4);
            this.rdoLeftFoot.Name = "rdoLeftFoot";
            this.rdoLeftFoot.Size = new System.Drawing.Size(70, 25);
            this.rdoLeftFoot.TabIndex = 35;
            this.rdoLeftFoot.TabStop = true;
            this.rdoLeftFoot.Text = "Left";
            this.rdoLeftFoot.UseVisualStyleBackColor = true;
            // 
            // rdoRightFoot
            // 
            this.rdoRightFoot.AutoSize = true;
            this.rdoRightFoot.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoRightFoot.Location = new System.Drawing.Point(88, 5);
            this.rdoRightFoot.Margin = new System.Windows.Forms.Padding(4);
            this.rdoRightFoot.Name = "rdoRightFoot";
            this.rdoRightFoot.Size = new System.Drawing.Size(82, 25);
            this.rdoRightFoot.TabIndex = 36;
            this.rdoRightFoot.Text = "Right";
            this.rdoRightFoot.UseVisualStyleBackColor = true;
            // 
            // panelFrontDirection
            // 
            this.panelFrontDirection.Controls.Add(this.rdoFrontDown);
            this.panelFrontDirection.Controls.Add(this.rdoFrontUp);
            this.panelFrontDirection.Location = new System.Drawing.Point(1284, 67);
            this.panelFrontDirection.Margin = new System.Windows.Forms.Padding(4);
            this.panelFrontDirection.Name = "panelFrontDirection";
            this.panelFrontDirection.Size = new System.Drawing.Size(190, 35);
            this.panelFrontDirection.TabIndex = 38;
            // 
            // rdoFrontUp
            // 
            this.rdoFrontUp.AutoSize = true;
            this.rdoFrontUp.Checked = true;
            this.rdoFrontUp.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoFrontUp.Location = new System.Drawing.Point(0, 5);
            this.rdoFrontUp.Margin = new System.Windows.Forms.Padding(4);
            this.rdoFrontUp.Name = "rdoFrontUp";
            this.rdoFrontUp.Size = new System.Drawing.Size(56, 25);
            this.rdoFrontUp.TabIndex = 37;
            this.rdoFrontUp.TabStop = true;
            this.rdoFrontUp.Text = "Up";
            this.rdoFrontUp.UseVisualStyleBackColor = true;
            // 
            // rdoFrontDown
            // 
            this.rdoFrontDown.AutoSize = true;
            this.rdoFrontDown.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.rdoFrontDown.Location = new System.Drawing.Point(78, 5);
            this.rdoFrontDown.Margin = new System.Windows.Forms.Padding(4);
            this.rdoFrontDown.Name = "rdoFrontDown";
            this.rdoFrontDown.Size = new System.Drawing.Size(80, 25);
            this.rdoFrontDown.TabIndex = 38;
            this.rdoFrontDown.Text = "Down";
            this.rdoFrontDown.UseVisualStyleBackColor = true;
            // 
            // lblOrientationMode
            // 
            this.lblOrientationMode.AutoSize = true;
            this.lblOrientationMode.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.lblOrientationMode.Location = new System.Drawing.Point(483, 73);
            this.lblOrientationMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOrientationMode.Name = "lblOrientationMode";
            this.lblOrientationMode.Size = new System.Drawing.Size(126, 21);
            this.lblOrientationMode.TabIndex = 39;
            this.lblOrientationMode.Text = "Run Direction";
            // 
            // lblShoeSide
            // 
            this.lblShoeSide.AutoSize = true;
            this.lblShoeSide.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.lblShoeSide.Location = new System.Drawing.Point(848, 73);
            this.lblShoeSide.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblShoeSide.Name = "lblShoeSide";
            this.lblShoeSide.Size = new System.Drawing.Size(95, 21);
            this.lblShoeSide.TabIndex = 40;
            this.lblShoeSide.Text = "Shoe Side";
            // 
            // lblFrontDirection
            // 
            this.lblFrontDirection.AutoSize = true;
            this.lblFrontDirection.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.lblFrontDirection.Location = new System.Drawing.Point(1170, 73);
            this.lblFrontDirection.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFrontDirection.Name = "lblFrontDirection";
            this.lblFrontDirection.Size = new System.Drawing.Size(96, 21);
            this.lblFrontDirection.TabIndex = 41;
            this.lblFrontDirection.Text = "Front Side";
            // 
            // txtMaskRoot
            // 
            this.txtMaskRoot.Location = new System.Drawing.Point(483, 949);
            this.txtMaskRoot.Margin = new System.Windows.Forms.Padding(4);
            this.txtMaskRoot.Name = "txtMaskRoot";
            this.txtMaskRoot.Size = new System.Drawing.Size(1221, 28);
            this.txtMaskRoot.TabIndex = 9;
            // 
            // lblCurrentFile
            // 
            this.lblCurrentFile.AutoSize = true;
            this.lblCurrentFile.Location = new System.Drawing.Point(483, 595);
            this.lblCurrentFile.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCurrentFile.Name = "lblCurrentFile";
            this.lblCurrentFile.Size = new System.Drawing.Size(108, 18);
            this.lblCurrentFile.TabIndex = 10;
            this.lblCurrentFile.Text = "No mask file";
            // 
            // lblBatchSummary
            // 
            this.lblBatchSummary.AutoSize = true;
            this.lblBatchSummary.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.lblBatchSummary.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblBatchSummary.Location = new System.Drawing.Point(1106, 595);
            this.lblBatchSummary.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblBatchSummary.Name = "lblBatchSummary";
            this.lblBatchSummary.Size = new System.Drawing.Size(234, 24);
            this.lblBatchSummary.TabIndex = 22;
            this.lblBatchSummary.Text = "OK 0 / BAD 0 / TOTAL 0";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(17, 949);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(59, 18);
            this.lblStatus.TabIndex = 11;
            this.lblStatus.Text = "Ready";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(483, 913);
            this.progressBar.Margin = new System.Windows.Forms.Padding(4);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(1223, 32);
            this.progressBar.TabIndex = 12;
            // 
            // trackIslandBox
            // 
            this.trackIslandBox.Location = new System.Drawing.Point(483, 990);
            this.trackIslandBox.Margin = new System.Windows.Forms.Padding(4);
            this.trackIslandBox.Maximum = 60;
            this.trackIslandBox.Minimum = 1;
            this.trackIslandBox.Name = "trackIslandBox";
            this.trackIslandBox.Size = new System.Drawing.Size(560, 69);
            this.trackIslandBox.TabIndex = 23;
            this.trackIslandBox.TickFrequency = 5;
            this.trackIslandBox.Value = 16;
            this.trackIslandBox.Scroll += new System.EventHandler(this.trackIslandBox_Scroll);
            // 
            // lblIslandBox
            // 
            this.lblIslandBox.AutoSize = true;
            this.lblIslandBox.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.lblIslandBox.Location = new System.Drawing.Point(1063, 997);
            this.lblIslandBox.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblIslandBox.Name = "lblIslandBox";
            this.lblIslandBox.Size = new System.Drawing.Size(273, 24);
            this.lblIslandBox.TabIndex = 24;
            this.lblIslandBox.Text = "Defect score >= 16 => BAD";
            // 
            // labelReference
            // 
            this.labelReference.AutoSize = true;
            this.labelReference.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.labelReference.Location = new System.Drawing.Point(17, 80);
            this.labelReference.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelReference.Name = "labelReference";
            this.labelReference.Size = new System.Drawing.Size(252, 21);
            this.labelReference.TabIndex = 13;
            this.labelReference.Text = "Reference Shape + 6 Points";
            // 
            // labelMask
            // 
            this.labelMask.AutoSize = true;
            this.labelMask.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.labelMask.Location = new System.Drawing.Point(483, 80);
            this.labelMask.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMask.Name = "labelMask";
            this.labelMask.Size = new System.Drawing.Size(104, 21);
            this.labelMask.TabIndex = 14;
            this.labelMask.Text = "Input Mask";
            // 
            // labelResult
            // 
            this.labelResult.AutoSize = true;
            this.labelResult.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.labelResult.Location = new System.Drawing.Point(1106, 80);
            this.labelResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelResult.Name = "labelResult";
            this.labelResult.Size = new System.Drawing.Size(123, 21);
            this.labelResult.TabIndex = 15;
            this.labelResult.Text = "Match Result";
            // 
            // btnLabelCurrent
            // 
            this.btnLabelCurrent.Location = new System.Drawing.Point(1346, 990);
            this.btnLabelCurrent.Margin = new System.Windows.Forms.Padding(4);
            this.btnLabelCurrent.Name = "btnLabelCurrent";
            this.btnLabelCurrent.Size = new System.Drawing.Size(158, 42);
            this.btnLabelCurrent.TabIndex = 31;
            this.btnLabelCurrent.Text = "Label Current";
            this.btnLabelCurrent.UseVisualStyleBackColor = true;
            this.btnLabelCurrent.Click += new System.EventHandler(this.btnLabelCurrent_Click);
            // 
            // btnExportLabels
            // 
            this.btnExportLabels.Location = new System.Drawing.Point(1512, 990);
            this.btnExportLabels.Margin = new System.Windows.Forms.Padding(4);
            this.btnExportLabels.Name = "btnExportLabels";
            this.btnExportLabels.Size = new System.Drawing.Size(192, 42);
            this.btnExportLabels.TabIndex = 32;
            this.btnExportLabels.Text = "Export Labels";
            this.btnExportLabels.UseVisualStyleBackColor = true;
            this.btnExportLabels.Click += new System.EventHandler(this.btnExportLabels_Click);            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1726, 1069);
            this.Controls.Add(this.lblFrontDirection);
            this.Controls.Add(this.lblShoeSide);
            this.Controls.Add(this.lblOrientationMode);
            this.Controls.Add(this.panelFrontDirection);
            this.Controls.Add(this.panelShoeSide);
            this.Controls.Add(this.panelOrientationMode);
            this.Controls.Add(this.btnExportLabels);
            this.Controls.Add(this.btnLabelCurrent);
            this.Controls.Add(this.panelFilter);
            this.Controls.Add(this.panelStep);
            this.Controls.Add(this.labelResult);
            this.Controls.Add(this.labelMask);
            this.Controls.Add(this.labelReference);
            this.Controls.Add(this.lblIslandBox);
            this.Controls.Add(this.trackIslandBox);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblCurrentFile);
            this.Controls.Add(this.lblBatchSummary);
            this.Controls.Add(this.txtMaskRoot);
            this.Controls.Add(this.btnNextResult);
            this.Controls.Add(this.btnPrevResult);
            this.Controls.Add(this.btnResetPoints);
            this.Controls.Add(this.btnMatchAll);
            this.Controls.Add(this.btnMatch);
            this.Controls.Add(this.btnReadFile);
            this.Controls.Add(this.txtResultInfo);
            this.Controls.Add(this.txtPointInfo);
            this.Controls.Add(this.pictureResult);
            this.Controls.Add(this.pictureMask);
            this.Controls.Add(this.pictureReference);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "Term Project - Shape Corresponding Points";
            ((System.ComponentModel.ISupportInitialize)(this.pictureReference)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureMask)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureResult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackIslandBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.PictureBox pictureReference;
        private System.Windows.Forms.PictureBox pictureMask;
        private System.Windows.Forms.PictureBox pictureResult;
        private System.Windows.Forms.TextBox txtPointInfo;
        private System.Windows.Forms.TextBox txtResultInfo;
        private System.Windows.Forms.Button btnReadFile;
        private System.Windows.Forms.Button btnMatch;
        private System.Windows.Forms.Button btnMatchAll;
        private System.Windows.Forms.Button btnResetPoints;
        private System.Windows.Forms.Button btnPrevResult;
        private System.Windows.Forms.Button btnNextResult;
        private System.Windows.Forms.Panel panelStep;
        private System.Windows.Forms.RadioButton rdoStep1;
        private System.Windows.Forms.RadioButton rdoStep10;
        private System.Windows.Forms.Panel panelFilter;
        private System.Windows.Forms.RadioButton rdoShowGood;
        private System.Windows.Forms.RadioButton rdoShowBad;
        private System.Windows.Forms.Panel panelOrientationMode;
        private System.Windows.Forms.RadioButton rdoAutoOrientation;
        private System.Windows.Forms.RadioButton rdoManualOrientation;
        private System.Windows.Forms.Panel panelShoeSide;
        private System.Windows.Forms.RadioButton rdoLeftFoot;
        private System.Windows.Forms.RadioButton rdoRightFoot;
        private System.Windows.Forms.Panel panelFrontDirection;
        private System.Windows.Forms.RadioButton rdoFrontUp;
        private System.Windows.Forms.RadioButton rdoFrontDown;
        private System.Windows.Forms.Label lblOrientationMode;
        private System.Windows.Forms.Label lblShoeSide;
        private System.Windows.Forms.Label lblFrontDirection;
        private System.Windows.Forms.TextBox txtMaskRoot;
        private System.Windows.Forms.Label lblCurrentFile;
        private System.Windows.Forms.Label lblBatchSummary;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TrackBar trackIslandBox;
        private System.Windows.Forms.Label lblIslandBox;
        private System.Windows.Forms.Label labelReference;
        private System.Windows.Forms.Label labelMask;
        private System.Windows.Forms.Label labelResult;
        private System.Windows.Forms.Button btnLabelCurrent;
        private System.Windows.Forms.Button btnExportLabels;
    }
}



