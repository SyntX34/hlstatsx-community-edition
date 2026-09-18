using System;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.GameEventDefinitions;
using HLStatsX.Contract;

namespace HLStatsX.Example;

/// <summary>
/// Example plugin demonstrating how third-party plugins (e.g. NHZombieReloaded, VIP_CORE, RPG, etc.)
/// interact with HLStatsX:CE using the HLStatsX.Contract.IHLStatsXApi shared interface.
/// </summary>
[PluginMetadata(
    Id = "HLStatsX.Example",
    Version = "1.0.0",
    Name = "HLStatsX Example Consumer Plugin",
    Author = "SyntX34",
    Description = "Demonstrates how to log actions and trigger events in HLStatsX:CE"
)]
public class HLStatsXExample : BasePlugin
{
    private IHLStatsXApi? _hlxApi;

    public HLStatsXExample(ISwiftlyCore core) : base(core)
    {
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        try
        {
            _hlxApi = interfaceManager.GetSharedInterface<IHLStatsXApi>("HlStatsX.API");
            if (_hlxApi != null)
            {
                _hlxApi.RegisterConsumer("HLStatsX.Example");
                Console.WriteLine($"[HLStatsX.Example] Bound to HLStatsX API v{_hlxApi.PluginVersion}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HLStatsX.Example] Could not get HLStatsX API: {ex.Message}");
        }
    }

    public override void Load(bool hotReload)
    {
        Core.GameEvent.HookPost<EventPlayerDeath>((@event) =>
        {
            var attacker = @event.AttackerPlayer;
            if (attacker == null || !attacker.IsValid || _hlxApi == null) return HookResult.Continue;

            if (@event.Headshot)
            {
                _hlxApi.TriggerPlayerAction(attacker, "vip_bonus_kill");
            }

            return HookResult.Continue;
        });

        Core.Command.RegisterCommand("mycustomrank", (ctx) =>
        {
            if (ctx.Sender is { } player && player.IsValid && _hlxApi != null)
            {
                _hlxApi.OpenStatsMenu(player);
            }
        });

        Console.WriteLine("[HLStatsX.Example] Loaded successfully.");
    }

    public override void Unload()
    {
        _hlxApi = null;
    }
}
