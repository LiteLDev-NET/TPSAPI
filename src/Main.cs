using LiteLoader.Event;
using LiteLoader.Hook;
using LiteLoader.NET;
using LiteLoader.RemoteCall;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TPSAPI;
[PluginMain("TPSAPI")]
public class TPSAPI : IPluginInitializer
{
    public string Introduction => "TPSAPI";
    public Dictionary<string, string> MetaData => new();
    public Version Version => new();
    public void OnInitialize()
    {
        System.Timers.Timer timer = new(1000);
        timer.Elapsed += (sender, e) =>
        {
            Data.RealTPS = (uint)Data.MSPTs.Count;
            Data.AvgMSPT = Data.MSPTs.Sum() / Data.RealTPS;
            Data.MSPTs.Clear();
        };
        Thook.RegisterHook<TickHook, TickHookDelegate>();
        ServerStartedEvent.Subscribe(ev =>
        {
            timer.Start();
            return true;
        });
        RemoteCallAPI.ExportAs("TPSAPI", "GetAvgMSPT", () => Data.AvgMSPT);
        RemoteCallAPI.ExportAs("TPSAPI", "GetAvgTPS", () => Data.AvgTPS);
        RemoteCallAPI.ExportAs("TPSAPI", "GetRealTPS", () => Data.RealTPS);
        RemoteCallAPI.ExportAs("TPSAPI", "GetCurrectMSPT", () => Data.CurrectMSPT);
        RemoteCallAPI.ExportAs("TPSAPI", "GetCurrectTPS", () => Data.CurrectTPS);
        RemoteCallAPI.ExportAs("InfoAPI", "GetWorkingSet", () => Process.GetCurrentProcess().WorkingSet64);
    }
}

internal delegate void TickHookDelegate(nint @this);
[HookSymbol("?tick@ServerLevel@@UEAAXXZ")]
internal class TickHook : THookBase<TickHookDelegate>
{
    public override TickHookDelegate Hook => (@this) =>
    {
        long preTickTime = DateTime.Now.Ticks;
        Original(@this);
        long nowTickTime = DateTime.Now.Ticks;
        Data.CurrectMSPT = TimeSpan.FromTicks(nowTickTime - preTickTime).TotalMilliseconds;
        Data.MSPTs.Add(Data.CurrectMSPT);
    };
}

public static class Data
{
    internal static ConcurrentBag<double> MSPTs = new();
    public static double AvgMSPT { get; set; }
    public static double AvgTPS => 1000 / AvgMSPT;
    public static uint RealTPS { get; set; }
    public static double CurrectMSPT { get; set; }
    public static double CurrectTPS => 1000 / CurrectMSPT;
}
