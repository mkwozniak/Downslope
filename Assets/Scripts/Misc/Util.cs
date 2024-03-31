using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace Wozware.Downslope
{
	public static class Util
	{
		public static void Log(string msg, string owner)
		{
			Debug.Log($"[{owner}]: {msg}");
		}

		public static string GetColorTag(Color color)
		{
			return $"<color=#{color.ToHexString()}>";
		}

		public static string EndColorTag
		{
			get 
			{
				return "</color>";
			}
		}
	}
}

