namespace CM3D2.ExternalSaveData.Managed;

public class PluginSaveData {
	private readonly string _pluginName;

	public PluginSaveData(string pluginName) {
		_pluginName = pluginName;
	}

	public MaidSaveData GetMaidData(Maid maid) {
		return new MaidSaveData(_pluginName, maid);
	}

	public class MaidSaveData {
		private readonly string _pluginName;
		private readonly Maid _maid;

		public MaidSaveData(string pluginName, Maid maid) {
			_pluginName = pluginName;
			_maid = maid;
		}

		public string GetString(string propName, string defaultValue) => ExSaveData.Get(_maid, _pluginName, propName, defaultValue);
		public bool GetBoolean(string propName, bool defaultValue) => ExSaveData.GetBool(_maid, _pluginName, propName, defaultValue);
		public int GetInteger(string propName, int defaultValue) => ExSaveData.GetInt(_maid, _pluginName, propName, defaultValue);
		public float GetFloat(string propName, float defaultValue) => ExSaveData.GetFloat(_maid, _pluginName, propName, defaultValue);

		public void SetString(string propName, string value) => ExSaveData.Set(_maid, _pluginName, propName, value);
		public void SetBoolean(string propName, bool value) => ExSaveData.SetBool(_maid, _pluginName, propName, value);
		public void SetInteger(string propName, int value) => ExSaveData.SetInt(_maid, _pluginName, propName, value);
		public void SetFloat(string propName, float value) => ExSaveData.SetFloat(_maid, _pluginName, propName, value);

		public void Remove(string propName) => ExSaveData.Remove(_maid, _pluginName, propName);
	}
}
