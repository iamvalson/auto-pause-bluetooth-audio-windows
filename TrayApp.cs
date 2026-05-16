using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BtAudioGuard;

sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _startupItem;
    // Hidden control created on the UI thread — used to reliably marshal COM callbacks
    // onto the message loop. SynchronizationContext.Current is null at ApplicationContext
    // constructor time (before Application.Run installs WindowsFormsSynchronizationContext).
    private readonly Control _invoker;
    private readonly Icon _iconActive;
    private readonly Icon _iconPaused;
    private AudioDeviceMonitor? _monitor;
    private bool _enabled = true;

    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BtAudioGuard";

    public TrayApp()
    {
        _invoker = new Control();
        _invoker.CreateControl(); // force HWND creation so BeginInvoke works before any form is shown

        var baseIcon = BuildBaseIcon();
        _iconActive = BuildDottedIcon(baseIcon, Color.FromArgb(0, 200, 80));
        _iconPaused = BuildDottedIcon(baseIcon, Color.FromArgb(255, 140, 0));

        _enabledItem = new ToolStripMenuItem("Pause monitoring", null, ToggleEnabled);
        _startupItem = new ToolStripMenuItem("Run at startup", null, ToggleStartup)
        {
            Checked = IsStartupEnabled()
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("BT Audio Guard") { Enabled = false, Font = new Font(SystemFonts.MenuFont!, FontStyle.Bold) });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_enabledItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, Exit);

        _trayIcon = new NotifyIcon
        {
            Icon             = _iconActive,
            ContextMenuStrip = menu,
            Text             = "BT Audio Guard — Active",
            Visible          = true
        };

        StartMonitor();

        // Show after Application.Run starts the message loop
        _invoker.BeginInvoke(() => NotificationToast.Show("BT Audio Guard is active"));
    }

    private void StartMonitor()
    {
        _monitor = new AudioDeviceMonitor();
        _monitor.OnPaused = reason =>
        {
            _invoker.BeginInvoke(() => NotificationToast.Show($"Media paused — {reason}"));
        };
        _monitor.Start();
    }

    private void StopMonitor()
    {
        _monitor?.Dispose();
        _monitor = null;
    }

    private void ToggleEnabled(object? sender, EventArgs e)
    {
        _enabled = !_enabled;

        if (_enabled)
        {
            _enabledItem.Text  = "Pause monitoring";
            _trayIcon.Text     = "BT Audio Guard — Active";
            _trayIcon.Icon     = _iconActive;
            StartMonitor();
            NotificationToast.Show("Monitoring resumed");
        }
        else
        {
            _enabledItem.Text  = "Resume monitoring";
            _trayIcon.Text     = "BT Audio Guard — Paused";
            _trayIcon.Icon     = _iconPaused;
            StopMonitor();
            NotificationToast.Show("Monitoring paused");
        }
    }

    private void ToggleStartup(object? sender, EventArgs e)
    {
        if (IsStartupEnabled()) RemoveStartup();
        else AddStartup();
        _startupItem.Checked = IsStartupEnabled();
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
        return key?.GetValue(AppName) != null;
    }

    private static void AddStartup()
    {
        string? exe = Environment.ProcessPath;
        if (exe == null) return;
        using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
        key?.SetValue(AppName, $"\"{exe}\"");
    }

    private static void RemoveStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private void Exit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        StopMonitor();
        Application.Exit();
    }

    private static Icon BuildBaseIcon() =>
        Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static Icon BuildDottedIcon(Icon baseIcon, Color dotColor)
    {
        const int sz = 16;
        using var bmp = new Bitmap(sz, sz);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawIcon(baseIcon, new Rectangle(0, 0, sz, sz));
            using var brush = new SolidBrush(dotColor);
            // 5px dot, top-right corner, 1px from edges
            g.FillEllipse(brush, sz - 6, 1, 5, 5);
        }
        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone(); // Clone so DestroyIcon is safe immediately
        DestroyIcon(hIcon);
        return icon;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopMonitor();
            _trayIcon.Dispose();
            _invoker.Dispose();
            _iconActive.Dispose();
            _iconPaused.Dispose();
        }
        base.Dispose(disposing);
    }
}
