using System.Runtime.InteropServices;
using Windows.Media.Control;
using PlaybackStatus = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus;

namespace BtAudioGuard;

static class MediaController
{
    // ── Public entry point ─────────────────────────────────────────────────

    /// <summary>
    /// Pauses every media session that is currently playing.
    /// Uses SMTC (targeted Pause, not a toggle) so already-paused media is not affected.
    /// Falls back to a media key press for apps not registered with SMTC.
    /// </summary>
    public static void PauseMedia() => _ = PauseAllAsync();

    // ── SMTC ───────────────────────────────────────────────────────────────

    private static async Task PauseAllAsync()
    {
        try
        {
            var manager  = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var sessions = manager.GetSessions();

            var tasks = new List<Task>();
            foreach (var session in sessions)
            {
                try
                {
                    if (session.GetPlaybackInfo().PlaybackStatus == PlaybackStatus.Playing)
                        tasks.Add(session.TryPauseAsync().AsTask());
                }
                catch { /* session may have vanished between enumeration and query */ }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                return; // SMTC handled it — skip key fallback
            }
        }
        catch { /* SMTC unavailable — fall through */ }

        // Fallback: simulate the media pause key.
        // Covers legacy apps that don't register with SMTC.
        SendMediaKey();
    }

    // ── SendInput fallback ─────────────────────────────────────────────────

    private const int    INPUT_KEYBOARD      = 1;
    private const uint   KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint   KEYEVENTF_KEYUP     = 0x0002;
    private const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public INPUTUNION u; }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] private long _p1;
        [FieldOffset(8)] private long _p2;
        [FieldOffset(16)] private long _p3;
        [FieldOffset(24)] private int  _p4;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk; public ushort wScan;
        public uint dwFlags; public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint n, INPUT[] inputs, int size);

    private static void SendMediaKey()
    {
        var inputs = new[]
        {
            BuildKey(VK_MEDIA_PLAY_PAUSE, KEYEVENTF_EXTENDEDKEY),
            BuildKey(VK_MEDIA_PLAY_PAUSE, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT BuildKey(ushort vk, uint flags) => new INPUT
    {
        type = INPUT_KEYBOARD,
        u    = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } }
    };
}
