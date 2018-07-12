using UnityEngine;

namespace Cloverview
{

[System.Serializable]
public abstract class DataItem : IDataItem
{
	public string name { get { return m_name; } set { m_name = value; } }
	[SerializeField]
	private string m_name;
}

public interface IDataItem
{
	string name { get; set; }
}

}
