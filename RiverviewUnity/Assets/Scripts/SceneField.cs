using UnityEngine;

[System.Serializable]
public struct SceneField
{
	public string scenePath;

	// makes it work with the existing Unity methods (LoadLevel/LoadScene)
	public static implicit operator string(SceneField sceneField)
	{
		return sceneField.scenePath;
	}

	public override string ToString()
	{
		return this.scenePath;
	}
}
