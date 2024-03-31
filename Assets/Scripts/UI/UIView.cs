using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	public sealed class UIView : MonoBehaviour
	{
		public GameObject Root;
		public UIViewTypes Type;
		public Action<UIViewTypes> OnSwitchTo;

		public void Show(bool show)
		{
			Root.SetActive(show);
		}

		public void SwitchTo()
		{
			Debug.Log("???????");
			OnSwitchTo(Type);
		}
	}
}

