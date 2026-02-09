using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using VideoTranslator.Interfaces;

namespace VideoEditor.Windows;

public partial class DetailLogWindow : Window
{
    #region Fields

    private readonly Paragraph _logParagraph;
    private bool _isClosed = false;

    #endregion

    #region Constructor

    public DetailLogWindow()
    {
        InitializeComponent();
        _logParagraph = LogParagraph;
    }

    #endregion

    #region Public Methods

    public void AppendMessage(string message, MessageType messageType = MessageType.Info, bool newline = true)
    {
        if (_isClosed)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            var run = new Run(message)
            {
                Foreground = GetMessageColor(messageType)
            };

            if (messageType == MessageType.Title)
            {
                run.FontWeight = FontWeights.Bold;
                run.FontSize = 14;
            }

            _logParagraph.Inlines.Add(run);
            if (newline)
            {
                _logParagraph.Inlines.Add(new LineBreak());
            }

            if (AutoScrollCheckBox.IsChecked == true)
            {
                LogRichTextBox.ScrollToEnd();
            }
        });
    }

    public void Clear()
    {
        if (_isClosed)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            _logParagraph.Inlines.Clear();
        });
    }

    #endregion

    #region Private Methods

    private Brush GetMessageColor(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Info => Brushes.Black,
            MessageType.Success => Brushes.Green,
            MessageType.Warning => Brushes.Orange,
            MessageType.Error => Brushes.Red,
            MessageType.Debug => Brushes.Gray,
            MessageType.Title => Brushes.Blue,
            _ => Brushes.Black
        };
    }

    #endregion

    #region Event Handlers

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Clear();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _isClosed = true;
    }

    #endregion
}
