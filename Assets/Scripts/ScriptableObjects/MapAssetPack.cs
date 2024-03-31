using System.Collections.Generic;
using UnityEngine;


namespace Wozware.Downslope
{
	[CreateAssetMenu(fileName = "MapAssetPack", menuName = "Downslope/MapAssetPack", order = 2)]
	public class MapAssetPack : ScriptableObject
	{
		public List<SlopeAngleData> SlopeAngleDataList;
		public WeightedMapData ClearMapData;
		public List<WeightedMapData> MenuMapList;
		public List<WeightedMapData> TutorialMapList;
		public List<WeightedMapData> BeginnerArcadeMapList;

		public Dictionary<string, WeightedMapData> ArcadeModeMaps;
		public Dictionary<SlopeAngleTypes, SlopeAngleData> SlopeAngleData;

		public void Initialize()
		{
			ArcadeModeMaps = new();
			SlopeAngleData = new();

			int i = 0;
			for (i = 0; i < BeginnerArcadeMapList.Count; i++)
			{
				ArcadeModeMaps.Add(BeginnerArcadeMapList[i].Name, BeginnerArcadeMapList[i]);
			}

			for (i = 0; i < SlopeAngleDataList.Count; i++)
			{
				SlopeAngleData.Add(SlopeAngleDataList[i].AngleType, SlopeAngleDataList[i]);
			}
		}
	}
}

