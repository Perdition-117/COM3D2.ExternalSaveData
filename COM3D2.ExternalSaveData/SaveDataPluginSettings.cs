namespace CM3D2.ExternalSaveData.Managed;

internal class SaveDataPluginSettings {
	public const string GlobalMaidGuid = "global";

	internal ExternalSaveData saveData = new();

	public SaveDataPluginSettings Load(string xmlFilePath) {
		var xml = LoadXmlDocument(xmlFilePath);
		saveData = new ExternalSaveData().Load(xml.SelectSingleNode("/savedata"));
		return this;
	}

	public void Save(string xmlFilePath, string targetSaveDataFileName) {
		var xml = LoadXmlDocument(xmlFilePath);
		saveData.SaveFileName = targetSaveDataFileName;

		var xmlSaveData = xml.SelectOrAppendNode("savedata", "savedata");
		saveData.Save(xmlSaveData);
		xml.Save(xmlFilePath);
	}

	private static XmlDocument LoadXmlDocument(string xmlFilePath) {
		var xml = new XmlDocument();
		if (File.Exists(xmlFilePath)) {
			xml.Load(xmlFilePath);
		}
		return xml;
	}

	public bool Contains(string guid, string pluginName, string propName) {
		return saveData.Contains(guid, pluginName, propName);
	}

	public string Get(string guid, string pluginName, string propName, string defaultValue) {
		return saveData.Get(guid, pluginName, propName, defaultValue);
	}

	public bool Set(string guid, string pluginName, string propName, string value) {
		return saveData.Set(guid, pluginName, propName, value);
	}

	public bool Remove(string guid, string pluginName, string propName) {
		return saveData.Remove(guid, pluginName, propName);
	}

	public bool ContainsMaid(string guid) => saveData.ContainsMaid(guid);

	public void SetMaid(string guid, string lastName, string firstName, string createTime) {
		saveData.SetMaid(guid, lastName, firstName, createTime);
	}

	public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
		return saveData.SetMaidName(guid, lastName, firstName, createTime);
	}

	public void Cleanup(List<string> guids) {
		saveData.Cleanup(guids);
	}
}
