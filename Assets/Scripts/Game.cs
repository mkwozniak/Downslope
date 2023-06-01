using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Wozware.Downslope;

namespace Wozware.Downslope
{
	public sealed class Game : MonoBehaviour
	{
		public static readonly int PLAYER_DEFAULT_Y = 4;

		#region Public Members

		public AssetPack Assets;
		public WorldGenerator World;
		public PlayerControl Player;

		public TextMeshProUGUI KMHLabel;
		public TextMeshProUGUI DistanceLabel;

		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		private void UpdateKMHLabel(float val)
		{
			KMHLabel.text = ((int)val).ToString();
		}

		private void UpdateDistanceLabel(float val)
		{
			string km = (val * 0.001f).ToString("F1");
			DistanceLabel.text = (km).ToString();
		}

		private void InitializeEvents()
		{
			Player.OnStartMoving += World.GenerateForward;
			Player.OnSpeedUpdated += World.UpdateWorldSpeed;
			Player.OnHitIce += World.CreatePlayerTrail;

			Player.CreateSFX = Assets.CreateSFX;
			Player.CreatePFX = World.CreatePFXSprite;
			Player.GetKMH = World.KMH;

			World.OnUpdateDistanceTravelled += UpdateDistanceLabel;
			World.OnUpdateKMH += UpdateKMHLabel;
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			Assets.Initialize();
		}

		private void Start()
		{
			InitializeEvents();
			Player.GameInitialize();
		}

		private void Update()
		{

		}

		#endregion

	}
}


