using System.Linq;

namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalSaveData {
	public ExternalSaveData() {
		SetMaid(ExternalMaidData.GlobalMaidGuid, "", "", "");
	}

	internal Dictionary<string, ExternalMaidData> Maids { get; private set; } = new();

	public void Load(string xmlFilePath) {
		var xml = LoadXmlDocument(xmlFilePath);
		var xmlNode = xml.SelectSingleNode("/savedata");

		foreach (XmlNode node in xmlNode.SelectNodes("maids/maid")) {
			if (node.TryGetAttribute("guid", out var guid)) {
				Maids[guid] = new ExternalMaidData().Load(node);
			}
		}
	}

	private static XmlDocument LoadXmlDocument(string xmlFilePath) {
		var document = new XmlDocument();
		if (File.Exists(xmlFilePath)) {
			document.Load(xmlFilePath);
		}
		return document;
	}

	public void Save(string xmlFilePath, string targetSaveDataFileName) {
		var document = LoadXmlDocument(xmlFilePath);

		var xmlNode = document.SelectOrAppendNode("savedata");
		xmlNode.SetAttribute("target", targetSaveDataFileName);
		var xmlMaids = xmlNode.SelectOrAppendNode("maids", "maids");

		// 存在しない<maid>を削除
		foreach (XmlNode node in xmlMaids.SelectNodes("maid")) {
			if (!(node.TryGetAttribute("guid", out var guid) && Maids.ContainsKey(guid))) {
				xmlMaids.RemoveChild(node);
			}
		}

		foreach (var kv in Maids.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)) {
			var node = xmlMaids.SelectOrAppendNode($"maid[@guid='{kv.Key}']", "maid");
			kv.Value.Save(node);
		}

		document.Save(xmlFilePath);
	}

	public void Cleanup(List<string> guids) {
		Maids = Maids.Where(kv => guids.Contains(kv.Key) || kv.Key == ExternalMaidData.GlobalMaidGuid).ToDictionary(kv => kv.Key, kv => kv.Value);
	}

	private bool TryGetValue(string guid, out ExternalMaidData maid) {
		return Maids.TryGetValue(guid, out maid);
	}

	public void SetMaid(string guid, string lastName, string firstName, string createTime) {
		if (!TryGetValue(guid, out var maid)) {
			maid = new();
			Maids[guid] = maid;
		}
		maid.SetMaid(guid, lastName, firstName, createTime);
	}

	public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
		return TryGetValue(guid, out var maid) && maid.SetMaidName(lastName, firstName, createTime);
	}

	public bool ContainsMaid(string guid) => Maids.ContainsKey(guid);

	public bool Contains(string guid, string pluginName, string propName) {
		return TryGetValue(guid, out var maid) && maid.Contains(pluginName, propName);
	}

	public string Get(string guid, string pluginName, string propName, string defaultValue) {
		return TryGetValue(guid, out var maid) ? maid.Get(pluginName, propName, defaultValue) : defaultValue;
	}

	public bool Set(string guid, string pluginName, string propName, string value) {
		return TryGetValue(guid, out var maid) && maid.Set(pluginName, propName, value);
	}

	public bool Remove(string guid, string pluginName, string propName) {
		return TryGetValue(guid, out var maid) && maid.Remove(pluginName, propName);
	}
}
