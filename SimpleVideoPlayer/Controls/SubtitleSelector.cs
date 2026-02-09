using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleVideoPlayer.Controls
{
    public class SubtitleSelector : FlowLayoutPanel
    {
        #region 字段

        private Label _subtitleLabel;
        private ComboBox _subtitleComboBox;
        private Button _addSubtitleButton;
        private Button _removeSubtitleButton;
        private List<string> _subtitleFiles;
        private int _currentSubtitleIndex;

        #endregion

        #region 属性

        public List<string> SubtitleFiles
        {
            get => _subtitleFiles;
            private set => _subtitleFiles = value;
        }

        public int CurrentSubtitleIndex
        {
            get => _currentSubtitleIndex;
            set
            {
                if (_currentSubtitleIndex != value)
                {
                    _currentSubtitleIndex = value;
                    OnSubtitleSelected();
                }
            }
        }

        public string CurrentSubtitlePath
        {
            get
            {
                if (_currentSubtitleIndex >= 0 && _currentSubtitleIndex < _subtitleFiles.Count)
                {
                    return _subtitleFiles[_currentSubtitleIndex];
                }
                return null;
            }
        }

        #endregion

        #region 构造函数
        MediaPlayer _mediaPlayer;
        VideoPlayer rootui;
        public SubtitleSelector(MediaPlayer mediaPlayer,VideoPlayer rootui)
        {
            this.rootui = rootui;
            this._mediaPlayer = mediaPlayer;
            _subtitleFiles = new List<string>();
            _currentSubtitleIndex = -1;
            InitializeControl();
            InitializeControls();
        }

        #endregion

        #region 初始化方法

        private void InitializeControl()
        {
            FlowDirection = FlowDirection.LeftToRight;
            WrapContents = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Height = 60;
            BackColor = Color.FromArgb(245, 245, 245);
            Padding = new Padding(5, 0, 5, 0);
        }

        private void InitializeControls()
        {
            _subtitleLabel = new Label
            {
                Text = "字幕:",
                Width = 40,
                Height = 30,
                Font = new Font("Microsoft YaHei", 9),
                BackColor = Color.FromArgb(245, 245, 245),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 15, 5, 15)
            };

            _subtitleComboBox = new ComboBox
            {
                Width = 120,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9),
                Margin = new Padding(0, 15, 5, 15)
            };

            _addSubtitleButton = CreateButton("添加", 50, Color.FromArgb(76, 175, 80));
            _addSubtitleButton.Margin = new Padding(0, 15, 5, 15);

            _removeSubtitleButton = CreateButton("移除", 50, Color.FromArgb(220, 53, 69));
            _removeSubtitleButton.Margin = new Padding(0, 15, 0, 15);

            _subtitleComboBox.SelectedIndexChanged += OnSubtitleComboBoxSelectedIndexChanged;
            _addSubtitleButton.Click += OnAddSubtitleButtonClicked;
            _removeSubtitleButton.Click += OnRemoveSubtitleButtonClicked;

            Controls.Add(_subtitleLabel);
            Controls.Add(_subtitleComboBox);
            Controls.Add(_addSubtitleButton);
            Controls.Add(_removeSubtitleButton);
        }

        #endregion

        #region 公共方法

        public void AddSubtitleFile(string subtitlePath)
        {
            if (string.IsNullOrEmpty(subtitlePath) || !System.IO.File.Exists(subtitlePath))
            {
                return;
            }

            if (_subtitleFiles.Contains(subtitlePath))
            {
                return;
            }

            _subtitleFiles.Add(subtitlePath);
            UpdateComboBox();
        }

        public void RemoveSubtitleFile(int index)
        {
            if (index < 0 || index >= _subtitleFiles.Count)
            {
                return;
            }

            _subtitleFiles.RemoveAt(index);

            if (_currentSubtitleIndex == index)
            {
                _currentSubtitleIndex = -1;
            }
            else if (_currentSubtitleIndex > index)
            {
                _currentSubtitleIndex--;
            }

            UpdateComboBox();
            OnSubtitleSelected();
        }

        public void ClearSubtitles()
        {
            _subtitleFiles.Clear();
            _currentSubtitleIndex = -1;
            UpdateComboBox();
            OnSubtitleSelected();
        }

        #endregion

        #region 私有方法

        private void UpdateComboBox()
        {
            _subtitleComboBox.Items.Clear();

            foreach (var subtitleFile in _subtitleFiles)
            {
                var fileName = System.IO.Path.GetFileName(subtitleFile);
                _subtitleComboBox.Items.Add(fileName);
            }

            if (_currentSubtitleIndex >= 0 && _currentSubtitleIndex < _subtitleFiles.Count)
            {
                _subtitleComboBox.SelectedIndex = _currentSubtitleIndex;
            }
            else
            {
                _subtitleComboBox.SelectedIndex = -1;
            }
        }

        private Button CreateButton(string text, int width, Color backColor)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void OnSubtitleComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentSubtitleIndex = _subtitleComboBox.SelectedIndex;
        }

        private void OnAddSubtitleButtonClicked(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "字幕文件|*.srt;*.ass;*.ssa;*.vtt|所有文件|*.*",
                Title = "选择字幕文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AddSubtitleFile(openFileDialog.FileName);
            }
        }

        private void OnRemoveSubtitleButtonClicked(object sender, EventArgs e)
        {
            if (_subtitleComboBox.SelectedIndex >= 0)
            {
                RemoveSubtitleFile(_subtitleComboBox.SelectedIndex);
            }
        }

        private void OnSubtitleSelected()
        {
            if (_currentSubtitleIndex >= 0 && _currentSubtitleIndex < _subtitleFiles.Count)
            {
                SetMediaPlayerSubtitleFile(_subtitleFiles[_currentSubtitleIndex]);
            }
            else
            {
                ClearSubtitle();
            }
        }

        #endregion

        #region 字幕控制

        public void SetMediaPlayerSubtitleFile(string subtitlePath)
        {
            if (_mediaPlayer == null || string.IsNullOrEmpty(subtitlePath))
            {
                return;
            }

            var fileUri = new Uri(subtitlePath).AbsoluteUri;
            _mediaPlayer.AddSlave(MediaSlaveType.Subtitle, fileUri, true);
        }

        public void LoadSubtitleFile(string subtitlePath)
        {
            AddSubtitleFile(subtitlePath);
            SetMediaPlayerSubtitleFile(subtitlePath);
        }

        public void ClearSubtitle()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetSpu(-1);
            }
        }

        #endregion
    }

    public class SubtitleSelectedEventArgs : EventArgs
    {
        public string SubtitlePath { get; set; }
        public int SubtitleIndex { get; set; }
    }
}
