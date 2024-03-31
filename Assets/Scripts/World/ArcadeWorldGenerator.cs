using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wozware.Downslope
{
	public sealed partial class WorldGenerator : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members

		#endregion

		#region Private Members

		private uint _currIcePathCountID;
		private uint _currChunkCountID;

		// random weighted layers
		private Dictionary<RanWeightedLayerTypes, WorldWeightedRandomLayerData> _currWeightedLayers;

		// active chunk obstacles
		private Dictionary<float, ChunkFlagTypes> _currActiveChunkFlags;

		// active ice paths
		private Dictionary<int, WorldIcePath> _currActiveIcePathObjects;
		private Dictionary<float, ActiveIcePathData> _currActiveIcePaths;
		private Dictionary<float, WorldIcePath> _lastCreatedIcePaths;
		private Dictionary<float, int> _currOccupiedIce;
		private List<float> _icePathDeletion = new();

		private List<string> _currObstaclePossibilities;

		#endregion

		#region Public Methods

		/// <summary> Sets the arcade generator layer weight type to the given weights. </summary>
		/// <param name="id"> The layer type of the weight. </param>
		/// <param name="weights"> The weights to set. </param>
		/// <param name="seed"> The random seed of the layer. </param>
		public void SetLayerWeight(RanWeightedLayerTypes id, List<WeightIdentity> weights, int seed = 0)
		{
			_currWeightedLayers[id] = new WorldWeightedRandomLayerData();

			_currWeightedLayers[id].Rand = new System.Random();
			if (seed != 0)
			{
				_currWeightedLayers[id].Rand = new System.Random(seed);
			}

			List<int> numWeights = new List<int>();
			for (int i = 0; i < weights.Count; i++)
			{
				numWeights.Add(weights[i].Weight);
				_currWeightedLayers[id].Possibilities.Add(weights[i].Name);
			}

			_currWeightedLayers[id].Distribution = new DDRandom(numWeights, _currWeightedLayers[id].Rand);
		}

		/// <summary> Generates the next random (world, obstacles, etc) 
		/// arcade world chunk from last generated position. </summary>
		public void ArcadeWorldGenerateNextChunks()
		{
			Vector3 defaultVector = Vector3.zero;
			Vector3 pos = defaultVector + WorldCenter.position;
			Vector3 lastStartingChunkPos = pos;

			if(_firstPath)
			{
				_currIcePathCountID = 0;
				_currChunkCountID = 0;
			}

			if (!_firstPath)
			{
				// if not first path, last pos is the last active chunk y - 1
				lastStartingChunkPos.y = _activeChunks[_lastChunkID].transform.position.y - 1;

				// get a new random directional ice path width from last path possibilities
				// RandomizeIcePath();
			}

			int icePathAbove = 0;
			int chunkAbove = 0;

			_currActiveChunkFlags.Clear();

			for (int i = 0; i < PathForwardGenerationDistance; i++)
			{
				// pos of chunk is current height + the last chunk y
				pos = new Vector3(0, _chunkDirection * i, 0) + lastStartingChunkPos;
				icePathAbove = _lastIcePathID;
				chunkAbove = _lastChunkID;

				// randomize world
				RandomizeWorld(2);

				// create next chunk
				CreateChunkObject(pos);
				float yPos = _activeChunks[_lastChunkID].transform.position.y;

				// randomize ice path
				RandomizeIcePath(yPos);

				// generate obstacles
				GenerateChunkObstacles(yPos);

				// generate the level edges
				GenerateRowLevelEdge(yPos);

				/// TODO: generate the wilderness

				// if the first chunk
				if (i == 0)
				{
					// subscribe to its height event
					// chunk will auto generate next chunk when its passed the world height threshold
					_activeChunks[_lastChunkID].ReachHeightThresholdSubscribe(WorldAutoGenerate);
				}

				// iterate chunk id
				_currIcePathCountID++;
				_currChunkCountID++;
			}

			_firstPath = false;
		}

		#endregion

		#region Private Methods

		/// <summary> Generates a random obstacle at the given position with DDRandom. </summary>
		private void CheckGenerateObstacle(Vector3 pos, DDRandom ranDistribution)
		{
			int ran = ranDistribution.Next();

			if (ran >= _currObstaclePossibilities.Count)
				return;

			string currObstacle = _currObstaclePossibilities[ran];

			// no obstacle from random, return
			if (currObstacle == "Empty")
			{
				return;
			}

			// if obstacle and ice there already, return
			if (_currActiveChunkFlags.ContainsKey(pos.x))
			{
				if (_currActiveChunkFlags[pos.x] == ChunkFlagTypes.Obstacle)
				{
					return;
				}
			}

			if (_currOccupiedIce.ContainsKey(pos.x))
			{
				if (_currOccupiedIce[pos.x] == 1)
				{
					return;
				}
			}

			// if this obstacle is extended, make sure it can fit
			if (Assets.ObstacleIDs[currObstacle].RightExtendedObstacle)
			{
				if(!CheckRightExtendedObstaclesValid(pos, currObstacle))
				{
					return;
				}
			}

			// if this obstacle is extended, make sure it can fit
			if (Assets.ObstacleIDs[currObstacle].TopExtendedObstacle)
			{
				if (!CheckTopExtendedObstaclesValid(pos, currObstacle))
				{
					return;
				}
			}

			// ok to create the obstacle
			CreateObstacle(currObstacle, pos);
		}

		private void GenerateOuterWilderness()
		{

		}

		private bool CheckRightExtendedObstaclesValid(Vector3 pos, string currObstacle)
		{
			string obsId = Assets.ObstacleIDs[currObstacle].RightExtendedObstacles[0];

			float posX = (pos.x + 1);

			if (posX >= _currRightBorder)
			{
				return false;
			}

			if (_currActiveChunkFlags.ContainsKey(posX))
			{
				if (_currActiveChunkFlags[posX] == ChunkFlagTypes.Obstacle)
				{
					return false;
				}
			}

			if (_currOccupiedIce.ContainsKey(posX))
			{
				if (_currOccupiedIce[posX] == 1)
				{
					return false;
				}
			}

			float posXIceExtended = posX + 0.5f;
			if (_currOccupiedIce.ContainsKey(posXIceExtended))
			{
				if (_currOccupiedIce[posXIceExtended] == 1)
				{
					return false;
				}
			}

			return true;
		}

		private bool CheckTopExtendedObstaclesValid(Vector3 pos, string currObstacle)
		{
			for (int i = 0; i < Assets.ObstacleIDs[currObstacle].TopExtendedObstacles.Count; i++)
			{
				string obsId = Assets.ObstacleIDs[currObstacle].TopExtendedObstacles[i];
				if (!Assets.ObstacleIDs[obsId].Obstrusive)
				{
					continue;
				}

				if (_currActiveChunkFlags.ContainsKey(pos.x) &&
					_currActiveChunkFlags[pos.x] == ChunkFlagTypes.Obstacle)
				{
					return false;
				}
			}

			return true;
		}

		private void CheckGenerateXOffsetVariation()
		{
			if (_currWeightedLayers[RanWeightedLayerTypes.XOffset].Distribution == null)
				return;

			if (_currWeightedLayers[RanWeightedLayerTypes.XOffset].Possibilities.Count == 0)
				return;

			int ran = _currWeightedLayers[RanWeightedLayerTypes.XOffset].Distribution.Next();
			string result = _currWeightedLayers[RanWeightedLayerTypes.XOffset].Possibilities[ran];

			if (result == WeightedRandomID.WORLD_TURN_FLAT)
				return;

			// left
			if (result == WeightedRandomID.WORLD_TURN_LEFT)
			{
				if (_currWorldXOffset - 0.5f < -MaxWorldXOffset)
					return;
				_currWorldXOffset -= 0.5f;
				_worldShifted = true;
				return;
			}

			// right
			if(result == WeightedRandomID.WORLD_TURN_RIGHT)
			{
				if (_currWorldXOffset + 0.5f > MaxWorldXOffset)
					return;
				_currWorldXOffset += 0.5f;
				_worldShifted = true;
			}
		}

		private void GenerateChunkEdgeVariation(int rightOffset)
		{
			if (_worldShifted)
				return;
			if (!_currWeightedLayers.ContainsKey(RanWeightedLayerTypes.WorldEdge))
				return;
			if (_currWeightedLayers[RanWeightedLayerTypes.WorldEdge].Distribution == null)
				return;
			if (_currWeightedLayers[RanWeightedLayerTypes.WorldEdge].Possibilities.Count == 0)
				return;

			WorldWeightedRandomLayerData data = _currWeightedLayers[RanWeightedLayerTypes.WorldEdge];
			int ran = data.Distribution.Next();

			// 0 is no variation
			if (ran == 0)
				return;

			float edgeSize = _leftWorldEdge + WeightedRandomID.WORLD_EDGE_VARIATIONS[data.Possibilities[ran]];
			if (edgeSize > MaxWorldEdgeSize)
			{
				edgeSize = MaxWorldEdgeSize;
			}
			else if(edgeSize < MinWorldEdgeSize)
			{
				edgeSize = MinWorldEdgeSize;
			}

			_leftWorldEdge = edgeSize;
			_rightWorldEdge = _leftWorldEdge - rightOffset;
		}

		private void GenerateChunkSnowVariation(Vector3 pos)
		{
			WorldWeightedRandomLayerData data = _currWeightedLayers[RanWeightedLayerTypes.SnowVariation];
			int ran = data.Distribution.Next();

			// 0 is no variation
			if (ran == 0)
				return;

			if (ran >= data.Possibilities.Count)
				return;

			string id = data.Possibilities[ran];
			CreateSnowVariation(id, pos);
		}

		private void GenerateChunkObstacles(float yPos)
		{
			WorldWeightedRandomLayerData data = _currWeightedLayers[RanWeightedLayerTypes.Obstacle];
			_currObstaclePossibilities = data.Possibilities;

			for (int i = (int)_currLeftBorder + 1; i < _currRightBorder; i++)
			{
				Vector3 colPos = new Vector3(i, yPos, 0);
				CheckGenerateObstacle(colPos, data.Distribution);
				GenerateChunkSnowVariation(colPos);
			}
		}

		private void GenerateRowLevelEdge(float yPos)
		{
			Vector3 edgeTreePos = new Vector3(0, yPos, 0);

			// generate left
			edgeTreePos.x = -_leftWorldEdge + _currWorldXOffset;
			CreateObstacle("LargeTree", edgeTreePos);

			// generate right
			edgeTreePos.x = (_rightWorldEdge + _currWorldXOffset);
			CreateObstacle("LargeTree", edgeTreePos);
		}

		private void RandomizeWorld(int rightOffset)
		{
			_worldShifted = false;
			CheckGenerateXOffsetVariation();
			GenerateChunkEdgeVariation(rightOffset);
			_currLeftBorder = (-_leftWorldEdge + _currWorldXOffset);
			_currRightBorder = (_rightWorldEdge + _currWorldXOffset);
		}

		private void OnDrawGizmos()
		{
			return;

			if (_currOccupiedIce == null)
				return;

			List<float> keys = _currOccupiedIce.Keys.ToList();
			foreach(float f in keys)
			{
				if (_currOccupiedIce[f] == 1)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawCube(new Vector3(f, 0, 0), new Vector3(.4f, .4f, .4f));
					continue;
				}

				Gizmos.color = Color.green;
				Gizmos.DrawCube(new Vector3(f, 0, 0), new Vector3(.4f, .4f, .4f));
			}		
		}

		/// <summary> Randomizes the next path to stay the same, turn, expand or contract. </summary>
		private void RandomizeIcePath(float yPos)
		{
			WorldWeightedRandomLayerData data = _currWeightedLayers[RanWeightedLayerTypes.IcePathSpawn];
			_currObstaclePossibilities = data.Possibilities;
			float currThickness = Assets.IcePathPrefabs["Path1Start"].Thickness;

			//int leftEdge = (int)(-_leftWorldEdge + _currWorldXOffset);
			//int rightEdge = (int)(_rightWorldEdge + _currWorldXOffset);
			float i;
			int j;

			for (i = _currLeftBorder + 2; i < _currRightBorder; i += 0.5f)
			{
				Vector3 colPos = new Vector3(i, yPos, 0);

				string id = data.Possibilities[data.Distribution.Next()];

				// check if should spawn a new ice path
				if (id == "Empty")
				{
					continue;
				}

				List<float> occupations = new();
				bool canOccupy = TryIcePathOccupy(i, currThickness, out occupations);

				if(canOccupy)
				{
					// trigger new ice path to spawn
					_currActiveIcePaths[i] = new(true, false, i, currThickness, colPos, "Start", occupations);
					SetIceOccupiedFromKey(i, 1);
				}
			}

			List<float> keys = _currActiveIcePaths.Keys.ToList();
			List<float> deletion = new();

			// spawn new ice paths if needed, and expand/contract any existing ice paths
			for (j = 0; j < keys.Count; j++)
			{
				float key = keys[j];
				Vector3 pos = new Vector3(_currActiveIcePaths[key].Position.x, yPos, 0);

				// if this is a fresh ice path
				if (_currActiveIcePaths[key].NotYetStarted)
				{
					// TODO: RANDOMIZE STARTING WIDTH		
					_lastCreatedIcePaths[key] = CreateIcePath(pos, "Path1Start", true);
					_currActiveIcePaths[key].NotYetStarted = false;
					_currActiveIcePaths[key].SizeId = _lastCreatedIcePaths[key].SizeID;
					continue;
				}

				// if this is a turning ice path
				if (_currActiveIcePaths[key].IsTurning)
				{
					List<float> occupations = new();
					string turnDir = _currActiveIcePaths[key].TurnDirection;

					// clear the new occupations for this ice path
					_currActiveIcePaths[key].NewOccupations.Clear();

					// shift the ice path occupation data and get the new occupations
					ShiftIcePathOccupationsAtKey(key, turnDir, out occupations);

					// shift the position
					float offset = 0.5f;
					if (turnDir == "Left")
					{
						offset = -0.5f;
					}

					pos.x = pos.x + offset;

					// create a new active ice path at the new shifted position with the new occupations
					_currActiveIcePaths[pos.x] = new(false, false, pos.x, _currActiveIcePaths[key].Thickness, pos, "", occupations);
					_currActiveIcePaths[pos.x].SizeId = _currActiveIcePaths[key].SizeId;

					// if this x position isn't the same as the previous, mark it for deletion
					if (key != pos.x)
					{
						deletion.Add(key);
						key = pos.x;
					}
				}
				
				// not starting path or turning path, get a random path variance from the weighted random
				WorldWeightedRandomLayerData turnData = _currWeightedLayers[RanWeightedLayerTypes.IcePath];
				_currObstaclePossibilities = turnData.Possibilities;
				int id = turnData.Distribution.Next();
				string ranId = turnData.Possibilities[id];
				string pathId = "None";

				if (ranId == WeightedRandomID.ICE_PATH_TURN)
				{
					bool canTurn = TryGenerateRandomIcePathTurning(key, pos, _currLeftBorder, _currRightBorder);
					if(canTurn)
					{
						continue;
					}

					// cannot turn, just default to flat
					ranId = WeightedRandomID.ICE_PATH_FLAT;
				}

				if(ranId == WeightedRandomID.ICE_PATH_EXPAND)
				{
					bool canExpand = TryGenerateRandomIcePathExpand(key, pos, _currLeftBorder, _currRightBorder);
					if (canExpand)
					{
						continue;
					}

					// cannot turn, just default to flat
					ranId = WeightedRandomID.ICE_PATH_FLAT;
				}

				if(ranId == WeightedRandomID.ICE_PATH_CONTRACT)
				{
					bool canContract = TryGenerateRandomIcePathContract(key, pos);
					if (canContract)
					{
						continue;
					}

					// cannot contract, just default to flat
					ranId = WeightedRandomID.ICE_PATH_FLAT;
				}

				// out of bounds
				if (pos.x < _currLeftBorder || pos.x > _currRightBorder)
				{
					ranId = WeightedRandomID.ICE_PATH_END;
				}

				pathId = WeightedRandomID.ICE_PATH_PREFIX + _currActiveIcePaths[key].SizeId + ranId;

				if (ranId == WeightedRandomID.ICE_PATH_END)
				{
					SetIceOccupiedFromKey(key, 0);
					deletion.Add(key);
				}

				if (!Assets.IcePathPrefabs.ContainsKey(pathId))
				{
					Debug.Log($"Path Type Not Found: {pathId}");
					continue;
				}

				TriggerNewIcePathFlatChunk(key, pos, pathId);
			}

			// clear and delete any ice paths marked for deletion
			for(j = 0; j < deletion.Count; j++)
			{
				float key = deletion[j];

				// before deletion, swap the ice path data (it may be turning)
				SwapIcePathOccupied(key);
				_currActiveIcePaths[key].Occupations.Clear();
				_currActiveIcePaths.Remove(key);
				_lastCreatedIcePaths.Remove(key);
			}
		}

		/// <summary>
		/// Check if the ice data is occupied from the start and thickness outward.
		/// Gives a list of new occupations when complete.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="thickness"></param>
		/// <param name="occupations"></param>
		/// <returns> False when any key in between the range is occupied. </returns>
		private bool TryIcePathOccupy(float start, float thickness, out List<float> occupations)
		{
			List<float> newOccupations = new();

			// go from the left to right from the start
			for (float k = start - thickness; k <= start + thickness; k += 0.5f)
			{
				if (_currOccupiedIce.ContainsKey(k) && _currOccupiedIce[k] == 1)
				{
					occupations = new();
					return false;
				}

				newOccupations.Add(k);
			}

			occupations = newOccupations;
			return true;
		}

		/// <summary>
		/// Tries to trigger a new turning ice path to spawn if there is space.
		/// </summary>
		/// <param name="key">The root path X position.</param>
		/// <param name="pos">The world position to spawn the path.</param>
		/// <param name="leftEdge">The left world edge limit.</param>
		/// <param name="rightEdge">The right world edge limit.</param>
		/// <returns> True if there is room for the ice to turn. </returns>
		private bool TryGenerateRandomIcePathTurning(float key, Vector3 pos, float leftEdge, float rightEdge)
		{
			int leftRight = Random.Range(0, 2);

			// reset and modify the occupied ice
			string pathTurnId = "Right";
			bool isLeft = false;
			float currXPos = _currActiveIcePaths[key].XPosition;
			float activeThickness = _currActiveIcePaths[key].Thickness;

			if (leftRight == 1)
			{
				pathTurnId = "Left";
				isLeft = true;
			}

			// check occupation of 2 tiles to the left or right instead of 1
			// this leaves a snowy gap between possible colliding paths
			float occupyCheck = (currXPos + activeThickness) + 0.5f;
			float occupyCheckFar = occupyCheck + 0.5f;
			if (isLeft)
			{
				occupyCheck = (currXPos - activeThickness) - 0.5f;
				occupyCheckFar = occupyCheck - 0.5f;
			}

			// check occupied and bounds

			if(IsIceOccupied(occupyCheck) || IsIceOccupied(occupyCheckFar))
			{
				return false;
			}

			if (occupyCheck - 1 < leftEdge || occupyCheck + 1 > rightEdge)
			{
				return false;
			}

			TriggerNewIcePathTurnChunk(key, pos, pathTurnId);
			return true;
		}

		/// <summary>
		/// Tries to trigger a new turning ice path to spawn if there is space.
		/// </summary>
		/// <param name="key">The root path X position.</param>
		/// <param name="pos">The world position to spawn the path.</param>
		/// <param name="leftEdge">The left world edge limit.</param>
		/// <param name="rightEdge">The right world edge limit.</param>
		/// <returns> True if there is room for the ice to turn. </returns>
		private bool TryGenerateRandomIcePathExpand(float key, Vector3 pos, float leftEdge, float rightEdge)
		{
			float currXPos = _currActiveIcePaths[key].XPosition;
			float activeThickness = _currActiveIcePaths[key].Thickness;

			if((_currActiveIcePaths[key].SizeId + 1) > 2)
			{
				return false;
			}

			float occupyCheckRight = (currXPos + activeThickness) + 0.5f;
			float occupyCheckLeft = (currXPos - activeThickness) - 0.5f;

			// check occupied and bounds

			if (IsIceOccupied(occupyCheckRight) || IsIceOccupied(occupyCheckLeft))
			{
				return false;
			}

			if (occupyCheckLeft - 1 < leftEdge || occupyCheckRight + 1 > rightEdge)
			{
				return false;
			}

			int sizeId = _currActiveIcePaths[key].SizeId;

			string pathId = "Path" + sizeId + WeightedRandomID.ICE_PATH_EXPAND;
			_lastCreatedIcePaths[key] = CreateIcePath(pos, pathId);
			_currActiveIcePaths[key].Thickness = _lastCreatedIcePaths[key].Thickness;
			_currActiveIcePaths[key].SizeId = _lastCreatedIcePaths[key].SizeID;
			_currActiveIcePaths[key].Occupations.Add(occupyCheckRight);
			_currActiveIcePaths[key].Occupations.Add(occupyCheckLeft);
			_currOccupiedIce[occupyCheckRight] = 1;
			_currOccupiedIce[occupyCheckLeft] = 1;
			return true;
		}

		/// <summary>
		/// Tries to trigger a new turning ice path to spawn if there is space.
		/// </summary>
		/// <param name="key">The root path X position.</param>
		/// <param name="pos">The world position to spawn the path.</param>
		/// <param name="leftEdge">The left world edge limit.</param>
		/// <param name="rightEdge">The right world edge limit.</param>
		/// <returns> True if there is room for the ice to turn. </returns>
		private bool TryGenerateRandomIcePathContract(float key, Vector3 pos)
		{
			float currXPos = _currActiveIcePaths[key].XPosition;
			float activeThickness = _currActiveIcePaths[key].Thickness;

			if ((_currActiveIcePaths[key].SizeId - 1) < 1)
			{
				return false;
			}

			int sizeId = _currActiveIcePaths[key].SizeId;

			float occupyCheckRight = (currXPos + activeThickness);
			float occupyCheckLeft = (currXPos - activeThickness);

			string pathId = "Path" + sizeId + WeightedRandomID.ICE_PATH_CONTRACT;
			_lastCreatedIcePaths[key] = CreateIcePath(pos, pathId);
			_currActiveIcePaths[key].Thickness = _lastCreatedIcePaths[key].Thickness;
			_currActiveIcePaths[key].SizeId = _lastCreatedIcePaths[key].SizeID;

			_currActiveIcePaths[key].Occupations.Remove(occupyCheckRight);
			_currActiveIcePaths[key].Occupations.Remove(occupyCheckLeft);
			_currOccupiedIce[occupyCheckRight] = 0;
			_currOccupiedIce[occupyCheckLeft] = 0;

			return true;
		}

		/// <summary>
		/// Swaps the current occupations of a path by key to its new occupations.
		/// </summary>
		/// <param name="key"> The root X position key of the path. </param>
		private void SwapIcePathOccupied(float key)
		{
			for (int k = 0; k < _currActiveIcePaths[key].Occupations.Count; k++)
			{
				float occupation = _currActiveIcePaths[key].Occupations[k];
				if (!_currActiveIcePaths[key].NewOccupations.Contains(occupation))
				{
					_currOccupiedIce[occupation] = 0;
				}
				else
				{
					_currOccupiedIce[occupation] = 1;
				}
			}
		}

		/// <summary>
		/// Checks if a specific X position key is occupied by ice.
		/// </summary>
		/// <param name="key">The occupied X position to check. </param>
		/// <returns> True if the position exists and is occupied, false otherwise. </returns>
		private bool IsIceOccupied(float key)
		{
			if (_currOccupiedIce.ContainsKey(key) && _currOccupiedIce[key] == 1)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Shifts the given ice path key by left or right given the direction.
		/// Gives a list of new shifted occupations.
		/// </summary>
		/// <param name="key">The root path X position. </param>
		/// <param name="turnDir">The turn direction "Left" or "Right" </param>
		/// <param name="occupations">The new list of shifted occupations.</param>
		private void ShiftIcePathOccupationsAtKey(float key, string turnDir, out List<float> occupations)
		{
			List<float> newOccupations = new();
			float offset = 0.5f;
			if (turnDir == "Left")
			{
				offset = -0.5f;
			}

			for (int k = 0; k < _currActiveIcePaths[key].Occupations.Count; k++)
			{
				// get the old occupation as new
				float newOccupation = _currActiveIcePaths[key].Occupations[k];
				newOccupation = newOccupation + offset;
				newOccupations.Add(newOccupation);
				_currActiveIcePaths[key].NewOccupations.Add(newOccupation);
			}

			occupations = newOccupations;
		}

		/// <summary>
		/// Triggers a new flat chunk to spawn.
		/// </summary>
		/// <param name="key">The root path X position. </param>
		/// <param name="pos">The world position to spawn. </param>
		/// <param name="pathId">The original id of the path.</param>
		private void TriggerNewIcePathFlatChunk(float key, Vector3 pos, string pathId)
		{
			_currActiveIcePaths[key].IcePathID = pathId;
			_currActiveIcePaths[key].IsTurning = false;
			_currActiveIcePaths[key].TurnDirection = "";
			_lastCreatedIcePaths[key] = CreateIcePath(pos, pathId);
		}

		/// <summary>
		/// Triggers a new turning chunk to spawn and shift next ice paths below depending on its turn id.
		/// </summary>
		/// <param name="key">The root path X position. </param>
		/// <param name="pos">The world position to spawn. </param>
		/// <param name="pathTurnId">The turn direction "Left" or "Right" </param>
		private void TriggerNewIcePathTurnChunk(float key, Vector3 pos, string pathTurnId)
		{
			string pathId = "Path" + _currActiveIcePaths[key].SizeId + WeightedRandomID.ICE_PATH_TURN + pathTurnId;
			_lastCreatedIcePaths[key] = CreateIcePath(pos, pathId);
			_currActiveIcePaths[key].IsTurning = true;
			_currActiveIcePaths[key].TurnDirection = pathTurnId;
		}

		/// <summary>
		/// Sets a X position in the ice data to be occupied or not given the value.
		/// </summary>
		/// <param name="key">The occupied X position.</param>
		/// <param name="value">The value, either 1 for occupied or 0 for not occupied. </param>
		private void SetIceOccupiedFromKey(float key, int value)
		{
			for (int k = 0; k < _currActiveIcePaths[key].Occupations.Count; k++)
			{
				_currOccupiedIce[_currActiveIcePaths[key].Occupations[k]] = value;
			}
		}

		#endregion
	}
}

