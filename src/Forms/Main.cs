﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Nikse.SubtitleEdit.Controls;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.BluRaySup;
using Nikse.SubtitleEdit.Logic.Enums;
using Nikse.SubtitleEdit.Logic.Networking;
using Nikse.SubtitleEdit.Logic.SubtitleFormats;
using Nikse.SubtitleEdit.Logic.VobSub;

namespace Nikse.SubtitleEdit.Forms
{

    public sealed partial class Main : Form
    {

        private class ComboBoxZoomItem
        {
            public string Text { get; set; }
            public double ZoomFactor { get; set; }
            public override string ToString()
            {
                return Text;
            }
        }

        const int TabControlListView = 0;
        const int TabControlSourceView = 1;

        Subtitle _subtitle = new Subtitle();

        int _undoIndex = -1;
        string _listViewTextUndoLast = null;
        int _listViewTextUndoIndex = -1;
        long _listViewTextTicks = -1;
        string _listViewAlternateTextUndoLast = null;
        long _listViewAlternateTextTicks = -1;

        Subtitle _subtitleAlternate = new Subtitle();
        string _subtitleAlternateFileName;
        string _fileName;
        string _videoFileName;
        int _videoAudioTrackNumber = -1;

        public string VideoFileName
        {
            get { return _videoFileName; }
            set { _videoFileName = value; }
        }
        DateTime _fileDateTime;
        string _title;
        FindReplaceDialogHelper _findHelper;
        int _replaceStartLineIndex = 0;
        bool _sourceViewChange;
        private string _changeSubtitleToString = string.Empty;
        private string _changeAlternateSubtitleToString = string.Empty;
        int _subtitleListViewIndex = -1;
        Paragraph _oldSelectedParagraph;
        bool _converted;
        SubtitleFormat _oldSubtitleFormat;
        List<int> _selectedIndexes;
        LanguageStructure.Main _language;
        LanguageStructure.General _languageGeneral;
        SpellCheck _spellCheckForm;
        PositionsAndSizes _formPositionsAndSizes = new PositionsAndSizes();
        bool _loading = true;
        int _repeatCount = -1;
        double _endSeconds = -1;
        const double EndDelay = 0.05;
        int _autoContinueDelayCount = -1;
        long _lastTextKeyDownTicks = 0;
        long _lastHistoryTicks = 0;
        double? _audioWaveFormRightClickSeconds = null;
        private System.Windows.Forms.Timer _timerDoSyntaxColoring = new Timer();
        System.Windows.Forms.Timer _timerAutoSave = new Timer();
        System.Windows.Forms.Timer _timerClearStatus = new Timer();
        string _textAutoSave;
        StringBuilder _statusLog = new StringBuilder();
        bool _makeHistoryPaused = false;

        NikseWebServiceSession _networkSession;
        NetworkChat _networkChat = null;

        ShowEarlierLater _showEarlierOrLater = null;

        bool _isVideoControlsUnDocked = false;
        VideoPlayerUnDocked _videoPlayerUnDocked = null;
        WaveFormUnDocked _waveFormUnDocked = null;
        VideoControlsUndocked _videoControlsUnDocked = null;

        GoogleOrMicrosoftTranslate _googleOrMicrosoftTranslate = null;

        bool _cancelWordSpellCheck = true;

        Keys _toggleVideoDockUndock = Keys.None;
        Keys _videoPause = Keys.None;
        Keys _videoPlayPauseToggle = Keys.None;
        Keys _video100MsLeft = Keys.None;
        Keys _video100MsRight = Keys.None;
        Keys _video500MsLeft = Keys.None;
        Keys _video500MsRight = Keys.None;
        Keys _mainVideoFullscreen = Keys.None;
        Keys _mainTextBoxSplitAtCursor = Keys.None;
        Keys _mainCreateInsertSubAtVideoPos = Keys.None;
        Keys _mainCreatePlayFromJustBefore = Keys.None;
        Keys _mainCreateSetStart = Keys.None;
        Keys _mainCreateSetEnd = Keys.None;
        Keys _mainCreateStartDownEndUp = Keys.None;
        Keys _mainAdjustSetStartAndOffsetTheRest = Keys.None;
        Keys _mainAdjustSetEndAndGotoNext = Keys.None;
        Keys _mainAdjustInsertViaEndAutoStartAndGoToNext = Keys.None;
        Keys _mainAdjustSetStartAutoDurationAndGoToNext = Keys.None;
        Keys _mainAdjustSetEndNextStartAndGoToNext = Keys.None;
        Keys _mainAdjustStartDownEndUpAndGoToNext = Keys.None;
        Keys _mainAdjustSetStart = Keys.None;
        Keys _mainAdjustSetStartOnly = Keys.None;
        Keys _mainAdjustSetEnd = Keys.None;
        Keys _mainAdjustSelected100MsForward = Keys.None;
        Keys _mainAdjustSelected100MsBack = Keys.None;
        Keys _mainInsertAfter = Keys.None;
        Keys _mainInsertBefore = Keys.None;
        Keys _mainMergeDialogue = Keys.None;
        Keys _mainGoToNext = Keys.None;
        Keys _mainGoToPrevious = Keys.None;
        Keys _mainToggleFocus = Keys.None;
        Keys _mainListViewToggleDashes = Keys.None;
        Keys _mainListViewCopyText = Keys.None;
        Keys _mainEditReverseStartAndEndingForRTL = Keys.None;
        Keys _waveformVerticalZoom = Keys.None;
        Keys _waveformZoomIn = Keys.None;
        Keys _waveformZoomOut = Keys.None;
        Keys _waveformPlaySelection = Keys.None;
        Keys _waveformSearchSilenceForward = Keys.None;
        Keys _waveformSearchSilenceBack = Keys.None;
        Keys _waveformAddTextAtHere = Keys.None;
        Keys _mainTranslateCustomSearch1 = Keys.None;
        Keys _mainTranslateCustomSearch2 = Keys.None;
        Keys _mainTranslateCustomSearch3 = Keys.None;
        Keys _mainTranslateCustomSearch4 = Keys.None;
        Keys _mainTranslateCustomSearch5 = Keys.None;
        Keys _mainTranslateCustomSearch6 = Keys.None;
        bool _videoLoadedGoToSubPosAndPause = false;
        string _cutText = string.Empty;
        private Paragraph _mainCreateStartDownEndUpParagraph;
        private Paragraph _mainAdjustStartDownEndUpAndGoToNextParagraph;
        private string _lastDoNotPrompt = string.Empty;
        private VideoInfo _videoInfo = null;

        private bool AutoRepeatContinueOn
        {
            get
            {
                return tabControlButtons.SelectedIndex == 0;
            }
        }

        public string Title
        {
            get
            {
                if (_title == null)
                {
                    string[] versionInfo = Utilities.AssemblyVersion.Split('.');
                    _title = String.Format("{0} {1}.{2}", _languageGeneral.Title, versionInfo[0], versionInfo[1]);
                    if (versionInfo.Length >= 3 && versionInfo[2] != "0")
                        _title += "." + versionInfo[2];
                }
                return _title + " preview";
            }
        }

        public void SetCurrentFormat(SubtitleFormat format)
        {
            if (format.IsVobSubIndexFile)
            {
                comboBoxSubtitleFormats.Items.Clear();
                comboBoxSubtitleFormats.Items.Add(format.FriendlyName);

                SubtitleListview1.HideNonVobSubColumns();
            }
            else if (comboBoxSubtitleFormats.Items.Count == 1)
            {
                SetFormatToSubRip();
                SubtitleListview1.ShowAllColumns();
            }

            int i = 0;
            foreach (object obj in comboBoxSubtitleFormats.Items)
            {
                if (obj.ToString() == format.FriendlyName)
                    comboBoxSubtitleFormats.SelectedIndex = i;
                i++;
            }
        }

        public void SetCurrentFormat(string subtitleFormatFriendlyName)
        {

            foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
            {
                if (format.FriendlyName == subtitleFormatFriendlyName)
                {
                    SetCurrentFormat(format);
                    break;
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();


        public Main()
        {
            try
            {
                InitializeComponent();

                textBoxListViewTextAlternate.Visible = false;
                labelAlternateText.Visible = false;
                labelAlternateCharactersPerSecond.Visible = false;
                labelTextAlternateLineLengths.Visible = false;
                labelAlternateSingleLine.Visible = false;
                labelTextAlternateLineTotal.Visible = false;

                SetLanguage(Configuration.Settings.General.Language);
                toolStripStatusNetworking.Visible = false;
                labelTextLineLengths.Text = string.Empty;
                labelCharactersPerSecond.Text = string.Empty;
                labelTextLineTotal.Text = string.Empty;
                labelStartTimeWarning.Text = string.Empty;
                labelDurationWarning.Text = string.Empty;
                labelVideoInfo.Text = string.Empty;
                labelSingleLine.Text = string.Empty;
                Text = Title;
                timeUpDownStartTime.TimeCode = new TimeCode(0, 0, 0, 0);
                checkBoxAutoRepeatOn.Checked = Configuration.Settings.General.AutoRepeatOn;
                checkBoxAutoContinue.Checked = Configuration.Settings.General.AutoContinueOn;
                checkBoxSyncListViewWithVideoWhilePlaying.Checked = Configuration.Settings.General.SyncListViewWithVideoWhilePlaying;

                SetFormatToSubRip();

                comboBoxEncoding.Items.Clear();
                comboBoxEncoding.Items.Add(Encoding.UTF8.EncodingName);
                foreach (EncodingInfo ei in Encoding.GetEncodings())
                {
                    if (ei.Name != Encoding.UTF8.BodyName && ei.CodePage >= 949 && !ei.DisplayName.Contains("EBCDIC") && ei.CodePage != 1047) //Configuration.Settings.General.EncodingMininumCodePage)
                        comboBoxEncoding.Items.Add(ei.CodePage + ": " + ei.DisplayName);
                }
                SetEncoding(Configuration.Settings.General.DefaultEncoding);

                toolStripComboBoxFrameRate.Items.Add((23.976).ToString());
                toolStripComboBoxFrameRate.Items.Add((24.0).ToString());
                toolStripComboBoxFrameRate.Items.Add((25.0).ToString());
                toolStripComboBoxFrameRate.Items.Add((29.97).ToString());
                toolStripComboBoxFrameRate.Items.Add((30).ToString());
                toolStripComboBoxFrameRate.Text = Configuration.Settings.General.DefaultFrameRate.ToString();

                UpdateRecentFilesUI();
                InitializeToolbar();
                Utilities.InitializeSubtitleFont(textBoxSource);
                Utilities.InitializeSubtitleFont(textBoxListViewText);
                Utilities.InitializeSubtitleFont(textBoxListViewTextAlternate);
                Utilities.InitializeSubtitleFont(SubtitleListview1);

                if (Configuration.Settings.General.CenterSubtitleInTextBox)
                {
                    textBoxListViewText.TextAlign = HorizontalAlignment.Center;
                    textBoxListViewTextAlternate.TextAlign = HorizontalAlignment.Center;
                }

                //SubtitleListview1.AutoSizeAllColumns(this);

                tabControlSubtitle.SelectTab(TabControlSourceView); // AC
                ShowSourceLineNumber();                             // AC
                tabControlSubtitle.SelectTab(TabControlListView);   // AC
                if (Configuration.Settings.General.StartInSourceView)
                    tabControlSubtitle.SelectTab(TabControlSourceView);


                audioVisualizer.Visible = Configuration.Settings.General.ShowAudioVisualizer;
                audioVisualizer.ShowWaveform = Configuration.Settings.General.ShowWaveform;
                audioVisualizer.ShowSpectrogram = Configuration.Settings.General.ShowSpectrogram;
                panelWaveFormControls.Visible = Configuration.Settings.General.ShowAudioVisualizer;
                trackBarWaveFormPosition.Visible = Configuration.Settings.General.ShowAudioVisualizer;
                toolStripButtonToggleWaveForm.Checked = Configuration.Settings.General.ShowAudioVisualizer;
                toolStripButtonToggleVideo.Checked = Configuration.Settings.General.ShowVideoPlayer;

                if (Configuration.Settings.General.UseTimeFormatHHMMSSFF)
                {
                    numericUpDownDuration.DecimalPlaces = 2;
                    numericUpDownDuration.Increment = (decimal)(0.01);
                    toolStripSeparatorFrameRate.Visible = true;
                    toolStripLabelFrameRate.Visible = true;
                    toolStripComboBoxFrameRate.Visible = true;
                    toolStripButtonGetFrameRate.Visible = true;
                }

                _timerClearStatus.Interval = Configuration.Settings.General.ClearStatusBarAfterSeconds * 1000;
                _timerClearStatus.Tick += TimerClearStatus_Tick;

                string fileName = string.Empty;
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length >= 2 && args[1].ToLower() == "/convert")
                {
                    BatchConvert(args);
                    return;
                }
                else if (args.Length >= 2)
                    fileName = args[1];

                if (fileName.Length > 0 && File.Exists(fileName))
                {
                    OpenSubtitle(fileName, null);
                }
                else if (Configuration.Settings.General.StartLoadLastFile)
                {
                    if (Configuration.Settings.RecentFiles.Files.Count > 0)
                    {
                        fileName = Configuration.Settings.RecentFiles.Files[0].FileName;
                        if (File.Exists(fileName))
                        {
                            OpenSubtitle(fileName, null, Configuration.Settings.RecentFiles.Files[0].VideoFileName, Configuration.Settings.RecentFiles.Files[0].OriginalFileName);
                            SetRecentIndecies(fileName);
                            GotoSubPosAndPause();
                        }
                    }
                }

                labelAutoDuration.Visible = false;
                mediaPlayer.SubtitleText = string.Empty;
                comboBoxAutoRepeat.SelectedIndex = 2;
                comboBoxAutoContinue.SelectedIndex = 2;
                timeUpDownVideoPosition.TimeCode = new TimeCode(0, 0, 0, 0);
                timeUpDownVideoPositionAdjust.TimeCode = new TimeCode(0, 0, 0, 0);
                timeUpDownVideoPosition.TimeCodeChanged += VideoPositionChanged;
                timeUpDownVideoPositionAdjust.TimeCodeChanged += VideoPositionChanged;
                timeUpDownVideoPosition.Enabled = false;
                timeUpDownVideoPositionAdjust.Enabled = false;

                switch (Configuration.Settings.VideoControls.LastActiveTab)
                {
                    case "Translate":
                        tabControlButtons.SelectedIndex = 0;
                        break;
                    case "Create":
                        tabControlButtons.SelectedIndex = 1;
                        break;
                    case "Adjust":
                        tabControlButtons.SelectedIndex = 2;
                        break;
                }
                tabControl1_SelectedIndexChanged(null, null);

                buttonCustomUrl1.Text = Configuration.Settings.VideoControls.CustomSearchText1;
                buttonCustomUrl1.Visible = Configuration.Settings.VideoControls.CustomSearchUrl1.Length > 1;

                buttonCustomUrl2.Text = Configuration.Settings.VideoControls.CustomSearchText2;
                buttonCustomUrl2.Visible = Configuration.Settings.VideoControls.CustomSearchUrl2.Length > 1;

                // Initialize events etc. for audio wave form
                audioVisualizer.OnDoubleClickNonParagraph += AudioWaveForm_OnDoubleClickNonParagraph;
                audioVisualizer.OnPositionSelected += AudioWaveForm_OnPositionSelected;
                audioVisualizer.OnTimeChanged += AudioWaveForm_OnTimeChanged; // start and/or end position of paragraph changed
                audioVisualizer.OnNewSelectionRightClicked += AudioWaveForm_OnNewSelectionRightClicked;
                audioVisualizer.OnParagraphRightClicked += AudioWaveForm_OnParagraphRightClicked;
                audioVisualizer.OnNonParagraphRightClicked += new AudioVisualizer.PositionChangedEventHandler(AudioWaveForm_OnNonParagraphRightClicked);
                audioVisualizer.OnSingleClick += AudioWaveForm_OnSingleClick;
                audioVisualizer.OnPause += AudioWaveForm_OnPause;
                audioVisualizer.OnTimeChangedAndOffsetRest += AudioWaveForm_OnTimeChangedAndOffsetRest;
                audioVisualizer.OnZoomedChanged += AudioWaveForm_OnZoomedChanged;
                audioVisualizer.DrawGridLines = Configuration.Settings.VideoControls.WaveFormDrawGrid;
                audioVisualizer.GridColor = Configuration.Settings.VideoControls.WaveFormGridColor;
                audioVisualizer.SelectedColor = Configuration.Settings.VideoControls.WaveFormSelectedColor;
                audioVisualizer.Color = Configuration.Settings.VideoControls.WaveFormColor;
                audioVisualizer.BackgroundColor = Configuration.Settings.VideoControls.WaveFormBackgroundColor;
                audioVisualizer.TextColor = Configuration.Settings.VideoControls.WaveFormTextColor;
                audioVisualizer.MouseWheelScrollUpIsForward = Configuration.Settings.VideoControls.WaveFormMouseWheelScrollUpIsForward;

                for (double zoomCounter = AudioVisualizer.ZoomMininum; zoomCounter <= AudioVisualizer.ZoomMaxinum + (0.001); zoomCounter += 0.1)
                {
                    int percent = (int)Math.Round((zoomCounter * 100));
                    ComboBoxZoomItem item = new ComboBoxZoomItem() { Text = percent.ToString() + "%", ZoomFactor = zoomCounter };
                    toolStripComboBoxWaveForm.Items.Add(item);
                    if (percent == 100)
                        toolStripComboBoxWaveForm.SelectedIndex = toolStripComboBoxWaveForm.Items.Count - 1;
                }
                toolStripComboBoxWaveForm.SelectedIndexChanged += toolStripComboBoxWaveForm_SelectedIndexChanged;

                FixLargeFonts();
            }
            catch (Exception exception)
            {
                Cursor = Cursors.Default;
                MessageBox.Show(exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }

        void TimerClearStatus_Tick(object sender, EventArgs e)
        {
            ShowStatus(string.Empty);
        }

        private void SetEncoding(Encoding encoding)
        {
            if (encoding.BodyName == Encoding.UTF8.BodyName)
            {
                comboBoxEncoding.SelectedIndex= 0;
                return;
            }

            int i = 0;
            foreach (string s in comboBoxEncoding.Items)
            {
                if (s == encoding.CodePage + ": " + encoding.EncodingName)
                {
                    comboBoxEncoding.SelectedIndex = i;
                    return;
                }
                i++;
            }
            comboBoxEncoding.SelectedIndex = 0;
        }

        private void SetEncoding(string encodingName)
        {
            if (encodingName == Encoding.UTF8.BodyName || encodingName == Encoding.UTF8.EncodingName || encodingName == "utf-8")
            {
                comboBoxEncoding.SelectedIndex = 0;
                return;
            }

            int i = 0;
            foreach (string s in comboBoxEncoding.Items)
            {
                if (s == encodingName)
                {
                    comboBoxEncoding.SelectedIndex = i;
                    return;
                }
                i++;
            }
            comboBoxEncoding.SelectedIndex = 0;
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

        private void BatchConvert(string[] args) // E.g.: /convert *.txt SubRip
        {
            const int ATTACH_PARENT_PROCESS = -1;
            if (!Utilities.IsRunningOnMac() && !Utilities.IsRunningOnLinux())
                AttachConsole(ATTACH_PARENT_PROCESS);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(Title + " - Batch converter");
            Console.WriteLine();
            Console.WriteLine("- Syntax: SubtitleEdit /convert <pattern> <name-of-format-without-spaces> [/offset:hh:mm:ss:msec] [/encoding:<encoding name>] [/fps:<frame rate>] [/inputfolder:<input folder>] [/outputfolder:<output folder>]");
            Console.WriteLine();
            Console.WriteLine("    example: SubtitleEdit /convert *.srt sami");
            Console.WriteLine("    list available formats: SubtitleEdit /convert /list");
            Console.WriteLine();

            string currentDir = Directory.GetCurrentDirectory();

            if (args.Length < 4)
            {
                if (args.Length == 3 && (args[2].ToLower() == "/list" || args[2].ToLower() == "-list"))
                {
                    Console.WriteLine("- Supported formats (input/output):");
                    foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
                    {
                        Console.WriteLine("    " + format.Name.Replace(" ", string.Empty));
                    }
                    Console.WriteLine();
                    Console.WriteLine("- Supported formats (input only):");
                    Console.WriteLine("    " + new CapMakerPlus().FriendlyName);
                    Console.WriteLine("    " + new Captionate().FriendlyName);
                    Console.WriteLine("    " + new Cavena890().FriendlyName);
                    Console.WriteLine("    " + new CheetahCaption().FriendlyName);
                    Console.WriteLine("    " + new Ebu().FriendlyName);
                    Console.WriteLine("    Matroska (.mkv)");
                    Console.WriteLine("    Matroska subtitle (.mks)");
                    Console.WriteLine("    " + new NciCaption().FriendlyName);
                    Console.WriteLine("    " + new Pac().FriendlyName);
                    Console.WriteLine("    " + new Spt().FriendlyName);
                    Console.WriteLine("    " + new Ultech130().FriendlyName);
                }

                Console.WriteLine();
                Console.Write(currentDir + ">");
                if (!Utilities.IsRunningOnMac() && !Utilities.IsRunningOnLinux())
                    FreeConsole();
                Environment.Exit(1);
            }

            int count = 0;
            int converted = 0;
            int errors = 0;
            string inputDirectory = string.Empty;
            try
            {
                int max = args.Length;

                string pattern = args[2];
                string toFormat = args[3];
                string offset = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].ToLower().StartsWith("/offset:"))
                        offset = args[idx].ToLower();

                string fps = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].ToLower().StartsWith("/fps:"))
                        fps = args[idx].ToLower();
                if (fps.Length > 6)
                {
                    fps = fps.Replace(",", ".").Trim();
                    double d;
                    if (double.TryParse(fps, System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.InvariantCulture, out d))
                    {
                        toolStripComboBoxFrameRate.Text = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        Configuration.Settings.General.CurrentFrameRate = d;
                    }
                }

                string targetEncodingName = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].ToLower().StartsWith("/encoding:"))
                        targetEncodingName = args[idx].ToLower();
                Encoding targetEncoding = Encoding.UTF8;
                try
                {
                    if (!string.IsNullOrEmpty(targetEncodingName))
                    {
                        targetEncodingName = targetEncodingName.Substring(10);
                        if (!string.IsNullOrEmpty(targetEncodingName))
                            targetEncoding = Encoding.GetEncoding(targetEncodingName);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Unable to set encoding (" + exception.Message + ") - using UTF-8");
                    targetEncoding = Encoding.UTF8;
                }

                string outputFolder = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].ToLower().StartsWith("/outputfolder:"))
                        outputFolder = args[idx].ToLower();
                if (outputFolder.Length > "/outputFolder:".Length)
                {
                    outputFolder = outputFolder.Remove(0, "/outputFolder:".Length);
                    if (!Directory.Exists(outputFolder))
                        outputFolder = string.Empty;
                }

                string inputFolder = Directory.GetCurrentDirectory();
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].ToLower().StartsWith("/inputFolder:"))
                        inputFolder = args[idx].ToLower();
                if (inputFolder.Length > "/inputFolder:".Length)
                {
                    inputFolder = inputFolder.Remove(0, "/inputFolder:".Length);
                    if (!Directory.Exists(inputFolder))
                        inputFolder = Directory.GetCurrentDirectory();
                }

                bool overwrite = false;
                for (int idx=4; idx < max; idx++)
                    if (args.Length > idx && args[idx].ToLower() == ("/overwrite"))
                        overwrite = true;

                string[] files;
                inputDirectory = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(inputFolder))
                    inputDirectory = inputFolder;

                if (pattern.Contains(","))
                {
                    files = pattern.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < files.Length; k++)
                        files[k] = files[k].Trim();
                }
                else
                {
                    int indexOfDirectorySeparatorChar = pattern.LastIndexOf(Path.DirectorySeparatorChar.ToString());
                    if (indexOfDirectorySeparatorChar > 0 && indexOfDirectorySeparatorChar < pattern.Length)
                    {
                        pattern = pattern.Substring(indexOfDirectorySeparatorChar + 1);
                        inputDirectory = args[2].Substring(0, indexOfDirectorySeparatorChar);
                    }
                    files = Directory.GetFiles(inputDirectory, pattern);
                }

                var formats = SubtitleFormat.AllSubtitleFormats;
                foreach (string fName in files)
                {
                    string fileName = fName;
                    count++;

                    if (!string.IsNullOrEmpty(inputFolder) && File.Exists(Path.Combine(inputFolder, fileName)))
                        fileName = Path.Combine(inputFolder, fileName);

                    if (File.Exists(fileName))
                    {
                        Encoding encoding;
                        var sub = new Subtitle();
                        SubtitleFormat format = null;
                        bool done = false;

                        if (Path.GetExtension(fileName).ToLower() == ".mkv" || Path.GetExtension(fileName).ToLower() == ".mks")
                        {
                            Matroska mkv = new Matroska();
                            bool isValid = false;
                            bool hasConstantFrameRate = false;
                            double frameRate = 0;
                            int width = 0;
                            int height = 0;
                            double milliseconds = 0;
                            string videoCodec = string.Empty;
                            mkv.GetMatroskaInfo(fileName, ref isValid, ref hasConstantFrameRate, ref frameRate, ref width, ref height, ref milliseconds, ref videoCodec);
                            if (isValid)
                            {
                                var subtitleList = mkv.GetMatroskaSubtitleTracks(fileName, out isValid);
                                if (subtitleList.Count > 0)
                                {
                                    foreach (MatroskaSubtitleInfo x in subtitleList)
                                    {
                                        if (x.CodecId.ToUpper() == "S_VOBSUB")
                                        {
                                            Console.WriteLine(string.Format("{0}: {1} - Cannot convert from VobSub image based format!", count, fileName, toFormat));
                                        }
                                        else if (x.CodecId.ToUpper() == "S_HDMV/PGS")
                                        {
                                            Console.WriteLine(string.Format("{0}: {1} - Cannot convert from Blu-ray image based format!", count, fileName, toFormat));
                                        }
                                        else
                                        {
                                            LoadMatroskaSubtitle(x, fileName, true);
                                            sub = _subtitle;
                                            format = GetCurrentSubtitleFormat();
                                            string newFileName = fileName;
                                            if (subtitleList.Count > 1)
                                                newFileName = fileName.Insert(fileName.Length - 4, "_" + x.TrackNumber +  "_" + x.Language.Replace("?", string.Empty).Replace("!", string.Empty).Replace("*", string.Empty).Replace(",", string.Empty).Replace("/", string.Empty).Trim());

                                            if (format.GetType() == typeof(AdvancedSubStationAlpha) || format.GetType() == typeof(SubStationAlpha))
                                            {
                                                if (toFormat.ToLower() != new AdvancedSubStationAlpha().Name.ToLower().Replace(" ", string.Empty) &&
                                                    toFormat.ToLower() != new SubStationAlpha().Name.ToLower().Replace(" ", string.Empty))
                                                {
                                                    format.RemoveNativeFormatting(sub);
                                                }
                                            }

                                            BatchConvertSave(toFormat, offset, targetEncoding, outputFolder, count, ref converted, ref errors, formats, newFileName, sub, format, overwrite);
                                            done = true;
                                        }
                                    }
                                }
                            }
                        }

                        var fi = new FileInfo(fileName);
                        if (fi.Length < 1024 * 1024 && !done) // max 1 mb
                        {
                            format = sub.LoadSubtitle(fileName, out encoding, null);

                            if (format == null)
                            {
                                var ebu = new Ebu();
                                if (ebu.IsMine(null, fileName))
                                {
                                    ebu.LoadSubtitle(sub, null, fileName);
                                    format = ebu;
                                }
                            }
                            if (format == null)
                            {
                                var pac = new Pac();
                                if (pac.IsMine(null, fileName))
                                {
                                    pac.LoadSubtitle(sub, null, fileName);
                                    format = pac;
                                }
                            }
                            if (format == null)
                            {
                                var cavena890 = new Cavena890();
                                if (cavena890.IsMine(null, fileName))
                                {
                                    cavena890.LoadSubtitle(sub, null, fileName);
                                    format = cavena890;
                                }
                            }
                            if (format == null)
                            {
                                var spt = new Spt();
                                if (spt.IsMine(null, fileName))
                                {
                                    spt.LoadSubtitle(sub, null, fileName);
                                    format = spt;
                                }
                            }
                            if (format == null)
                            {
                                var cheetahCaption = new CheetahCaption();
                                if (cheetahCaption.IsMine(null, fileName))
                                {
                                    cheetahCaption.LoadSubtitle(_subtitle, null, fileName);
                                    format = cheetahCaption;
                                }
                            }
                            if (format == null)
                            {
                                var capMakerPlus = new CapMakerPlus();
                                if (capMakerPlus.IsMine(null, fileName))
                                {
                                    capMakerPlus.LoadSubtitle(_subtitle, null, fileName);
                                    format = capMakerPlus;
                                }
                            }
                            if (format == null)
                            {
                                var captionate = new Captionate();
                                if (captionate.IsMine(null, fileName))
                                {
                                    captionate.LoadSubtitle(_subtitle, null, fileName);
                                    format = captionate;
                                }
                            }
                            if (format == null)
                            {
                                var ultech130 = new Ultech130();
                                if (ultech130.IsMine(null, fileName))
                                {
                                    ultech130.LoadSubtitle(_subtitle, null, fileName);
                                    format = ultech130;
                                }
                            }
                            if (format == null)
                            {
                                var nciCaption = new NciCaption();
                                if (nciCaption.IsMine(null, fileName))
                                {
                                    nciCaption.LoadSubtitle(_subtitle, null, fileName);
                                    format = nciCaption;
                                }
                            }
                        }

                        if (format == null)
                        {
                            if (fi.Length < 1024 * 1024) // max 10 mb
                                Console.WriteLine(string.Format("{0}: {1} - input file format unknown!", count, fileName, toFormat));
                            else
                                Console.WriteLine(string.Format("{0}: {1} - input file too large!", count, fileName, toFormat));
                        }
                        else if (!done)
                        {
                            BatchConvertSave(toFormat, offset, targetEncoding, outputFolder, count, ref converted, ref errors, formats, fileName, sub, format, overwrite);
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0}: {1} - file not found!", count, fileName));
                        errors++;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine();
                Console.WriteLine("Ups - an error occured: " + exception.Message);
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine(string.Format("{0} file(s) converted", converted));
            Console.WriteLine();
            Console.Write(currentDir + ">");

            if (!Utilities.IsRunningOnMac() && !Utilities.IsRunningOnLinux())
                FreeConsole();

            if (count == converted && errors == 0)
                Environment.Exit(0);
            else
                Environment.Exit(1);
        }

        private void BatchConvertSave(string toFormat, string offset, Encoding targetEncoding, string outputFolder, int count, ref int converted, ref int errors, IList<SubtitleFormat> formats, string fileName, Subtitle sub, SubtitleFormat format, bool overwrite)
        {
            // adjust offset
            if (!string.IsNullOrEmpty(offset) && (offset.StartsWith("/offset:") || offset.StartsWith("offset:")))
            {
                string[] parts = offset.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 5)
                {
                    try
                    {
                        TimeSpan ts = new TimeSpan(0, int.Parse(parts[1].TrimStart('-')), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]));
                        if (parts[1].StartsWith("-"))
                            sub.AddTimeToAllParagraphs(ts.Negate());
                        else
                            sub.AddTimeToAllParagraphs(ts);
                    }
                    catch
                    {
                        Console.Write(" (unable to read offset " + offset + ")");
                    }
                }
            }

            bool targetFormatFound = false;
            string outputFileName;
            foreach (SubtitleFormat sf in formats)
            {
                if (sf.Name.ToLower().Replace(" ", string.Empty) == toFormat.ToLower())
                {
                    targetFormatFound = true;
                    outputFileName = FormatOutputFileNameForBatchConvert(fileName, sf.Extension, outputFolder, overwrite);
                    Console.Write(string.Format("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName));
                    if (sf.IsFrameBased && !sub.WasLoadedWithFrameNumbers)
                        sub.CalculateFrameNumbersFromTimeCodesNoCheck(Configuration.Settings.General.DefaultFrameRate);
                    else if (sf.IsTimeBased && sub.WasLoadedWithFrameNumbers)
                        sub.CalculateTimeCodesFromFrameNumbers(Configuration.Settings.General.DefaultFrameRate);
                    File.WriteAllText(outputFileName, sub.ToText(sf), targetEncoding);
                    if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                    {
                        var sami = (Sami)format;
                        foreach (string className in Sami.GetStylesFromHeader(sub.Header))
                        {
                            var newSub = new Subtitle();
                            foreach (Paragraph p in sub.Paragraphs)
                            {
                                if (p.Extra != null && p.Extra.ToLower().Trim() == className.ToLower().Trim())
                                    newSub.Paragraphs.Add(p);
                            }
                            if (newSub.Paragraphs.Count > 0 && newSub.Paragraphs.Count < sub.Paragraphs.Count)
                            {
                                string s = fileName;
                                if (s.LastIndexOf('.') > 0)
                                    s = s.Insert(s.LastIndexOf('.'), "_" + className);
                                else
                                    s += "_" + className + format.Extension;
                                outputFileName = FormatOutputFileNameForBatchConvert(s, sf.Extension, outputFolder, overwrite);
                                File.WriteAllText(outputFileName, newSub.ToText(sf), targetEncoding);
                            }
                        }
                    }
                    Console.WriteLine(" done.");
                }
            }
            if (!targetFormatFound)
            {
                var ebu = new Ebu();
                if (ebu.Name.ToLower().Replace(" ", string.Empty) == toFormat.ToLower())
                {
                    targetFormatFound = true;
                    outputFileName = FormatOutputFileNameForBatchConvert(fileName, ebu.Extension, outputFolder, overwrite);
                    Console.Write(string.Format("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName));
                    ebu.Save(outputFileName, sub);
                    Console.WriteLine(" done.");
                }
            }
            if (!targetFormatFound)
            {
                var pac = new Pac();
                if (pac.Name.ToLower().Replace(" ", string.Empty) == toFormat.ToLower())
                {
                    targetFormatFound = true;
                    outputFileName = FormatOutputFileNameForBatchConvert(fileName, pac.Extension, outputFolder, overwrite);
                    Console.Write(string.Format("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName));
                    pac.Save(outputFileName, sub);
                    Console.WriteLine(" done.");
                }
            }
            if (!targetFormatFound)
            {
                var cavena890 = new Cavena890();
                if (cavena890.Name.ToLower().Replace(" ", string.Empty) == toFormat.ToLower())
                {
                    targetFormatFound = true;
                    outputFileName = FormatOutputFileNameForBatchConvert(fileName, cavena890.Extension, outputFolder, overwrite);
                    Console.Write(string.Format("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName));
                    cavena890.Save(outputFileName, sub);
                    Console.WriteLine(" done.");
                }
            }
            if (!targetFormatFound)
            {
                var cheetahCaption = new CheetahCaption();
                if (cheetahCaption.Name.ToLower().Replace(" ", string.Empty) == toFormat.ToLower())
                {
                    targetFormatFound = true;
                    outputFileName = FormatOutputFileNameForBatchConvert(fileName, cheetahCaption.Extension, outputFolder, overwrite);
                    Console.Write(string.Format("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName));
                    cheetahCaption.Save(outputFileName, sub);
                    Console.WriteLine(" done.");
                }
            }
            if (!targetFormatFound)
            {
                var capMakerPlus = new CapMakerPlus();
                if (capMakerPlus.Name.ToLower().Replace(" ", string.Empty) == toFormat.ToLower())
                {
                    targetFormatFound = true;
                    outputFileName = FormatOutputFileNameForBatchConvert(fileName, capMakerPlus.Extension, outputFolder, overwrite);
                    Console.Write(string.Format("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName));
                    capMakerPlus.Save(outputFileName, sub);
                    Console.WriteLine(" done.");
                }
            }
            if (!targetFormatFound)
            {
                Console.WriteLine(string.Format("{0}: {1} - target format '{2}' not found!", count, fileName, toFormat));
                errors++;
            }
            else
            {
                converted++;
            }
        }

        string FormatOutputFileNameForBatchConvert(string fileName, string extension, string outputFolder, bool overwrite)
        {
            string outputFileName = Path.ChangeExtension(fileName,extension);
            if (!string.IsNullOrEmpty(outputFolder))
                outputFileName = Path.Combine(outputFolder, Path.GetFileName(outputFileName));
            if (File.Exists(outputFileName) && !overwrite)
                outputFileName = Path.ChangeExtension(outputFileName, Guid.NewGuid().ToString() + extension);
            return outputFileName;
        }

        void AudioWaveForm_OnNonParagraphRightClicked(double seconds, Paragraph paragraph)
        {
            addParagraphHereToolStripMenuItem.Visible = false;
            deleteParagraphToolStripMenuItem.Visible = false;
            splitToolStripMenuItem1.Visible = false;
            mergeWithPreviousToolStripMenuItem.Visible = false;
            mergeWithNextToolStripMenuItem.Visible = false;
            toolStripSeparator11.Visible = false;
            toolStripMenuItemWaveFormPlaySelection.Visible = false;
            toolStripSeparator24.Visible = false;
            contextMenuStripWaveForm.Show(MousePosition.X, MousePosition.Y);
        }

        void AudioWaveForm_OnDoubleClickNonParagraph(double seconds, Paragraph paragraph)
        {
            if (mediaPlayer.VideoPlayer != null)
            {
                _endSeconds = -1;
                if (paragraph == null)
                {
                    if (Configuration.Settings.VideoControls.WaveFormDoubleClickOnNonParagraphAction == "PlayPause")
                        mediaPlayer.TogglePlayPause();
                }
                else
                {
                    SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.GetIndex(paragraph));
                }
            }
        }

        void AudioWaveForm_OnZoomedChanged(object sender, EventArgs e)
        {
            SelectZoomTextInComboBox();
        }

        void AudioWaveForm_OnTimeChangedAndOffsetRest(double seconds, Paragraph paragraph)
        {
            if (mediaPlayer.VideoPlayer == null)
                return;

            int index = _subtitle.Paragraphs.IndexOf(paragraph);
            if (index < 0)
            {
                if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible && Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable)
                {
                    index = _subtitleAlternate.GetIndex(paragraph);
                    if (index >= 0)
                    {
                        var current = Utilities.GetOriginalParagraph(index, paragraph, _subtitle.Paragraphs);
                        if (current != null)
                        {
                            index = _subtitle.Paragraphs.IndexOf(current);
                        }
                    }
                }
                else if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible)
                {
                    index = _subtitle.GetIndex(paragraph);
                }
            }
            if (index >= 0)
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(index);
                mediaPlayer.CurrentPosition = seconds;
                ButtonSetStartAndOffsetRestClick(null, null);
            }
            audioVisualizer.Invalidate();
        }

        void AudioWaveForm_OnPause(object sender, EventArgs e)
        {
            _endSeconds = -1;
            if (mediaPlayer.VideoPlayer != null)
                mediaPlayer.Pause();
        }

        void AudioWaveForm_OnSingleClick(double seconds, Paragraph paragraph)
        {
            timerWaveForm.Stop();
            _endSeconds = -1;
            if (mediaPlayer.VideoPlayer != null)
                mediaPlayer.Pause();

            mediaPlayer.CurrentPosition = seconds;

            int index = -1;
            if (SubtitleListview1.SelectedItems.Count > 0)
                index = SubtitleListview1.SelectedItems[0].Index;
            SetWaveFormPosition(audioVisualizer.StartPositionSeconds, seconds, index);
            timerWaveForm.Start();
        }

        void AudioWaveForm_OnParagraphRightClicked(double seconds, Paragraph paragraph)
        {
            SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.GetIndex(paragraph));

            addParagraphHereToolStripMenuItem.Visible = false;
            deleteParagraphToolStripMenuItem.Visible = true;
            splitToolStripMenuItem1.Visible = true;
            mergeWithPreviousToolStripMenuItem.Visible = true;
            mergeWithNextToolStripMenuItem.Visible = true;
            toolStripSeparator11.Visible = true;
            toolStripMenuItemWaveFormPlaySelection.Visible = true;
            toolStripSeparator24.Visible = true;

            _audioWaveFormRightClickSeconds = seconds;
            contextMenuStripWaveForm.Show(MousePosition.X, MousePosition.Y);
        }

        void AudioWaveForm_OnNewSelectionRightClicked(Paragraph paragraph)
        {
            SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.GetIndex(paragraph));

            addParagraphHereToolStripMenuItem.Visible = true;
            deleteParagraphToolStripMenuItem.Visible = false;
            splitToolStripMenuItem1.Visible = false;
            mergeWithPreviousToolStripMenuItem.Visible = false;
            mergeWithNextToolStripMenuItem.Visible = false;

            contextMenuStripWaveForm.Show(MousePosition.X, MousePosition.Y);
        }

        void AudioWaveForm_OnTimeChanged(double seconds, Paragraph paragraph, Paragraph beforeParagraph)
        {
            if (beforeParagraph == null)
                beforeParagraph = paragraph;

            int selectedIndex = FirstSelectedIndex;
            _makeHistoryPaused = true;
            int index = _subtitle.Paragraphs.IndexOf(paragraph);
            if (index == _subtitleListViewIndex)
            {
                // Make history item for rollback (change paragraph back for history + change again)
                _subtitle.Paragraphs[index] = new Paragraph(beforeParagraph);
                MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + paragraph.Number + " " + paragraph.Text));
                _subtitle.Paragraphs[index] = paragraph;

                Paragraph original = null;
                if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible)
                    original = Utilities.GetOriginalParagraph(index, beforeParagraph, _subtitleAlternate.Paragraphs);

                timeUpDownStartTime.TimeCode = paragraph.StartTime;
                decimal durationInSeconds = (decimal) (paragraph.Duration.TotalSeconds);
                if (durationInSeconds >= numericUpDownDuration.Minimum && durationInSeconds <= numericUpDownDuration.Maximum)
                    SetDurationInSeconds((double)durationInSeconds);

                if (original != null)
                {
                    original.StartTime.TotalMilliseconds = paragraph.StartTime.TotalMilliseconds;
                    original.EndTime.TotalMilliseconds = paragraph.EndTime.TotalMilliseconds;
                }
            }
            else
            {
                if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible && Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable)
                {
                    index = _subtitleAlternate.GetIndex(paragraph);
                    if (index >= 0)
                    {
                        // Make history item for rollback (change paragraph back for history + change again)
                        _subtitleAlternate.Paragraphs[index] = new Paragraph(beforeParagraph);
                        MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + paragraph.Number + " " + paragraph.Text));
                        _subtitleAlternate.Paragraphs[index] = paragraph;

                        var current = Utilities.GetOriginalParagraph(index, beforeParagraph, _subtitle.Paragraphs);
                        if (current != null)
                        {
                            current.StartTime.TotalMilliseconds = paragraph.StartTime.TotalMilliseconds;
                            current.EndTime.TotalMilliseconds = paragraph.EndTime.TotalMilliseconds;

                            index = _subtitle.GetIndex(current);

                            SubtitleListview1.SetStartTime(index, paragraph);
                            SubtitleListview1.SetDuration(index, paragraph);

                            if (index == selectedIndex)
                            {
                                timeUpDownStartTime.TimeCode = paragraph.StartTime;
                                decimal durationInSeconds = (decimal)(paragraph.Duration.TotalSeconds);
                                if (durationInSeconds >= numericUpDownDuration.Minimum && durationInSeconds <= numericUpDownDuration.Maximum)
                                    SetDurationInSeconds((double)durationInSeconds);
                            }
                        }
                    }
                }
                else if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible)
                {
                    index = _subtitle.GetIndex(paragraph);
                    if (index >= 0)
                    {
                        // Make history item for rollback (change paragraph back for history + change again)
                        _subtitle.Paragraphs[index] = new Paragraph(beforeParagraph);
                        MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + paragraph.Number + " " + paragraph.Text));
                        _subtitle.Paragraphs[index] = paragraph;

                        var original = Utilities.GetOriginalParagraph(index, beforeParagraph, _subtitleAlternate.Paragraphs);
                        if (original != null)
                        {
                            original.StartTime.TotalMilliseconds = paragraph.StartTime.TotalMilliseconds;
                            original.EndTime.TotalMilliseconds = paragraph.EndTime.TotalMilliseconds;
                        }
                        SubtitleListview1.SetStartTime(index, paragraph);
                        SubtitleListview1.SetDuration(index, paragraph);
                    }
                }
                else
                {
                    if (index >= 0)
                    {
                        // Make history item for rollback (change paragraph back for history + change again)
                        _subtitle.Paragraphs[index] = new Paragraph(beforeParagraph);
                        MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + paragraph.Number + " " + paragraph.Text));
                        _subtitle.Paragraphs[index] = paragraph;
                    }

                    SubtitleListview1.SetStartTime(index, paragraph);
                    SubtitleListview1.SetDuration(index, paragraph);
                }
            }
            beforeParagraph.StartTime.TotalMilliseconds = paragraph.StartTime.TotalMilliseconds;
            beforeParagraph.EndTime.TotalMilliseconds = paragraph.EndTime.TotalMilliseconds;
            _makeHistoryPaused = false;
        }

        void AudioWaveForm_OnPositionSelected(double seconds, Paragraph paragraph)
        {
            mediaPlayer.CurrentPosition = seconds;
            if (paragraph != null)
                SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.GetIndex(paragraph));
        }

        private void VideoPositionChanged(object sender, EventArgs e)
        {
            var tud = (TimeUpDown)sender;
            if (tud.Enabled)
            {
                mediaPlayer.CurrentPosition = tud.TimeCode.TotalSeconds;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel1MinSize = 525;
            splitContainer1.Panel2MinSize = 250;
            splitContainerMain.Panel1MinSize = 200;
            splitContainerMain.Panel2MinSize = 220;

            if (Configuration.Settings.General.StartListViewWidth < 250)
                Configuration.Settings.General.StartListViewWidth = (Width / 3) * 2;

            if (Configuration.Settings.General.StartRememberPositionAndSize &&
                !string.IsNullOrEmpty(Configuration.Settings.General.StartPosition))
            {
                string[] parts = Configuration.Settings.General.StartPosition.Split(';');
                if (parts.Length == 2)
                {
                    int x;
                    int y;
                    if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                    {
                        if (x > -100 || y > -100)
                        {
                            Left = x;
                            Top = y;
                        }
                    }
                }

                if (Configuration.Settings.General.StartSize == "Maximized")
                {
                    CenterFormOnCurrentScreen();
                    WindowState = FormWindowState.Maximized;
                    if (!splitContainer1.Panel2Collapsed && Configuration.Settings.General.StartRememberPositionAndSize)
                        splitContainer1.SplitterDistance = Configuration.Settings.General.StartListViewWidth;
                    return;
                }

                parts = Configuration.Settings.General.StartSize.Split(';');
                if (parts.Length == 2)
                {
                    int x;
                    int y;
                    if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                    {
                        Width = x;
                        Height = y;
                    }
                }

                Screen screen = Screen.FromControl(this);

                if (screen.Bounds.Width < Width)
                    Width = screen.Bounds.Width;
                if (screen.Bounds.Height < Height)
                    Height = screen.Bounds.Height;

                if (screen.Bounds.X + screen.Bounds.Width - 200 < Left)
                    Left = screen.Bounds.X + screen.Bounds.Width - Width;
                if (screen.Bounds.Y + screen.Bounds.Height - 100 < Top)
                    Top = screen.Bounds.Y + screen.Bounds.Height - Height;
            }
            else
            {
                CenterFormOnCurrentScreen();
            }
            if (!splitContainer1.Panel2Collapsed && Configuration.Settings.General.StartRememberPositionAndSize)
            {
                splitContainer1.SplitterDistance = Configuration.Settings.General.StartListViewWidth;
            }

            if (Environment.OSVersion.Version.Major < 6 && Configuration.Settings.General.SubtitleFontName == Utilities.WinXp2kUnicodeFontName) // 6 == Vista/Win2008Server/Win7
            {
                const string unicodeFontName = Utilities.WinXp2kUnicodeFontName;
                Configuration.Settings.General.SubtitleFontName = unicodeFontName;
                float fontSize = toolStripMenuItemInsertUnicodeSymbol.Font.Size;
                textBoxSource.Font = new Font(unicodeFontName, fontSize);
                textBoxListViewText.Font = new Font(unicodeFontName, fontSize);
                SubtitleListview1.Font = new Font(unicodeFontName, fontSize);
                toolStripWaveControls.RenderMode = ToolStripRenderMode.System;
                toolStripMenuItemSurroundWithMusicSymbols.Font = new Font(unicodeFontName, fontSize);
            }
        }

        private void InitializeLanguage()
        {
            fileToolStripMenuItem.Text = _language.Menu.File.Title;
            newToolStripMenuItem.Text = _language.Menu.File.New;
            openToolStripMenuItem.Text = _language.Menu.File.Open;
            reopenToolStripMenuItem.Text = _language.Menu.File.Reopen;
            saveToolStripMenuItem.Text = _language.Menu.File.Save;
            saveAsToolStripMenuItem.Text = _language.Menu.File.SaveAs;
            toolStripMenuItemRestoreAutoBackup.Text = _language.Menu.File.RestoreAutoBackup;
            openOriginalToolStripMenuItem.Text = _language.Menu.File.OpenOriginal;
            saveOriginalToolStripMenuItem.Text = _language.Menu.File.SaveOriginal;
            saveOriginalAstoolStripMenuItem.Text = _language.SaveOriginalSubtitleAs;
            removeOriginalToolStripMenuItem.Text = _language.Menu.File.CloseOriginal;

            toolStripMenuItemOpenContainingFolder.Text = _language.Menu.File.OpenContainingFolder;
            toolStripMenuItemCompare.Text = _language.Menu.File.Compare;
            toolStripMenuItemStatistics.Text = _language.Menu.File.Statistics;
            toolStripMenuItemImportDvdSubtitles.Text = _language.Menu.File.ImportOcrFromDvd;
            toolStripMenuItemSubIdx.Text = _language.Menu.File.ImportOcrVobSubSubtitle;
            toolStripButtonGetFrameRate.ToolTipText = _language.GetFrameRateFromVideoFile;

            toolStripMenuItemImportBluRaySup.Text = _language.Menu.File.ImportBluRaySupFile;

            matroskaImportStripMenuItem.Text = _language.Menu.File.ImportSubtitleFromMatroskaFile;
            toolStripMenuItemManualAnsi.Text = _language.Menu.File.ImportSubtitleWithManualChosenEncoding;
            toolStripMenuItemImportText.Text = _language.Menu.File.ImportText;
            toolStripMenuItemImportTimeCodes.Text = _language.Menu.File.ImportTimecodes;
            toolStripMenuItemExport.Text = _language.Menu.File.Export;
            toolStripMenuItemExportPngXml.Text = _language.Menu.File.ExportBdnXml;
            bluraySupToolStripMenuItem.Text = _language.Menu.File.ExportBluRaySup;

            //vobSubsubidxToolStripMenuItem.Visible = true;
            //vobSubsubidxToolStripMenuItem.Text = _language.Menu.File.ExportVobSub;

            toolStripMenuItemCavena890.Text = _language.Menu.File.ExportCavena890;
            eBUSTLToolStripMenuItem.Text = _language.Menu.File.ExportEbu;
            pACScreenElectronicsToolStripMenuItem.Text = _language.Menu.File.ExportPac;
            plainTextToolStripMenuItem.Text = _language.Menu.File.ExportPlainText;
            exitToolStripMenuItem.Text = _language.Menu.File.Exit;

            editToolStripMenuItem.Text = _language.Menu.Edit.Title;
            showHistoryforUndoToolStripMenuItem.Text = _language.Menu.Edit.ShowUndoHistory;

            toolStripMenuItemInsertUnicodeCharacter.Text = _language.Menu.Edit.InsertUnicodeSymbol;

            findToolStripMenuItem.Text = _language.Menu.Edit.Find;
            findNextToolStripMenuItem.Text = _language.Menu.Edit.FindNext;
            replaceToolStripMenuItem.Text = _language.Menu.Edit.Replace;
            multipleReplaceToolStripMenuItem.Text = _language.Menu.Edit.MultipleReplace;
            gotoLineNumberToolStripMenuItem.Text = _language.Menu.Edit.GoToSubtitleNumber;
            toolStripMenuItemRightToLeftMode.Text = _language.Menu.Edit.RightToLeftMode;
            toolStripMenuItemReverseRightToLeftStartEnd.Text = _language.Menu.Edit.ReverseRightToLeftStartEnd;
            editSelectAllToolStripMenuItem.Text = _language.Menu.ContextMenu.SelectAll;

            toolsToolStripMenuItem.Text = _language.Menu.Tools.Title;
            adjustDisplayTimeToolStripMenuItem.Text = _language.Menu.Tools.AdjustDisplayDuration;
            fixToolStripMenuItem.Text = _language.Menu.Tools.FixCommonErrors;
            startNumberingFromToolStripMenuItem.Text = _language.Menu.Tools.StartNumberingFrom;
            removeTextForHearImparedToolStripMenuItem.Text = _language.Menu.Tools.RemoveTextForHearingImpaired;
            ChangeCasingToolStripMenuItem.Text = _language.Menu.Tools.ChangeCasing;
            toolStripMenuItemChangeFrameRate2.Text = _language.Menu.Tools.ChangeFrameRate;
            toolStripMenuItemAutoMergeShortLines.Text = _language.Menu.Tools.MergeShortLines;
            toolStripMenuItemAutoSplitLongLines.Text = _language.Menu.Tools.SplitLongLines;
            setMinimumDisplayTimeBetweenParagraphsToolStripMenuItem.Text = _language.Menu.Tools.MinimumDisplayTimeBetweenParagraphs;
            toolStripMenuItem1.Text = _language.Menu.Tools.SortBy;

            if (!string.IsNullOrEmpty(_language.Menu.Tools.Number))
                sortNumberToolStripMenuItem.Text = _language.Menu.Tools.Number;
            else
                sortNumberToolStripMenuItem.Text = _languageGeneral.Number;

            if (!string.IsNullOrEmpty(_language.Menu.Tools.StartTime))
                sortStartTimeToolStripMenuItem.Text = _language.Menu.Tools.StartTime;
            else
                sortStartTimeToolStripMenuItem.Text = _languageGeneral.StartTime;

            if (!string.IsNullOrEmpty(_language.Menu.Tools.EndTime))
                sortEndTimeToolStripMenuItem.Text = _language.Menu.Tools.EndTime;
            else
                sortEndTimeToolStripMenuItem.Text = _languageGeneral.EndTime;

            if (!string.IsNullOrEmpty(_language.Menu.Tools.Duration))
                sortDisplayTimeToolStripMenuItem.Text = _language.Menu.Tools.Duration;
            else
                sortDisplayTimeToolStripMenuItem.Text = _languageGeneral.Duration;

            sortTextAlphabeticallytoolStripMenuItem.Text = _language.Menu.Tools.TextAlphabetically;
            sortTextMaxLineLengthToolStripMenuItem.Text = _language.Menu.Tools.TextSingleLineMaximumLength;
            sortTextTotalLengthToolStripMenuItem.Text = _language.Menu.Tools.TextTotalLength;
            sortTextNumberOfLinesToolStripMenuItem.Text = _language.Menu.Tools.TextNumberOfLines;
            textCharssecToolStripMenuItem.Text = _language.Menu.Tools.TextNumberOfCharactersPerSeconds;
            textWordsPerMinutewpmToolStripMenuItem.Text = _language.Menu.Tools.WordsPerMinute;

            toolStripMenuItemShowOriginalInPreview.Text = _language.Menu.Tools.ShowOriginalTextInAudioAndVideoPreview;
            toolStripMenuItemMakeEmptyFromCurrent.Text = _language.Menu.Tools.MakeNewEmptyTranslationFromCurrentSubtitle;
            splitToolStripMenuItem.Text = _language.Menu.Tools.SplitSubtitle;
            appendTextVisuallyToolStripMenuItem.Text = _language.Menu.Tools.AppendSubtitle;
            joinSubtitlesToolStripMenuItem.Text = _language.Menu.Tools.JoinSubtitles;

            toolStripMenuItemVideo.Text = _language.Menu.Video.Title;
            openVideoToolStripMenuItem.Text = _language.Menu.Video.OpenVideo;
            toolStripMenuItemSetAudioTrack.Text = _language.Menu.Video.ChooseAudioTrack;
            closeVideoToolStripMenuItem.Text = _language.Menu.Video.CloseVideo;

            if (Configuration.Settings.VideoControls.GenerateSpectrogram)
                showhideWaveFormToolStripMenuItem.Text = _language.Menu.Video.ShowHideWaveformAndSpectrogram;
            else
                showhideWaveFormToolStripMenuItem.Text = _language.Menu.Video.ShowHideWaveForm;

            showhideVideoToolStripMenuItem.Text = _language.Menu.Video.ShowHideVideo;
            undockVideoControlsToolStripMenuItem.Text = _language.Menu.Video.UnDockVideoControls;
            redockVideoControlsToolStripMenuItem.Text = _language.Menu.Video.ReDockVideoControls;

            toolStripMenuItemSpellCheckMain.Text = _language.Menu.SpellCheck.Title;
            spellCheckToolStripMenuItem.Text = _language.Menu.SpellCheck.SpellCheck;
            findDoubleWordsToolStripMenuItem.Text = _language.Menu.SpellCheck.FindDoubleWords;
            FindDoubleLinesToolStripMenuItem.Text = _language.Menu.SpellCheck.FindDoubleLines;
            GetDictionariesToolStripMenuItem.Text = _language.Menu.SpellCheck.GetDictionaries;
            addWordToNamesetcListToolStripMenuItem.Text = _language.Menu.SpellCheck.AddToNamesEtcList;

            toolStripMenuItemSyncronization.Text = _language.Menu.Synchronization.Title;
            toolStripMenuItemAdjustAllTimes.Text = _language.Menu.Synchronization.AdjustAllTimes;
            visualSyncToolStripMenuItem.Text = _language.Menu.Synchronization.VisualSync;
            toolStripMenuItemPointSync.Text = _language.Menu.Synchronization.PointSync;
            pointSyncViaOtherSubtitleToolStripMenuItem.Text = _language.Menu.Synchronization.PointSyncViaOtherSubtitle;

            toolStripMenuItemAutoTranslate.Text = _language.Menu.AutoTranslate.Title;
            translateByGoogleToolStripMenuItem.Text = _language.Menu.AutoTranslate.TranslatePoweredByGoogle;
            translatepoweredByMicrosoftToolStripMenuItem.Text = _language.Menu.AutoTranslate.TranslatePoweredByMicrosoft;
            translatepoweredByMicrosoftToolStripMenuItem.Visible = Configuration.Settings.Tools.MicrosoftBingApiId != "C2C2E9A508E6748F0494D68DFD92FAA1FF9B0BA4";
            translateFromSwedishToDanishToolStripMenuItem.Text = _language.Menu.AutoTranslate.TranslateFromSwedishToDanish;

            optionsToolStripMenuItem.Text = _language.Menu.Options.Title;
            settingsToolStripMenuItem.Text = _language.Menu.Options.Settings;
            changeLanguageToolStripMenuItem.Text = _language.Menu.Options.ChooseLanguage;
            try
            {
                var ci = new System.Globalization.CultureInfo(_languageGeneral.CultureName);
                changeLanguageToolStripMenuItem.Text += " [" + ci.NativeName + "]";
            }
            catch
            {
            }

            toolStripMenuItemNetworking.Text = _language.Menu.Networking.Title;
            startServerToolStripMenuItem.Text = _language.Menu.Networking.StartNewSession;
            joinSessionToolStripMenuItem.Text = _language.Menu.Networking.JoinSession;
            showSessionKeyLogToolStripMenuItem.Text = _language.Menu.Networking.ShowSessionInfoAndLog;
            chatToolStripMenuItem.Text = _language.Menu.Networking.Chat;
            leaveSessionToolStripMenuItem.Text = _language.Menu.Networking.LeaveSession;

            helpToolStripMenuItem.Text = _language.Menu.Help.Title;
            helpToolStripMenuItem1.Text = _language.Menu.Help.Help;
            aboutToolStripMenuItem.Text = _language.Menu.Help.About;

            toolStripButtonFileNew.ToolTipText = _language.Menu.ToolBar.New;
            toolStripButtonFileOpen.ToolTipText = _language.Menu.ToolBar.Open;
            toolStripButtonSave.ToolTipText = _language.Menu.ToolBar.Save;
            toolStripButtonSaveAs.ToolTipText = _language.Menu.ToolBar.SaveAs;
            toolStripButtonFind.ToolTipText = _language.Menu.ToolBar.Find;
            toolStripButtonReplace.ToolTipText = _language.Menu.ToolBar.Replace;
            toolStripButtonVisualSync.ToolTipText = _language.Menu.ToolBar.VisualSync;
            toolStripButtonSpellCheck.ToolTipText = _language.Menu.ToolBar.SpellCheck;
            toolStripButtonSettings.ToolTipText = _language.Menu.ToolBar.Settings;
            toolStripButtonHelp.ToolTipText = _language.Menu.ToolBar.Help;
            toolStripButtonToggleWaveForm.ToolTipText = _language.Menu.ToolBar.ShowHideWaveForm;
            toolStripButtonToggleVideo.ToolTipText = _language.Menu.ToolBar.ShowHideVideo;

            toolStripMenuItemAssStyles.Text = _language.Menu.ContextMenu.SubStationAlphaStyles;
            setStylesForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.SubStationAlphaSetStyle;

            toolStripMenuItemDelete.Text = _language.Menu.ContextMenu.Delete;
            insertLineToolStripMenuItem.Text = _language.Menu.ContextMenu.InsertFirstLine;
            toolStripMenuItemInsertBefore.Text = _language.Menu.ContextMenu.InsertBefore;
            toolStripMenuItemInsertAfter.Text = _language.Menu.ContextMenu.InsertAfter;
            toolStripMenuItemInsertSubtitle.Text = _language.Menu.ContextMenu.InsertSubtitleAfter;

            toolStripMenuItemCopySourceText.Text = _language.Menu.ContextMenu.CopyToClipboard;

            splitLineToolStripMenuItem.Text = _language.Menu.ContextMenu.Split;
            toolStripMenuItemMergeLines.Text = _language.Menu.ContextMenu.MergeSelectedLines;
            toolStripMenuItemMergeDialogue.Text = _language.Menu.ContextMenu.MergeSelectedLinesASDialogue;
            mergeBeforeToolStripMenuItem.Text = _language.Menu.ContextMenu.MergeWithLineBefore;
            mergeAfterToolStripMenuItem.Text = _language.Menu.ContextMenu.MergeWithLineAfter;
            normalToolStripMenuItem.Text = _language.Menu.ContextMenu.Normal;
            boldToolStripMenuItem.Text = _languageGeneral.Bold;
            underlineToolStripMenuItem.Text = _language.Menu.ContextMenu.Underline;
            italicToolStripMenuItem.Text = _languageGeneral.Italic;
            colorToolStripMenuItem.Text = _language.Menu.ContextMenu.Color;
            toolStripMenuItemFont.Text = _language.Menu.ContextMenu.FontName;
            toolStripMenuItemAlignment.Text = _language.Menu.ContextMenu.Alignment;
            toolStripMenuItemAutoBreakLines.Text = _language.Menu.ContextMenu.AutoBalanceSelectedLines;
            toolStripMenuItemUnbreakLines.Text = _language.Menu.ContextMenu.RemoveLineBreaksFromSelectedLines;
            typeEffectToolStripMenuItem.Text = _language.Menu.ContextMenu.TypewriterEffect;
            karokeeEffectToolStripMenuItem.Text = _language.Menu.ContextMenu.KaraokeEffect;
            showSelectedLinesEarlierlaterToolStripMenuItem.Text = _language.Menu.ContextMenu.ShowSelectedLinesEarlierLater;
            visualSyncSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.VisualSyncSelectedLines;
            toolStripMenuItemGoogleMicrosoftTranslateSelLine.Text = _language.Menu.ContextMenu.GoogleAndMicrosoftTranslateSelectedLine;
            googleTranslateSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.GoogleTranslateSelectedLines;
            adjustDisplayTimeForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.AdjustDisplayDurationForSelectedLines;
            fixCommonErrorsInSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.FixCommonErrorsInSelectedLines;
            changeCasingForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.ChangeCasingForSelectedLines;
            toolStripMenuItemSaveSelectedLines.Text = _language.Menu.ContextMenu.SaveSelectedLines;

            // textbox context menu
            cutToolStripMenuItem.Text = _language.Menu.ContextMenu.Cut;
            copyToolStripMenuItem.Text = _language.Menu.ContextMenu.Copy;
            pasteToolStripMenuItem.Text = _language.Menu.ContextMenu.Paste;
            deleteToolStripMenuItem.Text = _language.Menu.ContextMenu.Delete;
            toolStripMenuItemSplitTextAtCursor.Text = _language.Menu.ContextMenu.SplitLineAtCursorPosition;
            selectAllToolStripMenuItem.Text = _language.Menu.ContextMenu.SelectAll;
            normalToolStripMenuItem1.Text = _language.Menu.ContextMenu.Normal;
            boldToolStripMenuItem1.Text = _languageGeneral.Bold;
            italicToolStripMenuItem1.Text = _languageGeneral.Italic;
            underlineToolStripMenuItem1.Text = _language.Menu.ContextMenu.Underline;
            colorToolStripMenuItem1.Text = _language.Menu.ContextMenu.Color;
            fontNameToolStripMenuItem.Text = _language.Menu.ContextMenu.FontName;
            toolStripMenuItemInsertUnicodeSymbol.Text = _language.Menu.Edit.InsertUnicodeSymbol;

            // main controls
            SubtitleListview1.InitializeLanguage(_languageGeneral, Configuration.Settings);
            toolStripLabelSubtitleFormat.Text = _language.Controls.SubtitleFormat;
            toolStripLabelEncoding.Text = _language.Controls.FileEncoding;
            tabControlSubtitle.TabPages[0].Text = _language.Controls.ListView;
            tabControlSubtitle.TabPages[1].Text = _language.Controls.SourceView;
            labelDuration.Text = _languageGeneral.Duration;
            labelStartTime.Text = _languageGeneral.StartTime;
            labelText.Text = _languageGeneral.Text;
            labelAlternateText.Text = Configuration.Settings.Language.General.OriginalText;
            toolStripLabelFrameRate.Text = _languageGeneral.FrameRate;
            buttonPrevious.Text = _language.Controls.Previous;
            buttonNext.Text = _language.Controls.Next;
            buttonAutoBreak.Text = _language.Controls.AutoBreak;
            buttonUnBreak.Text = _language.Controls.Unbreak;
            buttonSplitLine.Text = _languageGeneral.SplitLine;
            ShowSourceLineNumber();

            // Video controls
            tabPageTranslate.Text = _language.VideoControls.Translate + "  ";
            tabPageCreate.Text = _language.VideoControls.Create + "  ";
            tabPageAdjust.Text = _language.VideoControls.Adjust + "  ";
            checkBoxSyncListViewWithVideoWhilePlaying.Text = _language.VideoControls.SelectCurrentElementWhilePlaying;
            if (_videoFileName == null)
                labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
            toolStripButtonLockCenter.Text = _language.VideoControls.Center;
            toolStripSplitButtonPlayRate.Text = _language.VideoControls.PlayRate;
            toolStripMenuItemPlayRateSlow.Text = _language.VideoControls.Slow;
            toolStripMenuItemPlayRateNormal.Text = _language.VideoControls.Normal;
            toolStripMenuItemPlayRateFast.Text = _language.VideoControls.Fast;
            toolStripMenuItemPlayRateVeryFast.Text = _language.VideoControls.VeryFast;

            groupBoxAutoRepeat.Text = _language.VideoControls.AutoRepeat;
            checkBoxAutoRepeatOn.Text = _language.VideoControls.AutoRepeatOn;
            labelAutoRepeatCount.Text = _language.VideoControls.AutoRepeatCount;
            groupBoxAutoContinue.Text = _language.VideoControls.AutoContinue;
            checkBoxAutoContinue.Text = _language.VideoControls.AutoContinueOn;
            labelAutoContinueDelay.Text = _language.VideoControls.DelayInSeconds;
            buttonPlayPrevious.Text = _language.VideoControls.Previous;
            buttonPlayCurrent.Text = _language.VideoControls.PlayCurrent;
            buttonPlayNext.Text = _language.VideoControls.Next;
            buttonStop.Text = _language.VideoControls.Pause;
            groupBoxTranslateSearch.Text = _language.VideoControls.SearchTextOnline;
            buttonGoogleIt.Text = _language.VideoControls.GoogleIt;
            buttonGoogleTranslateIt.Text = _language.VideoControls.GoogleTranslate;
            labelTranslateTip.Text = _language.VideoControls.TranslateTip;

            buttonInsertNewText.Text = _language.VideoControls.InsertNewSubtitleAtVideoPosition;
            buttonBeforeText.Text = _language.VideoControls.PlayFromJustBeforeText;
            buttonGotoSub.Text = _language.VideoControls.GoToSubtitlePositionAndPause;
            buttonSetStartTime.Text = _language.VideoControls.SetStartTime;
            buttonSetEnd.Text = _language.VideoControls.SetEndTime;
            buttonSecBack1.Text = _language.VideoControls.SecondsBackShort;
            buttonSecBack2.Text = _language.VideoControls.SecondsBackShort;
            buttonForward1.Text = _language.VideoControls.SecondsForwardShort;
            buttonForward2.Text = _language.VideoControls.SecondsForwardShort;
            labelVideoPosition.Text = _language.VideoControls.VideoPosition;
            labelVideoPosition2.Text = _language.VideoControls.VideoPosition;
            labelCreateTip.Text = _language.VideoControls.CreateTip;

            buttonSetStartAndOffsetRest.Text = _language.VideoControls.SetstartTimeAndOffsetOfRest;
            buttonSetEndAndGoToNext.Text = _language.VideoControls.SetEndTimeAndGoToNext;
            buttonAdjustSetStartTime.Text = _language.VideoControls.SetStartTime;
            buttonAdjustSetEndTime.Text = _language.VideoControls.SetEndTime;
            buttonAdjustPlayBefore.Text = _language.VideoControls.PlayFromJustBeforeText;
            buttonAdjustGoToPosAndPause.Text = _language.VideoControls.GoToSubtitlePositionAndPause;
            buttonAdjustSecBack1.Text = _language.VideoControls.SecondsBackShort;
            buttonAdjustSecBack2.Text = _language.VideoControls.SecondsBackShort;
            buttonAdjustSecForward1.Text = _language.VideoControls.SecondsForwardShort;
            buttonAdjustSecForward2.Text = _language.VideoControls.SecondsForwardShort;
            labelAdjustTip.Text = _language.VideoControls.CreateTip;

            //waveform
            addParagraphHereToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.AddParagraphHere;
            deleteParagraphToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.DeleteParagraph;
            splitToolStripMenuItem1.Text = Configuration.Settings.Language.WaveForm.Split;
            mergeWithPreviousToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.MergeWithPrevious;
            mergeWithNextToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.MergeWithNext;
            toolStripMenuItemWaveFormPlaySelection.Text = Configuration.Settings.Language.WaveForm.PlaySelection;
            showWaveformAndSpectrogramToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.ShowWaveformAndSpectrogram;
            showOnlyWaveformToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.ShowWaveformOnly;
            showOnlySpectrogramToolStripMenuItem.Text = Configuration.Settings.Language.WaveForm.ShowSpectrogramOnly;

            toolStripButtonWaveFormZoomOut.ToolTipText = Configuration.Settings.Language.WaveForm.ZoomOut;
            toolStripButtonWaveFormZoomIn.ToolTipText = Configuration.Settings.Language.WaveForm.ZoomIn;

            if (Configuration.Settings.VideoControls.GenerateSpectrogram)
                audioVisualizer.WaveFormNotLoadedText = Configuration.Settings.Language.WaveForm.ClickToAddWaveformAndSpectrogram;
            else
                audioVisualizer.WaveFormNotLoadedText = Configuration.Settings.Language.WaveForm.ClickToAddWaveForm;
        }

        private void SetFormatToSubRip()
        {
            comboBoxSubtitleFormats.SelectedIndexChanged -= ComboBoxSubtitleFormatsSelectedIndexChanged;
            foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
            {
                if (!format.IsVobSubIndexFile)
                    comboBoxSubtitleFormats.Items.Add(format.FriendlyName);
            }
            comboBoxSubtitleFormats.SelectedIndex = 0;
            comboBoxSubtitleFormats.SelectedIndexChanged += ComboBoxSubtitleFormatsSelectedIndexChanged;
        }

        private int FirstSelectedIndex
        {
            get
            {
                if (SubtitleListview1.SelectedItems.Count == 0)
                    return -1;
                return SubtitleListview1.SelectedItems[0].Index;
            }
        }

        private int FirstVisibleIndex
        {
            get
            {
                if (SubtitleListview1.Items.Count == 0 || SubtitleListview1.TopItem == null)
                    return -1;
                return SubtitleListview1.TopItem.Index;
            }
        }

        private bool ContinueNewOrExit()
        {
            if (_changeSubtitleToString != _subtitle.ToText(new SubRip()).Trim())
            {
                if (_lastDoNotPrompt != _subtitle.ToText(new SubRip()).Trim())
                {

                    string promptText = _language.SaveChangesToUntitled;
                    if (!string.IsNullOrEmpty(_fileName))
                        promptText = string.Format(_language.SaveChangesToX, _fileName);

                    DialogResult dr = MessageBox.Show(promptText, Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                    if (dr == DialogResult.Cancel)
                        return false;

                    if (dr == DialogResult.Yes)
                    {
                        if (string.IsNullOrEmpty(_fileName))
                        {
                            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;
                            saveFileDialog1.Title = _language.SaveSubtitleAs;
                            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                            {
                                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                                _fileName = saveFileDialog1.FileName;
                                SetTitle();
                                Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
                                Configuration.Settings.Save();

                            }
                            else
                            {
                                return false;
                            }
                        }
                        if (SaveSubtitle(GetCurrentSubtitleFormat()) != DialogResult.OK)
                            return false;
                    }
                }
            }

            return ContinueNewOrExitAlternate();
        }

        private bool ContinueNewOrExitAlternate()
        {
            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0 && _changeAlternateSubtitleToString != _subtitleAlternate.ToText(new SubRip()).Trim())
            {
                string promptText = _language.SaveChangesToUntitledOriginal;
                if (!string.IsNullOrEmpty(_subtitleAlternateFileName))
                    promptText = string.Format(_language.SaveChangesToOriginalX, _subtitleAlternateFileName);

                DialogResult dr = MessageBox.Show(promptText, Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                if (dr == DialogResult.Cancel)
                    return false;

                if (dr == DialogResult.Yes)
                {
                    if (string.IsNullOrEmpty(_subtitleAlternateFileName))
                    {
                        if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                            saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;
                        saveFileDialog1.Title = _language.SaveOriginalSubtitleAs;
                        if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                        {
                            openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                            _subtitleAlternateFileName = saveFileDialog1.FileName;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    if (SaveOriginalSubtitle(GetCurrentSubtitleFormat()) != DialogResult.OK)
                        return false;
                }
            }
            _lastDoNotPrompt = _subtitle.ToText(new SubRip()).Trim();
            return true;
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Application.Exit();
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            var about = new About();
            _formPositionsAndSizes.SetPositionAndSize(about);
            about.ShowDialog(this);
            _formPositionsAndSizes.SavePositionAndSize(about);
        }

        private void VisualSyncToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            ShowVisualSync(false);
        }

        public void MakeHistoryForUndo(string description, bool resetTextUndo)
        {
            if (_makeHistoryPaused)
                return;

            if (resetTextUndo)
            {
                _listViewTextUndoLast = null;
                _listViewAlternateTextUndoLast = null;
            }

            if (_undoIndex == -1)
            {
                _subtitle.HistoryItems.Clear();
            }
            else
            {
                // remove items for redo
                while (_subtitle.HistoryItems.Count > _undoIndex + 1)
                    _subtitle.HistoryItems.RemoveAt(_subtitle.HistoryItems.Count - 1);
            }

            _subtitle.MakeHistoryForUndo(description, GetCurrentSubtitleFormat(), _fileDateTime, _subtitleAlternate, _subtitleAlternateFileName, _subtitleListViewIndex, textBoxListViewText.SelectionStart, textBoxListViewTextAlternate.SelectionStart);
            _undoIndex++;

            if (_undoIndex > Subtitle.MaximumHistoryItems)
                _undoIndex--;
        }

        public void MakeHistoryForUndo(string description)
        {
            MakeHistoryForUndo(description, true);
        }

        /// <summary>
        /// Add undo history - but only if last entry is older than 500 ms
        /// </summary>
        /// <param name="description">Undo description</param>
        public void MakeHistoryForUndoOnlyIfNotResent(string description)
        {
            if (_makeHistoryPaused)
                return;

            if ((DateTime.Now.Ticks - _lastHistoryTicks) > 10000 * 500) // only if last change was longer ago than 500 milliseconds
            {
                MakeHistoryForUndo(description);
                _lastHistoryTicks = DateTime.Now.Ticks;
            }
        }

        private bool IsSubtitleLoaded
        {
            get
            {
                if (_subtitle == null || _subtitle.Paragraphs.Count == 0)
                    return false;
                if (_subtitle.Paragraphs.Count == 1 && string.IsNullOrEmpty(_subtitle.Paragraphs[0].Text))
                    return false;
                return true;
            }
        }

        private void ShowVisualSync(bool onlySelectedLines)
        {
            if (IsSubtitleLoaded)
            {
                var visualSync = new VisualSync();
                _formPositionsAndSizes.SetPositionAndSize(visualSync);
                visualSync.VideoFileName = _videoFileName;
                visualSync.AudioTrackNumber = _videoAudioTrackNumber;

                SaveSubtitleListviewIndexes();
                if (onlySelectedLines)
                {
                    var selectedLines = new Subtitle { WasLoadedWithFrameNumbers = _subtitle.WasLoadedWithFrameNumbers };
                    foreach (int index in SubtitleListview1.SelectedIndices)
                        selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
                    visualSync.Initialize(toolStripButtonVisualSync.Image as Bitmap, selectedLines, _fileName, _language.VisualSyncSelectedLines, CurrentFrameRate);
                }
                else
                {
                    visualSync.Initialize(toolStripButtonVisualSync.Image as Bitmap, _subtitle, _fileName, _language.VisualSyncTitle, CurrentFrameRate);
                }

                _endSeconds = -1;
                mediaPlayer.Pause();
                if (visualSync.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeVisualSync);

                    if (onlySelectedLines)
                    { // we only update selected lines
                        int i = 0;
                        foreach (int index in SubtitleListview1.SelectedIndices)
                        {
                            _subtitle.Paragraphs[index] = visualSync.Paragraphs[i];
                            i++;
                        }
                        ShowStatus(_language.VisualSyncPerformedOnSelectedLines);
                    }
                    else
                    {
                        _subtitle.Paragraphs.Clear();
                        foreach (Paragraph p in visualSync.Paragraphs)
                            _subtitle.Paragraphs.Add(new Paragraph(p));
                        ShowStatus(_language.VisualSyncPerformed);
                    }
                    if (visualSync.FrameRateChanged)
                        toolStripComboBoxFrameRate.Text = string.Format("{0:0.###}", visualSync.FrameRate);
                    if (IsFramesRelevant && CurrentFrameRate > 0)
                    {
                        _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                        if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                            ShowSource();
                    }
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                    if (onlySelectedLines && SubtitleListview1.SelectedItems.Count > 0)
                    {
                        SubtitleListview1.EnsureVisible(SubtitleListview1.SelectedItems[SubtitleListview1.SelectedItems.Count - 1].Index);
                    }
                }
                _videoFileName = visualSync.VideoFileName;
                _formPositionsAndSizes.SavePositionAndSize(visualSync);
                visualSync.Dispose();
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void OpenToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            OpenNewFile();
        }

        private void OpenNewFile()
        {
            _lastDoNotPrompt = string.Empty;
            if (!ContinueNewOrExit())
                return;
            openFileDialog1.Title = _languageGeneral.OpenSubtitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
            try
            {
                if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                    OpenSubtitle(openFileDialog1.FileName, null);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.Write(exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine + openFileDialog1.Filter);
                openFileDialog1.Filter = Configuration.Settings.Language.General.AllFiles + "|*.*";
                if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                    OpenSubtitle(openFileDialog1.FileName, null);
            }
        }

        public double CurrentFrameRate
        {
            get
            {
                double f;
                if (double.TryParse(toolStripComboBoxFrameRate.Text, out f))
                    return f;
                return Configuration.Settings.General.DefaultFrameRate;
            }
        }

        private void OpenSubtitle(string fileName, Encoding encoding)
        {
            OpenSubtitle(fileName, encoding, null, null);
        }

        private void OpenSubtitle(string fileName, Encoding encoding, string videoFileName, string originalFileName)
        {
            if (File.Exists(fileName))
            {
                bool videoFileLoaded = false;

                // save last first visible index + first selected index from listview
                if (!string.IsNullOrEmpty(_fileName))
                    Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, originalFileName);

                openFileDialog1.InitialDirectory = Path.GetDirectoryName(fileName);

                if (Path.GetExtension(fileName).ToLower() == ".sub" && IsVobSubFile(fileName, false))
                {
                    if (MessageBox.Show(_language.ImportThisVobSubSubtitle, _title, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        ImportAndOcrVobSubSubtitleNew(fileName);
                    }
                    return;
                }

                if (Path.GetExtension(fileName).ToLower() == ".sup")
                {
                    if (IsBluRaySupFile(fileName))
                    {
                        ImportAndOcrBluRaySup(fileName);
                        return;
                    }
                    else if (IsSpDvdSupFile(fileName))
                    {
                        ImportAndOcrSpDvdSup(fileName);
                        return;
                    }
                }

                if (Path.GetExtension(fileName).ToLower() == ".mkv" || Path.GetExtension(fileName).ToLower() == ".mks")
                {
                    Matroska mkv = new Matroska();
                    bool isValid = false;
                    bool hasConstantFrameRate = false;
                    double frameRate = 0;
                    int width = 0;
                    int height = 0;
                    double milliseconds = 0;
                    string videoCodec = string.Empty;
                    mkv.GetMatroskaInfo(fileName, ref isValid, ref hasConstantFrameRate, ref frameRate, ref width, ref height, ref milliseconds, ref videoCodec);
                    if (isValid)
                    {
                        ImportSubtitleFromMatroskaFile(fileName);
                        return;
                    }
                }

                if (Path.GetExtension(fileName).ToLower() == ".divx" || Path.GetExtension(fileName).ToLower() == ".avi")
                {
                    if (ImportSubtitleFromDivX(fileName))
                        return;
                }

                var fi = new FileInfo(fileName);

                //if (Path.GetExtension(fileName).ToLower() == ".ts" && fi.Length > 10000  && IsTransportStream(fileName)) //TODO: Also check mpg, mpeg - and file header!
                //{
                //    ImportSubtitleFromTransportStream(fileName);
                //    return;
                //}

                if ((Path.GetExtension(fileName).ToLower() == ".mp4" || Path.GetExtension(fileName).ToLower() == ".m4v" || Path.GetExtension(fileName).ToLower() == ".3gp")
                    && fi.Length > 10000)
                {
                    if (ImportSubtitleFromMp4(fileName))
                        OpenVideo(fileName);
                    return;
                }

                if (fi.Length > 1024 * 1024 * 10) // max 10 mb
                {
                    if (MessageBox.Show(string.Format(_language.FileXIsLargerThan10Mb + Environment.NewLine +
                                                      Environment.NewLine +
                                                      _language.ContinueAnyway,
                                                      fileName), Title, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                        return;
                }

                if (_subtitle.HistoryItems.Count > 0 || _subtitle.Paragraphs.Count > 0)
                    MakeHistoryForUndo(string.Format(_language.BeforeLoadOf, Path.GetFileName(fileName)));

                bool change = _changeSubtitleToString != _subtitle.ToText(new SubRip()).Trim();
                if (change)
                    change = _lastDoNotPrompt != _subtitle.ToText(new SubRip()).Trim();

                SubtitleFormat format = _subtitle.LoadSubtitle(fileName, out encoding, encoding);
                if (!change)
                    _changeSubtitleToString = _subtitle.ToText(new SubRip()).Trim();

                bool justConverted = false;
                if (format == null)
                {
                    var ebu = new Ebu();
                    if (ebu.IsMine(null, fileName))
                    {
                        ebu.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = ebu;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var pac = new Pac();
                    if (pac.IsMine(null, fileName))
                    {
                        pac.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = pac;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var cavena890 = new Cavena890();
                    if (cavena890.IsMine(null, fileName))
                    {
                        cavena890.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = cavena890;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var spt = new Spt();
                    if (spt.IsMine(null, fileName))
                    {
                        spt.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = spt;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null && Path.GetExtension(fileName).ToLower() == ".wsb")
                {
                    string[] arr = File.ReadAllLines(fileName, Utilities.GetEncodingFromFile(fileName));
                    var list = new List<string>();
                    foreach (string l in arr)
                        list.Add(l);
                    var wsb = new Wsb();
                    if (wsb.IsMine(list, fileName))
                    {
                        wsb.LoadSubtitle(_subtitle, list, fileName);
                        _oldSubtitleFormat = wsb;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var cheetahCaption = new CheetahCaption();
                    if (cheetahCaption.IsMine(null, fileName))
                    {
                        cheetahCaption.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = cheetahCaption;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var capMakerPlus = new CapMakerPlus();
                    if (capMakerPlus.IsMine(null, fileName))
                    {
                        capMakerPlus.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = capMakerPlus;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var captionsInc = new CaptionsInc();
                    if (captionsInc.IsMine(null, fileName))
                    {
                        captionsInc.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = captionsInc;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var ultech130 = new Ultech130();
                    if (ultech130.IsMine(null, fileName))
                    {
                        ultech130.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = ultech130;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var nciCaption = new NciCaption();
                    if (nciCaption.IsMine(null, fileName))
                    {
                        nciCaption.LoadSubtitle(_subtitle, null, fileName);
                        _oldSubtitleFormat = nciCaption;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }

                if (format == null)
                {
                    var bdnXml = new BdnXml();
                    string[] arr = File.ReadAllLines(fileName, Utilities.GetEncodingFromFile(fileName));
                    var list = new List<string>();
                    foreach (string l in arr)
                        list.Add(l);
                    if (bdnXml.IsMine(list, fileName))
                    {
                        if (ContinueNewOrExit())
                        {
                            ImportAndOcrBdnXml(fileName, bdnXml, list);
                        }
                        return;
                    }
                }

                if (format == null || format.Name == new Scenarist().Name)
                {
                    var son = new Son();
                    string[] arr = File.ReadAllLines(fileName, Utilities.GetEncodingFromFile(fileName));
                    var list = new List<string>();
                    foreach (string l in arr)
                        list.Add(l);
                    if (son.IsMine(list, fileName))
                    {
                        if (ContinueNewOrExit())
                            ImportAndOcrSon(fileName, son, list);
                        return;
                    }
                }

                if (format == null || format.Name == new Scenarist().Name)
                {
                    var sst = new SonicScenaristBitmaps();
                    string[] arr = File.ReadAllLines(fileName, Utilities.GetEncodingFromFile(fileName));
                    var list = new List<string>();
                    foreach (string l in arr)
                        list.Add(l);
                    if (sst.IsMine(list, fileName))
                    {
                        if (ContinueNewOrExit())
                            ImportAndOcrSst(fileName, sst, list);
                        return;
                    }
                }

                if (format == null)
                {
                    var htmlSamiArray = new HtmlSamiArray();
                    string[] arr = File.ReadAllLines(fileName, Utilities.GetEncodingFromFile(fileName));
                    var list = new List<string>();
                    foreach (string l in arr)
                        list.Add(l);
                    if (htmlSamiArray.IsMine(list, fileName))
                    {
                        htmlSamiArray.LoadSubtitle(_subtitle, list, fileName);
                        _oldSubtitleFormat = htmlSamiArray;
                        SetFormatToSubRip();
                        SetEncoding(Configuration.Settings.General.DefaultEncoding);
                        encoding = GetCurrentEncoding();
                        justConverted = true;
                        format = GetCurrentSubtitleFormat();
                    }
                }


                _fileDateTime = File.GetLastWriteTime(fileName);

                if (GetCurrentSubtitleFormat().IsFrameBased)
                    _subtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                else
                    _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

                if (format != null)
                {
                    if (Configuration.Settings.General.RemoveBlankLinesWhenOpening)
                    {
                        _subtitle.RemoveEmptyLines();
                    }

                    _subtitleListViewIndex = -1;
                    SetCurrentFormat(format);
                    _subtitleAlternateFileName = null;
                    if (LoadAlternateSubtitleFile(originalFileName))
                        _subtitleAlternateFileName = originalFileName;

                    textBoxSource.Text = _subtitle.ToText(format);
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    if (SubtitleListview1.Items.Count > 0)
                        SubtitleListview1.Items[0].Selected = true;
                    _findHelper = null;
                    _spellCheckForm = null;
                    _videoFileName = null;
                    _videoInfo = null;
                    _videoAudioTrackNumber = -1;
                    labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
                    audioVisualizer.WavePeaks = null;
                    audioVisualizer.ResetSpectrogram();
                    audioVisualizer.Invalidate();

                    if (Configuration.Settings.General.ShowVideoPlayer || Configuration.Settings.General.ShowAudioVisualizer)
                    {
                        if (!string.IsNullOrEmpty(videoFileName) && File.Exists(videoFileName))
                        {
                            OpenVideo(videoFileName);
                        }
                        else if (!string.IsNullOrEmpty(fileName) && (toolStripButtonToggleVideo.Checked || toolStripButtonToggleWaveForm.Checked))
                        {
                            TryToFindAndOpenVideoFile(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName)));
                        }
                    }
                    videoFileLoaded = _videoFileName != null;

                    if (Configuration.Settings.RecentFiles.Files.Count > 0 &&
                        Configuration.Settings.RecentFiles.Files[0].FileName == fileName)
                    {
                    }
                    else
                    {
                        Configuration.Settings.RecentFiles.Add(fileName, _videoFileName, _subtitleAlternateFileName);
                        Configuration.Settings.Save();
                        UpdateRecentFilesUI();
                    }
                    _fileName = fileName;
                    SetTitle();
                    ShowStatus(string.Format(_language.LoadedSubtitleX, _fileName));
                    _sourceViewChange = false;
                    _changeSubtitleToString = _subtitle.ToText(new SubRip()).Trim();
                    _converted = false;

                    SetUndockedWindowsTitle();

                    if (justConverted)
                    {
                        _converted = true;
                        ShowStatus(string.Format(_language.LoadedSubtitleX, _fileName) + " - " + string.Format(_language.ConvertedToX, format.FriendlyName));
                    }
                    SetEncoding(encoding);

                    if (format.GetType() == typeof(SubStationAlpha))
                    {
                        string errors = AdvancedSubStationAlpha.CheckForErrors(_subtitle.Header);
                        if (!string.IsNullOrEmpty(errors))
                            MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        errors = (format as SubStationAlpha).Errors;
                        if (!string.IsNullOrEmpty(errors))
                            MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (format.GetType() == typeof(AdvancedSubStationAlpha))
                    {
                        string errors = AdvancedSubStationAlpha.CheckForErrors(_subtitle.Header);
                        if (!string.IsNullOrEmpty(errors))
                            MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        errors = (format as AdvancedSubStationAlpha).Errors;
                        if (!string.IsNullOrEmpty(errors))
                            MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (format.GetType() == typeof(SubRip))
                    {
                        string errors = (format as SubRip).Errors;
                        if (!string.IsNullOrEmpty(errors))
                            MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (format.GetType() == typeof(MicroDvd))
                    {
                        string errors = (format as MicroDvd).Errors;
                        if (!string.IsNullOrEmpty(errors))
                            MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    var info = new FileInfo(fileName);
                    if (info.Length < 50)
                    {
                        _findHelper = null;
                        _spellCheckForm = null;
                        _videoFileName = null;
                        _videoInfo = null;
                        _videoAudioTrackNumber = -1;
                        labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
                        audioVisualizer.WavePeaks = null;
                        audioVisualizer.ResetSpectrogram();
                        audioVisualizer.Invalidate();

                        Configuration.Settings.RecentFiles.Add(fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
                        Configuration.Settings.Save();
                        UpdateRecentFilesUI();
                        _fileName = fileName;
                        SetTitle();
                        ShowStatus(string.Format(_language.LoadedEmptyOrShort, _fileName));
                        _sourceViewChange = false;
                        _converted = false;

                        MessageBox.Show(_language.FileIsEmptyOrShort);
                    }
                    else
                        ShowUnknownSubtitle();
                }

                if (!videoFileLoaded && mediaPlayer.VideoPlayer != null)
                {
                    mediaPlayer.VideoPlayer.DisposeVideoPlayer();
                    mediaPlayer.VideoPlayer = null;
                    timer1.Stop();
                }
            }
            else
            {
                MessageBox.Show(string.Format(_language.FileNotFound, fileName));
            }
        }

        private bool IsTransportStream(string fileName)
        {
            try
            {
                var buffer = new byte[1];
                var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
                return buffer[0] == 0x47; // 47hex (71 dec) == TS sync byte
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private void SetUndockedWindowsTitle()
        {
            string title = Configuration.Settings.Language.General.NoVideoLoaded;
            if (!string.IsNullOrEmpty(_videoFileName))
                title = Path.GetFileNameWithoutExtension(_videoFileName);

            if (_videoControlsUnDocked != null && !_videoControlsUnDocked.IsDisposed)
                _videoControlsUnDocked.Text = string.Format(Configuration.Settings.Language.General.ControlsWindowTitle, title);

            if (_videoPlayerUnDocked != null && !_videoPlayerUnDocked.IsDisposed)
                _videoPlayerUnDocked.Text = string.Format(Configuration.Settings.Language.General.VideoWindowTitle, title);

            if (_waveFormUnDocked != null && !_waveFormUnDocked.IsDisposed)
                _waveFormUnDocked.Text = string.Format(Configuration.Settings.Language.General.AudioWindowTitle, title);
        }

        private void ImportAndOcrBdnXml(string fileName, BdnXml bdnXml, List<string> list)
        {
            Subtitle bdnSubtitle = new Subtitle();
            bdnXml.LoadSubtitle(bdnSubtitle, list, fileName);
            bdnSubtitle.FileName = fileName;
            var formSubOcr = new VobSubOcr();
            formSubOcr.Initialize(bdnSubtitle, Configuration.Settings.VobSubOcr, false);
            if (formSubOcr.ShowDialog(this) == DialogResult.OK)
            {
                MakeHistoryForUndo(_language.BeforeImportingBdnXml);
                FileNew();
                _subtitle.Paragraphs.Clear();
                SetCurrentFormat(new SubRip().FriendlyName);
                _subtitle.WasLoadedWithFrameNumbers = false;
                _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                {
                    _subtitle.Paragraphs.Add(p);
                }

                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                _subtitleListViewIndex = -1;
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0);

                _fileName = Path.ChangeExtension(formSubOcr.FileName, ".srt");
                SetTitle();
                _converted = true;
            }
        }

        private void ImportAndOcrSon(string fileName, Son format, List<string> list)
        {
            Subtitle sub = new Subtitle();
            format.LoadSubtitle(sub, list, fileName);
            sub.FileName = fileName;
            var formSubOcr = new VobSubOcr();
            formSubOcr.Initialize(sub, Configuration.Settings.VobSubOcr, true);
            if (formSubOcr.ShowDialog(this) == DialogResult.OK)
            {
                MakeHistoryForUndo(_language.BeforeImportingBdnXml);
                FileNew();
                _subtitle.Paragraphs.Clear();
                SetCurrentFormat(new SubRip().FriendlyName);
                _subtitle.WasLoadedWithFrameNumbers = false;
                _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                {
                    _subtitle.Paragraphs.Add(p);
                }

                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                _subtitleListViewIndex = -1;
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0);

                _fileName = Path.ChangeExtension(formSubOcr.FileName, ".srt");
                SetTitle();
                _converted = true;
            }
        }

        private void ImportAndOcrSst(string fileName, SonicScenaristBitmaps format, List<string> list)
        {
            Subtitle sub = new Subtitle();
            format.LoadSubtitle(sub, list, fileName);
            sub.FileName = fileName;
            var formSubOcr = new VobSubOcr();
            formSubOcr.Initialize(sub, Configuration.Settings.VobSubOcr, true);
            if (formSubOcr.ShowDialog(this) == DialogResult.OK)
            {
                MakeHistoryForUndo(_language.BeforeImportingBdnXml);
                FileNew();
                _subtitle.Paragraphs.Clear();
                SetCurrentFormat(new SubRip().FriendlyName);
                _subtitle.WasLoadedWithFrameNumbers = false;
                _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                {
                    _subtitle.Paragraphs.Add(p);
                }

                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                _subtitleListViewIndex = -1;
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0);

                _fileName = Path.ChangeExtension(formSubOcr.FileName, ".srt");
                SetTitle();
                _converted = true;
            }
        }

        private void ShowUnknownSubtitle()
        {
            var unknownSubtitle = new UnknownSubtitle();
            unknownSubtitle.Initialize(Title);
            unknownSubtitle.ShowDialog(this);
        }

        private void UpdateRecentFilesUI()
        {
            reopenToolStripMenuItem.DropDownItems.Clear();
            if (Configuration.Settings.General.ShowRecentFiles &&
                Configuration.Settings.RecentFiles.Files.Count > 0)
            {
                reopenToolStripMenuItem.Visible = true;
                foreach (var file in Configuration.Settings.RecentFiles.Files)
                {
                    if (File.Exists(file.FileName))
                        reopenToolStripMenuItem.DropDownItems.Add(file.FileName, null, ReopenSubtitleToolStripMenuItemClick);
                }
            }
            else
            {
                Configuration.Settings.RecentFiles.Files.Clear();
                reopenToolStripMenuItem.Visible = false;
            }
        }

        private void ReopenSubtitleToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            var item = sender as ToolStripItem;

            if (ContinueNewOrExit())
            {
                RecentFileEntry rfe = null;
                foreach (var file in Configuration.Settings.RecentFiles.Files)
                {
                    if (file.FileName == item.Text)
                        rfe = file;
                }

                if (rfe == null)
                    OpenSubtitle(item.Text, null);
                else
                    OpenSubtitle(rfe.FileName, null, rfe.VideoFileName, rfe.OriginalFileName);
                SetRecentIndecies(item.Text);
                GotoSubPosAndPause();
            }
        }

        private void GotoSubPosAndPause()
        {
            if (!string.IsNullOrEmpty(_videoFileName))
            {
                _videoLoadedGoToSubPosAndPause = true;
            }
            else
            {
                mediaPlayer.SubtitleText = string.Empty;
            }
        }

        private void SetRecentIndecies(string fileName)
        {
            if (!Configuration.Settings.General.RememberSelectedLine)
                return;

            foreach (var x in Configuration.Settings.RecentFiles.Files)
            {
                if (string.Compare(fileName, x.FileName, true) == 0)
                {
                    int sIndex = x.FirstSelectedIndex;
                    if (sIndex >= 0 && sIndex < SubtitleListview1.Items.Count)
                    {
                        SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                        for (int i = 0; i < SubtitleListview1.Items.Count; i++)
                            SubtitleListview1.Items[i].Selected = i == sIndex;
                        _subtitleListViewIndex = -1;
                        SubtitleListview1.EnsureVisible(sIndex);
                        SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                        SubtitleListview1.Items[sIndex].Focused = true;
                    }

                    int topIndex = x.FirstVisibleIndex;
                    if (topIndex >= 0 && topIndex < SubtitleListview1.Items.Count)
                    {
                        // to fix bug in .net framework we have to set topitem 3 times... wtf!?
                        SubtitleListview1.TopItem = SubtitleListview1.Items[topIndex];
                        SubtitleListview1.TopItem = SubtitleListview1.Items[topIndex];
                        SubtitleListview1.TopItem = SubtitleListview1.Items[topIndex];
                    }

                    RefreshSelectedParagraph();
                    break;
                }
            }
        }

        private void SaveToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            SaveSubtitle(GetCurrentSubtitleFormat());
        }

        private void SaveAsToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            FileSaveAs();
        }

        private DialogResult FileSaveAs()
        {
            SubtitleFormat currentFormat = GetCurrentSubtitleFormat();
            Utilities.SetSaveDialogFilter(saveFileDialog1, currentFormat);

            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + currentFormat.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                _converted = false;
                _fileName = saveFileDialog1.FileName;

                _fileDateTime = File.GetLastWriteTime(_fileName);
                SetTitle();
                Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
                Configuration.Settings.Save();

                int index = 0;
                foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
                {
                    if (saveFileDialog1.FilterIndex == index + 1)
                    {
                        // only allow current extension or ".txt"
                        string ext = Path.GetExtension(_fileName).ToLower();
                        bool extOk = ext == format.Extension.ToLower() || format.AlternateExtensions.Contains(ext) || ext == ".txt";
                        if (!extOk)
                        {
                            if (_fileName.EndsWith("."))
                                _fileName = _fileName.Substring(0, _fileName.Length - 1);
                            _fileName += format.Extension;
                        }

                        if (SaveSubtitle(format) == DialogResult.OK)
                            SetCurrentFormat(format);
                    }
                    index++;
                }
            }
            return result;
        }

        private DialogResult SaveSubtitle(SubtitleFormat format)
        {
            if (string.IsNullOrEmpty(_fileName) || _converted)
                return FileSaveAs();

            try
            {
                string allText = _subtitle.ToText(format);
                var currentEncoding = GetCurrentEncoding();
                if (currentEncoding == Encoding.Default && (allText.Contains("♪") || allText.Contains("♫") | allText.Contains("♥"))) // ANSI & music/unicode symbols
                {
                    if (MessageBox.Show(string.Format(_language.UnicodeMusicSymbolsAnsiWarning), Title, MessageBoxButtons.YesNo) == DialogResult.No)
                        return DialogResult.No;
                }


                bool containsNegativeTime = false;
                foreach (var p in _subtitle.Paragraphs)
                {
                    if (p.StartTime.TotalMilliseconds < 0 || p.EndTime.TotalMilliseconds < 0)
                    {
                        containsNegativeTime = true;
                        break;
                    }
                }
                if (containsNegativeTime && !string.IsNullOrEmpty(_language.NegativeTimeWarning))
                {
                    if (MessageBox.Show(_language.NegativeTimeWarning, Title, MessageBoxButtons.YesNo) == DialogResult.No)
                        return DialogResult.No;
                }

                if (File.Exists(_fileName))
                {
                    DateTime fileOnDisk = File.GetLastWriteTime(_fileName);
                    if (_fileDateTime != fileOnDisk && _fileDateTime != new DateTime())
                    {
                        if (MessageBox.Show(string.Format(_language.OverwriteModifiedFile,
                                                          _fileName, fileOnDisk.ToShortDateString(), fileOnDisk.ToString("HH:mm:ss"),
                                                          Environment.NewLine, _fileDateTime.ToShortDateString(), _fileDateTime.ToString("HH:mm:ss")),
                                             Title + " - " + _language.FileOnDiskModified, MessageBoxButtons.YesNo) == DialogResult.No)
                            return DialogResult.No;
                    }
                    File.Delete(_fileName);
                }

                if (Control.ModifierKeys == (Keys.Control | Keys.Shift))
                    allText = allText.Replace("\r\n", "\n");
                File.WriteAllText(_fileName, allText, currentEncoding);
                _fileDateTime = File.GetLastWriteTime(_fileName);
                ShowStatus(string.Format(_language.SavedSubtitleX, _fileName));
                _changeSubtitleToString = _subtitle.ToText(new SubRip()).Trim();
                return DialogResult.OK;
            }
            catch (Exception exception)
            {
                MessageBox.Show(string.Format(_language.UnableToSaveSubtitleX, _fileName));
                System.Diagnostics.Debug.Write(exception.Message);
                return DialogResult.Cancel;
            }
        }

        private DialogResult SaveOriginalSubtitle(SubtitleFormat format)
        {
            try
            {
                string allText = _subtitleAlternate.ToText(format).Trim();
                var currentEncoding = GetCurrentEncoding();
                if (currentEncoding == Encoding.Default && (allText.Contains("♪") || allText.Contains("♫") | allText.Contains("♥"))) // ANSI & music/unicode symbols
                {
                    if (MessageBox.Show(string.Format(_language.UnicodeMusicSymbolsAnsiWarning), Title, MessageBoxButtons.YesNo) == DialogResult.No)
                        return DialogResult.No;
                }

                bool containsNegativeTime = false;
                foreach (var p in _subtitleAlternate.Paragraphs)
                {
                    if (p.StartTime.TotalMilliseconds < 0 || p.EndTime.TotalMilliseconds < 0)
                    {
                        containsNegativeTime = true;
                        break;
                    }
                }
                if (containsNegativeTime && !string.IsNullOrEmpty(_language.NegativeTimeWarning))
                {
                    if (MessageBox.Show(_language.NegativeTimeWarning, Title, MessageBoxButtons.YesNo) == DialogResult.No)
                        return DialogResult.No;
                }

                File.WriteAllText(_subtitleAlternateFileName, allText, currentEncoding);
                ShowStatus(string.Format(_language.SavedOriginalSubtitleX, _subtitleAlternateFileName));
                _changeAlternateSubtitleToString = _subtitleAlternate.ToText(new SubRip()).Trim();
                return DialogResult.OK;
            }
            catch
            {
                MessageBox.Show(string.Format(_language.UnableToSaveSubtitleX, _fileName));
                return DialogResult.Cancel;
            }
        }

        private void NewToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            FileNew();
        }

        private void ResetSubtitle()
        {
            SetCurrentFormat(new SubRip());
            _subtitle = new Subtitle(_subtitle.HistoryItems);
            _subtitleAlternate = new Subtitle();
            _subtitleAlternateFileName = null;
            textBoxSource.Text = string.Empty;
            SubtitleListview1.Items.Clear();
            _fileName = string.Empty;
            _fileDateTime = new DateTime();
            Text = Title;
            _oldSubtitleFormat = null;
            labelSingleLine.Text = string.Empty;
            RemoveAlternate(true);

            SubtitleListview1.HideExtraColumn();
            SubtitleListview1.DisplayExtraFromExtra = false;

            toolStripComboBoxFrameRate.Text = Configuration.Settings.General.DefaultFrameRate.ToString();

            SetEncoding(Configuration.Settings.General.DefaultEncoding);

            toolStripComboBoxFrameRate.Text = Configuration.Settings.General.DefaultFrameRate.ToString();
            _findHelper = null;
            _spellCheckForm = null;
            _videoFileName = null;
            _videoInfo = null;
            _videoAudioTrackNumber = -1;
            labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
            audioVisualizer.WavePeaks = null;
            audioVisualizer.ResetSpectrogram();
            audioVisualizer.Invalidate();

            ShowStatus(_language.New);
            _sourceViewChange = false;

            _subtitleListViewIndex = -1;
            textBoxListViewText.Text = string.Empty;
            textBoxListViewTextAlternate.Text = string.Empty;
            textBoxListViewText.Enabled = false;
            labelTextLineLengths.Text = string.Empty;
            labelCharactersPerSecond.Text = string.Empty;
            labelTextLineTotal.Text = string.Empty;

            _listViewTextUndoLast = null;
            _listViewAlternateTextUndoLast = null;
            _listViewTextUndoIndex = -1;

            if (mediaPlayer.VideoPlayer != null)
            {
                mediaPlayer.VideoPlayer.DisposeVideoPlayer();
                mediaPlayer.VideoPlayer = null;
            }

            _changeSubtitleToString = _subtitle.ToText(new SubRip()).Trim();
            _converted = false;

            SetUndockedWindowsTitle();
            if (mediaPlayer != null)
                mediaPlayer.SubtitleText = string.Empty;
        }

        private void FileNew()
        {
            if (ContinueNewOrExit())
            {
                MakeHistoryForUndo(_language.BeforeNew);
                ResetSubtitle();
            }
        }

        private void ComboBoxSubtitleFormatsSelectedIndexChanged(object sender, EventArgs e)
        {
            _converted = true;
            if (_oldSubtitleFormat == null)
            {
                if (!_loading)
                    MakeHistoryForUndo(string.Format(_language.BeforeConvertingToX, GetCurrentSubtitleFormat().FriendlyName));
            }
            else
            {
                _subtitle.MakeHistoryForUndo(string.Format(_language.BeforeConvertingToX, GetCurrentSubtitleFormat().FriendlyName), _oldSubtitleFormat, _fileDateTime, _subtitleAlternate, _subtitleAlternateFileName, _subtitleListViewIndex, textBoxListViewText.SelectionStart, textBoxListViewTextAlternate.SelectionStart);
                _oldSubtitleFormat.RemoveNativeFormatting(_subtitle);
                SaveSubtitleListviewIndexes();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();

                if (_oldSubtitleFormat.HasStyleSupport && _networkSession == null)
                {
                    SubtitleListview1.HideExtraColumn();
                }
            }
            SubtitleFormat format = GetCurrentSubtitleFormat();
            if (_oldSubtitleFormat != null && !_oldSubtitleFormat.IsFrameBased && format.IsFrameBased)
                _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
            else if (_oldSubtitleFormat != null && _oldSubtitleFormat.IsFrameBased && !format.IsFrameBased)
                _subtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
            ShowSource();
            SubtitleListview1.DisplayExtraFromExtra = false;
            if (format != null)
            {
                ShowStatus(string.Format(_language.ConvertedToX, format.FriendlyName));
                _oldSubtitleFormat = format;

                if (format.HasStyleSupport && _networkSession == null)
                {
                    List<string> styles = new List<string>();
                    if (format.GetType() == typeof(AdvancedSubStationAlpha) || format.GetType() == typeof(SubStationAlpha))
                        styles = AdvancedSubStationAlpha.GetStylesFromHeader(_subtitle.Header);
                    else if (format.GetType() == typeof(TimedText10))
                        styles = TimedText10.GetStylesFromHeader(_subtitle.Header);
                    else if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                        styles = Sami.GetStylesFromHeader(_subtitle.Header);
                    foreach (Paragraph p in _subtitle.Paragraphs)
                    {
                        if (string.IsNullOrEmpty(p.Extra) && styles.Count > 0)
                            p.Extra = styles[0];
                    }
                    if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                        SubtitleListview1.ShowExtraColumn(Configuration.Settings.Language.General.Class);
                    else
                        SubtitleListview1.ShowExtraColumn(Configuration.Settings.Language.General.Style);
                    SubtitleListview1.DisplayExtraFromExtra = true;
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                }
            }
        }

        private void ComboBoxSubtitleFormatsEnter(object sender, EventArgs e)
        {
            SubtitleFormat format = GetCurrentSubtitleFormat();
            if (format != null)
                _oldSubtitleFormat = format;
        }

        private SubtitleFormat GetCurrentSubtitleFormat()
        {
            return Utilities.GetSubtitleFormatByFriendlyName(comboBoxSubtitleFormats.SelectedItem.ToString());
        }

        private void ShowSource()
        {
            if (_subtitle != null && _subtitle.Paragraphs.Count > 0)
            {
                SubtitleFormat format = GetCurrentSubtitleFormat();
                if (format != null)
                {
                    if (GetCurrentSubtitleFormat().IsFrameBased)
                        _subtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                    else
                        _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

                    textBoxSource.TextChanged -= TextBoxSourceTextChanged;
                    textBoxSource.Text = _subtitle.ToText(format);
                    textBoxSource.TextChanged += TextBoxSourceTextChanged;
                    return;
                }
            }
            textBoxSource.TextChanged -= TextBoxSourceTextChanged;
            textBoxSource.Text = string.Empty;
            textBoxSource.TextChanged += TextBoxSourceTextChanged;
        }

        private void SettingsToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowSettings();
        }

        private void ShowSettings()
        {
            string oldVideoPlayer = Configuration.Settings.General.VideoPlayer;
            string oldListViewLineSeparatorString = Configuration.Settings.General.ListViewLineSeparatorString;
            string oldSubtitleFontSettings = Configuration.Settings.General.SubtitleFontName +
                                          Configuration.Settings.General.SubtitleFontBold +
                                          Configuration.Settings.General.CenterSubtitleInTextBox +
                                          Configuration.Settings.General.SubtitleFontSize +
                                          Configuration.Settings.General.SubtitleFontColor.ToArgb().ToString() +
                                          Configuration.Settings.General.SubtitleBackgroundColor.ToArgb().ToString();
            bool oldUseTimeFormatHHMMSSFF = Configuration.Settings.General.UseTimeFormatHHMMSSFF;

            string oldSyntaxColoring = Configuration.Settings.Tools.ListViewSyntaxColorDurationSmall.ToString() +
                                       Configuration.Settings.Tools.ListViewSyntaxColorDurationBig.ToString() +
                                       Configuration.Settings.Tools.ListViewSyntaxColorLongLines.ToString() +
                                       Configuration.Settings.Tools.ListViewSyntaxColorOverlap.ToString() +
                                       Configuration.Settings.Tools.ListViewSyntaxMoreThanXLines.ToString() +
                                       Configuration.Settings.Tools.ListViewSyntaxMoreThanXLinesX.ToString() +
                                       Configuration.Settings.Tools.ListViewSyntaxErrorColor.ToArgb().ToString();

            var oldAllowEditOfOriginalSubtitle = Configuration.Settings.General.AllowEditOfOriginalSubtitle;
            var settings = new Settings();
            settings.Initialize(this.Icon, toolStripButtonFileNew.Image, toolStripButtonFileOpen.Image, toolStripButtonSave.Image, toolStripButtonSaveAs.Image,
                                toolStripButtonFind.Image, toolStripButtonReplace.Image, toolStripButtonVisualSync.Image, toolStripButtonSpellCheck.Image, toolStripButtonSettings.Image, toolStripButtonHelp.Image);
            _formPositionsAndSizes.SetPositionAndSize(settings);
            settings.ShowDialog(this);
            _formPositionsAndSizes.SavePositionAndSize(settings);
            settings.Dispose();

            InitializeToolbar();
            UpdateRecentFilesUI();
            Utilities.InitializeSubtitleFont(textBoxSource);
            Utilities.InitializeSubtitleFont(textBoxListViewText);
            Utilities.InitializeSubtitleFont(textBoxListViewTextAlternate);
            Utilities.InitializeSubtitleFont(SubtitleListview1);
            buttonCustomUrl1.Text = Configuration.Settings.VideoControls.CustomSearchText1;
            buttonCustomUrl1.Visible = Configuration.Settings.VideoControls.CustomSearchUrl1.Length > 1;
            buttonCustomUrl2.Text = Configuration.Settings.VideoControls.CustomSearchText2;
            buttonCustomUrl2.Visible = Configuration.Settings.VideoControls.CustomSearchUrl2.Length > 1;

            audioVisualizer.DrawGridLines = Configuration.Settings.VideoControls.WaveFormDrawGrid;
            audioVisualizer.GridColor = Configuration.Settings.VideoControls.WaveFormGridColor;
            audioVisualizer.SelectedColor = Configuration.Settings.VideoControls.WaveFormSelectedColor;
            audioVisualizer.Color = Configuration.Settings.VideoControls.WaveFormColor;
            audioVisualizer.BackgroundColor = Configuration.Settings.VideoControls.WaveFormBackgroundColor;
            audioVisualizer.TextColor =  Configuration.Settings.VideoControls.WaveFormTextColor;
            audioVisualizer.MouseWheelScrollUpIsForward = Configuration.Settings.VideoControls.WaveFormMouseWheelScrollUpIsForward;

            string newSyntaxColoring = Configuration.Settings.Tools.ListViewSyntaxColorDurationSmall.ToString() +
                           Configuration.Settings.Tools.ListViewSyntaxColorDurationBig.ToString() +
                           Configuration.Settings.Tools.ListViewSyntaxColorLongLines.ToString() +
                           Configuration.Settings.Tools.ListViewSyntaxColorOverlap.ToString() +
                           Configuration.Settings.Tools.ListViewSyntaxMoreThanXLines.ToString() +
                           Configuration.Settings.Tools.ListViewSyntaxMoreThanXLinesX.ToString() +
                           Configuration.Settings.Tools.ListViewSyntaxErrorColor.ToArgb().ToString();


            if (oldSubtitleFontSettings != Configuration.Settings.General.SubtitleFontName +
                                          Configuration.Settings.General.SubtitleFontBold +
                                          Configuration.Settings.General.CenterSubtitleInTextBox +
                                          Configuration.Settings.General.SubtitleFontSize +
                                          Configuration.Settings.General.SubtitleFontColor.ToArgb().ToString() +
                                          Configuration.Settings.General.SubtitleBackgroundColor.ToArgb().ToString() ||
                oldSyntaxColoring != newSyntaxColoring)
            {
                Utilities.InitializeSubtitleFont(textBoxListViewText);
                Utilities.InitializeSubtitleFont(textBoxListViewTextAlternate);
                Utilities.InitializeSubtitleFont(textBoxSource);
                SubtitleListview1.SubtitleFontName = Configuration.Settings.General.SubtitleFontName;
                SubtitleListview1.SubtitleFontBold = Configuration.Settings.General.SubtitleFontBold;
                SubtitleListview1.SubtitleFontSize = Configuration.Settings.General.SubtitleFontSize;
                SubtitleListview1.ForeColor = Configuration.Settings.General.SubtitleFontColor;
                SubtitleListview1.BackColor = Configuration.Settings.General.SubtitleBackgroundColor;
                if (Configuration.Settings.General.CenterSubtitleInTextBox)
                    textBoxListViewText.TextAlign = HorizontalAlignment.Center;
                else if (textBoxListViewText.TextAlign == HorizontalAlignment.Center)
                    textBoxListViewText.TextAlign = HorizontalAlignment.Left;

                SaveSubtitleListviewIndexes();
                Utilities.InitializeSubtitleFont(SubtitleListview1);
                SubtitleListview1.AutoSizeAllColumns(this);
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();
                mediaPlayer.SetSubtitleFont();
                ShowSubtitle();
            }
            mediaPlayer.SetSubtitleFont();
            mediaPlayer.ShowStopButton = Configuration.Settings.General.VideoPlayerShowStopButton;
            mediaPlayer.ShowMuteButton = Configuration.Settings.General.VideoPlayerShowMuteButton;
            mediaPlayer.ShowFullscreenButton = Configuration.Settings.General.VideoPlayerShowFullscreenButton;

            if (oldListViewLineSeparatorString != Configuration.Settings.General.ListViewLineSeparatorString)
            {
                SubtitleListview1.InitializeLanguage(_languageGeneral, Configuration.Settings);
                SaveSubtitleListviewIndexes();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();
            }

            if (oldAllowEditOfOriginalSubtitle != Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                if (Configuration.Settings.General.AllowEditOfOriginalSubtitle)
                {
                    buttonUnBreak.Visible = false;
                    buttonAutoBreak.Visible = false;
                    buttonSplitLine.Visible = false;
                    textBoxListViewTextAlternate.Visible = true;
                    labelAlternateText.Visible = true;
                    labelAlternateCharactersPerSecond.Visible = true;
                    labelTextAlternateLineLengths.Visible = true;
                    labelAlternateSingleLine.Visible = true;
                    labelTextAlternateLineTotal.Visible = true;
                }
                else
                {
                    RemoveAlternate(false);
                }
                Main_Resize(null, null);
            }
            textBoxListViewTextAlternate.Enabled = Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleListViewIndex >= 0;

            SetShortcuts();

            _timerAutoSave.Stop();
            if (!string.IsNullOrEmpty(_videoFileName) && oldVideoPlayer != Configuration.Settings.General.VideoPlayer && mediaPlayer.VideoPlayer != null)
            {
                string vfn = _videoFileName;
                CloseVideoToolStripMenuItemClick(null, null);
                OpenVideo(vfn);
            }

            if (Configuration.Settings.General.AutoBackupSeconds > 0)
            {
                _timerAutoSave.Interval = 1000 * Configuration.Settings.General.AutoBackupSeconds; // take backup every x second if changes were made
                _timerAutoSave.Start();
            }
            SetTitle();
            if (Configuration.Settings.VideoControls.GenerateSpectrogram)
            {
                audioVisualizer.WaveFormNotLoadedText = Configuration.Settings.Language.WaveForm.ClickToAddWaveformAndSpectrogram;
                showhideWaveFormToolStripMenuItem.Text = _language.Menu.Video.ShowHideWaveformAndSpectrogram;
            }
            else
            {
                audioVisualizer.WaveFormNotLoadedText = Configuration.Settings.Language.WaveForm.ClickToAddWaveForm;
                showhideWaveFormToolStripMenuItem.Text = _language.Menu.Video.ShowHideWaveForm;
            }
            audioVisualizer.Invalidate();

            if (oldUseTimeFormatHHMMSSFF != Configuration.Settings.General.UseTimeFormatHHMMSSFF)
                RefreshTimeCodeMode();
        }

        private int ShowSubtitle()
        {
            if (SubtitleListview1.IsAlternateTextColumnVisible && Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable)
                return Utilities.ShowSubtitle(_subtitleAlternate.Paragraphs, mediaPlayer);
            return Utilities.ShowSubtitle(_subtitle.Paragraphs, mediaPlayer);
        }

        private void TryLoadIcon(ToolStripButton button, string iconName)
        {
            string fullPath = Configuration.IconsFolder + iconName + ".png";
            if (File.Exists(fullPath))
                button.Image = new Bitmap(fullPath);
        }

        private void InitializeToolbar()
        {
            GeneralSettings gs = Configuration.Settings.General;

            TryLoadIcon(toolStripButtonFileNew, "New");
            TryLoadIcon(toolStripButtonFileOpen, "Open");
            TryLoadIcon(toolStripButtonSave, "Save");
            TryLoadIcon(toolStripButtonSaveAs, "SaveAs");
            TryLoadIcon(toolStripButtonFind, "Find");
            TryLoadIcon(toolStripButtonReplace, "Replace");
            TryLoadIcon(toolStripButtonVisualSync, "VisualSync");
            TryLoadIcon(toolStripButtonSettings, "Settings");
            TryLoadIcon(toolStripButtonSpellCheck, "SpellCheck");
            TryLoadIcon(toolStripButtonHelp, "Help");

            TryLoadIcon(toolStripButtonToggleVideo, "VideoToggle");
            TryLoadIcon(toolStripButtonToggleWaveForm, "WaveFormToggle");

            toolStripButtonFileNew.Visible = gs.ShowToolbarNew;
            toolStripButtonFileOpen.Visible = gs.ShowToolbarOpen;
            toolStripButtonSave.Visible = gs.ShowToolbarSave;
            toolStripButtonSaveAs.Visible = gs.ShowToolbarSaveAs;
            toolStripButtonFind.Visible = gs.ShowToolbarFind;
            toolStripButtonReplace.Visible = gs.ShowToolbarReplace;
            toolStripButtonVisualSync.Visible = gs.ShowToolbarVisualSync;
            toolStripButtonSettings.Visible = gs.ShowToolbarSettings;
            toolStripButtonSpellCheck.Visible = gs.ShowToolbarSpellCheck;
            toolStripButtonHelp.Visible = gs.ShowToolbarHelp;

            toolStripSeparatorFrameRate.Visible = gs.ShowFrameRate;
            toolStripLabelFrameRate.Visible = gs.ShowFrameRate;
            toolStripComboBoxFrameRate.Visible = gs.ShowFrameRate;
            toolStripButtonGetFrameRate.Visible = gs.ShowFrameRate;

            toolStripSeparatorFindReplace.Visible = gs.ShowToolbarFind || gs.ShowToolbarReplace;
            toolStripSeparatorHelp.Visible = gs.ShowToolbarHelp;

            toolStrip1.Visible = gs.ShowToolbarNew || gs.ShowToolbarOpen || gs.ShowToolbarSave || gs.ShowToolbarSaveAs || gs.ShowToolbarFind || gs.ShowToolbarReplace ||
                                 gs.ShowToolbarVisualSync || gs.ShowToolbarSettings || gs.ShowToolbarSpellCheck || gs.ShowToolbarHelp;
        }

        private void ToolStripButtonFileNewClick(object sender, EventArgs e)
        {
            _lastDoNotPrompt = string.Empty;
            ReloadFromSourceView();
            FileNew();
        }

        private void ToolStripButtonFileOpenClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            OpenNewFile();
        }

        private void ToolStripButtonSaveClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            bool oldChange = _changeSubtitleToString != _subtitle.ToText(new SubRip()).Trim();
            SaveSubtitle(GetCurrentSubtitleFormat());

            if (_subtitleAlternate != null && _changeAlternateSubtitleToString != _subtitleAlternate.ToText(new SubRip()).Trim() && Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate.Paragraphs.Count > 0)
            {
                SaveOriginalToolStripMenuItemClick(null, null);
                if (oldChange && _changeSubtitleToString == _subtitle.ToText(new SubRip()).Trim() && _changeAlternateSubtitleToString == _subtitleAlternate.ToText(new SubRip()).Trim())
                    ShowStatus(string.Format(_language.SavedSubtitleX, Path.GetFileName(_fileName)) + " + " +
                        string.Format(_language.SavedOriginalSubtitleX, Path.GetFileName(_subtitleAlternateFileName)));
            }
        }

        private void ToolStripButtonSaveAsClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            FileSaveAs();
        }

        private void ToolStripButtonFindClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Find();
        }

        private void ToolStripButtonVisualSyncClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            ShowVisualSync(false);
        }

        private void ToolStripButtonSettingsClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            ShowSettings();
        }

        private void TextBoxSourceClick(object sender, EventArgs e)
        {
            ShowSourceLineNumber();
        }

        private void TextBoxSourceKeyDown(object sender, KeyEventArgs e)
        {
            ShowSourceLineNumber();
            e.Handled = false;

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
            {
                textBoxSource.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.D)
            {
                textBoxSource.SelectionLength = 0;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBoxSourceTextChanged(object sender, EventArgs e)
        {
            ShowSourceLineNumber();
            _sourceViewChange = true;
            labelStatus.Text = string.Empty;
        }


        private void ShowSourceLineNumber()
        {
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
            {
                string number = textBoxSource.GetLineFromCharIndex(textBoxSource.SelectionStart).ToString();
                if (number.Length > 0)
                    toolStripSelected.Text = string.Format(_language.LineNumberX, int.Parse(number) + 1);
                else
                    toolStripSelected.Text = string.Empty;
            }
        }

        private void ButtonGetFrameRateClick(object sender, EventArgs e)
        {
            openFileDialog1.Title = _language.OpenVideoFile;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetVideoFileFilter();
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                _videoFileName = openFileDialog1.FileName;
                VideoInfo info = Utilities.GetVideoInfo(openFileDialog1.FileName, delegate { Application.DoEvents(); });
                if (info != null && info.Success)
                {
                    string oldFrameRate = toolStripComboBoxFrameRate.Text;
                    toolStripComboBoxFrameRate.Text = string.Format("{0:0.###}", info.FramesPerSecond);

                    if (oldFrameRate != toolStripComboBoxFrameRate.Text)
                    {
                        ShowSource();
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);

                        SubtitleFormat format = Utilities.GetSubtitleFormatByFriendlyName(comboBoxSubtitleFormats.SelectedItem.ToString());
                        if (_subtitle.WasLoadedWithFrameNumbers && format.IsTimeBased)
                        {
                            MessageBox.Show(string.Format(_language.NewFrameRateUsedToCalculateTimeCodes, info.FramesPerSecond));
                        }
                        else if (!_subtitle.WasLoadedWithFrameNumbers && format.IsFrameBased)
                        {
                            MessageBox.Show(string.Format(_language.NewFrameRateUsedToCalculateFrameNumbers, info.FramesPerSecond));
                        }
                    }
                }
            }
        }

        private void FindToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Find();
        }

        private void Find()
        {
            string selectedText;
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                selectedText = textBoxSource.SelectedText;
            else
                selectedText = textBoxListViewText.SelectedText;

            if (selectedText.Length == 0 && _findHelper != null)
                selectedText = _findHelper.FindText;

            var findDialog = new FindDialog();
            findDialog.SetIcon(toolStripButtonFind.Image as Bitmap);
            findDialog.Initialize(selectedText, _findHelper);
            if (findDialog.ShowDialog(this) == DialogResult.OK)
            {
                _findHelper = findDialog.GetFindDialogHelper(_subtitleListViewIndex);
                ShowStatus(string.Format(_language.SearchingForXFromLineY, _findHelper.FindText, _subtitleListViewIndex +1));
                if (tabControlSubtitle.SelectedIndex == TabControlListView)
                {
                    int selectedIndex = -1;
                    //set the starting selectedIndex if a row is highlighted
                    if (SubtitleListview1.SelectedItems.Count > 0)
                        selectedIndex = SubtitleListview1.SelectedItems[0].Index;

                    //if we fail to find the text, we might want to start searching from the top of the file.
                    bool foundIt = false;
                    if (_findHelper.Find(_subtitle, _subtitleAlternate, selectedIndex))
                    {
                        foundIt = true;
                    }
                    else if (_findHelper.StartLineIndex >= 1)
                    {
                        if (MessageBox.Show(_language.FindContinue, _language.FindContinueTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            selectedIndex = -1;
                            if (_findHelper.Find(_subtitle, _subtitleAlternate, selectedIndex))
                                foundIt = true;
                        }
                    }

                    if (foundIt)
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex);
                        TextBox tb;
                        if (_findHelper.MatchInOriginal)
                            tb = textBoxListViewTextAlternate;
                        else
                            tb = textBoxListViewText;
                        tb.Focus();
                        tb.SelectionStart = _findHelper.SelectedPosition;
                        tb.SelectionLength = _findHelper.FindTextLength;
                        ShowStatus(string.Format(_language.XFoundAtLineNumberY, _findHelper.FindText, _findHelper.SelectedIndex + 1));
                        _findHelper.SelectedPosition++;

                    }
                    else
                    {
                        ShowStatus(string.Format(_language.XNotFound, _findHelper.FindText));
                    }
                }
                else if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                {
                    if (_findHelper.Find(textBoxSource, textBoxSource.SelectionStart))
                    {
                        textBoxSource.SelectionStart = _findHelper.SelectedIndex;
                        textBoxSource.SelectionLength = _findHelper.FindTextLength;
                        textBoxSource.ScrollToCaret();
                        ShowStatus(string.Format(_language.XFoundAtLineNumberY, _findHelper.FindText, textBoxSource.GetLineFromCharIndex(textBoxSource.SelectionStart)));
                    }
                    else
                    {
                        ShowStatus(string.Format(_language.XNotFound, _findHelper.FindText));
                    }
                }
            }
            findDialog.Dispose();
        }

        private void FindNextToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            FindNext();
        }

        private void FindNext()
        {
            if (_findHelper != null)
            {
                if (tabControlSubtitle.SelectedIndex == TabControlListView)
                {
                    int selectedIndex = -1;
                    if (SubtitleListview1.SelectedItems.Count > 0)
                        selectedIndex = SubtitleListview1.SelectedItems[0].Index;
                    if (_findHelper.FindNext(_subtitle, _subtitleAlternate,  selectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex);
                        ShowStatus(string.Format(_language.XFoundAtLineNumberY, _findHelper.FindText, _findHelper.SelectedIndex+1));
                        TextBox tb;
                        if (_findHelper.MatchInOriginal)
                            tb = textBoxListViewTextAlternate;
                        else
                            tb = textBoxListViewText;
                        tb.Focus();
                        tb.SelectionStart = _findHelper.SelectedPosition;
                        tb.SelectionLength = _findHelper.FindTextLength;
                        _findHelper.SelectedPosition++;
                    }
                    else
                    {
                        if (_findHelper.StartLineIndex >= 1)
                        {
                            if (MessageBox.Show(_language.FindContinue, _language.FindContinueTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                _findHelper.StartLineIndex = 0;
                                if (_findHelper.Find(_subtitle, _subtitleAlternate, 0))
                                {
                                    SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex);
                                    TextBox tb;
                                    if (_findHelper.MatchInOriginal)
                                        tb = textBoxListViewTextAlternate;
                                    else
                                        tb = textBoxListViewText;
                                    tb.Focus();
                                    tb.SelectionStart = _findHelper.SelectedPosition;
                                    tb.SelectionLength = _findHelper.FindTextLength;
                                    ShowStatus(string.Format(_language.XFoundAtLineNumberY, _findHelper.FindText, _findHelper.SelectedIndex + 1));
                                    _findHelper.SelectedPosition++;
                                    return;
                                }
                            }
                        }
                        ShowStatus(string.Format(_language.XNotFound, _findHelper.FindText));
                    }
                }
                else if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                {
                    if (_findHelper.FindNext(textBoxSource, textBoxSource.SelectionStart))
                    {
                        textBoxSource.SelectionStart = _findHelper.SelectedIndex;
                        textBoxSource.SelectionLength = _findHelper.FindTextLength;
                        textBoxSource.ScrollToCaret();
                        ShowStatus(string.Format(_language.XFoundAtLineNumberY, _findHelper.FindText, textBoxSource.GetLineFromCharIndex(textBoxSource.SelectionStart)));
                    }
                    else
                    {
                        ShowStatus(string.Format(_language.XNotFound, _findHelper.FindText));
                    }
                }
            }

        }

        private void ToolStripButtonReplaceClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Replace(null);
        }

        private void ReplaceToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Replace(null);
        }

        private void ReplaceSourceView(ReplaceDialog replaceDialog)
        {
            bool isFirst = true;
            string selectedText = textBoxSource.SelectedText;
            if (selectedText.Length == 0 && _findHelper != null)
                selectedText = _findHelper.FindText;

            if (replaceDialog == null)
            {
                replaceDialog = new ReplaceDialog();
                replaceDialog.SetIcon(toolStripButtonReplace.Image as Bitmap);
                if (_findHelper == null)
                {
                    _findHelper = replaceDialog.GetFindDialogHelper(_subtitleListViewIndex);
                    _findHelper.WindowPositionLeft = Left + (Width / 2) - (replaceDialog.Width / 2);
                    _findHelper.WindowPositionTop = Top + (Height / 2) - (replaceDialog.Height / 2);
                }
            }
            else
                isFirst = false;

            replaceDialog.Initialize(selectedText, _findHelper);
            if (replaceDialog.ShowDialog(this) == DialogResult.OK)
            {
                _findHelper = replaceDialog.GetFindDialogHelper(_subtitleListViewIndex);
                ShowStatus(string.Format(_language.SearchingForXFromLineY, _findHelper.FindText, _subtitleListViewIndex + 1));
                int replaceCount = 0;
                bool searchStringFound = true;
                while (searchStringFound)
                {
                    searchStringFound = false;
                    int start = textBoxSource.SelectionStart;
                    if (isFirst)
                    {
                        MakeHistoryForUndo(string.Format(_language.BeforeReplace, _findHelper.FindText));
                        isFirst = false;
                        _makeHistoryPaused = true;
                        if (start >= 0)
                            start--;
                    }
                    if (_findHelper.FindNext(textBoxSource, start))
                    {
                        textBoxSource.SelectionStart = _findHelper.SelectedIndex;
                        textBoxSource.SelectionLength = _findHelper.FindTextLength;
                        if (!replaceDialog.FindOnly)
                            textBoxSource.SelectedText = _findHelper.ReplaceText;
                        textBoxSource.ScrollToCaret();

                        replaceCount++;
                        searchStringFound = true;
                    }
                    if (replaceDialog.FindOnly)
                    {
                        if (searchStringFound)
                            ShowStatus(string.Format(_language.MatchFoundX, _findHelper.FindText));
                        else
                            ShowStatus(string.Format(_language.NoMatchFoundX, _findHelper.FindText));

                        Replace(replaceDialog);
                        return;
                    }
                    if (!replaceDialog.ReplaceAll)
                    {
                        break; // out of while loop
                    }
                }
                ReloadFromSourceView();
                if (replaceCount == 0)
                    ShowStatus(_language.FoundNothingToReplace);
                else
                    ShowStatus(string.Format(_language.ReplaceCountX, replaceCount));
            }
            if (_makeHistoryPaused)
                RestartHistory();
            replaceDialog.Dispose();
        }

        private void ReplaceListView(ReplaceDialog replaceDialog)
        {
            int firstIndex = FirstSelectedIndex;
            bool isFirst = true;
            string selectedText = textBoxListViewText.SelectedText;
            if (selectedText.Length == 0 && _findHelper != null)
                selectedText = _findHelper.FindText;

            if (replaceDialog == null)
            {
                replaceDialog = new ReplaceDialog();
                replaceDialog.SetIcon(toolStripButtonReplace.Image as Bitmap);
                if (_findHelper == null)
                {
                    _findHelper = replaceDialog.GetFindDialogHelper(_subtitleListViewIndex);
                    _findHelper.WindowPositionLeft = Left + (Width / 2) - (replaceDialog.Width / 2);
                    _findHelper.WindowPositionTop = Top + (Height / 2) - (replaceDialog.Height / 2);
                }
                int index = 0;

                if (SubtitleListview1.SelectedItems.Count > 0)
                    index = SubtitleListview1.SelectedItems[0].Index;

                _findHelper.SelectedIndex = index;
                _findHelper.SelectedPosition = index;
                _replaceStartLineIndex = index;
            }
            else
            {
                isFirst = false;
                if (_findHelper != null)
                    selectedText = _findHelper.FindText;
            }
            replaceDialog.Initialize(selectedText, _findHelper);
            if (replaceDialog.ShowDialog(this) == DialogResult.OK)
            {
                if (_findHelper == null)
                {
                    _findHelper = replaceDialog.GetFindDialogHelper(_subtitleListViewIndex);
                }
                else
                {
                    int line = _findHelper.SelectedIndex;
                    int pos = _findHelper.SelectedPosition;
                    bool success = _findHelper.Success;
                    _findHelper = replaceDialog.GetFindDialogHelper(_subtitleListViewIndex);
                    _findHelper.SelectedIndex = line;
                    _findHelper.SelectedPosition = pos;
                    _findHelper.Success = success;
                }
                ShowStatus(string.Format(_language.SearchingForXFromLineY, _findHelper.FindText, _subtitleListViewIndex + 1));
                int replaceCount = 0;
                bool searchStringFound = true;
                while (searchStringFound)
                {
                    searchStringFound = false;
                    if (isFirst)
                    {
                        MakeHistoryForUndo(string.Format(_language.BeforeReplace, _findHelper.FindText));
                        isFirst = false;
                        _makeHistoryPaused = true;
                    }

                    if (replaceDialog.ReplaceAll)
                    {
                        if (_findHelper.FindNext(_subtitle, _subtitleAlternate, _findHelper.SelectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                        {
                            textBoxListViewText.Visible = false;
                            SetTextForFindAndReplace(true);
                            _findHelper.SelectedPosition += _findHelper.ReplaceText.Length;
                            searchStringFound = true;
                            replaceCount++;
                        }
                        else
                        {
                            textBoxListViewText.Visible = true;
                            _subtitleListViewIndex = -1;
                            if (firstIndex >= 0 && firstIndex < SubtitleListview1.Items.Count)
                            {
                                SubtitleListview1.Items[firstIndex].Selected = true;
                                SubtitleListview1.Items[firstIndex].Focused = true;
                                SubtitleListview1.Focus();
                                textBoxListViewText.Text = _subtitle.Paragraphs[firstIndex].Text;
                                if (_subtitleAlternate != null && textBoxListViewTextAlternate.Visible)
                                {
                                    var orginial = Utilities.GetOriginalParagraph(_findHelper.SelectedIndex, _subtitle.Paragraphs[_findHelper.SelectedIndex], _subtitleAlternate.Paragraphs);
                                    if (orginial != null)
                                        textBoxListViewTextAlternate.Text = orginial.Text;
                                }
                            }
                            else
                            {
                                SubtitleListview1.SelectIndexAndEnsureVisible(0, true);
                            }
                            ShowStatus(string.Format(_language.NoMatchFoundX, _findHelper.FindText));

                            if (_replaceStartLineIndex >= 1) // Prompt for start over
                            {
                                _replaceStartLineIndex = 0;
                                if (MessageBox.Show(_language.FindContinue, _language.FindContinueTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    _findHelper.StartLineIndex = 0;
                                    _findHelper.SelectedIndex = 0;
                                    _findHelper.SelectedPosition = 0;
                                    SetTextForFindAndReplace(false);

                                    if (_findHelper.FindNext(_subtitle, _subtitleAlternate, _findHelper.SelectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                                    {
                                        SetTextForFindAndReplace(true);
                                        _findHelper.SelectedPosition += _findHelper.ReplaceText.Length;
                                        searchStringFound = true;
                                        replaceCount++;
                                    }
                                }
                            }
                        }
                    }
                    else if (replaceDialog.FindOnly)
                    {
                        if (_findHelper.FindNext(_subtitle, _subtitleAlternate, _findHelper.SelectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                        {
                            SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex, true);
                            textBoxListViewText.Focus();
                            textBoxListViewText.SelectionStart = _findHelper.SelectedPosition;
                            textBoxListViewText.SelectionLength = _findHelper.FindTextLength;
                            _findHelper.SelectedPosition += _findHelper.FindTextLength;
                            ShowStatus(string.Format(_language.NoXFoundAtLineY, _findHelper.SelectedIndex + 1, _findHelper.FindText));
                            Replace(replaceDialog);
                            return;
                        }
                        else if (_replaceStartLineIndex >= 1) // Prompt for start over
                        {
                            _replaceStartLineIndex = 0;
                            if (MessageBox.Show(_language.FindContinue, _language.FindContinueTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                SubtitleListview1.SelectIndexAndEnsureVisible(0);
                                _findHelper.StartLineIndex = 0;
                                _findHelper.SelectedIndex = 0;
                                _findHelper.SelectedPosition = 0;
                                if (_findHelper.FindNext(_subtitle, _subtitleAlternate, _findHelper.SelectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                                {
                                    SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex);
                                    textBoxListViewText.Focus();
                                    textBoxListViewText.SelectionStart = _findHelper.SelectedPosition;
                                    textBoxListViewText.SelectionLength = _findHelper.FindTextLength;
                                    _findHelper.SelectedPosition += _findHelper.FindTextLength;
                                    ShowStatus(string.Format(_language.NoXFoundAtLineY, _findHelper.SelectedIndex + 1, _findHelper.FindText));
                                    Replace(replaceDialog);
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        ShowStatus(string.Format(_language.NoMatchFoundX, _findHelper.FindText));
                    }
                    else if (!replaceDialog.FindOnly) // replace once only
                    {
                        string msg = string.Empty;
                        if (_findHelper.FindType == FindType.RegEx && _findHelper.Success)
                        {
                            textBoxListViewText.SelectedText = _findHelper.ReplaceText;
                            msg = _language.OneReplacementMade + " ";
                        }
                        else if (textBoxListViewText.SelectionLength == _findHelper.FindTextLength)
                        {
                            textBoxListViewText.SelectedText = _findHelper.ReplaceText;
                            msg = _language.OneReplacementMade + " ";
                        }

                        if (_findHelper.FindNext(_subtitle, _subtitleAlternate, _findHelper.SelectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                        {
                            SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex);
                            if (_findHelper.MatchInOriginal)
                            {
                                textBoxListViewTextAlternate.Focus();
                                textBoxListViewTextAlternate.SelectionStart = _findHelper.SelectedPosition;
                                textBoxListViewTextAlternate.SelectionLength = _findHelper.FindTextLength;
                            }
                            else
                            {
                                textBoxListViewText.Focus();
                                textBoxListViewText.SelectionStart = _findHelper.SelectedPosition;
                                textBoxListViewText.SelectionLength = _findHelper.FindTextLength;
                            }
                            _findHelper.SelectedPosition += _findHelper.ReplaceText.Length;
                            ShowStatus(string.Format(msg + _language.XFoundAtLineNumberY + _language.XFoundAtLineNumberY, _findHelper.SelectedIndex + 1, _findHelper.FindText));
                        }
                        else
                        {
                            ShowStatus(msg + string.Format(_language.XNotFound, _findHelper.FindText));

                            // Prompt for start over
                            if (_replaceStartLineIndex >= 1)
                            {
                                _replaceStartLineIndex = 0;
                                if (MessageBox.Show(_language.FindContinue, _language.FindContinueTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    SubtitleListview1.SelectIndexAndEnsureVisible(0);
                                    _findHelper.StartLineIndex = 0;
                                    _findHelper.SelectedIndex = 0;
                                    _findHelper.SelectedPosition = 0;


                                    if (_findHelper.FindNext(_subtitle, _subtitleAlternate, _findHelper.SelectedIndex, _findHelper.SelectedPosition, Configuration.Settings.General.AllowEditOfOriginalSubtitle))
                                    {
                                        SubtitleListview1.SelectIndexAndEnsureVisible(_findHelper.SelectedIndex, true);
                                        textBoxListViewText.Focus();
                                        textBoxListViewText.SelectionStart = _findHelper.SelectedPosition;
                                        textBoxListViewText.SelectionLength = _findHelper.FindTextLength;
                                        _findHelper.SelectedPosition += _findHelper.ReplaceText.Length;
                                        ShowStatus(string.Format(msg + _language.XFoundAtLineNumberY + _language.XFoundAtLineNumberY, _findHelper.SelectedIndex + 1, _findHelper.FindText));
                                    }

                                }
                                else
                                {
                                    return;
                                }
                            }

                        }
                        Replace(replaceDialog);
                        return;
                    }
                }

                ShowSource();
                if (replaceCount == 0)
                    ShowStatus(_language.FoundNothingToReplace);
                else
                    ShowStatus(string.Format(_language.ReplaceCountX, replaceCount));
            }
            if (_makeHistoryPaused)
                RestartHistory();
            replaceDialog.Dispose();
        }

        private void SetTextForFindAndReplace(bool replace)
        {
            _subtitleListViewIndex = _findHelper.SelectedIndex;
            textBoxListViewText.Text = _subtitle.Paragraphs[_findHelper.SelectedIndex].Text;
            if (_subtitleAlternate != null && textBoxListViewTextAlternate.Visible)
            {
                var orginial = Utilities.GetOriginalParagraph(_findHelper.SelectedIndex, _subtitle.Paragraphs[_findHelper.SelectedIndex], _subtitleAlternate.Paragraphs);
                if (orginial != null)
                    textBoxListViewTextAlternate.Text = orginial.Text;
            }

            if (replace)
            {
                if (_findHelper.MatchInOriginal)
                {
                    textBoxListViewTextAlternate.SelectionStart = _findHelper.SelectedPosition;
                    textBoxListViewTextAlternate.SelectionLength = _findHelper.FindTextLength;
                    textBoxListViewTextAlternate.SelectedText = _findHelper.ReplaceText;
                }
                else
                {
                    textBoxListViewText.SelectionStart = _findHelper.SelectedPosition;
                    textBoxListViewText.SelectionLength = _findHelper.FindTextLength;
                    textBoxListViewText.SelectedText = _findHelper.ReplaceText;
                }
            }
        }

        private void Replace(ReplaceDialog replaceDialog)
        {
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
            {
                ReplaceSourceView(replaceDialog);
            }
            else
            {
                ReplaceListView(replaceDialog);
            }
        }

        public void ShowStatus(string message)
        {
            labelStatus.Text = message;
            statusStrip1.Refresh();
            if (!string.IsNullOrEmpty(message))
            {
                _timerClearStatus.Stop();
                _statusLog.AppendLine(string.Format("{0:0000}-{1:00}-{2:00} {3}: {4}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.ToLongTimeString(), message));
                _timerClearStatus.Start();
            }
        }

        private void ReloadFromSourceView()
        {
            if (_sourceViewChange)
            {
                SaveSubtitleListviewIndexes();
                if (textBoxSource.Text.Trim().Length > 0)
                {
                    Subtitle temp = new Subtitle(_subtitle);
                    SubtitleFormat format = temp.ReloadLoadSubtitle(new List<string>(textBoxSource.Lines), null);
                    if (format == null)
                    {
                        MessageBox.Show(_language.UnableToParseSourceView);
                        return;
                    }
                    else
                    {
                        _sourceViewChange = false;
                        MakeHistoryForUndo(_language.BeforeChangesMadeInSourceView);
                        _subtitle.ReloadLoadSubtitle(new List<string>(textBoxSource.Lines), null);
                        if (format.IsFrameBased)
                            _subtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                        int index = 0;
                        foreach (object obj in comboBoxSubtitleFormats.Items)
                        {
                            if (obj.ToString() == format.FriendlyName)
                                comboBoxSubtitleFormats.SelectedIndex = index;
                            index++;
                        }

                        if (format.GetType() == typeof(AdvancedSubStationAlpha) || format.GetType() == typeof(SubStationAlpha))
                        {
                            string errors = AdvancedSubStationAlpha.CheckForErrors(_subtitle.Header);
                            if (!string.IsNullOrEmpty(errors))
                                MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (format.GetType() == typeof(SubRip))
                        {
                            string errors = (format as SubRip).Errors;
                            if (!string.IsNullOrEmpty(errors))
                                MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (format.GetType() == typeof(MicroDvd))
                        {
                            string errors = (format as MicroDvd).Errors;
                            if (!string.IsNullOrEmpty(errors))
                                MessageBox.Show(errors, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    _sourceViewChange = false;
                    MakeHistoryForUndo(_language.BeforeChangesMadeInSourceView);
                    _sourceViewChange = false;
                    _subtitle.Paragraphs.Clear();
                }
                _subtitleListViewIndex = -1;
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();
            }
        }

        private void HelpToolStripMenuItem1Click(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Utilities.ShowHelp(string.Empty);
        }

        private void ToolStripButtonHelpClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            Utilities.ShowHelp(string.Empty);
        }

        private void GotoLineNumberToolStripMenuItemClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            if (!IsSubtitleLoaded)
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var goToLine = new GoToLine();
            if (tabControlSubtitle.SelectedIndex == TabControlListView)
            {
                goToLine.Initialize(1, SubtitleListview1.Items.Count);
            }
            else if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
            {
                goToLine.Initialize(1, textBoxSource.Lines.Length);
            }
            if (goToLine.ShowDialog(this) == DialogResult.OK)
            {
                if (tabControlSubtitle.SelectedIndex == TabControlListView)
                {
                    SubtitleListview1.SelectNone();

                    SubtitleListview1.Items[goToLine.LineNumber - 1].Selected = true;
                    SubtitleListview1.Items[goToLine.LineNumber - 1].EnsureVisible();
                    SubtitleListview1.Items[goToLine.LineNumber - 1].Focused = true;
                    ShowStatus(string.Format(_language.GoToLineNumberX, goToLine.LineNumber));
                }
                else if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                {
                    // binary search
                    int start = 0;
                    int end = textBoxSource.Text.Length;
                    while (end - start > 10)
                    {
                        int middle = (end - start) / 2;
                        if (goToLine.LineNumber - 1 >= textBoxSource.GetLineFromCharIndex(start + middle))
                            start += middle;
                        else
                            end = start + middle;
                    }

                    // go before line, so we can find first char on line
                    start -= 100;
                    if (start < 0)
                        start = 0;

                    for (int i = start; i <= end; i++)
                    {
                        if (textBoxSource.GetLineFromCharIndex(i) == goToLine.LineNumber - 1)
                        {
                            // select line, scroll to line, and focus...
                            textBoxSource.SelectionStart = i;
                            textBoxSource.SelectionLength = textBoxSource.Lines[goToLine.LineNumber - 1].Length;
                            textBoxSource.ScrollToCaret();
                            ShowStatus(string.Format(_language.GoToLineNumberX, goToLine.LineNumber));
                            if (textBoxSource.CanFocus)
                                textBoxSource.Focus();
                            break;
                        }
                    }

                    ShowSourceLineNumber();
                }
            }
            goToLine.Dispose();
        }

        private void TextBoxSourceLeave(object sender, EventArgs e)
        {
            ReloadFromSourceView();
        }

        private void AdjustDisplayTimeToolStripMenuItemClick(object sender, EventArgs e)
        {
            AdjustDisplayTime(false);
        }

        private void AdjustDisplayTime(bool onlySelectedLines)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var adjustDisplayTime = new AdjustDisplayDuration();
                _formPositionsAndSizes.SetPositionAndSize(adjustDisplayTime);

                ListView.SelectedIndexCollection selectedIndexes = null;
                if (onlySelectedLines)
                {
                    adjustDisplayTime.Text += " - " + _language.SelectedLines;
                    selectedIndexes = SubtitleListview1.SelectedIndices;
                }

                if (adjustDisplayTime.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeDisplayTimeAdjustment);
                    if (adjustDisplayTime.AdjustUsingPercent)
                    {
                        double percent = double.Parse(adjustDisplayTime.AdjustValue);
                        _subtitle.AdjustDisplayTimeUsingPercent(percent, selectedIndexes);
                    }
                    else
                    {
                        double seconds = double.Parse(adjustDisplayTime.AdjustValue);
                        _subtitle.AdjustDisplayTimeUsingSeconds(seconds, selectedIndexes);
                    }
                    ShowStatus(string.Format(_language.DisplayTimesAdjustedX, adjustDisplayTime.AdjustValue));
                    SaveSubtitleListviewIndexes();
                    if (IsFramesRelevant && CurrentFrameRate > 0)
                    {
                        _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                        if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                            ShowSource();
                    }
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                }
                _formPositionsAndSizes.SavePositionAndSize(adjustDisplayTime);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool IsFramesRelevant
        {
            get
            {
                return _subtitle.WasLoadedWithFrameNumbers || GetCurrentSubtitleFormat().IsFrameBased;
            }
        }

        private void FixToolStripMenuItemClick(object sender, EventArgs e)
        {
            FixCommonErrors(false);
        }

        private void FixCommonErrors(bool onlySelectedLines)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                SaveSubtitleListviewIndexes();
                var fixErrors = new FixCommonErrors();
                //_formPositionsAndSizes.SetPositionAndSize(fixErrors);

                if (onlySelectedLines)
                {
                    var selectedLines = new Subtitle { WasLoadedWithFrameNumbers = _subtitle.WasLoadedWithFrameNumbers };
                    foreach (int index in SubtitleListview1.SelectedIndices)
                        selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
                    fixErrors.Initialize(selectedLines, GetCurrentSubtitleFormat(), GetCurrentEncoding());
                }
                else
                {
                    fixErrors.Initialize(_subtitle, GetCurrentSubtitleFormat(), GetCurrentEncoding());
                }

                if (fixErrors.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeCommonErrorFixes);

                    if (onlySelectedLines)
                    { // we only update selected lines
                        int i = 0;
                        foreach (int index in SubtitleListview1.SelectedIndices)
                        {
                            _subtitle.Paragraphs[index] = fixErrors.FixedSubtitle.Paragraphs[i];
                            i++;
                        }
                        ShowStatus(_language.CommonErrorsFixedInSelectedLines);
                    }
                    else
                    {
                        _subtitle.Paragraphs.Clear();
                        foreach (Paragraph p in fixErrors.FixedSubtitle.Paragraphs)
                            _subtitle.Paragraphs.Add(p);
                        ShowStatus(_language.CommonErrorsFixed);
                    }
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                    //_formPositionsAndSizes.SavePositionAndSize(fixErrors);
                }
                Configuration.Settings.CommonErrors.StartSize = fixErrors.Width + ";" + fixErrors.Height;
                Configuration.Settings.CommonErrors.StartPosition = fixErrors.Left + ";" + fixErrors.Top;
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ShowInTaskbar = true;
        }

        private void StartNumberingFromToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var startNumberingFrom = new StartNumberingFrom();
                _formPositionsAndSizes.SetPositionAndSize(startNumberingFrom);
                if (startNumberingFrom.ShowDialog(this) == DialogResult.OK)
                {
                    SaveSubtitleListviewIndexes();
                    MakeHistoryForUndo(_language.BeforeRenumbering);
                    ShowStatus(string.Format(_language.RenumberedStartingFromX, startNumberingFrom.StartFromNumber));
                    _subtitle.Renumber(startNumberingFrom.StartFromNumber);
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                }
                _formPositionsAndSizes.SavePositionAndSize(startNumberingFrom);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Renumber()
        {
            if (_subtitle != null && _subtitle.Paragraphs != null && _subtitle.Paragraphs.Count > 0)
                _subtitle.Renumber(_subtitle.Paragraphs[0].Number);
        }

        private void RemoveTextForHearImparedToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var removeTextFromHearImpaired = new FormRemoveTextForHearImpaired();
                _formPositionsAndSizes.SetPositionAndSize(removeTextFromHearImpaired);
                removeTextFromHearImpaired.Initialize(_subtitle);
                if (removeTextFromHearImpaired.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeRemovalOfTextingForHearingImpaired);
                    int count = removeTextFromHearImpaired.RemoveTextFromHearImpaired();
                    if (count > 0)
                    {
                        if (count == 1)
                            ShowStatus(_language.TextingForHearingImpairedRemovedOneLine);
                        else
                            ShowStatus(string.Format(_language.TextingForHearingImpairedRemovedXLines, count));
                        _subtitleListViewIndex = -1;
                        Renumber();
                        ShowSource();
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        if (_subtitle.Paragraphs.Count > 0)
                            SubtitleListview1.SelectIndexAndEnsureVisible(0);
                    }
                }
                _formPositionsAndSizes.SavePositionAndSize(removeTextFromHearImpaired);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SplitToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();

                if (Configuration.Settings.Tools.SplitAdvanced)
                {
                    var split = new Split();
                    double lengthInSeconds = 0;
                    if (mediaPlayer.VideoPlayer != null)
                        lengthInSeconds = mediaPlayer.Duration;
                    split. Initialize(_subtitle, _fileName, GetCurrentSubtitleFormat(), GetCurrentEncoding(), lengthInSeconds);
                    if (split.ShowDialog(this) == DialogResult.OK)
                    {
                        ShowStatus(_language.SubtitleSplitted);
                    }
                    else if (split.ShowBasic)
                    {
                        Configuration.Settings.Tools.SplitAdvanced = false;
                        SplitToolStripMenuItemClick(null, null);
                    }
                }
                else
                {
                    var splitSubtitle = new SplitSubtitle();
                    double lengthInSeconds = 0;
                    if (mediaPlayer.VideoPlayer != null)
                        lengthInSeconds = mediaPlayer.Duration;
                    splitSubtitle.Initialize(_subtitle, _fileName, GetCurrentSubtitleFormat(), GetCurrentEncoding(), lengthInSeconds);
                    if (splitSubtitle.ShowDialog(this) == DialogResult.OK)
                    {
                        ShowStatus(_language.SubtitleSplitted);
                    }
                    else if (splitSubtitle.ShowAdvanced)
                    {
                        Configuration.Settings.Tools.SplitAdvanced = true;
                        SplitToolStripMenuItemClick(null, null);
                    }
                }
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AppendTextVisuallyToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();

                if (MessageBox.Show(_language.SubtitleAppendPrompt, _language.SubtitleAppendPromptTitle, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                {
                    openFileDialog1.Title = _language.OpenSubtitleToAppend;
                    openFileDialog1.FileName = string.Empty;
                    openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
                    if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                    {
                        bool success = false;
                        string fileName = openFileDialog1.FileName;
                        if (File.Exists(fileName))
                        {
                            var subtitleToAppend = new Subtitle();
                            Encoding encoding;
                            SubtitleFormat format = null;

                            // do not allow blu-ray/vobsub
                            string extension = Path.GetExtension(fileName).ToLower();
                            if (extension == ".sub" && (IsVobSubFile(fileName, false) || IsSpDvdSupFile(fileName)))
                            {
                                format = null;
                            }
                            else if (extension == ".sup" && IsBluRaySupFile(fileName))
                            {
                                format = null;
                            }
                            else
                            {
                                format = subtitleToAppend.LoadSubtitle(fileName, out encoding, null);
                                if (GetCurrentSubtitleFormat().IsFrameBased)
                                    subtitleToAppend.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                                else
                                    subtitleToAppend.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                            }

                            if (format != null)
                            {
                                if (subtitleToAppend != null && subtitleToAppend.Paragraphs.Count > 1)
                                {
                                    VisualSync visualSync = new VisualSync();

                                    visualSync.Initialize(toolStripButtonVisualSync.Image as Bitmap, subtitleToAppend, _fileName, _language.AppendViaVisualSyncTitle, CurrentFrameRate);

                                    visualSync.ShowDialog(this);
                                    if (visualSync.OKPressed)
                                    {
                                        if (MessageBox.Show(_language.AppendSynchronizedSubtitlePrompt, _language.SubtitleAppendPromptTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                                        {
                                            int start = _subtitle.Paragraphs.Count +1;
                                            var fr = CurrentFrameRate;
                                            MakeHistoryForUndo(_language.BeforeAppend);
                                            foreach (Paragraph p in visualSync.Paragraphs)
                                            {
                                                if (format.IsFrameBased)
                                                    p.CalculateFrameNumbersFromTimeCodes(fr);
                                                _subtitle.Paragraphs.Add(new Paragraph(p));
                                            }
                                            _subtitle.Renumber(1);

                                            ShowSource();
                                            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);

                                            // select appended lines
                                            for (int i = start; i < _subtitle.Paragraphs.Count; i++)
                                                SubtitleListview1.Items[i].Selected = true;
                                            SubtitleListview1.EnsureVisible(start);

                                            ShowStatus(string.Format(_language.SubtitleAppendedX, fileName));
                                            success = true;
                                        }
                                    }
                                    visualSync.Dispose();
                                }
                            }
                        }
                        if (!success)
                            ShowStatus(_language.SubtitleNotAppended);
                    }
                }
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TranslateByGoogleToolStripMenuItemClick(object sender, EventArgs e)
        {
            TranslateViaGoogle(false, true);
        }

        private void TranslateViaGoogle(bool onlySelectedLines, bool useGoogle)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var googleTranslate = new GoogleTranslate();
                _formPositionsAndSizes.SetPositionAndSize(googleTranslate);
                SaveSubtitleListviewIndexes();
                string title = _language.GoogleTranslate;
                if (!useGoogle)
                    title = _language.MicrosoftTranslate;
                if (onlySelectedLines)
                {
                    var selectedLines = new Subtitle { WasLoadedWithFrameNumbers = _subtitle.WasLoadedWithFrameNumbers };
                    foreach (int index in SubtitleListview1.SelectedIndices)
                        selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
                    title += " - " + _language.SelectedLines;
                    googleTranslate.Initialize(selectedLines, title, useGoogle);
                }
                else
                {
                    googleTranslate.Initialize(_subtitle, title, useGoogle);
                }
                if (googleTranslate.ShowDialog(this) == DialogResult.OK)
                {
                    _subtitleListViewIndex = -1;

                    MakeHistoryForUndo(_language.BeforeGoogleTranslation);
                    if (onlySelectedLines)
                    { // we only update selected lines
                        int i = 0;
                        foreach (int index in SubtitleListview1.SelectedIndices)
                        {
                            _subtitle.Paragraphs[index] = googleTranslate.TranslatedSubtitle.Paragraphs[i];
                            i++;
                        }
                        ShowStatus(_language.SelectedLinesTranslated);
                    }
                    else
                    {
                        _subtitleAlternate = new Subtitle(_subtitle);
                        _subtitleAlternateFileName = _fileName;
                        _fileName = null;
                        _subtitle.Paragraphs.Clear();
                        foreach (Paragraph p in googleTranslate.TranslatedSubtitle.Paragraphs)
                            _subtitle.Paragraphs.Add(new Paragraph(p));
                        ShowStatus(_language.SubtitleTranslated);
                    }
                    ShowSource();

                    if (!onlySelectedLines)
                    {
                        SubtitleListview1.ShowAlternateTextColumn(Configuration.Settings.Language.General.OriginalText);
                        SubtitleListview1.AutoSizeAllColumns(this);
                        SetupAlternateEdit();
                    }
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);

                    RestoreSubtitleListviewIndexes();
                    _converted = true;
                    SetTitle();
                    //if (googleTranslate.ScreenScrapingEncoding != null)
                    //    SetEncoding(googleTranslate.ScreenScrapingEncoding);
                    SetEncoding(Encoding.UTF8);
                }
                _formPositionsAndSizes.SavePositionAndSize(googleTranslate);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string GetTranslateStringFromNikseDk(string input)
        {
            WebRequest.DefaultWebProxy = Utilities.GetProxy();
//            WebRequest request = WebRequest.Create("http://localhost:54942/MultiTranslator/TranslateForSubtitleEdit");
            WebRequest request = WebRequest.Create("http://www.nikse.dk/MultiTranslator/TranslateForSubtitleEdit");
            request.Method = "POST";
            string postData = String.Format("languagePair={1}&text={0}", Utilities.UrlEncode(input), "svda");
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            string result = responseFromServer;
            reader.Close();
            dataStream.Close();
            response.Close();
            return result;
        }

        private void TranslateFromSwedishToDanishToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                bool isSwedish = Utilities.AutoDetectGoogleLanguage(_subtitle) == "sv";
                string promptText = _language.TranslateSwedishToDanish;
                if (!isSwedish)
                    promptText = _language.TranslateSwedishToDanishWarning;

                if (MessageBox.Show(promptText, Title, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        _subtitleAlternate = new Subtitle(_subtitle);
                        _subtitleAlternateFileName = null;
                        int firstSelectedIndex = 0;
                        if (SubtitleListview1.SelectedItems.Count > 0)
                            firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;
                        _subtitleListViewIndex = -1;

                        Cursor.Current = Cursors.WaitCursor;
                        ShowStatus(_language.TranslatingViaNikseDkMt);
                        var sb = new StringBuilder();
                        var output = new StringBuilder();
                        foreach (Paragraph p in _subtitle.Paragraphs)
                        {
                            string s = p.Text;
                            s = s.Replace(Environment.NewLine, "<br/>");
                            s = "<p>" + s + "</p>";
                            sb.Append(s);

                            if (sb.Length > 9000)
                            {
                                output.Append(GetTranslateStringFromNikseDk(sb.ToString()));
                                sb = new StringBuilder();
                            }
                        }
                        if (sb.Length > 0)
                            output.Append(GetTranslateStringFromNikseDk(sb.ToString()));

                        MakeHistoryForUndo(_language.BeforeSwedishToDanishTranslation);
                        string result = output.ToString();
                        if (result.Length > 0)
                        {
                            int index = 0;
                            foreach (string s in result.Split(new string[] { "<p>", "</p>" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (index < _subtitle.Paragraphs.Count)
                                    _subtitle.Paragraphs[index].Text = s;
                                index++;
                            }
                            ShowSource();
                            SubtitleListview1.ShowAlternateTextColumn(Configuration.Settings.Language.General.OriginalText);
                            SubtitleListview1.AutoSizeAllColumns(this);
                            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                            ShowStatus(_language.TranslationFromSwedishToDanishComplete);
                            SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
                            _converted = true;
                        }
                    }
                    catch
                    {
                        ShowStatus(_language.TranslationFromSwedishToDanishFailed);
                    }
                    Cursor.Current = Cursors.Default;
                }
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UndoLastAction()
        {
            if (_subtitle != null && _subtitle.CanUndo && _undoIndex >= 0)
                UndoToIndex(true);
        }

        /// <summary>
        /// Undo or Redo
        /// </summary>
        /// <param name="undo">True equals undo, false triggers redo</param>
        private void UndoToIndex(bool undo)
        {
            lock (this)
            {
                if (!undo && _undoIndex >= _subtitle.HistoryItems.Count - 1)
                    return;
                if (undo && !_subtitle.CanUndo && _undoIndex < 0)
                    return;

                // Add latest changes if any (also stop changes from being added while redoing/undoing)
                timerTextUndo.Stop();
                timerAlternateTextUndo.Stop();
                _listViewTextTicks = 0;
                _listViewAlternateTextTicks = 0;
                TimerTextUndoTick(null, null);
                TimerAlternateTextUndoTick(null, null);

                try
                {
                    int selectedIndex = FirstSelectedIndex;
                    string text = string.Empty;
                    if (undo)
                    {
                        _subtitle.HistoryItems[_undoIndex].RedoParagraphs = new List<Paragraph>();
                        _subtitle.HistoryItems[_undoIndex].RedoParagraphsAlternate = new List<Paragraph>();

                        foreach (Paragraph p in _subtitle.Paragraphs)
                            _subtitle.HistoryItems[_undoIndex].RedoParagraphs.Add(new Paragraph(p));
                        if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null)
                        {
                            foreach (Paragraph p in _subtitleAlternate.Paragraphs)
                                _subtitle.HistoryItems[_undoIndex].RedoParagraphsAlternate.Add(new Paragraph(p));
                        }
                        _subtitle.HistoryItems[_undoIndex].RedoFileName = _fileName;
                        _subtitle.HistoryItems[_undoIndex].RedoFileModified = _fileDateTime;

                        if (selectedIndex >= 0)
                        {
                            _subtitle.HistoryItems[_undoIndex].RedoParagraphs[selectedIndex].Text =
                                textBoxListViewText.Text;
                            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null &&
                                selectedIndex < _subtitle.HistoryItems[_undoIndex].RedoParagraphsAlternate.Count)
                                _subtitle.HistoryItems[_undoIndex].RedoParagraphsAlternate[selectedIndex].Text =
                                    textBoxListViewTextAlternate.Text;
                            _subtitle.HistoryItems[_undoIndex].RedoLineIndex = selectedIndex;
                            _subtitle.HistoryItems[_undoIndex].RedoLinePosition = textBoxListViewText.SelectionStart;
                            _subtitle.HistoryItems[_undoIndex].RedoLinePositionAlternate =
                                textBoxListViewTextAlternate.SelectionStart;
                        }
                        else
                        {
                            _subtitle.HistoryItems[_undoIndex].RedoLineIndex = -1;
                            _subtitle.HistoryItems[_undoIndex].RedoLinePosition = -1;
                        }
                    }
                    else
                    {
                        _undoIndex++;
                    }
                    text = _subtitle.HistoryItems[_undoIndex].Description;

                    _subtitleListViewIndex = -1;
                    textBoxListViewText.Text = string.Empty;
                    textBoxListViewTextAlternate.Text = string.Empty;
                    string subtitleFormatFriendlyName;

                    string oldFileName = _fileName;
                    DateTime oldFileDateTime = _fileDateTime;

                    _fileName = _subtitle.UndoHistory(_undoIndex, out subtitleFormatFriendlyName, out _fileDateTime,
                                                      out _subtitleAlternate, out _subtitleAlternateFileName);
                    if (!undo)
                    {
                        if (_subtitle.HistoryItems[_undoIndex].RedoParagraphs != null)
                            //TODO: sometimes redo paragraphs can be null - how?
                        {
                            _subtitle.Paragraphs.Clear();
                            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null)
                                _subtitleAlternate.Paragraphs.Clear();
                            foreach (Paragraph p in _subtitle.HistoryItems[_undoIndex].RedoParagraphs)
                                _subtitle.Paragraphs.Add(new Paragraph(p));
                            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null)
                            {
                                foreach (Paragraph p in _subtitle.HistoryItems[_undoIndex].RedoParagraphsAlternate)
                                    _subtitleAlternate.Paragraphs.Add(new Paragraph(p));
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Undo failed at undo index: " + _undoIndex);
                        }
                        _subtitle.HistoryItems[_undoIndex].RedoParagraphs = null;
                        _subtitle.HistoryItems[_undoIndex].RedoParagraphsAlternate = null;
                    }

                    if (string.Compare(oldFileName, _fileName, true) == 0)
                        _fileDateTime = oldFileDateTime; // undo will not give overwrite-newer-file warning

                    comboBoxSubtitleFormats.SelectedIndexChanged -= ComboBoxSubtitleFormatsSelectedIndexChanged;
                    SetCurrentFormat(subtitleFormatFriendlyName);
                    comboBoxSubtitleFormats.SelectedIndexChanged += ComboBoxSubtitleFormatsSelectedIndexChanged;

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);

                    if (selectedIndex >= _subtitle.Paragraphs.Count)
                        SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.Paragraphs.Count-1, true);
                    else if (selectedIndex >= 0 && selectedIndex < _subtitle.Paragraphs.Count)
                        SubtitleListview1.SelectIndexAndEnsureVisible(selectedIndex, true);
                    else
                        SubtitleListview1.SelectIndexAndEnsureVisible(0, true);

                    audioVisualizer.Invalidate();
                    if (undo)
                    {
                        if (_subtitle.HistoryItems[_undoIndex].LineIndex == FirstSelectedIndex)
                        {
                            textBoxListViewText.SelectionStart = _subtitle.HistoryItems[_undoIndex].LinePosition;
                            if (_subtitleAlternate != null)
                                textBoxListViewTextAlternate.SelectionStart =
                                    _subtitle.HistoryItems[_undoIndex].LinePositionAlternate;
                        }
                        ShowStatus(_language.UndoPerformed + ": " + text.Replace(Environment.NewLine, "  "));
                        _undoIndex--;
                    }
                    else
                    {
                        if (_subtitle.HistoryItems[_undoIndex].RedoLineIndex >= 0 &&
                            _subtitle.HistoryItems[_undoIndex].RedoLineIndex == FirstSelectedIndex)
                            textBoxListViewText.SelectionStart = _subtitle.HistoryItems[_undoIndex].RedoLinePosition;
                        if (_subtitleAlternate != null && _subtitle.HistoryItems[_undoIndex].RedoLineIndex >= 0 &&
                            _subtitle.HistoryItems[_undoIndex].RedoLineIndex == FirstSelectedIndex)
                            textBoxListViewTextAlternate.SelectionStart =
                                _subtitle.HistoryItems[_undoIndex].RedoLinePositionAlternate;
                        if (string.Compare(_subtitle.HistoryItems[_undoIndex].RedoFileName, _fileName, true) == 0)
                            _fileDateTime = _subtitle.HistoryItems[_undoIndex].RedoFileModified;
                        _fileName = _subtitle.HistoryItems[_undoIndex].RedoFileName;
                        ShowStatus(_language.UndoPerformed);
                    }

                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message);
                }

                timerTextUndo.Start();
                timerAlternateTextUndo.Start();
                SetTitle();
            }
        }

        private void RedoLastAction()
        {
            if (_undoIndex < _subtitle.HistoryItems.Count-1)
                UndoToIndex(false);
        }

        private void ShowHistoryforUndoToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_subtitle != null && _subtitle.CanUndo)
            {
                ReloadFromSourceView();
                var showHistory = new ShowHistory();
                showHistory.Initialize(_subtitle, _undoIndex);
                if (showHistory.ShowDialog(this) == DialogResult.OK)
                {
                    int selectedIndex = FirstSelectedIndex;
                    _subtitleListViewIndex = -1;
                    textBoxListViewText.Text = string.Empty;
                    textBoxListViewTextAlternate.Text = string.Empty;
                    MakeHistoryForUndo(_language.BeforeUndo);
                    string subtitleFormatFriendlyName;

                    string oldFileName = _fileName;
                    DateTime oldFileDateTime = _fileDateTime;

                    _fileName = _subtitle.UndoHistory(showHistory.SelectedIndex, out subtitleFormatFriendlyName, out _fileDateTime, out _subtitleAlternate, out _subtitleAlternateFileName);

                    if (string.Compare(oldFileName, _fileName, true) == 0)
                        _fileDateTime = oldFileDateTime; // undo will not give overwrite-newer-file warning

                    SetTitle();
                    ShowStatus(_language.UndoPerformed);

                    comboBoxSubtitleFormats.SelectedIndexChanged -= ComboBoxSubtitleFormatsSelectedIndexChanged;
                    SetCurrentFormat(subtitleFormatFriendlyName);
                    comboBoxSubtitleFormats.SelectedIndexChanged += ComboBoxSubtitleFormatsSelectedIndexChanged;

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);

                    if (selectedIndex >= 0 && selectedIndex < _subtitle.Paragraphs.Count)
                        SubtitleListview1.SelectIndexAndEnsureVisible(selectedIndex, true);
                    else
                        SubtitleListview1.SelectIndexAndEnsureVisible(0, true);

                    audioVisualizer.Invalidate();
                }
            }
            else
            {
                MessageBox.Show(_language.NothingToUndo, Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ToolStripButtonSpellCheckClick(object sender, EventArgs e)
        {
            SpellCheck(true);
        }

        private void SpellCheckToolStripMenuItemClick(object sender, EventArgs e)
        {
            SpellCheck(true);
        }

        private void SpellCheckViaWord()
        {
            if (_subtitle == null | _subtitle.Paragraphs.Count == 0)
                return;

            WordSpellChecker wordSpellChecker = null;
            int totalLinesChanged = 0;
            try
            {
                wordSpellChecker = new WordSpellChecker(this, Utilities.AutoDetectGoogleLanguage(_subtitle));
                wordSpellChecker.NewDocument();
                Application.DoEvents();
            }
            catch
            {
                MessageBox.Show(_language.UnableToStartWord);
                return;
            }
            string version = wordSpellChecker.Version;

            int index = FirstSelectedIndex;
            if (index < 0)
                index = 0;

            _cancelWordSpellCheck = false;
            for (;index < _subtitle.Paragraphs.Count; index++)
            {
                Paragraph p = _subtitle.Paragraphs[index];
                int errorsBefore;
                int errorsAfter;
                ShowStatus(string.Format(_language.SpellChekingViaWordXLineYOfX, version, index+1, _subtitle.Paragraphs.Count.ToString()));
                SubtitleListview1.SelectIndexAndEnsureVisible(index);
                string newText = wordSpellChecker.CheckSpelling(p.Text, out errorsBefore, out errorsAfter);
                if (errorsAfter > 0)
                {
                    wordSpellChecker.CloseDocument();
                    wordSpellChecker.Quit();
                    ShowStatus(string.Format(_language.SpellCheckAbortedXCorrections, totalLinesChanged));
                    Cursor = Cursors.Default;
                    return;
                }
                else if (errorsBefore != errorsAfter)
                {
                    if (textBoxListViewText.Text != newText)
                    {
                        textBoxListViewText.Text = newText;
                        totalLinesChanged++;
                    }
                }

                Application.DoEvents();
                if (_cancelWordSpellCheck)
                    break;
            }
            wordSpellChecker.CloseDocument();
            wordSpellChecker.Quit();
            ShowStatus(string.Format(_language.SpellCheckCompletedXCorrections, totalLinesChanged));
            Cursor = Cursors.Default;
            _cancelWordSpellCheck = true;
        }

        private void SpellCheck(bool autoDetect)
        {
            if (Configuration.Settings.General.SpellChecker.ToLower().Contains("word"))
            {
                SpellCheckViaWord();
                return;
            }

            try
            {
                string dictionaryFolder = Utilities.DictionaryFolder;
                if (!Directory.Exists(dictionaryFolder) || Directory.GetFiles(dictionaryFolder, "*.dic").Length == 0)
                {
                    ShowGetDictionaries();
                    return;
                }

                if (_subtitle != null && _subtitle.Paragraphs.Count > 0)
                {
                    if (_spellCheckForm != null)
                    {
                        DialogResult result = MessageBox.Show(_language.ContinueWithCurrentSpellCheck, Title, MessageBoxButtons.YesNoCancel);
                        if (result == DialogResult.Cancel)
                            return;

                        if (result == DialogResult.No)
                        {
                            _spellCheckForm = new SpellCheck();
                            _spellCheckForm.DoSpellCheck(autoDetect, _subtitle, dictionaryFolder, this);
                        }
                        else
                        {
                            _spellCheckForm.ContinueSpellcheck(_subtitle);
                        }
                    }
                    else
                    {
                        _spellCheckForm = new SpellCheck();
                        _spellCheckForm.DoSpellCheck(autoDetect, _subtitle, dictionaryFolder, this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("{0}{1}{2}{3}{4}", ex.Source, Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace), _title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ChangeWholeTextMainPart(ref int noOfChangedWords, ref bool firstChange, int i, Paragraph p)
        {
            SubtitleListview1.SetText(i, p.Text);
            noOfChangedWords++;
            if (firstChange)
            {
                MakeHistoryForUndo(_language.BeforeSpellCheck);
                firstChange = false;
            }
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                ShowSource();
            else
                RefreshSelectedParagraph();
        }

        public void FocusParagraph(int index)
        {
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
            {
                tabControlSubtitle.SelectedIndex = TabControlListView;
            }

            if (tabControlSubtitle.SelectedIndex == TabControlListView)
            {
               SubtitleListview1.SelectIndexAndEnsureVisible(index);
            }
        }

        private void RefreshSelectedParagraph()
        {
            _subtitleListViewIndex = -1;
            SubtitleListview1_SelectedIndexChanged(null, null);
        }

        public void CorrectWord(string changeWord, Paragraph p, string oldWord, ref bool firstChange)
        {
            if (oldWord != changeWord)
            {
                if (firstChange)
                {
                    MakeHistoryForUndo(_language.BeforeSpellCheck);
                    firstChange = false;
                }
                var regEx = new Regex("\\b" + oldWord + "\\b");
                if (regEx.IsMatch(p.Text))
                {
                    p.Text = regEx.Replace(p.Text, changeWord);
                }
                else
                {
                    int startIndex = p.Text.IndexOf(oldWord);
                    while (startIndex >= 0 && startIndex < p.Text.Length && p.Text.Substring(startIndex).Contains(oldWord))
                    {
                        bool startOk = (startIndex == 0) || (p.Text[startIndex - 1] == ' ') ||
                                       (Environment.NewLine.EndsWith(p.Text[startIndex - 1].ToString()));

                        if (startOk)
                        {
                            int end = startIndex + oldWord.Length;
                            if (end <= p.Text.Length)
                            {
                                if ((end == p.Text.Length) || ((" ,.!?:;')" + Environment.NewLine).Contains(p.Text[end].ToString())))
                                    p.Text = p.Text.Remove(startIndex, oldWord.Length).Insert(startIndex, changeWord);
                            }
                        }
                        startIndex = p.Text.IndexOf(oldWord, startIndex + 2);
                    }

                }
                ShowStatus(string.Format(_language.SpellCheckChangedXToY, oldWord, changeWord));
                SubtitleListview1.SetText(_subtitle.GetIndex(p), p.Text);
                if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                {
                    ShowSource();
                }
                else
                {
                    RefreshSelectedParagraph();
                }
            }
        }

        private void GetDictionariesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowGetDictionaries();
        }

        private void ShowGetDictionaries()
        {
            new GetDictionaries().ShowDialog(this); // backup plan..
        }

        private void ContextMenuStripListviewOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((GetCurrentSubtitleFormat().GetType() == typeof(AdvancedSubStationAlpha) || GetCurrentSubtitleFormat().GetType() == typeof(SubStationAlpha))
                && SubtitleListview1.SelectedItems.Count > 0)
            {
                var styles = AdvancedSubStationAlpha.GetStylesFromHeader(_subtitle.Header);
                setStylesForSelectedLinesToolStripMenuItem.DropDownItems.Clear();
                foreach (string style in styles)
                {
                    setStylesForSelectedLinesToolStripMenuItem.DropDownItems.Add(style, null, tsi_Click);
                }
                setStylesForSelectedLinesToolStripMenuItem.Visible = styles.Count > 1;
                toolStripMenuItemAssStyles.Visible = true;
                if (GetCurrentSubtitleFormat().GetType() == typeof(AdvancedSubStationAlpha))
                {
                    toolStripMenuItemAssStyles.Text = _language.Menu.ContextMenu.AdvancedSubStationAlphaStyles;
                    setStylesForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.AdvancedSubStationAlphaSetStyle;
                }
                else
                {
                    toolStripMenuItemAssStyles.Text = _language.Menu.ContextMenu.SubStationAlphaStyles;
                    setStylesForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.SubStationAlphaSetStyle;
                }
            }
            else if (GetCurrentSubtitleFormat().GetType() == typeof(TimedText10) && SubtitleListview1.SelectedItems.Count > 0)
            {
                toolStripMenuItemAssStyles.Text = _language.Menu.ContextMenu.TimedTextStyles;
                var styles = TimedText10.GetStylesFromHeader(_subtitle.Header);
                setStylesForSelectedLinesToolStripMenuItem.DropDownItems.Clear();
                foreach (string style in styles)
                {
                    setStylesForSelectedLinesToolStripMenuItem.DropDownItems.Add(style, null, tsi_Click);
                }
                setStylesForSelectedLinesToolStripMenuItem.Visible = styles.Count > 1;
                toolStripMenuItemAssStyles.Visible = true;
                setStylesForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.TimedTextSetStyle;
            }
            else if ((GetCurrentSubtitleFormat().GetType() == typeof(Sami) || GetCurrentSubtitleFormat().GetType() == typeof(SamiModern)) && SubtitleListview1.SelectedItems.Count > 0)
            {
                toolStripMenuItemAssStyles.Text = _language.Menu.ContextMenu.TimedTextStyles;
                var styles = Sami.GetStylesFromHeader(_subtitle.Header);
                setStylesForSelectedLinesToolStripMenuItem.DropDownItems.Clear();
                foreach (string style in styles)
                {
                    setStylesForSelectedLinesToolStripMenuItem.DropDownItems.Add(style, null, tsi_Click);
                }
                setStylesForSelectedLinesToolStripMenuItem.Visible = styles.Count > 1;
                toolStripMenuItemAssStyles.Visible = false;
                setStylesForSelectedLinesToolStripMenuItem.Text = _language.Menu.ContextMenu.SamiSetStyle;
            }
            else
            {
                setStylesForSelectedLinesToolStripMenuItem.Visible = false;
                toolStripMenuItemAssStyles.Visible = false;
            }


            toolStripMenuItemGoogleMicrosoftTranslateSelLine.Visible = false;
            if (SubtitleListview1.SelectedItems.Count == 0)
            {
                contextMenuStripEmpty.Show(MousePosition.X, MousePosition.Y);
                e.Cancel = true;
            }
            else
            {
                toolStripMenuItemSaveSelectedLines.Visible = false;
                toolStripMenuItemInsertBefore.Visible = true;
                toolStripMenuItemInsertAfter.Visible = true;
                toolStripMenuItemInsertSubtitle.Visible = _networkSession == null;
                toolStripMenuItemMergeLines.Visible = true;
                mergeAfterToolStripMenuItem.Visible = true;
                mergeBeforeToolStripMenuItem.Visible = true;
                splitLineToolStripMenuItem.Visible = true;
                toolStripSeparator7.Visible = true;
                typeEffectToolStripMenuItem.Visible = _networkSession == null;
                karokeeEffectToolStripMenuItem.Visible = _networkSession == null;
                toolStripSeparatorAdvancedFunctions.Visible = _networkSession == null;
                showSelectedLinesEarlierlaterToolStripMenuItem.Visible = true;
                visualSyncSelectedLinesToolStripMenuItem.Visible = true;
                googleTranslateSelectedLinesToolStripMenuItem.Visible = true;
                adjustDisplayTimeForSelectedLinesToolStripMenuItem.Visible = true;
                toolStripMenuItemUnbreakLines.Visible = true;
                toolStripMenuItemAutoBreakLines.Visible = true;
                toolStripSeparatorBreakLines.Visible = true;
                toolStripMenuItemSurroundWithMusicSymbols.Visible = IsUnicode;

                if (SubtitleListview1.SelectedItems.Count == 1)
                {
                    toolStripMenuItemMergeLines.Visible = false;
                    visualSyncSelectedLinesToolStripMenuItem.Visible = false;
                    toolStripMenuItemUnbreakLines.Visible = false;
                    toolStripMenuItemAutoBreakLines.Visible = false;
                    toolStripSeparatorBreakLines.Visible = false;
                    toolStripMenuItemGoogleMicrosoftTranslateSelLine.Visible = _subtitleAlternate != null;
                    toolStripMenuItemMergeDialogue.Visible = false;
                }
                else if (SubtitleListview1.SelectedItems.Count == 2)
                {
                    toolStripMenuItemInsertBefore.Visible = false;
                    toolStripMenuItemInsertAfter.Visible = false;
                    toolStripMenuItemInsertSubtitle.Visible = false;
                    mergeAfterToolStripMenuItem.Visible = false;
                    mergeBeforeToolStripMenuItem.Visible = false;
                    splitLineToolStripMenuItem.Visible = false;
                    typeEffectToolStripMenuItem.Visible = false;
                    toolStripMenuItemMergeDialogue.Visible = true;
                }
                else if (SubtitleListview1.SelectedItems.Count >= 2)
                {
                    toolStripMenuItemSaveSelectedLines.Visible = true;
                    toolStripMenuItemInsertBefore.Visible = false;
                    toolStripMenuItemInsertAfter.Visible = false;
                    toolStripMenuItemInsertSubtitle.Visible = false;
                    splitLineToolStripMenuItem.Visible = false;
                    mergeAfterToolStripMenuItem.Visible = false;
                    mergeBeforeToolStripMenuItem.Visible = false;
                    typeEffectToolStripMenuItem.Visible = false;
                    toolStripSeparator7.Visible = false;

                    if (SubtitleListview1.SelectedItems.Count > 5)
                        toolStripMenuItemMergeLines.Visible = false;
                    toolStripMenuItemMergeDialogue.Visible = false;
                }

                if (GetCurrentSubtitleFormat().GetType() != typeof(SubRip))
                {
                    karokeeEffectToolStripMenuItem.Visible = false;
                    toolStripSeparatorAdvancedFunctions.Visible = SubtitleListview1.SelectedItems.Count == 1;
                }
            }
        }

        void tsi_Click(object sender, EventArgs e)
        {
            string style = (sender as ToolStripItem).Text;
            foreach (int index in SubtitleListview1.SelectedIndices)
            {
                _subtitle.Paragraphs[index].Extra = style;
                SubtitleListview1.SetExtraText(index, style, SubtitleListview1.ForeColor);
            }
        }

        private void BoldToolStripMenuItemClick(object sender, EventArgs e)
        {
            ListViewToggleTag("b");
        }

        private void ItalicToolStripMenuItemClick(object sender, EventArgs e)
        {
            ListViewToggleTag("i");
        }

        private void UnderlineToolStripMenuItemClick(object sender, EventArgs e)
        {
            ListViewToggleTag("u");
        }

        private void ListViewToggleTag(string tag)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                MakeHistoryForUndo(string.Format(_language.BeforeAddingTagX, tag));

                var indexes = new List<int>();
                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    indexes.Add(item.Index);

                SubtitleListview1.BeginUpdate();
                foreach (int i in indexes)
                {
                    if (_subtitleAlternate != null)
                    {
                        Paragraph original = Utilities.GetOriginalParagraph(i, _subtitle.Paragraphs[i], _subtitleAlternate.Paragraphs);
                        if (original != null)
                        {
                            if (original.Text.Contains("<" + tag + ">"))
                            {
                                original.Text = original.Text.Replace("<" + tag + ">", string.Empty);
                                original.Text = original.Text.Replace("</" + tag + ">", string.Empty);
                            }
                            else
                            {
                                int indexOfEndBracket = original.Text.IndexOf("}");
                                if (original.Text.StartsWith("{\\") && indexOfEndBracket > 1 && indexOfEndBracket < 6)
                                    original.Text = string.Format("{2}<{0}>{1}</{0}>", tag, original.Text.Remove(0, indexOfEndBracket+1), original.Text.Substring(0, indexOfEndBracket+1));
                                else
                                    original.Text = string.Format("<{0}>{1}</{0}>", tag, original.Text);
                            }
                            SubtitleListview1.SetAlternateText(i, original.Text);
                        }
                    }

                    if (_subtitle.Paragraphs[i].Text.Contains("<" + tag + ">"))
                    {
                        _subtitle.Paragraphs[i].Text = _subtitle.Paragraphs[i].Text.Replace("<" + tag + ">", string.Empty);
                        _subtitle.Paragraphs[i].Text = _subtitle.Paragraphs[i].Text.Replace("</" + tag + ">", string.Empty);
                    }
                    else
                    {
                        int indexOfEndBracket = _subtitle.Paragraphs[i].Text.IndexOf("}");
                        if (_subtitle.Paragraphs[i].Text.StartsWith("{\\") && indexOfEndBracket > 1 && indexOfEndBracket < 6)
                            _subtitle.Paragraphs[i].Text = string.Format("{2}<{0}>{1}</{0}>", tag, _subtitle.Paragraphs[i].Text.Remove(0, indexOfEndBracket + 1), _subtitle.Paragraphs[i].Text.Substring(0, indexOfEndBracket+1));
                        else
                            _subtitle.Paragraphs[i].Text = string.Format("<{0}>{1}</{0}>", tag, _subtitle.Paragraphs[i].Text);
                    }
                    SubtitleListview1.SetText(i, _subtitle.Paragraphs[i].Text);
                }
                SubtitleListview1.EndUpdate();

                ShowStatus(string.Format(_language.TagXAdded, tag));
                ShowSource();
                RefreshSelectedParagraph();
                SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
            }
        }

        private void ToolStripMenuItemDeleteClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                string statusText;
                string historyText;
                string askText;

                if (SubtitleListview1.SelectedItems.Count > 1)
                {
                    statusText = string.Format(_language.XLinesDeleted, SubtitleListview1.SelectedItems.Count);
                    historyText = string.Format(_language.BeforeDeletingXLines, SubtitleListview1.SelectedItems.Count);
                    askText = string.Format(_language.DeleteXLinesPrompt, SubtitleListview1.SelectedItems.Count);
                }
                else
                {
                    statusText = _language.OneLineDeleted;
                    historyText = _language.BeforeDeletingOneLine;
                    askText = _language.DeleteOneLinePrompt;
                }

                if (Configuration.Settings.General.PromptDeleteLines && MessageBox.Show(askText, Title, MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    _cutText = string.Empty;
                    return;
                }

                if (!string.IsNullOrEmpty(_cutText))
                {
                    Clipboard.SetText(_cutText);
                    _cutText = string.Empty;
                }

                MakeHistoryForUndo(historyText);
                _subtitleListViewIndex = -1;

                if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                {
                    var alternateIndexes = new List<int>();
                    foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    {
                        Paragraph p = _subtitle.GetParagraphOrDefault(item.Index);
                        if (p != null)
                        {
                            Paragraph original = Utilities.GetOriginalParagraph(item.Index, p, _subtitleAlternate.Paragraphs);
                            if (original != null)
                                alternateIndexes.Add(_subtitleAlternate.GetIndex(original));
                        }
                    }

                    alternateIndexes.Reverse();
                    foreach (int i in alternateIndexes)
                    {
                        if (i <_subtitleAlternate.Paragraphs.Count)
                            _subtitleAlternate.Paragraphs.RemoveAt(i);
                    }
                    _subtitleAlternate.Renumber(1);
                }

                var indexes = new List<int>();
                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    indexes.Add(item.Index);
                int firstIndex = SubtitleListview1.SelectedItems[0].Index;

                if (_networkSession != null)
                {
                    _networkSession.TimerStop();
                    NetworkGetSendUpdates(indexes, 0, null);
                }
                else
                {
                    int startNumber = _subtitle.Paragraphs[0].Number;
                    indexes.Reverse();
                    foreach (int i in indexes)
                    {
                        _subtitle.Paragraphs.RemoveAt(i);
                        if (_networkSession != null && _networkSession.LastSubtitle != null && i < _networkSession.LastSubtitle.Paragraphs.Count)
                            _networkSession.LastSubtitle.Paragraphs.RemoveAt(i);
                    }
                    _subtitle.Renumber(startNumber);
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    if (SubtitleListview1.FirstVisibleIndex == 0)
                        SubtitleListview1.FirstVisibleIndex = -1;
                    if (SubtitleListview1.Items.Count > firstIndex)
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(firstIndex, true);
                    }
                    else if (SubtitleListview1.Items.Count > 0)
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(SubtitleListview1.Items.Count - 1, true);
                    }
                }

                ShowStatus(statusText);
                ShowSource();
            }
        }

        private void ToolStripMenuItemInsertBeforeClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0)
                InsertBefore();
            textBoxListViewText.Focus();
        }

        private void InsertBefore()
        {
            var format = GetCurrentSubtitleFormat();
            bool useExtraForStyle = format.HasStyleSupport;
            List<string> styles = new List<string>();
            if (format.GetType() == typeof(AdvancedSubStationAlpha) || format.GetType() == typeof(SubStationAlpha))
                styles = AdvancedSubStationAlpha.GetStylesFromHeader(_subtitle.Header);
            else if (format.GetType() == typeof(TimedText10))
                styles = TimedText10.GetStylesFromHeader(_subtitle.Header);
            else if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                styles = Sami.GetStylesFromHeader(_subtitle.Header);
            string style = "Default";
            if (styles.Count > 0)
                style = styles[0];

            MakeHistoryForUndo(_language.BeforeInsertLine);

            int startNumber = 1;
            if (_subtitle.Paragraphs.Count > 0)
                startNumber = _subtitle.Paragraphs[0].Number;
            int firstSelectedIndex = 0;
            if (SubtitleListview1.SelectedItems.Count > 0)
                firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

            int addMilliseconds = Configuration.Settings.General.MininumMillisecondsBetweenLines +1;
            if (addMilliseconds < 1)
                addMilliseconds = 1;

            var newParagraph = new Paragraph();
            if (useExtraForStyle)
                newParagraph.Extra = style;

            Paragraph prev = _subtitle.GetParagraphOrDefault(firstSelectedIndex - 1);
            Paragraph next = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
            if (prev != null && next != null)
            {
                newParagraph.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - addMilliseconds;
                newParagraph.StartTime.TotalMilliseconds = newParagraph.EndTime.TotalMilliseconds - 2000;
                if (newParagraph.StartTime.TotalMilliseconds <= prev.EndTime.TotalMilliseconds)
                    newParagraph.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + 1;
                if (newParagraph.Duration.TotalMilliseconds < 100)
                    newParagraph.EndTime.TotalMilliseconds += 100;
            }
            else if (prev != null)
            {
                newParagraph.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + addMilliseconds;
                newParagraph.EndTime.TotalMilliseconds = newParagraph.StartTime.TotalMilliseconds + 2000;
                if (next != null && newParagraph.EndTime.TotalMilliseconds > next.StartTime.TotalMilliseconds)
                    newParagraph.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 1;
                if (newParagraph.StartTime.TotalMilliseconds > newParagraph.EndTime.TotalMilliseconds)
                    newParagraph.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + 1;
            }
            else if (next != null)
            {
                newParagraph.StartTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 2001;
                newParagraph.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 1;
            }
            else
            {
                newParagraph.StartTime.TotalMilliseconds = 1000;
                newParagraph.EndTime.TotalMilliseconds = 3000;
            }
            if (GetCurrentSubtitleFormat().IsFrameBased)
            {
                newParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                newParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
            }

            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                _subtitleAlternate.InsertParagraphInCorrectTimeOrder(new Paragraph(newParagraph));
                _subtitleAlternate.Renumber(1);
            }

            if (_networkSession != null)
            {
                _networkSession.TimerStop();
                NetworkGetSendUpdates(new List<int>(), firstSelectedIndex, newParagraph);
            }
            else
            {
                _subtitle.Paragraphs.Insert(firstSelectedIndex, newParagraph);
                _subtitleListViewIndex = -1;
                _subtitle.Renumber(startNumber);
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            }
            SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
            ShowSource();
            ShowStatus(_language.LineInserted);
        }

        private void ToolStripMenuItemInsertAfterClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0)
            {
                InsertAfter();
                textBoxListViewText.Focus();
            }
        }

        private void InsertAfter()
        {
            var format = GetCurrentSubtitleFormat();
            bool useExtraForStyle = format.HasStyleSupport;
            var styles = new List<string>();
            if (format.GetType() == typeof(AdvancedSubStationAlpha) || format.GetType() == typeof(SubStationAlpha))
                styles = AdvancedSubStationAlpha.GetStylesFromHeader(_subtitle.Header);
            else if (format.GetType() == typeof(TimedText10))
                styles = TimedText10.GetStylesFromHeader(_subtitle.Header);
            else if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                styles = Sami.GetStylesFromHeader(_subtitle.Header);
            string style = "Default";
            if (styles.Count > 0)
                style = styles[0];

            MakeHistoryForUndo(_language.BeforeInsertLine);

            int startNumber = 1;
            if (_subtitle.Paragraphs.Count > 0)
                startNumber = _subtitle.Paragraphs[0].Number;

            int firstSelectedIndex = 0;
            if (SubtitleListview1.SelectedItems.Count > 0)
                firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index + 1;

            var newParagraph = new Paragraph();
            if (useExtraForStyle)
                newParagraph.Extra = style;
            Paragraph prev = _subtitle.GetParagraphOrDefault(firstSelectedIndex - 1);
            Paragraph next = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
            if (prev != null)
            {
                int addMilliseconds = Configuration.Settings.General.MininumMillisecondsBetweenLines;
                if (addMilliseconds < 1)
                    addMilliseconds = 1;

                newParagraph.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + addMilliseconds;
                newParagraph.EndTime.TotalMilliseconds = newParagraph.StartTime.TotalMilliseconds + 2000;
                if (next != null && newParagraph.EndTime.TotalMilliseconds > next.StartTime.TotalMilliseconds)
                    newParagraph.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 1;
                if (newParagraph.StartTime.TotalMilliseconds > newParagraph.EndTime.TotalMilliseconds)
                    newParagraph.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + 1;
            }
            else if (next != null)
            {
                newParagraph.StartTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 2000;
                newParagraph.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 1;
            }
            else
            {
                newParagraph.StartTime.TotalMilliseconds = 1000;
                newParagraph.EndTime.TotalMilliseconds = 3000;
            }
            if (GetCurrentSubtitleFormat().IsFrameBased)
            {
                newParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                newParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
            }

            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                _subtitleAlternate.InsertParagraphInCorrectTimeOrder(new Paragraph(newParagraph));
                _subtitleAlternate.Renumber(1);
            }

            if (_networkSession != null)
            {
                _networkSession.TimerStop();
                NetworkGetSendUpdates(new List<int>(), firstSelectedIndex, newParagraph);
            }
            else
            {
                _subtitle.Paragraphs.Insert(firstSelectedIndex, newParagraph);
                _subtitle.Renumber(startNumber);
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            }
            SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
            ShowSource();
            ShowStatus(_language.LineInserted);
        }

        private void SubtitleListView1SelectedIndexChange()
        {
            StopAutoDuration();
            ShowLineInformationListView();
            if (_subtitle.Paragraphs.Count > 0)
            {
                int firstSelectedIndex = 0;
                if (SubtitleListview1.SelectedItems.Count > 0)
                    firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                if (_subtitleListViewIndex >= 0)
                {
                    if (_subtitleListViewIndex == firstSelectedIndex)
                        return;

                    bool showSource = false;

                    Paragraph last = _subtitle.GetParagraphOrDefault(_subtitleListViewIndex);
                    if (textBoxListViewText.Text != last.Text)
                    {
                        last.Text = textBoxListViewText.Text.TrimEnd();
                        SubtitleListview1.SetText(_subtitleListViewIndex, last.Text);
                        showSource = true;
                    }

                    TimeCode startTime = timeUpDownStartTime.TimeCode;
                    if (startTime != null)
                    {
                        if (last.StartTime.TotalMilliseconds != startTime.TotalMilliseconds)
                        {
                            double dur = last.Duration.TotalMilliseconds;
                            last.StartTime.TotalMilliseconds = startTime.TotalMilliseconds;
                            last.EndTime.TotalMilliseconds = startTime.TotalMilliseconds + dur;
                            SubtitleListview1.SetStartTime(_subtitleListViewIndex, last);
                            showSource = true;
                        }
                    }

                    double duration = GetDurationInMilliseconds();
                    if (duration > 0 && duration < 100000 && duration != last.Duration.TotalMilliseconds)
                    {
                        last.EndTime.TotalMilliseconds = last.StartTime.TotalMilliseconds + duration;
                        SubtitleListview1.SetDuration(_subtitleListViewIndex, last);
                        showSource = true;
                    }

                    if (showSource)
                    {
                        MakeHistoryForUndoOnlyIfNotResent(_language.BeforeLineUpdatedInListView);
                        ShowSource();
                    }
                }

                Paragraph p = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (p != null)
                {
                    InitializeListViewEditBox(p);
                    _subtitleListViewIndex = firstSelectedIndex;
                    _oldSelectedParagraph = new Paragraph(p);
                    UpdateListViewTextInfo(labelTextLineLengths, labelSingleLine, labelTextLineTotal, labelCharactersPerSecond, p, textBoxListViewText);

                    if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                    {
                        InitializeListViewEditBoxAlternate(p, firstSelectedIndex);
                        labelAlternateCharactersPerSecond.Left = textBoxListViewTextAlternate.Left + (textBoxListViewTextAlternate.Width - labelAlternateCharactersPerSecond.Width);
                        labelTextAlternateLineTotal.Left = textBoxListViewTextAlternate.Left + (textBoxListViewTextAlternate.Width - labelTextAlternateLineTotal.Width);
                    }
                }
            }
        }

        private void SubtitleListview1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_makeHistoryPaused)
            {
                if (_listViewTextUndoLast != null && _listViewTextUndoIndex >= 0 && _subtitle.Paragraphs.Count > _listViewTextUndoIndex &&
                    _subtitle.Paragraphs[_listViewTextUndoIndex].Text.TrimEnd() != _listViewTextUndoLast.TrimEnd())
                {
                    MakeHistoryForUndo(Configuration.Settings.Language.General.Text + ": " + _listViewTextUndoLast.TrimEnd() + " -> " + _subtitle.Paragraphs[_listViewTextUndoIndex].Text.TrimEnd(), false);
                    _subtitle.HistoryItems[_subtitle.HistoryItems.Count - 1].Subtitle.Paragraphs[_listViewTextUndoIndex].Text = _listViewTextUndoLast;
                    if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null)
                    {
                        var original = Utilities.GetOriginalParagraph(_listViewTextUndoIndex, _subtitle.Paragraphs[_listViewTextUndoIndex], _subtitleAlternate.Paragraphs);
                        var idx = _subtitleAlternate.GetIndex(original);
                        if (idx >= 0)
                            _subtitle.HistoryItems[_subtitle.HistoryItems.Count - 1].OriginalSubtitle.Paragraphs[idx].Text = _listViewAlternateTextUndoLast;
                    }
                }
                else if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _listViewAlternateTextUndoLast != null &&
                        _subtitle.Paragraphs.Count > _listViewTextUndoIndex && _listViewTextUndoIndex >= 0)
                {
                    var original = Utilities.GetOriginalParagraph(_listViewTextUndoIndex, _subtitle.Paragraphs[_listViewTextUndoIndex], _subtitleAlternate.Paragraphs);
                    if (original != null && original.Text.TrimEnd() != _listViewAlternateTextUndoLast.TrimEnd())
                    {
                        var idx = _subtitleAlternate.GetIndex(original);
                        if (idx >= 0)
                        {
                            MakeHistoryForUndo(Configuration.Settings.Language.General.Text + ": " + _listViewAlternateTextUndoLast.TrimEnd() + " -> " + original.Text.TrimEnd(), false);
                            _subtitle.HistoryItems[_subtitle.HistoryItems.Count - 1].OriginalSubtitle.Paragraphs[idx].Text = _listViewAlternateTextUndoLast;
                        }
                    }
                }
            }

            _listViewTextUndoIndex = -1;
            SubtitleListView1SelectedIndexChange();
        }

        private void ShowLineInformationListView()
        {
            if (SubtitleListview1.SelectedItems.Count == 1)
                toolStripSelected.Text = string.Format("{0}/{1}", SubtitleListview1.SelectedItems[0].Index + 1, SubtitleListview1.Items.Count);
            else
                toolStripSelected.Text = string.Format(_language.XLinesSelected, SubtitleListview1.SelectedItems.Count);
        }

        private void UpdateListViewTextCharactersPerSeconds(Label charsPerSecond, Paragraph paragraph)
        {
            if (paragraph.Duration.TotalSeconds > 0)
            {
                double charactersPerSecond = Utilities.GetCharactersPerSecond(paragraph);
                if (charactersPerSecond > Configuration.Settings.General.SubtitleMaximumCharactersPerSeconds + 7)
                    charsPerSecond.ForeColor = System.Drawing.Color.Red;
                else if (charactersPerSecond > Configuration.Settings.General.SubtitleMaximumCharactersPerSeconds)
                    charsPerSecond.ForeColor = System.Drawing.Color.Orange;
                else
                    charsPerSecond.ForeColor = System.Drawing.Color.Black;
                charsPerSecond.Text = string.Format(_language.CharactersPerSecond, charactersPerSecond);
            }
            else
            {
                charsPerSecond.ForeColor = System.Drawing.Color.Red;
                charsPerSecond.Text = string.Format(_language.CharactersPerSecond, _languageGeneral.NotAvailable);
            }
        }

        private void UpdateListViewTextInfo(Label lineLengths, Label singleLine, Label lineTotal, Label charactersPerSecond, Paragraph paragraph, TextBox textBox)
        {
            if (paragraph == null)
                return;
            bool textBoxHasFocus = textBox.Focused;
            string text = paragraph.Text;
            lineLengths.Text = _languageGeneral.SingleLineLengths;
            singleLine.Left = lineLengths.Left + lineLengths.Width - 6;
            Utilities.GetLineLengths(singleLine, text);

            buttonSplitLine.Visible = false;
            string s = Utilities.RemoveHtmlTags(text).Replace(Environment.NewLine, string.Empty); // we don't count new line in total length... correct?
            if (s.Length < Configuration.Settings.General.SubtitleLineMaximumLength * 1.9)
            {
                lineTotal.ForeColor = Color.Black;
                if (!textBoxHasFocus)
                    lineTotal.Text = string.Format(_languageGeneral.TotalLengthX, s.Length);
            }
            else if (s.Length < Configuration.Settings.General.SubtitleLineMaximumLength * 2.1)
            {
                lineTotal.ForeColor = Color.Orange;
                if (!textBoxHasFocus)
                    lineTotal.Text = string.Format(_languageGeneral.TotalLengthX, s.Length);
            }
            else
            {
                lineTotal.ForeColor = Color.Red;
                if (!textBoxHasFocus)
                    lineTotal.Text = string.Format(_languageGeneral.TotalLengthXSplitLine, s.Length);
                if (buttonUnBreak.Visible)
                {
                    if (!textBoxHasFocus)
                        lineTotal.Text = string.Format(_languageGeneral.TotalLengthX, s.Length);
                    buttonSplitLine.Visible = true;
                }
            }
            UpdateListViewTextCharactersPerSeconds(charactersPerSecond, paragraph);
            charactersPerSecond.Left = textBox.Left + (textBox.Width - labelCharactersPerSecond.Width);
            lineTotal.Left = textBox.Left + (textBox.Width - lineTotal.Width);
            FixVerticalScrollBars(textBox);
        }

        private void ButtonNextClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0)
            {
                int firstSelectedIndex = 0;
                if (SubtitleListview1.SelectedItems.Count > 0)
                    firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                firstSelectedIndex++;
                Paragraph p = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (p != null)
                    SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
            }
        }

        private void ButtonPreviousClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0)
            {
                int firstSelectedIndex = 1;
                if (SubtitleListview1.SelectedItems.Count > 0)
                    firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                firstSelectedIndex--;
                Paragraph p = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (p != null)
                    SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
            }
        }

        private string RemoveSsaStyle(string text)
        {
            int indexOfBegin = text.IndexOf("{");
            while (indexOfBegin >= 0 && text.IndexOf("}") > indexOfBegin)
            {
                int indexOfEnd = text.IndexOf("}");
                text = text.Remove(indexOfBegin, (indexOfEnd - indexOfBegin) + 1);
                indexOfBegin = text.IndexOf("{");
            }
            return text;
        }

        private void NormalToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                MakeHistoryForUndo(_language.BeforeSettingFontToNormal);

                bool isSsa = GetCurrentSubtitleFormat().FriendlyName == new SubStationAlpha().FriendlyName ||
                             GetCurrentSubtitleFormat().FriendlyName == new AdvancedSubStationAlpha().FriendlyName;

                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(item.Index);
                    if (p != null)
                    {
                        int indexOfEndBracket = p.Text.IndexOf("}");
                        if (p.Text.StartsWith("{\\") && indexOfEndBracket > 1 && indexOfEndBracket < 6)
                            p.Text = p.Text.Remove(0, indexOfEndBracket+1);
                        p.Text = Utilities.RemoveHtmlTags(p.Text);
                        p.Text = p.Text.Replace("♪", string.Empty);
                        if (isSsa)
                            p.Text = RemoveSsaStyle(p.Text);
                        SubtitleListview1.SetText(item.Index, p.Text);

                        if (_subtitleAlternate != null)
                        {
                            Paragraph original = Utilities.GetOriginalParagraph(item.Index, p, _subtitleAlternate.Paragraphs);
                            if (original != null)
                            {
                                original.Text = Utilities.RemoveHtmlTags(original.Text);
                                original.Text = original.Text.Replace("♪", string.Empty);
                                if (isSsa)
                                    original.Text = RemoveSsaStyle(original.Text);
                                SubtitleListview1.SetAlternateText(item.Index, original.Text);
                            }
                        }
                    }
                }
                ShowSource();
                RefreshSelectedParagraph();
            }
        }

        private void ButtonAutoBreakClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count > 1)
            {
                MakeHistoryForUndo(_language.BeforeRemoveLineBreaksInSelectedLines);

                SubtitleListview1.BeginUpdate();
                foreach (int index in SubtitleListview1.SelectedIndices)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(index);
                    if (p.Text.Length > Configuration.Settings.General.SubtitleLineMaximumLength)
                    {
                        p.Text = Utilities.AutoBreakLine(p.Text);
                        SubtitleListview1.SetText(index, p.Text);
                    }

                    if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible && Configuration.Settings.General.AllowEditOfOriginalSubtitle)
                    {
                        var original = Utilities.GetOriginalParagraph(index, p, _subtitleAlternate.Paragraphs);
                        if (original != null)
                        {
                            if (original.Text.Length > Configuration.Settings.General.SubtitleLineMaximumLength)
                            {
                                original.Text = Utilities.AutoBreakLine(p.Text);
                                SubtitleListview1.SetAlternateText(index, original.Text);
                            }
                        }
                    }
                }
                SubtitleListview1.EndUpdate();
                RefreshSelectedParagraph();
            }
            else
            {
                if (textBoxListViewText.Text.Length > 0)
                    textBoxListViewText.Text = Utilities.AutoBreakLine(textBoxListViewText.Text);
                if (_subtitleAlternate != null && Configuration.Settings.General.AllowEditOfOriginalSubtitle && textBoxListViewTextAlternate.Text.Length > 0)
                    textBoxListViewTextAlternate.Text = Utilities.AutoBreakLine(textBoxListViewTextAlternate.Text);
            }

        }

        private void FixVerticalScrollBars(TextBox tb)
        {
            var lineCount = Utilities.CountTagInText(tb.Text, Environment.NewLine)+1;
            if (lineCount > 3)
                tb.ScrollBars = ScrollBars.Vertical;
            else
                tb.ScrollBars = ScrollBars.None;
        }

        private void TextBoxListViewTextTextChanged(object sender, EventArgs e)
        {
            if (_subtitleListViewIndex >= 0)
            {
                string text = textBoxListViewText.Text.TrimEnd();

                // update _subtitle + listview
                _subtitle.Paragraphs[_subtitleListViewIndex].Text = text;
                UpdateListViewTextInfo(labelTextLineLengths, labelSingleLine, labelTextLineTotal, labelCharactersPerSecond, _subtitle.Paragraphs[_subtitleListViewIndex], textBoxListViewText);
                SubtitleListview1.SetText(_subtitleListViewIndex, text);

                _listViewTextUndoIndex = _subtitleListViewIndex;
                labelStatus.Text = string.Empty;

                StartUpdateListSyntaxColoring();
                FixVerticalScrollBars(textBoxListViewText);
            }
        }

        private void TextBoxListViewTextAlternateTextChanged(object sender, EventArgs e)
        {
            if (_subtitleListViewIndex >= 0)
            {
                Paragraph p = _subtitle.GetParagraphOrDefault(_subtitleListViewIndex);
                if (p == null)
                    return;

                Paragraph original = Utilities.GetOriginalParagraph(_subtitleListViewIndex, p, _subtitleAlternate.Paragraphs);
                if (original != null)
                {
                    string text = textBoxListViewTextAlternate.Text.TrimEnd();

                    // update _subtitle + listview
                    original.Text = text;
                    UpdateListViewTextInfo(labelTextAlternateLineLengths, labelAlternateSingleLine, labelTextAlternateLineTotal, labelAlternateCharactersPerSecond, original, textBoxListViewTextAlternate);
                    SubtitleListview1.SetAlternateText(_subtitleListViewIndex, text);
                    _listViewTextUndoIndex = _subtitleListViewIndex;
                }
                labelStatus.Text = string.Empty;

                StartUpdateListSyntaxColoring();
                FixVerticalScrollBars(textBoxListViewTextAlternate);
            }

        }

        private void TextBoxListViewTextKeyDown(object sender, KeyEventArgs e)
        {
            _listViewTextTicks = DateTime.Now.Ticks;
            if (e.Modifiers == Keys.Shift && e.KeyCode == Keys.ShiftKey)
                return;
            if (e.Modifiers == Keys.Control && e.KeyCode == (Keys.LButton | Keys.ShiftKey))
                return;

            int numberOfNewLines = textBoxListViewText.Text.Length - textBoxListViewText.Text.Replace(Environment.NewLine, " ").Length;

            Utilities.CheckAutoWrap(textBoxListViewText, e, numberOfNewLines);

            if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None && numberOfNewLines > 1)
            {
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
            {
                textBoxListViewText.SelectAll();
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.R)
            {
                ButtonAutoBreakClick(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.U)
            {
                ButtonUnBreakClick(null, null);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.I)
            {
                if (textBoxListViewText.SelectionLength == 0)
                {
                    string tag = "i";
                    if (textBoxListViewText.Text.Contains("<" + tag + ">"))
                    {
                        textBoxListViewText.Text = textBoxListViewText.Text.Replace("<" + tag + ">", string.Empty);
                        textBoxListViewText.Text = textBoxListViewText.Text.Replace("</" + tag + ">", string.Empty);
                    }
                    else
                    {
                        textBoxListViewText.Text = string.Format("<{0}>{1}</{0}>", tag, textBoxListViewText.Text);
                    }
                    //SubtitleListview1.SetText(i, textBoxListViewText.Text);
                }
                else
                {
                    TextBoxListViewToggleTag("i");
                }
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.D)
            {
                textBoxListViewText.SelectionLength = 0;
                e.SuppressKeyPress = true;
            }
            else if (_mainTextBoxSplitAtCursor == e.KeyData)
            {
                ToolStripMenuItemSplitTextAtCursorClick(null, null);
                e.SuppressKeyPress = true;
            }

            // last key down in text
            _lastTextKeyDownTicks = DateTime.Now.Ticks;

            UpdatePositionAndTotalLength(labelTextLineTotal, textBoxListViewText);
        }

        private void SplitLineToolStripMenuItemClick(object sender, EventArgs e)
        {
            SplitSelectedParagraph(null, null);
        }

        private void SplitSelectedParagraph(double? splitSeconds, int? textIndex)
        {
            int? alternateTextIndex = null;
            if (textBoxListViewTextAlternate.Focused)
            {
                alternateTextIndex = textIndex;
                textIndex = null;
            }

            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                MakeHistoryForUndo(_language.BeforeSplitLine);

                int startNumber = _subtitle.Paragraphs[0].Number;
                int firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                Paragraph currentParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                var newParagraph = new Paragraph();

                string oldText = currentParagraph.Text;
                string[] lines = currentParagraph.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (textIndex != null && textIndex.Value > 2 && textIndex.Value < oldText.Length -2)
                {
                    string a = oldText.Substring(0, textIndex.Value).Trim();
                    string b = oldText.Substring(textIndex.Value).Trim();
                    if (oldText.TrimStart().StartsWith("<i>") && oldText.TrimEnd().EndsWith("</i>") &&
                        Utilities.CountTagInText(oldText, "<i>") == 1 && Utilities.CountTagInText(oldText, "</i>") == 1)
                    {
                        a = a + "</i>";
                        b = "<i>" + b;
                    }
                    if (a.StartsWith("-") && (a.EndsWith(".") || a.EndsWith("!") || a.EndsWith("?")) &&
                        b.StartsWith("-") && (b.EndsWith(".") || b.EndsWith("!") || b.EndsWith("?")))
                    {
                        a = a.TrimStart('-').TrimStart();
                        b = b.TrimStart('-').TrimStart();
                    }
                    if (a.StartsWith("<i>-") && (a.EndsWith(".</i>") || a.EndsWith("!</i>") || a.EndsWith("?</i>")) &&
                        b.StartsWith("<i>-") && (b.EndsWith(".</i>") || b.EndsWith("!</i>") || b.EndsWith("?</i>")))
                    {
                        a = a.Remove(3,1).Replace("  ", " ");
                        b = b.Remove(3, 1).Replace("  ", " ");
                    }

                    currentParagraph.Text = Utilities.AutoBreakLine(a);
                    newParagraph.Text = Utilities.AutoBreakLine(b);
                }
                else
                {
                    if (lines.Length == 2 && (lines[0].EndsWith(".") || lines[0].EndsWith("!") || lines[0].EndsWith("?")))
                    {
                        currentParagraph.Text = Utilities.AutoBreakLine(lines[0]);
                        newParagraph.Text = Utilities.AutoBreakLine(lines[1]);
                        if (lines[0].Length > 2 && lines[0][0] == '-' && lines[0][1] != '-' &&
                            lines[1].Length > 2 && lines[1][0] == '-' && lines[1][1] != '-')
                        {
                            currentParagraph.Text = currentParagraph.Text.TrimStart('-').Trim();
                            newParagraph.Text = newParagraph.Text.TrimStart('-').Trim();
                        }
                    }
                    else if (lines.Length == 2 && (lines[0].EndsWith(".</i>") || lines[0].EndsWith("!</i>") || lines[0].EndsWith("?</i>")))
                    {
                        currentParagraph.Text = Utilities.AutoBreakLine(lines[0]);
                        newParagraph.Text = Utilities.AutoBreakLine(lines[1]);
                        if (lines[0].Length > 5 && lines[0].StartsWith("<i>-") && lines[0][4] != '-' &&
                            lines[1].Length > 5 && lines[1].StartsWith("<i>-") && lines[1][4] != '-')
                        {
                            currentParagraph.Text = currentParagraph.Text.Remove(3, 1);
                            if (currentParagraph.Text[3] == ' ')
                                currentParagraph.Text = currentParagraph.Text.Remove(3, 1);

                            newParagraph.Text = newParagraph.Text.Remove(3, 1);
                            if (newParagraph.Text[3] == ' ')
                                newParagraph.Text = newParagraph.Text.Remove(3, 1);
                        }
                    }
                    else
                    {
                        string s = Utilities.AutoBreakLine(currentParagraph.Text, 5, Configuration.Settings.General.SubtitleLineMaximumLength * 2, Configuration.Settings.Tools.MergeLinesShorterThan);
                        lines = s.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length == 2)
                        {
                            if (Utilities.CountTagInText(s, "<i>") == 1 && lines[0].StartsWith("<i>") && lines[1].EndsWith("</i>"))
                            {
                                lines[0] += "</i>";
                                lines[1] = "<i>" + lines[1];
                            }
                            currentParagraph.Text = Utilities.AutoBreakLine(lines[0]);
                            newParagraph.Text = Utilities.AutoBreakLine(lines[1]);
                        }
                    }
                }

                double startFactor = 0;
                double middle = currentParagraph.StartTime.TotalMilliseconds + (currentParagraph.Duration.TotalMilliseconds / 2);
                if (Utilities.RemoveHtmlTags(oldText).Trim().Length > 0)
                {
                    startFactor = (double)Utilities.RemoveHtmlTags(currentParagraph.Text).Length / Utilities.RemoveHtmlTags(oldText).Length;
                    if (startFactor < 0.20)
                        startFactor = 0.20;
                    if (startFactor > 0.80)
                        startFactor = 0.80;
                    middle = currentParagraph.StartTime.TotalMilliseconds + (currentParagraph.Duration.TotalMilliseconds * startFactor);
                }

                if (splitSeconds.HasValue && splitSeconds.Value > (currentParagraph.StartTime.TotalSeconds + 0.2) && splitSeconds.Value < (currentParagraph.EndTime.TotalSeconds - 0.2))
                    middle = splitSeconds.Value * 1000.0;
                newParagraph.EndTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds;
                currentParagraph.EndTime.TotalMilliseconds = middle;
                newParagraph.StartTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds + 1;
                if (Configuration.Settings.General.MininumMillisecondsBetweenLines > 0)
                {
                    Paragraph next = _subtitle.GetParagraphOrDefault(firstSelectedIndex+1);
                    if (next == null || next.StartTime.TotalMilliseconds > newParagraph.EndTime.TotalMilliseconds + Configuration.Settings.General.MininumMillisecondsBetweenLines + Configuration.Settings.General.MininumMillisecondsBetweenLines)
                    {
                        newParagraph.StartTime.TotalMilliseconds += Configuration.Settings.General.MininumMillisecondsBetweenLines;
                        newParagraph.EndTime.TotalMilliseconds += Configuration.Settings.General.MininumMillisecondsBetweenLines;
                    }
                    else
                    {
                        newParagraph.StartTime.TotalMilliseconds += Configuration.Settings.General.MininumMillisecondsBetweenLines;
                    }
                }

                if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                {
                    Paragraph originalCurrent = Utilities.GetOriginalParagraph(firstSelectedIndex, currentParagraph, _subtitleAlternate.Paragraphs);
                    if (originalCurrent != null)
                    {
                        originalCurrent.EndTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds;
                        Paragraph originalNew = new Paragraph(newParagraph);

                        lines = originalCurrent.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                        oldText = originalCurrent.Text;
                        if (alternateTextIndex != null && alternateTextIndex.Value > 2 && alternateTextIndex.Value < oldText.Length - 2)
                        {
                            originalCurrent.Text = Utilities.AutoBreakLine(oldText.Substring(0, alternateTextIndex.Value).Trim());
                            originalNew.Text = Utilities.AutoBreakLine(oldText.Substring(alternateTextIndex.Value).Trim());
                            lines = new string[0];
                        }
                        else if (lines.Length == 2 && (lines[0].EndsWith(".") || lines[0].EndsWith("!") || lines[0].EndsWith("?")))
                        {
                            string a = lines[0].Trim();
                            string b = lines[1].Trim();
                            if (oldText.TrimStart().StartsWith("<i>") && oldText.TrimEnd().EndsWith("</i>") &&
                                Utilities.CountTagInText(oldText, "<i>") == 1 && Utilities.CountTagInText(oldText, "</i>") == 1)
                            {
                                a = a + "</i>";
                                b = "<i>" + b;
                            }
                            if (a.StartsWith("-") && (a.EndsWith(".") || a.EndsWith("!") || a.EndsWith("?")) &&
                                b.StartsWith("-") && (b.EndsWith(".") || b.EndsWith("!") || b.EndsWith("?")))
                            {
                                a = a.TrimStart('-').TrimStart();
                                b = b.TrimStart('-').TrimStart();
                            }
                            if (a.StartsWith("<i>-") && (a.EndsWith(".</i>") || a.EndsWith("!</i>") || a.EndsWith("?</i>")) &&
                                b.StartsWith("<i>-") && (b.EndsWith(".</i>") || b.EndsWith("!</i>") || b.EndsWith("?</i>")))
                            {
                                a = a.Remove(3, 1).Replace("  ", " ");
                                b = b.Remove(3, 1).Replace("  ", " ");
                            }

                            lines[0] = a;
                            lines[1] = b;
                            originalCurrent.Text = Utilities.AutoBreakLine(a);
                            originalNew.Text = Utilities.AutoBreakLine(b);
                        }
                        else
                        {
                            string s = Utilities.AutoBreakLine(originalCurrent.Text, 5, Configuration.Settings.General.SubtitleLineMaximumLength * 2, Configuration.Settings.Tools.MergeLinesShorterThan);
                            lines = s.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        }
                        if (lines.Length == 2)
                        {
                            originalCurrent.Text = Utilities.AutoBreakLine(lines[0]);
                            originalNew.Text = Utilities.AutoBreakLine(lines[1]);
                        }
                        _subtitleAlternate.InsertParagraphInCorrectTimeOrder(originalNew);
                        _subtitleAlternate.Renumber(1);
                    }
                }

                if (_networkSession != null)
                {
                    _networkSession.TimerStop();
                    NetworkGetSendUpdates(new List<int>(), firstSelectedIndex, newParagraph);
                }
                else
                {
                    if (GetCurrentSubtitleFormat().IsFrameBased)
                    {
                        if (currentParagraph != null)
                        {
                            currentParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                            currentParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                        }
                        newParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                        newParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                    }
                    _subtitle.Paragraphs.Insert(firstSelectedIndex + 1, newParagraph);
                    _subtitle.Renumber(startNumber);
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                }
                SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
                ShowSource();
                ShowStatus(_language.LineSplitted);
                SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                RefreshSelectedParagraph();
                SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex, true);
            }
        }

        private void MergeBeforeToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                int startNumber = _subtitle.Paragraphs[0].Number;
                int firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                Paragraph prevParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex-1);
                Paragraph currentParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex);

                if (prevParagraph != null && currentParagraph != null)
                {
                    SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                    MakeHistoryForUndo(_language.BeforeMergeLines);

                    if (_subtitleAlternate != null)
                    {
                        Paragraph prevOriginal = Utilities.GetOriginalParagraph(firstSelectedIndex, prevParagraph, _subtitleAlternate.Paragraphs);
                        Paragraph currentOriginal = Utilities.GetOriginalParagraph(firstSelectedIndex + 1, currentParagraph, _subtitleAlternate.Paragraphs);

                        if (currentOriginal != null)
                        {
                            if (prevOriginal == null)
                            {
                                currentOriginal.StartTime = prevParagraph.StartTime;
                                currentOriginal.EndTime = currentParagraph.EndTime;
                            }
                            else
                            {
                                prevOriginal.Text = prevOriginal.Text.Replace(Environment.NewLine, " ");
                                prevOriginal.Text += Environment.NewLine + currentOriginal.Text.Replace(Environment.NewLine, " ");
                                prevOriginal.Text = ChangeAllLinesItalictoSingleItalic(prevOriginal.Text);
                                prevOriginal.Text = Utilities.AutoBreakLine(prevOriginal.Text);
                                prevOriginal.EndTime = currentOriginal.EndTime;
                                _subtitleAlternate.Paragraphs.Remove(currentOriginal);
                            }
                            _subtitleAlternate.Renumber(1);
                        }
                    }

                    prevParagraph.Text = prevParagraph.Text.Replace(Environment.NewLine, " ");
                    prevParagraph.Text += Environment.NewLine + currentParagraph.Text.Replace(Environment.NewLine, " ");
                    prevParagraph.Text = Utilities.AutoBreakLine(prevParagraph.Text);

//                    prevParagraph.EndTime.TotalMilliseconds = prevParagraph.EndTime.TotalMilliseconds + currentParagraph.Duration.TotalMilliseconds;
                    prevParagraph.EndTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds;

                    if (_networkSession != null)
                    {
                        _networkSession.TimerStop();
                        List<int> deleteIndices = new List<int>();
                        deleteIndices.Add(_subtitle.GetIndex(currentParagraph));
                        NetworkGetSendUpdates(deleteIndices, 0, null);
                    }
                    else
                    {
                        _subtitle.Paragraphs.Remove(currentParagraph);
                        _subtitle.Renumber(startNumber);
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        SubtitleListview1.Items[firstSelectedIndex-1].Selected = true;
                    }
                    SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex - 1, true);
                    ShowSource();
                    ShowStatus(_language.LinesMerged);
                    SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                    RefreshSelectedParagraph();
                }
            }
        }

        private void MergeSelectedLines()
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 1)
            {
                var sb = new StringBuilder();
                var deleteIndices = new List<int>();
                bool first = true;
                int firstIndex = 0;
                double durationMilliseconds = 0;
                int next = 0;
                foreach (int index in SubtitleListview1.SelectedIndices)
                {
                    if (first)
                    {
                        firstIndex = index;
                        next = index + 1;
                    }
                    else
                    {
                        deleteIndices.Add(index);
                        if (next != index)
                            return;
                        next++;
                    }
                    first = false;
                    sb.AppendLine(_subtitle.Paragraphs[index].Text);
                    durationMilliseconds += _subtitle.Paragraphs[index].Duration.TotalMilliseconds;
                }

                if (sb.Length > 200)
                    return;

                SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                MakeHistoryForUndo(_language.BeforeMergeLines);

                Paragraph currentParagraph = _subtitle.Paragraphs[firstIndex];
                string text = sb.ToString();
                text = Utilities.FixInvalidItalicTags(text);
                text = ChangeAllLinesItalictoSingleItalic(text);
                text = Utilities.AutoBreakLine(text);
                currentParagraph.Text = text;

                //display time
                currentParagraph.EndTime.TotalMilliseconds = currentParagraph.StartTime.TotalMilliseconds + durationMilliseconds;

                Paragraph nextParagraph = _subtitle.GetParagraphOrDefault(next);
                if (nextParagraph != null && currentParagraph.EndTime.TotalMilliseconds > nextParagraph.StartTime.TotalMilliseconds && currentParagraph.StartTime.TotalMilliseconds < nextParagraph.StartTime.TotalMilliseconds)
                {
                    currentParagraph.EndTime.TotalMilliseconds = nextParagraph.StartTime.TotalMilliseconds - 1;
                }

                // original subtitle
                if (_subtitleAlternate != null && Configuration.Settings.General.AllowEditOfOriginalSubtitle)
                {
                    Paragraph original = Utilities.GetOriginalParagraph(firstIndex, currentParagraph, _subtitleAlternate.Paragraphs);
                    if (original != null)
                    {
                        var originalTexts = new StringBuilder();
                        originalTexts.Append(original.Text + " ");
                        for (int i = 0; i < deleteIndices.Count; i++)
                        {
                            Paragraph originalNext = Utilities.GetOriginalParagraph(deleteIndices[i], _subtitle.Paragraphs[deleteIndices[i]], _subtitleAlternate.Paragraphs);
                            if (originalNext != null)
                                originalTexts.Append(originalNext.Text + " ");
                        }
                        for (int i = deleteIndices.Count - 1; i >= 0; i--)
                        {
                            Paragraph originalNext = Utilities.GetOriginalParagraph(deleteIndices[i], _subtitle.Paragraphs[deleteIndices[i]], _subtitleAlternate.Paragraphs);
                            if (originalNext != null)
                                _subtitleAlternate.Paragraphs.Remove(originalNext);
                        }
                        original.Text = originalTexts.ToString().Replace("  ", " ");
                        original.Text = original.Text.Replace(Environment.NewLine, " ");
                        original.Text = ChangeAllLinesItalictoSingleItalic(original.Text);
                        original.Text = Utilities.AutoBreakLine(original.Text);
                        original.EndTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds;
                        _subtitleAlternate.Renumber(1);
                    }
                }

                if (_networkSession != null)
                {
                    _networkSession.TimerStop();
                    _networkSession.UpdateLine(firstIndex, currentParagraph);
                    NetworkGetSendUpdates(deleteIndices, 0, null);
                }
                else
                {
                    for (int i = deleteIndices.Count - 1; i >= 0; i--)
                        _subtitle.Paragraphs.RemoveAt(deleteIndices[i]);
                    _subtitle.Renumber(1);
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                }
                ShowSource();
                ShowStatus(_language.LinesMerged);
                SubtitleListview1.SelectIndexAndEnsureVisible(firstIndex, true);
                SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                RefreshSelectedParagraph();
            }
        }

        private static string ChangeAllLinesItalictoSingleItalic(string text)
        {
            bool allLinesStartAndEndsWithItalic = text.Contains("<i>");
            foreach (string line in text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.Trim().StartsWith("<i>") || !line.Trim().EndsWith("</i>"))
                    allLinesStartAndEndsWithItalic = false;
            }
            if (allLinesStartAndEndsWithItalic)
            {
                text = text.Replace("<i>", string.Empty).Replace("</i>", string.Empty).Trim();
                text = "<i>" + text + "</i>";
            }
            return text;
        }

        private void MergeAfterToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                if (SubtitleListview1.SelectedItems.Count > 2)
                {
                    MergeSelectedLines();
                    return;
                }

                MergeWithLineAfter(false);
            }
        }

        private void MergeWithLineAfter(bool insertDash)
        {
            int startNumber = _subtitle.Paragraphs[0].Number;
            int firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

            Paragraph currentParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
            Paragraph nextParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex + 1);

            if (nextParagraph != null && currentParagraph != null)
            {
                SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                MakeHistoryForUndo(_language.BeforeMergeLines);

                if (_subtitleAlternate != null)
                {
                    Paragraph original = Utilities.GetOriginalParagraph(firstSelectedIndex, currentParagraph, _subtitleAlternate.Paragraphs);
                    Paragraph originalNext = Utilities.GetOriginalParagraph(firstSelectedIndex + 1, nextParagraph, _subtitleAlternate.Paragraphs);

                    if (originalNext != null)
                    {
                        if (original == null)
                        {
                            originalNext.StartTime.TotalMilliseconds = currentParagraph.StartTime.TotalMilliseconds;
                            originalNext.EndTime.TotalMilliseconds = nextParagraph.EndTime.TotalMilliseconds;
                        }
                        else
                        {
                            if (insertDash)
                            {
                                string s = Utilities.UnbreakLine(original.Text);
                                if (s.StartsWith("-") || s.StartsWith("<i>-"))
                                    original.Text = s;
                                else if (s.StartsWith("<i>"))
                                    original.Text = s.Insert(3, "- ");
                                else
                                    original.Text = "- " + s;

                                s = Utilities.UnbreakLine(originalNext.Text);
                                if (s.StartsWith("-") || s.StartsWith("<i>-"))
                                    original.Text += Environment.NewLine + s;
                                else if (s.StartsWith("<i>"))
                                    original.Text += Environment.NewLine + s.Insert(3, "- ");
                                else
                                    original.Text += Environment.NewLine + "- " + s;

                                original.Text = original.Text.Replace("</i>" + Environment.NewLine + "<i>", Environment.NewLine);
                            }
                            else
                            {

                                original.Text = original.Text.Replace(Environment.NewLine, " ");
                                original.Text += Environment.NewLine + originalNext.Text.Replace(Environment.NewLine, " ");
                                original.Text = ChangeAllLinesItalictoSingleItalic(original.Text);
                                original.Text = Utilities.AutoBreakLine(original.Text);
                            }
                            original.EndTime = originalNext.EndTime;
                            _subtitleAlternate.Paragraphs.Remove(originalNext);
                        }
                        _subtitleAlternate.Renumber(1);
                    }
                }

                if (insertDash)
                {
                    string s = Utilities.UnbreakLine(currentParagraph.Text);
                    if (s.StartsWith("-") || s.StartsWith("<i>-"))
                        currentParagraph.Text = s;
                    else if (s.StartsWith("<i>"))
                        currentParagraph.Text = s.Insert(3, "- ");
                    else
                        currentParagraph.Text = "- " + s;

                    s = Utilities.UnbreakLine(nextParagraph.Text);
                    if (s.StartsWith("-") || s.StartsWith("<i>-"))
                        currentParagraph.Text += Environment.NewLine + s;
                    else if (s.StartsWith("<i>"))
                        currentParagraph.Text += Environment.NewLine + s.Insert(3, "- ");
                    else
                        currentParagraph.Text += Environment.NewLine + "- " + s;

                    currentParagraph.Text = currentParagraph.Text.Replace("</i>" + Environment.NewLine + "<i>", Environment.NewLine);
                }
                else
                {
                    currentParagraph.Text = currentParagraph.Text.Replace(Environment.NewLine, " ");
                    currentParagraph.Text += Environment.NewLine + nextParagraph.Text.Replace(Environment.NewLine, " ");
                    currentParagraph.Text = ChangeAllLinesItalictoSingleItalic(currentParagraph.Text);
                    currentParagraph.Text = Utilities.AutoBreakLine(currentParagraph.Text);
                }

                //currentParagraph.EndTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds + nextParagraph.Duration.TotalMilliseconds; //nextParagraph.EndTime;
                currentParagraph.EndTime.TotalMilliseconds = nextParagraph.EndTime.TotalMilliseconds;

                if (_networkSession != null)
                {
                    _networkSession.TimerStop();
                    _networkSession.UpdateLine(_subtitle.GetIndex(currentParagraph), currentParagraph);
                    List<int> deleteIndices = new List<int>();
                    deleteIndices.Add(_subtitle.GetIndex(nextParagraph));
                    NetworkGetSendUpdates(deleteIndices, 0, null);
                }
                else
                {
                    _subtitle.Paragraphs.Remove(nextParagraph);
                    _subtitle.Renumber(startNumber);
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                }
                ShowSource();
                ShowStatus(_language.LinesMerged);
                SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex);
                SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                RefreshSelectedParagraph();
                SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedIndex, true);
            }
        }

        private void UpdateStartTimeInfo(TimeCode startTime)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0 && startTime != null)
            {
                UpdateOverlapErrors(startTime);

                // update _subtitle + listview
                Paragraph p = _subtitle.Paragraphs[_subtitleListViewIndex];
                p.EndTime.TotalMilliseconds += (startTime.TotalMilliseconds - p.StartTime.TotalMilliseconds);
                p.StartTime = startTime;
                SubtitleListview1.SetStartTime(_subtitleListViewIndex, p);
                if (GetCurrentSubtitleFormat().IsFrameBased)
                {
                    p.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                    p.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                }

                StartUpdateListSyntaxColoring();
            }
        }

        private void StartUpdateListSyntaxColoring()
        {
            if (!_timerDoSyntaxColoring.Enabled)
                _timerDoSyntaxColoring.Start();
        }

        private void UpdateListSyntaxColoring()
        {
            if (_loading)
                return;

            if (!IsSubtitleLoaded || _subtitleListViewIndex < 0 || _subtitleListViewIndex >= _subtitle.Paragraphs.Count)
                return;

            SubtitleListview1.SyntaxColorLine(_subtitle.Paragraphs, _subtitleListViewIndex, _subtitle.Paragraphs[_subtitleListViewIndex]);
            Paragraph next = _subtitle.GetParagraphOrDefault(_subtitleListViewIndex + 1);
            if (next != null)
                SubtitleListview1.SyntaxColorLine(_subtitle.Paragraphs, _subtitleListViewIndex + 1, _subtitle.Paragraphs[_subtitleListViewIndex + 1]);
        }

        private void UpdateOverlapErrors(TimeCode startTime)
        {
            labelStartTimeWarning.Text = string.Empty;
            labelDurationWarning.Text = string.Empty;
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0 && startTime != null)
            {

                int firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                Paragraph prevParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex - 1);
                if (prevParagraph != null && prevParagraph.EndTime.TotalMilliseconds > startTime.TotalMilliseconds)
                    labelStartTimeWarning.Text = string.Format(_languageGeneral.OverlapPreviousLineX, prevParagraph.EndTime.TotalSeconds - startTime.TotalSeconds);

                Paragraph nextParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex + 1);
                if (nextParagraph != null)
                {
                    double durationMilliSeconds = GetDurationInMilliseconds();
                    if (startTime.TotalMilliseconds + durationMilliSeconds > nextParagraph.StartTime.TotalMilliseconds)
                    {
                        labelDurationWarning.Text = string.Format(_languageGeneral.OverlapX, ((startTime.TotalMilliseconds + durationMilliSeconds) - nextParagraph.StartTime.TotalMilliseconds) / 1000.0);
                    }

                    if (labelStartTimeWarning.Text.Length == 0 &&
                        startTime.TotalMilliseconds > nextParagraph.StartTime.TotalMilliseconds)
                    {
                        double di = (startTime.TotalMilliseconds - nextParagraph.StartTime.TotalMilliseconds) / 1000.0;
                        labelStartTimeWarning.Text = string.Format(_languageGeneral.OverlapNextX, di);
                    }
                    else if (numericUpDownDuration.Value < 0)
                    {
                        labelDurationWarning.Text = _languageGeneral.Negative;
                    }
                }
            }
        }

        private double GetDurationInMilliseconds()
        {
            if (Configuration.Settings.General.UseTimeFormatHHMMSSFF)
            {
                int seconds = (int)numericUpDownDuration.Value;
                int frames = (int)(Convert.ToDouble(numericUpDownDuration.Value) % 1.0 * 100.0);
                return seconds * 1000.0 + frames * (1000.0 / Configuration.Settings.General.CurrentFrameRate);
            }
            return ((double)numericUpDownDuration.Value * 1000.0);
        }

        private void SetDurationInSeconds(double seconds)
        {
            if (Configuration.Settings.General.UseTimeFormatHHMMSSFF)
            {
                int wholeSeconds = (int)seconds;
                int frames = SubtitleFormat.MillisecondsToFrames(seconds % 1.0 * 1000);
                int extraSeconds = (int)(frames / Configuration.Settings.General.CurrentFrameRate);
                int restFrames = (int)(frames % Configuration.Settings.General.CurrentFrameRate);
                numericUpDownDuration.Value = (decimal)(wholeSeconds + extraSeconds + restFrames / 100.0);
            }
            else
            {
                numericUpDownDuration.Value = (decimal)seconds;
            }
        }

        private void NumericUpDownDurationValueChanged(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                int firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                Paragraph currentParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (currentParagraph != null)
                {
                    UpdateOverlapErrors(timeUpDownStartTime.TimeCode);
                    UpdateListViewTextCharactersPerSeconds(labelCharactersPerSecond, currentParagraph);

                    // update _subtitle + listview
                    string oldDuration = currentParagraph.Duration.ToString();

                    var temp = new Paragraph(currentParagraph);

                    if (Configuration.Settings.General.UseTimeFormatHHMMSSFF)
                    {
                        int frames = (int)(Convert.ToDouble(numericUpDownDuration.Value) % 1.0 * 100.0);
                        if (frames > Configuration.Settings.General.CurrentFrameRate-1)
                        {
                            int seconds = (int)numericUpDownDuration.Value;
                            numericUpDownDuration.ValueChanged -= NumericUpDownDurationValueChanged;
                            int extraSeconds = (int)(frames / (Configuration.Settings.General.CurrentFrameRate-1));
                            int restFrames = (int)(frames % (Configuration.Settings.General.CurrentFrameRate-1));
                            if (frames == 99)
                                numericUpDownDuration.Value = (decimal)(seconds + (((int)(Configuration.Settings.General.CurrentFrameRate - 1)) / 100.0));
                            else
                                numericUpDownDuration.Value = (decimal)(seconds + extraSeconds + restFrames / 100.0);
                            numericUpDownDuration.ValueChanged += NumericUpDownDurationValueChanged;
                        }

                    }
                    temp.EndTime.TotalMilliseconds = currentParagraph.StartTime.TotalMilliseconds + GetDurationInMilliseconds();

                    MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.DisplayTimeAdjustedX, "#" + currentParagraph.Number + ": " + oldDuration + " -> " + temp.Duration));

                    currentParagraph.EndTime.TotalMilliseconds = temp.EndTime.TotalMilliseconds;
                    SubtitleListview1.SetDuration(firstSelectedIndex, currentParagraph);

                    if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                    {
                        Paragraph original = Utilities.GetOriginalParagraph(firstSelectedIndex, currentParagraph, _subtitleAlternate.Paragraphs);
                        if (original != null)
                        {
                            original.EndTime.TotalMilliseconds = currentParagraph.EndTime.TotalMilliseconds;
                        }
                    }

                    StartUpdateListSyntaxColoring();

                    if (GetCurrentSubtitleFormat().IsFrameBased)
                    {
                        currentParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                        currentParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                    }
                }
                labelStatus.Text = string.Empty;
                StartUpdateListSyntaxColoring();
            }
        }

        private void InitializeListViewEditBoxAlternate(Paragraph p, int firstSelectedIndex)
        {
            if (_subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                Paragraph original = Utilities.GetOriginalParagraph(firstSelectedIndex, p, _subtitleAlternate.Paragraphs);
                if (original == null)
                {
                    textBoxListViewTextAlternate.Enabled = false;
                    textBoxListViewTextAlternate.Text = string.Empty;
                    labelAlternateCharactersPerSecond.Text = string.Empty;
                }
                else
                {
                    textBoxListViewTextAlternate.Enabled = true;
                    textBoxListViewTextAlternate.TextChanged -= TextBoxListViewTextAlternateTextChanged;
                    textBoxListViewTextAlternate.Text = original.Text;
                    textBoxListViewTextAlternate.TextChanged += TextBoxListViewTextAlternateTextChanged;
                    UpdateListViewTextCharactersPerSeconds(labelAlternateCharactersPerSecond, p);
                    _listViewAlternateTextUndoLast = original.Text;
                }
            }
        }

        private void InitializeListViewEditBox(Paragraph p)
        {
            textBoxListViewText.TextChanged -= TextBoxListViewTextTextChanged;
            textBoxListViewText.Text = p.Text;
            textBoxListViewText.TextChanged += TextBoxListViewTextTextChanged;
            _listViewTextUndoLast = p.Text;

            timeUpDownStartTime.MaskedTextBox.TextChanged -= MaskedTextBoxTextChanged;
            timeUpDownStartTime.TimeCode = p.StartTime;
            timeUpDownStartTime.MaskedTextBox.TextChanged += MaskedTextBoxTextChanged;

            numericUpDownDuration.ValueChanged -= NumericUpDownDurationValueChanged;
            if (p.Duration.TotalSeconds > (double)numericUpDownDuration.Maximum)
                SetDurationInSeconds((double)numericUpDownDuration.Maximum);
            else
                SetDurationInSeconds(p.Duration.TotalSeconds);
            numericUpDownDuration.ValueChanged += NumericUpDownDurationValueChanged;

            UpdateOverlapErrors(timeUpDownStartTime.TimeCode);
            UpdateListViewTextCharactersPerSeconds(labelCharactersPerSecond, p);
            if (_subtitle != null && _subtitle.Paragraphs.Count > 0)
                textBoxListViewText.Enabled = true;

            StartUpdateListSyntaxColoring();
        }

        void MaskedTextBoxTextChanged(object sender, EventArgs e)
        {
            if (_subtitleListViewIndex >= 0)
            {
                MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.StarTimeAdjustedX, "#" + (_subtitleListViewIndex + 1).ToString() + ": " + timeUpDownStartTime.TimeCode.ToString()));

                int firstSelectedIndex = FirstSelectedIndex;
                Paragraph oldParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (oldParagraph != null)
                    oldParagraph = new Paragraph(oldParagraph);

                UpdateStartTimeInfo(timeUpDownStartTime.TimeCode);

                UpdateOriginalTimeCodes(oldParagraph);
                labelStatus.Text = string.Empty;
            }
        }

        private void UpdateOriginalTimeCodes(Paragraph currentPargraphBeforeChange)
        {
            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                int firstSelectedIndex = FirstSelectedIndex;
                Paragraph p = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (currentPargraphBeforeChange != null && p != null)
                {
                    Paragraph original = Utilities.GetOriginalParagraph(FirstSelectedIndex, currentPargraphBeforeChange, _subtitleAlternate.Paragraphs);
                    if (original != null)
                    {
                        original.StartTime.TotalMilliseconds = p.StartTime.TotalMilliseconds;
                        original.EndTime.TotalMilliseconds = p.EndTime.TotalMilliseconds;
                    }
                }
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _lastDoNotPrompt = string.Empty;
            ReloadFromSourceView();
            if (!ContinueNewOrExit())
            {
                e.Cancel = true;
            }
            else
            {
                if (_networkSession != null)
                {
                    try
                    {
                        _networkSession.TimerStop();
                        _networkSession.Leave();
                    }
                    catch
                    {
                    }
                }

                if (Configuration.Settings.General.StartRememberPositionAndSize && WindowState != FormWindowState.Minimized)
                {
                    Configuration.Settings.General.StartPosition = Left + ";" + Top;
                    if (WindowState == FormWindowState.Maximized)
                        Configuration.Settings.General.StartSize = "Maximized";
                    else
                        Configuration.Settings.General.StartSize = Width + ";" + Height;
                    Configuration.Settings.General.StartListViewWidth = splitContainer1.SplitterDistance;
                }
                else if (Configuration.Settings.General.StartRememberPositionAndSize)
                {
                    Configuration.Settings.General.StartListViewWidth = splitContainer1.SplitterDistance;
                }
                Configuration.Settings.General.AutoRepeatOn = checkBoxAutoRepeatOn.Checked;
                Configuration.Settings.General.AutoContinueOn = checkBoxAutoContinue.Checked;
                Configuration.Settings.General.SyncListViewWithVideoWhilePlaying = checkBoxSyncListViewWithVideoWhilePlaying.Checked;
                if (audioVisualizer != null)
                {
                    Configuration.Settings.General.ShowWaveform = audioVisualizer.ShowWaveform;
                    Configuration.Settings.General.ShowSpectrogram = audioVisualizer.ShowSpectrogram;
                }
                if (!string.IsNullOrEmpty(_fileName))
                    Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);

                SaveUndockedPositions();

                SaveListViewWidths();

                Configuration.Settings.Save();

                if (mediaPlayer.VideoPlayer != null)
                {
                    mediaPlayer.VideoPlayer.DisposeVideoPlayer();
                }

            }
        }

        private void SaveListViewWidths()
        {
            if (Configuration.Settings.General.ListViewColumsRememberSize)
            {
                Configuration.Settings.General.ListViewNumberWidth = SubtitleListview1.Columns[0].Width;
                Configuration.Settings.General.ListViewStartWidth = SubtitleListview1.Columns[1].Width;
                Configuration.Settings.General.ListViewEndWidth = SubtitleListview1.Columns[2].Width;
                Configuration.Settings.General.ListViewDurationWidth = SubtitleListview1.Columns[3].Width;
                Configuration.Settings.General.ListViewTextWidth = SubtitleListview1.Columns[4].Width;
            }
        }

        private void SaveUndockedPositions()
        {
            if (_videoPlayerUnDocked != null && !_videoPlayerUnDocked.IsDisposed)
                Configuration.Settings.General.UndockedVideoPosition = _videoPlayerUnDocked.Left.ToString() + ";" + _videoPlayerUnDocked.Top.ToString() + ";" + _videoPlayerUnDocked.Width + ";" + _videoPlayerUnDocked.Height;
            if (_waveFormUnDocked != null && !_waveFormUnDocked.IsDisposed)
                Configuration.Settings.General.UndockedWaveformPosition = _waveFormUnDocked.Left.ToString() + ";" + _waveFormUnDocked.Top.ToString() + ";" + _waveFormUnDocked.Width + ";" + _waveFormUnDocked.Height;
            if (_videoControlsUnDocked != null && !_videoControlsUnDocked.IsDisposed)
                Configuration.Settings.General.UndockedVideoControlsPosition = _videoControlsUnDocked.Left.ToString() + ";" + _videoControlsUnDocked.Top.ToString() + ";" + _videoControlsUnDocked.Width + ";" + _videoControlsUnDocked.Height;
        }

        private void ButtonUnBreakClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count > 1)
            {
                MakeHistoryForUndo(_language.BeforeRemoveLineBreaksInSelectedLines);

                SubtitleListview1.BeginUpdate();
                foreach (int index in SubtitleListview1.SelectedIndices)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(index);
                    p.Text = Utilities.UnbreakLine(p.Text);
                    SubtitleListview1.SetText(index, p.Text);
                }
                SubtitleListview1.EndUpdate();
                RefreshSelectedParagraph();
            }
            else
            {
                textBoxListViewText.Text = Utilities.UnbreakLine(textBoxListViewText.Text);
            }
        }

        private void TabControlSubtitleSelectedIndexChanged(object sender, EventArgs e)
        {
            var format = GetCurrentSubtitleFormat();
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
            {
                ShowSource();
                ShowSourceLineNumber();
                if (textBoxSource.CanFocus)
                    textBoxSource.Focus();

                // go to correct line in source view
                if (SubtitleListview1.SelectedItems.Count > 0)
                {
                    if (format.GetType() == typeof(SubRip))
                    {
                        Paragraph p = _subtitle.GetParagraphOrDefault(FirstSelectedIndex);
                        if (p != null)
                        {
                            string tc = p.StartTime.ToString() + " --> " + p.EndTime.ToString();
                            int start = textBoxSource.Text.IndexOf(tc);
                            if (start > 0)
                            {
                                textBoxSource.SelectionStart = start + tc.Length + Environment.NewLine.Length;
                                textBoxSource.SelectionLength = 0;
                                textBoxSource.ScrollToCaret();
                            }
                        }
                    }
                }
            }
            else
            {
                ReloadFromSourceView();
                ShowLineInformationListView();
                if (SubtitleListview1.CanFocus)
                    SubtitleListview1.Focus();

                // go to (select + focus) correct line in list view
                if (textBoxSource.SelectionStart > 0 && textBoxSource.TextLength > 30)
                {
                    if (format.GetType() == typeof(SubRip))
                    {
                        int pos = textBoxSource.SelectionStart;
                        if (pos + 35 < textBoxSource.TextLength)
                            pos += 35;
                        string s = textBoxSource.Text.Substring(0, pos);
                        int lastTimeCode = s.LastIndexOf(" --> "); // 00:02:26,407 --> 00:02:31,356
                        if (lastTimeCode > 14 && lastTimeCode + 16 >= s.Length)
                        {
                            s = s.Substring(0, lastTimeCode - 5);
                            lastTimeCode = s.LastIndexOf(" --> ");
                        }

                        if (lastTimeCode > 14 && lastTimeCode + 16 < s.Length)
                        {
                            string tc = s.Substring(lastTimeCode - 13, 30).Trim();
                            int index = 0;
                            foreach (Paragraph p in _subtitle.Paragraphs)
                            {
                                if (tc == p.StartTime.ToString() + " --> " + p.EndTime.ToString())
                                {
                                    SubtitleListview1.SelectIndexAndEnsureVisible(index, true);
                                    break;
                                }
                                index++;
                            }
                        }
                    }
                }
                else if (textBoxSource.SelectionStart == 0 && textBoxSource.TextLength > 30)
                {
                    SubtitleListview1.SelectIndexAndEnsureVisible(0, true);
                }
            }
        }

        private void ColorToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                if (colorDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    string color = Utilities.ColorToHex(colorDialog1.Color);

                    MakeHistoryForUndo(_language.BeforeSettingColor);

                    foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    {
                        Paragraph p = _subtitle.GetParagraphOrDefault(item.Index);
                        if (p != null)
                        {
                            SetFontColor(p, color);
                            SubtitleListview1.SetText(item.Index, p.Text);
                            if (_subtitleAlternate != null)
                            {
                                Paragraph original = Utilities.GetOriginalParagraph(item.Index, p, _subtitleAlternate.Paragraphs);
                                SetFontColor(original, color);
                                SubtitleListview1.SetAlternateText(item.Index, p.Text);
                            }
                        }
                    }
                    RefreshSelectedParagraph();
                }
            }
        }

        private void SetFontColor(Paragraph p, string color)
        {
            if (p == null)
                return;

            bool done = false;

            string s = p.Text;
            if (s.StartsWith("<font "))
            {
                int start = s.IndexOf("<font ");
                if (start >= 0)
                {
                    int end = s.IndexOf(">", start);
                    if (end > 0)
                    {
                        string f = s.Substring(start, end - start);
                        if (f.Contains(" face=") && !f.Contains(" color="))
                        {
                            start = s.IndexOf(" face=", start);
                            s = s.Insert(start, string.Format(" color=\"{0}\"", color));
                            p.Text = s;
                            done = true;
                        }
                        else if (f.Contains(" color="))
                        {
                            int colorStart = f.IndexOf(" color=");
                            if (s.IndexOf("\"", colorStart + " color=".Length + 1) > 0)
                                end = s.IndexOf("\"", colorStart + " color=".Length + 1);
                            s = s.Substring(0, colorStart) + string.Format(" color=\"{0}", color) + s.Substring(end);
                            p.Text = s;
                            done = true;
                        }
                    }
                }
            }

            if (!done)
                p.Text = string.Format("<font color=\"{0}\">{1}</font>", color, p.Text);
        }

        private void toolStripMenuItemFont_Click(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                if (fontDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeSettingFontName);

                    foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    {
                        Paragraph p = _subtitle.GetParagraphOrDefault(item.Index);
                        if (p != null)
                        {
                            SetFontName(p);
                            SubtitleListview1.SetText(item.Index, p.Text);
                            if (_subtitleAlternate != null)
                            {
                                Paragraph original = Utilities.GetOriginalParagraph(item.Index, p, _subtitleAlternate.Paragraphs);
                                SetFontName(original);
                                SubtitleListview1.SetAlternateText(item.Index, p.Text);
                            }
                        }
                    }
                    RefreshSelectedParagraph();
                }
            }
        }

        private void SetFontName(Paragraph p)
        {
            if (p == null)
                return;

            bool done = false;

            string s = p.Text;
            if (s.StartsWith("<font "))
            {
                int start = s.IndexOf("<font ");
                if (start >= 0)
                {
                    int end = s.IndexOf(">", start);
                    if (end > 0)
                    {
                        string f = s.Substring(start, end - start);
                        if (f.Contains(" color=") && !f.Contains(" face="))
                        {
                            start = s.IndexOf(" color=", start);
                            s = s.Insert(start, string.Format(" face=\"{0}\"", fontDialog1.Font.Name));
                            p.Text = s;
                            done = true;
                        }
                        else if (f.Contains(" face="))
                        {
                            int faceStart = f.IndexOf(" face=");
                            if (s.IndexOf("\"", faceStart + " face=".Length + 1) > 0)
                                end = s.IndexOf("\"", faceStart + " face=".Length + 1);
                            s = s.Substring(0, faceStart) + string.Format(" face=\"{0}", fontDialog1.Font.Name) + s.Substring(end);
                            p.Text = s;
                            done = true;
                        }
                    }
                }
            }

            if (!done)
                p.Text = string.Format("<font face=\"{0}\">{1}</font>", fontDialog1.Font.Name, s);
        }

        private void TypeEffectToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count > 0)
            {
                var typewriter = new EffectTypewriter();

                typewriter.Initialize(SubtitleListview1.GetSelectedParagraph(_subtitle));

                if (typewriter.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeTypeWriterEffect);
                    int firstNumber = _subtitle.Paragraphs[0].Number;
                    int lastSelectedIndex = SubtitleListview1.SelectedItems[0].Index;
                    int index = lastSelectedIndex;
                    _subtitle.Paragraphs.RemoveAt(index);
                    bool isframeBased = GetCurrentSubtitleFormat().IsFrameBased;
                    foreach (Paragraph p in typewriter.TypewriterParagraphs)
                    {
                        if (isframeBased)
                        {
                            p.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                            p.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                        }
                        _subtitle.Paragraphs.Insert(index, p);
                        index++;
                    }
                    _subtitle.Renumber(firstNumber);
                    _subtitleListViewIndex = -1;
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    SubtitleListview1.SelectIndexAndEnsureVisible(lastSelectedIndex);
                }
                typewriter.Dispose();
            }
        }

        private void KarokeeEffectToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count > 0)
            {
                var karaoke = new EffectKaraoke();

                karaoke.Initialize(SubtitleListview1.GetSelectedParagraph(_subtitle));

                if (karaoke.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeKaraokeEffect);
                    int firstNumber = _subtitle.Paragraphs[0].Number;
                    int lastSelectedIndex = SubtitleListview1.SelectedItems[0].Index;
                    bool isframeBased = GetCurrentSubtitleFormat().IsFrameBased;

                    int i = SubtitleListview1.SelectedItems.Count - 1;
                    while (i >= 0)
                    {
                        ListViewItem item = SubtitleListview1.SelectedItems[i];
                        Paragraph p = _subtitle.GetParagraphOrDefault(item.Index);
                        if (p != null)
                        {
                            int index = item.Index;
                            _subtitle.Paragraphs.RemoveAt(index);
                            foreach (Paragraph kp in karaoke.MakeAnimation(p))
                            {
                                if (isframeBased)
                                {
                                    p.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                                    p.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                                }
                                _subtitle.Paragraphs.Insert(index, kp);
                                index++;
                            }
                        }
                        i--;
                    }

                    _subtitle.Renumber(firstNumber);
                    _subtitleListViewIndex = -1;
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    SubtitleListview1.SelectIndexAndEnsureVisible(lastSelectedIndex);
                }
                karaoke.Dispose();
            }
        }

        private void MatroskaImportStripMenuItemClick(object sender, EventArgs e)
        {
            openFileDialog1.Title = _language.OpenMatroskaFile;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = _language.MatroskaFiles + "|*.mkv;*.mks|" + _languageGeneral.AllFiles + "|*.*";
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                ImportSubtitleFromMatroskaFile(openFileDialog1.FileName);
            }
        }

        private void ImportSubtitleFromMatroskaFile(string fileName)
        {
            bool isValid;
            var matroska = new Matroska();
            var subtitleList = matroska.GetMatroskaSubtitleTracks(fileName, out isValid);
            if (isValid)
            {
                if (subtitleList.Count == 0)
                {
                    MessageBox.Show(_language.NoSubtitlesFound);
                }
                else
                {
                    if (ContinueNewOrExit())
                    {
                        if (subtitleList.Count > 1)
                        {
                            MatroskaSubtitleChooser subtitleChooser = new MatroskaSubtitleChooser();
                            subtitleChooser.Initialize(subtitleList);
                            if (subtitleChooser.ShowDialog(this) == DialogResult.OK)
                            {
                                LoadMatroskaSubtitle(subtitleList[subtitleChooser.SelectedIndex], fileName, false);
                                if (Path.GetExtension(fileName).ToLower() == ".mkv")
                                    OpenVideo(fileName);
                            }
                        }
                        else
                        {
                            LoadMatroskaSubtitle(subtitleList[0], fileName, false);
                            if (Path.GetExtension(fileName).ToLower() == ".mkv")
                                OpenVideo(fileName);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(string.Format(_language.NotAValidMatroskaFileX, fileName));
            }
        }

        private void MatroskaProgress(long position, long total)
        {
            ShowStatus(string.Format("{0}, {1:0}%", _language.ParsingMatroskaFile, position * 100 / total));
            statusStrip1.Refresh();
            if (DateTime.Now.Ticks % 10 == 0)
                Application.DoEvents();
        }

        private void LoadMatroskaSubtitle(MatroskaSubtitleInfo matroskaSubtitleInfo, string fileName, bool batchMode)
        {
            bool isValid;
            bool isSsa = false;
            var matroska = new Matroska();
            SubtitleFormat format;

            if (matroskaSubtitleInfo.CodecId.ToUpper() == "S_VOBSUB")
            {
                if (batchMode)
                    return;
                LoadVobSubFromMatroska(matroskaSubtitleInfo, fileName);
                return;
            }
            if (matroskaSubtitleInfo.CodecId.ToUpper() == "S_HDMV/PGS")
            {
                if (batchMode)
                    return;
                LoadBluRaySubFromMatroska(matroskaSubtitleInfo, fileName);
                return;
            }

            ShowStatus(_language.ParsingMatroskaFile);
            Refresh();
            Cursor.Current = Cursors.WaitCursor;
            List<SubtitleSequence> sub = matroska.GetMatroskaSubtitle(fileName, (int)matroskaSubtitleInfo.TrackNumber, out isValid, MatroskaProgress);
            Cursor.Current = Cursors.Default;
            if (isValid)
            {
                MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
                _subtitleListViewIndex = -1;
                if (!batchMode)
                    ResetSubtitle();
                _subtitle.Paragraphs.Clear();

                if (matroskaSubtitleInfo.CodecPrivate.ToLower().Contains("[script info]"))
                {
                    if (matroskaSubtitleInfo.CodecPrivate.ToLower().Contains("[V4 Styles]".ToLower()))
                        format = new SubStationAlpha();
                    else
                        format = new AdvancedSubStationAlpha();
                    isSsa = true;
                    if (_networkSession == null)
                    {
                        SubtitleListview1.ShowExtraColumn(Configuration.Settings.Language.General.Style);
                        SubtitleListview1.DisplayExtraFromExtra = true;
                    }
                }
                else
                {
                    format = new SubRip();
                    if (_networkSession == null && SubtitleListview1.IsExtraColumnVisible)
                        SubtitleListview1.HideExtraColumn();
                }

                comboBoxSubtitleFormats.SelectedIndexChanged -= ComboBoxSubtitleFormatsSelectedIndexChanged;
                SetCurrentFormat(format);
                comboBoxSubtitleFormats.SelectedIndexChanged += ComboBoxSubtitleFormatsSelectedIndexChanged;

                if (isSsa)
                {
                    int commaCount = 100;
                    _subtitle.Header = matroskaSubtitleInfo.CodecPrivate;
                    if (!_subtitle.Header.Contains("[Events]"))
                    {
                        _subtitle.Header = _subtitle.Header.Trim() + Environment.NewLine +
                                           Environment.NewLine +
                                           "[Events]" + Environment.NewLine;
                    }
                    else
                    {
                        _subtitle.Header = _subtitle.Header.Remove(_subtitle.Header.IndexOf("[Events]"));
                        _subtitle.Header = _subtitle.Header.Trim() + Environment.NewLine +
                                           Environment.NewLine +
                                           "[Events]" + Environment.NewLine;
                    }

                    foreach (SubtitleSequence p in sub)
                    {
                        string s1 = p.Text;
                        if (s1.Contains(@"{\"))
                            s1 = s1.Substring(0, s1.IndexOf(@"{\"));
                        int temp = s1.Split(',').Length;
                        if (temp < commaCount)
                            commaCount = temp;
                    }

                    foreach (SubtitleSequence p in sub)
                    {
                        string extra = null;
                        string s = string.Empty;
                        string[] arr = p.Text.Split(',');
                        if (arr.Length >= commaCount)
                        {

                            extra = arr[2].TrimStart('*');

                            for (int i = commaCount; i <= arr.Length; i++)
                            {
                                if (s.Length > 0)
                                    s += ",";
                                s += arr[i - 1];
                            }
                        }
                        var p2 = new Paragraph(AdvancedSubStationAlpha.GetFormattedText(s), p.StartMilliseconds, p.EndMilliseconds);
                        p2.Extra = extra;
                        _subtitle.Paragraphs.Add(p2);
                    }
                }
                else
                {
                    foreach (SubtitleSequence p in sub)
                    {
                        _subtitle.Paragraphs.Add(new Paragraph(p.Text, p.StartMilliseconds, p.EndMilliseconds));
                    }
                }

                SetEncoding(Encoding.UTF8);
                ShowStatus(_language.SubtitleImportedFromMatroskaFile);
                _subtitle.Renumber(1);
                _subtitle.WasLoadedWithFrameNumbers = false;
                if (fileName.ToLower().EndsWith(".mkv") || fileName.ToLower().EndsWith(".mks"))
                {
                    _fileName = fileName.Substring(0, fileName.Length - 4);
                    Text = Title + " - " + _fileName;
                }
                else
                {
                    Text = Title;
                }
                _fileDateTime = new DateTime();

                _converted = true;

                if (batchMode)
                    return;

                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                if (_subtitle.Paragraphs.Count > 0)
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                ShowSource();
            }
        }

        public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[128000];
            int len;
            while ((len = input.Read(buffer, 0, 128000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }

        private void LoadVobSubFromMatroska(MatroskaSubtitleInfo matroskaSubtitleInfo, string fileName)
        {
            if (matroskaSubtitleInfo.ContentEncodingType == 1)
            {
                MessageBox.Show("Encrypted vobsub content not supported");
            }

            bool isValid;
            var matroska = new Matroska();

            ShowStatus(_language.ParsingMatroskaFile);
            Refresh();
            Cursor.Current = Cursors.WaitCursor;
            List<SubtitleSequence> sub = matroska.GetMatroskaSubtitle(fileName, (int)matroskaSubtitleInfo.TrackNumber, out isValid, MatroskaProgress);
            Cursor.Current = Cursors.Default;

            if (isValid)
            {
                MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
                _subtitleListViewIndex = -1;
                _subtitle.Paragraphs.Clear();

                List<VobSubMergedPack> mergedVobSubPacks = new List<VobSubMergedPack>();
                Nikse.SubtitleEdit.Logic.VobSub.Idx idx = new Logic.VobSub.Idx(matroskaSubtitleInfo.CodecPrivate.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                foreach (SubtitleSequence p in sub)
                {
                    if (matroskaSubtitleInfo.ContentEncodingType == 0) // compressed with zlib
                    {
                        bool error = false;
                        MemoryStream outStream = new MemoryStream();
                        ComponentAce.Compression.Libs.zlib.ZOutputStream outZStream = new ComponentAce.Compression.Libs.zlib.ZOutputStream(outStream);
                        MemoryStream inStream = new MemoryStream(p.BinaryData);
                        byte[] buffer = null;
                        try
                        {
                            CopyStream(inStream, outZStream);
                            buffer = new byte[outZStream.TotalOut];
                            outStream.Position = 0;
                            outStream.Read(buffer, 0, buffer.Length);
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace);
                            error = true;
                        }
                        finally
                        {
                            outStream.Close();
                            outZStream.Close();
                            inStream.Close();
                        }

                        if (!error)
                            mergedVobSubPacks.Add(new VobSubMergedPack(buffer, TimeSpan.FromMilliseconds(p.StartMilliseconds), 32, null));
                    }
                    else
                    {
                        mergedVobSubPacks.Add(new VobSubMergedPack(p.BinaryData, TimeSpan.FromMilliseconds(p.StartMilliseconds), 32, null));
                    }
                    mergedVobSubPacks[mergedVobSubPacks.Count - 1].EndTime = TimeSpan.FromMilliseconds(p.EndMilliseconds);

                    // fix overlapping (some versions of Handbrake makes overlapping time codes - thx Hawke)
                    if (mergedVobSubPacks.Count > 1 && mergedVobSubPacks[mergedVobSubPacks.Count - 2].EndTime > mergedVobSubPacks[mergedVobSubPacks.Count - 1].StartTime)
                        mergedVobSubPacks[mergedVobSubPacks.Count - 2].EndTime = TimeSpan.FromMilliseconds(mergedVobSubPacks[mergedVobSubPacks.Count - 1].StartTime.TotalMilliseconds - 1);
                }

                var formSubOcr = new VobSubOcr();
                formSubOcr.Initialize(mergedVobSubPacks, idx.Palette, Configuration.Settings.VobSubOcr, null); //TODO - language???
                if (formSubOcr.ShowDialog(this) == DialogResult.OK)
                {
                    ResetSubtitle();
                    _subtitle.Paragraphs.Clear();
                    _subtitle.WasLoadedWithFrameNumbers = false;
                    foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                        _subtitle.Paragraphs.Add(p);

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    _subtitleListViewIndex = -1;
                    SubtitleListview1.FirstVisibleIndex = -1;
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    _fileName =  Path.GetFileNameWithoutExtension(fileName);
                    _converted = true;
                    Text = Title;

                    Configuration.Settings.Save();
                }
            }
        }

        private void LoadBluRaySubFromMatroska(MatroskaSubtitleInfo matroskaSubtitleInfo, string fileName)
        {
            if (matroskaSubtitleInfo.ContentEncodingType == 1)
            {
                MessageBox.Show("Encrypted vobsub content not supported");
            }

            bool isValid;
            var matroska = new Matroska();

            ShowStatus(_language.ParsingMatroskaFile);
            Refresh();
            Cursor.Current = Cursors.WaitCursor;
            List<SubtitleSequence> sub = matroska.GetMatroskaSubtitle(fileName, (int)matroskaSubtitleInfo.TrackNumber, out isValid, MatroskaProgress);
            Cursor.Current = Cursors.Default;
            int noOfErrors = 0;
            string lastError = string.Empty;

            if (isValid)
            {
                MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
                _subtitleListViewIndex = -1;
                _subtitle.Paragraphs.Clear();
                List<BluRaySupPicture> subtitles = new List<BluRaySupPicture>();
                StringBuilder log = new StringBuilder();
                foreach (SubtitleSequence p in sub)
                {
                    byte[] buffer = null;
                    if (matroskaSubtitleInfo.ContentEncodingType == 0) // compressed with zlib
                    {
                        MemoryStream outStream = new MemoryStream();
                        ComponentAce.Compression.Libs.zlib.ZOutputStream outZStream = new ComponentAce.Compression.Libs.zlib.ZOutputStream(outStream);
                        MemoryStream inStream = new MemoryStream(p.BinaryData);
                        try
                        {
                            CopyStream(inStream, outZStream);
                            buffer = new byte[outZStream.TotalOut];
                            outStream.Position = 0;
                            outStream.Read(buffer, 0, buffer.Length);
                        }
                        catch (Exception exception)
                        {
                            TimeCode tc = new TimeCode(TimeSpan.FromMilliseconds(p.StartMilliseconds));
                            lastError = tc.ToString() + ": " + exception.Message + ": " + exception.StackTrace;
                            noOfErrors++;
                        }
                        finally
                        {
                            outStream.Close();
                            outZStream.Close();
                            inStream.Close();
                        }
                    }
                    else
                    {
                        buffer = p.BinaryData;
                    }
                    if (buffer != null && buffer.Length > 100)
                    {
                        MemoryStream ms = new MemoryStream(buffer);
                        var list = BluRaySupParser.ParseBluRaySup(ms, log, true);
                        foreach (var sup in list)
                        {
                            sup.StartTime = (long)((p.StartMilliseconds - 45) * 90.0);
                            sup.EndTime = (long)((p.EndMilliseconds - 45) * 90.0);
                            subtitles.Add(sup);

                            // fix overlapping
                            if (subtitles.Count > 1 && sub[subtitles.Count - 2].EndMilliseconds > sub[subtitles.Count - 1].StartMilliseconds)
                                subtitles[subtitles.Count - 2].EndTime = subtitles[subtitles.Count - 1].StartTime - 1;
                        }
                        ms.Close();
                    }
                }

                if (noOfErrors > 0)
                {
                    MessageBox.Show(string.Format("{0} errror(s) occured during extraction of bdsup\r\n\r\n{1}", noOfErrors, lastError));
                }

                var formSubOcr = new VobSubOcr();
                formSubOcr.Initialize(subtitles, Configuration.Settings.VobSubOcr, fileName);
                if (formSubOcr.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeImportingDvdSubtitle);

                    _subtitle.Paragraphs.Clear();
                    SetCurrentFormat(new SubRip().FriendlyName);
                    _subtitle.WasLoadedWithFrameNumbers = false;
                    _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                    foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                    {
                        _subtitle.Paragraphs.Add(p);
                    }

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    _subtitleListViewIndex = -1;
                    SubtitleListview1.FirstVisibleIndex = -1;
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    _fileName = string.Empty;
                    Text = Title;

                    Configuration.Settings.Save();
                }
            }
        }

        private bool ImportSubtitleFromTransportStream(string fileName)
        {
            var tsParser = new Nikse.SubtitleEdit.Logic.TransportStream.TransportStreamParser();
            tsParser.ParseTsFile(fileName);

            if (tsParser.SubtitlePacketIds.Count == 0)
            {
                MessageBox.Show(_language.NoSubtitlesFound);
                return false;
            }

            var log = new StringBuilder();
            List<BluRaySupPicture> subtitles = new List<BluRaySupPicture>();
            var pesList = tsParser.GetSubtitlePesPackets(tsParser.SubtitlePacketIds[0]);
            foreach (var sp in pesList)
            {
                if (sp.DataBuffer != null)
                {
                    MemoryStream ms = new MemoryStream(sp.DataBuffer);
                    var list = BluRaySupParser.ParseBluRaySup(ms, log, true);
                    foreach (var sup in list)
                    {
                        //sup.StartTime = p.StartMilliseconds;
                        //sup.EndTime = p.EndMilliseconds;
                        subtitles.Add(sup);

                        // fix overlapping
                        //if (subtitles.Count > 1 && sub[subtitles.Count - 2].EndMilliseconds > sub[subtitles.Count - 1].StartMilliseconds)
                        //    subtitles[subtitles.Count - 2].EndTime = subtitles[subtitles.Count - 1].StartTime - 1;
                    }
                    ms.Close();
                }
            }

            var formSubOcr = new VobSubOcr();
            formSubOcr.Initialize(subtitles, Configuration.Settings.VobSubOcr, fileName);
            if (formSubOcr.ShowDialog(this) == DialogResult.OK)
            {
                MakeHistoryForUndo(_language.BeforeImportingDvdSubtitle);

                _subtitle.Paragraphs.Clear();
                SetCurrentFormat(new SubRip().FriendlyName);
                _subtitle.WasLoadedWithFrameNumbers = false;
                _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                {
                    _subtitle.Paragraphs.Add(p);
                }

                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                _subtitleListViewIndex = -1;
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0);

                _fileName = string.Empty;
                Text = Title;

                Configuration.Settings.Save();
                return true;
            }
            return false;
        }

        private bool ImportSubtitleFromMp4(string fileName)
        {
            var mp4Parser = new Logic.Mp4.Mp4Parser(fileName);
            var mp4SubtitleTracks = mp4Parser.GetSubtitleTracks();
            if (mp4SubtitleTracks.Count == 0)
            {
                MessageBox.Show(_language.NoSubtitlesFound);
                return false;
            }
            else if (mp4SubtitleTracks.Count == 1)
            {
                LoadMp4Subtitle(fileName, mp4SubtitleTracks[0]);
                return true;
            }
            else
            {
                var subtitleChooser = new MatroskaSubtitleChooser();
                subtitleChooser.Initialize(mp4SubtitleTracks);
                if (subtitleChooser.ShowDialog(this) == DialogResult.OK)
                {
                    LoadMp4Subtitle(fileName, mp4SubtitleTracks[subtitleChooser.SelectedIndex]);
                    return true;
                }
                return false;
            }
        }

        private bool ImportSubtitleFromDivX(string fileName)
        {
            var f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int count = 0;
            f.Position = 0;
            var list = new List<XSub>();
            var searchBuffer = new byte[2048];
            long pos = 0;
            long length = f.Length - 50;
            while (pos < length)
            {
                f.Position = pos;
                int readCount = f.Read(searchBuffer, 0, searchBuffer.Length);
                for (int i = 0; i < readCount; i++)
                {
                    if (searchBuffer[i] == 0x5b &&
                        (i + 4 >= readCount || (searchBuffer[i + 1] >= 0x30 && searchBuffer[i + 1] <= 0x39 && searchBuffer[i + 3] == 0x3a)))
                    {
                        f.Position =  pos + i + 1;

                        byte[] buffer = new byte[26];
                        f.Read(buffer, 0, 26);

                        if (buffer[2] == 0x3a && // :
                            buffer[5] == 0x3a && // :
                            buffer[8] == 0x2e && // .
                            buffer[12] == 0x2d && // -
                            buffer[15] == 0x3a && // :
                            buffer[18] == 0x3a && // :
                            buffer[21] == 0x2e && // .
                            buffer[25] == 0x5d) // ]
                        { // subtitle time code
                            string timeCode = Encoding.ASCII.GetString(buffer, 0, 25);

                            f.Read(buffer, 0, 2);
                            int width = (int)BitConverter.ToUInt16(buffer, 0);
                            f.Read(buffer, 0, 2);
                            int height = (int)BitConverter.ToUInt16(buffer, 0);
                            f.Read(buffer, 0, 2);
                            int x = (int)BitConverter.ToUInt16(buffer, 0);
                            f.Read(buffer, 0, 2);
                            int y = (int)BitConverter.ToUInt16(buffer, 0);
                            f.Read(buffer, 0, 2);
                            int xEnd = (int)BitConverter.ToUInt16(buffer, 0);
                            f.Read(buffer, 0, 2);
                            int yEnd = (int)BitConverter.ToUInt16(buffer, 0);
                            f.Read(buffer, 0, 2);
                            int RleLength = (int)BitConverter.ToUInt16(buffer, 0);

                            byte[] colorBuffer = new byte[4 * 3]; // four colors with rgb (3 bytes)
                            f.Read(colorBuffer, 0, colorBuffer.Length);

                            buffer = new byte[RleLength];
                            int bytesRead = f.Read(buffer, 0, buffer.Length);

                            if (width > 0 && height > 0 && bytesRead == buffer.Length)
                            {
                                var xSub = new XSub(timeCode, width, height, colorBuffer, buffer);
                                list.Add(xSub);
                                count++;
                            }
                        }
                    }
                }
                pos += searchBuffer.Length;
            }
            f.Close();

            if (count > 0)
            {
                var formSubOcr = new VobSubOcr();
                formSubOcr.Initialize(list, Configuration.Settings.VobSubOcr, fileName); //TODO - language???
                if (formSubOcr.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
                    _subtitleListViewIndex = -1;
                    FileNew();
                    _subtitle.WasLoadedWithFrameNumbers = false;
                    foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                        _subtitle.Paragraphs.Add(p);

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    _subtitleListViewIndex = -1;
                    SubtitleListview1.FirstVisibleIndex = -1;
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    _fileName = Path.GetFileNameWithoutExtension(fileName);
                    _converted = true;
                    Text = Title;

                    Configuration.Settings.Save();
                    OpenVideo(fileName);
                }
            }

            return count > 0;
        }

        private void LoadMp4Subtitle(string fileName, Logic.Mp4.Boxes.Trak mp4SubtitleTrack)
        {
            if (mp4SubtitleTrack.Mdia.IsVobSubSubtitle)
            {
                var subPicturesWithTimeCodes = new List<VobSubOcr.SubPicturesWithSeparateTimeCodes>();
                for (int i = 0; i < mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes.Count; i++)
                {
                    if (mp4SubtitleTrack.Mdia.Minf.Stbl.SubPictures.Count > i)
                    {
                        var start = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.StartTimeCodes[i]);
                        var end = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes[i]);
                        subPicturesWithTimeCodes.Add(new VobSubOcr.SubPicturesWithSeparateTimeCodes(mp4SubtitleTrack.Mdia.Minf.Stbl.SubPictures[i], start, end));
                    }
                }

                var formSubOcr = new VobSubOcr();
                formSubOcr.Initialize(subPicturesWithTimeCodes, Configuration.Settings.VobSubOcr, fileName); //TODO - language???
                if (formSubOcr.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
                    _subtitleListViewIndex = -1;
                    FileNew();
                    _subtitle.WasLoadedWithFrameNumbers = false;
                    foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                        _subtitle.Paragraphs.Add(p);

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    _subtitleListViewIndex = -1;
                    SubtitleListview1.FirstVisibleIndex = -1;
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    _fileName = Path.GetFileNameWithoutExtension(fileName);
                    _converted = true;
                    Text = Title;

                    Configuration.Settings.Save();
                }
            }
            else
            {
                MakeHistoryForUndo(_language.BeforeImportFromMatroskaFile);
                _subtitleListViewIndex = -1;
                FileNew();

                for (int i = 0; i < mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes.Count; i++)
                {
                    if (mp4SubtitleTrack.Mdia.Minf.Stbl.Texts.Count > i)
                    {
                        var start = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.StartTimeCodes[i]);
                        var end = TimeSpan.FromSeconds(mp4SubtitleTrack.Mdia.Minf.Stbl.EndTimeCodes[i]);
                        string text = mp4SubtitleTrack.Mdia.Minf.Stbl.Texts[i];
                        Paragraph p = new Paragraph(text, start.TotalMilliseconds, end.TotalMilliseconds);
                        if (p.EndTime.TotalMilliseconds - p.StartTime.TotalMilliseconds > Configuration.Settings.General.SubtitleMaximumDisplayMilliseconds)
                            p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds +Configuration.Settings.General.SubtitleMaximumDisplayMilliseconds;

                        if (mp4SubtitleTrack.Mdia.IsClosedCaption && string.IsNullOrEmpty(text))
                        {
                            // do not add empty lines
                        }
                        else
                        {
                            _subtitle.Paragraphs.Add(p);
                        }
                    }
                }

                SetEncoding(Encoding.UTF8);
                ShowStatus(_language.SubtitleImportedFromMatroskaFile);
                _subtitle.Renumber(1);
                _subtitle.WasLoadedWithFrameNumbers = false;
                if (fileName.ToLower().EndsWith(".mp4") || fileName.ToLower().EndsWith(".m4v"))
                {
                    _fileName = fileName.Substring(0, fileName.Length - 4);
                    Text = Title + " - " + _fileName;
                }
                else
                {
                    Text = Title;
                }
                _fileDateTime = new DateTime();

                _converted = true;

                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                if (_subtitle.Paragraphs.Count > 0)
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);
                ShowSource();
            }
        }

        private void SubtitleListview1_DragEnter(object sender, DragEventArgs e)
        {
            // make sure they're actually dropping files (not text or anything else)
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        private void SubtitleListview1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                if (ContinueNewOrExit())
                {
                    string fileName = files[0];

                    var fi = new FileInfo(fileName);
                    string ext = Path.GetExtension(fileName).ToLower();

                    if (ext == ".mkv")
                    {
                        bool isValid;
                        var matroska = new Matroska();
                        var subtitleList = matroska.GetMatroskaSubtitleTracks(fileName, out isValid);
                        if (isValid)
                        {
                            if (subtitleList.Count == 0)
                            {
                                MessageBox.Show(_language.NoSubtitlesFound);
                            }
                            else if (subtitleList.Count > 1)
                            {
                                MatroskaSubtitleChooser subtitleChooser = new MatroskaSubtitleChooser();
                                subtitleChooser.Initialize(subtitleList);
                                if (subtitleChooser.ShowDialog(this) == DialogResult.OK)
                                {
                                    LoadMatroskaSubtitle(subtitleList[subtitleChooser.SelectedIndex], fileName, false);
                                }
                            }
                            else
                            {
                                LoadMatroskaSubtitle(subtitleList[0], fileName, false);
                            }
                        }
                        return;
                    }

                    if (fi.Length < 1024 * 1024 * 2) // max 2 mb
                    {
                        OpenSubtitle(fileName, null);
                    }
                    else if (fi.Length < 50000000 && ext == ".sub" && IsVobSubFile(fileName, true)) // max 50 mb
                    {
                        OpenSubtitle(fileName, null);
                    }
                    else
                    {
                        MessageBox.Show(string.Format(_language.DropFileXNotAccepted, fileName));
                    }
                }
            }
            else
            {
                MessageBox.Show(_language.DropOnlyOneFile);
            }
        }

        private void TextBoxSourceDragEnter(object sender, DragEventArgs e)
        {
            // make sure they're actually dropping files (not text or anything else)
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        private void TextBoxSourceDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                if (ContinueNewOrExit())
                {
                    OpenSubtitle(files[0], null);
                }
            }
            else
            {
                MessageBox.Show(_language.DropOnlyOneFile);
            }
        }

        private void ToolStripMenuItemManualAnsiClick(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            openFileDialog1.Title = _language.OpenAnsiSubtitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var chooseEncoding = new ChooseEncoding();
                chooseEncoding.Initialize(openFileDialog1.FileName);
                if (chooseEncoding.ShowDialog(this) == DialogResult.OK)
                {
                    Encoding encoding = chooseEncoding.GetEncoding();
                    SetEncoding(Encoding.UTF8);
                    OpenSubtitle(openFileDialog1.FileName, encoding);
                    _converted = true;
                }
            }

        }

        private void ChangeCasingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeCasing(false);
        }

        private void ChangeCasing(bool onlySelectedLines)
        {
            if (IsSubtitleLoaded)
            {
                SaveSubtitleListviewIndexes();
                var changeCasing = new ChangeCasing();
                _formPositionsAndSizes.SetPositionAndSize(changeCasing);
                if (onlySelectedLines)
                    changeCasing.Text += " - " + _language.SelectedLines;
                ReloadFromSourceView();
                if (changeCasing.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeChangeCasing);

                    Cursor.Current = Cursors.WaitCursor;
                    var selectedLines = new Subtitle();
                    selectedLines.WasLoadedWithFrameNumbers = _subtitle.WasLoadedWithFrameNumbers;
                    if (onlySelectedLines)
                    {
                        foreach (int index in SubtitleListview1.SelectedIndices)
                            selectedLines.Paragraphs.Add(new Paragraph(_subtitle.Paragraphs[index]));
                    }
                    else
                    {
                        foreach (Paragraph p in _subtitle.Paragraphs)
                            selectedLines.Paragraphs.Add(new Paragraph(p));
                    }

                    bool saveChangeCaseChanges = true;
                    changeCasing.FixCasing(selectedLines, Utilities.AutoDetectLanguageName(Configuration.Settings.General.SpellCheckLanguage, _subtitle));
                    var changeCasingNames = new ChangeCasingNames();
                    if (changeCasing.ChangeNamesToo)
                    {
                        changeCasingNames.Initialize(selectedLines);
                        if (changeCasingNames.ShowDialog(this) == DialogResult.OK)
                        {
                            changeCasingNames.FixCasing();

                            if (changeCasing.LinesChanged == 0)
                                ShowStatus(string.Format(_language.CasingCompleteMessageOnlyNames, changeCasingNames.LinesChanged, _subtitle.Paragraphs.Count));
                            else
                                ShowStatus(string.Format(_language.CasingCompleteMessage, changeCasing.LinesChanged, _subtitle.Paragraphs.Count, changeCasingNames.LinesChanged));
                        }
                        else
                        {
                            saveChangeCaseChanges = false;
                        }
                    }
                    else
                    {
                        ShowStatus(string.Format(_language.CasingCompleteMessageNoNames, changeCasing.LinesChanged, _subtitle.Paragraphs.Count));
                    }

                    if (saveChangeCaseChanges)
                    {
                        if (onlySelectedLines)
                        {
                            int i = 0;
                            foreach (int index in SubtitleListview1.SelectedIndices)
                            {
                                _subtitle.Paragraphs[index].Text = selectedLines.Paragraphs[i].Text;
                                i++;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
                            {
                                _subtitle.Paragraphs[i].Text = selectedLines.Paragraphs[i].Text;
                            }
                        }
                        ShowSource();
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        if (changeCasing.LinesChanged > 0 || changeCasingNames.LinesChanged > 0)
                        {
                            _subtitleListViewIndex = -1;
                            RestoreSubtitleListviewIndexes();
                            UpdateSourceView();
                        }
                    }
                    Cursor.Current = Cursors.Default;
                }
                _formPositionsAndSizes.SavePositionAndSize(changeCasing);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ToolStripMenuItemChangeFramerateClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                int lastSelectedIndex = 0;
                if (SubtitleListview1.SelectedItems.Count > 0)
                    lastSelectedIndex = SubtitleListview1.SelectedItems[0].Index;

                ReloadFromSourceView();
                var changeFramerate = new ChangeFrameRate();
                _formPositionsAndSizes.SetPositionAndSize(changeFramerate);
                changeFramerate.Initialize(CurrentFrameRate.ToString());
                if (changeFramerate.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeChangeFrameRate);

                    double oldFramerate = changeFramerate.OldFrameRate;
                    double newFramerate = changeFramerate.NewFrameRate;
                    _subtitle.ChangeFramerate(oldFramerate, newFramerate);

                    ShowStatus(string.Format(_language.FrameRateChangedFromXToY, oldFramerate, newFramerate));
                    toolStripComboBoxFrameRate.Text = newFramerate.ToString();

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    _subtitleListViewIndex = -1;
                    SubtitleListview1.SelectIndexAndEnsureVisible(lastSelectedIndex);
                }
                _formPositionsAndSizes.SavePositionAndSize(changeFramerate);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool IsVobSubFile(string subFileName, bool verbose)
        {
            try
            {
                bool isHeaderOk = HasVobSubHeader(subFileName);
                if (isHeaderOk)
                {
                    if (!verbose)
                        return true;

                    string idxFileName = Path.Combine(Path.GetDirectoryName(subFileName), Path.GetFileNameWithoutExtension(subFileName) + ".idx");
                    if (File.Exists(idxFileName))
                        return true;
                    return (MessageBox.Show(string.Format(_language.IdxFileNotFoundWarning, idxFileName ), _title, MessageBoxButtons.YesNo) ==  DialogResult.Yes);
                }
                if (verbose)
                    MessageBox.Show(string.Format(_language.InvalidVobSubHeader,  subFileName));
            }
            catch (Exception ex)
            {
                if (verbose)
                    MessageBox.Show(ex.Message);
            }
            return false;
        }

        public static bool HasVobSubHeader(string subFileName)
        {
            var buffer = new byte[4];
            var fs = new FileStream(subFileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };
            fs.Read(buffer, 0, 4);
            bool isHeaderOk = VobSubParser.IsMpeg2PackHeader(buffer) || VobSubParser.IsPrivateStream1(buffer, 0);
            fs.Close();
            return isHeaderOk;
        }

        public static bool IsBluRaySupFile(string subFileName)
        {
            var buffer = new byte[4];
            var fs = new FileStream(subFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) {Position = 0};
            fs.Read(buffer, 0, 4);
            fs.Close();
            return (buffer[0] == 0x50 && buffer[1] == 0x47); // 80 + 71 - P G
        }

        private bool IsSpDvdSupFile(string subFileName)
        {
            byte[] buffer = new byte[SpHeader.SpHeaderLength];
            var fs = new FileStream(subFileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };
            int bytesRead = fs.Read(buffer, 0, buffer.Length);
            if (bytesRead == buffer.Length)
            {
                var header = new SpHeader(buffer);
                if (header.Identifier == "SP" && header.NextBlockPosition > 4)
                {
                    buffer = new byte[header.NextBlockPosition];
                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                    if (bytesRead == buffer.Length)
                    {
                        buffer = new byte[SpHeader.SpHeaderLength];
                        bytesRead = fs.Read(buffer, 0, buffer.Length);
                        if (bytesRead == buffer.Length)
                        {
                            header = new SpHeader(buffer);
                            fs.Close();
                            return header.Identifier == "SP";
                        }
                    }
                }
            }
            fs.Close();
            return false;
        }

        private void ImportAndOcrSpDvdSup(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read) { Position = 0 };

            byte[] buffer = new byte[SpHeader.SpHeaderLength];
            int bytesRead = fs.Read(buffer, 0, buffer.Length);
            var header = new SpHeader(buffer);
            var spList = new List<SpHeader>();

            while (header.Identifier == "SP" && bytesRead > 0 && header.NextBlockPosition > 4)
            {
                buffer = new byte[header.NextBlockPosition];
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                if (bytesRead == buffer.Length)
                {
                    header.AddPicture(buffer);
                    spList.Add(header);
                }

                buffer = new byte[SpHeader.SpHeaderLength];
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                header = new SpHeader(buffer);
            }
            fs.Close();

            var vobSubOcr = new VobSubOcr();
            vobSubOcr.Initialize(fileName, null, Configuration.Settings.VobSubOcr, spList);
            if (vobSubOcr.ShowDialog(this) == DialogResult.OK)
            {
                MakeHistoryForUndo(_language.BeforeImportingVobSubFile);
                FileNew();
                _subtitle.Paragraphs.Clear();
                SetCurrentFormat(new SubRip().FriendlyName);
                _subtitle.WasLoadedWithFrameNumbers = false;
                _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                foreach (Paragraph p in vobSubOcr.SubtitleFromOcr.Paragraphs)
                {
                    _subtitle.Paragraphs.Add(p);
                }

                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                _subtitleListViewIndex = -1;
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0);

                _fileName = Path.ChangeExtension(vobSubOcr.FileName, ".srt");
                SetTitle();
                _converted = true;

                Configuration.Settings.Save();
            }
        }

        private void ImportAndOcrVobSubSubtitleNew(string fileName)
        {
            if (IsVobSubFile(fileName, true))
            {
                var vobSubOcr = new VobSubOcr();
                if (vobSubOcr.Initialize(fileName, Configuration.Settings.VobSubOcr, true))
                {
                    if (vobSubOcr.ShowDialog(this) == DialogResult.OK)
                    {
                        MakeHistoryForUndo(_language.BeforeImportingVobSubFile);
                        FileNew();
                        _subtitle.Paragraphs.Clear();
                        SetCurrentFormat(new SubRip().FriendlyName);
                        _subtitle.WasLoadedWithFrameNumbers = false;
                        _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                        foreach (Paragraph p in vobSubOcr.SubtitleFromOcr.Paragraphs)
                        {
                            _subtitle.Paragraphs.Add(p);
                        }

                        ShowSource();
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        _subtitleListViewIndex = -1;
                        SubtitleListview1.FirstVisibleIndex = -1;
                        SubtitleListview1.SelectIndexAndEnsureVisible(0);

                        _fileName = Path.ChangeExtension(vobSubOcr.FileName, ".srt");
                        SetTitle();
                        _converted = true;

                        Configuration.Settings.Save();
                    }
                }
            }
        }

        private void ToolStripMenuItemMergeLinesClick(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count >= 1)
                MergeAfterToolStripMenuItemClick(null, null);
        }

        private void VisualSyncSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowVisualSync(true);
        }

        private void GoogleTranslateSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            TranslateViaGoogle(true, true);
        }

        private void SaveSubtitleListviewIndexes()
        {
            _selectedIndexes = new List<int>();
            foreach (int index in SubtitleListview1.SelectedIndices)
                _selectedIndexes.Add(index);
        }

        private void RestoreSubtitleListviewIndexes()
        {
            _subtitleListViewIndex = -1;
            if (_selectedIndexes != null)
            {
                SubtitleListview1.SelectNone();
                int i = 0;
                foreach (int index in _selectedIndexes)
                {
                    if (index >= 0 && index < SubtitleListview1.Items.Count)
                    {
                        SubtitleListview1.Items[index].Selected = true;
                        if (i == 0)
                            SubtitleListview1.Items[index].EnsureVisible();
                    }
                    i++;
                }
            }
        }

        private void ShowSelectedLinesEarlierlaterToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                if (_showEarlierOrLater != null && !_showEarlierOrLater.IsDisposed)
                {
                    _showEarlierOrLater.WindowState = FormWindowState.Normal;
                    _showEarlierOrLater.Focus();
                    return;
                }

                bool waveFormEnabled = timerWaveForm.Enabled;
                timerWaveForm.Stop();
                bool videoTimerEnabled = videoTimer.Enabled;
                videoTimer.Stop();
                timer1.Stop();

                _showEarlierOrLater = new ShowEarlierLater();
                if (!_formPositionsAndSizes.SetPositionAndSize(_showEarlierOrLater))
                {
                    _showEarlierOrLater.Top = this.Top + 100;
                    _showEarlierOrLater.Left = this.Left + (this.Width / 2) - (_showEarlierOrLater.Width / 3);
                }
                _showEarlierOrLater.Initialize(ShowEarlierOrLater, _formPositionsAndSizes, true);
                MakeHistoryForUndo(_language.BeforeShowSelectedLinesEarlierLater);
                _showEarlierOrLater.Show(this);

                timerWaveForm.Enabled = waveFormEnabled;
                videoTimer.Enabled = videoTimerEnabled;
                timer1.Start();

                RefreshSelectedParagraph();
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal void MainKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Alt && e.KeyCode == (Keys.RButton | Keys.ShiftKey) && textBoxListViewText.Focused)
            { // annoying that focus leaves textbox while typing, when pressing Alt alone
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Modifiers == Keys.Alt && e.KeyCode == (Keys.RButton | Keys.ShiftKey))
                return;


            if (e.Modifiers == Keys.Shift && e.KeyCode == Keys.ShiftKey)
                return;
            if (e.Modifiers == Keys.Control && e.KeyCode == (Keys.LButton | Keys.ShiftKey))
                return;
            if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.ShiftKey)
                return;

            bool inListView = tabControlSubtitle.SelectedIndex == TabControlListView;

            if (e.KeyCode == Keys.Escape && !_cancelWordSpellCheck)
            {
                _cancelWordSpellCheck = true;
            }
            else if (inListView && (Keys.Shift | Keys.Control) == e.Modifiers && e.KeyCode == Keys.B)
            {
                AutoBalanceLinesAndAllow2PlusLines();
                e.SuppressKeyPress = true;
            }
            else if (audioVisualizer != null && audioVisualizer.Visible & e.KeyData == _waveformVerticalZoom)
            {
                if (audioVisualizer.VerticalZoomPercent > 0.2)
                    audioVisualizer.VerticalZoomPercent -= 0.1;
                else
                    audioVisualizer.VerticalZoomPercent = 1;
                e.SuppressKeyPress = true;
            }
            if (audioVisualizer != null && audioVisualizer.Visible & e.KeyData == _waveformZoomIn)
            {
                audioVisualizer.ZoomIn();
                e.SuppressKeyPress = true;
            }
            if (audioVisualizer != null && audioVisualizer.Visible & e.KeyData == _waveformZoomOut)
            {
                audioVisualizer.ZoomOut();
                e.SuppressKeyPress = true;
            }
            else if (audioVisualizer != null && audioVisualizer.Visible & e.KeyData == _waveformPlaySelection)
            {
                toolStripMenuItemWaveFormPlaySelection_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (audioVisualizer != null && audioVisualizer.Visible & e.KeyData == _waveformSearchSilenceForward)
            {
                audioVisualizer.FindDataBelowThresshold(Configuration.Settings.VideoControls.WaveformSeeksSilenceMaxVolume, Configuration.Settings.VideoControls.WaveformSeeksSilenceDurationSeconds);
                e.SuppressKeyPress = true;
            }
            else if (audioVisualizer != null && audioVisualizer.Visible & e.KeyData == _waveformSearchSilenceBack)
            {
                audioVisualizer.FindDataBelowThressholdBack(Configuration.Settings.VideoControls.WaveformSeeksSilenceMaxVolume, Configuration.Settings.VideoControls.WaveformSeeksSilenceDurationSeconds);
                e.SuppressKeyPress = true;
            }
            else if (_mainInsertBefore == e.KeyData && inListView)
            {
                InsertBefore();
                e.SuppressKeyPress = true;
                textBoxListViewText.Focus();
            }
            else if (_mainMergeDialogue == e.KeyData && inListView)
            {
                MergeDialogues();
                e.SuppressKeyPress = true;
            }
            else if (_mainListViewToggleDashes == e.KeyData && inListView)
            {
                ToggleDashes();
                e.SuppressKeyPress = true;
            }
            else if (!toolStripMenuItemReverseRightToLeftStartEnd.Visible && _mainEditReverseStartAndEndingForRTL == e.KeyData && inListView)
            {
                ReverseStartAndEndingForRTL();
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Z) // undo
            {
                UndoLastAction();
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Y) // redo
            {
                RedoLastAction();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Right && e.Modifiers == Keys.Control)
            {
                if (!textBoxListViewText.Focused && !textBoxListViewTextAlternate.Focused)
                {
                    mediaPlayer.CurrentPosition += 0.10;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Left && e.Modifiers == Keys.Control)
            {
                if (!textBoxListViewText.Focused && !textBoxListViewTextAlternate.Focused)
                {
                    mediaPlayer.CurrentPosition -= 0.10;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Right && e.Modifiers == Keys.Alt)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    mediaPlayer.CurrentPosition += 0.5;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Left && e.Modifiers == Keys.Alt)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    mediaPlayer.CurrentPosition -= 0.5;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Down && e.Modifiers == Keys.Alt)
            {
                if (AutoRepeatContinueOn)
                    Next();
                else
                    ButtonNextClick(null, null);
            }
            else if (e.KeyCode == Keys.Up && e.Modifiers == Keys.Alt)
            {
                if (AutoRepeatContinueOn)
                    PlayPrevious();
                else
                    ButtonPreviousClick(null, null);
            }
            else if (_mainGoToNext == e.KeyData && inListView)
            {
                if (AutoRepeatContinueOn)
                    Next();
                else
                    ButtonNextClick(null, null);
                e.SuppressKeyPress = true;
            }
            else if (_mainGoToPrevious == e.KeyData && inListView)
            {
                if (AutoRepeatContinueOn)
                    PlayPrevious();
                else
                    ButtonPreviousClick(null, null);
                e.SuppressKeyPress = true;
            }
            else if (_mainToggleFocus == e.KeyData && inListView)
            {
                if (SubtitleListview1.Focused)
                    textBoxListViewText.Focus();
                else
                    SubtitleListview1.Focus();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Home && e.Modifiers == Keys.Alt)
            {
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.End && e.Modifiers == Keys.Alt)
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(SubtitleListview1.Items.Count - 1);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.L) //Locate first selected line in subtitle listview
            {
                if (SubtitleListview1.SelectedItems.Count > 0)
                    SubtitleListview1.SelectedItems[0].EnsureVisible();
                e.SuppressKeyPress = true;
            }
            //else if (e.Modifiers == (Keys.Control | Keys.Alt | Keys.Shift) && e.KeyCode == Keys.R) // reload "Language.xml"
            //{
            //    if (File.Exists(Configuration.BaseDirectory + "Language.xml"))
            //        SetLanguage("Language.xml");
            //}
            else if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.M)
            {
                if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count >= 1)
                {
                    e.SuppressKeyPress = true;
                    if (SubtitleListview1.SelectedItems.Count == 2)
                        MergeAfterToolStripMenuItemClick(null, null);
                    else
                        MergeSelectedLines();
                }
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.K)
            {
                if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count >= 1)
                {
                    e.SuppressKeyPress = true;
                    if (SubtitleListview1.SelectedItems.Count == 2)
                        MergeAfterToolStripMenuItemClick(null, null);
                    else
                        MergeSelectedLines();
                }
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.U)
            { // toggle translator mode
                EditToolStripMenuItemDropDownOpening(null, null);
                toolStripMenuItemTranslationMode_Click(null, null);
            }
            else if (e.KeyData == _videoPlayPauseToggle)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    _endSeconds = -1;
                    mediaPlayer.TogglePlayPause();
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyData == _videoPause)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    _endSeconds = -1;
                    mediaPlayer.Pause();
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.Right)
            {
                if (!textBoxListViewText.Focused && !textBoxListViewTextAlternate.Focused)
                {
                    mediaPlayer.CurrentPosition += 1.0;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.Left)
            {
                if (!textBoxListViewText.Focused && !textBoxListViewTextAlternate.Focused)
                {
                    mediaPlayer.CurrentPosition -= 1.0;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Space)
            {
                if (!textBoxListViewText.Focused && !textBoxListViewTextAlternate.Focused && !textBoxSource.Focused && mediaPlayer.VideoPlayer != null)
                {
                    _endSeconds = -1;
                    mediaPlayer.TogglePlayPause();
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.D1)
            {
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle != null && mediaPlayer.VideoPlayer != null)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(SubtitleListview1.SelectedItems[0].Index);
                    if (p != null)
                    {
                        mediaPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.D2)
            {
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle != null && mediaPlayer.VideoPlayer != null)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(SubtitleListview1.SelectedItems[0].Index);
                    if (p != null)
                    {
                        mediaPlayer.CurrentPosition = p.EndTime.TotalSeconds;
                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.D3)
            {
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle != null && mediaPlayer.VideoPlayer != null)
                {
                    int index = SubtitleListview1.SelectedItems[0].Index -1;
                    Paragraph p = _subtitle.GetParagraphOrDefault(index);
                    if (p != null)
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(index);
                        mediaPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.D4)
            {
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle != null && mediaPlayer.VideoPlayer != null)
                {
                    int index = SubtitleListview1.SelectedItems[0].Index + 1;
                    Paragraph p = _subtitle.GetParagraphOrDefault(index);
                    if (p != null)
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(index);
                        mediaPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F4)
            {
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle != null && mediaPlayer.VideoPlayer != null)
                {
                    mediaPlayer.Pause();
                    Paragraph p = _subtitle.GetParagraphOrDefault(SubtitleListview1.SelectedItems[0].Index);
                    if (p != null)
                    {
                        if (Math.Abs(mediaPlayer.CurrentPosition - p.StartTime.TotalSeconds) < 0.1)
                            mediaPlayer.CurrentPosition = p.EndTime.TotalSeconds;
                        else
                            mediaPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        e.SuppressKeyPress = true;
                    }

                }
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F5)
            {
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle != null && mediaPlayer.VideoPlayer != null)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(SubtitleListview1.SelectedItems[0].Index);
                    if (p != null)
                    {
                        mediaPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        ShowSubtitle();
                        mediaPlayer.Play();
                        _endSeconds = p.EndTime.TotalSeconds;
                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F6)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    GotoSubPositionAndPause();
                }
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F7)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    GoBackSeconds(3);
                }
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F8)
            {
                if (mediaPlayer.VideoPlayer != null)
                {
                    _endSeconds = -1;
                    mediaPlayer.TogglePlayPause();
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.Modifiers == (Keys.Control | Keys.Alt | Keys.Shift) && e.KeyCode == Keys.W) // watermak
            {
                Encoding enc = GetCurrentEncoding();
                if (enc != Encoding.UTF8 && enc != Encoding.UTF32 && enc != Encoding.Unicode && enc != Encoding.UTF7)
                {
                    MessageBox.Show("Watermark only works with unicode file encoding");
                }
                else
                {
                    Watermark watermarkForm = new Watermark();
                    watermarkForm.Initialize(_subtitle, FirstSelectedIndex);
                    if (watermarkForm.ShowDialog(this) == DialogResult.OK)
                    {
                        watermarkForm.AddOrRemove(_subtitle);
                        RefreshSelectedParagraph();
                    }
                }
            }
            else if (e.Modifiers == (Keys.Control | Keys.Alt | Keys.Shift) && e.KeyCode == Keys.F) // Toggle HHMMSSFF / HHMMSSMMM
            {
                Configuration.Settings.General.UseTimeFormatHHMMSSFF = !Configuration.Settings.General.UseTimeFormatHHMMSSFF;
                RefreshTimeCodeMode();
            }
            else if (e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.U) // Ctrl+Shift+U = switch original/current
            {
                if (_subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0 && _networkSession == null)
                {
                    int firstIndex = FirstSelectedIndex;
                    double firstMs = -1;
                    if (firstIndex >= 0)
                        firstMs = _subtitle.Paragraphs[firstIndex].StartTime.TotalMilliseconds;

                    Subtitle temp = _subtitle;
                    _subtitle = _subtitleAlternate;
                    _subtitleAlternate = temp;

                    string tempName = _fileName;
                    _fileName = _subtitleAlternateFileName;
                    _subtitleAlternateFileName = tempName;

                    string tempChangeSubText = _changeSubtitleToString;
                    _changeSubtitleToString = _changeAlternateSubtitleToString;
                    _changeAlternateSubtitleToString = tempChangeSubText;

                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);

                    _subtitleListViewIndex = -1;
                    if (firstIndex >= 0 && _subtitle.Paragraphs.Count > firstIndex && _subtitle.Paragraphs[firstIndex].StartTime.TotalMilliseconds == firstMs)
                        SubtitleListview1.SelectIndexAndEnsureVisible(firstIndex);
                    else
                        RefreshSelectedParagraph();

                    SetTitle();

                    _fileDateTime = new DateTime();
                }
            }
            else if (e.Modifiers == (Keys.Control | Keys.Shift | Keys.Alt) && e.KeyCode == Keys.M) // Ctrl+Shift+U = switch original/current
            {
                if (_subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0 && _networkSession == null)
                {
                    if (ContinueNewOrExit())
                    {
                        Subtitle subtitle = new Subtitle();
                        foreach (var p in _subtitle.Paragraphs)
                        {
                            var newP = new Paragraph(p);
                            var original = Utilities.GetOriginalParagraph(_subtitle.GetIndex(p), p, _subtitleAlternate.Paragraphs);
                            if (original != null)
                                newP.Text += Environment.NewLine + Environment.NewLine + original.Text;
                            subtitle.Paragraphs.Add(newP);
                        }
                        RemoveAlternate(true);
                        FileNew();
                        _subtitle = subtitle;
                        _subtitleListViewIndex = -1;
                        ShowSource();
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        SubtitleListview1.SelectIndexAndEnsureVisible(0);

                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (e.KeyData == _toggleVideoDockUndock)
            {
                if (_isVideoControlsUnDocked)
                    RedockVideoControlsToolStripMenuItemClick(null, null);
                else
                    UndockVideoControlsToolStripMenuItemClick(null, null);
            }
            else if (mediaPlayer != null && mediaPlayer.VideoPlayer != null && e.KeyData == _video100MsLeft)
            {
                mediaPlayer.CurrentPosition -= 0.1;
                e.SuppressKeyPress = true;
            }
            else if (mediaPlayer != null && mediaPlayer.VideoPlayer != null && e.KeyData == _video100MsRight)
            {
                mediaPlayer.CurrentPosition += 0.1;
                e.SuppressKeyPress = true;
            }
            else if (mediaPlayer != null && mediaPlayer.VideoPlayer != null && e.KeyData == _video500MsLeft)
            {
                mediaPlayer.CurrentPosition -= 0.5;
                e.SuppressKeyPress = true;
            }
            else if (mediaPlayer != null && mediaPlayer.VideoPlayer != null && e.KeyData == _video500MsRight)
            {
                mediaPlayer.CurrentPosition += 0.5;
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == (Keys.Control | Keys.Alt | Keys.Shift) && e.KeyCode == Keys.B) // Ctrl+Alt+Shift+B = Beam subtitles
            {
                Beamer beamer = new Beamer(this, _subtitle, _subtitleListViewIndex);
                beamer.ShowDialog(this);
            }
            else if (e.KeyData == _mainVideoFullscreen) // fullscreen
            {
                GoFullscreen();
            }
            else if (audioVisualizer.Focused && audioVisualizer.NewSelectionParagraph != null && e.KeyData == _waveformAddTextAtHere)
            {
                addParagraphHereToolStripMenuItem_Click(null, null);
                e.SuppressKeyPress = true;
            }
            else if (audioVisualizer.Focused && e.KeyCode == Keys.Delete)
            {
                ToolStripMenuItemDeleteClick(null, null);
                e.SuppressKeyPress = true;
            }

            // TABS - MUST BE LAST
            else if (tabControlButtons.SelectedTab == tabPageAdjust)
            {
                if (_mainAdjustSelected100MsForward == e.KeyData)
                {
                    ShowEarlierOrLater(100, SelectionChoice.SelectionOnly);
                    e.SuppressKeyPress = true;
                }
                else if (_mainAdjustSelected100MsBack == e.KeyData)
                {
                    ShowEarlierOrLater(-100, SelectionChoice.SelectionOnly);
                    e.SuppressKeyPress = true;
                }
                else if (mediaPlayer.VideoPlayer != null)
                {
                    if (_mainAdjustSetStartAndOffsetTheRest == e.KeyData) // ((e.Modifiers == Keys.Control && e.KeyCode == Keys.Space))
                    {
                        ButtonSetStartAndOffsetRestClick(null, null);
                        e.SuppressKeyPress = true;
                    }
                    else if (_mainAdjustSetEndAndGotoNext == e.KeyData) // e.Modifiers == Keys.Shift && e.KeyCode == Keys.Space)
                    {
                        ButtonSetEndAndGoToNextClick(null, null);
                        e.SuppressKeyPress = true;
                    }
                    else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F9)
                    {
                        ButtonSetStartAndOffsetRestClick(null, null);
                        e.SuppressKeyPress = true;
                    }
                    else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F10)
                    {
                        ButtonSetEndAndGoToNextClick(null, null);
                        e.SuppressKeyPress = true;
                    }
                    else if ((e.Modifiers == Keys.None && e.KeyCode == Keys.F11) || _mainAdjustSetStart == e.KeyData)
                    {
                        buttonSetStartTime_Click(null, null);
                        e.SuppressKeyPress = true;
                    }
                    else if ((e.Modifiers == Keys.None && e.KeyCode == Keys.F11) || _mainAdjustSetStartOnly == e.KeyData)
                    {
                        SetStartTime(false);
                        e.SuppressKeyPress = true;
                    }
                    else if ((e.Modifiers == Keys.None && e.KeyCode == Keys.F12) || _mainAdjustSetEnd == e.KeyData)
                    {
                        StopAutoDuration();
                        ButtonSetEndClick(null, null);
                        e.SuppressKeyPress = true;
                    }
                    else if (_mainAdjustInsertViaEndAutoStartAndGoToNext == e.KeyData)
                    {
                        SetCurrentViaEndPositionAndGotoNext(FirstSelectedIndex);
                        e.SuppressKeyPress = true;
                    }
                    else if (_mainAdjustSetStartAutoDurationAndGoToNext == e.KeyData)
                    {
                        SetCurrentStartAutoDurationAndGotoNext(FirstSelectedIndex);
                        e.SuppressKeyPress = true;
                    }
                    else if (_mainAdjustSetEndNextStartAndGoToNext == e.KeyData)
                    {
                        SetCurrentEndNextStartAndGoToNext(FirstSelectedIndex);
                        e.SuppressKeyPress = true;
                    }
                    else if (_mainAdjustStartDownEndUpAndGoToNext == e.KeyData && _mainAdjustStartDownEndUpAndGoToNextParagraph == null)
                    {
                        _mainAdjustStartDownEndUpAndGoToNextParagraph = _subtitle.GetParagraphOrDefault(FirstSelectedIndex);
                        SetStartTime(true);
                        e.SuppressKeyPress = true;
                    }
                }
            }
            else if (tabControlButtons.SelectedTab == tabPageCreate && mediaPlayer.VideoPlayer != null)
            {
                if (e.Modifiers == Keys.Control && e.KeyCode == Keys.F9)
                {
                    InsertNewTextAtVideoPosition();
                    e.SuppressKeyPress = true;
                }
                else if ((e.Modifiers == Keys.Shift && e.KeyCode == Keys.F9) || _mainCreateInsertSubAtVideoPos == e.KeyData)
                {
                    var p = InsertNewTextAtVideoPosition();
                    p.Text = p.StartTime.ToShortString();
                    SubtitleListview1.SetText(_subtitle.GetIndex(p), p.Text);
                    textBoxListViewText.Text = p.Text;
                    e.SuppressKeyPress = true;
                }
                else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.F9)
                {
                    StopAutoDuration();
                    ButtonSetEndClick(null, null);
                    e.SuppressKeyPress = true;
                }
                else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F9)
                {
                    ButtonInsertNewTextClick(null, null);
                    e.SuppressKeyPress = true;
                }
                else if ((e.Modifiers == Keys.None && e.KeyCode == Keys.F10) || _mainCreatePlayFromJustBefore == e.KeyData)
                {
                    buttonBeforeText_Click(null, null);
                    e.SuppressKeyPress = true;
                }
                else if ((e.Modifiers == Keys.None && e.KeyCode == Keys.F11) || _mainCreateSetStart == e.KeyData)
                {
                    buttonSetStartTime_Click(null, null);
                    e.SuppressKeyPress = true;
                }
                else if ((e.Modifiers == Keys.None && e.KeyCode == Keys.F12) || _mainCreateSetEnd == e.KeyData)
                {
                    StopAutoDuration();
                    ButtonSetEndClick(null, null);
                    e.SuppressKeyPress = true;
                }
                else if (_mainCreateStartDownEndUp == e.KeyData)
                {
                    if (_mainCreateStartDownEndUpParagraph == null)
                        _mainCreateStartDownEndUpParagraph = InsertNewTextAtVideoPosition();
                    e.SuppressKeyPress = true;
                }
            }
            else if (tabControlButtons.SelectedTab == tabPageTranslate)
            {
                if (_mainTranslateCustomSearch1 == e.KeyData)
                {
                    e.SuppressKeyPress = true;
                    RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl1);
                }
                else if (_mainTranslateCustomSearch2 == e.KeyData)
                {
                    e.SuppressKeyPress = true;
                    RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl2);
                }
                else if (_mainTranslateCustomSearch3 == e.KeyData)
                {
                    e.SuppressKeyPress = true;
                    RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl3);
                }
                else if (_mainTranslateCustomSearch4 == e.KeyData)
                {
                    e.SuppressKeyPress = true;
                    RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl4);
                }
                else if (_mainTranslateCustomSearch5 == e.KeyData)
                {
                    e.SuppressKeyPress = true;
                    RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl5);
                }
                else if (_mainTranslateCustomSearch6 == e.KeyData)
                {
                    e.SuppressKeyPress = true;
                    RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl6);
                }
            }
            // put new entries above tabs
        }

        

        private void RunCustomSearch(string url)
        {
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(textBoxSearchWord.Text))
            {
                if (url.Contains("{0}"))
                    url = string.Format(url, Utilities.UrlEncode(textBoxSearchWord.Text));
                System.Diagnostics.Process.Start(url);
            }
        }
        
        private void GoFullscreen()
        {
            if (_videoPlayerUnDocked == null || _videoPlayerUnDocked.IsDisposed)
                UndockVideoControlsToolStripMenuItemClick(null, null);
            _videoPlayerUnDocked.Focus();
            _videoPlayerUnDocked.GoFullscreen();
            _videoPlayerUnDocked.RedockOnFullscreenEnd = true;
        }

        private void RefreshTimeCodeMode()
        {
            if (Configuration.Settings.General.UseTimeFormatHHMMSSFF)
            {
                numericUpDownDuration.DecimalPlaces = 2;
                numericUpDownDuration.Increment = (decimal)(0.01);

                toolStripSeparatorFrameRate.Visible = true;
                toolStripLabelFrameRate.Visible = true;
                toolStripComboBoxFrameRate.Visible = true;
                toolStripButtonGetFrameRate.Visible = true;
            }
            else
            {
                numericUpDownDuration.DecimalPlaces = 3;
                numericUpDownDuration.Increment = (decimal)(0.1);

                toolStripSeparatorFrameRate.Visible = Configuration.Settings.General.ShowFrameRate;
                toolStripLabelFrameRate.Visible = Configuration.Settings.General.ShowFrameRate;
                toolStripComboBoxFrameRate.Visible = Configuration.Settings.General.ShowFrameRate;
                toolStripButtonGetFrameRate.Visible = Configuration.Settings.General.ShowFrameRate;
            }

            SaveSubtitleListviewIndexes();
            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            RestoreSubtitleListviewIndexes();
            RefreshSelectedParagraph();
        }

        private void ReverseStartAndEndingForRTL()
        {
            int selectedIndex = FirstSelectedIndex;
            foreach (int index in SubtitleListview1.SelectedIndices)
            {
                Paragraph p = _subtitle.Paragraphs[index];
                p.Text = ReverseStartAndEndingForRTL(p.Text);
                SubtitleListview1.SetText(index , p.Text);
                if (index == selectedIndex)
                    textBoxListViewText.Text = p.Text;
            }
        }

        private static string ReverseString(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private static string ReverseParethesis(string s)
        {
            string k = "@__<<>___@";

            s = s.Replace("(", k);
            s = s.Replace(")", "(");
            s = s.Replace(k, ")");

            s = s.Replace("[", k);
            s = s.Replace("]", "[");
            s = s.Replace(k, "]");

            return s;
        }

        private static string ReverseStartAndEndingForRTL(string s)
        {
            var lines = s.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var newLines = new StringBuilder();
            foreach (string line in lines)
            {
                string s2 = line;

                bool startsWithItalic = false;
                if (s2.StartsWith("<i>"))
                {
                    startsWithItalic = true;
                    s2 = s2.Remove(0,3);
                }
                bool endsWithItalic = false;
                if (s2.EndsWith("</i>"))
                {
                    endsWithItalic = true;
                    s2 = s2.Remove(s2.Length-4, 4);
                }

                var pre = new StringBuilder();
                var post = new StringBuilder();
                var text = new StringBuilder();

                int i = 0;
                while (i < s2.Length && "- !?.\"،,():;[]".Contains(s2[i].ToString()))
                {
                    pre.Append(s2[i].ToString());
                    i++;
                }
                int j = s2.Length - 1;
                while (j > i && "- !?.\"،,():;[]".Contains(s2[j].ToString()))
                {
                    post.Append(s2[j].ToString());
                    j--;
                }
                if (startsWithItalic)
                    newLines.Append("<i>");
                newLines.Append(ReverseParethesis(post.ToString()));
                newLines.Append(s2.Substring(pre.Length, s2.Length - (pre.Length + post.Length)));
                newLines.Append(ReverseParethesis(ReverseString(pre.ToString())));
                if (endsWithItalic)
                    newLines.Append("</i>");
                newLines.AppendLine();

            }
            return newLines.ToString().Trim();
        }

        private void MergeDialogues()
        {
            if (SubtitleListview1.SelectedItems.Count == 2 && SubtitleListview1.SelectedIndices[0] + 1 == SubtitleListview1.SelectedIndices[1])
                MergeWithLineAfter(true);
        }

        private void ToggleDashes()
        {
            int index = FirstSelectedIndex;
            if (index >= 0)
            {
                bool hasStartDash = false;
                var p = _subtitle.Paragraphs[index];
                string[] lines = p.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("-") || line.Trim().StartsWith("<i>-") || line.Trim().StartsWith("<i> -"))
                        hasStartDash = true;
                }
                MakeHistoryForUndo(_language.BeforeToggleDialogueDashes);
                if (hasStartDash)
                    RemoveDashes();
                else
                    AddDashes();
            }
        }

        private void AddDashes()
        {
            foreach (int index in SubtitleListview1.SelectedIndices)
            {
                var p = _subtitle.Paragraphs[index];
                string[] lines = p.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("-") || line.Trim().StartsWith("<i>-") || line.Trim().StartsWith("<i> -"))
                        sb.AppendLine(line);
                    else if (line.Trim().StartsWith("<i>") && line.Trim().Length > 3)
                        sb.AppendLine("<i>-" + line.Substring(3));
                    else
                        sb.AppendLine("- " + line);
                }
                string text = sb.ToString().Trim(); ;
                _subtitle.Paragraphs[index].Text = text;
                SubtitleListview1.SetText(index, text);
                if (index == _subtitleListViewIndex)
                    textBoxListViewText.Text = text;
            }
        }

        private void RemoveDashes()
        {
            foreach (int index in SubtitleListview1.SelectedIndices)
            {
                var p = _subtitle.Paragraphs[index];
                string[] lines = p.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("-"))
                        sb.AppendLine(line.TrimStart().TrimStart('-').TrimStart());
                    else if (line.Trim().StartsWith("<i>-") || line.Trim().StartsWith("<i> -"))
                        sb.AppendLine("<i>" + line.TrimStart().Substring(3).TrimStart().TrimStart('-').TrimStart());
                    else
                        sb.AppendLine(line);
                }
                string text = sb.ToString().Trim(); ;
                _subtitle.Paragraphs[index].Text = text;
                SubtitleListview1.SetText(index, text);
                if (index == _subtitleListViewIndex)
                    textBoxListViewText.Text = text;
            }
        }

        private void SetTitle()
        {
            Text = Title;

            string seperator = " - ";
            if (!string.IsNullOrEmpty(_fileName))
            {
                Text = Text + seperator + _fileName;
                seperator = " + ";
            }

            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                Text = Text + seperator;
                if (string.IsNullOrEmpty(_fileName))
                    Text = Text + Configuration.Settings.Language.Main.New + " + ";
                if (!string.IsNullOrEmpty(_subtitleAlternateFileName))
                    Text = Text + _subtitleAlternateFileName;
                else
                    Text = Text + Configuration.Settings.Language.Main.New;
            }
        }

        private void SubtitleListview1KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control) //Ctrl+c = Copy to clipboard
            {
                var tmp = new Subtitle();
                foreach (int i in SubtitleListview1.SelectedIndices)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(i);
                    if (p != null)
                        tmp.Paragraphs.Add(new Paragraph(p));
                }
                if (tmp.Paragraphs.Count > 0)
                {
                    Clipboard.SetText(tmp.ToText(new SubRip()));
                }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyData == _mainListViewCopyText)
            {
                StringBuilder sb = new StringBuilder();
                foreach (int i in SubtitleListview1.SelectedIndices)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(i);
                    if (p != null)
                        sb.AppendLine(p.Text + Environment.NewLine);
                }
                if (sb.Length > 0)
                {
                    Clipboard.SetText(sb.ToString().Trim());
                }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) //Ctrl+vPaste from clipboard
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    var tmp = new Subtitle();
                    var format = new SubRip();
                    var list = new List<string>();
                    foreach (string line in text.Replace(Environment.NewLine, "|").Split("|".ToCharArray(), StringSplitOptions.None))
                        list.Add(line);
                    format.LoadSubtitle(tmp, list, null);
                    if (SubtitleListview1.SelectedItems.Count == 1 && tmp.Paragraphs.Count > 0)
                    {
                        MakeHistoryForUndo(_language.BeforeInsertLine);
                        _makeHistoryPaused = true;
                        Paragraph lastParagraph = null;
                        Paragraph lastTempParagraph = null;
                        foreach (Paragraph p in tmp.Paragraphs)
                        {
                            InsertAfter();
                            textBoxListViewText.Text = p.Text;
                            if (lastParagraph != null && lastTempParagraph != null)
                            {
                                double millisecondsBetween = p.StartTime.TotalMilliseconds - lastTempParagraph.EndTime.TotalMilliseconds;
                                timeUpDownStartTime.TimeCode = new TimeCode(TimeSpan.FromMilliseconds(lastParagraph.EndTime.TotalMilliseconds + millisecondsBetween));
                            }
                            SetDurationInSeconds(p.Duration.TotalSeconds);
                            lastParagraph = _subtitle.GetParagraphOrDefault(_subtitleListViewIndex);
                            lastTempParagraph = p;
                        }
                        RestartHistory();
                    }
                    else if (SubtitleListview1.Items.Count == 0 && tmp.Paragraphs.Count > 0)
                    { // insert into empty subtitle
                        MakeHistoryForUndo(_language.BeforeInsertLine);
                        foreach (Paragraph p in tmp.Paragraphs)
                        {
                            _subtitle.Paragraphs.Add(p);
                        }
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        SubtitleListview1.SelectIndexAndEnsureVisible(0, true);
                    }
                    else if (list.Count > 1 && list.Count < 500)
                    {
                        MakeHistoryForUndo(_language.BeforeInsertLine);
                        _makeHistoryPaused = true;
                        foreach (string line in list)
                        {
                            if (line.Trim().Length > 0)
                            {
                                InsertAfter();
                                textBoxListViewText.Text = Utilities.AutoBreakLine(line);
                            }
                        }
                        RestartHistory();
                    }
                }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.X && e.Modifiers == Keys.Control) //Ctrl+X = Cut to clipboard
            {
                var tmp = new Subtitle();
                foreach (int i in SubtitleListview1.SelectedIndices)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(i);
                    if (p != null)
                        tmp.Paragraphs.Add(new Paragraph(p));
                }
                e.SuppressKeyPress = true;
                _cutText = tmp.ToText(new SubRip());
                ToolStripMenuItemDeleteClick(null, null);
            }
            else if (e.KeyCode == Keys.A && e.Modifiers == Keys.Control) //SelectAll
            {
                foreach (ListViewItem item in SubtitleListview1.Items)
                    item.Selected = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.D && e.Modifiers == Keys.Control) //SelectFirstSelectedItemOnly
            {
                if (SubtitleListview1.SelectedItems.Count > 0)
                {
                    bool skipFirst = true;
                    foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    {
                        if (skipFirst)
                            skipFirst = false;
                        else
                            item.Selected = false;
                    }
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Delete && SubtitleListview1.SelectedItems.Count > 0) //Delete
            {
                ToolStripMenuItemDeleteClick(null, null);
            }
            else if (e.KeyData == _mainInsertBefore)
            {
                InsertBefore();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyData == _mainInsertAfter)
            {
                InsertAfter();
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == (Keys.Shift | Keys.Control) && e.KeyCode == Keys.I) //InverseSelection
            {
                foreach (ListViewItem item in SubtitleListview1.Items)
                    item.Selected = !item.Selected;
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Home)
            {
                SubtitleListview1.FirstVisibleIndex = -1;
                SubtitleListview1.SelectIndexAndEnsureVisible(0, true);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.End)
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(SubtitleListview1.Items.Count-1, true);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Enter)
            {
                SubtitleListview1_MouseDoubleClick(null, null);
            }
        }

        private void RestartHistory()
        {
            _listViewTextUndoLast = null;
            _listViewTextUndoIndex = -1;
            _listViewTextTicks = -1;
            _listViewAlternateTextUndoLast = null;
            _listViewAlternateTextTicks = -1;
            _undoIndex = _subtitle.HistoryItems.Count - 1;
            _makeHistoryPaused = false;
        }

        private void AutoBalanceLinesAndAllow2PlusLines()
        {
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                MakeHistoryForUndo(_language.BeforeAutoBalanceSelectedLines);

                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                {
                    Paragraph p = _subtitle.GetParagraphOrDefault(item.Index);
                    if (p != null)
                    {
                        string s = Utilities.AutoBreakLineMoreThanTwoLines(p.Text, Configuration.Settings.General.SubtitleLineMaximumLength);
                        if (s != p.Text)
                        {
                            p.Text = s;
                            SubtitleListview1.SetText(item.Index, p.Text);
                        }
                        if (_subtitleAlternate != null)
                        {
                            Paragraph original = Utilities.GetOriginalParagraph(item.Index, p, _subtitleAlternate.Paragraphs);
                            if (original != null)
                            {
                                string s2 = Utilities.AutoBreakLineMoreThanTwoLines(original.Text, Configuration.Settings.General.SubtitleLineMaximumLength);
                                if (s2 != original.Text)
                                {
                                    original.Text = s;
                                    SubtitleListview1.SetAlternateText(item.Index, original.Text);
                                }
                            }
                        }
                    }
                }
                ShowSource();
                RefreshSelectedParagraph();
            }
        }

        private void AdjustDisplayTimeForSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            AdjustDisplayTime(true);
        }

        private void FixCommonErrorsInSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            FixCommonErrors(true);
        }

        private void FindDoubleWordsToolStripMenuItemClick(object sender, EventArgs e)
        {
            var regex = new Regex(@"\b([\w]+)[ \r\n]+\1[ ,.!?]");
            _findHelper = new FindReplaceDialogHelper(FindType.RegEx, string.Format(_language.DoubleWordsViaRegEx, regex), regex, string.Empty, 0, 0, _subtitleListViewIndex);

            ReloadFromSourceView();
            FindNext();
        }

        private void ChangeCasingForSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            ChangeCasing(true);
        }

        private void CenterFormOnCurrentScreen()
        {
            Screen screen = Screen.FromControl(this);
            Left = screen.Bounds.X + ((screen.Bounds.Width - Width) / 2);
            Top = screen.Bounds.Y + ((screen.Bounds.Height - Height) / 2);
        }

        private void SortSubtitle(SubtitleSortCriteria subtitleSortCriteria, string description)
        {
            Paragraph firstSelectedParagraph = null;
            if (SubtitleListview1.SelectedItems.Count > 0)
                firstSelectedParagraph = _subtitle.Paragraphs[SubtitleListview1.SelectedItems[0].Index];

            _subtitleListViewIndex = -1;
            MakeHistoryForUndo(string.Format(_language.BeforeSortX, description));
            _subtitle.Sort(subtitleSortCriteria);
            ShowSource();
            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            SubtitleListview1.SelectIndexAndEnsureVisible(firstSelectedParagraph);
            ShowStatus(string.Format(_language.SortedByX, description));
        }

        private void SortNumberToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.Number, (sender as ToolStripItem).Text);
        }

        private void SortStartTimeToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.StartTime, (sender as ToolStripItem).Text);
        }

        private void SortEndTimeToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.EndTime, (sender as ToolStripItem).Text);
        }

        private void SortDisplayTimeToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.Duration, (sender as ToolStripItem).Text);
        }

        private void SortTextMaxLineLengthToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.TextMaxLineLength, (sender as ToolStripItem).Text);
        }

        private void SortTextTotalLengthToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.TextTotalLength, (sender as ToolStripItem).Text);
        }

        private void SortTextNumberOfLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.TextNumberOfLines, (sender as ToolStripItem).Text);
        }

        private void SortTextAlphabeticallytoolStripMenuItemClick(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.Text, (sender as ToolStripItem).Text);
        }

        private void textCharssecToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.TextCharactersPerSeconds, (sender as ToolStripItem).Text);
        }

        private void ChangeLanguageToolStripMenuItemClick(object sender, EventArgs e)
        {
            var cl = new ChooseLanguage();
            _formPositionsAndSizes.SetPositionAndSize(cl);
            if (cl.ShowDialog(this) == DialogResult.OK)
            {
                SetLanguage(cl.CultureName);
                Configuration.Settings.Save();
            }
            _formPositionsAndSizes.SavePositionAndSize(cl);
         }

        private void SetLanguage(string cultureName)
        {
            try
            {
                if (string.IsNullOrEmpty(cultureName) || cultureName == "en-US")
                {
                    Configuration.Settings.Language = new Language(); // default is en-US
                }
                else
                {
                    var reader = new System.IO.StreamReader(Path.Combine(Configuration.BaseDirectory, "Languages") + Path.DirectorySeparatorChar + cultureName + ".xml");
                    Configuration.Settings.Language = Language.Load(reader);
                    reader.Close();
                }
                Configuration.Settings.General.Language = cultureName;
                _languageGeneral = Configuration.Settings.Language.General;
                _language = Configuration.Settings.Language.Main;
                InitializeLanguage();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine +
                                Environment.NewLine +
                                exception.StackTrace, "Error loading language file");
                Configuration.Settings.Language = new Language(); // default is en-US
                _languageGeneral = Configuration.Settings.Language.General;
                _language = Configuration.Settings.Language.Main;
                InitializeLanguage();
                Configuration.Settings.General.Language = null;
            }
        }

        private void ToolStripMenuItemCompareClick(object sender, EventArgs e)
        {
            var compareForm = new Compare();
            if (_subtitleAlternate != null && _subtitleAlternateFileName != null)
                compareForm.Initialize(_subtitle, _fileName, _subtitleAlternate, _subtitleAlternateFileName);
            else
                compareForm.Initialize(_subtitle, _fileName, Configuration.Settings.Language.General.CurrentSubtitle);
            compareForm.Show();
        }

        private void ToolStripMenuItemAutoBreakLinesClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var autoBreakUnbreakLines = new AutoBreakUnbreakLines();
                var selectedLines = new Subtitle();
                foreach (int index in SubtitleListview1.SelectedIndices)
                    selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
                autoBreakUnbreakLines.Initialize(selectedLines, true);

                if (autoBreakUnbreakLines.ShowDialog() == DialogResult.OK && autoBreakUnbreakLines.FixedParagraphs.Count > 0)
                {
                    MakeHistoryForUndo(_language.BeforeAutoBalanceSelectedLines);

                    SubtitleListview1.BeginUpdate();
                    foreach (int index in SubtitleListview1.SelectedIndices)
                    {
                        Paragraph p = _subtitle.GetParagraphOrDefault(index);

                        int indexFixed = autoBreakUnbreakLines.FixedParagraphs.IndexOf(p);
                        if (indexFixed >= 0)
                        {
                            p.Text = Utilities.AutoBreakLine(p.Text, 5, autoBreakUnbreakLines.MininumLength, autoBreakUnbreakLines.MergeLinesShorterThan);
                            SubtitleListview1.SetText(index, p.Text);
                        }
                    }
                    SubtitleListview1.EndUpdate();
                    RefreshSelectedParagraph();
                    ShowStatus(string.Format(_language.NumberOfLinesAutoBalancedX, autoBreakUnbreakLines.FixedParagraphs.Count));
                }
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ToolStripMenuItemUnbreakLinesClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var autoBreakUnbreakLines = new AutoBreakUnbreakLines();
                var selectedLines = new Subtitle();
                foreach (int index in SubtitleListview1.SelectedIndices)
                    selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
                autoBreakUnbreakLines.Initialize(selectedLines, false);

                if (autoBreakUnbreakLines.ShowDialog() == DialogResult.OK && autoBreakUnbreakLines.FixedParagraphs.Count > 0)
                {
                    MakeHistoryForUndo(_language.BeforeRemoveLineBreaksInSelectedLines);

                    SubtitleListview1.BeginUpdate();
                    foreach (int index in SubtitleListview1.SelectedIndices)
                    {
                        Paragraph p = _subtitle.GetParagraphOrDefault(index);

                        int indexFixed = autoBreakUnbreakLines.FixedParagraphs.IndexOf(p);
                        if (indexFixed >= 0)
                        {
                            p.Text = Utilities.UnbreakLine(p.Text);
                            SubtitleListview1.SetText(index, p.Text);
                        }
                    }
                    SubtitleListview1.EndUpdate();
                    RefreshSelectedParagraph();
                    ShowStatus(string.Format(_language.NumberOfWithRemovedLineBreakX, autoBreakUnbreakLines.FixedParagraphs.Count));
                }
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MultipleReplaceToolStripMenuItemClick(object sender, EventArgs e)
        {
            var multipleReplace = new MultipleReplace();
            multipleReplace.Initialize(_subtitle);
            _formPositionsAndSizes.SetPositionAndSize(multipleReplace);
            if (multipleReplace.ShowDialog(this) == DialogResult.OK)
            {
                MakeHistoryForUndo(_language.BeforeMultipleReplace);
                SaveSubtitleListviewIndexes();

                for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
                {
                    _subtitle.Paragraphs[i].Text = multipleReplace.FixedSubtitle.Paragraphs[i].Text;
                }

                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();
                RefreshSelectedParagraph();
                ShowSource();
                ShowStatus(string.Format(_language.NumberOfLinesReplacedX , multipleReplace.FixCount));
            }
            _formPositionsAndSizes.SavePositionAndSize(multipleReplace);
        }

        private void ToolStripMenuItemImportDvdSubtitlesClick(object sender, EventArgs e)
        {
            if (ContinueNewOrExit())
            {
                var formSubRip = new DvdSubRip();
                if (formSubRip.ShowDialog(this) == DialogResult.OK)
                {
                    var showSubtitles = new DvdSubRipChooseLanguage();
                    showSubtitles.Initialize(formSubRip.MergedVobSubPacks, formSubRip.Palette, formSubRip.Languages, formSubRip.SelectedLanguage);
                    if (formSubRip.Languages.Count == 1 || showSubtitles.ShowDialog(this) == DialogResult.OK)
                    {
                        var formSubOcr = new VobSubOcr();
                        var subs = formSubRip.MergedVobSubPacks;
                        if (showSubtitles.SelectedVobSubMergedPacks != null)
                            subs = showSubtitles.SelectedVobSubMergedPacks;
                        formSubOcr.Initialize(subs, formSubRip.Palette, Configuration.Settings.VobSubOcr, formSubRip.SelectedLanguage);
                        if (formSubOcr.ShowDialog(this) == DialogResult.OK)
                        {
                            MakeHistoryForUndo(_language.BeforeImportingDvdSubtitle);
                            FileNew();
                            _subtitle.Paragraphs.Clear();
                            SetCurrentFormat(new SubRip().FriendlyName);
                            _subtitle.WasLoadedWithFrameNumbers = false;
                            _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                            foreach (Paragraph p in formSubOcr.SubtitleFromOcr.Paragraphs)
                            {
                                _subtitle.Paragraphs.Add(p);
                            }

                            ShowSource();
                            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                            _subtitleListViewIndex = -1;
                            SubtitleListview1.FirstVisibleIndex = -1;
                            SubtitleListview1.SelectIndexAndEnsureVisible(0, true);

                            _fileName = string.Empty;
                            Text = Title;

                            Configuration.Settings.Save();
                        }
                    }
                }
            }
        }

        private void ToolStripMenuItemSubIdxClick1(object sender, EventArgs e)
        {
            if (ContinueNewOrExit())
            {
                openFileDialog1.Title = _language.OpenVobSubFile;
                openFileDialog1.FileName = string.Empty;
                openFileDialog1.Filter = _language.VobSubFiles + "|*.sub";
                if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    ImportAndOcrVobSubSubtitleNew(openFileDialog1.FileName);
                    openFileDialog1.InitialDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                }
            }
        }

        private void SubtitleListview1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (Configuration.Settings.General.ListViewDoubleClickAction == 1)
            {
                GotoSubPositionAndPause();
            }
            else if (Configuration.Settings.General.ListViewDoubleClickAction == 2)
            {
                if (AutoRepeatContinueOn)
                    PlayCurrent();
                else
                    buttonBeforeText_Click(null, null);
            }
            else if (Configuration.Settings.General.ListViewDoubleClickAction == 3)
            {
                GotoSubPositionAndPause(-0.5);
            }
            else if (Configuration.Settings.General.ListViewDoubleClickAction == 4)
            {
                GotoSubPositionAndPause(-1.0);
            }
            else if (Configuration.Settings.General.ListViewDoubleClickAction == 5)
            {
                if (AutoRepeatContinueOn)
                    PlayCurrent();
                else
                {
                    if (SubtitleListview1.SelectedItems.Count > 0)
                    {
                        int index = SubtitleListview1.SelectedItems[0].Index;

                        mediaPlayer.Pause();
                        double pos = _subtitle.Paragraphs[index].StartTime.TotalSeconds;
                        if (pos > 1)
                            mediaPlayer.CurrentPosition = (_subtitle.Paragraphs[index].StartTime.TotalSeconds) - 1.0;
                        else
                            mediaPlayer.CurrentPosition = _subtitle.Paragraphs[index].StartTime.TotalSeconds;
                        mediaPlayer.Play();
                    }
                }
            }
            else if (Configuration.Settings.General.ListViewDoubleClickAction == 6)
            {
                GotoSubPositionAndPause();
                textBoxListViewText.Focus();
            }
            else if (Configuration.Settings.General.ListViewDoubleClickAction == 7)
            {
                textBoxListViewText.Focus();
            }
        }

        private void AddWordToNamesetcListToolStripMenuItemClick(object sender, EventArgs e)
        {
            var addToNamesList = new AddToNamesList();
            _formPositionsAndSizes.SetPositionAndSize(addToNamesList);
            addToNamesList.Initialize(_subtitle, textBoxListViewText.SelectedText);
            if (addToNamesList.ShowDialog(this) == DialogResult.OK)
                ShowStatus(string.Format(_language.NameXAddedToNamesEtcList, addToNamesList.NewName));
            else if (!string.IsNullOrEmpty(addToNamesList.NewName))
                ShowStatus(string.Format(_language.NameXNotAddedToNamesEtcList, addToNamesList.NewName));
            _formPositionsAndSizes.SavePositionAndSize(addToNamesList);
        }

        private bool IsUnicode
        {
            get
            {
                var enc = GetCurrentEncoding();
                return enc == Encoding.UTF8 || enc == Encoding.Unicode || enc == Encoding.UTF7 || enc == Encoding.UTF32 || enc == Encoding.BigEndianUnicode;
            }
        }

        private void EditToolStripMenuItemDropDownOpening(object sender, EventArgs e)
        {
            if (!IsUnicode || _subtitleListViewIndex == -1)
            {
                toolStripMenuItemInsertUnicodeCharacter.Visible = false;
                toolStripSeparatorInsertUnicodeCharacter.Visible = false;
            }
            else
            {
                if (toolStripMenuItemInsertUnicodeCharacter.DropDownItems.Count == 0)
                {
                    foreach (string s in Configuration.Settings.Tools.UnicodeSymbolsToInsert.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        toolStripMenuItemInsertUnicodeCharacter.DropDownItems.Add(s, null, InsertUnicodeGlyph);
                        if (Environment.OSVersion.Version.Major < 6 && Configuration.Settings.General.SubtitleFontName == Utilities.WinXp2kUnicodeFontName) // 6 == Vista/Win2008Server/Win7
                            toolStripMenuItemInsertUnicodeCharacter.DropDownItems[toolStripMenuItemInsertUnicodeCharacter.DropDownItems.Count - 1].Font = new Font(Utilities.WinXp2kUnicodeFontName, toolStripMenuItemInsertUnicodeSymbol.Font.Size);
                    }
                }
                toolStripMenuItemInsertUnicodeCharacter.Visible = toolStripMenuItemInsertUnicodeCharacter.DropDownItems.Count > 0;
                toolStripSeparatorInsertUnicodeCharacter.Visible = toolStripMenuItemInsertUnicodeCharacter.DropDownItems.Count > 0;
            }
        }

        private void InsertUnicodeGlyph(object sender, EventArgs e)
        {
            var item = sender as ToolStripItem;
            if (item != null)
            {
                string s = item.Text;

                if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                {
                    textBoxSource.Text = textBoxSource.Text.Insert(textBoxSource.SelectionStart, s);
                }
                else
                {
                    if (textBoxListViewTextAlternate.Visible && textBoxListViewTextAlternate.Enabled && textBoxListViewTextAlternate.Focused)
                        textBoxListViewTextAlternate.Text = textBoxListViewTextAlternate.Text.Insert(textBoxListViewTextAlternate.SelectionStart, s);
                    else
                        textBoxListViewText.Text = textBoxListViewText.Text.Insert(textBoxListViewText.SelectionStart, s);
                }
            }
        }

        private void ToolStripMenuItemAutoMergeShortLinesClick(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var formMergeShortLines = new MergeShortLines();
                _formPositionsAndSizes.SetPositionAndSize(formMergeShortLines);
                formMergeShortLines.Initialize(_subtitle);
                if (formMergeShortLines.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeMergeShortLines);
                    _subtitle = formMergeShortLines.MergedSubtitle;
                    ShowStatus(string.Format(_language.MergedShortLinesX, formMergeShortLines.NumberOfMerges));
                    SaveSubtitleListviewIndexes();
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                }
                _formPositionsAndSizes.SavePositionAndSize(formMergeShortLines);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void toolStripMenuItemAutoSplitLongLines_Click(object sender, EventArgs e)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var splitLongLines = new SplitLongLines();
                _formPositionsAndSizes.SetPositionAndSize(splitLongLines);
                splitLongLines.Initialize(_subtitle);
                if (splitLongLines.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeMergeShortLines);
                    _subtitle = splitLongLines.SplittedSubtitle;
                    ShowStatus(string.Format(_language.MergedShortLinesX, splitLongLines.NumberOfSplits));
                    SaveSubtitleListviewIndexes();
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                }
                _formPositionsAndSizes.SavePositionAndSize(splitLongLines);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetMinimalDisplayTimeDifferenceToolStripMenuItemClick(object sender, EventArgs e)
        {
            var setMinDisplayDiff = new SetMinimumDisplayTimeBetweenParagraphs();
            _formPositionsAndSizes.SetPositionAndSize(setMinDisplayDiff);
            setMinDisplayDiff.Initialize(_subtitle);
            if (setMinDisplayDiff.ShowDialog() == DialogResult.OK && setMinDisplayDiff.FixCount > 0)
            {
                MakeHistoryForUndo(_language.BeforeSetMinimumDisplayTimeBetweenParagraphs);
                _subtitle.Paragraphs.Clear();
                foreach (Paragraph p in setMinDisplayDiff.FixedSubtitle.Paragraphs)
                    _subtitle.Paragraphs.Add(p);
                _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                ShowStatus(string.Format(_language.XMinimumDisplayTimeBetweenParagraphsChanged, setMinDisplayDiff.FixCount));
                SaveSubtitleListviewIndexes();
                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();
            }
            _formPositionsAndSizes.SavePositionAndSize(setMinDisplayDiff);
        }

        private void ToolStripMenuItemImportTextClick(object sender, EventArgs e)
        {
            var importText = new ImportText();
            if (importText.ShowDialog(this) == DialogResult.OK)
            {
                if (ContinueNewOrExit())
                {
                    MakeHistoryForUndo(_language.BeforeImportText);
                    ResetSubtitle();
                    if (!string.IsNullOrEmpty(importText.VideoFileName))
                        OpenVideo(importText.VideoFileName);

                    ResetSubtitle();
                    _subtitleListViewIndex = -1;
                    _subtitle = importText.FixedSubtitle;
                    _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    ShowStatus(_language.TextImported);
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                }
            }
        }

        private void toolStripMenuItemPointSync_Click(object sender, EventArgs e)
        {
            SyncPointsSync pointSync = new SyncPointsSync();
            pointSync.Initialize(_subtitle, _fileName,  _videoFileName, _videoAudioTrackNumber);
            _formPositionsAndSizes.SetPositionAndSize(pointSync);
            mediaPlayer.Pause();
            if (pointSync.ShowDialog(this) == DialogResult.OK)
            {
                _subtitleListViewIndex = -1;
                MakeHistoryForUndo(_language.BeforePointSynchronization);
                _subtitle = pointSync.FixedSubtitle;
                _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                ShowStatus(_language.PointSynchronizationDone);
                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            }
            _videoFileName = pointSync.VideoFileName;
            _formPositionsAndSizes.SavePositionAndSize(pointSync);
        }

        private void pointSyncViaOtherSubtitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyncPointsSync pointSync = new SyncPointsSync();
            openFileDialog1.Title = _language.OpenOtherSubtitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Subtitle sub = new Subtitle();
                Encoding enc;
                SubtitleFormat f = sub.LoadSubtitle(openFileDialog1.FileName, out enc, null);
                if (f == null)
                {
                    ShowUnknownSubtitle();
                    return;
                }

                pointSync.Initialize(_subtitle, _fileName, _videoFileName, _videoAudioTrackNumber, openFileDialog1.FileName, sub);
                mediaPlayer.Pause();
                if (pointSync.ShowDialog(this) == DialogResult.OK)
                {
                    _subtitleListViewIndex = -1;
                    MakeHistoryForUndo(_language.BeforePointSynchronization);
                    _subtitle = pointSync.FixedSubtitle;
                    _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    ShowStatus(_language.PointSynchronizationDone);
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                }
                _videoFileName = pointSync.VideoFileName;
            }
        }

        private void toolStripMenuItemImportTimeCodes_Click(object sender, EventArgs e)
        {
            if (_subtitle.Paragraphs.Count < 1)
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            openFileDialog1.Title = _languageGeneral.OpenSubtitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Encoding encoding = null;
                Subtitle timeCodeSubtitle = new Subtitle();
                SubtitleFormat format = timeCodeSubtitle.LoadSubtitle(openFileDialog1.FileName, out encoding, encoding);
                if (format == null)
                {
                    ShowUnknownSubtitle();
                    return;
                }

                MakeHistoryForUndo(_language.BeforeTimeCodeImport);

                if (GetCurrentSubtitleFormat().IsFrameBased)
                    timeCodeSubtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                else
                    timeCodeSubtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

                int count = 0;
                for (int i = 0; i < timeCodeSubtitle.Paragraphs.Count; i++)
                {
                    Paragraph existing = _subtitle.GetParagraphOrDefault(i);
                    Paragraph newTimeCode = timeCodeSubtitle.GetParagraphOrDefault(i);
                    if (existing == null || newTimeCode == null)
                        break;
                    existing.StartTime.TotalMilliseconds = newTimeCode.StartTime.TotalMilliseconds;
                    existing.EndTime.TotalMilliseconds = newTimeCode.EndTime.TotalMilliseconds;
                    existing.StartFrame = newTimeCode.StartFrame;
                    existing.EndFrame = newTimeCode.EndFrame;
                    count++;

                }
                ShowStatus(string.Format(_language.TimeCodeImportedFromXY, Path.GetFileName(openFileDialog1.FileName), count));
                SaveSubtitleListviewIndexes();
                ShowSource();
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();
            }

        }

        private void toolStripMenuItemTranslationMode_Click(object sender, EventArgs e)
        {
            if (SubtitleListview1.IsAlternateTextColumnVisible)
            {
                SubtitleListview1.HideAlternateTextColumn();
                SubtitleListview1.AutoSizeAllColumns(this);
                _subtitleAlternate = new Subtitle();
                _subtitleAlternateFileName = null;

                buttonUnBreak.Visible = true;
                buttonAutoBreak.Visible = true;
                textBoxListViewTextAlternate.Visible = false;
                labelAlternateText.Visible = false;
                labelAlternateCharactersPerSecond.Visible = false;
                labelTextAlternateLineLengths.Visible = false;
                labelAlternateSingleLine.Visible = false;
                labelTextAlternateLineTotal.Visible = false;
                textBoxListViewText.Width = (groupBoxEdit.Width - (textBoxListViewText.Left + 8 + buttonUnBreak.Width));
                textBoxListViewText.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

                labelCharactersPerSecond.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelCharactersPerSecond.Width);
                labelTextLineTotal.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelTextLineTotal.Width);
            }
            else
            {
                OpenAlternateSubtitle();
            }
            SetTitle();
        }

        private void OpenAlternateSubtitle()
        {
            if (ContinueNewOrExitAlternate())
            {
                SaveSubtitleListviewIndexes();
                openFileDialog1.Title = Configuration.Settings.Language.General.OpenOriginalSubtitleFile;
                openFileDialog1.FileName = string.Empty;
                openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
                if (!(openFileDialog1.ShowDialog(this) == DialogResult.OK))
                    return;

                if (!LoadAlternateSubtitleFile(openFileDialog1.FileName))
                    return;

                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                RestoreSubtitleListviewIndexes();

                Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
                Configuration.Settings.Save();
                UpdateRecentFilesUI();
            }
        }

        private bool LoadAlternateSubtitleFile(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            if (Path.GetExtension(fileName).ToLower() == ".sub" && IsVobSubFile(fileName, false))
                return false;

            var fi = new FileInfo(fileName);
            if (fi.Length > 1024 * 1024 * 10) // max 10 mb
            {
                if (MessageBox.Show(string.Format(_language.FileXIsLargerThan10Mb + Environment.NewLine +
                                                    Environment.NewLine +
                                                    _language.ContinueAnyway,
                                                    fileName), Title, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                    return false;
            }

            Encoding encoding;
            _subtitleAlternate = new Subtitle();
            _subtitleAlternateFileName = fileName;
            SubtitleFormat format = _subtitleAlternate.LoadSubtitle(fileName, out encoding, null);
            if (format == null)
                return false;

            if (format.IsFrameBased)
                _subtitleAlternate.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
            else
                _subtitleAlternate.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

            SetupAlternateEdit();
            return true;
        }

        private void SetupAlternateEdit()
        {
            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate.Paragraphs.Count > 1)
            {
                InsertMissingParagraphs(_subtitle, _subtitleAlternate);
                InsertMissingParagraphs(_subtitleAlternate, _subtitle);

                buttonUnBreak.Visible = false;
                buttonAutoBreak.Visible = false;
                buttonSplitLine.Visible = false;

                textBoxListViewText.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                textBoxListViewText.Width = (groupBoxEdit.Width - (textBoxListViewText.Left + 10)) / 2;
                textBoxListViewTextAlternate.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                textBoxListViewTextAlternate.Left = textBoxListViewText.Left + textBoxListViewText.Width + 3;
                textBoxListViewTextAlternate.Width = textBoxListViewText.Width;
                textBoxListViewTextAlternate.Visible = true;
                labelAlternateText.Text = Configuration.Settings.Language.General.OriginalText;
                labelAlternateText.Visible = true;
                labelAlternateCharactersPerSecond.Visible = true;
                labelTextAlternateLineLengths.Visible = true;
                labelAlternateSingleLine.Visible = true;
                labelTextAlternateLineTotal.Visible = true;

                labelCharactersPerSecond.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelCharactersPerSecond.Width);
                labelTextLineTotal.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelTextLineTotal.Width);
                Main_Resize(null, null);
                _changeAlternateSubtitleToString = _subtitleAlternate.ToText(new SubRip()).Trim();

                SetTitle();
            }

            SubtitleListview1.ShowAlternateTextColumn(Configuration.Settings.Language.General.OriginalText);
            SubtitleListview1.AutoSizeAllColumns(this);
        }

        private void InsertMissingParagraphs(Subtitle masterSubtitle, Subtitle insertIntoSubtitle)
        {
            int index = 0;
            foreach (Paragraph p in masterSubtitle.Paragraphs)
            {

                Paragraph insertParagraph = Utilities.GetOriginalParagraph(index, p, insertIntoSubtitle.Paragraphs);
                if (insertParagraph == null)
                {
                    insertParagraph = new Paragraph(p);
                    insertParagraph.Text = string.Empty;
                    insertIntoSubtitle.InsertParagraphInCorrectTimeOrder(insertParagraph);
                }
                index++;
            }
            insertIntoSubtitle.Renumber(1);
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void OpenVideo(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                if (_loading)
                {
                    _videoFileName = fileName;
                    return;
                }

                FileInfo fi = new FileInfo(fileName);
                if (fi.Length < 1000)
                    return;

                Cursor = Cursors.WaitCursor;
                VideoFileName = fileName;
                if (mediaPlayer.VideoPlayer != null)
                {
                    mediaPlayer.Pause();
                    mediaPlayer.VideoPlayer.DisposeVideoPlayer();
                }
                _endSeconds = -1;

                _videoInfo = ShowVideoInfo(fileName);
                if (_videoInfo.FramesPerSecond > 0)
                    toolStripComboBoxFrameRate.Text = string.Format("{0:0.###}", _videoInfo.FramesPerSecond);

                Utilities.InitializeVideoPlayerAndContainer(fileName, _videoInfo, mediaPlayer, VideoLoaded, VideoEnded);
                mediaPlayer.ShowFullscreenButton = Configuration.Settings.General.VideoPlayerShowFullscreenButton;
                mediaPlayer.OnButtonClicked -= MediaPlayer_OnButtonClicked;
                mediaPlayer.OnButtonClicked += MediaPlayer_OnButtonClicked;
                mediaPlayer.Volume = 0;
                labelVideoInfo.Text = Path.GetFileName(fileName) + " " + _videoInfo.Width + "x" + _videoInfo.Height + " " + _videoInfo.VideoCodec.Trim();
                if (_videoInfo.FramesPerSecond > 0)
                    labelVideoInfo.Text = labelVideoInfo.Text + " " + string.Format("{0:0.0##}", _videoInfo.FramesPerSecond);


                string peakWaveFileName = GetPeakWaveFileName(fileName);
                string spectrogramFolder = GetSpectrogramFolder(fileName);
                if (File.Exists(peakWaveFileName))
                {
                    audioVisualizer.WavePeaks = new WavePeakGenerator(peakWaveFileName);
                    audioVisualizer.ResetSpectrogram();
                    audioVisualizer.InitializeSpectrogram(spectrogramFolder);
                    toolStripComboBoxWaveForm_SelectedIndexChanged(null, null);
                    audioVisualizer.WavePeaks.GenerateAllSamples();
                    audioVisualizer.WavePeaks.Close();
                    SetWaveFormPosition(0, 0, 0);
                    timerWaveForm.Start();
                }
                Cursor = Cursors.Default;

                SetUndockedWindowsTitle();
            }
        }

        void MediaPlayer_OnButtonClicked(object sender, EventArgs e)
        {
            PictureBox pb = (PictureBox)(sender as PictureBox);
            if (pb != null && (sender as PictureBox).Name == "_pictureBoxFullscreenOver")
                GoFullscreen();
        }

        private void SetWaveFormPosition(double startPositionSeconds, double currentVideoPositionSeconds, int subtitleIndex)
        {
            if (SubtitleListview1.IsAlternateTextColumnVisible && Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable)
            {
                int index = -1;
                if (SubtitleListview1.SelectedItems.Count > 0 && _subtitle.Paragraphs.Count > 0)
                {
                    int i = SubtitleListview1.SelectedItems[0].Index;
                    var p = Utilities.GetOriginalParagraph(i, _subtitle.Paragraphs[i], _subtitleAlternate.Paragraphs);
                    index = _subtitleAlternate.GetIndex(p);
                }
                audioVisualizer.SetPosition(startPositionSeconds, _subtitleAlternate, currentVideoPositionSeconds, index);
            }
            else
            {
                audioVisualizer.SetPosition(startPositionSeconds, _subtitle, currentVideoPositionSeconds, subtitleIndex);
            }
        }

        void VideoLoaded(object sender, EventArgs e)
        {
            mediaPlayer.Stop();
            mediaPlayer.Volume = Configuration.Settings.General.VideoPlayerDefaultVolume;
            timer1.Start();

            trackBarWaveFormPosition.Maximum = (int)mediaPlayer.Duration;

            if (_videoLoadedGoToSubPosAndPause)
            {
                Application.DoEvents();
                _videoLoadedGoToSubPosAndPause = false;
                GotoSubPositionAndPause();
            }
        }

        void VideoEnded(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
        }

        private VideoInfo ShowVideoInfo(string fileName)
        {
            return Utilities.GetVideoInfo(fileName, delegate { Application.DoEvents(); });
        }

        private void TryToFindAndOpenVideoFile(string fileNameNoExtension)
        {
            string movieFileName = null;

            foreach (string extension in Utilities.GetMovieFileExtensions())
            {
                movieFileName = fileNameNoExtension + extension;
                if (File.Exists(movieFileName))
                    break;
            }

            if (movieFileName != null && File.Exists(movieFileName))
            {
                OpenVideo(movieFileName);
            }
            else if (fileNameNoExtension.Contains("."))
            {
                fileNameNoExtension = fileNameNoExtension.Substring(0, fileNameNoExtension.LastIndexOf("."));
                TryToFindAndOpenVideoFile(fileNameNoExtension);
            }
        }

        internal void GoBackSeconds(double seconds)
        {
            if (mediaPlayer != null)
            {
                if (mediaPlayer.CurrentPosition > seconds)
                    mediaPlayer.CurrentPosition -= seconds;
                else
                    mediaPlayer.CurrentPosition = 0;
                ShowSubtitle();
            }
        }

        private void ButtonStartHalfASecondBackClick(object sender, EventArgs e)
        {
            GoBackSeconds(0.5);
        }

        private void ButtonStartThreeSecondsBackClick(object sender, EventArgs e)
        {
            GoBackSeconds(3.0);
        }

        private void ButtonStartOneMinuteBackClick(object sender, EventArgs e)
        {
            GoBackSeconds(60);
        }

        private void ButtonStartHalfASecondAheadClick(object sender, EventArgs e)
        {
            GoBackSeconds(-0.5);
        }

        private void ButtonStartThreeSecondsAheadClick(object sender, EventArgs e)
        {
            GoBackSeconds(-3);
        }

        private void ButtonStartOneMinuteAheadClick(object sender, EventArgs e)
        {
            GoBackSeconds(-60);
        }

        private void videoTimer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer != null)
            {
                if (!mediaPlayer.IsPaused)
                {
                    mediaPlayer.RefreshProgressBar();
                    ShowSubtitle();
                }
            }
        }

        private void videoModeHiddenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HideVideoPlayer();
        }

        private void createadjustLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowVideoPlayer();
        }

        private void HideVideoPlayer()
        {
            if (mediaPlayer != null)
                mediaPlayer.Pause();

            splitContainer1.Panel2Collapsed = true;
            splitContainerMain.Panel2Collapsed = true;
            Main_Resize(null, null);
        }

        private void ShowVideoPlayer()
        {
            if (_isVideoControlsUnDocked)
            {
                ShowHideUnDockedVideoControls();
            }
            else
            {
                if (toolStripButtonToggleVideo.Checked && toolStripButtonToggleWaveForm.Checked)
                {
                    splitContainer1.Panel2Collapsed = false;
                    MoveVideoUp();
                }
                else
                {
                    splitContainer1.Panel2Collapsed = true;
                    MoveVideoDown();
                }

                splitContainerMain.Panel2Collapsed = false;
                if (toolStripButtonToggleVideo.Checked)
                {
                    if (audioVisualizer.Visible)
                    {
                        audioVisualizer.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                    }
                    else
                    {
                        panelVideoPlayer.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                    }
                }
                else if (audioVisualizer.Visible)
                {
                    audioVisualizer.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                }
                audioVisualizer.Width = groupBoxVideo.Width - (audioVisualizer.Left + 10);

                checkBoxSyncListViewWithVideoWhilePlaying.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                panelWaveFormControls.Left = audioVisualizer.Left;
                trackBarWaveFormPosition.Left = panelWaveFormControls.Left + panelWaveFormControls.Width + 5;
                trackBarWaveFormPosition.Width = audioVisualizer.Left + audioVisualizer.Width - trackBarWaveFormPosition.Left + 5;
            }

            if (mediaPlayer.VideoPlayer == null && !string.IsNullOrEmpty(_fileName))
                TryToFindAndOpenVideoFile(Path.Combine(Path.GetDirectoryName(_fileName), Path.GetFileNameWithoutExtension(_fileName)));
            Main_Resize(null, null);
        }

        private void ShowHideUnDockedVideoControls()
        {
            if (_videoPlayerUnDocked == null || _videoPlayerUnDocked.IsDisposed)
                UnDockVideoPlayer();
            _videoPlayerUnDocked.Visible = false;
            if (toolStripButtonToggleVideo.Checked)
            {
                _videoPlayerUnDocked.Show(this);
                if (_videoPlayerUnDocked.WindowState == FormWindowState.Minimized)
                    _videoPlayerUnDocked.WindowState = FormWindowState.Normal;
            }

            if (_waveFormUnDocked == null || _waveFormUnDocked.IsDisposed)
                UnDockWaveForm();
            _waveFormUnDocked.Visible = false;
            if (toolStripButtonToggleWaveForm.Checked)
            {
                _waveFormUnDocked.Show(this);
                if (_waveFormUnDocked.WindowState == FormWindowState.Minimized)
                    _waveFormUnDocked.WindowState = FormWindowState.Normal;
            }

            if (toolStripButtonToggleVideo.Checked || toolStripButtonToggleWaveForm.Checked)
            {
                if (_videoControlsUnDocked == null || _videoControlsUnDocked.IsDisposed)
                    UnDockVideoButtons();
                _videoControlsUnDocked.Visible = false;
                _videoControlsUnDocked.Show(this);
            }
            else
            {
                if (_videoControlsUnDocked != null && !_videoControlsUnDocked.IsDisposed)
                    _videoControlsUnDocked.Visible = false;
            }
        }

        private void MoveVideoUp()
        {
            if (splitContainer1.Panel2.Controls.Count == 0)
            {
                var control = panelVideoPlayer;
                groupBoxVideo.Controls.Remove(control);
                splitContainer1.Panel2.Controls.Add(control);
            }
            panelVideoPlayer.Top = 0;
            panelVideoPlayer.Left = 0;
            panelVideoPlayer.Height = splitContainer1.Panel2.Height - 2;
            panelVideoPlayer.Width = splitContainer1.Panel2.Width - 2;
        }

        private void MoveVideoDown()
        {
            if (splitContainer1.Panel2.Controls.Count > 0)
            {
                var control = panelVideoPlayer;
                splitContainer1.Panel2.Controls.Clear();
                groupBoxVideo.Controls.Add(control);
            }
            panelVideoPlayer.Top = 32;
            panelVideoPlayer.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
            panelVideoPlayer.Height = groupBoxVideo.Height - (panelVideoPlayer.Top + 5);
            panelVideoPlayer.Width = groupBoxVideo.Width - (panelVideoPlayer.Left + 5);
        }

        private void FixLargeFonts()
        {
            Graphics graphics = this.CreateGraphics();
            SizeF textSize = graphics.MeasureString(buttonPlayPrevious.Text, this.Font);
            if (textSize.Height > buttonPlayPrevious.Height - 4)
            {
                int newButtonHeight = (int)(textSize.Height + 7 + 0.5);
                Utilities.SetButtonHeight(this, newButtonHeight, 1);

                // List view
                SubtitleListview1.InitializeTimeStampColumWidths(this);
                int adjustUp = 8;
                SubtitleListview1.Height = SubtitleListview1.Height - adjustUp;
                groupBoxEdit.Top = groupBoxEdit.Top - adjustUp;
                groupBoxEdit.Height = groupBoxEdit.Height + adjustUp;
                numericUpDownDuration.Left = timeUpDownStartTime.Left + timeUpDownStartTime.Width;
                numericUpDownDuration.Width = numericUpDownDuration.Width + 5;
                labelDuration.Left = numericUpDownDuration.Left - 3;
                labelAutoDuration.Left = labelDuration.Left - (labelAutoDuration.Width - 5);

                // Video controls - Create
                timeUpDownVideoPosition.Left = labelVideoPosition.Left + labelVideoPosition.Width;
                int buttonWidth = labelVideoPosition.Width + timeUpDownVideoPosition.Width;
                buttonInsertNewText.Width = buttonWidth;
                buttonBeforeText.Width = buttonWidth;
                buttonGotoSub.Width = buttonWidth;
                buttonSetStartTime.Width = buttonWidth;
                buttonSetEnd.Width = buttonWidth;
                int FKeyLeft = buttonInsertNewText.Left + buttonInsertNewText.Width;
                labelCreateF9.Left = FKeyLeft;
                labelCreateF10.Left = FKeyLeft;
                labelCreateF11.Left = FKeyLeft;
                labelCreateF12.Left = FKeyLeft;
                buttonForward1.Left = buttonInsertNewText.Left + buttonInsertNewText.Width - buttonForward1.Width;
                numericUpDownSec1.Width = buttonInsertNewText.Width - (numericUpDownSec1.Left + buttonForward1.Width);
                buttonForward2.Left = buttonInsertNewText.Left + buttonInsertNewText.Width - buttonForward2.Width;
                numericUpDownSec2.Width = buttonInsertNewText.Width - (numericUpDownSec2.Left + buttonForward2.Width);

                // Video controls - Adjust
                timeUpDownVideoPositionAdjust.Left = labelVideoPosition2.Left + labelVideoPosition2.Width;
                buttonSetStartAndOffsetRest.Width = buttonWidth;
                buttonSetEndAndGoToNext.Width = buttonWidth;
                buttonAdjustSetStartTime.Width = buttonWidth;
                buttonAdjustSetEndTime.Width = buttonWidth;
                buttonAdjustPlayBefore.Width = buttonWidth;
                buttonAdjustGoToPosAndPause.Width = buttonWidth;
                labelAdjustF9.Left = FKeyLeft;
                labelAdjustF10.Left = FKeyLeft;
                labelAdjustF11.Left = FKeyLeft;
                labelAdjustF12.Left = FKeyLeft;
                buttonAdjustSecForward1.Left = buttonInsertNewText.Left + buttonInsertNewText.Width - buttonAdjustSecForward1.Width;
                numericUpDownSecAdjust1.Width = buttonInsertNewText.Width - (numericUpDownSecAdjust2.Left + buttonAdjustSecForward1.Width);
                buttonAdjustSecForward2.Left = buttonInsertNewText.Left + buttonInsertNewText.Width - buttonAdjustSecForward2.Width;
                numericUpDownSecAdjust2.Width = buttonInsertNewText.Width - (numericUpDownSecAdjust2.Left + buttonAdjustSecForward2.Width);

                tabControl1_SelectedIndexChanged(null, null);
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (_loading)
                return;

            panelVideoPlayer.Invalidate();

            MainResize();

            // Due to strange bug in listview when maximizing
            SaveSubtitleListviewIndexes();
            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            RestoreSubtitleListviewIndexes();

            panelVideoPlayer.Refresh();
        }

        private void MainResize()
        {
            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null &&
                _subtitleAlternate.Paragraphs.Count > 0)
            {
                textBoxListViewText.Width = (groupBoxEdit.Width - (textBoxListViewText.Left + 10))/2;
                textBoxListViewTextAlternate.Left = textBoxListViewText.Left + textBoxListViewText.Width + 3;
                labelAlternateText.Left = textBoxListViewTextAlternate.Left;

                textBoxListViewTextAlternate.Width = textBoxListViewText.Width;

                labelAlternateCharactersPerSecond.Left = textBoxListViewTextAlternate.Left +
                                                         (textBoxListViewTextAlternate.Width -
                                                          labelAlternateCharactersPerSecond.Width);
                labelTextAlternateLineLengths.Left = textBoxListViewTextAlternate.Left;
                labelAlternateSingleLine.Left = labelTextAlternateLineLengths.Left + labelTextAlternateLineLengths.Width;
                labelTextAlternateLineTotal.Left = textBoxListViewTextAlternate.Left +
                                                   (textBoxListViewTextAlternate.Width - labelTextAlternateLineTotal.Width);
            }

            labelCharactersPerSecond.Left = textBoxListViewText.Left +
                                            (textBoxListViewText.Width - labelCharactersPerSecond.Width);
            labelTextLineTotal.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelTextLineTotal.Width);
            SubtitleListview1.AutoSizeAllColumns(this);
        }

        private void PlayCurrent()
        {
            if (_subtitleListViewIndex >= 0)
            {
                GotoSubtitleIndex(_subtitleListViewIndex);
                textBoxListViewText.Focus();
                ReadyAutoRepeat();
                PlayPart(_subtitle.Paragraphs[_subtitleListViewIndex]);
            }
        }

        private void ReadyAutoRepeat()
        {
            if (checkBoxAutoRepeatOn.Checked)
            {
                _repeatCount = int.Parse(comboBoxAutoRepeat.Text);
            }
            else
            {
                _repeatCount = -1;
            }
            labelStatus.Text = _language.VideoControls.Playing;
        }

        private void Next()
        {
            int newIndex = _subtitleListViewIndex + 1;
            if (newIndex < _subtitle.Paragraphs.Count)
            {
                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    item.Selected = false;
                SubtitleListview1.Items[newIndex].Selected = true;
                SubtitleListview1.Items[newIndex].EnsureVisible();
                textBoxListViewText.Focus();
                textBoxListViewText.SelectAll();
                _subtitleListViewIndex = newIndex;
                GotoSubtitleIndex(newIndex);
                ShowSubtitle();
                PlayCurrent();
            }
        }

        private void PlayPrevious()
        {
            if (_subtitleListViewIndex > 0)
            {
                int newIndex = _subtitleListViewIndex - 1;
                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    item.Selected = false;
                SubtitleListview1.Items[newIndex].Selected = true;
                SubtitleListview1.Items[newIndex].EnsureVisible();
                textBoxListViewText.Focus();
                textBoxListViewText.SelectAll();
                GotoSubtitleIndex(newIndex);
                ShowSubtitle();
                _subtitleListViewIndex = newIndex;
                PlayCurrent();
            }
        }

        private void GotoSubtitleIndex(int index)
        {
            if (mediaPlayer != null && mediaPlayer.VideoPlayer != null && mediaPlayer.Duration > 0)
            {
                mediaPlayer.CurrentPosition = _subtitle.Paragraphs[index].StartTime.TotalSeconds;
            }
        }

        private void PlayPart(Paragraph paragraph)
        {
            if (mediaPlayer != null && mediaPlayer.VideoPlayer != null)
            {
                double startSeconds = paragraph.StartTime.TotalSeconds;
                if (startSeconds > 0.2)
                    startSeconds -= 0.2; // go a little back

                _endSeconds = paragraph.EndTime.TotalSeconds;
                if (mediaPlayer.Duration > _endSeconds + 0.2)
                    _endSeconds += 0.2; // go a little forward

                mediaPlayer.CurrentPosition = startSeconds;
                ShowSubtitle();
                mediaPlayer.Play();
            }
        }

        private void buttonSetStartTime_Click(object sender, EventArgs e)
        {
            SetStartTime(true);
        }

        private void SetStartTime(bool adjustEndTime)
        {
            if (SubtitleListview1.SelectedItems.Count == 1)
            {
                timeUpDownStartTime.MaskedTextBox.TextChanged -= MaskedTextBoxTextChanged;
                int index = SubtitleListview1.SelectedItems[0].Index;
                Paragraph oldParagraph = new Paragraph(_subtitle.Paragraphs[index]);
                double videoPosition = mediaPlayer.CurrentPosition;

                timeUpDownStartTime.TimeCode = new TimeCode(TimeSpan.FromSeconds(videoPosition));

                var duration = _subtitle.Paragraphs[index].Duration.TotalMilliseconds;

                _subtitle.Paragraphs[index].StartTime.TotalMilliseconds = TimeSpan.FromSeconds(videoPosition).TotalMilliseconds;
                if (adjustEndTime)
                    _subtitle.Paragraphs[index].EndTime.TotalMilliseconds = _subtitle.Paragraphs[index].StartTime.TotalMilliseconds + duration;
                SubtitleListview1.SetStartTime(index, _subtitle.Paragraphs[index]);
                SubtitleListview1.SetDuration(index, _subtitle.Paragraphs[index]);
                timeUpDownStartTime.TimeCode = _subtitle.Paragraphs[index].StartTime;
                timeUpDownStartTime.MaskedTextBox.TextChanged += MaskedTextBoxTextChanged;
                UpdateOriginalTimeCodes(oldParagraph);
                if (IsFramesRelevant && CurrentFrameRate > 0)
                {
                    _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                        ShowSource();
                }
            }
        }


        private void buttonSetEndTime_Click(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count == 1)
            {
                int index = SubtitleListview1.SelectedItems[0].Index;
                double videoPosition = mediaPlayer.CurrentPosition;

                _subtitle.Paragraphs[index].EndTime = new TimeCode(TimeSpan.FromSeconds(videoPosition));
                SubtitleListview1.SetStartTime(index, _subtitle.Paragraphs[index]);
                SubtitleListview1.SetDuration(index, _subtitle.Paragraphs[index]);

                if (index + 1 < _subtitle.Paragraphs.Count)
                {
                    SubtitleListview1.SelectedItems[0].Selected = false;
                    SubtitleListview1.Items[index + 1].Selected = true;
                    _subtitle.Paragraphs[index + 1].StartTime = new TimeCode(TimeSpan.FromSeconds(videoPosition));
                    SubtitleListview1.AutoScrollOffset.Offset(0, index * 16);
                    SubtitleListview1.EnsureVisible(Math.Min(SubtitleListview1.Items.Count - 1, index + 5));
                }
                else
                {
                    SetDurationInSeconds(_subtitle.Paragraphs[index].Duration.TotalSeconds);
                }
                if (IsFramesRelevant && CurrentFrameRate > 0)
                {
                    _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                        ShowSource();
                }
            }
        }

        private void ButtonSetEndClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count == 1)
            {
                double videoPosition = mediaPlayer.CurrentPosition;
                int index = SubtitleListview1.SelectedItems[0].Index;
                MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + _subtitle.Paragraphs[index].Number + " " + _subtitle.Paragraphs[index].Text));

                _subtitle.Paragraphs[index].EndTime = new TimeCode(TimeSpan.FromSeconds(videoPosition));
                SubtitleListview1.SetStartTime(index, _subtitle.Paragraphs[index]);
                SubtitleListview1.SetDuration(index, _subtitle.Paragraphs[index]);

                SetDurationInSeconds(_subtitle.Paragraphs[index].Duration.TotalSeconds);
            }
        }

        private void ButtonInsertNewTextClick(object sender, EventArgs e)
        {
            mediaPlayer.Pause();

            var newParagraph = InsertNewTextAtVideoPosition();

            textBoxListViewText.Focus();
            timerAutoDuration.Start();

            ShowStatus(string.Format(_language.VideoControls.NewTextInsertAtX, newParagraph.StartTime.ToShortString()));
        }

        private Paragraph InsertNewTextAtVideoPosition()
        {
            // current movie pos
            double totalMilliseconds = mediaPlayer.CurrentPosition * 1000.0;

            int startNumber = 1;
            if (_subtitle.Paragraphs.Count > 0)
                startNumber = _subtitle.Paragraphs[0].Number;

            var tc = new TimeCode(TimeSpan.FromMilliseconds(totalMilliseconds));
            MakeHistoryForUndo(_language.BeforeInsertSubtitleAtVideoPosition + "  " + tc.ToString());

            // find index where to insert
            int index = 0;
            foreach (Paragraph p in _subtitle.Paragraphs)
            {
                if (p.StartTime.TotalMilliseconds > totalMilliseconds)
                    break;
                index++;
            }

            // create and insert
            var newParagraph = new Paragraph("", totalMilliseconds, totalMilliseconds + 2000);
            if (GetCurrentSubtitleFormat().IsFrameBased)
            {
                newParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                newParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
            }
            _subtitle.Paragraphs.Insert(index, newParagraph);

            // check if original is available - and insert new paragraph in the original too
            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                _subtitleAlternate.InsertParagraphInCorrectTimeOrder(new Paragraph(newParagraph));
                _subtitleAlternate.Renumber(1);
            }

            _subtitleListViewIndex = -1;
            _subtitle.Renumber(startNumber);
            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            SubtitleListview1.SelectIndexAndEnsureVisible(index);
            return newParagraph;
        }

        private void timerAutoDuration_Tick(object sender, EventArgs e)
        {
            labelAutoDuration.Visible = !labelAutoDuration.Visible;

            double duration = Utilities.GetDisplayMillisecondsFromText(textBoxListViewText.Text) * 1.4;
            SetDurationInSeconds(duration / 1000.0);

            // update _subtitle + listview
            if (SubtitleListview1.SelectedItems.Count > 0)
            {
                int firstSelectedIndex = SubtitleListview1.SelectedItems[0].Index;
                Paragraph currentParagraph = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                currentParagraph.EndTime.TotalMilliseconds = currentParagraph.StartTime.TotalMilliseconds + duration;
                SubtitleListview1.SetDuration(firstSelectedIndex, currentParagraph);
            }
        }

        private void buttonBeforeText_Click(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count > 0)
            {
                int index = SubtitleListview1.SelectedItems[0].Index;

                mediaPlayer.Pause();
                double pos = _subtitle.Paragraphs[index].StartTime.TotalSeconds;
                if (pos > 1)
                    mediaPlayer.CurrentPosition = (_subtitle.Paragraphs[index].StartTime.TotalSeconds) - 0.5;
                else
                    mediaPlayer.CurrentPosition = _subtitle.Paragraphs[index].StartTime.TotalSeconds;
                mediaPlayer.Play();
            }
        }

        private void GotoSubPositionAndPause()
        {
            GotoSubPositionAndPause(0);
        }

        private void GotoSubPositionAndPause(double adjustSeconds)
        {
            if (SubtitleListview1.SelectedItems.Count > 0)
            {
                int index = SubtitleListview1.SelectedItems[0].Index;

                mediaPlayer.Pause();
                double newPos = _subtitle.Paragraphs[index].StartTime.TotalSeconds + adjustSeconds;
                if (newPos < 0)
                    newPos = 0;
                mediaPlayer.CurrentPosition = newPos;
                ShowSubtitle();

                double startPos = mediaPlayer.CurrentPosition - 1;
                if (startPos < 0)
                    startPos = 0;

                SetWaveFormPosition(startPos, mediaPlayer.CurrentPosition, index);
            }
        }

        private void buttonGotoSub_Click(object sender, EventArgs e)
        {
            GotoSubPositionAndPause();
        }

        private void buttonOpenVideo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(openFileDialog1.InitialDirectory) && !string.IsNullOrEmpty(_fileName))
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(_fileName);
            openFileDialog1.Title = Configuration.Settings.Language.General.OpenVideoFileTitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetVideoFileFilter();
            openFileDialog1.FileName = string.Empty;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                OpenVideo(openFileDialog1.FileName);
            }
        }

        private void toolStripButtonToggleVideo_Click(object sender, EventArgs e)
        {
            toolStripButtonToggleVideo.Checked = !toolStripButtonToggleVideo.Checked;
            panelVideoPlayer.Visible = toolStripButtonToggleVideo.Checked;
            mediaPlayer.BringToFront();
            if (!toolStripButtonToggleVideo.Checked && !toolStripButtonToggleWaveForm.Checked)
            {
                if (_isVideoControlsUnDocked)
                    ShowHideUnDockedVideoControls();
                else
                    HideVideoPlayer();
            }
            else
            {
                ShowVideoPlayer();
            }
            Configuration.Settings.General.ShowVideoPlayer = toolStripButtonToggleVideo.Checked;
            if (!_loading)
                Refresh();
        }

        private void toolStripButtonToggleWaveForm_Click(object sender, EventArgs e)
        {
            toolStripButtonToggleWaveForm.Checked = !toolStripButtonToggleWaveForm.Checked;
            audioVisualizer.Visible = toolStripButtonToggleWaveForm.Checked;
            trackBarWaveFormPosition.Visible = toolStripButtonToggleWaveForm.Checked;
            panelWaveFormControls.Visible = toolStripButtonToggleWaveForm.Checked;
            if (!toolStripButtonToggleWaveForm.Checked && !toolStripButtonToggleVideo.Checked)
            {
                if (_isVideoControlsUnDocked)
                    ShowHideUnDockedVideoControls();
                else
                    HideVideoPlayer();
            }
            else
            {
                ShowVideoPlayer();
            }
            Configuration.Settings.General.ShowAudioVisualizer = toolStripButtonToggleWaveForm.Checked;
            Refresh();
        }

        public void ShowEarlierOrLater(double adjustMilliseconds, SelectionChoice selection)
        {
            TimeCode tc = new TimeCode(TimeSpan.FromMilliseconds(adjustMilliseconds));
            MakeHistoryForUndo(_language.BeforeShowSelectedLinesEarlierLater  + ": " + tc.ToString());
            if (adjustMilliseconds < 0)
            {
                if (selection == SelectionChoice.AllLines)
                    ShowStatus(string.Format(_language.ShowAllLinesXSecondsLinesEarlier, adjustMilliseconds / -1000.0));
                else if (selection == SelectionChoice.SelectionOnly)
                    ShowStatus(string.Format(_language.ShowSelectedLinesXSecondsLinesEarlier, adjustMilliseconds / -1000.0));
                else if (selection == SelectionChoice.SelectionAndForward)
                    ShowStatus(string.Format(_language.ShowSelectionAndForwardXSecondsLinesEarlier, adjustMilliseconds / -1000.0));
            }
            else
            {
                if (selection == SelectionChoice.AllLines)
                    ShowStatus(string.Format(_language.ShowAllLinesXSecondsLinesLater, adjustMilliseconds / 1000.0));
                else if (selection == SelectionChoice.SelectionOnly)
                    ShowStatus(string.Format(_language.ShowSelectedLinesXSecondsLinesLater, adjustMilliseconds / 1000.0));
                else if (selection == SelectionChoice.SelectionAndForward)
                    ShowStatus(string.Format(_language.ShowSelectionAndForwardXSecondsLinesLater, adjustMilliseconds / 1000.0));
            }

            double frameRate = CurrentFrameRate;
            SubtitleListview1.BeginUpdate();

            int startFrom = 0;
            if (selection == SelectionChoice.SelectionAndForward)
            {
                if (SubtitleListview1.SelectedItems.Count > 0)
                    startFrom = SubtitleListview1.SelectedItems[0].Index;
                else
                    startFrom = _subtitle.Paragraphs.Count;
            }

            for (int i = startFrom; i < _subtitle.Paragraphs.Count; i++)
            {
                switch (selection)
                {
                    case SelectionChoice.SelectionOnly:
                        if (SubtitleListview1.Items[i].Selected)
                            ShowEarlierOrLaterParagraph(adjustMilliseconds, i);
                        break;
                    case SelectionChoice.AllLines:
                        ShowEarlierOrLaterParagraph(adjustMilliseconds, i);
                        break;
                    case SelectionChoice.SelectionAndForward:
                        ShowEarlierOrLaterParagraph(adjustMilliseconds, i);
                        break;
                }
            }

            SubtitleListview1.EndUpdate();
            if (_subtitle.WasLoadedWithFrameNumbers)
                _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(frameRate);
            RefreshSelectedParagraph();
            UpdateSourceView();
            UpdateListSyntaxColoring();
        }

        private void ShowEarlierOrLaterParagraph(double adjustMilliseconds, int i)
        {
            Paragraph p = _subtitle.GetParagraphOrDefault(i);
            if (p != null)
            {
                if (_subtitleAlternate != null)
                {
                    Paragraph original = Utilities.GetOriginalParagraph(i, p, _subtitleAlternate.Paragraphs);
                    if (original != null)
                    {
                        original.StartTime.TotalMilliseconds += adjustMilliseconds;
                        original.EndTime.TotalMilliseconds += adjustMilliseconds;
                    }
                }

                p.StartTime.TotalMilliseconds += adjustMilliseconds;
                p.EndTime.TotalMilliseconds += adjustMilliseconds;
                SubtitleListview1.SetStartTime(i, p);
            }
        }

        private void UpdateSourceView()
        {
            if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                ShowSource();
        }

        private void toolStripMenuItemAdjustAllTimes_Click(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count > 1)
            {
                ShowSelectedLinesEarlierlaterToolStripMenuItemClick(null, null);
            }
            else
            {
                if (IsSubtitleLoaded)
                {
                    mediaPlayer.Pause();

                    if (_showEarlierOrLater != null && !_showEarlierOrLater.IsDisposed)
                    {
                        _showEarlierOrLater.WindowState = FormWindowState.Normal;
                        _showEarlierOrLater.Focus();
                        return;
                    }

                    _showEarlierOrLater = new ShowEarlierLater();
                    if (!_formPositionsAndSizes.SetPositionAndSize(_showEarlierOrLater))
                    {
                        _showEarlierOrLater.Top = this.Top + 100;
                        _showEarlierOrLater.Left = this.Left + (this.Width /2)  - (_showEarlierOrLater.Width / 3);
                    }
                    SaveSubtitleListviewIndexes();
                    _showEarlierOrLater.Initialize(ShowEarlierOrLater, _formPositionsAndSizes, false);
                    _showEarlierOrLater.Show(this);
                }
                else
                {
                    MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer != null && mediaPlayer.VideoPlayer != null)
            {
                if (!mediaPlayer.IsPaused)
                {
                    timeUpDownVideoPosition.Enabled = false;
                    timeUpDownVideoPositionAdjust.Enabled = false;

                    if (_endSeconds >= 0 && mediaPlayer.CurrentPosition > _endSeconds && !AutoRepeatContinueOn)
                    {
                        mediaPlayer.Pause();
                        mediaPlayer.CurrentPosition = _endSeconds + EndDelay;
                        _endSeconds = -1;
                    }

                    if (AutoRepeatContinueOn)
                    {
                        if (_endSeconds >= 0 && mediaPlayer.CurrentPosition > _endSeconds && checkBoxAutoRepeatOn.Checked)
                        {
                            mediaPlayer.Pause();
                            _endSeconds = -1;

                            if (checkBoxAutoRepeatOn.Checked && _repeatCount > 0)
                            {
                                if (_repeatCount == 1)
                                    labelStatus.Text = _language.VideoControls.RepeatingLastTime;
                                else
                                    labelStatus.Text = string.Format(Configuration.Settings.Language.Main.VideoControls.RepeatingXTimesLeft, _repeatCount);

                                _repeatCount--;
                                if (_subtitleListViewIndex >= 0 && _subtitleListViewIndex < _subtitle.Paragraphs.Count)
                                    PlayPart(_subtitle.Paragraphs[_subtitleListViewIndex]);
                            }
                            else if (checkBoxAutoContinue.Checked)
                            {
                                _autoContinueDelayCount = int.Parse(comboBoxAutoContinue.Text);
                                if (_repeatCount == 1)
                                    labelStatus.Text = _language.VideoControls.AutoContinueInOneSecond;
                                else
                                    labelStatus.Text = string.Format(Configuration.Settings.Language.Main.VideoControls.AutoContinueInXSeconds, _autoContinueDelayCount);
                                timerAutoContinue.Start();
                            }
                        }
                    }
                }
                else
                {
                    timeUpDownVideoPosition.Enabled = true;
                    timeUpDownVideoPositionAdjust.Enabled = true;
                }
                timeUpDownVideoPosition.TimeCode = new TimeCode(TimeSpan.FromMilliseconds(mediaPlayer.CurrentPosition * 1000.0));
                timeUpDownVideoPositionAdjust.TimeCode = new TimeCode(TimeSpan.FromMilliseconds(mediaPlayer.CurrentPosition * 1000.0));
                mediaPlayer.RefreshProgressBar();
                int index = ShowSubtitle();
                if (index != -1 && checkBoxSyncListViewWithVideoWhilePlaying.Checked)
                {
                    if ((DateTime.Now.Ticks - _lastTextKeyDownTicks) > 10000 * 700) // only if last typed char was entered > 700 milliseconds
                    {
                        if (_endSeconds <= 0 || !checkBoxAutoRepeatOn.Checked)
                        {
                            if (!timerAutoDuration.Enabled)
                            {
                                SubtitleListview1.BeginUpdate();
                                SubtitleListview1.SelectIndexAndEnsureVisible(index, true);
                                SubtitleListview1.EndUpdate();
                            }
                        }
                    }
                }

                trackBarWaveFormPosition.ValueChanged -= trackBarWaveFormPosition_ValueChanged;
                int value = (int)mediaPlayer.CurrentPosition;
                if (value > trackBarWaveFormPosition.Maximum)
                    value = trackBarWaveFormPosition.Maximum;
                if (value < trackBarWaveFormPosition.Minimum)
                    value = trackBarWaveFormPosition.Minimum;
                trackBarWaveFormPosition.Value = value;
                trackBarWaveFormPosition.ValueChanged += trackBarWaveFormPosition_ValueChanged;
            }
        }

        private void StopAutoDuration()
        {
            timerAutoDuration.Stop();
            labelAutoDuration.Visible = false;
        }

        private void textBoxListViewText_Leave(object sender, EventArgs e)
        {
            StopAutoDuration();
        }

        private void timerAutoContinue_Tick(object sender, EventArgs e)
        {
            _autoContinueDelayCount--;

            if (_autoContinueDelayCount == 0)
            {
                timerAutoContinue.Stop();

                if (timerStillTyping.Enabled)
                {
                    labelStatus.Text = _language.VideoControls.StillTypingAutoContinueStopped;
                }
                else
                {
                    labelStatus.Text = string.Empty;
                    Next();
                }
            }
            else
            {
                if (_repeatCount == 1)
                    labelStatus.Text = _language.VideoControls.AutoContinueInOneSecond;
                else
                    labelStatus.Text = string.Format(Configuration.Settings.Language.Main.VideoControls.AutoContinueInXSeconds, _autoContinueDelayCount);
            }
        }

        private void timerStillTyping_Tick(object sender, EventArgs e)
        {
            timerStillTyping.Stop();
        }

        private void textBoxListViewText_MouseMove(object sender, MouseEventArgs e)
        {
            if (AutoRepeatContinueOn && !textBoxSearchWord.Focused)
            {
                string selectedText = textBoxListViewText.SelectedText;
                if (!string.IsNullOrEmpty(selectedText))
                {
                    selectedText = selectedText.Trim();
                    selectedText = selectedText.TrimEnd('.');
                    selectedText = selectedText.TrimEnd(',');
                    selectedText = selectedText.TrimEnd('!');
                    selectedText = selectedText.TrimEnd('?');
                    selectedText = selectedText.Trim();
                    if (!string.IsNullOrEmpty(selectedText) && selectedText != textBoxSearchWord.Text)
                    {
                        textBoxSearchWord.Text = Utilities.RemoveHtmlTags(selectedText);
                    }
                }
            }
        }

        private void textBoxListViewText_KeyUp(object sender, KeyEventArgs e)
        {
            textBoxListViewText_MouseMove(sender, null);
            textBoxListViewText.ClearUndo();
            UpdatePositionAndTotalLength(labelTextLineTotal, textBoxListViewText);
        }

        private void buttonGoogleIt_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.google.com/search?q=" + Utilities.UrlEncode(textBoxSearchWord.Text));
        }

        private void buttonGoogleTranslateIt_Click(object sender, EventArgs e)
        {
            string languageId = Utilities.AutoDetectGoogleLanguage(_subtitle);
            System.Diagnostics.Process.Start("http://translate.google.com/#auto|" + languageId + "|" + Utilities.UrlEncode(textBoxSearchWord.Text));
        }

        private void ButtonPlayCurrentClick(object sender, EventArgs e)
        {
            PlayCurrent();
        }

        private void buttonPlayNext_Click(object sender, EventArgs e)
        {
            Next();
        }

        private void buttonPlayPrevious_Click(object sender, EventArgs e)
        {
            PlayPrevious();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            _endSeconds = -1;
            timerAutoContinue.Stop();
            mediaPlayer.Pause();
            labelStatus.Text = string.Empty;
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItemOpenContainingFolder.Visible = !string.IsNullOrEmpty(_fileName) && File.Exists(_fileName);
            bool subtitleLoaded = IsSubtitleLoaded;
            toolStripMenuItemStatistics.Visible = subtitleLoaded;
            toolStripSeparator22.Visible = subtitleLoaded;
            toolStripMenuItemExport.Visible = subtitleLoaded;
            openOriginalToolStripMenuItem.Visible = subtitleLoaded;
            if (subtitleLoaded && Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                saveOriginalToolStripMenuItem.Visible = true;
                saveOriginalAstoolStripMenuItem.Visible = true;
                removeOriginalToolStripMenuItem.Visible = true;
            }
            else
            {
                saveOriginalToolStripMenuItem.Visible = false;
                saveOriginalAstoolStripMenuItem.Visible = false;
                if (subtitleLoaded && SubtitleListview1.IsAlternateTextColumnVisible && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                    removeOriginalToolStripMenuItem.Visible = true;
                else
                    removeOriginalToolStripMenuItem.Visible = false;
            }
            var format = GetCurrentSubtitleFormat();
            if (format.GetType() == typeof(AdvancedSubStationAlpha))
            {
                toolStripMenuItemSubStationAlpha.Visible = true;
                toolStripMenuItemSubStationAlpha.Text = Configuration.Settings.Language.Main.Menu.File.AdvancedSubStationAlphaProperties;
            }
            else if (format.GetType() == typeof(SubStationAlpha))
            {
                toolStripMenuItemSubStationAlpha.Visible = true;
                toolStripMenuItemSubStationAlpha.Text = Configuration.Settings.Language.Main.Menu.File.SubStationAlphaProperties;
            }
            else
            {
                toolStripMenuItemSubStationAlpha.Visible = false;
            }

            if (format.GetType() == typeof(DCSubtitle) || format.GetType() == typeof(DCinemaSmpte))
            {
                toolStripMenuItemDCinemaProperties.Visible = true;
            }
            else
            {
                toolStripMenuItemDCinemaProperties.Visible = false;
            }

            if (format.GetType() == typeof(TimedText10))
            {
                toolStripMenuItemTTProperties.Visible = true;
            }
            else
            {
                toolStripMenuItemTTProperties.Visible = false;
            }

            toolStripSeparator20.Visible = subtitleLoaded;
        }

        private void toolStripMenuItemOpenContainingFolder_Click(object sender, EventArgs e)
        {
            string folderName = Path.GetDirectoryName(_fileName);
            if (Utilities.IsRunningOnMono())
            {
                System.Diagnostics.Process.Start(folderName);
            }
            else
            {
                string argument = @"/select, " + _fileName;
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlButtons.SelectedIndex == 0)
            {
                tabControlButtons.Width = groupBoxTranslateSearch.Left + groupBoxTranslateSearch.Width + 10;
                Configuration.Settings.VideoControls.LastActiveTab = "Translate";

            }
            else if (tabControlButtons.SelectedIndex == 1)
            {
                tabControlButtons.Width = buttonInsertNewText.Left + buttonInsertNewText.Width + 35;
                Configuration.Settings.VideoControls.LastActiveTab = "Create";
            }
            else if (tabControlButtons.SelectedIndex == 2)
            {
                tabControlButtons.Width = buttonInsertNewText.Left + buttonInsertNewText.Width + 35;
                Configuration.Settings.VideoControls.LastActiveTab = "Adjust";
            }

            if (!_isVideoControlsUnDocked)
            {
                if (toolStripButtonToggleWaveForm.Checked)
                    audioVisualizer.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                if (!toolStripButtonToggleWaveForm.Checked && toolStripButtonToggleVideo.Checked)
                {
                    panelVideoPlayer.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                    panelVideoPlayer.Width = groupBoxVideo.Width - (panelVideoPlayer.Left + 10);
                }

                audioVisualizer.Width = groupBoxVideo.Width - (audioVisualizer.Left + 10);
                panelWaveFormControls.Left = audioVisualizer.Left;
                trackBarWaveFormPosition.Left = panelWaveFormControls.Left + panelWaveFormControls.Width + 5;
                trackBarWaveFormPosition.Width = groupBoxVideo.Width - (trackBarWaveFormPosition.Left + 10);
                Main_Resize(null, null);
                checkBoxSyncListViewWithVideoWhilePlaying.Left = tabControlButtons.Left + tabControlButtons.Width + 5;
                if (!_loading)
                    Refresh();
            }
            else if (_videoControlsUnDocked != null && !_videoControlsUnDocked.IsDisposed)
            {
                _videoControlsUnDocked.Width = tabControlButtons.Width + 20;
                _videoControlsUnDocked.Height = tabControlButtons.Height + 65;
            }
        }

        private void buttonSecBack1_Click(object sender, EventArgs e)
        {
            GoBackSeconds((double)numericUpDownSec1.Value);
        }

        private void buttonForward1_Click(object sender, EventArgs e)
        {
            GoBackSeconds(-(double)numericUpDownSec1.Value);
        }

        private void ButtonSetStartAndOffsetRestClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count == 1)
            {
                bool oldSync = checkBoxSyncListViewWithVideoWhilePlaying.Checked;
                checkBoxSyncListViewWithVideoWhilePlaying.Checked = false;

                timeUpDownStartTime.MaskedTextBox.TextChanged -= MaskedTextBoxTextChanged;
                int index = SubtitleListview1.SelectedItems[0].Index;
                double videoPosition = mediaPlayer.CurrentPosition;
                var tc = new TimeCode(TimeSpan.FromSeconds(videoPosition));
                timeUpDownStartTime.TimeCode = tc;

                MakeHistoryForUndo(_language.BeforeSetStartTimeAndOffsetTheRest + "  " +_subtitle.Paragraphs[index].Number.ToString() + " - " + tc.ToString());

                double offset = _subtitle.Paragraphs[index].StartTime.TotalMilliseconds - tc.TotalMilliseconds;
                for (int i = index; i < SubtitleListview1.Items.Count; i++)
                {
                    _subtitle.Paragraphs[i].StartTime = new TimeCode(TimeSpan.FromMilliseconds(_subtitle.Paragraphs[i].StartTime.TotalMilliseconds - offset));
                    _subtitle.Paragraphs[i].EndTime = new TimeCode(TimeSpan.FromMilliseconds(_subtitle.Paragraphs[i].EndTime.TotalMilliseconds - offset));
                    SubtitleListview1.SetStartTime(i, _subtitle.Paragraphs[i]);
                }

                if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                {
                    Paragraph original = Utilities.GetOriginalParagraph(index, _subtitle.Paragraphs[index], _subtitleAlternate.Paragraphs);
                    if (original != null)
                    {
                        index = _subtitleAlternate.GetIndex(original);
                        for (int i = index; i < _subtitleAlternate.Paragraphs.Count; i++)
                        {
                            _subtitleAlternate.Paragraphs[i].StartTime = new TimeCode(TimeSpan.FromMilliseconds(_subtitleAlternate.Paragraphs[i].StartTime.TotalMilliseconds - offset));
                            _subtitleAlternate.Paragraphs[i].EndTime = new TimeCode(TimeSpan.FromMilliseconds(_subtitleAlternate.Paragraphs[i].EndTime.TotalMilliseconds - offset));
                        }
                    }
                }
                if (IsFramesRelevant && CurrentFrameRate > 0)
                {
                    _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                        ShowSource();
                }

                checkBoxSyncListViewWithVideoWhilePlaying.Checked = oldSync;
                timeUpDownStartTime.MaskedTextBox.TextChanged += MaskedTextBoxTextChanged;
            }
        }

        private void ButtonSetEndAndGoToNextClick(object sender, EventArgs e)
        {
            if (SubtitleListview1.SelectedItems.Count == 1)
            {
                int index = SubtitleListview1.SelectedItems[0].Index;
                double videoPosition = mediaPlayer.CurrentPosition;

                string oldDuration = _subtitle.Paragraphs[index].Duration.ToString();
                var temp = new Paragraph(_subtitle.Paragraphs[index]);
                temp.EndTime.TotalMilliseconds = new TimeCode(TimeSpan.FromSeconds(videoPosition)).TotalMilliseconds;
                MakeHistoryForUndo(string.Format(_language.DisplayTimeAdjustedX, "#" + _subtitle.Paragraphs[index].Number + ": " + oldDuration + " -> " + temp.Duration.ToString()));
                _makeHistoryPaused = true;

                _subtitle.Paragraphs[index].EndTime = new TimeCode(TimeSpan.FromSeconds(videoPosition));
                SubtitleListview1.SetDuration(index, _subtitle.Paragraphs[index]);
                SetDurationInSeconds(_subtitle.Paragraphs[index].Duration.TotalSeconds);

                if (index + 1 < _subtitle.Paragraphs.Count)
                {
                    SubtitleListview1.Items[index].Selected = false;
                    SubtitleListview1.Items[index + 1].Selected = true;
                    _subtitle.Paragraphs[index + 1].StartTime = new TimeCode(TimeSpan.FromSeconds(videoPosition+0.001));
                    if (IsFramesRelevant && CurrentFrameRate > 0)
                    {
                        _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                        if (tabControlSubtitle.SelectedIndex == TabControlSourceView)
                            ShowSource();
                    }
                    SubtitleListview1.SetStartTime(index + 1, _subtitle.Paragraphs[index + 1]);
                    SubtitleListview1.SelectIndexAndEnsureVisible(index + 1, true);
                }
                _makeHistoryPaused = false;
            }
        }

        private void ButtonAdjustSecBackClick(object sender, EventArgs e)
        {
            GoBackSeconds((double)numericUpDownSecAdjust1.Value);
        }

        private void ButtonAdjustSecForwardClick(object sender, EventArgs e)
        {
            GoBackSeconds(-(double)numericUpDownSecAdjust1.Value);
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            toolStripButtonToggleVideo.Checked = !Configuration.Settings.General.ShowVideoPlayer;
            toolStripButtonToggleVideo_Click(null, null);

            _timerAutoSave.Tick += TimerAutoSaveTick;
            if (Configuration.Settings.General.AutoBackupSeconds > 0)
            {
                _timerAutoSave.Interval = 1000 * Configuration.Settings.General.AutoBackupSeconds; // take backup every x second if changes were made
                _timerAutoSave.Start();
            }
            ToolStripMenuItemPlayRateNormalClick(null, null);

            SetPositionFromXYString(Configuration.Settings.General.UndockedVideoPosition, "VideoPlayerUnDocked");
            SetPositionFromXYString(Configuration.Settings.General.UndockedWaveformPosition, "WaveFormUnDocked");
            SetPositionFromXYString(Configuration.Settings.General.UndockedVideoControlsPosition, "VideoControlsUndocked");
            if (Configuration.Settings.General.Undocked && Configuration.Settings.General.StartRememberPositionAndSize)
            {
                Configuration.Settings.General.Undocked = false;
                UndockVideoControlsToolStripMenuItemClick(null, null);
            }
            Main_Resize(null, null);

            toolStripButtonLockCenter.Checked = Configuration.Settings.General.WaveFormCenter;
            audioVisualizer.Locked = toolStripButtonLockCenter.Checked;

            numericUpDownSec1.Value = (decimal) (Configuration.Settings.General.SmallDelayMilliseconds / 1000.0);
            numericUpDownSec2.Value = (decimal) (Configuration.Settings.General.LargeDelayMilliseconds / 1000.0);

            numericUpDownSecAdjust1.Value = (decimal)(Configuration.Settings.General.SmallDelayMilliseconds / 1000.0);
            numericUpDownSecAdjust2.Value = (decimal)(Configuration.Settings.General.LargeDelayMilliseconds / 1000.0);

            SetShortcuts();
            LoadPlugins();

            if (Configuration.Settings.General.StartInSourceView)
            {
                textBoxSource.Focus();
            }
            else
            {
                SubtitleListview1.Focus();
                int index = FirstSelectedIndex;
                if (index > 0 && SubtitleListview1.Items.Count > index)
                {
                    SubtitleListview1.Focus();
                    SubtitleListview1.Items[index].Focused = true;
                }
            }
            MainResize();
            _loading = false;
            OpenVideo(_videoFileName);
            timerTextUndo.Start();
            timerAlternateTextUndo.Start();
            if (Utilities.IsRunningOnLinux())
            {
                numericUpDownDuration.Left = timeUpDownStartTime.Left + timeUpDownStartTime.Width + 10;
                numericUpDownDuration.Width = numericUpDownDuration.Width + 10;
                numericUpDownSec1.Width = numericUpDownSec1.Width + 10;
                numericUpDownSec2.Width = numericUpDownSec2.Width + 10;
                numericUpDownSecAdjust1.Width = numericUpDownSecAdjust1.Width + 10;
                numericUpDownSecAdjust2.Width = numericUpDownSecAdjust2.Width + 10;
                labelDuration.Left = numericUpDownDuration.Left;
            }

            _timerDoSyntaxColoring.Interval = 100;
            _timerDoSyntaxColoring.Tick +=_timerDoSyntaxColoring_Tick;

            if (Configuration.Settings.General.ShowBetaStuff)
            {
                generateDatetimeInfoFromVideoToolStripMenuItem.Visible = true;
//                toolStripMenuItemApplyDurationLimits.Visible = true;
//                toolStripMenuItemAlignment.Visible = true;
//                toolStripMenuItemRightToLeftMode.Visible = true;
                //toolStripMenuItemReverseRightToLeftStartEnd.Visible = true;
                //joinSubtitlesToolStripMenuItem.Visible = true;
                //plainTextWithoutLineBreaksToolStripMenuItem.Visible = false;

                toolStripMenuItemExportCaptionInc.Visible = true;
                toolStripMenuItemExportUltech130.Visible = true;
            }
            else
            {
                generateDatetimeInfoFromVideoToolStripMenuItem.Visible = false;
//                toolStripMenuItemApplyDurationLimits.Visible = false;
//                toolStripMenuItemAlignment.Visible = false;
                //                toolStripMenuItemRightToLeftMode.Visible = false;
//                toolStripMenuItemReverseRightToLeftStartEnd.Visible = !string.IsNullOrEmpty(_language.Menu.Edit.ReverseRightToLeftStartEnd);
                //joinSubtitlesToolStripMenuItem.Visible = false;

                toolStripMenuItemExportCaptionInc.Visible = false;
                toolStripMenuItemExportUltech130.Visible = false;
            }
        }

        void  _timerDoSyntaxColoring_Tick(object sender, EventArgs e)
        {
            UpdateListSyntaxColoring();
            _timerDoSyntaxColoring.Stop();
        }

        private void SetPositionFromXYString(string positionAndSize, string name)
        {
            string[] parts = positionAndSize.Split(';');
            if (parts.Length == 4)
            {
                try
                {
                    int x = int.Parse(parts[0]);
                    int y = int.Parse(parts[1]);
                    int w = int.Parse(parts[2]);
                    int h = int.Parse(parts[3]);
                    _formPositionsAndSizes.AddPositionAndSize(new PositionAndSize() { Left = x, Top = y, Size = new Size(w, h), Name = name });
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message);
                }
            }
        }

        private void SetShortcuts()
        {
            newToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainFileNew);
            openToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainFileOpen);
            saveToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainFileSave);
            saveAsToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainFileSaveAs);
            eBUSTLToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainFileExportEbu);

            findToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditFind);
            findNextToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditFindNext);
            replaceToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditReplace);
            multipleReplaceToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditMultipleReplace);
            gotoLineNumberToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditGoToLineNumber);
            toolStripMenuItemRightToLeftMode.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditRightToLeft);

            fixToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainToolsFixCommonErrors);
            removeTextForHearImparedToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainToolsRemoveTextForHI);

            showhideVideoToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoShowHideVideo);
            _toggleVideoDockUndock = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoToggleVideoControls);
            _videoPause = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoPause);
            _videoPlayPauseToggle = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoPlayPauseToggle);
            _video100MsLeft = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideo100MsLeft);
            _video100MsRight = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideo100MsRight);
            _video500MsLeft  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideo500MsLeft);
            _video500MsRight = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideo500MsRight);
            _mainVideoFullscreen = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoFullscreen);

            toolStripMenuItemAdjustAllTimes.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainSynchronizationAdjustTimes);
            italicToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainListViewItalic);
            _mainListViewToggleDashes = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainListViewToggleDashes);
            toolStripMenuItemAlignment.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainListViewAlignment);
            _mainEditReverseStartAndEndingForRTL = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainEditReverseStartAndEndingForRTL);
            _mainListViewCopyText = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainListViewCopyText);
            toolStripMenuItemReverseRightToLeftStartEnd.ShortcutKeys = _mainEditReverseStartAndEndingForRTL;
            italicToolStripMenuItem1.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTextBoxItalic);
            _mainTextBoxSplitAtCursor = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTextBoxSplitAtCursor);
            _mainCreateInsertSubAtVideoPos = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainCreateInsertSubAtVideoPos);
            _mainCreatePlayFromJustBefore = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainCreatePlayFromJustBefore);
            _mainCreateSetStart = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainCreateSetStart);
            _mainCreateSetEnd = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainCreateSetEnd);
            _mainCreateStartDownEndUp = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainCreateStartDownEndUp);
            _mainAdjustSetStartAndOffsetTheRest = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetStartAndOffsetTheRest);
            _mainAdjustSetEndAndGotoNext = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetEndAndGotoNext);
            _mainAdjustInsertViaEndAutoStartAndGoToNext = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustViaEndAutoStartAndGoToNext);
            _mainAdjustSetStartAutoDurationAndGoToNext = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetStartAutoDurationAndGoToNext);
            _mainAdjustSetEndNextStartAndGoToNext = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetEndNextStartAndGoToNext);
            _mainAdjustStartDownEndUpAndGoToNext = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustStartDownEndUpAndGoToNext);
            _mainAdjustSetStart = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetStart);
            _mainAdjustSetStartOnly = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetStartOnly);
            _mainAdjustSetEnd = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSetEnd);
            _mainAdjustSelected100MsForward = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSelected100MsForward);
            _mainAdjustSelected100MsBack = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainAdjustSelected100MsBack);
            _mainInsertAfter = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainInsertAfter);
            _mainInsertBefore = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainInsertBefore);
            _mainMergeDialogue = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainMergeDialogue);
            _mainGoToNext = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainGoToNext);
            _mainGoToPrevious = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainGoToPrevious);
            _mainToggleFocus = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainToogleFocus);
            _waveformVerticalZoom = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformVerticalZoom);
            _waveformZoomIn = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformZoomIn);
            _waveformZoomOut = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformZoomOut);
            _waveformPlaySelection = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformPlaySelection);
            _waveformSearchSilenceForward = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformSearchSilenceForward);
            _waveformSearchSilenceBack = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformSearchSilenceBack);
            _waveformAddTextAtHere = Utilities.GetKeys(Configuration.Settings.Shortcuts.WaveformAddTextHere);
            _mainTranslateCustomSearch1  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTranslateCustomSearch1);
            _mainTranslateCustomSearch2  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTranslateCustomSearch2);
            _mainTranslateCustomSearch3  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTranslateCustomSearch3);
            _mainTranslateCustomSearch4  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTranslateCustomSearch4);
            _mainTranslateCustomSearch5  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTranslateCustomSearch5);
            _mainTranslateCustomSearch6  = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainTranslateCustomSearch6);
        }

        private void LoadPlugins()
        {
            string path = Path.Combine(Configuration.BaseDirectory, "Plugins");
            if (!Directory.Exists(path))
                return;
            string[] pluginFiles = Directory.GetFiles(path, "*.DLL");

            int filePluginCount = 0;
            int toolsPluginCount = 0;
            int syncPluginCount = 0;
            foreach (string pluginFileName in pluginFiles)
            {
                Type pluginType = null;
                System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFile(pluginFileName);
                string objectName = Path.GetFileNameWithoutExtension(pluginFileName);
                if (assembly != null)
                {
                    try
                    {
                        pluginType = assembly.GetType("SubtitleEdit." + objectName);
                        object pluginObject = Activator.CreateInstance(pluginType);
                        System.Reflection.PropertyInfo pi = pluginType.GetProperty("Name");
                        string name = (string)pi.GetValue(pluginObject, null);
                        pi = pluginType.GetProperty("Version");
                        string version = (string)pi.GetValue(pluginObject, null);
                        pi = pluginType.GetProperty("ActionType");
                        string actionType = (string)pi.GetValue(pluginObject, null);
                        System.Reflection.MethodInfo mi = pluginType.GetMethod("DoAction");

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(actionType) && mi != null)
                        {
                            ToolStripMenuItem item = new ToolStripMenuItem();
                            item.Name = "Plugin" + toolsPluginCount.ToString();
                            item.Text = name;
                            item.Tag = pluginFileName;

                            pi = pluginType.GetProperty("ShortCut");
                            if (pi != null)
                                item.ShortcutKeys = Utilities.GetKeys((string)pi.GetValue(pluginObject, null));

                            if (string.Compare(actionType, "File", true) == 0)
                            {
                                if (filePluginCount == 0)
                                    fileToolStripMenuItem.DropDownItems.Insert(fileToolStripMenuItem.DropDownItems.Count - 2, new ToolStripSeparator());
                                item.Click += PluginToolClick;
                                fileToolStripMenuItem.DropDownItems.Insert(fileToolStripMenuItem.DropDownItems.Count - 2, item);
                                filePluginCount++;
                            }
                            else if (string.Compare(actionType, "Tool", true) == 0)
                            {
                                if (toolsPluginCount == 0)
                                    toolsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                                item.Click += PluginToolClick;
                                toolsToolStripMenuItem.DropDownItems.Add(item);
                                toolsPluginCount++;
                            }
                            else if (string.Compare(actionType, "Sync", true) == 0)
                            {
                                if (syncPluginCount == 0)
                                    toolStripMenuItemSyncronization.DropDownItems.Add(new ToolStripSeparator());
                                item.Click += PluginToolClick;
                                toolStripMenuItemSyncronization.DropDownItems.Add(item);
                                syncPluginCount++;
                            }
                        }

                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Error loading plugin:" + pluginFileName + ": " + exception.Message);
                    }
                    finally
                    {
                        assembly = null;
                    }
                }
            }
        }

        void PluginToolClick(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem item = (ToolStripItem) sender;
                Type pluginType = null;
                System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFile(item.Tag.ToString());
                if (assembly != null)
                {
                    string objectName = Path.GetFileNameWithoutExtension(item.Tag.ToString());
                    pluginType = assembly.GetType("SubtitleEdit." + objectName);
                    object pluginObject = Activator.CreateInstance(pluginType);
                    System.Reflection.MethodInfo mi = pluginType.GetMethod("DoAction");

                    System.Reflection.PropertyInfo pi = pluginType.GetProperty("Name");
                    string name = (string)pi.GetValue(pluginObject, null);
                    pi = pluginType.GetProperty("Version");
                    string version = (string)pi.GetValue(pluginObject, null);


                    Subtitle temp = new Subtitle(_subtitle);
                    string text = temp.ToText(new SubRip());
                    string pluginResult = (string)mi.Invoke(pluginObject, new object[] { this, text, 25.0, _fileName, "", "" });

                    if (!string.IsNullOrEmpty(pluginResult) && pluginResult.Length > 10 && text != pluginResult)
                    {
                        _subtitle.MakeHistoryForUndo(string.Format("Before running plugin: {0} {1}", name, version), GetCurrentSubtitleFormat(), _fileDateTime, _subtitleAlternate, _subtitleAlternateFileName, _subtitleListViewIndex, textBoxListViewText.SelectionStart, textBoxListViewTextAlternate.SelectionStart);
                        string[] lineArray = pluginResult.Split(Environment.NewLine.ToCharArray());
                        List<string> lines = new List<string>();
                        foreach (string line in lineArray)
                            lines.Add(line);
                        new SubRip().LoadSubtitle(_subtitle, lines, _fileName);
                        SaveSubtitleListviewIndexes();
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                        RestoreSubtitleListviewIndexes();
                        ShowSource();
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        void TimerAutoSaveTick(object sender, EventArgs e)
        {
            string currentText = _subtitle.ToText(GetCurrentSubtitleFormat());
            if (_textAutoSave != null && _subtitle.Paragraphs.Count > 0)
            {
                if (currentText != _textAutoSave && currentText.Trim().Length > 0)
                {
                    if (!Directory.Exists(Configuration.AutoBackupFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(Configuration.AutoBackupFolder);
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show("Unable to create backup directory " + Configuration.AutoBackupFolder + ": " + exception.Message);
                        }
                    }
                    string title = string.Empty;
                    if (!string.IsNullOrEmpty(_fileName))
                        title = "_" + Path.GetFileNameWithoutExtension(_fileName);
                    string fileName = string.Format("{0}{1:0000}-{2:00}-{3:00}_{4:00}-{5:00}-{6:00}{7}{8}", Configuration.AutoBackupFolder, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, title, GetCurrentSubtitleFormat().Extension);
                    File.WriteAllText(fileName, currentText);
                }
            }
            _textAutoSave = currentText;
        }

        private void mediaPlayer_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                string fileName = files[0];
                string ext = Path.GetExtension(fileName).ToLower();
                if (Utilities.GetVideoFileFilter().Contains(ext))
                {
                    OpenVideo(fileName);
                }
                else
                {
                    MessageBox.Show(string.Format(_language.DropFileXNotAccepted, fileName));
                }
            }
            else
            {
                MessageBox.Show(_language.DropOnlyOneFile);
            }
        }

        private void mediaPlayer_DragEnter(object sender, DragEventArgs e)
        {
            // make sure they're actually dropping files (not text or anything else)
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        private void buttonSecBack2_Click(object sender, EventArgs e)
        {
            GoBackSeconds((double)numericUpDownSec2.Value);
        }

        private void buttonForward2_Click(object sender, EventArgs e)
        {
            GoBackSeconds(-(double)numericUpDownSec2.Value);
        }

        private void buttonAdjustSecBack2_Click(object sender, EventArgs e)
        {
            GoBackSeconds((double)numericUpDownSecAdjust2.Value);
        }

        private void buttonAdjustSecForward2_Click(object sender, EventArgs e)
        {
            GoBackSeconds(-(double)numericUpDownSecAdjust2.Value);
        }

        private void translatepoweredByMicrosoftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TranslateViaGoogle(false, false);
        }

        public static string Sha256Hash(string value)
        {
            System.Security.Cryptography.SHA256Managed hasher = new System.Security.Cryptography.SHA256Managed();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] hash = hasher.ComputeHash(bytes);
            return Convert.ToBase64String(hash, 0, hash.Length);
        }

        private string GetPeakWaveFileName(string videoFileName)
        {
            string dir = Configuration.WaveFormsFolder.TrimEnd(Path.DirectorySeparatorChar);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FileInfo fi = new FileInfo(videoFileName);
            string wavePeakName = Sha256Hash(Path.GetFileName(videoFileName) + fi.Length.ToString() + fi.CreationTimeUtc.ToShortDateString()) + ".wav";
            wavePeakName = wavePeakName.Replace("=", string.Empty).Replace("/", string.Empty).Replace(",", string.Empty).Replace("?", string.Empty).Replace("*", string.Empty).Replace("+", string.Empty).Replace("\\", string.Empty);
            wavePeakName = Path.Combine(dir, wavePeakName);
            return wavePeakName;
        }

        private string GetSpectrogramFolder(string videoFileName)
        {
            string dir = Configuration.SpectrogramsFolder.TrimEnd(Path.DirectorySeparatorChar);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FileInfo fi = new FileInfo(videoFileName);
            string name = Sha256Hash(Path.GetFileName(videoFileName) + fi.Length.ToString() + fi.CreationTimeUtc.ToShortDateString());
            name = name.Replace("=", string.Empty).Replace("/", string.Empty).Replace(",", string.Empty).Replace("?", string.Empty).Replace("*", string.Empty).Replace("+", string.Empty).Replace("\\", string.Empty);
            name = Path.Combine(dir, name);
            return name;
        }

        private void AudioWaveForm_Click(object sender, EventArgs e)
        {
            if (audioVisualizer.WavePeaks == null)
            {
                if (string.IsNullOrEmpty(_videoFileName))
                {
                    buttonOpenVideo_Click(sender, e);
                    if (string.IsNullOrEmpty(_videoFileName))
                        return;
                }

                AddWareForm addWaveForm = new AddWareForm();
                string peakWaveFileName = GetPeakWaveFileName(_videoFileName);
                string spectrogramFolder = GetSpectrogramFolder(_videoFileName);
                addWaveForm.Initialize(_videoFileName, spectrogramFolder);
                if (addWaveForm.ShowDialog() == DialogResult.OK)
                {
                    addWaveForm.WavePeak.WritePeakSamples(peakWaveFileName);
                    var audioPeakWave = new WavePeakGenerator(peakWaveFileName);
                    audioPeakWave.GenerateAllSamples();
                    audioPeakWave.Close();
                    audioVisualizer.WavePeaks = audioPeakWave;
                    if (addWaveForm.SpectrogramBitmaps != null)
                        audioVisualizer.InitializeSpectrogram(addWaveForm.SpectrogramBitmaps, spectrogramFolder);
                    timerWaveForm.Start();
                }
            }
        }

        private void timerWaveForm_Tick(object sender, EventArgs e)
        {
            if (audioVisualizer.Visible && mediaPlayer.VideoPlayer != null && audioVisualizer.WavePeaks != null)
            {
                int index = -1;
                if (SubtitleListview1.SelectedItems.Count > 0)
                    index = SubtitleListview1.SelectedItems[0].Index;

                if (audioVisualizer.Locked)
                {
                    double startPos = mediaPlayer.CurrentPosition - ((audioVisualizer.EndPositionSeconds - audioVisualizer.StartPositionSeconds) / 2.0);
                    if (startPos < 0)
                        startPos = 0;
                    SetWaveFormPosition(startPos, mediaPlayer.CurrentPosition, index);
                }
                else if (mediaPlayer.CurrentPosition > audioVisualizer.EndPositionSeconds || mediaPlayer.CurrentPosition < audioVisualizer.StartPositionSeconds)
                {
                    double startPos = mediaPlayer.CurrentPosition - 0.01;
                    if (startPos < 0)
                        startPos = 0;
                    audioVisualizer.ClearSelection();
                    SetWaveFormPosition(startPos, mediaPlayer.CurrentPosition, index);
                }
                else
                {
                    SetWaveFormPosition(audioVisualizer.StartPositionSeconds, mediaPlayer.CurrentPosition, index);
                }

                bool paused = mediaPlayer.IsPaused;
                toolStripButtonWaveFormPause.Visible = !paused;
                toolStripButtonWaveFormPlay.Visible = paused;
            }
            else
            {
                toolStripButtonWaveFormPlay.Visible = true;
                toolStripButtonWaveFormPause.Visible = false;
            }
        }

        private void addParagraphHereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            audioVisualizer.ClearSelection();
            Paragraph newParagraph = new Paragraph(audioVisualizer.NewSelectionParagraph);
            if (newParagraph == null)
                return;

            mediaPlayer.Pause();

            int startNumber = 1;
            if (_subtitle.Paragraphs.Count > 0)
                startNumber = _subtitle.Paragraphs[0].Number;

            // find index where to insert
            int index = 0;
            foreach (Paragraph p in _subtitle.Paragraphs)
            {
                if (p.StartTime.TotalMilliseconds > newParagraph.StartTime.TotalMilliseconds)
                    break;
                index++;
            }

            // create and insert
            if (GetCurrentSubtitleFormat().IsFrameBased)
            {
                newParagraph.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                newParagraph.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
            }
            _subtitle.Paragraphs.Insert(index, newParagraph);

            if (_subtitleAlternate != null && SubtitleListview1.IsAlternateTextColumnVisible && Configuration.Settings.General.AllowEditOfOriginalSubtitle)
            {
                _subtitleAlternate.InsertParagraphInCorrectTimeOrder(new Paragraph(newParagraph));
                _subtitleAlternate.Renumber(1);
            }

            _subtitleListViewIndex = -1;
            _subtitle.Renumber(startNumber);
            SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
            SubtitleListview1.SelectIndexAndEnsureVisible(index);

            textBoxListViewText.Focus();
            audioVisualizer.NewSelectionParagraph = null;

            ShowStatus(string.Format(_language.VideoControls.NewTextInsertAtX, newParagraph.StartTime.ToShortString()));
            audioVisualizer.Invalidate();
        }

        private void mergeWithPreviousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = _subtitle.GetIndex(audioVisualizer.RightClickedParagraph);
            if (index >= 0)
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(index);
                MergeBeforeToolStripMenuItemClick(null, null);
            }
            audioVisualizer.Invalidate();
        }

        private void deleteParagraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = _subtitle.GetIndex(audioVisualizer.RightClickedParagraph);
            if (index >= 0)
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(index);
                ToolStripMenuItemDeleteClick(null, null);
            }
            audioVisualizer.Invalidate();
        }

        private void splitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                if (audioVisualizer.RightClickedParagraph.StartTime.TotalMilliseconds == _subtitle.Paragraphs[i].StartTime.TotalMilliseconds &&
                    audioVisualizer.RightClickedParagraph.EndTime.TotalMilliseconds == _subtitle.Paragraphs[i].EndTime.TotalMilliseconds)
                {
                    SubtitleListview1.SelectIndexAndEnsureVisible(i);
                    SplitSelectedParagraph(_audioWaveFormRightClickSeconds, null);
                    break;
                }
            }
            audioVisualizer.Invalidate();
        }

        private void mergeWithNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = _subtitle.GetIndex(audioVisualizer.RightClickedParagraph);
            if (index >= 0)
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(index);
                MergeAfterToolStripMenuItemClick(null, null);
            }
            audioVisualizer.Invalidate();
        }

        private void buttonWaveFormZoomIn_Click(object sender, EventArgs e)
        {
            if (audioVisualizer.WavePeaks != null && audioVisualizer.Visible)
            {
                audioVisualizer.ZoomFactor += 0.1;
            }
        }

        private void buttonWaveFormZoomOut_Click(object sender, EventArgs e)
        {
            if (audioVisualizer.WavePeaks != null && audioVisualizer.Visible)
            {
                audioVisualizer.ZoomFactor -= 0.1;
            }
        }

        private void buttonWaveFormZoomReset_Click(object sender, EventArgs e)
        {
            if (audioVisualizer.WavePeaks != null && audioVisualizer.Visible)
            {
                audioVisualizer.ZoomFactor = 1.0;
            }
        }

        private void toolStripMenuItemWaveFormPlaySelection_Click(object sender, EventArgs e)
        {
            if (mediaPlayer != null && mediaPlayer.VideoPlayer != null)
            {
                Paragraph p = audioVisualizer.NewSelectionParagraph;
                if (p == null)
                    p = audioVisualizer.RightClickedParagraph;

                if (p != null)
                {
                    mediaPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                    Utilities.ShowSubtitle(_subtitle.Paragraphs, mediaPlayer);
                    mediaPlayer.Play();
                    _endSeconds = p.EndTime.TotalSeconds;
                }
            }
        }

        private void toolStripButtonWaveFormZoomIn_Click(object sender, EventArgs e)
        {
            if (audioVisualizer.WavePeaks != null && audioVisualizer.Visible)
            {
                audioVisualizer.ZoomFactor += 0.1;
                SelectZoomTextInComboBox();
            }
        }

        private void toolStripButtonWaveFormZoomOut_Click(object sender, EventArgs e)
        {
            if (audioVisualizer.WavePeaks != null && audioVisualizer.Visible)
            {
                audioVisualizer.ZoomFactor -= 0.1;
                SelectZoomTextInComboBox();
            }
        }

        private void toolStripComboBoxWaveForm_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxZoomItem item = toolStripComboBoxWaveForm.SelectedItem as ComboBoxZoomItem;
            if (item != null)
            {
                audioVisualizer.ZoomFactor = item.ZoomFactor;
            }
        }

        private void SelectZoomTextInComboBox()
        {
            int i = 0;
            foreach (object obj in toolStripComboBoxWaveForm.Items)
            {
                ComboBoxZoomItem item = obj as ComboBoxZoomItem;
                if (Math.Abs(audioVisualizer.ZoomFactor - item.ZoomFactor) < 0.001)
                {
                    toolStripComboBoxWaveForm.SelectedIndex = i;
                    return;
                }
                i++;
            }
        }

        private void toolStripButtonWaveFormPause_Click(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void toolStripButtonWaveFormPlay_Click(object sender, EventArgs e)
        {
            mediaPlayer.Play();
        }

        private void toolStripButtonLockCenter_Click(object sender, EventArgs e)
        {
            toolStripButtonLockCenter.Checked = !toolStripButtonLockCenter.Checked;
            audioVisualizer.Locked = toolStripButtonLockCenter.Checked;
            Configuration.Settings.General.WaveFormCenter = audioVisualizer.Locked;
        }

        private void trackBarWaveFormPosition_ValueChanged(object sender, EventArgs e)
        {
            mediaPlayer.CurrentPosition = trackBarWaveFormPosition.Value;
        }

        private void buttonCustomUrl_Click(object sender, EventArgs e)
        {
            RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl1);
        }

        private void buttonCustomUrl2_Click(object sender, EventArgs e)
        {
            RunCustomSearch(Configuration.Settings.VideoControls.CustomSearchUrl2);
        }

        private void ShowhideWaveFormToolStripMenuItemClick(object sender, EventArgs e)
        {
            toolStripButtonToggleWaveForm_Click(null, null);
        }

        private void AudioWaveFormDragEnter(object sender, DragEventArgs e)
        {
            // make sure they're actually dropping files (not text or anything else)
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        private void AudioWaveFormDragDrop(object sender, DragEventArgs e)
        {
            if (string.IsNullOrEmpty(_videoFileName))
                buttonOpenVideo_Click(null, null);
            if (_videoFileName == null)
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                string fileName = files[0];
                string ext = Path.GetExtension(fileName).ToLower();
                if (ext != ".wav")
                {
                    MessageBox.Show(string.Format(".Wav only!", fileName));
                    return;
                }

                var addWaveForm = new AddWareForm();
                addWaveForm.InitializeViaWaveFile(fileName);
                if (addWaveForm.ShowDialog() == DialogResult.OK)
                {
                    string peakWaveFileName = GetPeakWaveFileName(_videoFileName);
                    addWaveForm.WavePeak.WritePeakSamples(peakWaveFileName);
                    var audioPeakWave = new WavePeakGenerator(peakWaveFileName);
                    audioPeakWave.GenerateAllSamples();
                    audioVisualizer.WavePeaks = audioPeakWave;
                    timerWaveForm.Start();
                }
            }
            else
            {
                MessageBox.Show(_language.DropOnlyOneFile);
            }
        }

        private void toolStripMenuItemImportBluRaySup_Click(object sender, EventArgs e)
        {
            if (ContinueNewOrExit())
            {
                openFileDialog1.Title = _language.OpenBluRaySupFile;
                openFileDialog1.FileName = string.Empty;
                openFileDialog1.Filter = _language.BluRaySupFiles + "|*.sup";
                if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    ImportAndOcrBluRaySup(openFileDialog1.FileName);
                }
            }
        }

        private void ImportAndOcrBluRaySup(string fileName)
        {
            var log = new StringBuilder();
            var subtitles = BluRaySupParser.ParseBluRaySup(fileName, log);
            subtitles = SplitBitmaps(subtitles);
            if (subtitles.Count > 0)
            {
                var vobSubOcr = new VobSubOcr();
                vobSubOcr.Initialize(subtitles, Configuration.Settings.VobSubOcr, fileName);
                vobSubOcr.FileName = Path.GetFileName(fileName);
                if (vobSubOcr.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeImportingBluRaySupFile);
                    FileNew();
                    _subtitle.Paragraphs.Clear();
                    SetCurrentFormat(new SubRip().FriendlyName);
                    _subtitle.WasLoadedWithFrameNumbers = false;
                    _subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);
                    foreach (Paragraph p in vobSubOcr.SubtitleFromOcr.Paragraphs)
                    {
                        _subtitle.Paragraphs.Add(p);
                    }

                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    _subtitleListViewIndex = -1;
                    SubtitleListview1.FirstVisibleIndex = -1;
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    _fileName = Path.ChangeExtension(vobSubOcr.FileName, ".srt");
                    SetTitle();
                    _converted = true;

                    Configuration.Settings.Save();
                }
            }
        }

        private List<BluRaySupPicture> SplitBitmaps(List<BluRaySupPicture> subtitles)
        {
            return subtitles;
            var list = new List<BluRaySupPicture>();
            int lastCompositionNumber = -1;

            foreach (var sub in subtitles)
            {
                for (int i=0; i<sub.ImageObjects.Count; i++)
                {
                    var s = new BluRaySupPicture(sub);
                    s.ObjectId = i;
                    if (s.CompositionNumber >= lastCompositionNumber)
                    {
                        int start = list.Count - 20;
                        if (start < 0)
                            start = 0;
                        bool found = false;
                        if (sub.ImageObjects.Count > 1)
                        {

                            for (int k = start; k < list.Count; k++)
                            {
                                if (list[k].ObjectIdImage.Width == sub.ObjectIdImage.Width && list[k].ObjectIdImage.Height == sub.ObjectIdImage.Height &&
                                    list[k].ObjectIdImage.XOffset == sub.ObjectIdImage.XOffset && list[k].ObjectIdImage.YOffset == sub.ObjectIdImage.YOffset)
                                    found = true;
                            }
                        }

                        if (!found)
                            list.Add(s);
                    }
                    lastCompositionNumber = s.CompositionNumber;
                }
            }
            return list;
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBoxListViewTextAlternate.Focused)
                textBoxListViewTextAlternate.SelectAll();
            else
                textBoxListViewText.SelectAll();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBoxListViewTextAlternate.Focused)
                textBoxListViewTextAlternate.Cut();
            else
                textBoxListViewText.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBoxListViewTextAlternate.Focused)
                textBoxListViewTextAlternate.Copy();
            else
                textBoxListViewText.Copy();
        }

        private void PasteToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (textBoxListViewTextAlternate.Focused)
                textBoxListViewTextAlternate.Paste();
            else
                textBoxListViewText.Paste();
        }

        private void DeleteToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (textBoxListViewTextAlternate.Focused)
                textBoxListViewTextAlternate.DeselectAll();
            else
                textBoxListViewText.DeselectAll();
        }

        private void NormalToolStripMenuItem1Click(object sender, EventArgs e)
        {
            TextBox tb;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            else
                tb = textBoxListViewText;

            string text = tb.SelectedText;
            int selectionStart = tb.SelectionStart;
            text = Utilities.RemoveHtmlTags(text);
            tb.SelectedText = text;
            tb.SelectionStart = selectionStart;
            tb.SelectionLength = text.Length;
        }

        private void TextBoxListViewToggleTag(string tag)
        {
            TextBox tb;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            else
                tb = textBoxListViewText;

            string text = tb.SelectedText;
            int selectionStart = tb.SelectionStart;

            if (text.Contains("<" + tag + ">"))
            {
                text = text.Replace("<" + tag + ">", string.Empty);
                text = text.Replace("</" + tag + ">", string.Empty);
            }
            else
            {
                text = string.Format("<{0}>{1}</{0}>", tag, text);
            }

            tb.SelectedText = text;
            tb.SelectionStart = selectionStart;
            tb.SelectionLength = text.Length;
        }

        private void BoldToolStripMenuItem1Click(object sender, EventArgs e)
        {
            TextBoxListViewToggleTag("b");
        }

        private void ItalicToolStripMenuItem1Click(object sender, EventArgs e)
        {
            TextBoxListViewToggleTag("i");
        }

        private void UnderlineToolStripMenuItem1Click(object sender, EventArgs e)
        {
            TextBoxListViewToggleTag("u");
        }

        private void ColorToolStripMenuItem1Click(object sender, EventArgs e)
        {
            TextBox tb;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            else
                tb = textBoxListViewText;

            //color
            string text = tb.SelectedText;
            int selectionStart = tb.SelectionStart;

            if (colorDialog1.ShowDialog(this) == DialogResult.OK)
            {
                string color = Utilities.ColorToHex(colorDialog1.Color);
                bool done = false;
                string s = text;
                if (s.StartsWith("<font "))
                {
                    int start = s.IndexOf("<font ");
                    if (start >= 0)
                    {
                        int end = s.IndexOf(">", start);
                        if (end > 0)
                        {
                            string f = s.Substring(start, end - start);
                            if (f.Contains(" face=") && !f.Contains(" color="))
                            {
                                start = s.IndexOf(" face=", start);
                                s = s.Insert(start, string.Format(" color=\"{0}\"", color));
                                text = s;
                                done = true;
                            }
                            else if (f.Contains(" color="))
                            {
                                int colorStart = f.IndexOf(" color=");
                                if (s.IndexOf("\"", colorStart + " color=".Length + 1) > 0)
                                    end = s.IndexOf("\"", colorStart + " color=".Length + 1);
                                s = s.Substring(0, colorStart) + string.Format(" color=\"{0}", color) + s.Substring(end);
                                text = s;
                                done = true;
                            }
                        }
                    }
                }

                if (!done)
                    text = string.Format("<font color=\"{0}\">{1}</font>", color, text);

                tb.SelectedText = text;
                tb.SelectionStart = selectionStart;
                tb.SelectionLength = text.Length;
            }
        }

        private void FontNameToolStripMenuItemClick(object sender, EventArgs e)
        {
            TextBox tb;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            else
                tb = textBoxListViewText;

            // font name
            string text = tb.SelectedText;
            int selectionStart = tb.SelectionStart;

            if (fontDialog1.ShowDialog(this) == DialogResult.OK)
            {
                bool done = false;

                string s = text;
                if (s.StartsWith("<font "))
                {
                    int start = s.IndexOf("<font ");
                    if (start >= 0)
                    {
                        int end = s.IndexOf(">", start);
                        if (end > 0)
                        {
                            string f = s.Substring(start, end - start);
                            if (f.Contains(" color=") && !f.Contains(" face="))
                            {
                                start = s.IndexOf(" color=", start);
                                s = s.Insert(start, string.Format(" face=\"{0}\"", fontDialog1.Font.Name));
                                text = s;
                                done = true;
                            }
                            else if (f.Contains(" face="))
                            {
                                int faceStart = f.IndexOf(" face=");
                                if (s.IndexOf("\"", faceStart + " face=".Length + 1) > 0)
                                    end = s.IndexOf("\"", faceStart + " face=".Length + 1);
                                s = s.Substring(0, faceStart) + string.Format(" face=\"{0}", fontDialog1.Font.Name) + s.Substring(end);
                                text = s;
                                done = true;
                            }
                        }
                    }
                }
                if (!done)
                    text = string.Format("<font face=\"{0}\">{1}</font>", fontDialog1.Font.Name, s);

                tb.SelectedText = text;
                tb.SelectionStart = selectionStart;
                tb.SelectionLength = text.Length;
            }
        }

        public void SetSubtitle(Subtitle subtitle, string message)
        {
            _subtitle = subtitle;
            SubtitleListview1.Fill(subtitle, _subtitleAlternate);
            ShowStatus(message);
        }

        #region Networking
        private void startServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _networkSession = new NikseWebServiceSession(_subtitle, _subtitleAlternate, TimerWebServiceTick, OnUpdateUserLogEntries);
            NetworkStart networkNew = new NetworkStart();
            networkNew.Initialize(_networkSession, _fileName);
            _formPositionsAndSizes.SetPositionAndSize(networkNew);
            if (networkNew.ShowDialog(this) == DialogResult.OK)
            {
                if (GetCurrentSubtitleFormat().HasStyleSupport)
                {
                    SubtitleListview1.HideExtraColumn();
                }

                _networkSession.AppendToLog(string.Format(_language.XStartedSessionYAtZ, _networkSession.CurrentUser.UserName, _networkSession.SessionId, DateTime.Now.ToLongTimeString()));
                toolStripStatusNetworking.Visible = true;
                toolStripStatusNetworking.Text = _language.NetworkMode;
                EnableDisableControlsNotWorkingInNetworkMode(false);
                SubtitleListview1.ShowExtraColumn(_language.UserAndAction);
                SubtitleListview1.AutoSizeAllColumns(this);
                TimerWebServiceTick(null, null);
            }
            else
            {
                _networkSession = null;
            }
            _formPositionsAndSizes.SavePositionAndSize(networkNew);
        }

        private void joinSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _networkSession = new NikseWebServiceSession(_subtitle, _subtitleAlternate, TimerWebServiceTick, OnUpdateUserLogEntries);
            NetworkJoin networkJoin = new NetworkJoin();
            networkJoin.Initialize(_networkSession);

            if (networkJoin.ShowDialog(this) == DialogResult.OK)
            {
                _subtitle = _networkSession.Subtitle;
                _subtitleAlternate = _networkSession.OriginalSubtitle;
                if (_subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                    SubtitleListview1.ShowAlternateTextColumn(Configuration.Settings.Language.General.OriginalText);
                _fileName = networkJoin.FileName;
                SetTitle();
                Text = Title;
                toolStripStatusNetworking.Visible = true;
                toolStripStatusNetworking.Text = _language.NetworkMode;
                EnableDisableControlsNotWorkingInNetworkMode(false);
                _networkSession.AppendToLog(string.Format(_language.XStartedSessionYAtZ, _networkSession.CurrentUser.UserName, _networkSession.SessionId, DateTime.Now.ToLongTimeString()));
                SubtitleListview1.ShowExtraColumn(_language.UserAndAction);
                SubtitleListview1.AutoSizeAllColumns(this);
                _subtitleListViewIndex = -1;
                _oldSelectedParagraph = null;
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                SubtitleListview1.SelectIndexAndEnsureVisible(0);
                TimerWebServiceTick(null, null);
            }
            else
            {
                _networkSession = null;
            }
        }

        private void EnableDisableControlsNotWorkingInNetworkMode(bool enabled)
        {
            //Top menu
            newToolStripMenuItem.Enabled = enabled;
            openToolStripMenuItem.Enabled = enabled;
            reopenToolStripMenuItem.Enabled = enabled;
            toolStripMenuItemOpenContainingFolder.Enabled = enabled;
            toolStripMenuItemCompare.Enabled = enabled;
            toolStripMenuItemImportDvdSubtitles.Enabled = enabled;
            toolStripMenuItemSubIdx.Enabled = enabled;
            toolStripMenuItemImportBluRaySup.Enabled = enabled;
            matroskaImportStripMenuItem.Enabled = enabled;
            toolStripMenuItemManualAnsi.Enabled = enabled;
            toolStripMenuItemImportText.Enabled = enabled;
            toolStripMenuItemImportTimeCodes.Enabled = enabled;

            showHistoryforUndoToolStripMenuItem.Enabled = enabled;
            multipleReplaceToolStripMenuItem.Enabled = enabled;

            toolsToolStripMenuItem.Enabled = enabled;

            toolStripMenuItemSyncronization.Enabled = enabled;

            toolStripMenuItemAutoTranslate.Enabled = enabled;

            //Toolbar
            toolStripButtonFileNew.Enabled = enabled;
            toolStripButtonFileOpen.Enabled = enabled;
            toolStripButtonVisualSync.Enabled = enabled;

            // textbox source
            textBoxSource.ReadOnly = !enabled;
        }

        internal void TimerWebServiceTick(object sender, EventArgs e)
        {
            if (_networkSession == null)
                return;

            List<int> deleteIndices = new List<int>();
            NetworkGetSendUpdates(deleteIndices, 0, null);
        }

        private void NetworkGetSendUpdates(List<int> deleteIndices, int insertIndex, Paragraph insertParagraph)
        {
            _networkSession.TimerStop();

            bool doReFill = false;
            bool updateListViewStatus = false;
            SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
            string message = string.Empty;

            int numberOfLines = 0;
            List<SeNetworkService.SeUpdate> updates = null;
            try
            {
                updates = _networkSession.GetUpdates(out message, out numberOfLines);
            }
            catch (Exception exception)
            {
                MessageBox.Show(string.Format(_language.NetworkUnableToConnectToServer, exception.Message));
                _networkSession.TimerStop();
                if (_networkChat != null && !_networkChat.IsDisposed)
                {
                    _networkChat.Close();
                    _networkChat = null;
                }
                _networkSession = null;
                EnableDisableControlsNotWorkingInNetworkMode(true);
                toolStripStatusNetworking.Visible = false;
                SubtitleListview1.HideExtraColumn();
                _networkChat = null;
                return;
            }
            int currentSelectedIndex = -1;
            if (SubtitleListview1.SelectedItems.Count > 0)
                currentSelectedIndex = SubtitleListview1.SelectedItems[0].Index;
            int oldCurrentSelectedIndex = currentSelectedIndex;
            if (message == "OK")
            {
                foreach (var update in updates)
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        if (!update.Text.Contains(Environment.NewLine))
                            update.Text = update.Text.Replace("\n", Environment.NewLine);
//                        update.Text = HttpUtility.HtmlDecode(update.Text).Replace("<br />", Environment.NewLine);
                        update.Text = Utilities.HtmlDecode(update.Text).Replace("<br />", Environment.NewLine);
                    }
                    if (update.User.Ip != _networkSession.CurrentUser.Ip || update.User.UserName != _networkSession.CurrentUser.UserName)
                    {
                        if (update.Action == "USR")
                        {
                            _networkSession.Users.Add(update.User);
                            if (_networkChat != null && !_networkChat.IsDisposed)
                            {
                                _networkChat.AddUser(update.User);
                            }
                            _networkSession.AppendToLog(string.Format(_language.NetworkNewUser, update.User.UserName, update.User.Ip ));
                        }
                        else if (update.Action == "MSG")
                        {
                            _networkSession.ChatLog.Add(new NikseWebServiceSession.ChatEntry() { User = update.User, Message = update.Text });
                            if (_networkChat == null || _networkChat.IsDisposed)
                            {
                                _networkChat = new NetworkChat();
                                _networkChat.Initialize(_networkSession);
                                _networkChat.Show(this);
                            }
                            else
                            {
                                _networkChat.AddChatMessage(update.User, update.Text);
                            }
                            _networkSession.AppendToLog(string.Format(_language.NetworkMessage, update.User.UserName, update.User.Ip, update.Text));
                        }
                        else if (update.Action == "DEL")
                        {
                            doReFill = true;
                            _subtitle.Paragraphs.RemoveAt(update.Index);
                            if (_networkSession.LastSubtitle != null)
                                _networkSession.LastSubtitle.Paragraphs.RemoveAt(update.Index);
                            _networkSession.AppendToLog(string.Format(_language.NetworkDelete, update.User.UserName , update.User.Ip, update.Index.ToString()));
                            _networkSession.AdjustUpdateLogToDelete(update.Index);

                            if (deleteIndices.Count > 0)
                            {
                                for (int i = deleteIndices.Count - 1; i >= 0; i--)
                                {
                                    int index = deleteIndices[i];
                                    if (index == update.Index)
                                        deleteIndices.RemoveAt(i);
                                    else if (index > update.Index)
                                        deleteIndices[i] = index - 1;
                                }
                            }

                            if (insertIndex > update.Index)
                                insertIndex--;
                            if (currentSelectedIndex >= 0 && currentSelectedIndex > update.Index)
                                currentSelectedIndex--;
                        }
                        else if (update.Action == "INS")
                        {
                            doReFill = true;
                            Paragraph p = new Paragraph(update.Text, update.StartMilliseconds, update.EndMilliseconds);
                            _subtitle.Paragraphs.Insert(update.Index, p);
                            if (_networkSession.LastSubtitle != null)
                                _networkSession.LastSubtitle.Paragraphs.Insert(update.Index, new Paragraph(p));
                            _networkSession.AppendToLog(string.Format(_language.NetworkInsert, update.User.UserName, update.User.Ip, update.Index.ToString(), update.Text.Replace(Environment.NewLine, Configuration.Settings.General.ListViewLineSeparatorString)));
                            _networkSession.AddToWsUserLog(update.User, update.Index, update.Action, false);
                            updateListViewStatus = true;
                            _networkSession.AdjustUpdateLogToInsert(update.Index);

                            if (deleteIndices.Count > 0)
                            {
                                for (int i = deleteIndices.Count - 1; i >= 0; i--)
                                {
                                    int index = deleteIndices[i];
                                    if (index > update.Index)
                                        deleteIndices[i] = index +1;
                                }
                            }
                            if (insertIndex > update.Index)
                                insertIndex++;
                            if (currentSelectedIndex >= 0 && currentSelectedIndex > update.Index)
                                currentSelectedIndex++;
                        }
                        else if (update.Action == "UPD")
                        {
                            updateListViewStatus = true;
                            Paragraph p = _subtitle.GetParagraphOrDefault(update.Index);
                            if (p != null)
                            {
                                p.StartTime.TotalMilliseconds = update.StartMilliseconds;
                                p.EndTime.TotalMilliseconds = update.EndMilliseconds;
                                p.Text = update.Text;
                                SubtitleListview1.SetTimeAndText(update.Index, p);
                                _networkSession.AppendToLog(string.Format(_language.NetworkUpdate, update.User.UserName, update.User.Ip, update.Index.ToString(), update.Text.Replace(Environment.NewLine, Configuration.Settings.General.ListViewLineSeparatorString)));
                                _networkSession.AddToWsUserLog(update.User, update.Index, update.Action, true);
                                updateListViewStatus = true;
                            }
                            if (_networkSession.LastSubtitle != null)
                            {
                                p = _networkSession.LastSubtitle.GetParagraphOrDefault(update.Index);
                                if (p != null)
                                {
                                    p.StartTime.TotalMilliseconds = update.StartMilliseconds;
                                    p.EndTime.TotalMilliseconds = update.EndMilliseconds;
                                    p.Text = update.Text;
                                }
                            }
                        }
                        else if (update.Action == "BYE")
                        {
                            if (_networkChat != null && !_networkChat.IsDisposed)
                                _networkChat.RemoveUser(update.User);

                            SeNetworkService.SeUser removeUser = null;
                            foreach (var user in _networkSession.Users)
                            {
                                if (user.UserName == update.User.UserName)
                                {
                                    removeUser = user;
                                    break;
                                }
                            }
                            if (removeUser != null)
                                _networkSession.Users.Remove(removeUser);

                            _networkSession.AppendToLog(string.Format(_language.NetworkByeUser, update.User.UserName, update.User.Ip));
                        }
                        else
                        {
                            _networkSession.AppendToLog("UNKNOWN ACTION: " + update.Action + " by " + update.User.UserName + " (" + update.User.Ip + ")");
                        }
                    }
                }
                if (numberOfLines != _subtitle.Paragraphs.Count)
                {
                    _subtitle = _networkSession.ReloadSubtitle();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    UpdateListviewWithUserLogEntries();
                    _networkSession.LastSubtitle = new Subtitle(_subtitle);
                    _oldSelectedParagraph = null;
                    SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                    _networkSession.TimerStart();
                    RefreshSelectedParagraph();
                    return;
                }
                if (deleteIndices.Count > 0)
                {
                    deleteIndices.Sort();
                    deleteIndices.Reverse();
                    foreach (int i in deleteIndices)
                    {
                        _subtitle.Paragraphs.RemoveAt(i);
                        if (_networkSession != null && _networkSession.LastSubtitle != null && i < _networkSession.LastSubtitle.Paragraphs.Count)
                            _networkSession.LastSubtitle.Paragraphs.RemoveAt(i);
                    }

                    _networkSession.DeleteLines(deleteIndices);
                    doReFill = true;
                }
                if (insertIndex >= 0 && insertParagraph != null)
                {
                    _subtitle.Paragraphs.Insert(insertIndex, insertParagraph);
                    if (_networkSession != null && _networkSession.LastSubtitle != null && insertIndex < _networkSession.LastSubtitle.Paragraphs.Count)
                        _networkSession.LastSubtitle.Paragraphs.Insert(insertIndex, insertParagraph);
                    _networkSession.InsertLine(insertIndex, insertParagraph);
                    doReFill = true;
                }
                _networkSession.CheckForAndSubmitUpdates(updates); // updates only (no inserts/deletes)
            }
            else
            {
                MessageBox.Show(message);
                LeaveSessionToolStripMenuItemClick(null, null);
                SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                return;
            }
            if (doReFill)
            {
                _subtitle.Renumber(1);
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                UpdateListviewWithUserLogEntries();

                if (oldCurrentSelectedIndex != currentSelectedIndex)
                {
                    _oldSelectedParagraph = null;
                    _subtitleListViewIndex = currentSelectedIndex;
                    SubtitleListview1.SelectIndexAndEnsureVisible(_subtitleListViewIndex);
                }
                else if (_oldSelectedParagraph != null)
                {
                    Paragraph p = _subtitle.GetFirstAlike(_oldSelectedParagraph);
                    if (p != null)
                    {
                        _subtitleListViewIndex = _subtitle.GetIndex(p);
                        SubtitleListview1.SelectIndexAndEnsureVisible(_subtitleListViewIndex);
                    }
                }
            }
            else if (updateListViewStatus)
            {
                UpdateListviewWithUserLogEntries();
            }
            _networkSession.LastSubtitle = new Subtitle(_subtitle);
            SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
            _networkSession.TimerStart();
        }

        private void UpdateListviewWithUserLogEntries()
        {
            SubtitleListview1.BeginUpdate();
            foreach (UpdateLogEntry entry in _networkSession.UpdateLog)
                SubtitleListview1.SetExtraText(entry.Index, entry.ToString(), Utilities.GetColorFromUserName(entry.UserName));
            SubtitleListview1.EndUpdate();
        }

        private void LeaveSessionToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_networkSession != null)
            {
                _networkSession.TimerStop();
                _networkSession.Leave();
            }
            if (_networkChat != null && !_networkChat.IsDisposed)
            {
                _networkChat.Close();
                _networkChat = null;
            }
            _networkSession = null;
            EnableDisableControlsNotWorkingInNetworkMode(true);
            toolStripStatusNetworking.Visible = false;
            SubtitleListview1.HideExtraColumn();
            _networkChat = null;

            var format = GetCurrentSubtitleFormat();
            if (format.HasStyleSupport && _networkSession == null)
            {
                if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                    SubtitleListview1.ShowExtraColumn(Configuration.Settings.Language.General.Class);
                else
                    SubtitleListview1.ShowExtraColumn(Configuration.Settings.Language.General.Style);
                SubtitleListview1.DisplayExtraFromExtra = true;
            }
        }

        private void toolStripMenuItemNetworking_DropDownOpening(object sender, EventArgs e)
        {
            startServerToolStripMenuItem.Visible = _networkSession == null;
            joinSessionToolStripMenuItem.Visible = _networkSession == null;
            showSessionKeyLogToolStripMenuItem.Visible = _networkSession != null;
            leaveSessionToolStripMenuItem.Visible = _networkSession != null;
            chatToolStripMenuItem.Visible = _networkSession != null;
        }

        internal void OnUpdateUserLogEntries(object sender, EventArgs e)
        {
            UpdateListviewWithUserLogEntries();
        }

        private void toolStripStatusNetworking_Click(object sender, EventArgs e)
        {
            showSessionKeyLogToolStripMenuItem_Click(null, null);
        }

        private void showSessionKeyLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetworkLogAndInfo networkLog = new NetworkLogAndInfo();
            networkLog.Initialize(_networkSession);
            networkLog.ShowDialog(this);
        }

        private void chatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_networkSession != null)
            {
                if (_networkChat == null || _networkChat.IsDisposed)
                {
                    _networkChat = new NetworkChat();
                    _networkChat.Initialize(_networkSession);
                    _networkChat.Show(this);
                }
                else
                {
                    _networkChat.WindowState = FormWindowState.Normal;
                }
            }
        }
        #endregion

        private void UnDockVideoPlayer()
        {
            bool firstUndock = _videoPlayerUnDocked != null && !_videoPlayerUnDocked.IsDisposed;

            _videoPlayerUnDocked = new VideoPlayerUnDocked(this, _formPositionsAndSizes, mediaPlayer);
            _formPositionsAndSizes.SetPositionAndSize(_videoPlayerUnDocked);

            if (firstUndock)
            {
                Configuration.Settings.General.UndockedVideoPosition = _videoPlayerUnDocked.Left.ToString() + ";" + _videoPlayerUnDocked.Top.ToString() + ";" + _videoPlayerUnDocked.Width + ";" + _videoPlayerUnDocked.Height;
            }

            Control control = null;
            if (splitContainer1.Panel2.Controls.Count == 0)
            {
                control = panelVideoPlayer;
                groupBoxVideo.Controls.Remove(control);
            }
            else if (splitContainer1.Panel2.Controls.Count > 0)
            {
                control = panelVideoPlayer;
                splitContainer1.Panel2.Controls.Clear();
            }
            if (control != null)
            {
                control.Top = 0;
                control.Left = 0;
                control.Width = _videoPlayerUnDocked.PanelContainer.Width;
                control.Height = _videoPlayerUnDocked.PanelContainer.Height;
                _videoPlayerUnDocked.PanelContainer.Controls.Add(control);
            }
        }

        public void ReDockVideoPlayer(Control control)
        {
            groupBoxVideo.Controls.Add(control);
            mediaPlayer.FontSizeFactor = 1.0F;
            mediaPlayer.SetSubtitleFont();
            mediaPlayer.SubtitleText = string.Empty;
        }

        private void UnDockWaveForm()
        {
            _waveFormUnDocked = new WaveFormUnDocked(this, _formPositionsAndSizes);
            _formPositionsAndSizes.SetPositionAndSize(_waveFormUnDocked);

            var control = audioVisualizer;
            groupBoxVideo.Controls.Remove(control);
            control.Top = 0;
            control.Left = 0;
            control.Width = _waveFormUnDocked.PanelContainer.Width;
            control.Height = _waveFormUnDocked.PanelContainer.Height - panelWaveFormControls.Height;
            _waveFormUnDocked.PanelContainer.Controls.Add(control);

            var control2 = (Control)panelWaveFormControls;
            groupBoxVideo.Controls.Remove(control2);
            control2.Top = control.Height;
            control2.Left = 0;
            _waveFormUnDocked.PanelContainer.Controls.Add(control2);

            var control3 = (Control)trackBarWaveFormPosition;
            groupBoxVideo.Controls.Remove(control3);
            control3.Top = control.Height;
            control3.Left = control2.Width +2;
            control3.Width = _waveFormUnDocked.PanelContainer.Width - control3.Left;
            _waveFormUnDocked.PanelContainer.Controls.Add(control3);
        }

        public void ReDockWaveForm(Control waveForm, Control buttons, Control trackBar)
        {
            groupBoxVideo.Controls.Add(waveForm);
            waveForm.Top = 30;
            waveForm.Height = groupBoxVideo.Height - (waveForm.Top + buttons.Height + 10);

            groupBoxVideo.Controls.Add(buttons);
            buttons.Top = waveForm.Top + waveForm.Height + 5;

            groupBoxVideo.Controls.Add(trackBar);
            trackBar.Top = buttons.Top;
        }

        private void UnDockVideoButtons()
        {
            _videoControlsUnDocked = new VideoControlsUndocked(this, _formPositionsAndSizes);
            _formPositionsAndSizes.SetPositionAndSize(_videoControlsUnDocked);
            var control = tabControlButtons;
            groupBoxVideo.Controls.Remove(control);
            control.Top = 25;
            control.Left = 0;
            _videoControlsUnDocked.PanelContainer.Controls.Add(control);

            groupBoxVideo.Controls.Remove(checkBoxSyncListViewWithVideoWhilePlaying);
            _videoControlsUnDocked.PanelContainer.Controls.Add(checkBoxSyncListViewWithVideoWhilePlaying);
            checkBoxSyncListViewWithVideoWhilePlaying.Top = 5;
            checkBoxSyncListViewWithVideoWhilePlaying.Left = 5;

            splitContainerMain.Panel2Collapsed = true;
            splitContainer1.Panel2Collapsed = true;
        }

        public void ReDockVideoButtons(Control videoButtons, Control checkBoxSyncSubWithVideo)
        {
            groupBoxVideo.Controls.Add(videoButtons);
            videoButtons.Top = 12;
            videoButtons.Left = 5;

            groupBoxVideo.Controls.Add(checkBoxSyncSubWithVideo);
            checkBoxSyncSubWithVideo.Top = 11;
            checkBoxSyncSubWithVideo.Left = videoButtons.Left + videoButtons.Width + 5;
        }

        private void UndockVideoControlsToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (Configuration.Settings.General.Undocked)
                return;

            Configuration.Settings.General.Undocked = true;

            UnDockVideoPlayer();
            if (toolStripButtonToggleVideo.Checked)
            {
                _videoPlayerUnDocked.Show(this);
                if (_videoPlayerUnDocked.Top < -999 || _videoPlayerUnDocked.Left < -999)
                {
                    _videoPlayerUnDocked.WindowState = FormWindowState.Minimized;
                    _videoPlayerUnDocked.Top = Top + 40;
                    _videoPlayerUnDocked.Left = Math.Abs(Left - 20);
                    _videoPlayerUnDocked.Width = 600;
                    _videoPlayerUnDocked.Height = 400;
                }
            }

            UnDockWaveForm();
            if (toolStripButtonToggleWaveForm.Checked)
            {
                _waveFormUnDocked.Show(this);
                if (_waveFormUnDocked.Top < -999 || _waveFormUnDocked.Left < -999)
                {
                    _waveFormUnDocked.WindowState = FormWindowState.Minimized;
                    _waveFormUnDocked.Top = Top + 60;
                    _waveFormUnDocked.Left = Math.Abs(Left - 15);
                    _waveFormUnDocked.Width = 600;
                    _waveFormUnDocked.Height= 200;
                }
            }

            UnDockVideoButtons();
            _videoControlsUnDocked.Show(this);
            if (_videoControlsUnDocked.Top < -999 || _videoControlsUnDocked.Left < -999)
            {
                _videoControlsUnDocked.WindowState = FormWindowState.Minimized;
                _videoControlsUnDocked.Top = Top + 40;
                _videoControlsUnDocked.Left = Math.Abs(Left - 10);
                _videoControlsUnDocked.Width = tabControlButtons.Width + 20;
                _videoControlsUnDocked.Height = tabControlButtons.Height + 65;
            }

            _isVideoControlsUnDocked = true;
            SetUndockedWindowsTitle();

            undockVideoControlsToolStripMenuItem.Visible = false;
            redockVideoControlsToolStripMenuItem.Visible = true;

            tabControl1_SelectedIndexChanged(null, null);
        }

        public void RedockVideoControlsToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (!Configuration.Settings.General.Undocked)
                return;

            SaveUndockedPositions();

            Configuration.Settings.General.Undocked = false;

            if (_videoControlsUnDocked != null && !_videoControlsUnDocked.IsDisposed)
            {
                var control = _videoControlsUnDocked.PanelContainer.Controls[0];
                var controlCheckBox = _videoControlsUnDocked.PanelContainer.Controls[1];
                _videoControlsUnDocked.PanelContainer.Controls.Clear();
                ReDockVideoButtons(control, controlCheckBox);
                _videoControlsUnDocked.Close();
                _videoControlsUnDocked = null;
            }

            if (_waveFormUnDocked != null && !_waveFormUnDocked.IsDisposed)
            {
                var controlWaveForm = _waveFormUnDocked.PanelContainer.Controls[0];
                var controlButtons = _waveFormUnDocked.PanelContainer.Controls[1];
                var controlTrackBar = _waveFormUnDocked.PanelContainer.Controls[2];
                _waveFormUnDocked.PanelContainer.Controls.Clear();
                ReDockWaveForm(controlWaveForm, controlButtons, controlTrackBar);
                _waveFormUnDocked.Close();
                _waveFormUnDocked = null;
            }

            if (_videoPlayerUnDocked != null && !_videoPlayerUnDocked.IsDisposed)
            {
                var control = _videoPlayerUnDocked.PanelContainer.Controls[0];
                _videoPlayerUnDocked.PanelContainer.Controls.Remove(control);
                ReDockVideoPlayer(control);
                _videoPlayerUnDocked.Close();
                _videoPlayerUnDocked = null;
                if (mediaPlayer != null)
                    mediaPlayer.ShowFullscreenButton = Configuration.Settings.General.VideoPlayerShowFullscreenButton;
            }

            _isVideoControlsUnDocked = false;
            _videoPlayerUnDocked = null;
            _waveFormUnDocked = null;
            _videoControlsUnDocked = null;
            ShowVideoPlayer();

            audioVisualizer.Visible = toolStripButtonToggleWaveForm.Checked;
            trackBarWaveFormPosition.Visible = toolStripButtonToggleWaveForm.Checked;
            panelWaveFormControls.Visible = toolStripButtonToggleWaveForm.Checked;
            if (!toolStripButtonToggleVideo.Checked)
                HideVideoPlayer();

            mediaPlayer.Invalidate();
            Refresh();

            undockVideoControlsToolStripMenuItem.Visible = true;
            redockVideoControlsToolStripMenuItem.Visible = false;
        }

        internal void SetWaveFormToggleOff()
        {
            toolStripButtonToggleWaveForm.Checked = false;
        }

        internal void SetVideoPlayerToggleOff()
        {
            toolStripButtonToggleVideo.Checked = false;
        }

        private void ToolStripMenuItemInsertSubtitleClick(object sender, EventArgs e)
        {
            openFileDialog1.Title = _languageGeneral.OpenSubtitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetOpenDialogFilter();
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                if (!File.Exists(openFileDialog1.FileName))
                    return;

                var fi = new FileInfo(openFileDialog1.FileName);
                if (fi.Length > 1024 * 1024 * 10) // max 10 mb
                {
                    if (MessageBox.Show(string.Format(_language.FileXIsLargerThan10Mb + Environment.NewLine +
                                                      Environment.NewLine +
                                                      _language.ContinueAnyway,
                                                      openFileDialog1.FileName), Title, MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                        return;
                }

                MakeHistoryForUndo(string.Format(_language.BeforeInsertLine, openFileDialog1.FileName));

                Encoding encoding = null;
                var subtitle = new Subtitle();
                SubtitleFormat format = subtitle.LoadSubtitle(openFileDialog1.FileName, out encoding, encoding);

                if (format != null)
                {
                    SaveSubtitleListviewIndexes();
                    if (format.IsFrameBased)
                        subtitle.CalculateTimeCodesFromFrameNumbers(CurrentFrameRate);
                    else
                        subtitle.CalculateFrameNumbersFromTimeCodes(CurrentFrameRate);

                    if (Configuration.Settings.General.RemoveBlankLinesWhenOpening)
                        subtitle.RemoveEmptyLines();

                    int index = FirstSelectedIndex;
                    if (index < 0)
                        index = 0;
                    foreach (Paragraph p in subtitle.Paragraphs)
                    {
                        _subtitle.Paragraphs.Insert(index, new Paragraph(p));
                        index++;
                    }

                    if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
                    {
                        index = FirstSelectedIndex;
                        if (index < 0)
                            index = 0;
                        Paragraph current = _subtitle.GetParagraphOrDefault(index);
                        if (current != null)
                        {
                            Paragraph original = Utilities.GetOriginalParagraph(index, current, _subtitleAlternate.Paragraphs);
                            if (original != null)
                            {
                                index = _subtitleAlternate.GetIndex(original);
                                foreach (Paragraph p in subtitle.Paragraphs)
                                {
                                    _subtitleAlternate.Paragraphs.Insert(index, new Paragraph(p));
                                    index++;
                                }
                                if (subtitle.Paragraphs.Count > 0)
                                    _subtitleAlternate.Renumber(1);
                            }
                        }
                    }
                    _subtitle.Renumber(1);
                    ShowSource();
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    RestoreSubtitleListviewIndexes();
                }
            }
        }

        private void InsertLineToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (!IsSubtitleLoaded)
            {
                InsertBefore();
            }
            else
            {
                SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.Paragraphs.Count - 1);
                InsertAfter();
                SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.Paragraphs.Count - 1);
            }
        }

        private void CloseVideoToolStripMenuItemClick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (mediaPlayer != null && mediaPlayer.VideoPlayer != null)
            {
                mediaPlayer.SubtitleText = string.Empty;
                mediaPlayer.VideoPlayer.DisposeVideoPlayer();
            }
            mediaPlayer.SetPlayerName(string.Empty);
            mediaPlayer.ResetTimeLabel();
            mediaPlayer.VideoPlayer = null;
            _videoFileName = null;
            _videoInfo = null;
            _videoAudioTrackNumber = -1;
            labelVideoInfo.Text = Configuration.Settings.Language.General.NoVideoLoaded;
            audioVisualizer.WavePeaks = null;
            audioVisualizer.ResetSpectrogram();
            audioVisualizer.Invalidate();

        }

        private void ToolStripMenuItemVideoDropDownOpening(object sender, EventArgs e)
        {
            if (_isVideoControlsUnDocked)
            {
                redockVideoControlsToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoToggleVideoControls);
                undockVideoControlsToolStripMenuItem.ShortcutKeys = Keys.None;
            }
            else
            {
                undockVideoControlsToolStripMenuItem.ShortcutKeys = Utilities.GetKeys(Configuration.Settings.Shortcuts.MainVideoToggleVideoControls);
                redockVideoControlsToolStripMenuItem.ShortcutKeys = Keys.None;
            }

            closeVideoToolStripMenuItem.Visible = !string.IsNullOrEmpty(_videoFileName);
            toolStripMenuItemSetAudioTrack.Visible = false;
            if (mediaPlayer.VideoPlayer != null && mediaPlayer.VideoPlayer is Logic.VideoPlayers.LibVlc11xDynamic)
            {
                var libVlc = (Logic.VideoPlayers.LibVlc11xDynamic)mediaPlayer.VideoPlayer;
                int numberOfTracks = libVlc.AudioTrackCount;
                _videoAudioTrackNumber = libVlc.AudioTrackNumber;
                if (numberOfTracks > 1)
                {
                    toolStripMenuItemSetAudioTrack.DropDownItems.Clear();
                    for (int i = 0; i < numberOfTracks; i++)
                    {
                        toolStripMenuItemSetAudioTrack.DropDownItems.Add((i + 1).ToString(), null, ChooseAudioTrack);
                        if (i == _videoAudioTrackNumber)
                            toolStripMenuItemSetAudioTrack.DropDownItems[toolStripMenuItemSetAudioTrack.DropDownItems.Count - 1].Select();
                    }
                    toolStripMenuItemSetAudioTrack.Visible = true;
                }
            }
        }

        private void ChooseAudioTrack(object sender, EventArgs e)
        {
            if (mediaPlayer.VideoPlayer != null && mediaPlayer.VideoPlayer is Logic.VideoPlayers.LibVlc11xDynamic)
            {
                var libVlc = (Logic.VideoPlayers.LibVlc11xDynamic)mediaPlayer.VideoPlayer;
                var item = sender as ToolStripItem;

                int number = int.Parse(item.Text);
                number--;
                libVlc.AudioTrackNumber = number;
                _videoAudioTrackNumber = number;
            }
        }

        private void textBoxListViewTextAlternate_TextChanged(object sender, EventArgs e)
        {
            if (_subtitleAlternate == null || _subtitleAlternate.Paragraphs.Count < 1)
                return;

            if (_subtitleListViewIndex >= 0)
            {
                Paragraph original = Utilities.GetOriginalParagraph(_subtitleListViewIndex, _subtitle.Paragraphs[_subtitleListViewIndex], _subtitleAlternate.Paragraphs);
                if (original != null)
                {
                    string text = textBoxListViewTextAlternate.Text.TrimEnd();

                    // update _subtitle + listview
                    original.Text = text;
                    UpdateListViewTextInfo(labelTextAlternateLineLengths, labelAlternateSingleLine, labelTextAlternateLineTotal, labelAlternateCharactersPerSecond, original, textBoxListViewTextAlternate);
                    SubtitleListview1.SetAlternateText(_subtitleListViewIndex, text);
                }
            }
        }

        private void TextBoxListViewTextAlternateKeyDown(object sender, KeyEventArgs e)
        {
            _listViewAlternateTextTicks = DateTime.Now.Ticks;
            if (_subtitleAlternate == null || _subtitleAlternate.Paragraphs.Count < 1)
                return;

            int numberOfNewLines = textBoxListViewTextAlternate.Text.Length - textBoxListViewTextAlternate.Text.Replace(Environment.NewLine, " ").Length;

            Utilities.CheckAutoWrap(textBoxListViewTextAlternate, e, numberOfNewLines);

            if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None && numberOfNewLines > 1)
            {
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.R)
            {
                if (textBoxListViewTextAlternate.Text.Length > 0)
                    textBoxListViewTextAlternate.Text = Utilities.AutoBreakLine(textBoxListViewTextAlternate.Text);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.U)
            {
                textBoxListViewTextAlternate.Text = Utilities.UnbreakLine(textBoxListViewTextAlternate.Text);
                e.SuppressKeyPress = true;
            }
            else if (e.Modifiers == Keys.Alt && e.KeyCode == Keys.I)
            {
                if (textBoxListViewTextAlternate.SelectionLength == 0)
                {
                    string tag = "i";
                    if (textBoxListViewTextAlternate.Text.Contains("<" + tag + ">"))
                    {
                        textBoxListViewTextAlternate.Text = textBoxListViewTextAlternate.Text.Replace("<" + tag + ">", string.Empty);
                        textBoxListViewTextAlternate.Text = textBoxListViewTextAlternate.Text.Replace("</" + tag + ">", string.Empty);
                    }
                    else
                    {
                        textBoxListViewTextAlternate.Text = string.Format("<{0}>{1}</{0}>", tag, textBoxListViewTextAlternate.Text);
                    }
                }
                else
                {
                   TextBoxListViewToggleTag("i");
                }
            }
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.D)
            {
                textBoxListViewTextAlternate.SelectionLength = 0;
                e.SuppressKeyPress = true;
            }

            // last key down in text
            _lastTextKeyDownTicks = DateTime.Now.Ticks;

            UpdatePositionAndTotalLength(labelTextAlternateLineTotal, textBoxListViewTextAlternate);
        }

        private void OpenOriginalToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenAlternateSubtitle();
        }

        private void SaveOriginalAstoolStripMenuItemClick(object sender, EventArgs e)
        {
            SubtitleFormat currentFormat = GetCurrentSubtitleFormat();
            Utilities.SetSaveDialogFilter(saveFileDialog1, currentFormat);

            saveFileDialog1.Title = _language.SaveOriginalSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + currentFormat.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_subtitleAlternateFileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                _subtitleAlternateFileName = saveFileDialog1.FileName;
                SaveOriginalSubtitle(GetCurrentSubtitleFormat());
                Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
            }
        }

        private void SaveOriginalToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_subtitleAlternateFileName))
            {
                SaveOriginalAstoolStripMenuItemClick(null, null);
                return;
            }

            try
            {
                SaveOriginalSubtitle(GetCurrentSubtitleFormat());
            }
            catch
            {
                MessageBox.Show(string.Format(_language.UnableToSaveSubtitleX, _subtitleAlternateFileName));
            }
        }

        private void RemoveOriginalToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContinueNewOrExitAlternate())
            {
                RemoveAlternate(true);
            }
        }

        private void RemoveAlternate(bool removeFromListView)
        {
            if (removeFromListView)
            {
                SubtitleListview1.HideAlternateTextColumn();
                SubtitleListview1.AutoSizeAllColumns(this);
                _subtitleAlternate = new Subtitle();
                _subtitleAlternateFileName = null;

                Configuration.Settings.RecentFiles.Add(_fileName, FirstVisibleIndex, FirstSelectedIndex, _videoFileName, _subtitleAlternateFileName);
                Configuration.Settings.Save();
                UpdateRecentFilesUI();
            }

            buttonUnBreak.Visible = true;
            buttonAutoBreak.Visible = true;
            textBoxListViewTextAlternate.Visible = false;
            labelAlternateText.Visible = false;
            labelAlternateCharactersPerSecond.Visible = false;
            labelTextAlternateLineLengths.Visible = false;
            labelAlternateSingleLine.Visible = false;
            labelTextAlternateLineTotal.Visible = false;
            textBoxListViewText.Width = (groupBoxEdit.Width - (textBoxListViewText.Left + 8 + buttonUnBreak.Width));
            textBoxListViewText.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            labelCharactersPerSecond.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelCharactersPerSecond.Width);
            labelTextLineTotal.Left = textBoxListViewText.Left + (textBoxListViewText.Width - labelTextLineTotal.Width);

            SetTitle();
        }

        private void ToolStripMenuItemSpellCheckMainDropDownOpening(object sender, EventArgs e)
        {
            if (Configuration.Settings.General.SpellChecker.ToLower().Contains("word"))
            {
                toolStripSeparator9.Visible = false;
                GetDictionariesToolStripMenuItem.Visible = false;
                addWordToNamesetcListToolStripMenuItem.Visible = false;
            }
            else
            {
                toolStripSeparator9.Visible = true;
                GetDictionariesToolStripMenuItem.Visible = true;
                addWordToNamesetcListToolStripMenuItem.Visible = true;
            }
        }

        private void ToolStripMenuItemPlayRateSlowClick(object sender, EventArgs e)
        {
            if (mediaPlayer.VideoPlayer != null)
            {
                toolStripMenuItemPlayRateSlow.Checked = true;
                toolStripMenuItemPlayRateNormal.Checked = false;
                toolStripMenuItemPlayRateFast.Checked = false;
                toolStripMenuItemPlayRateVeryFast.Checked = false;
                mediaPlayer.VideoPlayer.PlayRate = 0.8;
                toolStripSplitButtonPlayRate.Image = imageListPlayRate.Images[1];
            }
        }

        private void ToolStripMenuItemPlayRateNormalClick(object sender, EventArgs e)
        {
            if (mediaPlayer.VideoPlayer != null)
            {
                toolStripMenuItemPlayRateSlow.Checked = false;
                toolStripMenuItemPlayRateNormal.Checked = true;
                toolStripMenuItemPlayRateFast.Checked = false;
                toolStripMenuItemPlayRateVeryFast.Checked = false;
                mediaPlayer.VideoPlayer.PlayRate = 1.0;
                toolStripSplitButtonPlayRate.Image = imageListPlayRate.Images[0];
            }
        }

        private void ToolStripMenuItemPlayRateFastClick(object sender, EventArgs e)
        {
            if (mediaPlayer.VideoPlayer != null)
            {
                toolStripMenuItemPlayRateSlow.Checked = false;
                toolStripMenuItemPlayRateNormal.Checked = false;
                toolStripMenuItemPlayRateFast.Checked = true;
                toolStripMenuItemPlayRateVeryFast.Checked = false;
                mediaPlayer.VideoPlayer.PlayRate = 1.2;
                toolStripSplitButtonPlayRate.Image = imageListPlayRate.Images[1];
            }
        }

        private void VeryFastToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (mediaPlayer.VideoPlayer != null)
            {
                toolStripMenuItemPlayRateSlow.Checked = false;
                toolStripMenuItemPlayRateNormal.Checked = false;
                toolStripMenuItemPlayRateFast.Checked = false;
                toolStripMenuItemPlayRateVeryFast.Checked = true;
                mediaPlayer.VideoPlayer.PlayRate = 1.6;
                toolStripSplitButtonPlayRate.Image = imageListPlayRate.Images[1];
            }
        }

        private void SplitContainer1SplitterMoved(object sender, SplitterEventArgs e)
        {
            Main_Resize(null, null);
        }

        private void ButtonSplitLineClick(object sender, EventArgs e)
        {
            SplitSelectedParagraph(null, null);
        }

        private void ToolStripMenuItemCopySourceTextClick(object sender, EventArgs e)
        {
            Subtitle selectedLines = new Subtitle(_subtitle);
            selectedLines.Paragraphs.Clear();
            foreach (int index in SubtitleListview1.SelectedIndices)
                selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
            Clipboard.SetText(selectedLines.ToText(GetCurrentSubtitleFormat()));
        }

        public void PlayPause()
        {
            mediaPlayer.TogglePlayPause();
        }

        public void SetCurrentViaEndPositionAndGotoNext(int index)
        {
            Paragraph p = _subtitle.GetParagraphOrDefault(index);
            if (p == null)
                return;

            if (mediaPlayer.VideoPlayer == null || string.IsNullOrEmpty(_videoFileName))
            {
                MessageBox.Show(Configuration.Settings.Language.General.NoVideoLoaded);
                return;
            }

            //if (autoDuration)
            //{
            //    //TODO: auto duration
            //    //TODO: search for start via wave file (must only be minor adjustment)
            //}

            // current movie pos
            double durationTotalMilliseconds = p.Duration.TotalMilliseconds;
            double totalMillisecondsEnd = mediaPlayer.CurrentPosition * 1000.0;

            var tc = new TimeCode(TimeSpan.FromMilliseconds(totalMillisecondsEnd - durationTotalMilliseconds));
            MakeHistoryForUndo(_language.BeforeSetEndAndVideoPosition + "  " + tc.ToString());
            _makeHistoryPaused = true;

            p.StartTime.TotalMilliseconds = totalMillisecondsEnd - durationTotalMilliseconds;
            p.EndTime.TotalMilliseconds = totalMillisecondsEnd;

            timeUpDownStartTime.TimeCode = p.StartTime;
            var durationInSeconds = (decimal)(p.Duration.TotalSeconds);
            if (durationInSeconds >= numericUpDownDuration.Minimum && durationInSeconds <= numericUpDownDuration.Maximum)
                SetDurationInSeconds((double)durationInSeconds);

            SubtitleListview1.SelectIndexAndEnsureVisible(index+1);
            ShowStatus(string.Format(_language.VideoControls.AdjustedViaEndTime, p.StartTime.ToShortString()));
            audioVisualizer.Invalidate();
            _makeHistoryPaused = false;
        }

        public void SetCurrentStartAutoDurationAndGotoNext(int index)
        {
            Paragraph prev = _subtitle.GetParagraphOrDefault(index-1);
            Paragraph p = _subtitle.GetParagraphOrDefault(index);
            if (p == null)
                return;

            if (mediaPlayer.VideoPlayer == null || string.IsNullOrEmpty(_videoFileName))
            {
                MessageBox.Show(Configuration.Settings.Language.General.NoVideoLoaded);
                return;
            }

            MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + p.Number + " " + p.Text));

            timeUpDownStartTime.MaskedTextBox.TextChanged -= MaskedTextBoxTextChanged;
            var oldParagraph = new Paragraph(_subtitle.Paragraphs[index]);
            double videoPosition = mediaPlayer.CurrentPosition;

            timeUpDownStartTime.TimeCode = new TimeCode(TimeSpan.FromSeconds(videoPosition));

            double duration = Utilities.GetDisplayMillisecondsFromText(textBoxListViewText.Text) * 1.4;

            _subtitle.Paragraphs[index].StartTime.TotalMilliseconds = TimeSpan.FromSeconds(videoPosition).TotalMilliseconds;
            if (prev != null && prev.EndTime.TotalMilliseconds > _subtitle.Paragraphs[index].StartTime.TotalMilliseconds)
            {
                int minDiff = Configuration.Settings.General.MininumMillisecondsBetweenLines + 1;
                if (minDiff < 1)
                    minDiff = 1;
                prev.EndTime.TotalMilliseconds = _subtitle.Paragraphs[index].StartTime.TotalMilliseconds - minDiff;
            }
            _subtitle.Paragraphs[index].EndTime.TotalMilliseconds = _subtitle.Paragraphs[index].StartTime.TotalMilliseconds + duration;
            SubtitleListview1.SetStartTime(index, _subtitle.Paragraphs[index]);
            SubtitleListview1.SetDuration(index, _subtitle.Paragraphs[index]);
            timeUpDownStartTime.TimeCode = _subtitle.Paragraphs[index].StartTime;
            timeUpDownStartTime.MaskedTextBox.TextChanged += MaskedTextBoxTextChanged;
            UpdateOriginalTimeCodes(oldParagraph);
            _subtitleListViewIndex = -1;
            SubtitleListview1.SelectIndexAndEnsureVisible(index + 1);
            audioVisualizer.Invalidate();
        }

        public void SetCurrentEndNextStartAndGoToNext(int index)
        {
            Paragraph p = _subtitle.GetParagraphOrDefault(index);
            Paragraph next = _subtitle.GetParagraphOrDefault(index + 1);
            if (p == null)
                return;

            if (mediaPlayer.VideoPlayer == null || string.IsNullOrEmpty(_videoFileName))
            {
                MessageBox.Show(Configuration.Settings.Language.General.NoVideoLoaded);
                return;
            }

            MakeHistoryForUndoOnlyIfNotResent(string.Format(_language.VideoControls.BeforeChangingTimeInWaveFormX, "#" + p.Number + " " + p.Text));


            double videoPosition = mediaPlayer.CurrentPosition;

            p.EndTime = new TimeCode(TimeSpan.FromSeconds(videoPosition));
            if (p.Duration.TotalSeconds < 0 || p.Duration.TotalSeconds > 10)
                p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds + Utilities.GetDisplayMillisecondsFromText(p.Text);

            SubtitleListview1.SetStartTime(index, p);
            SubtitleListview1.SetDuration(index, p);

            SetDurationInSeconds(_subtitle.Paragraphs[index].Duration.TotalSeconds + 0.001);
            if (next != null)
            {
                var oldDuration = next.Duration.TotalMilliseconds;
                next.StartTime.TotalMilliseconds = p.EndTime.TotalMilliseconds + 1;
                next.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds + oldDuration;
                SubtitleListview1.SelectIndexAndEnsureVisible(index + 1);
            }
            audioVisualizer.Invalidate();
        }

        private void EditSelectAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            for (int i = 0; i < SubtitleListview1.Items.Count; i++)
                SubtitleListview1.Items[i].Selected = true;
        }

        private void ToolStripMenuItemSplitTextAtCursorClick(object sender, EventArgs e)
        {
            TextBox tb =textBoxListViewText;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;

            int? pos = null;
            if (tb.SelectionStart > 2 && tb.SelectionStart < tb.Text.Length - 2)
                pos = tb.SelectionStart;
            SplitSelectedParagraph(null, pos);
            tb.Focus();
            tb.SelectionStart = tb.Text.Length;
        }

        private void ContextMenuStripTextBoxListViewOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox tb = textBoxListViewText;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            toolStripMenuItemSplitTextAtCursor.Visible = tb.Text.Length > 5 && tb.SelectionStart > 2 && tb.SelectionStart < tb.Text.Length - 2;

            if (IsUnicode)
            {
                if (toolStripMenuItemInsertUnicodeSymbol.DropDownItems.Count == 0)
                {
                    foreach (string s in Configuration.Settings.Tools.UnicodeSymbolsToInsert.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        toolStripMenuItemInsertUnicodeSymbol.DropDownItems.Add(s, null, InsertUnicodeGlyph);
                        if (Environment.OSVersion.Version.Major < 6 && Configuration.Settings.General.SubtitleFontName == Utilities.WinXp2kUnicodeFontName) // 6 == Vista/Win2008Server/Win7
                            toolStripMenuItemInsertUnicodeSymbol.DropDownItems[toolStripMenuItemInsertUnicodeSymbol.DropDownItems.Count - 1].Font = new Font(Utilities.WinXp2kUnicodeFontName, toolStripMenuItemInsertUnicodeSymbol.Font.Size);
                    }
                }
                toolStripMenuItemInsertUnicodeSymbol.Visible = toolStripMenuItemInsertUnicodeSymbol.DropDownItems.Count > 0;
                toolStripSeparator26.Visible = toolStripMenuItemInsertUnicodeSymbol.DropDownItems.Count > 0;

                superscriptToolStripMenuItem.Visible = tb.SelectionLength > 0;
                subscriptToolStripMenuItem.Visible = tb.SelectionLength > 0;
            }
            else
            {
                toolStripMenuItemInsertUnicodeSymbol.Visible = false;
                toolStripSeparator26.Visible = false;
                superscriptToolStripMenuItem.Visible = false;
                subscriptToolStripMenuItem.Visible = false;
            }
        }

        private void ToolStripMenuItemExportPngXmlClick(object sender, EventArgs e)
        {
            var exportBdnXmlPng = new ExportPngXml();
            exportBdnXmlPng.Initialize(_subtitle, GetCurrentSubtitleFormat(), "BDNXML", _fileName, _videoInfo);
            exportBdnXmlPng.ShowDialog(this);
        }

        private void TabControlSubtitleSelecting(object sender, TabControlCancelEventArgs e)
        {
            if (tabControlSubtitle.SelectedIndex != TabControlSourceView && textBoxSource.Text.Trim().Length > 1)
            {
                var temp = new Subtitle(_subtitle);
                SubtitleFormat format = temp.ReloadLoadSubtitle(new List<string>(textBoxSource.Lines), null);
                if (format == null)
                    e.Cancel = true;
            }
        }

        private void ToolStripComboBoxFrameRateTextChanged(object sender, EventArgs e)
        {
            Configuration.Settings.General.CurrentFrameRate = CurrentFrameRate;
        }

        private void ToolStripMenuItemGoogleMicrosoftTranslateSelLineClick(object sender, EventArgs e)
        {
            int firstSelectedIndex = FirstSelectedIndex;
            if (firstSelectedIndex >= 0)
            {
                Paragraph p = _subtitle.GetParagraphOrDefault(firstSelectedIndex);
                if (p != null)
                {
                    string defaultFromLanguage = Utilities.AutoDetectGoogleLanguage(_subtitle);
                    string defaultToLanguage = defaultFromLanguage;
                    if (_subtitleAlternate != null)
                     {
                         Paragraph o = Utilities.GetOriginalParagraph(firstSelectedIndex, p, _subtitleAlternate.Paragraphs);
                         if (o != null)
                         {
                             p = o;
                             defaultFromLanguage = Utilities.AutoDetectGoogleLanguage(_subtitleAlternate);
                         }
                     }
                     Cursor = Cursors.WaitCursor;
                     if (_googleOrMicrosoftTranslate == null || _googleOrMicrosoftTranslate.IsDisposed)
                     {
                         _googleOrMicrosoftTranslate = new GoogleOrMicrosoftTranslate();
                         _googleOrMicrosoftTranslate.InitializeFromLanguage(defaultFromLanguage, defaultToLanguage);
                     }
                     _googleOrMicrosoftTranslate.Initialize(p);
                    Cursor = Cursors.Default;
                    if (_googleOrMicrosoftTranslate.ShowDialog() == DialogResult.OK)
                    {
                        textBoxListViewText.Text = _googleOrMicrosoftTranslate.TranslatedText;
                    }
                }
            }
        }

        private void NumericUpDownSec1ValueChanged(object sender, EventArgs e)
        {
            Configuration.Settings.General.SmallDelayMilliseconds = (int)(numericUpDownSec1.Value * 1000);
        }

        private void NumericUpDownSec2ValueChanged(object sender, EventArgs e)
        {
            Configuration.Settings.General.LargeDelayMilliseconds = (int)(numericUpDownSec2.Value * 1000);
        }

        private void NumericUpDownSecAdjust1ValueChanged(object sender, EventArgs e)
        {
            Configuration.Settings.General.SmallDelayMilliseconds = (int)(numericUpDownSecAdjust1.Value * 1000);
        }

        private void NumericUpDownSecAdjust2ValueChanged(object sender, EventArgs e)
        {
            Configuration.Settings.General.LargeDelayMilliseconds = (int)(numericUpDownSecAdjust2.Value * 1000);
        }

        private void ToolStripMenuItemMakeEmptyFromCurrentClick(object sender, EventArgs e)
        {
            if (ContinueNewOrExit())
            {
                _subtitleAlternate = new Subtitle(_subtitle);
                _subtitleAlternateFileName = null;
                int oldIndex = FirstSelectedIndex;
                if (oldIndex < 0)
                    oldIndex = 0;

                foreach (Paragraph p in _subtitle.Paragraphs)
                {
                    if (Configuration.Settings.General.RemoveBlankLinesWhenOpening && string.IsNullOrEmpty(Configuration.Settings.Tools.NewEmptyTranslationText))
                        p.Text = "-";
                    else if (Configuration.Settings.Tools.NewEmptyTranslationText != null)
                        p.Text = Configuration.Settings.Tools.NewEmptyTranslationText;
                    else
                        p.Text = string.Empty;
                }
                SubtitleListview1.ShowAlternateTextColumn(Configuration.Settings.Language.General.OriginalText);
                _subtitleListViewIndex = -1;
                SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                SubtitleListview1.SelectIndexAndEnsureVisible(oldIndex);
                textBoxListViewText.Focus();
                Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable = true;

                _subtitleAlternateFileName = _fileName;
                _fileName = null;
                SetupAlternateEdit();
            }
        }

        private void ToolStripMenuItemShowOriginalInPreviewClick(object sender, EventArgs e)
        {
            toolStripMenuItemShowOriginalInPreview.Checked = !toolStripMenuItemShowOriginalInPreview.Checked;
            Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable = toolStripMenuItemShowOriginalInPreview.Checked;
        }

        private void ToolStripMenuItemVideoDropDownClosed(object sender, EventArgs e)
        {
            redockVideoControlsToolStripMenuItem.ShortcutKeys = Keys.None;
            undockVideoControlsToolStripMenuItem.ShortcutKeys = Keys.None;

        }

        private void ToolsToolStripMenuItemDropDownOpening(object sender, EventArgs e)
        {
            if (_subtitle != null && _subtitle.Paragraphs.Count > 0 && _networkSession == null)
            {
                toolStripSeparator23.Visible = true;
                toolStripMenuItemMakeEmptyFromCurrent.Visible = _subtitle != null && _subtitle.Paragraphs.Count > 0 && !SubtitleListview1.IsAlternateTextColumnVisible;
                toolStripMenuItemShowOriginalInPreview.Checked = Configuration.Settings.General.ShowOriginalAsPreviewIfAvailable;
            }
            else
            {
                toolStripSeparator23.Visible = false;
                toolStripMenuItemMakeEmptyFromCurrent.Visible = false;
                toolStripMenuItemShowOriginalInPreview.Checked = false;
            }
            toolStripMenuItemShowOriginalInPreview.Visible = SubtitleListview1.IsAlternateTextColumnVisible;
        }

        private void ContextMenuStripWaveFormOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (audioVisualizer.IsSpectrogramAvailable)
            {
                if (audioVisualizer.ShowSpectrogram && audioVisualizer.ShowWaveform)
                {
                    showWaveformAndSpectrogramToolStripMenuItem.Visible = false;
                    showOnlyWaveformToolStripMenuItem.Visible = true;
                    showOnlySpectrogramToolStripMenuItem.Visible = true;
                }
                else if (audioVisualizer.ShowSpectrogram)
                {
                    showWaveformAndSpectrogramToolStripMenuItem.Visible = true;
                    showOnlyWaveformToolStripMenuItem.Visible = true;
                    showOnlySpectrogramToolStripMenuItem.Visible = false;
                }
                else
                {
                    showWaveformAndSpectrogramToolStripMenuItem.Visible = true;
                    showOnlyWaveformToolStripMenuItem.Visible = false;
                    showOnlySpectrogramToolStripMenuItem.Visible = true;
                }
            }
            else
            {
                toolStripSeparator24.Visible = false;
                showWaveformAndSpectrogramToolStripMenuItem.Visible = false;
                showOnlyWaveformToolStripMenuItem.Visible = false;
                showOnlySpectrogramToolStripMenuItem.Visible = false;
            }
        }

        private void ShowWaveformAndSpectrogramToolStripMenuItemClick(object sender, EventArgs e)
        {
            audioVisualizer.ShowSpectrogram = true;
            audioVisualizer.ShowWaveform = true;
        }

        private void ShowOnlyWaveformToolStripMenuItemClick(object sender, EventArgs e)
        {
            audioVisualizer.ShowSpectrogram = false;
            audioVisualizer.ShowWaveform = true;
        }

        private void ShowOnlySpectrogramToolStripMenuItemClick(object sender, EventArgs e)
        {
            audioVisualizer.ShowSpectrogram = true;
            audioVisualizer.ShowWaveform = false;
        }

        private void SplitContainerMainSplitterMoved(object sender, SplitterEventArgs e)
        {
            mediaPlayer.Refresh();
        }

        private void FindDoubleLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            for (int i = FirstSelectedIndex+1; i < _subtitle.Paragraphs.Count; i++)
            {
                var current = _subtitle.GetParagraphOrDefault(i);
                var next = _subtitle.GetParagraphOrDefault(i+1);
                if (current != null && next != null)
                {
                    if (current.Text.Trim().ToLower() == next.Text.Trim().ToLower())
                    {
                        SubtitleListview1.SelectIndexAndEnsureVisible(i);
                        SubtitleListview1.Items[i + 1].Selected = true;
                        break;
                    }
                }
            }
        }

        private void TextBoxListViewTextAlternateMouseMove(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control && MouseButtons == MouseButtons.Left)
            {
                if (!string.IsNullOrEmpty(textBoxListViewTextAlternate.SelectedText))
                    textBoxListViewTextAlternate.DoDragDrop(textBoxListViewTextAlternate.SelectedText, DragDropEffects.Copy);
                else
                    textBoxListViewTextAlternate.DoDragDrop(textBoxListViewTextAlternate.Text, DragDropEffects.Copy);
            }
        }

        private void EBustlToolStripMenuItemClick(object sender, EventArgs e)
        {
            var ebu = new Ebu();
            saveFileDialog1.Filter =  ebu.Name + "|*" + ebu.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + ebu.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == ebu.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += ebu.Extension;
                }
                ebu.Save(fileName, _subtitle);
            }
        }

        private void ToolStripMenuItemCavena890Click(object sender, EventArgs e)
        {
            var cavena890 = new Cavena890();
            saveFileDialog1.Filter = cavena890.Name + "|*" + cavena890.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + cavena890.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == cavena890.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += cavena890.Extension;
                }
                cavena890.Save(fileName, _subtitle);
            }
        }

        private void PAcScreenElectronicsToolStripMenuItemClick(object sender, EventArgs e)
        {
            var pac = new Pac();
            saveFileDialog1.Filter = pac.Name + "|*" + pac.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + pac.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == pac.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += pac.Extension;
                }
                pac.Save(fileName, _subtitle);
            }
        }

        private void TextBoxListViewTextEnter(object sender, EventArgs e)
        {
            if (_findHelper != null)
                _findHelper.MatchInOriginal = false;
        }

        private void TextBoxListViewTextAlternateEnter(object sender, EventArgs e)
        {
            if (_findHelper != null)
                _findHelper.MatchInOriginal = true;
        }

        private void PlainTextToolStripMenuItemClick(object sender, EventArgs e)
        {
            var exportText = new ExportText();
            exportText.Initialize(_subtitle, _fileName);
            if (exportText.ShowDialog() == DialogResult.OK)
            {
                ShowStatus(Configuration.Settings.Language.Main.SubtitleExported);
            }
        }

        private void BluraySupToolStripMenuItemClick(object sender, EventArgs e)
        {
            var exportBdnXmlPng = new ExportPngXml();
            exportBdnXmlPng.Initialize(_subtitle, GetCurrentSubtitleFormat(), "BLURAYSUP", _fileName, _videoInfo);
            exportBdnXmlPng.ShowDialog(this);
        }

        private void VobSubsubidxToolStripMenuItemClick(object sender, EventArgs e)
        {
            var exportBdnXmlPng = new ExportPngXml();
            exportBdnXmlPng.Initialize(_subtitle, GetCurrentSubtitleFormat(), "VOBSUB", _fileName, _videoInfo);
            exportBdnXmlPng.ShowDialog(this);
        }

        private void TextBoxListViewTextAlternateKeyUp(object sender, KeyEventArgs e)
        {
            textBoxListViewTextAlternate.ClearUndo();
            UpdatePositionAndTotalLength(labelTextAlternateLineTotal, textBoxListViewTextAlternate);
        }

        private void TimerTextUndoTick(object sender, EventArgs e)
        {
            int index = _listViewTextUndoIndex;
            if (_listViewTextTicks == -1 || !this.CanFocus | _subtitle == null || _subtitle.Paragraphs.Count == 0 || index < 0 || index >= _subtitle.Paragraphs.Count)
                return;

            if ((DateTime.Now.Ticks - _listViewTextTicks) > 10000 * 700) // only if last typed char was entered > 700 milliseconds
            {
                string newText = _subtitle.Paragraphs[index].Text.TrimEnd();
                string oldText = _listViewTextUndoLast;
                if (oldText == null)
                    return;

                if (_listViewTextUndoLast != newText)
                {
                    MakeHistoryForUndo(Configuration.Settings.Language.General.Text + ": " + _listViewTextUndoLast.TrimEnd() + " -> " + newText, false);
                    _subtitle.HistoryItems[_subtitle.HistoryItems.Count - 1].Subtitle.Paragraphs[index].Text = _listViewTextUndoLast;

                    _listViewTextUndoLast = newText;
                    _listViewTextUndoIndex = -1;
                }
            }
        }

        private void TimerAlternateTextUndoTick(object sender, EventArgs e)
        {
            if (Configuration.Settings.General.AllowEditOfOriginalSubtitle && _subtitleAlternate != null && _subtitleAlternate.Paragraphs.Count > 0)
            {
                int index = _listViewTextUndoIndex;
                if (_listViewAlternateTextTicks == -1 || !this.CanFocus | _subtitleAlternate == null || _subtitleAlternate.Paragraphs.Count == 0 || index < 0 || index >= _subtitleAlternate.Paragraphs.Count)
                    return;

                if ((DateTime.Now.Ticks - _listViewAlternateTextTicks) > 10000 * 700) // only if last typed char was entered > 700 milliseconds
                {
                    var original = Utilities.GetOriginalParagraph(index, _subtitle.Paragraphs[index], _subtitleAlternate.Paragraphs);
                    if (original != null)
                        index = _subtitleAlternate.Paragraphs.IndexOf(original);
                    else
                        return;

                    string newText = _subtitleAlternate.Paragraphs[index].Text.TrimEnd();
                    string oldText = _listViewAlternateTextUndoLast;
                    if (oldText == null)
                        return;

                    if (_listViewAlternateTextUndoLast != newText)
                    {
                        MakeHistoryForUndo(Configuration.Settings.Language.General.Text + ": " + _listViewAlternateTextUndoLast.TrimEnd() + " -> " + newText, false);
                        _subtitle.HistoryItems[_subtitle.HistoryItems.Count - 1].OriginalSubtitle.Paragraphs[index].Text = _listViewAlternateTextUndoLast;

                        _listViewAlternateTextUndoLast = newText;
                        _listViewTextUndoIndex = -1;
                    }
                }
            }
        }

        private void UpdatePositionAndTotalLength(Label lineTotal, TextBox textBox)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                lineTotal.Text = string.Empty;
                return;
            }

            int extraNewLineLength = Environment.NewLine.Length - 1;
            int lineBreakPos = textBox.Text.IndexOf(Environment.NewLine);
            int pos = textBox.SelectionStart;
            int totalLength = Utilities.RemoveHtmlTags(textBox.Text).Replace(Environment.NewLine, string.Empty).Length; // we don't count new line in total length... correct?
            string totalL = "     " + string.Format(_languageGeneral.TotalLengthX, totalLength);
            if (lineBreakPos == -1 || pos <= lineBreakPos)
            {
                lineTotal.Text = "1," + (pos+1) + totalL;
                lineTotal.Left = textBox.Left + (textBox.Width - lineTotal.Width);
                return;
            }
            int secondLineBreakPos = textBox.Text.IndexOf(Environment.NewLine, lineBreakPos + 1);
            if (secondLineBreakPos == -1 || pos <= secondLineBreakPos + extraNewLineLength)
            {
                lineTotal.Text = "2," + (pos - (lineBreakPos + extraNewLineLength)) + totalL;
                lineTotal.Left = textBox.Left + (textBox.Width - lineTotal.Width);
                return;
            }
            int thirdLineBreakPos = textBox.Text.IndexOf(Environment.NewLine, secondLineBreakPos + 1);
            if (thirdLineBreakPos == -1 || pos < thirdLineBreakPos + (extraNewLineLength * 2))
            {
                lineTotal.Text = "3," + (pos - (secondLineBreakPos + extraNewLineLength)) + totalL;
                lineTotal.Left = textBox.Left + (textBox.Width - lineTotal.Width);
                return;
            }
            int forthLineBreakPos = textBox.Text.IndexOf(Environment.NewLine, thirdLineBreakPos + 1);
            if (forthLineBreakPos == -1 || pos < forthLineBreakPos + (extraNewLineLength * 3))
            {
                lineTotal.Text = "4," + (pos - (thirdLineBreakPos + extraNewLineLength)) + totalL;
                lineTotal.Left = textBox.Left + (textBox.Width - lineTotal.Width);
                return;
            }
            lineTotal.Text = string.Empty;
        }

        private void TextBoxListViewTextMouseClick(object sender, MouseEventArgs e)
        {
            UpdatePositionAndTotalLength(labelTextLineTotal, textBoxListViewText);
        }

        private void TextBoxListViewTextAlternateMouseClick(object sender, MouseEventArgs e)
        {
            UpdatePositionAndTotalLength(labelTextAlternateLineTotal, textBoxListViewTextAlternate);
        }

        private void TabControlButtonsDrawItem(object sender, DrawItemEventArgs e)
        {
            var tc = (TabControl)sender;
            var textBrush = new SolidBrush(ForeColor);
            var tabFont = new Font(tc.Font, FontStyle.Regular);
            if (e.State == DrawItemState.Selected)
            {
                tabFont = new Font(tc.Font, FontStyle.Bold);
                e.Graphics.FillRectangle(new SolidBrush(SystemColors.Window), e.Bounds);
            }
            Rectangle tabBounds = tc.GetTabRect(e.Index);
            var stringFlags = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tc.TabPages[e.Index].Text.Trim(), tabFont, textBrush, tabBounds, new StringFormat(stringFlags));
            //tc.DrawMode = TabDrawMode.Normal;
        }

        public void GotoNextSubPosFromvideoPos()
        {
            if (mediaPlayer.VideoPlayer != null && _subtitle != null)
            {
                double ms = mediaPlayer.VideoPlayer.CurrentPosition * 1000.0;
                foreach (Paragraph p in _subtitle.Paragraphs)
                {
                    if (p.EndTime.TotalMilliseconds > ms && p.StartTime.TotalMilliseconds < ms)
                    {
                        // currrent sub
                    }
                    else if (p.Duration.TotalSeconds < 10 && p.StartTime.TotalMilliseconds > ms)
                    {
                        mediaPlayer.VideoPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.GetIndex(p));
                        return;
                    }
                }
            }
        }

        public void GotoPrevSubPosFromvideoPos()
        {
            if (mediaPlayer.VideoPlayer != null && _subtitle != null)
            {
                double ms = mediaPlayer.VideoPlayer.CurrentPosition * 1000.0;
                int i = _subtitle.Paragraphs.Count-1;
                while (i>0)
                {
                    Paragraph p = _subtitle.Paragraphs[i];
                    if (p.EndTime.TotalMilliseconds > ms && p.StartTime.TotalMilliseconds < ms)
                    {
                        // currrent sub
                    }
                    else if (p.Duration.TotalSeconds < 10 && p.StartTime.TotalMilliseconds < ms)
                    {
                        mediaPlayer.VideoPlayer.CurrentPosition = p.StartTime.TotalSeconds;
                        SubtitleListview1.SelectIndexAndEnsureVisible(_subtitle.GetIndex(p));
                        return;
                    }
                    i--;
                }
            }
        }

        private void AdobeEncoreFabImageScriptToolStripMenuItemClick(object sender, EventArgs e)
        {
            var exportBdnXmlPng = new ExportPngXml();
            exportBdnXmlPng.Initialize(_subtitle, GetCurrentSubtitleFormat(), "FAB", _fileName, _videoInfo);
            exportBdnXmlPng.ShowDialog(this);
        }

        private void ToolStripMenuItemMergeDialogueClick(object sender, EventArgs e)
        {
            MergeDialogues();
        }

        private void MainKeyUp(object sender, KeyEventArgs e)
        {
            if (_mainCreateStartDownEndUpParagraph != null)
            {
                var p = _subtitle.Paragraphs[_subtitleListViewIndex];
                if (p.ToString() == _mainCreateStartDownEndUpParagraph.ToString())
                    ButtonSetEndClick(null, null);
                _mainCreateStartDownEndUpParagraph = null;
            }
            else if (_mainAdjustStartDownEndUpAndGoToNextParagraph != null)
            {
                var p = _subtitle.Paragraphs[_subtitleListViewIndex];
                if (p.ToString() == _mainAdjustStartDownEndUpAndGoToNextParagraph.ToString())
                {
                    double videoPositionInSeconds = mediaPlayer.CurrentPosition;
                    if (p.StartTime.TotalSeconds + 0.1 < videoPositionInSeconds)
                        ButtonSetEndClick(null, null);
                    SubtitleListview1.SelectIndexAndEnsureVisible(_subtitleListViewIndex+1);
                }
                _mainAdjustStartDownEndUpAndGoToNextParagraph = null;
            }
        }

        private void ToolStripMenuItemSurroundWithMusicSymbolsClick(object sender, EventArgs e)
        {
            const string tag = "♪";
            if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
            {
                SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                MakeHistoryForUndo(string.Format(_language.BeforeAddingTagX, tag));

                var indexes = new List<int>();
                foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                    indexes.Add(item.Index);

                SubtitleListview1.BeginUpdate();
                foreach (int i in indexes)
                {
                    if (_subtitleAlternate != null)
                    {
                        Paragraph original = Utilities.GetOriginalParagraph(i, _subtitle.Paragraphs[i], _subtitleAlternate.Paragraphs);
                        if (original != null)
                        {
                            if (original.Text.Contains(tag))
                            {
                                original.Text = original.Text.Replace(tag, string.Empty);
                            }
                            else
                            {
                                original.Text = string.Format("{0}{1}{0}", tag, original.Text.Replace(Environment.NewLine, "♪" + Environment.NewLine + "♪"));
                            }
                            SubtitleListview1.SetAlternateText(i, original.Text);
                        }
                    }

                    if (_subtitle.Paragraphs[i].Text.Contains(tag))
                    {
                        _subtitle.Paragraphs[i].Text = _subtitle.Paragraphs[i].Text.Replace("♪", string.Empty);
                    }
                    else
                    {
                        _subtitle.Paragraphs[i].Text = string.Format("{0}{1}{0}", tag, _subtitle.Paragraphs[i].Text.Replace(Environment.NewLine, "♪" + Environment.NewLine + "♪"));
                    }
                    SubtitleListview1.SetText(i, _subtitle.Paragraphs[i].Text);
                }
                SubtitleListview1.EndUpdate();

                ShowStatus(string.Format(_language.TagXAdded, tag));
                ShowSource();
                RefreshSelectedParagraph();
                SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
            }
        }

        private void SuperscriptToolStripMenuItemClick(object sender, EventArgs e)
        {
            TextBox tb;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            else
                tb = textBoxListViewText;

            string text = tb.SelectedText;
            int selectionStart = tb.SelectionStart;
            text = Utilities.ToSuperscript(text);
            tb.SelectedText = text;
            tb.SelectionStart = selectionStart;
            tb.SelectionLength = text.Length;
        }

        private void SubscriptToolStripMenuItemClick(object sender, EventArgs e)
        {
            TextBox tb;
            if (textBoxListViewTextAlternate.Focused)
                tb = textBoxListViewTextAlternate;
            else
                tb = textBoxListViewText;

            string text = tb.SelectedText;
            int selectionStart = tb.SelectionStart;
            text = Utilities.ToSubscript(text);
            tb.SelectedText = text;
            tb.SelectionStart = selectionStart;
            tb.SelectionLength = text.Length;
        }

        private void ToolStripMenuItemImagePerFrameClick(object sender, EventArgs e)
        {
            var exportBdnXmlPng = new ExportPngXml();
            exportBdnXmlPng.Initialize(_subtitle, GetCurrentSubtitleFormat(), "IMAGE/FRAME", _fileName, _videoInfo);
            exportBdnXmlPng.ShowDialog(this);
        }

        private void toolStripMenuItemApplyDisplayTimeLimits_Click(object sender, EventArgs e)
        {
            ApplyDisplayTimeLimits(false);
        }

        private void ApplyDisplayTimeLimits(bool onlySelectedLines)
        {
            if (IsSubtitleLoaded)
            {
                ReloadFromSourceView();
                var applyDurationLimits = new ApplyDurationLimits();
                _formPositionsAndSizes.SetPositionAndSize(applyDurationLimits);

                if (onlySelectedLines)
                {
                    var selectedLines = new Subtitle { WasLoadedWithFrameNumbers = _subtitle.WasLoadedWithFrameNumbers };
                    foreach (int index in SubtitleListview1.SelectedIndices)
                        selectedLines.Paragraphs.Add(_subtitle.Paragraphs[index]);
                    applyDurationLimits.Initialize(selectedLines);
                }
                else
                {
                    applyDurationLimits.Initialize(_subtitle);
                }

                applyDurationLimits.Initialize(_subtitle);
                if (applyDurationLimits.ShowDialog(this) == DialogResult.OK)
                {
                    MakeHistoryForUndo(_language.BeforeDisplayTimeAdjustment);

                    if (onlySelectedLines)
                    { // we only update selected lines
                        int i = 0;
                        foreach (int index in SubtitleListview1.SelectedIndices)
                        {
                            _subtitle.Paragraphs[index] = applyDurationLimits.FixedSubtitle.Paragraphs[i];
                            i++;
                        }
                        ShowStatus(_language.VisualSyncPerformedOnSelectedLines);
                    }
                    else
                    {
                        SaveSubtitleListviewIndexes();
                        _subtitle.Paragraphs.Clear();
                        foreach (Paragraph p in applyDurationLimits.FixedSubtitle.Paragraphs)
                            _subtitle.Paragraphs.Add(new Paragraph(p));
                        SubtitleListview1.Fill(_subtitle, _subtitleAlternate );
                        RestoreSubtitleListviewIndexes();
                    }

                    if (IsFramesRelevant && CurrentFrameRate > 0)
                        _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    ShowSource();
                }
                _formPositionsAndSizes.SavePositionAndSize(applyDurationLimits);
            }
            else
            {
                MessageBox.Show(_language.NoSubtitleLoaded, Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void generateDatetimeInfoFromVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            var extractDateTimeInfo = new ExtractDateTimeInfo();
            _formPositionsAndSizes.SetPositionAndSize(extractDateTimeInfo);

            if (extractDateTimeInfo.ShowDialog(this) == DialogResult.OK)
            {
                if (ContinueNewOrExit())
                {
                    MakeHistoryForUndo(_language.BeforeDisplayTimeAdjustment);

                    ResetSubtitle();
                    _subtitle.Paragraphs.Clear();
                    foreach (Paragraph p in extractDateTimeInfo.DateTimeSubtitle.Paragraphs)
                        _subtitle.Paragraphs.Add(new Paragraph(p));
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    if (IsFramesRelevant && CurrentFrameRate > 0)
                        _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    ShowSource();

                    OpenVideo(extractDateTimeInfo.VideoFileName);
                }
            }
            _formPositionsAndSizes.SavePositionAndSize(extractDateTimeInfo);
        }

        private void ToolStripMenuItemRightToLeftModeClick(object sender, EventArgs e)
        {
            toolStripMenuItemRightToLeftMode.Checked = !toolStripMenuItemRightToLeftMode.Checked;
            if (textBoxListViewText.RightToLeft == System.Windows.Forms.RightToLeft.Yes)
            {
                textBoxListViewText.RightToLeft = System.Windows.Forms.RightToLeft.No;
                SubtitleListview1.RightToLeft = System.Windows.Forms.RightToLeft.No;
                textBoxSource.RightToLeft = System.Windows.Forms.RightToLeft.No;
                if (mediaPlayer != null)
                    mediaPlayer.TextRightToLeft = System.Windows.Forms.RightToLeft.No;
            }
            else
            {
                textBoxListViewText.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
                SubtitleListview1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
                textBoxSource.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
                if (mediaPlayer != null)
                    mediaPlayer.TextRightToLeft = System.Windows.Forms.RightToLeft.Yes;
            }
        }

        private void joinSubtitlesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadFromSourceView();
            var joinSubtitles = new JoinSubtitles();
            _formPositionsAndSizes.SetPositionAndSize(joinSubtitles);
            if (joinSubtitles.ShowDialog(this) == DialogResult.OK)
            {
                if (joinSubtitles.JoinedSubtitle != null && joinSubtitles.JoinedSubtitle.Paragraphs.Count > 0 && ContinueNewOrExit())
                {
                    MakeHistoryForUndo(_language.BeforeDisplayTimeAdjustment);//TODO: add language tags

                    ResetSubtitle();
                    _subtitle = joinSubtitles.JoinedSubtitle;
                    SubtitleListview1.Fill(_subtitle, _subtitleAlternate);
                    SubtitleListview1.SelectIndexAndEnsureVisible(0);

                    if (IsFramesRelevant && CurrentFrameRate > 0)
                        _subtitle.CalculateFrameNumbersFromTimeCodesNoCheck(CurrentFrameRate);
                    ShowSource();

                    ShowStatus(_language.SubtitleSplitted); //TODO: add language tags
                }
            }
            _formPositionsAndSizes.SavePositionAndSize(joinSubtitles);
        }

        private void toolStripMenuItemReverseRightToLeftStartEnd_Click(object sender, EventArgs e)
        {
            ReverseStartAndEndingForRTL();
        }

        private void toolStripMenuItemExportCapMakerPlus_Click(object sender, EventArgs e)
        {
            var capMakerPlus = new CapMakerPlus();
            saveFileDialog1.Filter = capMakerPlus.Name + "|*" + capMakerPlus.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + capMakerPlus.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == capMakerPlus.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += capMakerPlus.Extension;
                }
                capMakerPlus.Save(fileName, _subtitle);
            }
        }

        private void toolStripMenuItemExportCheetahCap_Click(object sender, EventArgs e)
        {
            var cheetahCaption = new CheetahCaption();
            saveFileDialog1.Filter = cheetahCaption.Name + "|*" + cheetahCaption.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + cheetahCaption.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == cheetahCaption.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += cheetahCaption.Extension;
                }
                cheetahCaption.Save(fileName, _subtitle);
            }
        }

        private void toolStripMenuItemExportCaptionInc_Click(object sender, EventArgs e)
        {
            var captionInc = new CaptionsInc();
            saveFileDialog1.Filter = captionInc.Name + "|*" + captionInc.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + captionInc.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == captionInc.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += captionInc.Extension;
                }
                captionInc.Save(fileName, _subtitle);
            }
        }

        private void toolStripMenuItemExportUltech130_Click(object sender, EventArgs e)
        {
            var ultech130 = new Ultech130();
            saveFileDialog1.Filter = ultech130.Name + "|*" + ultech130.Extension;
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + ultech130.Extension;
            saveFileDialog1.AddExtension = true;

            if (!string.IsNullOrEmpty(_videoFileName))
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_videoFileName);
            else
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(_fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                saveFileDialog1.InitialDirectory = openFileDialog1.InitialDirectory;

            DialogResult result = saveFileDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = saveFileDialog1.InitialDirectory;
                string fileName = saveFileDialog1.FileName;
                string ext = Path.GetExtension(fileName).ToLower();
                bool extOk = ext == ultech130.Extension.ToLower();
                if (!extOk)
                {
                    if (fileName.EndsWith("."))
                        fileName = fileName.Substring(0, fileName.Length - 1);
                    fileName += ultech130.Extension;
                }
                ultech130.Save(fileName, _subtitle);
            }
        }

        private void toolStripMenuItemAssStyles_Click(object sender, EventArgs e)
        {
            var formatType = GetCurrentSubtitleFormat().GetType();
            if (formatType == typeof(AdvancedSubStationAlpha) || formatType == typeof(SubStationAlpha))
            {
                var styles = new SubStationAlphaStyles(_subtitle, GetCurrentSubtitleFormat());
                if (styles.ShowDialog(this) == DialogResult.OK)
                    _subtitle.Header = styles.Header;
            }
            else if (formatType == typeof(TimedText10))
            {
                var styles = new TimedTextStyles(_subtitle, GetCurrentSubtitleFormat());
                if (styles.ShowDialog(this) == DialogResult.OK)
                    _subtitle.Header = styles.Header;
            }
        }

        private void toolStripMenuItemSubStationAlpha_Click(object sender, EventArgs e)
        {
            var properties = new SubStationAlphaProperties(_subtitle, GetCurrentSubtitleFormat(), _videoFileName);
            _formPositionsAndSizes.SetPositionAndSize(properties, true);
            properties.ShowDialog(this);
            _formPositionsAndSizes.SavePositionAndSize(properties);
        }

        private void SetAlignTag(Paragraph p, string tag)
        {
            if (p.Text.StartsWith("{\\a") && p.Text.Length > 5 && p.Text[5] == '}')
                p.Text = p.Text.Remove(0, 6);
            else if (p.Text.StartsWith("{\\a") && p.Text.Length > 4 && p.Text[4] == '}')
                p.Text = p.Text.Remove(0, 5);
            p.Text = string.Format(@"{0}{1}", tag, p.Text);
        }

        private void toolStripMenuItemAlignment_Click(object sender, EventArgs e)
        {
            var f = new AlignmentPicker();
            f.TopMost = true;
            f.StartPosition = FormStartPosition.Manual;
            f.Left = System.Windows.Forms.Cursor.Position.X - 150;
            f.Top = System.Windows.Forms.Cursor.Position.Y - 75;
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                string tag = string.Empty;
                var format = GetCurrentSubtitleFormat();
                if (format.GetType() == typeof(SubStationAlpha))
                {
                    //1: Bottom left
                    //2: Bottom center
                    //3: Bottom right
                    //9: Middle left
                    //10: Middle center
                    //11: Middle right
                    //5: Top left
                    //6: Top center
                    //7: Top right
                    switch (f.Alignment)
                    {
                        case ContentAlignment.BottomLeft:
                            tag = "{\\a1}";
                            break;
                        case ContentAlignment.BottomCenter:
                            tag = "{\\a2}";
                            break;
                        case ContentAlignment.BottomRight:
                            tag = "{\\a3}";
                            break;
                        case ContentAlignment.MiddleLeft:
                            tag = "{\\a9}";
                            break;
                        case ContentAlignment.MiddleCenter:
                            tag = "{\\a10}";
                            break;
                        case ContentAlignment.MiddleRight:
                            tag = "{\\a11}";
                            break;
                        case ContentAlignment.TopLeft:
                            tag = "{\\a5}";
                            break;
                        case ContentAlignment.TopCenter:
                            tag = "{\\a6}";
                            break;
                        case ContentAlignment.TopRight:
                            tag = "{\\a7}";
                            break;
                    }
                }
                else
                {
                    //1: Bottom left
                    //2: Bottom center
                    //3: Bottom right
                    //4: Middle left
                    //5: Middle center
                    //6: Middle right
                    //7: Top left
                    //8: Top center
                    //9: Top right
                    switch (f.Alignment)
                    {
                        case ContentAlignment.BottomLeft:
                            tag = "{\\an1}";
                            break;
                        case ContentAlignment.BottomCenter:
                            if (format.GetType() == typeof(SubRip))
                                tag = string.Empty;
                            else
                                tag = "{\\an2}";
                            break;
                        case ContentAlignment.BottomRight:
                            tag = "{\\an3}";
                            break;
                        case ContentAlignment.MiddleLeft:
                            tag = "{\\an4}";
                            break;
                        case ContentAlignment.MiddleCenter:
                            tag = "{\\an5}";
                            break;
                        case ContentAlignment.MiddleRight:
                            tag = "{\\an6}";
                            break;
                        case ContentAlignment.TopLeft:
                            tag = "{\\an7}";
                            break;
                        case ContentAlignment.TopCenter:
                            tag = "{\\an8}";
                            break;
                        case ContentAlignment.TopRight:
                            tag = "{\\an9}";
                            break;
                    }
                }
                if (_subtitle.Paragraphs.Count > 0 && SubtitleListview1.SelectedItems.Count > 0)
                {
                    SubtitleListview1.SelectedIndexChanged -= SubtitleListview1_SelectedIndexChanged;
                    MakeHistoryForUndo(string.Format(_language.BeforeAddingTagX, tag));

                    var indexes = new List<int>();
                    foreach (ListViewItem item in SubtitleListview1.SelectedItems)
                        indexes.Add(item.Index);

                    SubtitleListview1.BeginUpdate();
                    foreach (int i in indexes)
                    {
                        if (_subtitleAlternate != null && Configuration.Settings.General.AllowEditOfOriginalSubtitle)
                        {
                            Paragraph original = Utilities.GetOriginalParagraph(i, _subtitle.Paragraphs[i], _subtitleAlternate.Paragraphs);
                            if (original != null)
                            {
                                SetAlignTag(original, tag);
                                SubtitleListview1.SetAlternateText(i, original.Text);
                            }
                        }
                        SetAlignTag(_subtitle.Paragraphs[i], tag);
                        SubtitleListview1.SetText(i, _subtitle.Paragraphs[i].Text);
                    }
                    SubtitleListview1.EndUpdate();

                    ShowStatus(string.Format(_language.TagXAdded, tag));
                    ShowSource();
                    RefreshSelectedParagraph();
                    SubtitleListview1.SelectedIndexChanged += SubtitleListview1_SelectedIndexChanged;
                }
            }
        }

        private void toolStripMenuItemRestoreAutoBackup_Click(object sender, EventArgs e)
        {
            _lastDoNotPrompt = string.Empty;
            var restoreAutoBackup = new RestoreAutoBackup();
            _formPositionsAndSizes.SetPositionAndSize(restoreAutoBackup);
            if (restoreAutoBackup.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(restoreAutoBackup.AutoBackupFileName))
            {
                if (ContinueNewOrExit())
                {
                    OpenSubtitle(restoreAutoBackup.AutoBackupFileName, null);
                    _converted = true;
                }
            }
            _formPositionsAndSizes.SavePositionAndSize(restoreAutoBackup);
        }

        private void labelStatus_Click(object sender, EventArgs e)
        {
            if (_statusLog.Length > 0)
            {
                var statusLog = new StatusLog(_statusLog.ToString());
                _formPositionsAndSizes.SetPositionAndSize(statusLog);
                statusLog.ShowDialog(this);
                _formPositionsAndSizes.SavePositionAndSize(statusLog);
            }
        }

        private void toolStripMenuItemStatistics_Click(object sender, EventArgs e)
        {
            Statistics stats = new Statistics(_subtitle, _fileName, GetCurrentSubtitleFormat());
            _formPositionsAndSizes.SetPositionAndSize(stats);
            stats.ShowDialog(this);
            _formPositionsAndSizes.SavePositionAndSize(stats);
        }

        private void toolStripMenuItemDCinemaProperties_Click(object sender, EventArgs e)
        {
            if (GetCurrentSubtitleFormat().GetType() == typeof(DCSubtitle))
            {
                var properties = new DCinemaPropertiesInterop(_subtitle, _videoFileName);
                _formPositionsAndSizes.SetPositionAndSize(properties, true);
                properties.ShowDialog(this);
                _formPositionsAndSizes.SavePositionAndSize(properties);
            }
            else
            {
                var properties = new DCinemaPropertiesSmpte(_subtitle, _videoFileName);
                _formPositionsAndSizes.SetPositionAndSize(properties, true);
                properties.ShowDialog(this);
                _formPositionsAndSizes.SavePositionAndSize(properties);
            }
        }

        private void toolStripMenuItemTextTimeCodePair_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                saveFileDialog1.Filter =  "Text files|*.txt";
                saveFileDialog1.Title = _language.SaveSubtitleAs;
                saveFileDialog1.DefaultExt = "*.txt";
                saveFileDialog1.AddExtension = true;

                string fname = saveFileDialog1.FileName;
                if (string.IsNullOrEmpty(fname))
                    fname = "ATS";
                if (!fname.EndsWith(".txt"))
                    fname += ".txt";
                string fileNameTimeCode = fname.Insert(fname.Length - 4, "_timecode");
                string fileNameText = fname.Insert(fname.Length - 4, "_text");

                var timeCodeLines = new StringBuilder();
                var textLines = new StringBuilder();

                foreach (Paragraph p in _subtitle.Paragraphs)
                {
                    timeCodeLines.AppendLine(string.Format("{0:00}:{1:00}:{2:00}:{3:00}", p.StartTime.Hours, p.StartTime.Minutes, p.StartTime.Seconds, SubtitleFormat.MillisecondsToFrames(p.StartTime.Milliseconds)));
                    timeCodeLines.AppendLine(string.Format("{0:00}:{1:00}:{2:00}:{3:00}", p.EndTime.Hours, p.EndTime.Minutes, p.EndTime.Seconds, SubtitleFormat.MillisecondsToFrames(p.EndTime.Milliseconds)));

                    textLines.AppendLine(Utilities.RemoveHtmlTags(p.Text).Replace(Environment.NewLine, "|"));
                    textLines.AppendLine();
                }

                File.WriteAllText(fileNameTimeCode, timeCodeLines.ToString(), Encoding.UTF8);
                File.WriteAllText(fileNameText, textLines.ToString(), Encoding.UTF8);
            }
        }

        private void textWordsPerMinutewpmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SortSubtitle(SubtitleSortCriteria.WordsPerMinute, (sender as ToolStripItem).Text);
        }

        private void toolStripMenuItemTTPropertiesClick(object sender, EventArgs e)
        {
            if (GetCurrentSubtitleFormat().GetType() == typeof(TimedText10))
            {
                var properties = new TimedTextProperties(_subtitle, _videoFileName);
                _formPositionsAndSizes.SetPositionAndSize(properties, true);
                properties.ShowDialog(this);
                _formPositionsAndSizes.SavePositionAndSize(properties);
            }
        }

        private void toolStripMenuItemSaveSelectedLines_Click(object sender, EventArgs e)
        {
            var newSub = new Subtitle(_subtitle);
            newSub.Paragraphs.Clear();
            foreach (int index in SubtitleListview1.SelectedIndices)
                newSub.Paragraphs.Add(_subtitle.Paragraphs[index]);

            SubtitleFormat currentFormat = GetCurrentSubtitleFormat();
            Utilities.SetSaveDialogFilter(saveFileDialog1, currentFormat);
            saveFileDialog1.Title = _language.SaveSubtitleAs;
            saveFileDialog1.DefaultExt = "*" + currentFormat.Extension;
            saveFileDialog1.AddExtension = true;
            if (!string.IsNullOrEmpty(_fileName))
                saveFileDialog1.InitialDirectory = Path.GetDirectoryName(_fileName);

            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                int index = 0;
                foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
                {
                    if (saveFileDialog1.FilterIndex == index + 1)
                    {
                        // only allow current extension or ".txt"
                        string fileName = saveFileDialog1.FileName;
                        string ext = Path.GetExtension(fileName).ToLower();
                        bool extOk = ext == format.Extension.ToLower() || format.AlternateExtensions.Contains(ext) || ext == ".txt";
                        if (!extOk)
                        {
                            if (fileName.EndsWith("."))
                                fileName = fileName.Substring(0, _fileName.Length - 1);
                            fileName += format.Extension;
                        }

                        string allText = newSub.ToText(format);
                        File.WriteAllText(fileName, allText, GetCurrentEncoding());
                        ShowStatus(string.Format("{0} lines saved as {1}", newSub.Paragraphs.Count, fileName));
                        return;
                    }
                    index++;
                }
            }
        }        

    }
}