using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BtAudioGuard;

/// <summary>
/// A dark, fade-in/out toast notification that appears bottom-right.
/// Replaces NotifyIcon.ShowBalloonTip which is silently suppressed on Windows 11.
/// </summary>
sealed class NotificationToast : Form
{
    // ── Win32: rounded corners (Windows 11) ───────────────────────────────
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    // ── Timing ─────────────────────────────────────────────────────────────
    private const int TickMs   = 25;
    private const int HoldMs   = 3000;
    private const double FadeIn  = 0.09;
    private const double FadeOut = 0.06;

    private enum Phase { FadeIn, Hold, FadeOut }
    private Phase _phase = Phase.FadeIn;
    private int _holdElapsed;
    private readonly System.Windows.Forms.Timer _timer;

    // ── Factory ────────────────────────────────────────────────────────────
    /// <summary>Creates and shows a toast on the calling (UI) thread.</summary>
    public static void Show(string message)
    {
        var t = new NotificationToast(message);
        t.Show();
    }

    // ── Construction ───────────────────────────────────────────────────────
    private NotificationToast(string message)
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost         = true;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;
        Size            = new Size(310, 74);
        BackColor       = Color.FromArgb(28, 28, 28);
        Opacity         = 0;

        // Position: bottom-right above the taskbar
        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16);

        // Click anywhere to dismiss
        void dismiss(object? s, EventArgs e) => StartFadeOut();

        // Title
        var title = new Label
        {
            Text      = "BT Audio Guard",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Bounds    = new Rectangle(16, 10, 278, 20),
        };
        title.Click += dismiss;

        // Message
        var body = new Label
        {
            Text      = message,
            ForeColor = Color.FromArgb(195, 195, 195),
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 9f),
            Bounds    = new Rectangle(16, 33, 278, 30),
        };
        body.Click += dismiss;

        Controls.Add(title);
        Controls.Add(body);
        Click += dismiss;

        _timer = new System.Windows.Forms.Timer { Interval = TickMs };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Ask DWM for rounded corners on Windows 11 (no-op on older Windows)
        int pref = DWMWCP_ROUND;
        DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
    }

    // ── Fade state machine ─────────────────────────────────────────────────
    private void OnTick(object? sender, EventArgs e)
    {
        switch (_phase)
        {
            case Phase.FadeIn:
                Opacity = Math.Min(0.93, Opacity + FadeIn);
                if (Opacity >= 0.93) _phase = Phase.Hold;
                break;

            case Phase.Hold:
                _holdElapsed += TickMs;
                if (_holdElapsed >= HoldMs) StartFadeOut();
                break;

            case Phase.FadeOut:
                Opacity = Math.Max(0, Opacity - FadeOut);
                if (Opacity <= 0) { _timer.Stop(); Close(); }
                break;
        }
    }

    private void StartFadeOut()
    {
        if (_phase == Phase.FadeOut) return;
        _phase = Phase.FadeOut;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    // Required for Opacity to work on a borderless form
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80000; // WS_EX_LAYERED
            return cp;
        }
    }
}
