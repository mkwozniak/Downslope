using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using Wozware.Downslope;
using Action = System.Action;

namespace Wozware.Downslope
{
	[System.Serializable]
	public sealed partial class Game
	{
		#region Events

		private event Action OnFinishDistanceTutorialStage;

		#endregion

		#region Public Members

		#endregion

		#region Private Members

		[ReadOnly][SerializeField] private bool _distanceTutorialStage = false;
		[ReadOnly][SerializeField] private bool[] _currTuturialInputsCompleted;
		[ReadOnly][SerializeField] private int _currTutorialStage = 0;
		[ReadOnly][SerializeField] private int _currTutorialObstacleStage = 0;
		[ReadOnly][SerializeField] private int _currDistanceTutorialVal = 0;

		private Dictionary<string, List<Action>> _tutorialInputPerformCallbacks = new Dictionary<string, List<Action>>();
		private Dictionary<string, List<Action>> _tutorialInputCancelCallbacks = new Dictionary<string, List<Action>>();

		#endregion

		#region Public Methods

		/// <summary>
		/// Resets the current tutorial progress data.
		/// </summary>
		public void ResetTutorialProgress()
		{
			_currTutorialStage = 0;

			for (int i = 0; i < _currTuturialInputsCompleted.Length; i++)
			{
				_currTuturialInputsCompleted[i] = false;
			}
		}

		/// <summary>
		/// Reset tutorial data and starts the first tutorial stage.
		/// </summary>
		public void StartTutorialInitialStage()
		{
			ResetTutorialProgress();
			_ui.ShowTutorialView();
			_ui.SetTutorialMessageLabel(_ui.Tutorial.TutorialStages[_currTutorialStage].Message);
			_world.SetLayerWeight(RanWeightedLayerTypes.SnowVariation, _mapAssets.TutorialMapList[_currTutorialObstacleStage].SnowVariationWeights);
			PauseGameTime(true);

			Debug.Log("Tutorial: StartTutorialInitialStage");

			// subscribe to enter input
			SubscribeTutorialInputPerformed(DownslopeInput.GAME_ENTER, EnterTutorialStagePostInitial);
			//DownslopeInput.SubscribeInputPerformed(DownslopeInput.ENTER, EnterTutorialStageCarving);
		}

		private void SubscribeTutorialInputPerformed(string input, Action action)
		{
			if(!_tutorialInputPerformCallbacks.ContainsKey(input))
			{
				_tutorialInputPerformCallbacks[input] = new List<Action>();
			}

			_tutorialInputPerformCallbacks[input].Add(action);
			DownslopeInput.SubscribeInputPerformed(input, action);
		}

		private void SubscribeTutorialInputCancelled(string input, Action action)
		{
			if (!_tutorialInputCancelCallbacks.ContainsKey(input))
			{
				_tutorialInputCancelCallbacks[input] = new List<Action>();
			}

			_tutorialInputCancelCallbacks[input].Add(action);
			DownslopeInput.SubscribeInputCancelled(input, action);
		}

		private void ClearTutorialInputPerformed(string input)
		{
			if (!_tutorialInputPerformCallbacks.ContainsKey(input))
			{
				return;
			}

			for (int i = 0; i < _tutorialInputPerformCallbacks[input].Count; i++)
			{
				DownslopeInput.UnsubscribeInputPerformed(input, _tutorialInputPerformCallbacks[input][i]);
			}
		}

		private void ClearTutorialInputCancelled(string input)
		{
			if (!_tutorialInputCancelCallbacks.ContainsKey(input))
			{
				return;
			}

			for (int i = 0; i < _tutorialInputCancelCallbacks[input].Count; i++)
			{
				DownslopeInput.UnsubscribeInputCancelled(input, _tutorialInputCancelCallbacks[input][i]);
			}
		}

		private void EnterTutorialStagePostInitial()
		{
			EnterNextTutorialStageView();

			ClearTutorialInputPerformed(DownslopeInput.GAME_ENTER);

			SubscribeTutorialInputPerformed(DownslopeInput.GAME_ENTER, EnterTutorialStageCarving);
			Debug.Log("Tutorial: EnterTutorialStagePostInitial");
		}

		private void EnterTutorialStageCarving()
		{
			// enter next tutorial view
			EnterNextTutorialStageView();

			// unsubscribe any previous events on ENTER input
			ClearTutorialInputPerformed(DownslopeInput.GAME_ENTER);

			// subscribe new events to ENTER input
			SubscribeTutorialInputPerformed(DownslopeInput.GAME_ENTER, ContinueTutorialCarvingStage);
			SubscribeTutorialInputPerformed(DownslopeInput.GAME_ENTER, _player.StartInitialMovement);

			// pause game
			PauseGameTime(true);

			Debug.Log("Tutorial: EnterTutorialStageCarving");
		}

		private void EnterTutorialStageBraking()
		{
			if (_currTuturialInputsCompleted[0] && _currTuturialInputsCompleted[1])
			{
				// unsubscribe any previous events on CARVE inputs
				ClearTutorialInputCancelled(DownslopeInput.PLAYER_CARVE_LEFT);
				ClearTutorialInputCancelled(DownslopeInput.PLAYER_CARVE_RIGHT);

				// subscribe new events to BRAKE and ENTER input
				SubscribeTutorialInputCancelled(DownslopeInput.PLAYER_BRAKE, EnterTutorialStageSendIt);
				SubscribeTutorialInputPerformed(DownslopeInput.GAME_ENTER, ContinueTutorialPrompt);

				// pause game
				PauseGameTime(true);

				// hide objective view and show panel
				_ui.ShowTutorialObjectiveView(false);
				_ui.ShowTutorialMessageView(true);

				// enter next tutorial view
				EnterNextTutorialStageView();

				// next stage is obstacles so prepare data
				_currTutorialObstacleStage = 0;
				_world.SetLayerWeight(RanWeightedLayerTypes.Obstacle, _mapAssets.TutorialMapList[0].ObstacleWeights, _world.ObstacleSeed);
				_world.SetLayerWeight(RanWeightedLayerTypes.ObstacleIce, _mapAssets.TutorialMapList[0].IceObstacleWeights, _world.ObstacleSeed);
				_world.SetLayerWeight(RanWeightedLayerTypes.IcePath, _mapAssets.TutorialMapList[0].IcePathWeights, _world.ObstacleSeed);
			}
		}

		private void EnterTutorialStageSendIt()
		{
			// enter next tutorial view
			EnterNextTutorialStageView();

			// pause game
			PauseGameTime(true);
			_ui.ShowTutorialObjectiveView(false);

			// unsubcribe any previous events on BRAKE or ENTER input
			ClearTutorialInputCancelled(DownslopeInput.PLAYER_BRAKE);
			ClearTutorialInputPerformed(DownslopeInput.GAME_ENTER);

			SubscribeTutorialInputCancelled(DownslopeInput.PLAYER_SENDIT, EnterNextObstacleTutorialStage);
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.GAME_ENTER, ContinueTutorialPrompt);
		}

		private void EnterNextObstacleTutorialStage()
		{
			// if the obstacle stage is 0, we just came from the previous non obstacle stage
			if(_currTutorialObstacleStage == 0)
			{
				if(IS_PAUSED)
				{
					return;
				}

				// unsubcribe any previous events on SENDIT or ENTER input
				ClearTutorialInputCancelled(DownslopeInput.PLAYER_SENDIT);
				ClearTutorialInputPerformed(DownslopeInput.GAME_ENTER);

				_ui.ShowTutorialObjectiveView(false);
			}

			OnFinishDistanceTutorialStage -= EnterNextObstacleTutorialStage;

			if (_currTutorialStage >= _ui.Tutorial.TutorialStages.Count)
			{
				return;
			}

			PauseGameTime(true);
			EnterNextTutorialStageView();
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.GAME_ENTER, ContinueTutorialPrompt);

			_currTutorialObstacleStage++;

			if (_currTutorialObstacleStage >= _ui.Tutorial.TutorialStages.Count)
			{
				_distanceTutorialStage = false;
				_currDistanceTutorialVal = 0;
				return;
			}

			_world.SetLayerWeight(RanWeightedLayerTypes.Obstacle, _mapAssets.TutorialMapList[0].ObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.ObstacleIce, _mapAssets.TutorialMapList[_currTutorialObstacleStage].IceObstacleWeights, _world.ObstacleSeed);
			_world.SetLayerWeight(RanWeightedLayerTypes.IcePath, _mapAssets.TutorialMapList[0].IcePathWeights, _world.ObstacleSeed);

			_currDistanceTutorialVal = _world.DistanceTravelled();
			_distanceTutorialStage = true;
			OnFinishDistanceTutorialStage += EnterNextObstacleTutorialStage;
		}


		private void EnterNextTutorialStageView()
		{
			_currTutorialStage++;
			if (_currTutorialStage >= _ui.Tutorial.TutorialStages.Count)
			{
				_ui.HideTutorialView();
				return;
			}

			_ui.ShowTutorialMessageView(true);
			_ui.SetTutorialMessageLabel(_ui.Tutorial.TutorialStages[_currTutorialStage].Message);
			if(_currTutorialStage > _ui.Tutorial.InitialStageThreshold)
				PlaySound(SoundID.CHALLENGE_SUCCESS);
			else
				PlaySound("UIOptionHover");
		}

		private void ContinueTutorialCarvingStage()
		{
			Debug.Log("Tutorial: ContinueTutorialCarvingStage");

			// unsubscribe any previous events on ENTER input
			ClearTutorialInputPerformed(DownslopeInput.GAME_ENTER);

			// subscribe to new events on cancel for CARVE inputs
			SubscribeTutorialInputCancelled(DownslopeInput.PLAYER_CARVE_LEFT, TutorialCarveLeft);
			SubscribeTutorialInputCancelled(DownslopeInput.PLAYER_CARVE_RIGHT, TutorialCarveRight);

			// continue tutorial prompt
			ContinueTutorialPrompt();
		}

		private void ContinueTutorialPrompt()
		{
			// unpause
			PauseGameTime(false);

			// hide tutorial panel and show objective
			_ui.ShowTutorialMessageView(false);
			_ui.ShowTutorialObjectiveView(true);
			_ui.SetTutorialObjectiveLabel(_ui.Tutorial.TutorialStages[_currTutorialStage].Objective);
		}

		private void TutorialCarveLeft()
		{
			_currTuturialInputsCompleted[0] = true;
			EnterTutorialStageBraking();
		}

		private void TutorialCarveRight()
		{
			_currTuturialInputsCompleted[1] = true;
			EnterTutorialStageBraking();
		}

		#endregion

		#region Private Methods

		private void UpdateTutorial()
		{
			if(!_distanceTutorialStage)
			{
				return;
			}

			int obstacleTutorialTravelled = _world.DistanceTravelled() - _currDistanceTutorialVal;
			if (obstacleTutorialTravelled >= TUTORIAL_OBSTACLE_STAGE_DIST)
			{
				_distanceTutorialStage = false;
				OnFinishDistanceTutorialStage.Invoke();
			}

			_ui.SetTutorialObjectiveLabel($"SURVIVE {TUTORIAL_OBSTACLE_STAGE_DIST - obstacleTutorialTravelled} METRES");
		}

		#endregion
	}

}
