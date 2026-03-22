namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalNpcMaidData : BaseExternalMaidData {
	private string _presetName;

	public override void Load(XmlNode xmlNode) {
		var presetName = xmlNode.GetAttribute("presetName");
		SetMaid(presetName);

		LoadPlugins(xmlNode);
	}

	public override void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute("presetName", _presetName);

		SavePlugins(xmlNode);
	}

	public void SetMaid(string presetName) {
		_presetName = presetName;
	}
}
