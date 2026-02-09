using System;
using System.Drawing;
using System.Windows.Forms;
using VideoTranslator.Interfaces;

namespace VT.Win.Forms;
using MessageType = VideoTranslator.Interfaces.MessageType;
public partial class LogViewerForm : Form
{
    #region Private Fields

    private RichTextBox logRichTextBox;
    private Button clearButton;
    private Button closeButton;
    private Panel buttonPanel;
    private static LogViewerForm _instance;
    private static readonly object _lock = new object();

    #endregion

    #region Constructor

    public LogViewerForm()
    {
        InitializeComponent();
    }

    #endregion

    #region Public Methods

    public static LogViewerForm GetInstance()
    {
        lock (_lock)
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new LogViewerForm();
            }
            return _instance;
        }
    }

    public void AddLog(string message, MessageType type = MessageType.Info)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => AddLog(message, type)));
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";

        #region Set Color and Font Based on Type

        var originalColor = logRichTextBox.SelectionColor;
        var originalFont = logRichTextBox.SelectionFont;

        switch (type)
        {
            case MessageType.Info:
                logRichTextBox.SelectionColor = Color.Black;
                logRichTextBox.SelectionFont = new Font("Consolas", 9);
                break;

            case MessageType.Success:
                logRichTextBox.SelectionColor = Color.Green;
                logRichTextBox.SelectionFont = new Font("Consolas", 9, FontStyle.Bold);
                break;

            case MessageType.Warning:
                logRichTextBox.SelectionColor = Color.Orange;
                logRichTextBox.SelectionFont = new Font("Consolas", 9, FontStyle.Bold);
                break;

            case MessageType.Error:
                logRichTextBox.SelectionColor = Color.Red;
                logRichTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Bold);
                break;

            case MessageType.Debug:
                logRichTextBox.SelectionColor = Color.Gray;
                logRichTextBox.SelectionFont = new Font("Consolas", 8);
                break;

            case MessageType.Title:
                logRichTextBox.SelectionColor = Color.Blue;
                logRichTextBox.SelectionFont = new Font("Consolas", 11, FontStyle.Bold | FontStyle.Underline);
                break;
        }

        #endregion

        logRichTextBox.AppendText(logEntry + Environment.NewLine);
        logRichTextBox.ScrollToCaret();

        #region Restore Original Color and Font

        logRichTextBox.SelectionColor = originalColor;
        logRichTextBox.SelectionFont = originalFont;

        #endregion
    }

    public void ClearLogs()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(ClearLogs));
            return;
        }

        logRichTextBox.Clear();
    }

    #endregion

    #region Private Methods

    private void InitializeComponent()
    {
        #region Form Setup

        Text = "详细日志";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 400);
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar = true;

        #endregion

        #region Button Panel

        buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = SystemColors.Control
        };

        #endregion

        #region Clear Button

        clearButton = new Button
        {
            Text = "清空日志",
            Size = new Size(100, 30),
            Location = new Point(10, 10),
            FlatStyle = FlatStyle.Flat
        };
        clearButton.Click += ClearButton_Click;
        buttonPanel.Controls.Add(clearButton);

        #endregion

        #region Close Button

        closeButton = new Button
        {
            Text = "关闭",
            Size = new Size(100, 30),
            Location = new Point(120, 10),
            FlatStyle = FlatStyle.Flat
        };
        closeButton.Click += CloseButton_Click;
        buttonPanel.Controls.Add(closeButton);

        #endregion

        #region Log RichTextBox

        logRichTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        #endregion

        #region Add Controls

        Controls.Add(logRichTextBox);
        Controls.Add(buttonPanel);

        #endregion
    }

    private void ClearButton_Click(object sender, EventArgs e)
    {
        ClearLogs();
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    #endregion
}
