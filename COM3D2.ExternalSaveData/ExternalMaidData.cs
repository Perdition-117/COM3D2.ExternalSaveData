namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalMaidData : BaseExternalMaidData {
	public const string GlobalMaidGuid = "global";

	private string _guid;
	private string _lastName;
	private string _firstName;
	private string _createdTime;

	public override void Load(XmlNode xmlNode) {
		var guid = xmlNode.GetAttribute("guid");
		var lastName = xmlNode.GetAttribute("lastname");
		var firstName = xmlNode.GetAttribute("firstname");
		var createdTime = xmlNode.GetAttribute("createtime");
		SetMaid(guid, lastName, firstName, createdTime);

		LoadPlugins(xmlNode);
	}

	public override void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute("guid", _guid);
		xmlNode.SetAttribute("lastname", _lastName);
		xmlNode.SetAttribute("firstname", _firstName);
		xmlNode.SetAttribute("createtime", _createdTime);

		SavePlugins(xmlNode);
	}

	public void SetMaid(string guid, string lastName, string firstName, string createdTime) {
		_guid = guid;
		_lastName = lastName;
		_firstName = firstName;
		_createdTime = createdTime;
		_plugins.Clear();
	}

	public bool SetMaidName(string lastName, string firstName, string createdTime) {
		_lastName = lastName;
		_firstName = firstName;
		_createdTime = createdTime;
		return true;
	}
}
