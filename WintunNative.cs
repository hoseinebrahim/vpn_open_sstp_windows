using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenSSTP.Windows;

/// <summary>
/// Loads the official Wintun DLL shipped with this application and resolves a small set of exports.
/// This is an integration scaffold, not a complete VPN tunnel. The SSTP/PPP engine must feed and
/// receive IP packets through the Wintun session before traffic can flow.
/// </summary>
internal sealed class WintunNative : IDisposable
{
    private readonly nint _library;
    public CreateAdapterDelegate CreateAdapter { get; }
    public CloseAdapterDelegate CloseAdapter { get; }
    public StartSessionDelegate StartSession { get; }
    public EndSessionDelegate EndSession { get; }
    public GetRunningDriverVersionDelegate GetRunningDriverVersion { get; }
    public GetReadWaitEventDelegate GetReadWaitEvent { get; }
    public ReceivePacketDelegate ReceivePacket { get; }
    public ReleaseReceivePacketDelegate ReleaseReceivePacket { get; }
    public AllocateSendPacketDelegate AllocateSendPacket { get; }
    public SendPacketDelegate SendPacket { get; }

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate nint CreateAdapterDelegate(string name, string tunnelType, ref Guid requestedGuid);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void CloseAdapterDelegate(nint adapter);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint StartSessionDelegate(nint adapter, uint capacity);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void EndSessionDelegate(nint session);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate uint GetRunningDriverVersionDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint GetReadWaitEventDelegate(nint session);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint ReceivePacketDelegate(nint session, out uint packetSize);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void ReleaseReceivePacketDelegate(nint session, nint packet);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint AllocateSendPacketDelegate(nint session, uint packetSize);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void SendPacketDelegate(nint session, nint packet);

    public WintunNative()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.X86 => "win-x86",
            Architecture.Arm64 => "win-arm64",
            _ => throw new PlatformNotSupportedException($"Wintun is not packaged for {RuntimeInformation.ProcessArchitecture}.")
        };
        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", arch, "native", "wintun.dll");
        if (!File.Exists(path)) throw new FileNotFoundException("Wintun DLL not found for this CPU architecture.", path);
        _library = NativeLibrary.Load(path);
        try
        {
            CreateAdapter = Get<CreateAdapterDelegate>("WintunCreateAdapter");
            CloseAdapter = Get<CloseAdapterDelegate>("WintunCloseAdapter");
            StartSession = Get<StartSessionDelegate>("WintunStartSession");
            EndSession = Get<EndSessionDelegate>("WintunEndSession");
            GetRunningDriverVersion = Get<GetRunningDriverVersionDelegate>("WintunGetRunningDriverVersion");
            GetReadWaitEvent = Get<GetReadWaitEventDelegate>("WintunGetReadWaitEvent");
            ReceivePacket = Get<ReceivePacketDelegate>("WintunReceivePacket");
            ReleaseReceivePacket = Get<ReleaseReceivePacketDelegate>("WintunReleaseReceivePacket");
            AllocateSendPacket = Get<AllocateSendPacketDelegate>("WintunAllocateSendPacket");
            SendPacket = Get<SendPacketDelegate>("WintunSendPacket");
        }
        catch { NativeLibrary.Free(_library); throw; }
    }

    private T Get<T>(string export) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, export));

    public void Dispose() => NativeLibrary.Free(_library);
}
