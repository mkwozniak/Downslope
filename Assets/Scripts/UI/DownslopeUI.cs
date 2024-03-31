using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace Wozware.Downslope
{
	[System.Serializable]
	public sealed class DownslopeUI
	{
		#region Public Members

		public AssetPack Assets;

		public List<UIView> Views;

		[Header("Main Menu View")]
		public Transform MainMenuRootView;
		public Transform MainMenuFrontPanel;
		public Color CameraBG_Menu;
		public Color CameraBG_Map;

		[Header("Game View")]
		public Transform GameRootView;
		public Transform GameSidePanel;
		public Transform GameHealthPanel;
		public Transform EscapeRootView;
		public Transform ConfirmationPromptRootView;
		public Image HealthBarFront;
		public Transform GameOverView;

		public Text KMHLabel;
		public Text DistanceLabel;
		public Text ConfirmationPromptLabel;
		public Button ConfirmationPromptYesBtn;
		public Button ConfirmationPromptNoBtn;

		[Header("Level Creator View")]
		public LevelCreatorUI LevelCreator;

		[Header("Tutorial View")]
		public TutorialUI Tutorial;

		[Header("Map View")]
		public MapUI Map;
		public Transform DemoSelectionView;


		#endregion

		#region Private Members

		#endregion

		#region Static Members

		public static bool MouseOverUI()
		{
			return EventSystem.current.IsPointerOverGameObject();
		}

		public static void EnableUI(bool enable)
		{
			EventSystem.current.enabled = enable;
		}

		#endregion


		#region Public Methods

		public void ShowMainMenuFront()
		{
			EnableMainMenuRootView(true);
			MainMenuFrontPanel.gameObject.SetActive(true);
		}

		public void HideMainMenuFront()
		{
			EnableMainMenuRootView(false);
			MainMenuFrontPanel.gameObject.SetActive(false);
		}

		public void ShowGameView()
		{
			EnableGameRootView(true);
			GameSidePanel.gameObject.SetActive(true);
			GameHealthPanel.gameObject.SetActive(true);
		}

		public void HideGameView()
		{
			EnableGameRootView(false);
			GameSidePanel.gameObject.SetActive(false);
			GameHealthPanel.gameObject.SetActive(false);
		}

		public void ShowEscapeView()
		{
			EnableEscapeRootView(true);
		}

		public void HideEscapeView()
		{
			EnableEscapeRootView(false);
		}

		public void ShowGameOverView()
		{
			GameOverView.gameObject.SetActive(true);
		}

		public void HideGameOverView()
		{
			GameOverView.gameObject.SetActive(false);
		}

		public void ShowMapView()
		{
			EnableMapRootView(true);
		}

		public void HideMapView()
		{
			EnableMapRootView(false);
		}

		public void EnterConfirmationPromptView(string msg, UnityEngine.Events.UnityAction yesCallback, UnityEngine.Events.UnityAction noCallback)
		{
			ConfirmationPromptRootView.gameObject.SetActive(true);

			ConfirmationPromptLabel.text = msg;
			ConfirmationPromptYesBtn.onClick.RemoveAllListeners();
			ConfirmationPromptYesBtn.onClick.AddListener(yesCallback);

			ConfirmationPromptNoBtn.onClick.RemoveAllListeners();
			ConfirmationPromptNoBtn.onClick.AddListener(noCallback);
		}

		public void ExitConfirmationPromptView()
		{
			ConfirmationPromptLabel.text = "";
			ConfirmationPromptYesBtn.onClick.RemoveAllListeners();
			ConfirmationPromptNoBtn.onClick.RemoveAllListeners();

			ConfirmationPromptRootView.gameObject.SetActive(false);
		}

		public void ShowTutorialView()
		{
			EnableTutorialRootView(true);
			ShowTutorialMessageView(true);
			ShowTutorialObjectiveView(false);
		}

		public void HideTutorialView()
		{
			EnableTutorialRootView(false);
			ShowTutorialMessageView(false);
			ShowTutorialObjectiveView(false);
		}

		public void ShowTutorialMessageView(bool enable)
		{
			Tutorial.MessageView.SetActive(enable);
		}

		public void ShowTutorialObjectiveView(bool enable)
		{
			Tutorial.ObjectiveView.SetActive(enable);
		}

		public void SetTutorialMessageLabel(string text)
		{
			Tutorial.MessageLabel.text = text;
		}

		public void SetTutorialObjectiveLabel(string text)
		{
			Tutorial.ObjectiveLabel.text = text;
		}

		public void SetKMHLabel(float val)
		{
			KMHLabel.text = ((int)val).ToString();
		}

		public void SetDistanceLabel(float val)
		{
			string km = (val * 0.001f).ToString("F1");
			string m = ((int)val).ToString();
			DistanceLabel.text = (m).ToString();
		}

		public void SetHealthBarPercentage(float val)
		{
			if(val < 0 || val > 1)
			{
				return;
			}

			HealthBarFront.fillAmount = val;
		}

		#region Level Creator

		public void ShowLevelCreatorView()
		{
			EnableLevelCreatorRootView(true);
		}

		public void HideLevelCreatorView()
		{
			EnableLevelCreatorRootView(false);
		}

		public void LC_EnableLayerObjectChoicesPanel(bool enable)
		{
			LevelCreator.LayerObjectChoicesPanel.gameObject.SetActive(enable);
		}

		public void LC_EnableLayerChoicesPanel(bool enable)
		{
			LevelCreator.LayerChoicesPanel.gameObject.SetActive(enable);
		}

		public void LC_EnableInfoPanel(bool enable)
		{
			LevelCreator.InfoPanel.gameObject.SetActive(enable);
		}

		public void LC_EnableSavePanel(bool enable)
		{
			LevelCreator.SavePanel.gameObject.SetActive(enable);
		}

		public void LC_EnableSaveInputPanel(bool enable)
		{
			LevelCreator.SaveInputPanel.gameObject.SetActive(enable);
		}

		public void LC_SetSavePanelTitleLabel(string msg)
		{
			LevelCreator.SavePanelTitleLabel.text = msg;
		}

		public void LC_SetInfoPanelTipLabel(string msg)
		{
			LevelCreator.InfoTipLabel.text = msg;
		}

		public void LC_EnableTopMessagePanel(bool enable)
		{
			LevelCreator.TopMessagePanel.gameObject.SetActive(enable);
		}

		public void LC_SetTopMessageLabel(string msg)
		{
			LevelCreator.TopMessageLabel.text = msg;
		}

		public void LC_SetChunkRangeLabel(string val)
		{
			LevelCreator.ChunkRangeLabel.text = val;
		}

		public void LC_ClearSaveEntries()
		{
			for(int i = 0; i < LevelCreator.SaveEntryContentParent.childCount; i++)
			{
				UnityEngine.Object.Destroy(LevelCreator.SaveEntryContentParent.GetChild(i).gameObject);
			}
		}

		public void LC_CreateSaveEntry(MapFileData data, UnityEngine.Events.UnityAction overwriteCallback, UnityEngine.Events.UnityAction deleteCallback)
		{
			UIMapSaveEntry entry = UnityEngine.Object.Instantiate(Assets.UI_LevelCreatorSaveEntry, LevelCreator.SaveEntryContentParent);
			entry.MapNameLabel.text = data.Name;
			entry.MapPathLabel.text = data.FileName;
			entry.MapTimeLabel.text = data.Time;
			entry.OverwriteBtn.onClick.AddListener(overwriteCallback);
			entry.DeleteBtn.onClick.AddListener(deleteCallback);
		}

		public void LC_CreateLoadEntry(MapFileData data, UnityEngine.Events.UnityAction loadCallback, UnityEngine.Events.UnityAction deleteCallback)
		{
			UIMapSaveEntry entry = UnityEngine.Object.Instantiate(Assets.UI_LevelCreatorLoadEntry, LevelCreator.SaveEntryContentParent);
			entry.MapNameLabel.text = data.Name;
			entry.MapPathLabel.text = data.FileName;
			entry.MapTimeLabel.text = data.Time;
			entry.OverwriteBtn.onClick.AddListener(loadCallback);
			entry.DeleteBtn.onClick.AddListener(deleteCallback);
		}

		public void LC_CreateChoiceButton(Sprite icon, UnityEngine.Events.UnityAction callback, UnityEngine.Events.UnityAction hoverCallback)
		{
			UIElement element = UnityEngine.Object.Instantiate(Assets.UI_LevelCreatorPlacementChoice, LevelCreator.ChoiceParent);
			element.SourceImage.sprite = icon;
			//element.Icon.sprite = icon;
			element.Btn.onClick.AddListener(callback);
			element.OnHoverEvents.AddListener(hoverCallback);
		}

		public void LC_ClearChoices()
		{
			for(int i = 0; i < LevelCreator.ChoiceParent.childCount; i++)
			{
				UnityEngine.Object.Destroy(LevelCreator.ChoiceParent.GetChild(i).gameObject);
			}
		}

		public void LC_ClearSavePanelInputFields()
		{
			LevelCreator.SaveNameInputField.text = "";
			LevelCreator.SaveFileInputField.text = "";
		}

		public string LC_GetMapNameInputFieldText()
		{
			return LevelCreator.SaveNameInputField.text;
		}

		public string LC_GetMapFileNameInputFieldText()
		{
			return LevelCreator.SaveFileInputField.text;
		}

		#endregion

		#endregion

		#region Private Methods

		private void EnableMainMenuRootView(bool val)
		{
			MainMenuRootView.gameObject.SetActive(val);
		}

		private void EnableGameRootView(bool val)
		{
			GameRootView.gameObject.SetActive(val);
		}

		private void EnableTutorialRootView(bool val)
		{
			Tutorial.RootUIView.SetActive(val);
		}

		private void EnableEscapeRootView(bool val)
		{
			EscapeRootView.gameObject.SetActive(val);
		}

		public void EnableLevelCreatorRootView(bool enable)
		{
			LevelCreator.RootView.gameObject.SetActive(enable);
			LC_EnableLayerChoicesPanel(!enable);
			LC_EnableLayerObjectChoicesPanel(!enable);
			LC_EnableSavePanel(!enable);
		}

		private void EnableMapRootView(bool val)
		{
			Map.RootView.SetActive(val);
		}

		#endregion

	}
}

