namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalSaveData {
	private readonly MaidDataManager<ExternalMaidData> _maidDataManager = new("maids", "guid");

	public ExternalSaveData() {
		SetMaid(ExternalMaidData.GlobalMaidGuid, "", "", "");
	}

	public void Load(string xmlFilePath) {
		var xml = LoadXmlDocument(xmlFilePath);
		var xmlNode = xml.SelectSingleNode("/savedata");

		_maidDataManager.LoadMaids(xmlNode);
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

		_maidDataManager.SaveMaids(xmlNode);

		document.Save(xmlFilePath);
	}

	public void Cleanup(List<string> guids) {
		_maidDataManager.Cleanup(guids);
	}

	public bool TryGetValue(string guid, out BaseExternalMaidData maidData) {
		maidData = null;
		if (_maidDataManager.TryGetValue(guid, out var maid)) {
			maidData = maid;
			return true;
		}
		return false;
	}

	public void SetMaid(string guid, string lastName, string firstName, string createTime) {
		if (!_maidDataManager.TryGetValue(guid, out var maid)) {
			maid = _maidDataManager.Add(guid);
		}
		maid.SetMaid(guid, lastName, firstName, createTime);
	}

	public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
		return _maidDataManager.TryGetValue(guid, out var maid) && maid.SetMaidName(lastName, firstName, createTime);
	}

	public bool ContainsMaid(string guid) {
		return _maidDataManager.ContainsKey(guid);
	}

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
