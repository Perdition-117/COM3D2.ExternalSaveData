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

	private bool TryGetMaid(string guid, out BaseExternalMaidData maidData) {
		maidData = null;
		if (_maidDataManager.TryGetMaid(guid, out var maid)) {
			maidData = maid;
			return true;
		}
		return false;
	}

	public void SetMaid(string guid, string lastName, string firstName, string createTime) {
		if (!_maidDataManager.TryGetMaid(guid, out var maid)) {
			maid = _maidDataManager.AddMaid(guid);
		}
		maid.SetMaid(guid, lastName, firstName, createTime);
	}

	public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
		return _maidDataManager.TryGetMaid(guid, out var maid) && maid.SetMaidName(lastName, firstName, createTime);
	}

	public bool HasMaid(string guid) {
		return _maidDataManager.HasMaid(guid);
	}

	public BaseExternalMaidData GetMaidData(string guid) {
		return TryGetMaid(guid, out var maid) ? maid : default;
	}
}
