using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining one cutting pattern.
/// Each pattern has a list of slice lines — each defined by a position
/// and rotation within the overlay panel (normalized 0-1 space).
///
/// Create via: Assets → Create → Kuloniku → Slice Pattern
///
/// Recommended: create 3 assets (Pattern_A, Pattern_B, Pattern_C).
/// Each ingredient picks one randomly when placed on the board.
/// </summary>
[CreateAssetMenu(fileName = "SlicePattern", menuName = "Kuloniku/Slice Pattern")]
public class SlicePatternData : ScriptableObject
{
    [System.Serializable]
    public class SliceLine
    {
        [Tooltip("Center position of this line within the panel (0,0 = bottom-left, 1,1 = top-right).")]
        public Vector2 normalizedCenter = new Vector2(0.5f, 0.5f);

        [Tooltip("Rotation of the line in degrees (0 = horizontal, 90 = vertical).")]
        public float rotationDegrees = 0f;

        [Tooltip("Length of the dotted line as a fraction of the panel width.")]
        [Range(0.1f, 1f)]
        public float length = 0.7f;
    }

    [Header("Lines in this pattern")]
    public List<SliceLine> lines = new();

    [Header("Display name (shown in UI)")]
    public string patternName = "Pattern A";
}