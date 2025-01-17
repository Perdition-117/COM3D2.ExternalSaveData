using System.Linq;

namespace CM3D2.ExternalSaveData.Managed;

internal class MaidDataManager<T> where T : BaseExternalMaidData, new() {
	private Dictionary<string, T> _maids = new();
	private readonly string _xpath;
	private readonly string _attributeName;

	public MaidDataManager(string xpath, string attributeName) {
		_xpath = xpath;
		_attributeName = attributeName;
	}

	public bool HasMaid(string guid) => _maids.ContainsKey(guid);

	public bool TryGetMaid(string key, out T maid) {
		return _maids.TryGetValue(key, out maid);
	}

	public T AddMaid(string key) => _maids[key] = new();

	public void Cleanup(List<string> guids) {
		_maids = _maids.Where(kv => guids.Contains(kv.Key) || kv.Key == ExternalMaidData.GlobalMaidGuid).ToDictionary(kv => kv.Key, kv => kv.Value);
	}

	public void LoadMaids(XmlNode xmlNode) {
		foreach (XmlNode node in xmlNode.SelectNodes($"{_xpath}/maid")) {
			if (node.TryGetAttribute(_attributeName, out var key)) {
				var maid = new T();
				_maids[key] = maid;
				_maids[key].Load(node);
			}
		}
	}

	public void SaveMaids(XmlNode xmlNode) {
		var maidCollectionNode = xmlNode.SelectOrAppendNode(_xpath);

		foreach (XmlNode node in maidCollectionNode.SelectNodes("maid")) {
			if (!(node.TryGetAttribute(_attributeName, out var uniqueName) && _maids.ContainsKey(uniqueName))) {
				maidCollectionNode.RemoveChild(node);
			}
		}

		foreach (var kv in _maids.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)) {
			var node = maidCollectionNode.SelectOrAppendNode($"maid[@{_attributeName}='{kv.Key}']", "maid");
			kv.Value.Save(node);
		}
	}
}
