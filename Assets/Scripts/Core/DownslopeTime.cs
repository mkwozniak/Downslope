using System.Collections.Generic;
using UnityEngine;

public class DownslopeTime
{
	#region Events

	#endregion

	#region Public Members

	public static float LocalTimeScale = 1f;

	public static float DeltaTime
	{
		get
		{
			return Time.deltaTime * LocalTimeScale;
		}
	}

	public static float TimeScale
	{
		get
		{
			return Time.timeScale * LocalTimeScale;
		}
	}

	#endregion
	
	#region Private Members
	
	#endregion
	
	#region Public Methods

	

	
	#endregion
	
	#region Private Methods
	
	#endregion
}
