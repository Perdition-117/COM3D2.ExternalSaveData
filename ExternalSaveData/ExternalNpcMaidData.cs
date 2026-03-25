namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalNpcMaidData : BaseExternalMaidData {
	public const string SelectorAttribute = "presetName";

	private string _presetName;

	public override void Load(XmlNode xmlNode) {
		var presetName = xmlNode.GetAttribute(SelectorAttribute);
		SetMaid(presetName);

		LoadPlugins(xmlNode);
	}

	public override void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute(SelectorAttribute, _presetName);

		SavePlugins(xmlNode);
	}

	public void SetMaid(string presetName) {
		_presetName = presetName;
	}
}
