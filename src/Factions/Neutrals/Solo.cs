using TOHTOR.Factions.Interfaces;
using TOHTOR.Options;
using UnityEngine;

namespace TOHTOR.Factions.Neutrals;

public class Solo : Faction<Solo>
{
    private string factionName;

    public Solo(string? factionName = null)
    {
        this.factionName = factionName ?? "Solo";
    }

    public override string Name() => this.factionName;

    public override Relation Relationship(Solo sameFaction) => Relation.None;

    public override bool AlliesSeeRole() => RoleOptions.NeutralOptions.KnowAlliedRoles;

    public override Color FactionColor() => Color.gray;

    public override Relation RelationshipOther(IFaction other) => Relation.None;
}