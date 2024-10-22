namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalNpcMaidData : BaseExternalMaidData {
	private string _uniqueName;

	public override void Load(XmlNode xmlNode) {
		var uniqueName = xmlNode.GetAttribute("uniqueName");
		SetMaid(uniqueName);

		LoadPlugins(xmlNode);
	}

	public override void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute("uniqueName", _uniqueName);

		SavePlugins(xmlNode);
	}

	public void SetMaid(string uniqueName) {
		_uniqueName = uniqueName;
	}
}
