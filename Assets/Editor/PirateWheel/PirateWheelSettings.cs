using UnityEngine;

// Parameters for the procedurally built pirate wheel. All distances are in world meters.
// The walking radius is not a setting: it is read from the Wheel's scale (lossyScale.x / 2),
// the same value WheelController uses, so fighters always stand exactly on the groove floor.
public class PirateWheelSettings : ScriptableObject
{
    [Header("Target")]
    public string targetObjectName = "Wheel";

    [Header("Rim profile (groove + front/back flanges)")]
    [Tooltip("Width of the groove the fighters walk in (along the camera axis).")]
    public float grooveWidth = 0.26f;
    [Tooltip("Thickness of each raised flange (front and back).")]
    public float flangeThickness = 0.09f;
    [Tooltip("How far the flanges rise above the walking surface. This is what hides the feet.")]
    public float flangeHeight = 0.12f;
    [Tooltip("Radial depth of the rim below the walking surface.")]
    public float rimDepth = 0.14f;
    public float bevel = 0.012f;
    [Min(1)] public int felloeCount = 4;
    [Min(4)] public int segmentsPerFelloe = 32;

    [Header("Iron")]
    public bool ironTire = true;
    public float tireThickness = 0.012f;
    public bool jointPlates = true;

    [Header("Spokes")]
    [Min(0)] public int spokeCount = 8;
    public float spokeWidthAtHub = 0.06f;
    public float spokeWidthAtRim = 0.048f;
    public float spokeDepthAtHub = 0.08f;
    public float spokeDepthAtRim = 0.064f;

    [Header("Hub")]
    public float hubRadius = 0.15f;
    public float hubLength = 0.5f;
    public float capRadius = 0.09f;

    [Header("Textures")]
    [Tooltip("World size covered by one tile of the wood texture.")]
    public float woodTextureWorldSize = 1.5f;
    public float ironTextureWorldSize = 0.5f;
    public int woodTextureSize = 2048;
    public int woodRings = 36;
    public int seed = 7;
    public Color woodLight = new Color(0.47f, 0.30f, 0.17f);
    public Color woodDark = new Color(0.16f, 0.085f, 0.045f);
    public Color ironColor = new Color(0.11f, 0.105f, 0.10f);
    public Color rustColor = new Color(0.33f, 0.16f, 0.07f);
}
