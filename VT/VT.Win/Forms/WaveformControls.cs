using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace VT.Win.Forms;

public class WaveformControls
{
    #region Fields

    private readonly Control parent;
    private Button playButton;
    private HScrollBar hScrollBar;
    private TrackBar zoomTrackBar;
    private Label zoomValueLabel;
    private bool controlsInitialized;
    private Timer? debounceTimer;

    #endregion

    #region Events

    public event EventHandler? PlayButtonClicked;
    public event ScrollEventHandler? ScrollBarScrolled;
    public event EventHandler? ZoomTrackBarChanged;

    #endregion

    #region Properties

    public Button PlayButton => playButton;
    public HScrollBar HScrollBar => hScrollBar;
    public TrackBar ZoomTrackBar => zoomTrackBar;
    public Label ZoomValueLabel => zoomValueLabel;
    public double ZoomLevel { get; private set; } = 1.0;

    #endregion

    #region Constructor

    public WaveformControls(Control parent)
    {
        this.parent = parent;
        InitializeControls();
        InitializeDebounceTimer();
    }

    #endregion

    #region Initialization

    private void InitializeControls()
    {
        #region Play Button

        playButton = new Button
        {
            Text = "▶",
            Size = new Size(30, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.LightGray,
            Cursor = Cursors.Hand
        };
        playButton.Click += (s, e) => PlayButtonClicked?.Invoke(s, e);
        parent.Controls.Add(playButton);

        #endregion

        #region Horizontal Scroll Bar

        hScrollBar = new HScrollBar
        {
            Height = 20,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            LargeChange = 10,
            SmallChange = 1
        };
        hScrollBar.Scroll += (s, e) => ScrollBarScrolled?.Invoke(s, e);
        parent.Controls.Add(hScrollBar);

        #endregion

        #region Zoom Track Bar

        zoomTrackBar = new TrackBar
        {
            Orientation = Orientation.Horizontal,
            Minimum = 1,
            Maximum = 20,
            Value = 10,
            Height = 45,
            TickFrequency = 1,
            TickStyle = TickStyle.BottomRight
        };
        zoomTrackBar.ValueChanged += ZoomTrackBar_ValueChanged;
        parent.Controls.Add(zoomTrackBar);

        #endregion

        #region Zoom Value Label

        zoomValueLabel = new Label
        {
            Text = "1.00x",
            Size = new Size(50, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        parent.Controls.Add(zoomValueLabel);

        #endregion

        controlsInitialized = true;
        UpdateControlPositions();
    }

    private void InitializeDebounceTimer()
    {
        debounceTimer = new Timer();
        debounceTimer.Interval = 300;
        debounceTimer.Tick += DebounceTimer_Tick;
    }

    #endregion

    #region Event Handlers

    private void ZoomTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        ZoomLevel = zoomTrackBar.Value / 10.0;
        zoomValueLabel.Text = $"{ZoomLevel:F2}x";

        if (debounceTimer != null)
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        }
    }

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        if (debounceTimer != null)
        {
            debounceTimer.Stop();
        }
        ZoomTrackBarChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public Methods

    public void UpdateControlPositions()
    {
        if (!controlsInitialized)
        {
            return;
        }

        #region Play Button Position

        playButton.Location = new Point(10, 10);

        #endregion

        #region Scroll Bar Position

        hScrollBar.Location = new Point(50, parent.Height - 25);
        hScrollBar.Width = parent.Width - 60;

        #endregion

        #region Zoom Track Bar Position

        zoomTrackBar.Location = new Point(50, parent.Height - 60);
        zoomTrackBar.Width = parent.Width - 60;

        #endregion

        #region Zoom Value Label Position

        zoomValueLabel.Location = new Point(parent.Width - 60, parent.Height - 85);

        #endregion
    }

    public void UpdatePlayButton(bool isPlaying)
    {
        if (isPlaying)
        {
            playButton.Text = "⏹";
            playButton.BackColor = Color.Orange;
        }
        else
        {
            playButton.Text = "▶";
            playButton.BackColor = Color.LightGray;
        }
    }

    public void UpdateScrollBar(int totalWidth, int visibleWidth)
    {
        if (totalWidth <= visibleWidth)
        {
            hScrollBar.Enabled = false;
            hScrollBar.Maximum = 0;
        }
        else
        {
            hScrollBar.Enabled = true;
            hScrollBar.Maximum = totalWidth - visibleWidth;
            if (hScrollBar.Value > hScrollBar.Maximum)
            {
                hScrollBar.Value = hScrollBar.Maximum;
            }
        }
    }

    #endregion
}
