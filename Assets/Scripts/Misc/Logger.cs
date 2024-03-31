using System;
using System.Collections.Generic;
using UnityEngine;
using Text = TMPro.TextMeshProUGUI;

namespace Wozware.Downslope
{
	public class Logger : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members

		public static Logger Instance;

		public bool LoggerObjectEnabled;

		public int MaxMsgs = 24;

		public Transform MessageContentParent;
		public GameObject MessagePrefab;
		public Text MessagePrefabTime;
		public Text MessagePrefabLabel;

		#endregion

		#region Private Members

		#endregion

		#region Public Methods

		public void LogMsg(string owner, string msg)
		{
			if(LoggerObjectEnabled)
			{
				MessagePrefabLabel.text = $"{owner}:\n{msg}";
				MessagePrefabTime.text = DateTime.Now.ToString();
				GameObject g = Instantiate(MessagePrefab, MessageContentParent);
				g.SetActive(true);
				//g.transform.SetAsFirstSibling();
			}

			Util.Log(msg, owner);
		}


		#endregion

		#region Private Methods

		#endregion

		#region Unity Methods

		private void Awake()
		{
			if(Instance != null)
			{
				Debug.LogError("Only one Logger instance can exist. Destroying the other.");
				Destroy(Instance.gameObject);
			}

			Instance = this;
		}

		private void Start()
		{

		}

		private void Update()
		{

		}

		#endregion
	}
}

