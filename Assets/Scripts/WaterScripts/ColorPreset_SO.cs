using UnityEngine;

[CreateAssetMenu(fileName = "color preset", menuName = "RC_Ocean/Create Color Preset")]
public class ColorPreset_SO : ScriptableObject
{
    public Gradient absorptionRamp; // 吸收渐变色
    public Gradient scatterRamp; // 散射渐变色
}