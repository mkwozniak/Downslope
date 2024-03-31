using System.Collections.Generic;
using System;
using UnityEngine;
using PixelPerfectCamera = UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera;
using Unity.VisualScripting;
using UnityEngine.Windows;
using UnityEngine.TextCore.Text;

namespace Wozware.Downslope
{
	public sealed partial class Game : MonoBehaviour
	{
		#region Events

		public event Action<bool> OnGamePaused;

		#endregion

		#region Static Members		

		/// <summary> If the game is paused. </summary>
		public static bool IS_PAUSED
		{
			get { return _PAUSED; }
		}

		/// <summary> If objects behind the player should be destroyed. </summary>
		public static bool WORLD_DESTROY
		{
			get { return _WORLD_DESTROY; }
		}

		/// <summary> The default menu speed. </summary>
		public static readonly float PLAYER_DEFAULT_MENU_SPEED = 0.12f;

		/// <summary> The default number of forward chunks to generate. </summary>
		public static readonly int WORLD_GEN_DEFAULT_DIST = 24;

		/// <summary> The distance to clear each tutorial obstacle stage. </summary>
		public static readonly int TUTORIAL_OBSTACLE_STAGE_DIST = 100;

		/// <summary> The number of forward chunks to generate for tutorial. </summary>
		public static readonly int TUTORIAL_WORLD_GEN_DIST = 8;

		/// <summary> The number of chunks before destroying behind AI. </summary>
		public static readonly int AI_WORLD_DESTROY_OFFSET = 4;

		public static readonly int DEFAULT_CAMERA_PPU = 64;
		public static readonly int DEFAULT_CAMERA_PPU_MAP = 12;

		private static bool _PAUSED = false;
		private static bool _WORLD_DESTROY = true;
		private static string _NAME = "";

		#endregion

		#region Public Members

		public PlayerAI TestAI;
		public bool IsRunningLocally = false;
		public string DevArcadeTestMap = "Bunny Hill";
		public int DevArcadeTestMapMusic = 3;
		public float MusicSwitchFadeSpeed = 1.0f;

		public DownslopeUI UI
		{
			get
			{
				return _ui;
			}
		}

		#endregion

		#region Private Members

		/// <summary> The core SceneLoader. </summary>
		[SerializeField] private SceneLoader _loader;

		/// <summary> The core asset package. </summary>
		[SerializeField] private AssetPack _assets;

		/// <summary> The map asset package. </summary>
		[SerializeField] private MapAssetPack _mapAssets;

		/// <summary> The sprite asset package. </summary>
		[SerializeField] private SpriteAssetPack _spriteAssets;

		/// <summary> The DownslopeUI internal component. </summary>
		[SerializeField] private DownslopeUI _ui;

		/// <summary> The World instance. </summary>
		[SerializeField] private WorldGenerator _world;

		/// <summary> The Player instance. </summary>
		[SerializeField] private PlayerControl _player;

		/// <summary> The AudioSource for music. </summary>
		[SerializeField] private AudioSource _musicSource;

		/// <summary> The Camera instance. </summary>
		[SerializeField] private Camera _camera;

		/// <summary> The Pixel Perfect Camera instance. </summary>
		[SerializeField] private PixelPerfectCamera _pixelCamera;

		/// <summary> The DownslopeCamera instance. </summary>
		[SerializeField] private DownslopeCamera _downslopeCamera;

		/// <summary> The LevelCreator instance. </summary>
		[SerializeField] private LevelCreator _levelCreator;

		/// <summary> The world map Unity Tilemap game object. </summary>
		[SerializeField] private GameObject _worldMap;

		/// <summary> The current active game mode. </summary>
		[ReadOnly][SerializeField] private GameModes _mode;

		/// <summary> The current active game state. </summary>
		[ReadOnly][SerializeField] private GameStates _state;

		/// <summary> The current active view type. </summary>
		[ReadOnly][SerializeField] private UIViewTypes _currViewType;
		[ReadOnly][SerializeField] private UIViewTypes _prevViewType;
		[ReadOnly][SerializeField] private UIViewTypes _nextViewType;

		private Dictionary<UIViewTypes, UIView> _views = new();

		/// <summary> Unity Update method callbacks for each game mode. </summary>
		private Dictionary<GameModes, Action> _modeCallbacks;

		private Dictionary<UIViewTypes, Action> _enterViewCallbacks;
		private Dictionary<UIViewTypes, Action> _exitViewCallbacks;
		private bool _firstView = true;

		/// <summary> The current map data loaded. </summary>
		private WeightedMapData _currMapData;
		private bool _mapPointHovered = false;
		private WorldMapPoint _currMapPointSelected;
		private bool _demoPointSelected = false;

		private float _currMusicVolumeSetting = 1.0f;
		private bool _isMusicSwitching = false;
		private bool _isMusicNext = false;
		private int _nextMusicID = 0;

		#endregion

		#region Unity Methods

		private void Awake()
		{
			_NAME = GetType().Name;
			_assets.Initialize();
			_mapAssets.Initialize();
			_spriteAssets.Initialize();

			DownslopeFiles.CheckLocalPersistentDirectoryStructure();
			DownslopeInput.InitializeControls();
			InitializeViews();
			_levelCreator.Initialize(this);
			Canvas.ForceUpdateCanvases();
		}

		private void Start()
		{
			InitializeEvents();
			InitializeWorld();
			if(IsRunningLocally)
			{
				_currViewType = UIViewTypes.MainMenu;
				SwitchUIView(UIViewTypes.MainMenu);
			}
		}

		private void Update()
		{
			DownslopeInput.UpdateInputValues();
			_modeCallbacks[_mode].Invoke();
			UpdateMusicSwitch();
			IsAIBehindPlayer();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Get the internal UI instance.
		/// </summary>
		/// <returns> The Games internal UI instance.</returns>
		public DownslopeUI GetUI() { return _ui; }

		/// <summary>
		/// Pause the game.
		/// </summary>
		/// <param name="pause">True to pause.</param>
		public void PauseGameTime(bool pause)
		{
			if(pause)
			{
				DownslopeTime.LocalTimeScale = 0;
				_PAUSED = true;
				OnGamePaused.Invoke(true);
				return;
			}

			DownslopeTime.LocalTimeScale = 1;
			_PAUSED = false;
			OnGamePaused.Invoke(false);
		}

		/// <summary>
		/// Attaches this instance to a SceneLoader instance and enters the default starting state.
		/// </summary>
		/// <param name="loader">The SceneLoader to attach to. </param>
		public void StartupFromLoader(SceneLoader loader)
		{
			Util.Log("StartupFromLoader", name);
			_loader = loader;
			_currViewType = UIViewTypes.MainMenu;
			SwitchUIView(UIViewTypes.MainMenu);
		}

		/// <summary>
		/// Set the arcade map.
		/// </summary>
		/// <param name="id">The ID of the map. </param>
		public void SetArcadeMap(string id)
		{
			if(!_mapAssets.ArcadeModeMaps.ContainsKey(id))
			{
				Debug.LogError($"{this.name}: SetArcadeMap {id} does not exist in ArcadeModeMaps.");
				return;
			}

			Util.Log($"Set Arcade Map {id}", this.name);

			_currMapData = _mapAssets.ArcadeModeMaps[id];
			SetMusic(_currMapData.MusicID);

			_world.SetWorldEdgeSize(_currMapData.WorldEdgeSize, _currMapData.MinWorldEdgeSize);
			_world.SetLayerWeight(RanWeightedLayerTypes.Obstacle, _currMapData.ObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.ObstacleIce, _currMapData.IceObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePathSpawn, _currMapData.IcePathSpawnWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePath, _currMapData.IcePathWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.WorldEdge, _currMapData.WorldEdgeWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.XOffset, _currMapData.WorldXOffsetWeights, _world.ObstacleSeed);
			_world.SetMaxChunkWidth(_currMapData.MaxIcePathWidth);
		}

		public void SetMenuMap()
		{
			Util.Log("Switched To Menu Map.", this.name);
			_currMapData = _mapAssets.MenuMapList[0];
			_world.SetFirstIcePathName("PathEmpty");
			_world.SetWorldEdgeSize(10, 4);
			_world.SetLayerWeight(RanWeightedLayerTypes.Obstacle, _mapAssets.MenuMapList[0].ObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.ObstacleIce, _mapAssets.MenuMapList[0].IceObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePathSpawn, _mapAssets.MenuMapList[0].IcePathSpawnWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePath, _mapAssets.MenuMapList[0].IcePathWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.SnowVariation, _mapAssets.MenuMapList[0].SnowVariationWeights);
			_world.SetLayerWeight(RanWeightedLayerTypes.XOffset, _mapAssets.MenuMapList[0].WorldXOffsetWeights, _world.ObstacleSeed);
		}

		/// <summary> Enter Tutorial mode state. </summary>
		public void EnterTutorialMode()
		{
			Debug.Log("Game: EnterTutorialMode");

			_world.SetWorldEdgeSize(_mapAssets.TutorialMapList[0].WorldEdgeSize, _mapAssets.TutorialMapList[0].MinWorldEdgeSize);
			_world.SetLayerWeight(RanWeightedLayerTypes.Obstacle, _mapAssets.TutorialMapList[0].ObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.ObstacleIce, _mapAssets.ClearMapData.IceObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePathSpawn, _mapAssets.TutorialMapList[0].IcePathSpawnWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePath, _mapAssets.TutorialMapList[0].IcePathWeights, _world.ObstacleSeed);

			_world.PathForwardGenerationDistance = TUTORIAL_WORLD_GEN_DIST;

			if (_loader == null)
			{
				StartTutorialMode();
				return;
			}

			_loader.OnFadeInFinish += StartTutorialMode;
			_loader.StartFadeIn();
		}

		/// <summary> Enter Menu mode state. </summary>
		public void EnterMenuMode()
		{
			Util.Log("EnterMenuMode", name);

			// finalize the switch between states
			FinalizeUIViewSwitch();

			// unsub from fade in 
			//UnsubscribeFromLoaderFadeInFinish(EnterMenuMode);
			if(!IsRunningLocally)
				_loader.OnFadeInFinish -= EnterMenuMode;

			// do menu enter stuff
			DownslopeInput.EnablePlayerInputActionMap(false);
			DownslopeInput.EnableLevelCreatorInputActionMap(false);

			_world.ClearWorld();
			SetMenuMap();
			SetMusic(0);
			_views[UIViewTypes.MainMenu].Show(true);
			_player.StopAllMovement();
			_player.MenuModeStart();
			_player.EnableTrails(false);
			_camera.backgroundColor = _ui.CameraBG_Menu;
			_camera.transform.position = new Vector3(0, 0, -10);
			_downslopeCamera.FollowMode = false;
			_pixelCamera.assetsPPU = DEFAULT_CAMERA_PPU;
			_state = GameStates.Menu;
		}

		/// <summary> Exit Menu mode state. </summary>
		public void ExitMenuMode()
		{
			Util.Log("ExitMenuMode", name);
			_views[UIViewTypes.MainMenu].Show(false);
		}

		public void EnterMapMode()
		{
			Util.Log("EnterMapMode", name);

			// finalize the switch between states
			FinalizeUIViewSwitch();

			// unsub from fade in
			//UnsubscribeFromLoaderFadeInFinish(EnterMapMode);
			if (!IsRunningLocally)
				_loader.OnFadeInFinish -= EnterMapMode;

			// do map mode stuff
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.GAME_SELECT, SelectCurrentWorldMapPoint);

			SetMusic(1);
			_world.ClearWorld();
			_views[UIViewTypes.Map].Show(true);
			_pixelCamera.assetsPPU = DEFAULT_CAMERA_PPU_MAP;
			_camera.transform.position = new Vector3(0, 0, -10);
			_downslopeCamera.FollowMode = false;
			_camera.backgroundColor = _ui.CameraBG_Map;
			_worldMap.SetActive(true);
		}

		public void ExitMapMode()
		{
			Util.Log("ExitMapMode", name);
			_views[UIViewTypes.Map].Show(false);
			_worldMap.SetActive(false);
		}

		/// <summary> Enter LevelCreator mode state. </summary>
		public void EnterLevelCreatorMode()
		{
			Util.Log("EnterLevelCreatorMode", name);

			// unsub from fade in 
			UnsubscribeFromLoaderFadeInFinish(EnterLevelCreatorMode);

			// do level creator stuff
			DownslopeInput.EnablePlayerInputActionMap(false);
			DownslopeInput.EnableGameInputActionMap(false);
			DownslopeInput.EnableLevelCreatorInputActionMap(true);

			SetMusic(-1);
			_player.StopAllMovement();
			_world.ClearWorld();
			_views[UIViewTypes.LevelCreator].Show(true);
			_state = GameStates.LevelCreator;
			_levelCreator.EnterStartingState();

			// finalize the switch between states
			FinalizeUIViewSwitch();
		}

		public void ExitLevelCreatorMode()
		{
			Util.Log("ExitLevelCreatorMode", name);
			DownslopeInput.EnableLevelCreatorInputActionMap(false);
			_levelCreator.ClearMap();
			_views[UIViewTypes.LevelCreator].Show(false);
			_levelCreator.Close();
		}

		public void EnterGameMode()
		{
			Util.Log("EnterGameMode", name);
			// unsub from fade in 
			UnsubscribeFromLoaderFadeInFinish(EnterGameMode);

			// do game mode stuff
			_world.ClearWorld();
			InitializeAIOpponent(TestAI);
			_player.ArcadeModeStart();
			_player.DefaultForwardSpeed = _mapAssets.SlopeAngleData[_currMapData.Identity.AngleType].ForwardSpeed;
			_player.AccelerationSpeed = _mapAssets.SlopeAngleData[_currMapData.Identity.AngleType].ForwardAccel;
			_pixelCamera.assetsPPU = DEFAULT_CAMERA_PPU;
			_camera.backgroundColor = _ui.CameraBG_Menu;
			_player.transform.position = new Vector3(0, 4, 0);
			_camera.transform.position = new Vector3(0, 0, -10);
			_downslopeCamera.FollowMode = true;

			_world.ArcadeWorldGenerateNextChunks();

			_mode = GameModes.Arcade;
			_world.ArcadeWorldGenerateNextChunks();
			DownslopeInput.EnablePlayerInputActionMap(true);
			DownslopeInput.EnableGameInputActionMap(true);
			DownslopeInput.EnableLevelCreatorInputActionMap(false);
			_views[UIViewTypes.Game].Show(true);

			// finalize the switch between states
			FinalizeUIViewSwitch();
		}

		public void ExitGameMode()
		{
			Util.Log("ExitGameMode", name);
			_views[UIViewTypes.Game].Show(false);
			DownslopeInput.EnablePlayerInputActionMap(false);
		}

		public void EnterArcadeMode()
		{
			//SetArcadeMap(DevArcadeTestMap);

			Util.Log("StartArcadeMode", name);
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.GAME_ENTER, ArcadeModeStarted);
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.GAME_ENTER, _player.StartInitialMovement);
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.GAME_ESCAPE, EnterEscapeMode);

			SwitchUIView(UIViewTypes.Game);
		}

		/// <summary> Enter Escape mode state. </summary>
		public void EnterEscapeMode()
		{
			Debug.Log("Game: EnterEscapeMode");

			PauseGameTime(true);
			_ui.ShowEscapeView();
		}

		/// <summary> Exit Escape mode state. </summary>
		public void ExitEscapeMode()
		{
			Debug.Log("Game: ExitEscapeMode");

			PauseGameTime(false);
			_ui.HideEscapeView();
		}

		public void EnterGameOverMode()
		{
			Debug.Log("Game: EnterGameOverView");
			PauseGameTime(true);
			_ui.ShowGameOverView();
		}

		public void ExitGameOverMode()
		{
			Debug.Log("Game: ExitGameOverView");
			PauseGameTime(false);
			_ui.HideGameOverView();
		}

		/// <summary>
		/// Play a game sound by ID.
		/// </summary>
		/// <param name="id">The ID of the sound. </param>
		public void PlaySound(string id)
		{
			if (!TryCreateSFX(id))
			{
				Debug.LogWarning($"Game: PlaySound: TryCreateSFX returned false. SFX {id} was ignored.");
			}
		}

		/// <summary>
		/// Play game music by ID.
		/// </summary>
		/// <param name="id">The ID of the music track.</param>
		public void SetMusic(int id)
		{
			if (id >= _assets.Music.Count || id < -1)
				return;

			_isMusicSwitching = true;
			_isMusicNext = false;
			_nextMusicID = id;
		}

		public void InitializeAIOpponent(PlayerAI ai)
		{
			// initialize props
			WorldObjectProps props = new WorldObjectProps(-13, ai.gameObject);
			WorldMovementProps movement = new WorldMovementProps(_world.WorldDirection, 0, 0);

			ai.WorldObj.SetObjectSpeed(_world.WorldSpeed * DownslopeTime.TimeScale);

			// subscribe to events
			_world.OnChangedWorldSpeed += ai.WorldObj.SetObjectSpeed;
			ai.Initialize(props, movement);
		}

		public void IsAIBehindPlayer()
		{
			if(!TestAI.Controller.Active)
			{
				_WORLD_DESTROY = true;
				return;
			}

			if (TestAI.transform.position.y > _player.transform.position.y)
			{
				Debug.Log("No World Destroy");
				_WORLD_DESTROY = false;
				return;
			}
			_WORLD_DESTROY = true;
		}

		public bool IsYPositionAboveAI(float yPosition)
		{
			if (yPosition > TestAI.transform.position.y + AI_WORLD_DESTROY_OFFSET)
			{
				return true;
			}

			return false;
		}

		public void SelectDemoPoint()
		{
			_demoPointSelected = true;
		}

		public void DeselectDemoPoint()
		{
			_demoPointSelected = false;
		}

		#endregion

		#region Private Methods

		private void InitializeEvents()
		{
			OnGamePaused = (v) => { };
			OnFinishDistanceTutorialStage = () => { };

			// static ai event for all world objects
			WorldObject.IsObjectAboveAI = IsYPositionAboveAI;

			_modeCallbacks = new();
			_modeCallbacks[GameModes.Tutorial] = UpdateTutorial;
			_modeCallbacks[GameModes.Arcade] = () => { };
			_modeCallbacks[GameModes.Challenge] = () => { };

			_enterViewCallbacks = new();
			_exitViewCallbacks = new();
			_enterViewCallbacks[UIViewTypes.MainMenu] = EnterMenuMode;
			_exitViewCallbacks[UIViewTypes.MainMenu] = ExitMenuMode;
			_enterViewCallbacks[UIViewTypes.Map] = EnterMapMode;
			_exitViewCallbacks[UIViewTypes.Map] = ExitMapMode;
			_enterViewCallbacks[UIViewTypes.LevelCreator] = EnterLevelCreatorMode;
			_exitViewCallbacks[UIViewTypes.LevelCreator] = ExitLevelCreatorMode;
			_enterViewCallbacks[UIViewTypes.Game] = EnterGameMode;
			_exitViewCallbacks[UIViewTypes.Game] = ExitGameMode;

			_world.CreateSFX = TryCreateSFX;

			_world.OnUpdatedKMH += _ui.SetKMHLabel;
			_world.OnUpdatedDistanceTravelled += _ui.SetDistanceLabel;
			_world.ShiftWorldCameraX = _downslopeCamera.ShiftToXOffset;
			_world.GetPlayerXPosition = _player.GetXPosition;

			OnGamePaused += _player.GamePaused;
			_player.CreateSFX = TryCreateSFX;
			_player.CreatePFX = _world.CreatePFXSprite;
			_player.GetKMH = _world.KMH;

			_player.OnStartedMovement += _world.ArcadeWorldGenerateNextChunks;
			_player.OnUpdatedSpeed += _world.SetWorldSpeed;
			_player.OnCreateTrail += _world.CreatePlayerTrail;
			_player.OnHealthUpdated += _ui.SetHealthBarPercentage;
			_player.OnGameOver += TriggerGameOver;
			_player.AIOpponent.CheckAIReachedGenThreshold = _world.CheckAIReachedGenThreshold;

			_levelCreator.OnUpdateChunkScrollRange += _ui.LC_SetChunkRangeLabel;
		}

		private void InitializeWorld()
		{
			SetMenuMap();
			for (int i = 0; i < _ui.Map.MapPoints.Count; i++)
			{
				_ui.Map.MapPoints[i].HoverEnterEventHandler += SetCurrentWorldMapPoint;
				_ui.Map.MapPoints[i].HoverExitEventHandler += ClearCurrentWorldMapPoint;
			}
		}

		private void InitializeViews()
		{
			if(_ui == null)
			{
				Debug.LogError("Error initializing views. UI Instance is null.");
				return;
			}

			foreach (UIView view in _ui.Views)
			{
				_views[view.Type] = view;
				_views[view.Type].OnSwitchTo = SwitchUIView;
			}
		}

		/// <summary>
		/// Switches to a different primary ui view. 
		/// The previous view automatically exits itself based on its respective exit callback.
		/// </summary>
		/// <param name="type"></param>
		private void SwitchUIView(UIViewTypes type)
		{
			Util.Log($"SwitchUIView({type})", name);

			// cache passed to next view
			// enter to next view
			// exit from current view

			_nextViewType = type;
			if (_loader == null)
			{
				Util.Log($"Local Switch To: {_nextViewType}", name);
				_enterViewCallbacks[type]();
				return;
			}

			Util.Log($"Loader Switch To: {_nextViewType}", name);
			_loader.OnFadeInFinish += _enterViewCallbacks[_nextViewType];
			_loader.StartFadeIn();
		}

		/// <summary>
		/// Callback for when a view switch is finished and their states can be finalized.
		/// </summary>
		private void FinalizeUIViewSwitch()
		{
			Util.Log($"Finalize View Switch: {_currViewType} >> {_nextViewType}", name);
			// if not transitioning from the same state, exit the current state
			if (_currViewType != _nextViewType)
			{
				_exitViewCallbacks[_currViewType]();
			}
			_currViewType = _nextViewType;
			StartLoaderFadeOutFinish();
		}

		private void StartLoaderFadeOutFinish(Action onFinishedCallback = null)
		{
			// the initial scene loader will be null if being played from the editor
			if (_loader == null)
			{
				if(onFinishedCallback != null)
					onFinishedCallback();
				return;
			}

			_loader.OnFadeOutFinish += onFinishedCallback;
			_loader.StartFadeOut();
		}

		private void UnsubscribeFromLoaderFadeOutFinish(Action callback)
		{
			// the initial scene loader will be null if being played from the editor
			if (_loader == null)
			{
				return;
			}

			_loader.OnFadeOutFinish -= callback;
		}

		private void UnsubscribeFromLoaderFadeInFinish(Action callback)
		{
			// the initial scene loader will be null if being played from the editor
			if (_loader == null)
			{
				return;
			}

			_loader.OnFadeInFinish -= callback;
		}

		private void TriggerGameOver()
		{
			EnterGameOverMode();
			PlaySound("Voice-GameOver");
		}

		private void StartTutorialMode()
		{
			UnsubscribeFromLoaderFadeInFinish(StartTutorialMode);

			Debug.Log("Game: StartTutorialMode");

			_world.ClearWorld();
			_player.TutorialStart();
			_world.ArcadeWorldGenerateNextChunks();
			_ui.HideMainMenuFront();
			_ui.ShowGameView();
			SetMusic(1);
			StartTutorialInitialStage();
			_mode = GameModes.Tutorial;
			TryFadeOutLoader();
		}

		private void ArcadeModeStarted()
		{
			Debug.Log("Game: ArcadeModeStarted");
			DownslopeInput.UnsubscribeInputPerformed(DownslopeInput.GAME_ENTER, ArcadeModeStarted);
			DownslopeInput.UnsubscribeInputPerformed(DownslopeInput.GAME_ENTER, _player.StartInitialMovement);
		}

		private void StartLevelCreatorMode()
		{
			Debug.Log("Game: StartLevelCreatorMode: Unsubscribing Callback..");
			_loader.UnsubscribeFadeIn(StartLevelCreatorMode);

			Debug.Log("Game: StartLevelCreatorMode");

			_world.ClearWorld();
			DownslopeInput.EnableLevelCreatorInputActionMap(true);
			_ui.ShowLevelCreatorView();
			_levelCreator.EnterStartingState();

			_state = GameStates.LevelCreator;
			TryFadeOutLoader();
		}

		private bool TryCreateSFX(string id)
		{
			AudioClip clip;
			if (!_assets.TryGetSFX(id, out clip))
			{
				Debug.LogWarning($"Game: TryCreateSFX: Assets.TryGetSFX returned false. SFX {id} was ignored.");
				return false;
			}

			AudioSource source = Instantiate(_assets.SFXPrefab, Vector3.zero, Quaternion.identity);
			source.clip = clip;
			source.Play();

			Destroy(source.gameObject, clip.length + 0.1f);
			return true;
		}

		private bool TryFadeOutLoader()
		{
			if (_loader != null)
			{
				_loader.StartFadeOut();
				return true;
			}

			return false;
		}

		private void UpdateMusicSwitch()
		{
			if(_isMusicSwitching)
			{
				if(!_isMusicNext)
				{
					_musicSource.volume -= Time.deltaTime * MusicSwitchFadeSpeed;
					if (_musicSource.volume <= 0)
					{
						_isMusicNext = true;
						if(_nextMusicID == -1)
						{
							_musicSource.Stop();
							return;
						}
						_musicSource.clip = _assets.Music[_nextMusicID];
						_musicSource.Play();
					}
					return;
				}

				if (_musicSource.volume < _currMusicVolumeSetting)
				{
					_musicSource.volume += Time.deltaTime * MusicSwitchFadeSpeed;
				}

				if (_musicSource.volume >= _currMusicVolumeSetting)
				{
					_musicSource.volume = _currMusicVolumeSetting;
					_isMusicNext = false;
					_isMusicSwitching = false;
				}
			}
		}

		private void SelectCurrentWorldMapPoint()
		{
			if(_mapPointHovered && _currMapPointSelected != null)
			{
				Debug.Log("Selected Map Point");
				PlaySound("UIOptionSelect");
				_currMapPointSelected.OnClickEvents.Invoke();
				ClearCurrentWorldMapPoint();
			}
		}

		private void SetCurrentWorldMapPoint(WorldMapPoint point)
		{
			if (_demoPointSelected)
				return;

			_currMapPointSelected = point;
			_mapPointHovered = true;
			UI.DemoSelectionView.gameObject.SetActive(true);
			PlaySound("UIOptionHover");
		}

		private void ClearCurrentWorldMapPoint()
		{
			_currMapPointSelected = null;
			_mapPointHovered = false;
			UI.DemoSelectionView.gameObject.SetActive(false);
		}

		#endregion

	}
}


