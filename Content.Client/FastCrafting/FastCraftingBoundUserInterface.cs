using Content.Shared.Construction.Prototypes;
using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Client.Construction;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Construction.Steps;
using Content.Shared.Materials;
using Content.Client.Construction.UI;
using Robust.Client.Placement;

namespace Content.Client.FastCrafting;

[UsedImplicitly]
public sealed partial class FastCraftingBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ConstructionSystem _construction = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IPlacementManager _placementManager = default!;

    private SimpleRadialMenu? _menu;

    public FastCraftingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<PhysicalCompositionComponent>(Owner, out var component))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        var recipes = Test(_prototypeManager.EnumeratePrototypes<ConstructionPrototype>(), component);
        var ok = ConvertToButtons(recipes);

        _menu.SetButtons(ok);
        _menu.OpenOverMouseScreenPosition();
    }
    private List<EntProtoId> Test(IEnumerable<ConstructionPrototype> prototypes, PhysicalCompositionComponent component)
    {
        var material = component.MaterialComposition.ElementAt(0).Key;
        var recipes = new List<EntProtoId>();
        foreach (var prototype in prototypes)
        {
            if (_prototypeManager.TryIndex(prototype.Graph, out ConstructionGraphPrototype? graph) && graph != null)
            {
                if (!graph.Nodes.TryGetValue("start", out var node))
                    continue;
                var edge = node.Edges.ElementAt(0);
                if (edge == null)
                    continue;
                var step = edge.Steps.ElementAt(0);
                if (step == null)
                    continue;
                if (step is MaterialConstructionGraphStep materialstep &&
                materialstep.MaterialPrototypeId == material)
                {
                    if (prototype.Type == ConstructionType.Structure)
                        recipes.Add(prototype.ID);
                    continue;
                }


            }

        }
        return recipes;
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(List<EntProtoId> recipes)
    {
        var buttons = new List<RadialMenuOptionBase>();
        foreach (var recipe in recipes)
        {

            // Options for selecting identities.
            var option = new RadialMenuActionOption<EntProtoId>(Aaah, recipe)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(recipe),
            };
            buttons.Add(option);

            // if (!hasDropOption)
            //     continue;

            // // the changeling horror can't be dropped, and you can't drop your current identity
            // var droppable = !EntMan.HasComponent<ChangelingUnremovableIdentityComponent>(identity.Identity) && currentIdentity != identity.Identity;

            // // Options for dropping identities.
            // var dropOption = new RadialMenuActionOption<NetEntity>(SendIdentityDrop, EntMan.GetNetEntity(identity.Identity.Value))
            // {
            //     IconSpecifier = RadialMenuIconSpecifier.With(identity.Identity.Value),
            //     ToolTip = (currentIdentity == identity.Identity)
            //         ? Loc.GetString("changeling-transform-bui-drop-identity-cannot-drop")
            //         : Loc.GetString("changeling-transform-bui-drop-identity-entity", ("entity", identity.Identity)),
            //     BackgroundColor = droppable ? null : DisabledOptionBackground, // cannot drop your current identity
            //     HoverBackgroundColor = droppable ? null : DisabledOptionHoverBackground
            // };
            // dropButtons.Add(dropOption);
        }
        return buttons;
    }

    private void Aaah(EntProtoId entity)
    {
        _popup.PopupCursor(entity.ToString());
    }
}






