namespace Content.Shared._Shitmed.Body.Components;

/// <summary>
///     This component allows to regrow limbs.
///     Once added, it will first lookup if any limbs are gone.
///     Any severed limb will regrow over time
///     A limb regrows from 0% to 100%
///
///     In the default values below, one limb takes 100 seconds to regrow and will consume 100 hunger.
/// </summary>
[RegisterComponent]
public sealed partial class RegrowLimbsComponent : Component
{
    /// <summary>
    ///     Represents how long in seconds it takes to regrow 1% of a limb
    /// </summary>
    [DataField]
    public TimeSpan GrowthRate = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Represents how much hunger is consumed per limb regrowing, per second.
    ///     People roughly spawn with 150 hunger
    /// </summary>
    [DataField]
    public float NutrientConsumption = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public TimeSpan RegrowthUpdateRate = TimeSpan.FromSeconds(1);

    public TimeSpan NextUpdate = TimeSpan.FromSeconds(0);

    public Dictionary<string, float> LimbsDict = new Dictionary<string, float>();
}
