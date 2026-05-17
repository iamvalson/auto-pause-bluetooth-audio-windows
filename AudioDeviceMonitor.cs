using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;

namespace BtAudioGuard;

sealed class AudioDeviceMonitor : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;
    private string? _activeDeviceId;
    private DateTime _lastPauseAt = DateTime.MinValue;
    private static readonly TimeSpan DebouncePeriod = TimeSpan.FromSeconds(2);

    /// <summary>Fires on the COM callback thread when media is paused. Marshal to UI thread before touching UI.</summary>
    public Action<string>? OnPaused { get; set; }

    public AudioDeviceMonitor()
    {
        _enumerator = new MMDeviceEnumerator();
        _activeDeviceId = TryGetDefaultDeviceId();
        Debug.WriteLine($"[AudioMonitor] Watching: {TryGetFriendlyName(_activeDeviceId) ?? "(none)"}");
    }

    public void Start() => _enumerator.RegisterEndpointNotificationCallback(this);

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string newDeviceId)
    {
        if (flow != DataFlow.Render || role != Role.Multimedia) return;

        string? old = _activeDeviceId;
        _activeDeviceId = string.IsNullOrEmpty(newDeviceId) ? null : newDeviceId;

        if (old == null || old == _activeDeviceId) return;

        bool oldGone = !TryGetDevice(old, out var oldDevice);
        string oldName = oldDevice?.FriendlyName ?? "Audio device";

        // oldGone: device handle is gone entirely.
        // oldUnavailable: device still in registry (e.g. held open by OBS Application Audio
        // Capture) but physically disconnected — NotPresent/Unplugged.
        bool oldUnavailable = oldDevice != null &&
            oldDevice.State is DeviceState.NotPresent or DeviceState.Unplugged or DeviceState.Disabled;

        if (oldGone || oldUnavailable)
            TryPause($"{oldName} disconnected");
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (deviceId != _activeDeviceId) return;
        if (newState is DeviceState.NotPresent or DeviceState.Unplugged or DeviceState.Disabled)
        {
            string name = TryGetFriendlyName(deviceId) ?? "Audio device";
            TryPause($"{name} disconnected");
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        if (deviceId != _activeDeviceId) return;
        TryPause("Audio device removed");
    }

    public void OnDeviceAdded(string deviceId)
    {
        if (_activeDeviceId == null)
            _activeDeviceId = deviceId;
    }

    public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }

    private void TryPause(string reason)
    {
        if (DateTime.UtcNow - _lastPauseAt < DebouncePeriod) return;
        _lastPauseAt = DateTime.UtcNow;
        MediaController.PauseMedia();
        OnPaused?.Invoke(reason);
    }

    private string? TryGetDefaultDeviceId()
    {
        try { return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID; }
        catch { return null; }
    }

    private string? TryGetFriendlyName(string? deviceId)
    {
        if (deviceId == null) return null;
        return TryGetDevice(deviceId, out var dev) ? dev!.FriendlyName : null;
    }

    private bool TryGetDevice(string deviceId, out MMDevice? device)
    {
        try { device = _enumerator.GetDevice(deviceId); return true; }
        catch { device = null; return false; }
    }

    public void Dispose()
    {
        _enumerator.UnregisterEndpointNotificationCallback(this);
        _enumerator.Dispose();
    }
}
