using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Tilemaps;
using Wozware.Poolers;
using Text = TMPro.TextMeshPro;

namespace Wozware.Downslope
{
	public sealed class LevelCreator : MonoBehaviour
	{
		#region Events

		// events
		public event Action<string> OnUpdateChunkScrollRange;
		public event Action OnClearLevel;

		// actions
		public Action PreviousStateCallback;

		#endregion

		#region Public Members

		[Header("Core")]
		public AssetPack Assets;
		public SpriteAssetPack Sprites;
		public Transform WorldRootCenter;
		public Transform RootObjectParent;
		public Transform InfoPanelOverlay;
		public GameObject GridRoot;
		public Tilemap GridTilemap;
		public List<Text> NumberGridText = new List<Text>();

		public WozPooler<LevelCreatorObject> PrimaryObjectPooler;

		[Header("Value Settings")]
		public Vector2 GridOffset;
		public int BaseHeight = 4;
		public int ScrollIncrement_Normal = 1;
		public int ScrollIncrement_Shift = 10;
		public int ScrollIncrement_Ctrl = 100;
		public int ForwardChunkEditDistance;
		public int DefaultChunkEdgeSize = 12;
		public int ChunkBaseOffsetY = 4;
		public int ChunkBaseOffsetX = 4;
		public float TopMessageShowTime = 4f;

		[Header("Colors")]
		public Color ActiveChunkColor;
		public Color ForwardChunkColor;
		public Color BackwardChunkColor;

		public Color CursorSelectDefaultColor;
		public Color CursorSelectReplaceColor;
		public Color CursorSelectErrorColor;

		public Color TipInputHighlightColor;
		public Color TipKeywordHighlightColor;

		[Header("Sprites")]
		public SpriteRenderer ActivePlacementCursorSprite;
		public SpriteRenderer IndicatorCursorSprite;

		public Dictionary<SpriteLayerTypes, Dictionary<string, LevelCreatorPlaceable>> LayerPlaceables =
			new Dictionary<SpriteLayerTypes, Dictionary<string, LevelCreatorPlaceable>>();

		#endregion

		#region Private Members
		
		// serialized members
		[SerializeField][ReadOnly] private bool _isActive;
		[SerializeField][ReadOnly] private bool _controlMode = false;
		[SerializeField][ReadOnly] private bool _shiftMode = false;
		[SerializeField][ReadOnly] private bool _hasPlaceableSelected = false;
		[SerializeField][ReadOnly] private bool _placementPanelOpen = false;
		[SerializeField][ReadOnly] private bool _layerObjectChoiceViewOpen = false;
		[SerializeField][ReadOnly] private bool _savePanelOpen = false;
		[SerializeField][ReadOnly] private bool _loadPanelOpen = false;
		[SerializeField][ReadOnly] private bool _infoPanelOpen = false;
		[SerializeField][ReadOnly] private bool _topMsgShowing = false;
		[SerializeField][ReadOnly] private bool _hasStartingPoint = false;
		[SerializeField][ReadOnly] private bool _hasEndingPoint = false;
		[SerializeField][ReadOnly] private uint _currFocusedChunk = 0;
		[SerializeField][ReadOnly] private int _currFocusedChunkEdge = 10;
		[SerializeField][ReadOnly] private uint _currEndChunk = 0;

		[SerializeField][ReadOnly] private LevelCreatorStates _state;
		[SerializeField][ReadOnly] private SpriteLayerTypes _currSelectedPlacementType;
		[SerializeField][ReadOnly] private LevelCreatorPlaceable _currSelectedPlaceable;
		[SerializeField][ReadOnly] private LevelCreatorObjectTypes _currObjectTypeSelected;
		[SerializeField][ReadOnly] MapFileData _currLoadedMapData;

		// non serialized members
		private Game _game;
		private float _topMsgShowTimer = 0f;
		private Color _currIndicatorColor;

		private Dictionary<int, LevelCreatorObject> _activeLevelObjects = new Dictionary<int, LevelCreatorObject>();
		private Dictionary<uint, LevelChunkData> _currLevelChunkData = new Dictionary<uint, LevelChunkData>();
		private Dictionary<LevelCreatorStates, Action> _stateCallbacks = new Dictionary<LevelCreatorStates, Action>();
		private Dictionary<SpriteLayerTypes, string> _spriteLayerTypeStrings = new Dictionary<SpriteLayerTypes, string>();
		private Dictionary<string, SpriteLayerTypes> _spriteLayerTypeValues = new Dictionary<string, SpriteLayerTypes>();

		#endregion

		#region Public Methods

		public void Initialize(Game game)
		{
			_game = game;
		}

		public void EnterStartingState()
		{
			GridRoot.SetActive(true);
			_isActive = true;
			_state = LevelCreatorStates.None;
		}

		public void ExitFromUI()
		{
			ExitConfirmation();
		}

		public void Close()
		{
			GridRoot.SetActive(false);
			_isActive = false;
			_state = LevelCreatorStates.None;
		}

		public void EnableControlMode()
		{
			_controlMode = true;
		}

		public void DisableControlMode()
		{
			_controlMode = false;
		}

		public void EnableShiftMode()
		{
			_controlMode = true;
		}

		public void DisableShiftMode()
		{
			_controlMode = false;
		}

		public void SaveMapFromCurrentInputFields()
		{
			string name = _game.UI.LC_GetMapNameInputFieldText();
			string fileName = _game.UI.LC_GetMapFileNameInputFieldText();

			if (name.Length == 0 || fileName.Length == 0)
			{
				ShowTopMessage("Map or File name are empty.");
				return;
			}

			HashSet<char> nameChars = new HashSet<char>(name);
			HashSet<char> fileNameChars = new HashSet<char>(fileName);

			if(nameChars.Overlaps(FilePathData.MAP_NAME_ILLEGAL_CHARACTERS))
			{
				ShowTopMessage("Map Name contains invalid characters.");
				return;
			}

			if (fileNameChars.Overlaps(FilePathData.MAP_NAME_ILLEGAL_CHARACTERS))
			{
				ShowTopMessage("File Name contains invalid characters.");
				return;
			}

			string path = DownslopeFiles.GetPersistentMapPath(fileName);
			DateTime localDate = DateTime.Now;

			MapFileData newData = new MapFileData(name, fileName, path, localDate.ToString(), _currLevelChunkData);
			DownslopeFiles.SaveMapToFile(newData, fileName);

			ShowTopMessage($"Successfully saved map {fileName}.");

			// save entries refresh
			ReadAndRefreshSaveEntries();

			if (_savePanelOpen)
			{
				ToggleSaveMode();
			}
		}

		public void LoadMap(string fileName)
		{
			MapFileData data;
			bool loaded = DownslopeFiles.TryLoadMapFromFile(fileName, out data);
			if(!loaded)
			{
				ShowTopMessage($"Loading map {fileName} failed. The file may not exist.");
			}

			_currLoadedMapData = data;
			_currLevelChunkData = _currLoadedMapData.ChunkData;
			ShowTopMessage($"Successfully loaded map {fileName}.");
			List<uint> t = _currLevelChunkData.Keys.ToList();
			for(int i = 0; i < t.Count; i++)
			{
				Debug.Log($"LevelCreator: LoadMap: Loaded Chunk: {t[i]}");
			}

			UpdateChunkPlacements();

			if (_loadPanelOpen)
			{
				ToggleLoadMode();
			}
		}

		public void NewMap()
		{
			NewMapConfirmation();
		}

		public void ClearMap()
		{
			_currLevelChunkData.Clear();
			UpdateChunkPlacements();
		}

		public void SetNoneMode()
		{
			_state = LevelCreatorStates.None;
		}	

		public void ToggleLoadMode()
		{
			if (!_loadPanelOpen)
			{
				// callback previous state toggle
				CheckCallbackPreviousState(LevelCreatorStates.Load);

				// set new toggle callback
				SetPreviousStateCallback(ToggleLoadMode);

				// enable save panel
				_game.UI.LC_EnableSavePanel(true);
				_game.UI.LC_EnableSaveInputPanel(false);
				_game.UI.LC_SetSavePanelTitleLabel("Load Map");

				// refresh save entries
				ReadAndRefreshSaveEntries(true);

				// new save state
				_loadPanelOpen = true;
				_state = LevelCreatorStates.Load;
				return;
			}

			// disable panel
			_game.UI.LC_EnableSavePanel(false);

			// no state
			_loadPanelOpen = false;
			SetNoneMode();
		}

		public void ToggleSaveMode()
		{
			_game.UI.LC_ClearSavePanelInputFields();
			if (!_savePanelOpen)
			{
				// callback previous state toggle
				CheckCallbackPreviousState(LevelCreatorStates.Save);

				// set new toggle callback
				SetPreviousStateCallback(ToggleSaveMode);

				// enable save panel
				_game.UI.LC_EnableSavePanel(true);
				_game.UI.LC_EnableSaveInputPanel(true);
				_game.UI.LC_SetSavePanelTitleLabel("Save Map");

				// refresh save entries
				ReadAndRefreshSaveEntries();

				// new save state
				_savePanelOpen = true;
				_state = LevelCreatorStates.Save;
				return;
			}

			// disable panel
			_game.UI.LC_EnableSavePanel(false);

			// no state
			_savePanelOpen = false;
			SetNoneMode();
		}

		public void TogglePlacementMode()
		{
			if(!_placementPanelOpen)
			{
				// callback previous state toggle
				CheckCallbackPreviousState(LevelCreatorStates.Place);

				// set new toggle callback
				SetPreviousStateCallback(TogglePlacementMode);

				// open choices panel
				_game.UI.LC_EnableLayerChoicesPanel(true);

				// enable select cursor
				ActivePlacementCursorSprite.gameObject.SetActive(true);

				// new place state
				_placementPanelOpen = true;
				_state = LevelCreatorStates.Place;
				return;
			}

			// disable panels and select cursor
			ActivePlacementCursorSprite.gameObject.SetActive(false);
			_game.UI.LC_EnableLayerChoicesPanel(false);
			_game.UI.LC_EnableLayerObjectChoicesPanel(false);

			// no state
			_placementPanelOpen = false;
			SetNoneMode();
		}

		public void ToggleInfoMode()
		{
			if(!_infoPanelOpen)
			{
				// callback previous state toggle
				CheckCallbackPreviousState(LevelCreatorStates.Info);

				// set new toggle callback
				SetPreviousStateCallback(ToggleInfoMode);

				InfoPanelOverlay.gameObject.SetActive(true);
				_game.UI.LC_EnableInfoPanel(true);
				ShowInfoLabelModeNone();

				// new info state
				_infoPanelOpen = true;
				_state = LevelCreatorStates.Info;
				return;
			}

			// disable panel
			InfoPanelOverlay.gameObject.SetActive(false);
			_game.UI.LC_EnableInfoPanel(false);

			// no state
			_infoPanelOpen = false;
			SetNoneMode();
		}

		public void ShowInfoLabelModeNone()
		{
			_game.UI.LC_SetInfoPanelTipLabel(GetTipLabelNoneState());
		}

		public void ShowInfoLabelModePlacement()
		{
			_game.UI.LC_SetInfoPanelTipLabel(GetTipLabelPlaceState());
		}

		public void ShowInfoLabelModeSaveLoad()
		{
			_game.UI.LC_SetInfoPanelTipLabel(GetTipLabelSaveLoadState());
		}

		public void ShowTopMessage(string msg)
		{
			_topMsgShowing = true;
			_game.UI.LC_EnableTopMessagePanel(true);
			_game.UI.LC_SetTopMessageLabel(msg);
		}

		public void SelectPlacementType(string id)
		{
			if (_layerObjectChoiceViewOpen && _currSelectedPlacementType.ToString() == id)
			{
				_game.UI.LC_EnableLayerObjectChoicesPanel(false);
				_layerObjectChoiceViewOpen = false;
				return;
			}

			if (!_spriteLayerTypeValues.ContainsKey(id))
			{
				return;
			}

			_currSelectedPlacementType = _spriteLayerTypeValues[id];

			_game.UI.LC_EnableLayerObjectChoicesPanel(true);
			_layerObjectChoiceViewOpen = true;
			RefreshPlacementView();
		}

		public void SelectPlacementChoice(SpriteLayerTypes layerType, string id)
		{
			if (!LayerPlaceables.ContainsKey(layerType))
			{
				return;
			}

			LevelCreatorPlaceable placeable = LayerPlaceables[layerType][id];

			if (placeable.PlacementID != id)
			{
				return;
			}

			_currSelectedPlaceable = LayerPlaceables[layerType][id];
			_currObjectTypeSelected = placeable.ObjectType;
			ActivePlacementCursorSprite.sprite = Sprites.GetSprite(placeable.SpriteID);
			_hasPlaceableSelected = true;
			_game.UI.LC_EnableLayerObjectChoicesPanel(false);
			_layerObjectChoiceViewOpen = false;
		}

		public void PlaceCurrentSelectedObject()
		{
			if (!_hasPlaceableSelected || _state != LevelCreatorStates.Place)
			{
				return;
			}

			float x = 0f;
			uint y = 0;

			// check valid placement
			bool validPlacementPosition = TryGetPlacementPosition(out x, out y);

			if (!validPlacementPosition)
			{
				return;
			}

			// check chunk exists
			bool chunkExistsHere = true;

			if (!_currLevelChunkData.ContainsKey(y))
			{
				CreateChunkData(y);
				chunkExistsHere = false;
			}

			// check object exists
			bool objectExistsHere = chunkExistsHere;

			if (chunkExistsHere)
			{
				if (!_currLevelChunkData[y].PlacementData[_currSelectedPlaceable.LayerType].ContainsKey(x))
				{
					objectExistsHere = false;
				}
			}

			// check for eraser
			if (_currObjectTypeSelected == LevelCreatorObjectTypes.Eraser)
			{
				if(!objectExistsHere)
				{
					return;
				}

				LevelCreatorPlaceable placeable = _currLevelChunkData[y].PlacementData[_currSelectedPlaceable.LayerType][x];
				if (placeable.ObjectType == LevelCreatorObjectTypes.StartPoint)
				{
					_hasStartingPoint = false;
				}
				else if (placeable.ObjectType == LevelCreatorObjectTypes.EndPoint)
				{
					_hasEndingPoint = false;
					_currEndChunk = 0;
				}

				_currLevelChunkData[y].PlacementData[_currSelectedPlaceable.LayerType].Remove(x);
				Debug.Log($"LevelCreator: PlaceCurrentSelectedObject: Removed {_currSelectedPlaceable.PlacementID} at column {ActivePlacementCursorSprite.transform.position.x}");
				UpdateChunkPlacements();
				return;
			}
			else if (_currObjectTypeSelected == LevelCreatorObjectTypes.StartPoint)
			{
				if (y != 0)
				{
					ShowTopMessage("Start Point must be placed somewhere along the first chunk.");
					_hasStartingPoint = true;
					return;
				}
			}
			else if (_currObjectTypeSelected == LevelCreatorObjectTypes.EndPoint)
			{
				if (y < 25)
				{
					ShowTopMessage("The End Point must be at least 25 Metres away from the Start Point.");
					_hasEndingPoint = true;
					_currEndChunk = y;
					return;
				}
			}

			// replace or place object
			_currLevelChunkData[y].PlacementData[_currSelectedPlaceable.LayerType][x] = _currSelectedPlaceable;
			Debug.Log($"LevelCreator: PlaceCurrentSelectedObject: Added {_currSelectedPlaceable.PlacementID} at column {ActivePlacementCursorSprite.transform.position.x}");
			UpdateChunkPlacements();
		}

		#endregion

		#region Private Methods

		private void InitializeEvents()
		{
			PrimaryObjectPooler.OnAddToPool += LevelObjectPoolAdded;
			PrimaryObjectPooler.OnReturnToPool += LevelObjectPoolReturned;
			PrimaryObjectPooler.OnDestroyExcess += LevelObjectPoolDestroyed;
		}

		private void CreateChunkData(uint yPos)
		{
			_currLevelChunkData[yPos] = new LevelChunkData();
			List<string> layers = _spriteLayerTypeValues.Keys.ToList();

			for (int i = 0; i < layers.Count; i++)
			{
				_currLevelChunkData[yPos].PlacementData[_spriteLayerTypeValues[layers[i]]] = new();
			}
		}

		private void ReadAndRefreshSaveEntries(bool loadMode = false)
		{
			string[] maps = DownslopeFiles.GetAllPersistentMapPaths();
			_game.UI.LC_ClearSaveEntries();

			for (int i = 0; i < maps.Length; i++)
			{
				MapFileData data = DownslopeFiles.GetMapDataFromPath(maps[i]);

				if(loadMode)
				{
					_game.UI.LC_CreateLoadEntry(data,
					() => {
						LoadMapConfirmation(data.FileName);
					},
					() => {
						DeleteMapConfirmation(data.FileName);
					});

					continue;
				}

				_game.UI.LC_CreateSaveEntry(data,
					() => {
						OverwriteMapConfirmation(data.Name, data.FileName, data.Path, data.Time);
					},
					() => {
						DeleteMapConfirmation(data.FileName);
					});
			}
		}

		private void OverwriteSave(string name, string fileName, string path, string date)
		{
			MapFileData newData = new MapFileData(name, fileName, path, date, _currLevelChunkData);
			DownslopeFiles.SaveMapToFile(newData, fileName);
			ShowTopMessage($"Successfully overwritten map file {fileName}.");
			ReadAndRefreshSaveEntries();
		}

		private void DeleteSave(string fileName)
		{
			bool deleted = DownslopeFiles.TryDeleteMapFromPersistentPath(fileName);
			if(deleted)
			{
				ShowTopMessage($"Deleted map file {fileName}.");
				ReadAndRefreshSaveEntries();
				return;
			}

			ShowTopMessage($"Error deleting map file {fileName}. It may not exist anymore.");
		}

		private void LoadMapConfirmation(string fileName)
		{
			string msg = $"Are you sure you want to load the map {fileName}? \n " +
				$"Any unsaved changes in your current map will be lost.";
			_game.UI.EnterConfirmationPromptView(msg,
				() =>{
					LoadMap(fileName);
					_game.UI.ExitConfirmationPromptView();
				},
				() =>{
					_game.UI.ExitConfirmationPromptView();
				});
		}

		private void DeleteMapConfirmation(string fileName)
		{
			string msg = $"Are you sure you want to delete the map {fileName}? \n " +
				$"This cannot be undone.";
			_game.UI.EnterConfirmationPromptView(msg,
				() => {
					DeleteSave(fileName);
					_game.UI.ExitConfirmationPromptView();
				},
				() => {
					_game.UI.ExitConfirmationPromptView();
				});
		}

		private void OverwriteMapConfirmation(string mapName, string fileName, string path, string time)
		{
			string msg = $"Are you sure you want to overwrite the map {fileName}? \n " +
				$"The saved map will be replaced with your current map.";
			_game.UI.EnterConfirmationPromptView(msg,
				() => {
					OverwriteSave(mapName, fileName, path, time);
					_game.UI.ExitConfirmationPromptView();
				},
				() => {
					_game.UI.ExitConfirmationPromptView();
				});
		}

		private void NewMapConfirmation()
		{
			string msg = $"Are you sure you want to clear the map? \n " +
				$"Any unsaved changes will be lost.";
			_game.UI.EnterConfirmationPromptView(msg,
				() => {
					ClearMap();
					_game.UI.ExitConfirmationPromptView();
				},
				() => {
					_game.UI.ExitConfirmationPromptView();
				});
		}

		private void ExitConfirmation()
		{
			string msg = $"Are you sure you want to return to the main menu? \n " +
				$"Any unsaved changes will be lost.";
			_game.UI.EnterConfirmationPromptView(msg,
				() => {
					_game.UI.ExitConfirmationPromptView();
					_game.ExitLevelCreatorMode();
				},
				() => {
					_game.UI.ExitConfirmationPromptView();
				});
		}

		private void LevelObjectPoolAdded(LevelCreatorObject obj)
		{
			obj.UID = obj.GetHashCode();
			obj.gameObject.SetActive(false);
			obj.name = $"PooledSprite[{obj.UID}]";
		}

		private void LevelObjectPoolReturned(LevelCreatorObject obj)
		{
			obj.SetParent(PrimaryObjectPooler.PoolParent);
			obj.gameObject.SetActive(false);
			obj.name = $"PooledSprite[{obj.UID}]";
		}

		private void LevelObjectPoolDestroyed(LevelCreatorObject obj)
		{
			Destroy(obj);
		}

		/// <summary> Destroy LevelCreatorObject given obj. </summary>
		private void DestroyLevelObject(LevelCreatorObject obj)
		{
			DestroyLevelObject(obj.UID);
		}

		/// <summary> Destroy LevelCreatorObject given id. </summary>
		private void DestroyLevelObject(int id)
		{
			// unsub events
			// OnUpdate -= _activeObjects[id].UpdateObject;
			// OnSetWorldSpeed -= _activeLevelObjects[id].SetObjectSpeed;

			OnClearLevel -= _activeLevelObjects[id].TriggerDestroy;
			_activeLevelObjects[id].OnDestroyTriggered -= DestroyLevelObject;

			// return to pool

			PrimaryObjectPooler.ReturnToPool(_activeLevelObjects[id]);

			// remove object
			_activeLevelObjects.Remove(id);
		}

		/// <summary> Creates and initializes a WorldTile given sprite props and returns the WorldSprite.  </summary>
		private bool CreateLevelCreatorObject(Vector3 pos, LevelCreatorPlaceable placeable, out LevelCreatorObject obj)
		{
			// get sprite from pool			
			bool poolSuccess = PrimaryObjectPooler.GetFromPool(out obj);
			if (!poolSuccess)
			{
				Debug.LogError($"World CreateWorldObject failed. GetFromPool call returned false.");
				return false;
			}

			_activeLevelObjects.Add(obj.UID, obj);
			obj.transform.position = pos;
			obj.gameObject.SetActive(true);
			obj.Renderer.sprite = Sprites.GetSprite(placeable.SpriteID);
			obj.Renderer.sortingLayerName = SpriteSortData.LayerData[placeable.LayerType].LayerName;
			obj.Renderer.sortingOrder = SpriteSortData.LayerData[placeable.LayerType].LayerID;

			OnClearLevel += _activeLevelObjects[obj.UID].TriggerDestroy;
			_activeLevelObjects[obj.UID].OnDestroyTriggered += DestroyLevelObject;
			return true;
		}

		private bool TryGetPlacementPosition(out float x, out uint y)
		{
			uint yPos = (uint)ActivePlacementCursorSprite.transform.position.y;
			uint height = (uint)BaseHeight - yPos;

			if (height >= ForwardChunkEditDistance)
			{
				x = 0; y = 0;
				return false;
			}

			uint placementFocusedY = _currFocusedChunk + (height);
			float placementFocusedX = ActivePlacementCursorSprite.transform.position.x;
			x = placementFocusedX;
			y = placementFocusedY;

			return true;
		}

		private string GetTipLabelNoneState()
		{
			string scrollBinding = DownslopeInput.ACTIONS[DownslopeInput.LEVELCREATOR_SCROLL].GetBindingDisplayString(0);
			string shiftBinding = DownslopeInput.ACTIONS[DownslopeInput.LEVELCREATOR_SHIFT].GetBindingDisplayString(0);
			string ctrlBinding = DownslopeInput.ACTIONS[DownslopeInput.LEVELCREATOR_CTRL].GetBindingDisplayString(0);
			string ct_in = Util.GetColorTag(TipInputHighlightColor);
			string ct_key = Util.GetColorTag(TipKeywordHighlightColor);
			string ct_end = Util.EndColorTag;

			string msg = "EACH CHUNK IS 1M IN GAME DISTANCE.\n\n" +
				$"THE {ct_key}WIDTH{ct_end} OF THE CHUNK CAN BE SET IN THE RIGHT PANEL.\n" +
				$"THIS DETERMINES THE OUT OF BOUNDS BORDER FOR THE LEVEL.\n\n" +
				$"THE {ct_key}XSHIFT{ct_end} CAN BE SET IN THE RIGHT PANEL, BY MAX INCREMENTS OF 1.\n" +
				$"THE NUMBER ON THE RIGHT OF EACH CHUNK IS THE DISTANCE ID.\n" +
				$"YOU CAN SCROLL UP AND DOWN THROUGH THE LEVEL.\n\n" +
				$"{ct_in} {scrollBinding}{ct_end}\n" +
				$"INCREMENT BY {ScrollIncrement_Normal}\n\n" +
				$"{ct_in} {shiftBinding} + {scrollBinding} {ct_end}\n" +
				$"INCREMENT BY {ScrollIncrement_Shift}\n\n" +
				$"{ct_in} {ctrlBinding} + {scrollBinding} {ct_end}\n" +
				$"INCREMENT BY {ScrollIncrement_Ctrl}\n\n" +
				$"CLICK ON THE {ct_key}PLACE{ct_end} BUTTON AND SELECT A LAYER TO START PLACING OBJECTS.";

			return msg;
		}

		private string GetTipLabelPlaceState()
		{
			string placeBinding = DownslopeInput.ACTIONS[DownslopeInput.LEVELCREATOR_PLACE].GetBindingDisplayString(0);
			string ct_in = Util.GetColorTag(TipInputHighlightColor);
			string ct_key = Util.GetColorTag(TipKeywordHighlightColor);
			string ct_end = Util.EndColorTag;

			string msg = "SELECT A LAYER IN THE BOTTOM PANEL.\n" +
				$"SELECT A PLACEABLE AND {ct_in} {placeBinding} {ct_end} to place the object. \n\n" +
				$"SELECT THE {ct_key}ERASER{ct_end} PLACEABLE FOR EACH LAYER TO ERASE OBJECTS.\n\n" +
				$"SELECT THE LEVEL LAYER TO PLACE THE {ct_key}START{ct_end} AND {ct_key}END{ct_end} POINTS OF THE LEVEL.\n\n";

			return msg;
		}

		private string GetTipLabelSaveLoadState()
		{
			string placeBinding = DownslopeInput.ACTIONS[DownslopeInput.LEVELCREATOR_PLACE].GetBindingDisplayString(0);
			string ct_in = Util.GetColorTag(TipInputHighlightColor);
			string ct_key = Util.GetColorTag(TipKeywordHighlightColor);
			string ct_end = Util.EndColorTag;
			string msg = "TO SAVE YOUR MAP, ENTER THE NAME AND FILENAME THEN CLICK SAVE.\n" +
				"YOUR MAP SAVES ARE LOCATED IN:\n" +
				$"{ct_key}{DownslopeFiles.GetPersistentMapsFolderPath()}{ct_end} \n\n" +
				$"TO LOAD A MAP CLICK ON A MAP IN THE LIST AND CLICK THE LOAD BUTTON. \n" +
				$"NOTE THAT THE FOLLOWING MUST BE TRUE TO SAVE YOUR MAP:\n" +
				$"1. THERE MUST BE A {ct_key}START POINT{ct_end}.\n" +
				$"2. THERE MUST BE AN {ct_key}END POINT{ct_end} AT LEAST 100M.\n" +
				$"3. THE MAP NAME DOES NOT CONTAIN SPECIAL CHARACTERS.";

			return msg;
		}

		private void RefreshPlacementView()
		{
			_game.UI.LC_ClearChoices();

			List<string> vals = LayerPlaceables[_currSelectedPlacementType].Keys.ToList();

			for (int i = 0; i < vals.Count; i++)
			{
				LevelCreatorPlaceable placeable = LayerPlaceables[_currSelectedPlacementType][vals[i]];
				string placementID = placeable.PlacementID;
				SpriteLayerTypes layerType = placeable.LayerType;
				Sprite icon = Sprites.GetSprite(placeable.IconID);
				Debug.Log(icon.name);
				_game.UI.LC_CreateChoiceButton(icon,
					() => { 
						SelectPlacementChoice(layerType, placementID); 
					}, 
					() => {
						ShowTopMessage($"{placeable.DisplayName}");
					});
			}
		}

		private void GeneratePlaceables()
		{
			var layerTypeValues = System.Enum.GetValues(typeof(SpriteLayerTypes));

			foreach(SpriteLayerTypes layer in layerTypeValues)
			{
				_spriteLayerTypeStrings[layer] = layer.ToString();
				_spriteLayerTypeValues[layer.ToString()] = layer;
				GeneratePlaceableStructure(layer);

				for (int i = 0; i < Assets.LevelCreatorPlaceables.Count; i++)
				{
					if (Assets.LevelCreatorPlaceables[i].LayerType == layer)
					{
						LayerPlaceables[layer].Add(Assets.LevelCreatorPlaceables[i].PlacementID, Assets.LevelCreatorPlaceables[i]);
					}
				}
			}
		}

		private void GeneratePlaceableStructure(SpriteLayerTypes layerType)
		{
			if (!LayerPlaceables.ContainsKey(layerType))
			{
				LayerPlaceables[layerType] = new Dictionary<string, LevelCreatorPlaceable>();
			}
		}

		private void SetPreviousStateCallback(Action callback)
		{
			PreviousStateCallback = callback;
		}

		private void CheckCallbackPreviousState(LevelCreatorStates nextState)
		{
			if (PreviousStateCallback == null)
				return;

			if(_state != LevelCreatorStates.None && _state != nextState)
			{
				PreviousStateCallback.Invoke();
			}
		}

		private void UpdatePlacementMode()
		{
			if(DownslopeUI.MouseOverUI())
			{
				return;
			}

			Vector3 cursorScreenPos = new Vector3(DownslopeInput.CURSOR_POSITION.x, DownslopeInput.CURSOR_POSITION.y, -10);
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(cursorScreenPos);
			worldPos.z = 0;
			worldPos.x = (float)System.Math.Round(worldPos.x, System.MidpointRounding.ToEven);
			worldPos.y = (float)System.Math.Round(worldPos.y, System.MidpointRounding.ToEven);
			worldPos.x += GridOffset.x;
			worldPos.y += GridOffset.y;

			UpdatePlacementIndicatorColor();
			IndicatorCursorSprite.color = _currIndicatorColor;

			IndicatorCursorSprite.transform.position = worldPos;
			ActivePlacementCursorSprite.transform.position = worldPos;
		}

		private void UpdatePlacementIndicatorColor()
		{
			if(!_hasPlaceableSelected)
			{
				_currIndicatorColor = CursorSelectErrorColor;
				return;
			}

			float x = 0f;
			uint y = 0;

			bool validPlacementPosition = TryGetPlacementPosition(out x, out y);
			if (!validPlacementPosition)
			{
				_currIndicatorColor = CursorSelectErrorColor;
				return;
			}

			if (_currLevelChunkData.ContainsKey(y))
			{
				if (_currLevelChunkData[y].PlacementData[_currSelectedPlaceable.LayerType].ContainsKey(x))
				{
					_currIndicatorColor = CursorSelectReplaceColor;
					return;
				}
			}

			_currIndicatorColor = CursorSelectDefaultColor;
		}

		private void UpdateChunkEditScroll()
		{
			for(int j = 0; j < ForwardChunkEditDistance; j++)
			{
				for (int i = -_currFocusedChunkEdge; i < _currFocusedChunkEdge + 1; i++)
				{
					Color color = ForwardChunkColor;
					Vector3 worldPos = WorldRootCenter.position;
					worldPos.x += i;
					worldPos.y -= j;

					Vector3Int worldCell = GridTilemap.WorldToCell(worldPos);

					GridTilemap.SetTileFlags(worldCell, TileFlags.None);
					if (j == 0)
					{
						color = ActiveChunkColor;
					}

					GridTilemap.SetColor(worldCell, color);
				}
			}
		}

		private void UpdateChunkDirectionScroll()
		{
			if(DownslopeUI.MouseOverUI() || _state == LevelCreatorStates.Save || _state == LevelCreatorStates.Load)
			{
				return;
			}

			uint modifier = 1;

			if(_controlMode)
			{
				modifier = 100;
			}

			if(DownslopeInput.AXIS_LEVELCREATOR_SCROLL > 0)
			{
				if(_currFocusedChunk == 0 || (_controlMode && _currFocusedChunk < modifier))
				{
					Debug.Log($"LevelCreator: UpdateChunkDirectionScroll: Cannot scroll further up.");
					return;
				}

				_currFocusedChunk -= modifier;
				UpdateChunkPlacements();
			}
			else if (DownslopeInput.AXIS_LEVELCREATOR_SCROLL < 0)
			{
				_currFocusedChunk += modifier;
				UpdateChunkPlacements();
			}

			string msg = $"{_currFocusedChunk} TO {_currFocusedChunk + ForwardChunkEditDistance}";
			Util.Log(msg, this.name);
			OnUpdateChunkScrollRange.Invoke(msg);
		}

		private void UpdateChunkPlacements()
		{
			OnClearLevel();


			// iterate forward chunks
			for (uint j = 0; j < ForwardChunkEditDistance; j++)
			{
				uint focused = _currFocusedChunk + j;
				int currFocusedInt = (int)(focused);
				NumberGridText[(int)j].text = currFocusedInt.ToString();

				if (!_currLevelChunkData.ContainsKey(focused))
				{
					// no data, place only default border
					LevelCreatorObject obj;
					Vector3 pos = new Vector3(DefaultChunkEdgeSize - ChunkBaseOffsetX, ChunkBaseOffsetY - (int)j, 0);
					CreateLevelCreatorObject(pos, LayerPlaceables[SpriteLayerTypes.Obstacle]["tree-large-0"], out obj);
					pos = new Vector3(-DefaultChunkEdgeSize + ChunkBaseOffsetX, ChunkBaseOffsetY - (int)j, 0);
					CreateLevelCreatorObject(pos, LayerPlaceables[SpriteLayerTypes.Obstacle]["tree-large-0"], out obj);
					continue;
				}

				List<SpriteLayerTypes> activeLayersInChunk = _currLevelChunkData[focused].PlacementData.Keys.ToList();

				for(int i = 0; i < activeLayersInChunk.Count; i++)
				{
					List<float> activePositionsInLayer = _currLevelChunkData[focused].PlacementData[activeLayersInChunk[i]].Keys.ToList();

					for(int k = 0; k < activePositionsInLayer.Count; k++)
					{
						LevelCreatorPlaceable placeable = _currLevelChunkData[focused].PlacementData[activeLayersInChunk[i]][activePositionsInLayer[k]];

						Vector3 pos = new Vector3(activePositionsInLayer[k], ChunkBaseOffsetY - (int)j, 0);

						LevelCreatorObject obj;
						CreateLevelCreatorObject(pos, placeable, out obj);
					}
				}
			}
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{

		}

		private void Start()
		{
			GeneratePlaceables();
			DownslopeInput.SubscribeInputCancelled(DownslopeInput.LEVELCREATOR_PLACE, PlaceCurrentSelectedObject);
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.LEVELCREATOR_CTRL, EnableControlMode);
			DownslopeInput.SubscribeInputCancelled(DownslopeInput.LEVELCREATOR_CTRL, DisableControlMode);

			DownslopeInput.SubscribeInputPerformed(DownslopeInput.LEVELCREATOR_SHIFT, EnableShiftMode);
			DownslopeInput.SubscribeInputCancelled(DownslopeInput.LEVELCREATOR_SHIFT, DisableShiftMode);

			InitializeEvents();
			PrimaryObjectPooler.Initialize();

			OnClearLevel = () => { };
			_stateCallbacks[LevelCreatorStates.None] = () => { };
			_stateCallbacks[LevelCreatorStates.Place] = UpdatePlacementMode;
			_stateCallbacks[LevelCreatorStates.Erase] = () => { };
			_stateCallbacks[LevelCreatorStates.Move] = () => { };
			_stateCallbacks[LevelCreatorStates.Save] = () => { };
			_stateCallbacks[LevelCreatorStates.Load] = () => { };
			_stateCallbacks[LevelCreatorStates.Info] = () => { };
			SetNoneMode();

			//DownslopeInput.SubscribeInputPerformed(DownslopeInput.LEVELCREATOR_SCROLL, UpdateChunkDirectionScroll);
		}

		private void Update()
		{
			if(!_isActive)
			{
				return;
			}

			_stateCallbacks[_state].Invoke();
			UpdateChunkEditScroll();
			UpdateChunkDirectionScroll();
			PrimaryObjectPooler.CheckTrim();

			if(_topMsgShowing)
			{
				_topMsgShowTimer += Time.deltaTime;
				if(_topMsgShowTimer > TopMessageShowTime)
				{
					_topMsgShowTimer = 0f;
					_game.UI.LC_EnableTopMessagePanel(false);
					_topMsgShowing = false;
				}
			}
		}

		#endregion
	}
}

