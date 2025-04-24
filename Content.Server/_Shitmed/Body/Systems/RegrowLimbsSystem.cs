
using Content.Shared.Body.Systems;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
namespace Content.Server._Shitmed.Body.Systems;

public sealed class RegrowLimbsSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RegrowLimbsComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
    }

    private void OnBodyPartRemoved(Entity<RegrowLimbsComponent> ent, ref BodyPartRemovedEvent args)
    {

        if (args.Part.Comp.PartType == BodyPartType.Head) // don't regrow a head right
            return;

        if (args.Part.Comp.PartType == BodyPartType.Torso) // How would that happen?
            return;

        var slotId = _body.GetSlotFromBodyPart(args.Part);

        ent.Comp.LimbsDict.TryAdd(slotId, 0);

        return;
    }

    /// <summary>
    ///     This functions simply regrows a limb according to the BodyComponent prototype.
    ///     This way a moth regrows a moth limb.
    ///     slotId can be "right leg" "left leg" "right arm", etc.
    ///     Returns wether the limb was able to be regrown or not.
    /// </summary>
    public bool RegrowLimb(EntityUid ent, string slotId)
    {
        // Code inspired from SharedSurgerySystem.Steps.cs / OnAddPartStep method
        if (!TryComp<BodyComponent>(ent, out var BodyComp))
            return false;

        var bodyProto = BodyComp.Prototype;
        if (!bodyProto.HasValue)
            return false;

        var pro = _prototypes.Index(bodyProto.Value);

        var childPart = Spawn(pro.Slots[slotId].Part, new EntityCoordinates(ent, Vector2.Zero));
        var childPartComponent = Comp<BodyPartComponent>(childPart);

        if (!TryGetBodyPartParent(ent, slotId, out var parentPart))
        {
            QueueDel(childPart);
            return false;
        }

        _body.TryCreatePartSlot(parentPart, slotId, childPartComponent.PartType, out var _);
        Dirty(childPart, childPartComponent);
        _body.AttachPart(parentPart, slotId, childPart);

        return true;
    }

    /// <summary>
    ///     This function tries to return the entity that 'slotId' should be attached to on a body.
    ///     I.E right feet should go to right leg. Leg to torso. etc. (depends on the body prototype)
    ///     slotId can be "right leg" "left leg" "right arm", etc. (see body prototype)
    ///     slotId can be retrieved with a method like BodySystem.GetSlotFromBodyPart (or from the body prototype)
    /// </summary>
    public bool TryGetBodyPartParent(EntityUid ent, string slotId, out EntityUid parent)
    {
        parent = ent; // shitcode :>

        if (!TryComp<BodyComponent>(ent, out var body))
            return false;

        var bodyProto = body.Prototype;
        if (!bodyProto.HasValue)
            return false;

        var prototype = _prototypes.Index(bodyProto.Value);

        // Check if the slot Id exists in the body
        if (!prototype.Slots.TryGetValue(slotId, out var slotProto))
            return false;

        var root = _body.GetRootPartOrNull(ent);
        if (!root.HasValue)
            return false;

        parent = root.Value.Entity;
        var rootBodyPartComp = root.Value.BodyPart;

        string parentSlotId = "0";
        // Determines which slot is our slotId's parent
        foreach (var slot in prototype.Slots)
        {
            foreach(var con in slot.Value.Connections)
            {
                if (con == slotId)
                {
                    parentSlotId = slot.Key;
                    break;
                }
            }
            if (parentSlotId != "0") break;
        }

        // Didn't find a parent.. can't regrow a root part.
        if (parentSlotId == "0")
            return false;

        // The parent part is the torso, return torso.
        if (parentSlotId == prototype.Root)
            return true;

        foreach (var bodyPart in _body.GetBodyPartChildren(parent, rootBodyPartComp))
        {
            if (!bodyPart.Component.ParentSlot.HasValue)
                continue;

            if (bodyPart.Component.ParentSlot.Value.Id == parentSlotId) // We have found the parent entity
            {
                parent = bodyPart.Id;
                return true;
            }
        }
        return false; // We have not found the parent entity
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RegrowLimbsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<HungerComponent>(uid, out var hungerComp))
                continue;

            if (_timing.CurTime < comp.NextUpdate)
                continue;
            comp.NextUpdate = _timing.CurTime + comp.RegrowthUpdateRate;

            if (comp.LimbsDict.Count == 0)
                continue;

            foreach (var item in comp.LimbsDict)
            {
                RegrowLimb(uid, item.Key);
            }
            comp.LimbsDict.Clear();
        }
    }

}
