namespace CM3D2.ExternalSaveData.Managed;

static class XmlNodeExtensions {
	public static bool TryGetAttribute(this XmlNode xmlNode, string name, out string value) {
		var xmlElement = (XmlElement)xmlNode;
		value = null;
		if (xmlElement.HasAttribute(name)) {
			value = xmlElement.GetAttribute(name);
			return true;
		}
		return false;
	}

	public static string GetAttribute(this XmlNode xmlNode, string name) {
		return xmlNode.TryGetAttribute(name, out var value) ? value : null;
	}

	public static void SetAttribute(this XmlNode xmlNode, string name, string value) {
		var xmlElement = (XmlElement)xmlNode;
		xmlElement.SetAttribute(name, value);
	}

	public static XmlNode SelectOrAppendNode(this XmlNode xmlNode, string path, string prefix = null) {
		prefix ??= path;

		var node = xmlNode.SelectSingleNode(path);
		if (node == null) {
			var ownerDocument = xmlNode.OwnerDocument;
			if (xmlNode is XmlDocument document) {
				ownerDocument = document;
			}
			if (ownerDocument == null) {
				return null;
			}
			node = xmlNode.AppendChild(ownerDocument.CreateElement(prefix));
		}
		return node;
	}
}
