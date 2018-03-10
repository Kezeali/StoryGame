using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cloverview;

namespace Cloverview
{

public class PlanOptionCategoryUI : MonoBehaviour
{
	[SerializeField]
	public Transform optionsContainer;
	[SerializeField]
	public Button toggle;
	[SerializeField]
	public Text title;

	public Animator animator;

	[System.NonSerialized]
	public SubjectData subject;
	[System.NonSerialized]
	public List<PlanOptionUI> optionUis = new List<PlanOptionUI>();

	public void OnEnable()
	{
		Debug.Assert(this.toggle != null);

		this.toggle.onClick.AddListener(Toggle);
	}

	public void Toggle()
	{
		animator.SetBool("open", !animator.GetBool("open"));
	}

	public void Clear()
	{
		for (int i = 0; i < this.optionUis.Count; ++i)
		{
			Object.Destroy(this.optionUis[i].gameObject);
		}
		this.optionUis.Clear();
		this.subject = null;
	}
}

}
