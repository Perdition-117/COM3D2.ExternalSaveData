using System;
using HarmonyLib;

namespace CM3D2.ExternalSaveData.Managed.GameMainCallbacks;

public static class Deserialize {
	public delegate void Callback(GameMain that, int f_nSaveNo);
	public static SortedDictionary<string, Callback> Callbacks = new();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameMain), nameof(GameMain.Deserialize))]
	public static void Invoke(GameMain __instance, int f_nSaveNo, bool scriptExec = true) {
		try {
			foreach (var callback in Callbacks.Values) {
				callback(__instance, f_nSaveNo);
			}
		} catch (Exception e) {
			ExSaveData.LogError(e);
		}
	}
}

public static class Serialize {
	public delegate void Callback(GameMain that, int f_nSaveNo, string f_strComment);
	public static SortedDictionary<string, Callback> Callbacks = new();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameMain), nameof(GameMain.Serialize))]
	public static void Invoke(GameMain __instance, int f_nSaveNo, string f_strComment) {
		try {
			foreach (var callback in Callbacks.Values) {
				callback(__instance, f_nSaveNo, f_strComment);
			}
		} catch (Exception e) {
			ExSaveData.LogError(e);
		}
	}
}

public static class DeleteSerializeData {
	public delegate void Callback(GameMain that, int f_nSaveNo);
	public static SortedDictionary<string, Callback> Callbacks = new();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameMain), nameof(GameMain.DeleteSerializeData))]
	public static void Invoke(GameMain __instance, int f_nSaveNo) {
		try {
			foreach (var callback in Callbacks.Values) {
				callback(__instance, f_nSaveNo);
			}
		} catch (Exception e) {
			ExSaveData.LogError(e);
		}
	}
}
