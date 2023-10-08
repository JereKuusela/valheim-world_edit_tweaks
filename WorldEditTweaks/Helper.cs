

using System.Linq;

namespace WorldEditTweaks;

public class Helper
{
  public static EffectList ParseEffects(string data)
  {
    var effects = data.Split(',').Select(effect => ParseEffect(effect)!).Where(effect => effect != null);
    return new()
    {
      m_effectPrefabs = effects.ToArray()
    };
  }
  public static EffectList.EffectData? ParseEffect(string data)
  {
    var prefab = ZNetScene.instance.GetPrefab(data);
    if (!prefab) return null;
    return new()
    {
      m_prefab = ZNetScene.instance.GetPrefab(data)
    };
  }
}