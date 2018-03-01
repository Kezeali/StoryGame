using UnityEngine;

[System.Serializable]
public class SceneField
{
	[SerializeField]
	private Object sceneAsset;
	[SerializeField]
	private string scenePath = "";
	public string ScenePath
	{
		get { return scenePath; }
	}
	// makes it work with the existing Unity methods (LoadLevel/LoadScene)
	public static implicit operator string(SceneField sceneField)
	{
		return sceneField.scenePath;
	}
}
