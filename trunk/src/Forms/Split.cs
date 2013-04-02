﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.SubtitleFormats;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class Split : Form
    {
        Subtitle _subtitle;
        SubtitleFormat _format;
        Encoding _encoding;
        public bool ShowBasic { get; private set; }
        int _totalNumberOfCharacters;
        bool _loading = true;
        List<Subtitle> _parts;
        string _fileName;

        public Split()
        {
            InitializeComponent();

            var l = Configuration.Settings.Language.Split;
            Text = l.Title;
            groupBoxSplitOptions.Text = l.SplitOptions;
            RadioButtonLines.Text = l.Lines;
            radioButtonCharacters.Text = l.Characters;
            labelNumberOfParts.Text = l.NumberOfEqualParts;
            groupBoxSubtitleInfo.Text = l.SubtitleInfo;
            groupBoxOutput.Text = l.Output;
            labelFileName.Text = l.FileName;
            labelChooseOutputFolder.Text = l.OutputFolder;
            labelOutputFormat.Text = Configuration.Settings.Language.Main.Controls.SubtitleFormat;
            labelEncoding.Text = Configuration.Settings.Language.Main.Controls.FileEncoding;
            groupBoxPreview.Text = Configuration.Settings.Language.General.Preview;
            buttonOpenOutputFolder.Text = Configuration.Settings.Language.Main.Menu.File.Open;

            listViewParts.Columns[0].Text = l.Lines;
            listViewParts.Columns[1].Text = l.Characters;
            listViewParts.Columns[2].Text = l.FileName;

            buttonSplit.Text = l.DoSplit;
            buttonBasic.Text = l.Basic;
            buttonCancel.Text = Configuration.Settings.Language.General.Cancel;

            comboBoxSubtitleFormats.Left = labelOutputFormat.Left + labelOutputFormat.Width + 3;
            comboBoxEncoding.Left = labelEncoding.Left + labelEncoding.Width + 3;

            FixLargeFonts();
        }

        private void FixLargeFonts()
        {
            Graphics graphics = this.CreateGraphics();
            SizeF textSize = graphics.MeasureString(buttonSplit.Text, this.Font);
            if (textSize.Height > buttonSplit.Height - 4)
            {
                int newButtonHeight = (int)(textSize.Height + 7 + 0.5);
                Utilities.SetButtonHeight(this, newButtonHeight, 1);
            }
        }

        public void Initialize(Subtitle subtitle, string fileName, SubtitleFormat format, Encoding encoding, double lengthInSeconds)
        {
            ShowBasic = false;
            _subtitle = subtitle;
            if (string.IsNullOrEmpty(fileName))
                textBoxFileName.Text = Configuration.Settings.Language.SplitSubtitle.Untitled;
            else
                textBoxFileName.Text = fileName;
            _fileName = fileName;
            _format = format;
            _encoding = encoding;
            foreach (Paragraph p in _subtitle.Paragraphs)
                _totalNumberOfCharacters += p.Text.Length;
            labelLines.Text = string.Format(Configuration.Settings.Language.Split.NumberOfLinesX, _subtitle.Paragraphs.Count);
            labelCharacters.Text = string.Format(Configuration.Settings.Language.Split.NumberOfCharactersX, _totalNumberOfCharacters);

            try
            {
                numericUpDownParts.Value = Configuration.Settings.Tools.SplitNumberOfParts;
            }
            catch
            {
            }

            if (Configuration.Settings.Tools.SplitVia.Trim().ToLower() == "lines")
                RadioButtonLines.Checked = true;
            else
                radioButtonCharacters.Checked = true;


            foreach (SubtitleFormat f in SubtitleFormat.AllSubtitleFormats)
            {
                if (!f.IsVobSubIndexFile)
                    comboBoxSubtitleFormats.Items.Add(f.FriendlyName);
                if (f.FriendlyName == format.FriendlyName)
                    comboBoxSubtitleFormats.SelectedIndex = comboBoxSubtitleFormats.Items.Count - 1;
            }


            comboBoxEncoding.Items.Clear();
            int encodingSelectedIndex = 0;
            comboBoxEncoding.Items.Add(Encoding.UTF8.EncodingName);
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                if (ei.Name != Encoding.UTF8.BodyName && ei.CodePage >= 949 && !ei.DisplayName.Contains("EBCDIC") && ei.CodePage != 1047)
                {
                    comboBoxEncoding.Items.Add(ei.CodePage + ": " + ei.DisplayName);
                    if (ei.Name == Configuration.Settings.General.DefaultEncoding)
                        encodingSelectedIndex = comboBoxEncoding.Items.Count - 1;
                }
            }
            comboBoxEncoding.SelectedIndex = encodingSelectedIndex;


            if (numericUpDownParts.Maximum > _subtitle.Paragraphs.Count)
                numericUpDownParts.Maximum = _subtitle.Paragraphs.Count / 2;

            if (!string.IsNullOrEmpty(_fileName))
                textBoxOutputFolder.Text = System.IO.Path.GetDirectoryName(_fileName);
            else if (string.IsNullOrEmpty(Configuration.Settings.Tools.SplitOutputFolder) || !System.IO.Directory.Exists(Configuration.Settings.Tools.SplitOutputFolder))
                textBoxOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            else
                textBoxOutputFolder.Text = Configuration.Settings.Tools.SplitOutputFolder;
        }

        private void CalculateParts()
        {
            if (_loading)
                return;

            _loading = true;
            _parts = new List<Subtitle>();
            if (string.IsNullOrEmpty(textBoxOutputFolder.Text) || !System.IO.Directory.Exists(textBoxOutputFolder.Text))
                textBoxOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var format = Utilities.GetSubtitleFormatByFriendlyName(comboBoxSubtitleFormats.SelectedItem.ToString());
            string fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(textBoxFileName.Text);
            if (fileNameNoExt.Trim().Length == 0)
                fileNameNoExt = Configuration.Settings.Language.SplitSubtitle.Untitled;
            listViewParts.Items.Clear();
            int startNumber = 0;
            if (RadioButtonLines.Checked)
            {
                int partSize = (int)(_subtitle.Paragraphs.Count / numericUpDownParts.Value);
                for (int i = 0; i < numericUpDownParts.Value; i++)
                {
                    int noOfLines = (int) partSize;
                    if (i == numericUpDownParts.Value -1)
                        noOfLines = (int) (_subtitle.Paragraphs.Count - ((numericUpDownParts.Value-1) * partSize));

                    Subtitle temp = new Subtitle();
                    temp.Header = _subtitle.Header;
                    int size = 0;
                    for (int number = 0; number < noOfLines; number++)
                    {
                        Paragraph p = _subtitle.Paragraphs[startNumber + number];
                        temp.Paragraphs.Add(new Paragraph(p));
                        size += p.Text.Length;
                    }
                    startNumber += noOfLines;
                    _parts.Add(temp);

                    ListViewItem lvi = new ListViewItem(string.Format("{0:#,###,###}", noOfLines));
                    lvi.SubItems.Add(string.Format("{0:#,###,###}", size));
                    lvi.SubItems.Add(fileNameNoExt + ".Part" + (i + 1) + format.Extension);
                    listViewParts.Items.Add(lvi);
                }
            }
            else if (radioButtonCharacters.Checked)
            {
                int partSize = (int)(_totalNumberOfCharacters / numericUpDownParts.Value);
                int nextLimit = partSize;
                int currentSize = 0;
                Subtitle temp = new Subtitle();
                for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
                {
                    Paragraph p = _subtitle.Paragraphs[i];
                    int size = p.Text.Length;
                    if (currentSize + size > nextLimit + 4 && _parts.Count < numericUpDownParts.Value-1)
                    {
                        _parts.Add(temp);
                        ListViewItem lvi = new ListViewItem(string.Format("{0:#,###,###}", temp.Paragraphs.Count));
                        lvi.SubItems.Add(string.Format("{0:#,###,###}", currentSize));
                        lvi.SubItems.Add(fileNameNoExt + ".Part" + _parts.Count + format.Extension);
                        listViewParts.Items.Add(lvi);
                        currentSize = size;
                        temp = new Subtitle();
                        temp.Paragraphs.Add(new Paragraph(p));
                    }
                    else
                    {
                        currentSize += size;
                        temp.Paragraphs.Add(new Paragraph(p));
                    }
                }
                _parts.Add(temp);
                ListViewItem lvi2 = new ListViewItem(string.Format("{0:#,###,###}", temp.Paragraphs.Count));
                lvi2.SubItems.Add(string.Format("{0:#,###,###}", currentSize));
                lvi2.SubItems.Add(fileNameNoExt + ".Part" + numericUpDownParts.Value + ".srt");
                listViewParts.Items.Add(lvi2);
            }
            _loading = false;
        }

        private void FormSplitSubtitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                DialogResult = DialogResult.Cancel;
        }

        private void buttonBasic_Click(object sender, EventArgs e)
        {
            ShowBasic = true;
            DialogResult = DialogResult.Cancel;
        }

        private void buttonSplit_Click(object sender, EventArgs e)
        {
            bool overwrite = false;
            var format = Utilities.GetSubtitleFormatByFriendlyName(comboBoxSubtitleFormats.SelectedItem.ToString());
            string fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(textBoxFileName.Text);
            if (fileNameNoExt.Trim().Length == 0)
                fileNameNoExt = Configuration.Settings.Language.SplitSubtitle.Untitled;

            int number = 1;
            try
            {
                foreach (Subtitle sub in _parts)
                {
                    string fileName = System.IO.Path.Combine(textBoxOutputFolder.Text, fileNameNoExt + ".Part" + number + format.Extension);
                    string allText = sub.ToText(format);
                    if (System.IO.File.Exists(fileName) && !overwrite)
                    {
                        if (MessageBox.Show(Configuration.Settings.Language.SplitSubtitle.OverwriteExistingFiles, "", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        overwrite = true;
                    }
                    System.IO.File.WriteAllText(fileName, allText, GetCurrentEncoding());
                    number++;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }

            Configuration.Settings.Tools.SplitNumberOfParts = (int)numericUpDownParts.Value;
            Configuration.Settings.Tools.SplitOutputFolder = textBoxOutputFolder.Text;
            if (RadioButtonLines.Checked)
                Configuration.Settings.Tools.SplitVia = "Lines";
            else
                Configuration.Settings.Tools.SplitVia = "Characters";
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void numericUpDownParts_ValueChanged(object sender, EventArgs e)
        {
            CalculateParts();
        }

        private void radioButtonCharacters_CheckedChanged(object sender, EventArgs e)
        {
            CalculateParts();
        }

        private void RadioButtonLines_CheckedChanged(object sender, EventArgs e)
        {
            CalculateParts();
        }

        private void textBoxOutputFolder_TextChanged(object sender, EventArgs e)
        {
            CalculateParts();
        }

        private void Split_ResizeEnd(object sender, EventArgs e)
        {
            columnHeaderFileName.Width = -2;
        }

        private void buttonChooseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxOutputFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private Encoding GetCurrentEncoding()
        {
            if (comboBoxEncoding.Text == Encoding.UTF8.BodyName || comboBoxEncoding.Text == Encoding.UTF8.EncodingName || comboBoxEncoding.Text == "utf-8")
            {
                return Encoding.UTF8;
            }

            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                if (ei.CodePage + ": " + ei.DisplayName == comboBoxEncoding.Text)
                    return ei.GetEncoding();
            }

            return Encoding.UTF8;
        }

        private void Split_Shown(object sender, EventArgs e)
        {
            _loading = false;
            CalculateParts();
        }

        private void Split_Resize(object sender, EventArgs e)
        {
            columnHeaderFileName.Width = -2;
        }

        private void Split_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                DialogResult = DialogResult.Cancel;
        }

        private void textBoxFileName_TextChanged(object sender, EventArgs e)
        {
            CalculateParts();
        }

        private void comboBoxSubtitleFormats_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateParts();
        }

        private void buttonOpenOutputFolder_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(textBoxOutputFolder.Text))
                System.Diagnostics.Process.Start(textBoxOutputFolder.Text);
            else
                MessageBox.Show(string.Format(Configuration.Settings.Language.SplitSubtitle.FolderNotFoundX, textBoxOutputFolder.Text));
        }

    }
}
