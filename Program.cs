using System.Windows.Forms;
using BtAudioGuard;

Application.EnableVisualStyles();

using var mutex = new Mutex(true, "Local\\BtAudioGuard_SingleInstance", out bool isFirstInstance);
if (!isFirstInstance)
{
    MessageBox.Show(
        "BT Audio Guard is already running in your system tray.",
        "BT Audio Guard",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    return;
}

Application.SetHighDpiMode(HighDpiMode.SystemAware);
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TrayApp());
