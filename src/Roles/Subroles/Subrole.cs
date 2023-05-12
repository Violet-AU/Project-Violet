using System;
using System.Collections.Generic;
using System.Linq;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Factions.Interfaces;
using TOHTOR.Logging;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles.Subroles;

public abstract class Subrole: CustomRole
{
    public readonly HashSet<IFaction> FactionRestrictions = new();
    public readonly HashSet<Type> RoleRestrictions = new();
    
    public abstract string? Identifier();
    
    /// <summary>
    /// Returns the <see cref="CompatabilityMode"/> handling for set of types returned by <see cref="RestrictedRoles"/>
    /// </summary>
    public virtual CompatabilityMode RoleCompatabilityMode => RoleRestrictions.Count > 0 ? CompatabilityMode.Whitelisted : CompatabilityMode.Blacklisted;
    
    /// <summary>
    /// Returns the <see cref="CompatabilityMode"/> handling for set of factions returned by <see cref="RegulatedFactions"/>
    /// </summary>
    public virtual CompatabilityMode FactionCompatabilityMode => FactionRestrictions.Count > 0 ? CompatabilityMode.Whitelisted : CompatabilityMode.Blacklisted;
    
    /// <summary>
    /// A set of types of roles that this role is either restricted to or against determined by <see cref="RoleCompatabilityMode"/>
    /// </summary>
    /// <returns>Set of role types</returns>
    public virtual HashSet<Type>? RestrictedRoles() => RoleRestrictions;
    
    
    /// <summary>
    /// A set of factions that this role is either restricted to or against determined by <see cref="FactionCompatabilityMode"/>
    /// </summary>
    /// <returns>Set of factions</returns>
    public virtual HashSet<IFaction>? RegulatedFactions() => FactionRestrictions;

    public bool IsAssignableTo(PlayerControl player)
    {
        CustomRole role = player.GetCustomRole();
        Type factionType = role.Faction.GetType();
        if (RegulatedFactions() != null)
        {
            bool anyMatchFactions = RegulatedFactions()!.Any(f => factionType.IsAssignableTo(f.GetType()));
            DevLogger.Log($"Faction: {role.Faction} matches: {RegulatedFactions().Select(f => f.Name()).Fuse()} | {anyMatchFactions}");
            if (anyMatchFactions && FactionCompatabilityMode is CompatabilityMode.Blacklisted) return false;
            if (!anyMatchFactions && FactionCompatabilityMode is CompatabilityMode.Whitelisted) return false;
        }

        if (RestrictedRoles() == null || RestrictedRoles()!.Count == 0) return true;
        
        bool anyMatchRoles = RestrictedRoles()!.Any(r => r == role.GetType());
        if (anyMatchRoles && RoleCompatabilityMode is CompatabilityMode.Blacklisted) return false;
        return anyMatchRoles || FactionCompatabilityMode is not CompatabilityMode.Whitelisted;
    }
    
    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.Subrole(true);

    protected GameOptionBuilder AddRestrictToCrew(GameOptionBuilder builder, bool defaultOn = false)
    {
        return builder.SubOption(sub => sub.Name($"Restricted to {FactionInstances.Crewmates.FactionColor().Colorize(FactionInstances.Crewmates.Name())}")
            .Key("Restricted to Crew")
            .AddOnOffValues(defaultOn)
            .BindBool(b =>
            {
                if (b) FactionRestrictions.Add(FactionInstances.Crewmates);
                else FactionRestrictions.Remove(FactionInstances.Crewmates);
            })
            .Build());
    }
}

/// <summary>
/// Specifies the compatability of a list of objects for Subrole assignment
/// </summary>
public enum CompatabilityMode
{
    /// <summary>
    /// Exclusively includes items for assignment
    /// </summary>
    Whitelisted,
    /// <summary>
    /// Excludes items from assignment
    /// </summary>
    Blacklisted
}