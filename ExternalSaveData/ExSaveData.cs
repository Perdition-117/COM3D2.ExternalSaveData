using BepInEx;
using BepInEx.Logging;
using CM3D2.ExternalSaveData.Managed.GameMainCallbacks;
using ExternalSaveData;
using HarmonyLib;

namespace CM3D2.ExternalSaveData.Managed;

[BepInPlugin("COM3D2.ExternalSaveData", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ExSaveData : BaseUnityPlugin {
	private static ExternalSaveData _saveData = new();

	// 通常のプラグイン名よりも前に処理するため、先頭に '.' をつけている。
	// 通常のプラグインは "CM3D2～" のように英数字から始まる名前をつけること
	private const string CallbackName = ".CM3D2 ExternalSaveData";

	private static ManualLogSource _logger;

	private void Awake() {
		_logger = Logger;

		Harmony.CreateAndPatchAll(typeof(ExSaveData));

		Harmony.CreateAndPatchAll(typeof(Deserialize));
		Harmony.CreateAndPatchAll(typeof(Serialize));
		Harmony.CreateAndPatchAll(typeof(DeleteSerializeData));

		Deserialize.Callbacks.Add(CallbackName, OnDeserialize);
		Serialize.Callbacks.Add(CallbackName, OnSerialize);
		DeleteSerializeData.Callbacks.Add(CallbackName, OnDeleteSerializeData);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CharacterMgr.NpcData), nameof(CharacterMgr.NpcData.Apply))]
	private static void Apply(CharacterMgr.NpcData __instance, Maid maid) {
		if (GameMain.Instance.CharacterMgr != null && maid != null && !maid.boMAN) {
			_logger.LogDebug($"Applying NPC preset {__instance.presetFileName} ({maid.status.guid})...");
			_saveData.NpcGuids[maid.status.guid] = __instance.uniqueName;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.Deactivate))]
	private static void Deactivate(CharacterMgr __instance, int f_nActiveSlotNo, bool f_bMan) {
		if (f_nActiveSlotNo != -1 && !f_bMan && __instance.m_objActiveMaid[f_nActiveSlotNo] && __instance.m_gcActiveMaid[f_nActiveSlotNo] is Maid maid) {
			if (_saveData.NpcGuids.ContainsKey(maid.status.guid)) {
				_logger.LogDebug($"Deactivating NPC maid {maid.status.guid}...");
				_saveData.NpcGuids.Remove(maid.status.guid);
			}
		}
	}

	internal static void LogError(object data) {
		_logger.LogError(data);
	}

	private static BaseExternalMaidData GetMaidData(Maid maid) {
		return _saveData.GetMaidData(maid.status.guid);
	}

	private static ExternalPluginData GetMaidPluginData(Maid maid, string pluginName) {
		return GetMaidData(maid)?.GetPluginData(pluginName);
	}

	private static ExternalPluginData GetMaidPluginData(string pluginName) {
		return _saveData.GetMaidData(ExternalMaidData.GlobalMaidGuid)?.GetPluginData(pluginName);
	}

	public static bool TrySavePluginData(Maid maid, string pluginName, XmlNode xmlNode) {
		if (GetMaidPluginData(maid, pluginName) is ExternalPluginData plugin) {
			plugin.Save(xmlNode);
			return true;
		}
		return false;
	}

	public static void LoadPluginData(Maid maid, string pluginName, XmlNode xmlNode) {
		if (!Contains(maid)) {
			SetMaid(maid);
		}

		if (GetMaidData(maid) is BaseExternalMaidData maidSaveData) {
			maidSaveData.LoadPlugin(pluginName, xmlNode);
		}
	}

	/// <summary>
	/// 拡張セーブデータ内の設定を得る(文字列)
	/// <para>指定した設定が存在しない場合はdefaultValueを返す</para>
	/// <seealso cref="GetBool"/>
	/// <seealso cref="GetInt"/>
	/// <seealso cref="GetFloat"/>
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="defaultValue">プロパティが存在しない場合に返すデフォルト値</param>
	/// <returns>設定文字列</returns>
	public static string Get(Maid maid, string pluginName, string propName, string defaultValue) {
		if (maid == null || pluginName == null || propName == null) {
			return defaultValue;
		}
		if (!Contains(maid)) {
			SetMaid(maid);
		}
		return GetMaidPluginData(maid, pluginName)?.GetPropertyValue(propName) ?? defaultValue;
	}

	private static bool StringToBool(string s, bool defaultValue) {
		if (s == null) {
			return defaultValue;
		}
		if (bool.TryParse(s, out var v)) {
			return v;
		}
		if (float.TryParse(s, out var f)) {
			return f > 0.5f;
		}
		if (int.TryParse(s, out var i)) {
			return i > 0;
		}
		return defaultValue;
	}

	private static int StringToInt(string s, int defaultValue) {
		return s != null && int.TryParse(s, out var v) ? v : defaultValue;
	}

	private static float StringToFloat(string s, float defaultValue) {
		return s != null && float.TryParse(s, out var v) ? v : defaultValue;
	}

	public static bool GetBool(Maid maid, string pluginName, string propName, bool defaultValue) {
		return StringToBool(Get(maid, pluginName, propName, null), defaultValue);
	}

	public static int GetInt(Maid maid, string pluginName, string propName, int defaultValue) {
		return StringToInt(Get(maid, pluginName, propName, null), defaultValue);
	}

	public static float GetFloat(Maid maid, string pluginName, string propName, float defaultValue) {
		return StringToFloat(Get(maid, pluginName, propName, null), defaultValue);
	}

	/// <summary>
	/// 拡張セーブデータへ設定を書き込む
	/// <seealso cref="SetBool"/>
	/// <seealso cref="SetInt"/>
	/// <seealso cref="SetFloat"/>
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <param name="overwrite">trueなら常に上書き。falseなら設定が存在する場合は書き込みを行わない</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool Set(Maid maid, string pluginName, string propName, string value, bool overwrite) {
		if (maid == null || pluginName == null || propName == null) {
			return false;
		}
		if (!Contains(maid)) {
			SetMaid(maid);
		}
		if (!overwrite && Contains(maid, pluginName, propName)) {
			return false;
		}
		return GetMaidData(maid)?.SetPropertyValue(pluginName, propName, value) ?? false;
	}

	public static bool SetBool(Maid maid, string pluginName, string propName, bool value, bool overwrite) {
		return Set(maid, pluginName, propName, value.ToString(), overwrite);
	}

	public static bool SetInt(Maid maid, string pluginName, string propName, int value, bool overwrite) {
		return Set(maid, pluginName, propName, value.ToString(), overwrite);
	}

	public static bool SetFloat(Maid maid, string pluginName, string propName, float value, bool overwrite) {
		return Set(maid, pluginName, propName, value.ToString(), overwrite);
	}

	/// <summary>
	/// 拡張セーブデータへ設定を書き込む(常に上書き)
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool Set(Maid maid, string pluginName, string propName, string value) {
		return Set(maid, pluginName, propName, value, true);
	}

	public static bool SetBool(Maid maid, string pluginName, string propName, bool value) {
		return SetBool(maid, pluginName, propName, value, true);
	}

	public static bool SetInt(Maid maid, string pluginName, string propName, int value) {
		return SetInt(maid, pluginName, propName, value, true);
	}

	public static bool SetFloat(Maid maid, string pluginName, string propName, float value) {
		return SetFloat(maid, pluginName, propName, value, true);
	}

	/// <summary>
	/// 拡張セーブデータ内の設定を削除
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:削除に成功(設定が存在し、それを削除した)。false:失敗(設定が存在しないか、何らかのエラー)</returns>
	public static bool Remove(Maid maid, string pluginName, string propName) {
		if (maid == null || pluginName == null || propName == null) {
			return false;
		}
		return GetMaidPluginData(maid, pluginName)?.RemoveProperty(propName) ?? false;
	}

	/// <summary>
	/// 拡張セーブデータ内の設定の存在を調査
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:設定が存在。false:存在しない</returns>
	public static bool Contains(Maid maid, string pluginName, string propName) {
		if (maid == null || pluginName == null || propName == null) {
			return false;
		}
		if (!Contains(maid)) {
			SetMaid(maid);
		}
		return GetMaidPluginData(maid, pluginName)?.HasProperty(propName) ?? false;
	}

	/// <summary>
	/// 拡張セーブデータ内のメイドの存在を確認
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <returns>true:指定したメイドが存在する。false:存在しない</returns>
	public static bool Contains(Maid maid) {
		return maid != null && _saveData.HasMaid(maid.status.guid);
	}

	/// <summary>
	/// 拡張セーブデータへメイドを追加
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <returns></returns>
	public static void SetMaid(Maid maid) {
		if (maid == null) {
			return;
		}
		var status = maid.status;
		_saveData.SetMaid(status.guid, status.lastName, status.firstName, status.creationTime);
	}

	/// <summary>
	/// 与えたGUIDを持たないメイドを拡張セーブデータから削除
	/// </summary>
	/// <param name="guids">メイドGUIDリスト</param>
	/// <returns></returns>
	public static void CleanupMaids(List<string> guids) {
		_saveData.Cleanup(guids);
	}

	public static void CleanupMaids() {
		var guids = new List<string>();
		var cm = GameMain.Instance.CharacterMgr;
		for (var i = 0; i < cm.GetStockMaidCount(); i++) {
			var maid = cm.GetStockMaid(i);
			guids.Add(maid.status.guid);
		}
		CleanupMaids(guids);
	}

	/// <summary>
	/// 拡張セーブデータ内のグローバル設定を得る(文字列)
	/// <para>指定した設定が存在しない場合はdefaultValueを返す</para>
	/// <seealso cref="GlobalGetBool"/>
	/// <seealso cref="GlobalGetInt"/>
	/// <seealso cref="GlobalGetFloat"/>
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="defaultValue">プロパティが存在しない場合に返すデフォルト値</param>
	/// <returns>設定文字列</returns>
	public static string GlobalGet(string pluginName, string propName, string defaultValue) {
		if (pluginName == null || propName == null) {
			return defaultValue;
		}
		return GetMaidPluginData(pluginName)?.GetPropertyValue(propName) ?? defaultValue;
	}

	public static bool GlobalGetBool(string pluginName, string propName, bool defaultValue) {
		return StringToBool(GlobalGet(pluginName, propName, null), defaultValue);
	}

	public static int GlobalGetInt(string pluginName, string propName, int defaultValue) {
		return StringToInt(GlobalGet(pluginName, propName, null), defaultValue);
	}

	public static float GlobalGetFloat(string pluginName, string propName, float defaultValue) {
		return StringToFloat(GlobalGet(pluginName, propName, null), defaultValue);
	}

	/// <summary>
	/// 拡張セーブデータへグローバル設定を書き込む
	/// <seealso cref="GlobalSetBool"/>
	/// <seealso cref="GlobalSetInt"/>
	/// <seealso cref="GlobalSetFloat"/>
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <param name="overwrite">trueなら常に上書き。falseなら設定が存在する場合は書き込みを行わない</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool GlobalSet(string pluginName, string propName, string value, bool overwrite) {
		if (pluginName == null || propName == null) {
			return false;
		}
		if (!overwrite && GlobalContains(pluginName, propName)) {
			return false;
		}
		return _saveData.GetMaidData(ExternalMaidData.GlobalMaidGuid)?.SetPropertyValue(pluginName, propName, value) ?? false;
	}

	public static bool GlobalSetBool(string pluginName, string propName, bool value, bool overwrite) {
		return GlobalSet(pluginName, propName, value.ToString(), overwrite);
	}

	public static bool GlobalSetInt(string pluginName, string propName, int value, bool overwrite) {
		return GlobalSet(pluginName, propName, value.ToString(), overwrite);
	}

	public static bool GlobalSetFloat(string pluginName, string propName, float value, bool overwrite) {
		return GlobalSet(pluginName, propName, value.ToString(), overwrite);
	}

	/// <summary>
	/// 拡張セーブデータへグローバル設定を書き込む(常に上書き)
	/// <seealso cref="GlobalSetBool"/>
	/// <seealso cref="GlobalSetInt"/>
	/// <seealso cref="GlobalSetFloat"/>
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool GlobalSet(string pluginName, string propName, string value) {
		return GlobalSet(pluginName, propName, value, true);
	}

	public static bool GlobalSetBool(string pluginName, string propName, bool value) {
		return GlobalSetBool(pluginName, propName, value, true);
	}

	public static bool GlobalSetInt(string pluginName, string propName, int value) {
		return GlobalSetInt(pluginName, propName, value, true);
	}

	public static bool GlobalSetFloat(string pluginName, string propName, float value) {
		return GlobalSetFloat(pluginName, propName, value, true);
	}

	/// <summary>
	/// 拡張セーブデータ内のグローバル設定を削除
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:削除に成功(設定が存在し、それを削除した)。false:失敗(設定が存在しないか、何らかのエラー)</returns>
	public static bool GlobalRemove(string pluginName, string propName) {
		if (pluginName == null || propName == null) {
			return false;
		}
		return GetMaidPluginData(pluginName)?.RemoveProperty(propName) ?? false;
	}

	/// <summary>
	/// 拡張セーブデータ内のグローバル設定の存在を調査
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:設定が存在する。false:存在しない</returns>
	public static bool GlobalContains(string pluginName, string propName) {
		if (pluginName == null || propName == null) {
			return false;
		}
		return GetMaidPluginData(pluginName)?.HasProperty(propName) ?? false;
	}

	/// <summary>
	/// セーブファイル番号から拡張セーブデータのファイル名を生成
	/// </summary>
	/// <param name="that">GameMainインスタンス</param>
	/// <param name="f_nSaveNo">セーブファイル番号</param>
	/// <returns>拡張セーブデータのファイル名</returns>
	public static string makeXmlFilename(GameMain that, int f_nSaveNo) {
		return GetExternalSaveDataPath(f_nSaveNo);
	}

	/// <summary>
	/// セーブファイル番号からセーブデータのファイル名を生成 (GameMain.MakeSavePathFileNameを呼び出す)
	/// </summary>
	/// <param name="that">GameMainインスタンス</param>
	/// <param name="f_nSaveNo">セーブファイル番号</param>
	/// <returns>セーブデータのファイル名</returns>
	public static string GameMainMakeSavePathFileName(GameMain that, int f_nSaveNo) {
		return GetSaveDataPath(f_nSaveNo);
	}

	private static string GetSaveDataPath(int saveIndex) {
		return GameMain.Instance.MakeSavePathFileName(saveIndex);
	}

	private static string GetExternalSaveDataPath(int saveIndex) {
		return GetSaveDataPath(saveIndex) + ".exsave.xml";
	}

	private static bool SetMaidName(Maid maid) {
		if (maid == null) {
			return false;
		}
		var status = maid.status;
		return _saveData.SetMaidName(status.guid, status.lastName, status.firstName, status.creationTime);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GameMain), nameof(GameMain.OnInitialize))]
	public static void DummyInitialize(GameMain __instance) {
		// static class の生成を強要するためのダミーメソッド
		//
		// C# の static class の内容はクラスへの初アクセス時に遅延生成される。
		// このため、ExSaveData対応プラグインが１つも無い場合、
		// ExSaveDataのコンストラクタが呼ばれず、コールバックの設定ができない。
		//
		// このメソッドはこれを回避するためのダミーメソッドで、
		// GameMain.OnInitializeの末尾から呼び出される。
		// 
		// 本当はアトリビュート等でもっと良いやり方があるはずなんだろうけど、
		// 分かっていないのでとりあえずこのまま。
	}

	private static void OnDeserialize(GameMain __instance, int f_nSaveNo) {
		if (f_nSaveNo == -1) {
			return;
		}

		var xmlFilePath = GetExternalSaveDataPath(f_nSaveNo);
		_saveData = new();
		if (File.Exists(xmlFilePath)) {
			_saveData.Load(xmlFilePath);
		}
	}

	private static void OnSerialize(GameMain __instance, int f_nSaveNo, string f_strComment) {
		if (f_nSaveNo == -1) {
			return;
		}

		var cm = GameMain.Instance.CharacterMgr;
		for (var i = 0; i < cm.GetStockMaidCount(); i++) {
			var maid = cm.GetStockMaid(i);
			SetMaidName(maid);
		}
		CleanupMaids();
		var path = GetSaveDataPath(f_nSaveNo);
		var xmlFilePath = GetExternalSaveDataPath(f_nSaveNo);
		_saveData.Save(xmlFilePath, path);
	}

	private static void OnDeleteSerializeData(GameMain __instance, int f_nSaveNo) {
		var xmlFilePath = GetExternalSaveDataPath(f_nSaveNo);
		if (File.Exists(xmlFilePath)) {
			File.Delete(xmlFilePath);
		}
	}
}
