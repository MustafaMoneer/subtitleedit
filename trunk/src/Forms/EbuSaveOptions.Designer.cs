﻿namespace Nikse.SubtitleEdit.Forms
{
    partial class EbuSaveOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageHeader = new System.Windows.Forms.TabPage();
            this.textBoxCodePageNumber = new System.Windows.Forms.TextBox();
            this.contextMenuStripCodeTable = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.unitedStatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.multilingualToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.portugalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.canadaFrenchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nordicToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.numericUpDownMaxRows = new System.Windows.Forms.NumericUpDown();
            this.labelMaxRows = new System.Windows.Forms.Label();
            this.comboBoxDiscFormatCode = new System.Windows.Forms.ComboBox();
            this.labelDiskFormatCode = new System.Windows.Forms.Label();
            this.labelCodePageNumber = new System.Windows.Forms.Label();
            this.textBoxLanguageCode = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.numericUpDownMaxCharacters = new System.Windows.Forms.NumericUpDown();
            this.label12 = new System.Windows.Forms.Label();
            this.numericUpDownDiskSequenceNumber = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownTotalNumberOfDiscs = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownRevisionNumber = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textBoxTranslatorsName = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBoxSubtitleListReferenceCode = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBoxCharacterCodeTable = new System.Windows.Forms.ComboBox();
            this.buttonImport = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxTranslatedProgramTitle = new System.Windows.Forms.TextBox();
            this.textBoxTranslatedEpisodeTitle = new System.Windows.Forms.TextBox();
            this.textBoxOriginalEpisodeTitle = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxOriginalProgramTitle = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPageTextAndTiming = new System.Windows.Forms.TabPage();
            this.comboBoxJustificationCode = new System.Windows.Forms.ComboBox();
            this.label13 = new System.Windows.Forms.Label();
            this.tabPageErrors = new System.Windows.Forms.TabPage();
            this.textBoxErrors = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxCountryOfOrigin = new System.Windows.Forms.TextBox();
            this.labelCountryOfOrigin = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPageHeader.SuspendLayout();
            this.contextMenuStripCodeTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxRows)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxCharacters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDiskSequenceNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTotalNumberOfDiscs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRevisionNumber)).BeginInit();
            this.tabPageTextAndTiming.SuspendLayout();
            this.tabPageErrors.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.buttonOK.Location = new System.Drawing.Point(607, 445);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 21);
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "Save";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.buttonCancel.Location = new System.Drawing.Point(688, 445);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 21);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "C&ancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageHeader);
            this.tabControl1.Controls.Add(this.tabPageTextAndTiming);
            this.tabControl1.Controls.Add(this.tabPageErrors);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(758, 422);
            this.tabControl1.TabIndex = 32;
            // 
            // tabPageHeader
            // 
            this.tabPageHeader.Controls.Add(this.textBoxCountryOfOrigin);
            this.tabPageHeader.Controls.Add(this.labelCountryOfOrigin);
            this.tabPageHeader.Controls.Add(this.textBoxCodePageNumber);
            this.tabPageHeader.Controls.Add(this.numericUpDownMaxRows);
            this.tabPageHeader.Controls.Add(this.labelMaxRows);
            this.tabPageHeader.Controls.Add(this.comboBoxDiscFormatCode);
            this.tabPageHeader.Controls.Add(this.labelDiskFormatCode);
            this.tabPageHeader.Controls.Add(this.labelCodePageNumber);
            this.tabPageHeader.Controls.Add(this.textBoxLanguageCode);
            this.tabPageHeader.Controls.Add(this.label14);
            this.tabPageHeader.Controls.Add(this.numericUpDownMaxCharacters);
            this.tabPageHeader.Controls.Add(this.label12);
            this.tabPageHeader.Controls.Add(this.numericUpDownDiskSequenceNumber);
            this.tabPageHeader.Controls.Add(this.numericUpDownTotalNumberOfDiscs);
            this.tabPageHeader.Controls.Add(this.numericUpDownRevisionNumber);
            this.tabPageHeader.Controls.Add(this.label11);
            this.tabPageHeader.Controls.Add(this.label10);
            this.tabPageHeader.Controls.Add(this.label9);
            this.tabPageHeader.Controls.Add(this.textBoxTranslatorsName);
            this.tabPageHeader.Controls.Add(this.label8);
            this.tabPageHeader.Controls.Add(this.textBoxSubtitleListReferenceCode);
            this.tabPageHeader.Controls.Add(this.label7);
            this.tabPageHeader.Controls.Add(this.comboBoxCharacterCodeTable);
            this.tabPageHeader.Controls.Add(this.buttonImport);
            this.tabPageHeader.Controls.Add(this.label5);
            this.tabPageHeader.Controls.Add(this.textBoxTranslatedProgramTitle);
            this.tabPageHeader.Controls.Add(this.textBoxTranslatedEpisodeTitle);
            this.tabPageHeader.Controls.Add(this.textBoxOriginalEpisodeTitle);
            this.tabPageHeader.Controls.Add(this.label4);
            this.tabPageHeader.Controls.Add(this.label3);
            this.tabPageHeader.Controls.Add(this.label2);
            this.tabPageHeader.Controls.Add(this.textBoxOriginalProgramTitle);
            this.tabPageHeader.Controls.Add(this.label1);
            this.tabPageHeader.Location = new System.Drawing.Point(4, 22);
            this.tabPageHeader.Name = "tabPageHeader";
            this.tabPageHeader.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHeader.Size = new System.Drawing.Size(750, 396);
            this.tabPageHeader.TabIndex = 0;
            this.tabPageHeader.Text = "General subtitle information";
            this.tabPageHeader.UseVisualStyleBackColor = true;
            // 
            // textBoxCodePageNumber
            // 
            this.textBoxCodePageNumber.ContextMenuStrip = this.contextMenuStripCodeTable;
            this.textBoxCodePageNumber.Location = new System.Drawing.Point(147, 12);
            this.textBoxCodePageNumber.MaxLength = 3;
            this.textBoxCodePageNumber.Name = "textBoxCodePageNumber";
            this.textBoxCodePageNumber.Size = new System.Drawing.Size(219, 20);
            this.textBoxCodePageNumber.TabIndex = 61;
            // 
            // contextMenuStripCodeTable
            // 
            this.contextMenuStripCodeTable.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.unitedStatesToolStripMenuItem,
            this.multilingualToolStripMenuItem,
            this.portugalToolStripMenuItem,
            this.canadaFrenchToolStripMenuItem,
            this.nordicToolStripMenuItem});
            this.contextMenuStripCodeTable.Name = "contextMenuStripCodeTable";
            this.contextMenuStripCodeTable.Size = new System.Drawing.Size(185, 114);
            // 
            // unitedStatesToolStripMenuItem
            // 
            this.unitedStatesToolStripMenuItem.Name = "unitedStatesToolStripMenuItem";
            this.unitedStatesToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.unitedStatesToolStripMenuItem.Text = "United States (437)";
            this.unitedStatesToolStripMenuItem.Click += new System.EventHandler(this.unitedStatesToolStripMenuItem_Click);
            // 
            // multilingualToolStripMenuItem
            // 
            this.multilingualToolStripMenuItem.Name = "multilingualToolStripMenuItem";
            this.multilingualToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.multilingualToolStripMenuItem.Text = "Multilingual (850)";
            this.multilingualToolStripMenuItem.Click += new System.EventHandler(this.multilingualToolStripMenuItem_Click);
            // 
            // portugalToolStripMenuItem
            // 
            this.portugalToolStripMenuItem.Name = "portugalToolStripMenuItem";
            this.portugalToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.portugalToolStripMenuItem.Text = "Portugal (860)";
            this.portugalToolStripMenuItem.Click += new System.EventHandler(this.portugalToolStripMenuItem_Click);
            // 
            // canadaFrenchToolStripMenuItem
            // 
            this.canadaFrenchToolStripMenuItem.Name = "canadaFrenchToolStripMenuItem";
            this.canadaFrenchToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.canadaFrenchToolStripMenuItem.Text = "Canada-French (863)";
            this.canadaFrenchToolStripMenuItem.Click += new System.EventHandler(this.canadaFrenchToolStripMenuItem_Click);
            // 
            // nordicToolStripMenuItem
            // 
            this.nordicToolStripMenuItem.Name = "nordicToolStripMenuItem";
            this.nordicToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.nordicToolStripMenuItem.Text = "Nordic (865)";
            this.nordicToolStripMenuItem.Click += new System.EventHandler(this.nordicToolStripMenuItem_Click);
            // 
            // numericUpDownMaxRows
            // 
            this.numericUpDownMaxRows.Location = new System.Drawing.Point(613, 224);
            this.numericUpDownMaxRows.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.numericUpDownMaxRows.Name = "numericUpDownMaxRows";
            this.numericUpDownMaxRows.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownMaxRows.TabIndex = 12;
            // 
            // labelMaxRows
            // 
            this.labelMaxRows.AutoSize = true;
            this.labelMaxRows.Location = new System.Drawing.Point(473, 226);
            this.labelMaxRows.Name = "labelMaxRows";
            this.labelMaxRows.Size = new System.Drawing.Size(126, 13);
            this.labelMaxRows.TabIndex = 60;
            this.labelMaxRows.Text = "Max# of displayable rows";
            // 
            // comboBoxDiscFormatCode
            // 
            this.comboBoxDiscFormatCode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDiscFormatCode.FormattingEnabled = true;
            this.comboBoxDiscFormatCode.Items.AddRange(new object[] {
            "STL25.01",
            "STL30.01"});
            this.comboBoxDiscFormatCode.Location = new System.Drawing.Point(147, 39);
            this.comboBoxDiscFormatCode.Name = "comboBoxDiscFormatCode";
            this.comboBoxDiscFormatCode.Size = new System.Drawing.Size(219, 21);
            this.comboBoxDiscFormatCode.TabIndex = 1;
            // 
            // labelDiskFormatCode
            // 
            this.labelDiskFormatCode.AutoSize = true;
            this.labelDiskFormatCode.Location = new System.Drawing.Point(6, 42);
            this.labelDiskFormatCode.Name = "labelDiskFormatCode";
            this.labelDiskFormatCode.Size = new System.Drawing.Size(87, 13);
            this.labelDiskFormatCode.TabIndex = 58;
            this.labelDiskFormatCode.Text = "Disc format code";
            // 
            // labelCodePageNumber
            // 
            this.labelCodePageNumber.AutoSize = true;
            this.labelCodePageNumber.Location = new System.Drawing.Point(6, 15);
            this.labelCodePageNumber.Name = "labelCodePageNumber";
            this.labelCodePageNumber.Size = new System.Drawing.Size(97, 13);
            this.labelCodePageNumber.TabIndex = 56;
            this.labelCodePageNumber.Text = "Code page number";
            // 
            // textBoxLanguageCode
            // 
            this.textBoxLanguageCode.Location = new System.Drawing.Point(147, 94);
            this.textBoxLanguageCode.MaxLength = 32;
            this.textBoxLanguageCode.Name = "textBoxLanguageCode";
            this.textBoxLanguageCode.Size = new System.Drawing.Size(219, 20);
            this.textBoxLanguageCode.TabIndex = 3;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 97);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(82, 13);
            this.label14.TabIndex = 54;
            this.label14.Text = "Language code";
            // 
            // numericUpDownMaxCharacters
            // 
            this.numericUpDownMaxCharacters.Location = new System.Drawing.Point(613, 198);
            this.numericUpDownMaxCharacters.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.numericUpDownMaxCharacters.Name = "numericUpDownMaxCharacters";
            this.numericUpDownMaxCharacters.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownMaxCharacters.TabIndex = 11;
            this.numericUpDownMaxCharacters.ValueChanged += new System.EventHandler(this.numericUpDownMaxCharacters_ValueChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(473, 200);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(133, 13);
            this.label12.TabIndex = 51;
            this.label12.Text = "Max# of displayable chars ";
            // 
            // numericUpDownDiskSequenceNumber
            // 
            this.numericUpDownDiskSequenceNumber.Location = new System.Drawing.Point(613, 250);
            this.numericUpDownDiskSequenceNumber.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.numericUpDownDiskSequenceNumber.Name = "numericUpDownDiskSequenceNumber";
            this.numericUpDownDiskSequenceNumber.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownDiskSequenceNumber.TabIndex = 13;
            // 
            // numericUpDownTotalNumberOfDiscs
            // 
            this.numericUpDownTotalNumberOfDiscs.Location = new System.Drawing.Point(613, 276);
            this.numericUpDownTotalNumberOfDiscs.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.numericUpDownTotalNumberOfDiscs.Name = "numericUpDownTotalNumberOfDiscs";
            this.numericUpDownTotalNumberOfDiscs.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownTotalNumberOfDiscs.TabIndex = 14;
            // 
            // numericUpDownRevisionNumber
            // 
            this.numericUpDownRevisionNumber.Location = new System.Drawing.Point(613, 172);
            this.numericUpDownRevisionNumber.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.numericUpDownRevisionNumber.Name = "numericUpDownRevisionNumber";
            this.numericUpDownRevisionNumber.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownRevisionNumber.TabIndex = 10;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(473, 252);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(116, 13);
            this.label11.TabIndex = 49;
            this.label11.Text = "Disk sequence number";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(473, 278);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(108, 13);
            this.label10.TabIndex = 48;
            this.label10.Text = "Total number of disks";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(473, 174);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(86, 13);
            this.label9.TabIndex = 47;
            this.label9.Text = "Revision number";
            // 
            // textBoxTranslatorsName
            // 
            this.textBoxTranslatorsName.Location = new System.Drawing.Point(147, 224);
            this.textBoxTranslatorsName.MaxLength = 32;
            this.textBoxTranslatorsName.Name = "textBoxTranslatorsName";
            this.textBoxTranslatorsName.Size = new System.Drawing.Size(219, 20);
            this.textBoxTranslatorsName.TabIndex = 8;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 227);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 13);
            this.label8.TabIndex = 45;
            this.label8.Text = "Translator\'s name";
            // 
            // textBoxSubtitleListReferenceCode
            // 
            this.textBoxSubtitleListReferenceCode.Location = new System.Drawing.Point(147, 254);
            this.textBoxSubtitleListReferenceCode.MaxLength = 16;
            this.textBoxSubtitleListReferenceCode.Name = "textBoxSubtitleListReferenceCode";
            this.textBoxSubtitleListReferenceCode.Size = new System.Drawing.Size(219, 20);
            this.textBoxSubtitleListReferenceCode.TabIndex = 9;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 257);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(132, 13);
            this.label7.TabIndex = 43;
            this.label7.Text = "Subtitle list reference code";
            // 
            // comboBoxCharacterCodeTable
            // 
            this.comboBoxCharacterCodeTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCharacterCodeTable.FormattingEnabled = true;
            this.comboBoxCharacterCodeTable.Items.AddRange(new object[] {
            "Latin",
            "Latin/Cyrillic",
            "Latin/Arabic",
            "Latin/Greek",
            "Latin/Hebrew"});
            this.comboBoxCharacterCodeTable.Location = new System.Drawing.Point(147, 66);
            this.comboBoxCharacterCodeTable.Name = "comboBoxCharacterCodeTable";
            this.comboBoxCharacterCodeTable.Size = new System.Drawing.Size(219, 21);
            this.comboBoxCharacterCodeTable.TabIndex = 2;
            // 
            // buttonImport
            // 
            this.buttonImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonImport.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.buttonImport.Location = new System.Drawing.Point(613, 6);
            this.buttonImport.Name = "buttonImport";
            this.buttonImport.Size = new System.Drawing.Size(134, 21);
            this.buttonImport.TabIndex = 15;
            this.buttonImport.Text = "Import...";
            this.buttonImport.UseVisualStyleBackColor = true;
            this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 69);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(106, 13);
            this.label5.TabIndex = 40;
            this.label5.Text = "Character code table";
            // 
            // textBoxTranslatedProgramTitle
            // 
            this.textBoxTranslatedProgramTitle.Location = new System.Drawing.Point(147, 172);
            this.textBoxTranslatedProgramTitle.MaxLength = 32;
            this.textBoxTranslatedProgramTitle.Name = "textBoxTranslatedProgramTitle";
            this.textBoxTranslatedProgramTitle.Size = new System.Drawing.Size(219, 20);
            this.textBoxTranslatedProgramTitle.TabIndex = 6;
            // 
            // textBoxTranslatedEpisodeTitle
            // 
            this.textBoxTranslatedEpisodeTitle.Location = new System.Drawing.Point(147, 198);
            this.textBoxTranslatedEpisodeTitle.MaxLength = 32;
            this.textBoxTranslatedEpisodeTitle.Name = "textBoxTranslatedEpisodeTitle";
            this.textBoxTranslatedEpisodeTitle.Size = new System.Drawing.Size(219, 20);
            this.textBoxTranslatedEpisodeTitle.TabIndex = 7;
            // 
            // textBoxOriginalEpisodeTitle
            // 
            this.textBoxOriginalEpisodeTitle.Location = new System.Drawing.Point(147, 146);
            this.textBoxOriginalEpisodeTitle.MaxLength = 32;
            this.textBoxOriginalEpisodeTitle.Name = "textBoxOriginalEpisodeTitle";
            this.textBoxOriginalEpisodeTitle.Size = new System.Drawing.Size(219, 20);
            this.textBoxOriginalEpisodeTitle.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 201);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 13);
            this.label4.TabIndex = 36;
            this.label4.Text = "Translated episode title";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 175);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(117, 13);
            this.label3.TabIndex = 35;
            this.label3.Text = "Translated program title";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 149);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 34;
            this.label2.Text = "Original episode title";
            // 
            // textBoxOriginalProgramTitle
            // 
            this.textBoxOriginalProgramTitle.Location = new System.Drawing.Point(147, 120);
            this.textBoxOriginalProgramTitle.MaxLength = 32;
            this.textBoxOriginalProgramTitle.Name = "textBoxOriginalProgramTitle";
            this.textBoxOriginalProgramTitle.Size = new System.Drawing.Size(219, 20);
            this.textBoxOriginalProgramTitle.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 123);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 32;
            this.label1.Text = "Original program title";
            // 
            // tabPageTextAndTiming
            // 
            this.tabPageTextAndTiming.Controls.Add(this.comboBoxJustificationCode);
            this.tabPageTextAndTiming.Controls.Add(this.label13);
            this.tabPageTextAndTiming.Location = new System.Drawing.Point(4, 22);
            this.tabPageTextAndTiming.Name = "tabPageTextAndTiming";
            this.tabPageTextAndTiming.Size = new System.Drawing.Size(750, 396);
            this.tabPageTextAndTiming.TabIndex = 2;
            this.tabPageTextAndTiming.Text = "Text and timing information";
            this.tabPageTextAndTiming.UseVisualStyleBackColor = true;
            // 
            // comboBoxJustificationCode
            // 
            this.comboBoxJustificationCode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxJustificationCode.FormattingEnabled = true;
            this.comboBoxJustificationCode.Items.AddRange(new object[] {
            "unchanged presentation",
            "left-justified text",
            "centred text",
            "right-justified text"});
            this.comboBoxJustificationCode.Location = new System.Drawing.Point(145, 11);
            this.comboBoxJustificationCode.Name = "comboBoxJustificationCode";
            this.comboBoxJustificationCode.Size = new System.Drawing.Size(219, 21);
            this.comboBoxJustificationCode.TabIndex = 43;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 14);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(89, 13);
            this.label13.TabIndex = 44;
            this.label13.Text = "Justification code";
            // 
            // tabPageErrors
            // 
            this.tabPageErrors.Controls.Add(this.textBoxErrors);
            this.tabPageErrors.Controls.Add(this.label6);
            this.tabPageErrors.Location = new System.Drawing.Point(4, 22);
            this.tabPageErrors.Name = "tabPageErrors";
            this.tabPageErrors.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageErrors.Size = new System.Drawing.Size(750, 396);
            this.tabPageErrors.TabIndex = 1;
            this.tabPageErrors.Text = "Errors";
            this.tabPageErrors.UseVisualStyleBackColor = true;
            // 
            // textBoxErrors
            // 
            this.textBoxErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxErrors.Location = new System.Drawing.Point(6, 26);
            this.textBoxErrors.Multiline = true;
            this.textBoxErrors.Name = "textBoxErrors";
            this.textBoxErrors.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxErrors.Size = new System.Drawing.Size(909, 344);
            this.textBoxErrors.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 10);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(34, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Errors";
            // 
            // textBoxCountryOfOrigin
            // 
            this.textBoxCountryOfOrigin.Location = new System.Drawing.Point(147, 280);
            this.textBoxCountryOfOrigin.MaxLength = 3;
            this.textBoxCountryOfOrigin.Name = "textBoxCountryOfOrigin";
            this.textBoxCountryOfOrigin.Size = new System.Drawing.Size(219, 20);
            this.textBoxCountryOfOrigin.TabIndex = 62;
            // 
            // labelCountryOfOrigin
            // 
            this.labelCountryOfOrigin.AutoSize = true;
            this.labelCountryOfOrigin.Location = new System.Drawing.Point(6, 283);
            this.labelCountryOfOrigin.Name = "labelCountryOfOrigin";
            this.labelCountryOfOrigin.Size = new System.Drawing.Size(83, 13);
            this.labelCountryOfOrigin.TabIndex = 63;
            this.labelCountryOfOrigin.Text = "Country of origin";
            // 
            // EbuSaveOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(775, 477);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.MinimumSize = new System.Drawing.Size(648, 462);
            this.Name = "EbuSaveOptions";
            this.Text = "EbuSaveOptions";
            this.tabControl1.ResumeLayout(false);
            this.tabPageHeader.ResumeLayout(false);
            this.tabPageHeader.PerformLayout();
            this.contextMenuStripCodeTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxRows)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxCharacters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDiskSequenceNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTotalNumberOfDiscs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRevisionNumber)).EndInit();
            this.tabPageTextAndTiming.ResumeLayout(false);
            this.tabPageTextAndTiming.PerformLayout();
            this.tabPageErrors.ResumeLayout(false);
            this.tabPageErrors.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageHeader;
        private System.Windows.Forms.NumericUpDown numericUpDownDiskSequenceNumber;
        private System.Windows.Forms.NumericUpDown numericUpDownTotalNumberOfDiscs;
        private System.Windows.Forms.NumericUpDown numericUpDownRevisionNumber;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBoxTranslatorsName;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBoxSubtitleListReferenceCode;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBoxCharacterCodeTable;
        private System.Windows.Forms.Button buttonImport;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxTranslatedProgramTitle;
        private System.Windows.Forms.TextBox textBoxTranslatedEpisodeTitle;
        private System.Windows.Forms.TextBox textBoxOriginalEpisodeTitle;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxOriginalProgramTitle;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPageTextAndTiming;
        private System.Windows.Forms.TabPage tabPageErrors;
        private System.Windows.Forms.TextBox textBoxErrors;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxJustificationCode;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxCharacters;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBoxLanguageCode;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label labelCodePageNumber;
        private System.Windows.Forms.ComboBox comboBoxDiscFormatCode;
        private System.Windows.Forms.Label labelDiskFormatCode;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxRows;
        private System.Windows.Forms.Label labelMaxRows;
        private System.Windows.Forms.TextBox textBoxCodePageNumber;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripCodeTable;
        private System.Windows.Forms.ToolStripMenuItem unitedStatesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem multilingualToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem portugalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem canadaFrenchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nordicToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxCountryOfOrigin;
        private System.Windows.Forms.Label labelCountryOfOrigin;
    }
}