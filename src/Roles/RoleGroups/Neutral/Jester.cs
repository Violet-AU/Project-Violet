using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.Overrides;
using TOHTOR.Victory.Conditions;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;

namespace TOHTOR.Roles.RoleGroups.Neutral;

public class Jester : CustomRole
{
    private bool canUseVents;
    private bool impostorVision;

    [RoleAction(RoleActionType.SelfExiled)]
    public void JesterWin()
    {
        VentLogger.Fatal("Forcing Win by Jester");
        ManualWin jesterWin = new(MyPlayer, WinReason.SoloWinner, 999);
        jesterWin.Activate();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(opt =>
                opt.Name("Has Impostor Vision").Bind(v => impostorVision = (bool)v).AddOnOffValues().Build())
            .SubOption(opt => opt.Name("Can Use Vents")
                .Bind(v => canUseVents = (bool)v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
            .Faction(FactionInstances.Solo)
            .VanillaRole(canUseVents ? RoleTypes.Engineer : RoleTypes.Crewmate)
            .SpecialType(SpecialType.Neutral)
            .CanVent(canUseVents)
            .RoleFlags(RoleFlag.CannotWinAlone)
            .RoleColor(new Color(0.93f, 0.38f, 0.65f))
            .OptionOverride(Override.CrewLightMod, () => AUSettings.ImpostorLightMod(), () => impostorVision)
            .OptionOverride(Override.EngVentDuration, 100f)
            .OptionOverride(Override.EngVentCooldown, 0.1f);
    }
}