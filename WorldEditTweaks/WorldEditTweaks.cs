using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace WorldEditTweaks;
[BepInPlugin(GUID, NAME, VERSION)]
public class WorldEditTweaks : BaseUnityPlugin
{
  public const string GUID = "world_edit_tweaks";
  public const string NAME = "World Edit Tweaks";
  public const string VERSION = "1.0";
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    IsLocked = true,
    ModRequired = true
  };
  public void Awake()
  {
    new Harmony(GUID).PatchAll();
  }
  public void Start()
  {
    Patches.LoadTypes();
  }
}

[HarmonyPatch]
public class Patches
{
  static Dictionary<string, Type> Components = [];
  public static void LoadTypes()
  {
    List<Assembly> assemblies = [Assembly.GetAssembly(typeof(ZNetView)), .. Chainloader.PluginInfos.Values.Where(p => p.Instance != null).Select(p => p.Instance.GetType().Assembly)];
    var assembly = Assembly.GetAssembly(typeof(ZNetView));
    var baseType = typeof(MonoBehaviour);
    var types = assemblies.SelectMany(s =>
    {
      try
      {
        return s.GetTypes();
      }
      catch (ReflectionTypeLoadException e)
      {
        return e.Types.Where(t => t != null);
      }
    }).Where(t =>
    {
      try
      {
        return baseType.IsAssignableFrom(t);
      }
      catch
      {
        return false;
      }
    }).ToArray();
    foreach (var t in types)
      Components[t.Name] = t;
  }

  [HarmonyPatch(typeof(LocationProxy), nameof(LocationProxy.SpawnLocation)), HarmonyPostfix]
  static void LoadLocationFields(LocationProxy __instance, bool __result)
  {
    if (__result) __instance.m_nview.LoadFields();
  }

  [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> LoadFieldsBeforeScale(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      // Add new calls right before the scale check.
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ZNetView), nameof(ZNetView.m_syncInitialScale))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ZNetView), nameof(ZNetView.LoadFields))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ZNetView), nameof(ZNetView.m_syncInitialScale))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ZNetView), nameof(ZNetView.LoadFields))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
      // Remove original LoadFields call.
      .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ZNetView), nameof(ZNetView.LoadFields))))
      .Advance(-1)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .InstructionEnumeration();
  }

  [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.LoadFields)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> OverrideLoadFields(IEnumerable<CodeInstruction> instructions)
  {
    // Prefixing would add overhead to every LoadFields call, so instead add override after the first if check.
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ret))
      .Advance(2)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(LoadFieldsOverride).operand))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ret))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
      .InstructionEnumeration();
  }
  static readonly int HashComponents = "HasComponents".GetStableHashCode();
  static void LoadFieldsOverride(ZNetView view)
  {
    // Todo: This is called twice? Transpilers failing?
    var zdo = view.GetZDO();
    var components = zdo.GetString(HashComponents);
    if (components != "")
    {
      var split = components.Split(',').Select(s => s.Trim()).Where(Components.ContainsKey).Select(s => Components[s]).ToArray();
      foreach (var c in split)
      {
        if (view.GetComponent(c)) continue;
        view.gameObject.AddComponent(c);
      }
    }
    view.GetComponentsInChildren<MonoBehaviour>(ZNetView.m_tempComponents);
    foreach (var c in ZNetView.m_tempComponents)
    {
      var cName = c.GetType().Name;
      if (!zdo.GetBool("HasFields" + cName)) continue;
      var fields = c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
      foreach (var f in fields)
      {
        var t = f.FieldType;
        var h = (cName + "." + f.Name).GetStableHashCode();
        if (t == typeof(int) && zdo.GetInt(h, out var num))
          f.SetValue(c, num);
        else if (t == typeof(float) && zdo.GetFloat(h, out var fl))
          f.SetValue(c, fl);
        else if (t == typeof(bool) && zdo.GetInt(h, out num))
          f.SetValue(c, num > 0);
        else if (t == typeof(Vector3) && zdo.GetVec3(h, out var vec))
          f.SetValue(c, vec);
        else if (t == typeof(string) && zdo.GetString(h, out var str))
          f.SetValue(c, str);
        else if (t == typeof(Character.Faction) && zdo.GetInt(h, out num))
          f.SetValue(c, num);
        else if (t == typeof(GameObject) && zdo.GetString(h, out str))
        {
          if (str[0] == '/')
          {
            var child = c.transform.Find(str.Substring(1));
            if (child) f.SetValue(c, child.gameObject);
          }
          else
          {
            var p = ZNetScene.instance.GetPrefab(str);
            if (p) f.SetValue(c, p);
          }
        }
        else if (t == typeof(ItemDrop) && zdo.GetString(h, out str))
        {
          var i = ZNetScene.instance.GetPrefab(str)?.GetComponent<ItemDrop>();
          if (i) f.SetValue(c, i);
        }
        else if (t == typeof(EffectList) && zdo.GetString(h, out str))
          f.SetValue(c, Helper.ParseEffects(str));
      }
    }
  }
}
