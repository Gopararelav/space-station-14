using Content.Shared.Construction.Prototypes;
using Content.Client.UserInterface.Controls;
using Content.Client.Construction;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Materials;
using Robust.Client.Placement;
using Robust.Shared.Utility;
using Robust.Shared.Enums;
using System.Linq;

namespace Content.Client.FastCrafting;

[UsedImplicitly]
public sealed partial class FastCraftingBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ConstructionSystem _construction = default!;
    [Dependency] private IPlacementManager _placementManager = default!;

    private SimpleRadialMenu? _menu;
    private List<ConstructionPrototype> _constructionPrototypes = new();
    private const int ItemsPerPage = 16;
    private ProtoId<MaterialPrototype> _cachedMaterial = new();
    public FastCraftingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
    protected override void Open()
    {
        base.Open();

        if (!IsOpened) // Не работает :(
            return;

        if (!EntMan.TryGetComponent<PhysicalCompositionComponent>(Owner, out var component))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        GetRecipesForMaterial(_prototypeManager.EnumeratePrototypes<ConstructionPrototype>(), component);

        var buttons = BuildRootPage();
        _menu.SetButtons(buttons);
        _menu.OpenOverMouseScreenPosition();
    }

    /// <summary>
    /// Фильтрация нужных прототипов крафта
    /// </summary>
    private void GetRecipesForMaterial(IEnumerable<ConstructionPrototype> prototypes, PhysicalCompositionComponent component)
    {
        var material = component.MaterialComposition.ElementAt(0).Key;

        if (_cachedMaterial == material)
            return;

        _constructionPrototypes = new();
        _cachedMaterial = material;

        foreach (var prototype in prototypes)
        {
            if (_prototypeManager.TryIndex(prototype.Graph, out ConstructionGraphPrototype? graph) && graph != null)
            {
                if (!graph.Nodes.TryGetValue("start", out var node))
                    continue;

                foreach (var edge in node.Edges)
                {
                    if (edge == null)
                        continue;

                    if (edge.Target != prototype.TargetNode)
                        continue;

                    var step = edge.Steps.ElementAt(0);

                    if (step == null)
                        continue;

                    if (step is MaterialConstructionGraphStep materialstep &&
                    materialstep.MaterialPrototypeId == material)
                    {
                        if (prototype.Type == ConstructionType.Structure)
                            if (!_constructionPrototypes.Contains(prototype))
                                _constructionPrototypes.Add(prototype);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Сборка ui
    /// </summary>
    private List<RadialMenuOptionBase> BuildRootPage()
    {
        if (_constructionPrototypes.Count == 0)
            return new List<RadialMenuOptionBase>();

        var pageCount = (_constructionPrototypes.Count + ItemsPerPage - 1) / ItemsPerPage;

        List<RadialMenuOptionBase>? nextPage = null;

        for (var i = pageCount - 1; i >= 0; i--)
        {
            var page = new List<RadialMenuOptionBase>();

            var pageItems = _constructionPrototypes.Skip(i * ItemsPerPage).Take(ItemsPerPage);
            foreach (var prototype in pageItems)
            {
                if (!_construction.TryGetRecipePrototype(prototype.ID, out var targetProtoId))
                    continue;
                _prototypeManager.TryIndex(targetProtoId, out var proto);

                page.Add(new RadialMenuActionOption<ConstructionPrototype>(OnItemSelected, prototype)
                {
                    IconSpecifier = RadialMenuIconSpecifier.With(targetProtoId),
                    ToolTip = proto?.Name ?? targetProtoId,
                });
            }

            if (nextPage != null)
            {
                page.Add(new RadialMenuNestedLayerOption(nextPage)
                {
                    IconSpecifier = RadialMenuIconSpecifier.With(
                        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Default/right_arrow.svg.192dpi.png"))),
                    ToolTip = "Вперёд",
                    Order = 1,
                });
            }

            nextPage = page;
        }

        return nextPage!;
    }


    /// <summary>
    /// Вызывается при выборе предмета из списка.
    /// </summary>
    private void OnItemSelected(ConstructionPrototype constructionPrototype)
    {
        _placementManager.BeginPlacing(new PlacementInformation()
        {
            IsTile = false,
            PlacementOption = constructionPrototype.PlacementMode
        }, new ConstructionPlacementHijack(constructionPrototype));
    }
}