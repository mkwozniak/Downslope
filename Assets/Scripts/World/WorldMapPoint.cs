using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wozware.Downslope;
using System;
using UnityEngine.Events;

public class WorldMapPoint : MonoBehaviour
{
	public event Action<WorldMapPoint> HoverEnterEventHandler;
	public event Action HoverExitEventHandler;

	public MapPointTypes MapPointType;
    public Animator Anim;
    public SpriteRenderer Sprite;
    public Color PointColor;
	public string MapID;

	public UnityEvent OnClickEvents;

	private void Start()
    {
        if(Sprite != null)
        {
			Sprite.color = PointColor;
		}

        UpdateAnimatorTypeValues(MapPointType);
	}

    private void UpdateAnimatorTypeValues(MapPointTypes type)
    {
        int typeInt = 0;

        switch(type)
        {
			case MapPointTypes.Custom:
                typeInt = 1;
				break;
			case MapPointTypes.Town:
                typeInt = 2;
				break;
		}

		Anim.SetInteger("MapType", typeInt);
	}

	public void OnMouseUp()
	{
		
	}

	public void OnMouseEnter()
	{
        if (HoverEnterEventHandler != null)
			HoverEnterEventHandler(this);
	}

	public void OnMouseExit()
	{
		if (HoverExitEventHandler != null)
			HoverExitEventHandler();
	}

}
