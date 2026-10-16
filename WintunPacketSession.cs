using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpenSSTP.Windows;

/// <summary>
/// Low-level Wintun adapter/session wrapper. It provides Layer-3 packet I/O only;
/// it does not implement SSTP, PPP, IP configuration, routes, DNS, or a VPN by itself.
/// Creating an adapter modifies Windows networking and normally requires elevation.
/// </summary>
internal sealed class WintunPacketSession : IDisposable
{
    private readonly WintunNative _api;
    private nint _adapter;
    private nint _session;
    private bool _disposed;

    public nint ReadWaitEvent => _session == 0
        ? throw new InvalidOperationException("Wintun session has not been started.")
        : _api.GetReadWaitEvent(_session);

    public WintunPacketSession(string adapterName = "OpenSSTP", string tunnelType = "Open-SSTP")
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Wintun is Windows-only.");
        _api = new WintunNative();
        try
        {
            Guid requestedGuid = Guid.Parse("6C5A9D9E-6D2F-4D13-9F24-4F50454E5354");
            _adapter = _api.CreateAdapter(adapterName, tunnelType, ref requestedGuid);
            if (_adapter == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "WintunCreateAdapter failed. Run elevated and verify the Wintun driver is available.");
            _session = _api.StartSession(_adapter, 0x400000); // 4 MiB ring; valid power of two.
            if (_session == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "WintunStartSession failed.");
        }
        catch
        {
            if (_session != 0) _api.EndSession(_session);
            if (_adapter != 0) _api.CloseAdapter(_adapter);
            _api.Dispose();
            throw;
        }
    }

    /// <summary>Reads one IPv4/IPv6 packet queued by Windows. Returns null if no packet is ready.</summary>
    public byte[]? TryReceivePacket()
    {
        ThrowIfDisposed();
        nint packet = _api.ReceivePacket(_session, out uint size);
        if (packet == 0) return null;
        try
        {
            if (size == 0 || size > 65535) throw new InvalidDataException($"Invalid Wintun packet length: {size}.");
            byte[] data = new byte[checked((int)size)];
            Marshal.Copy(packet, data, 0, checked((int)size));
            return data;
        }
        finally { _api.ReleaseReceivePacket(_session, packet); }
    }

    /// <summary>Injects a complete IPv4/IPv6 Layer-3 packet into the Wintun adapter.</summary>
    public void SendPacket(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        if (data.IsEmpty || data.Length > 65535) throw new ArgumentOutOfRangeException(nameof(data), "Packet must be 1..65535 bytes.");
        nint packet = _api.AllocateSendPacket(_session, checked((uint)data.Length));
        if (packet == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "WintunAllocateSendPacket failed; the ring may be full.");
        byte[] copy = data.ToArray();
        Marshal.Copy(copy, 0, packet, copy.Length);
        _api.SendPacket(_session, packet);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_session == 0) throw new InvalidOperationException("Wintun session is not running.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_session != 0) { _api.EndSession(_session); _session = 0; }
        if (_adapter != 0) { _api.CloseAdapter(_adapter); _adapter = 0; }
        _api.Dispose();
    }
}
