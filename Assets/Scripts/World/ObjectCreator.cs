using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
    public sealed partial class WorldGenerator : MonoBehaviour
    {	
		#region Public Methods

		public void CreatePlayerTrail(Vector3 pos, string id)
		{
			// create trail props
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Sprites.GetSpriteAnimation(id),
				SpriteSortData.LayerData[SpriteLayerTypes.Trail], ActiveObjectParent);

			// try create sprite
			WorldSprite sprite;
			bool spriteSuccess = TryCreateWorldSprite(objProps, DestroyObstacleObject, out sprite);
			if (!spriteSuccess)
			{
				Debug.LogError($"World CreatePlayerTrail failed. CreateWorldObject call returned false.");
				return;
			}

			sprite.name = $"TrailFX[{Sprites.AllSprites[id].Name}][{sprite.UID}]";
		}

		public void CreatePlayerPowderFX(Vector3 pos)
		{
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Sprites.GetSpriteAnimation(SpriteID.EMPTY),
				SpriteSortData.LayerData[SpriteLayerTypes.Ground], ActiveFXParent);
			WorldSprite fxSprite;
			bool spriteSuccess = TryCreateWorldSprite(objProps, DestroyObstacleObject, out fxSprite);
			if (!spriteSuccess)
			{
				Debug.LogError($"World CreatePlayerPowderFX failed. CreateWorldObject call returned false.");
				return;
			}
			GameObject g = Instantiate(Assets.CollidePlayerFX[CollisionTypes.Powder], fxSprite.transform.position, Quaternion.identity, fxSprite.transform);
			fxSprite.name = $"FX[{Assets.CollidePlayerFX[CollisionTypes.Powder].name}][{fxSprite.UID}]";
			Destroy(g, 1f);
		}

		public void CreatePFXSprite(string id, Vector3 pos)
		{
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Sprites.GetSpriteAnimation(SpriteID.EMPTY),
				SpriteSortData.LayerData[SpriteLayerTypes.Ground], ActiveFXParent);
			WorldSprite fxSprite;
			bool spriteSuccess = TryCreateWorldSprite(objProps, DestroyObstacleObject, out fxSprite);
			if (!spriteSuccess)
			{
				Debug.LogError($"World CreatePlayerPowderFX failed. CreateWorldObject call returned false.");
				return;
			}

			ParticleSystem pfx;
			if (!Assets.TryCreatePFX(id, pos, out pfx, fxSprite.transform))
			{
				return;
			}

			Destroy(pfx.gameObject, pfx.main.startLifetime.constant + 0.1f);
			fxSprite.name = $"FX[{pfx.name}][{fxSprite.UID}]";
		}

		public void CreateSnowVariation(string id, Vector3 pos)
		{
			// create snow props
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Sprites.GetSpriteAnimation(id),
				SpriteSortData.LayerData[SpriteLayerTypes.Ground], _activeChunks[_lastChunkID].transform);

			// try create sprite
			WorldSprite sprite;
			bool spriteSuccess = TryCreateWorldSprite(objProps, DestroyChunkedObstacleObject, out sprite, isChunked: true);
			if (!spriteSuccess)
			{
				Debug.LogError($"World CreateSnowVariation failed. CreateWorldObject call returned false.");
				return;
			}

			sprite.ParentChunk.AddWorldSprite(sprite);
		}

		#endregion

		#region Private Methods

		/// <summary> Creates and initializes a WorldSprite given sprite props and returns the WorldSprite.  </summary>
		private bool TryCreateWorldSprite(WorldSpriteProps objectProps, Action<WorldObject> destroyCallback, out WorldSprite sprite, bool isChunked = false)
		{
			// get sprite from pool			
			bool poolSuccess = PrimarySpritePooler.GetFromPool(out sprite);
			if (!poolSuccess)
			{
				Debug.LogError($"World CreateWorldObject failed. GetFromPool call returned false.");
				return false;
			}

			// initialize new sprite from props
			InitializeSpriteObject(sprite, objectProps.Sprite, objectProps.SortData);

			// initialize game object from props
			InitializeGameObject(sprite.gameObject, objectProps.Position, objectProps.Parent);

			// initialize world object from sprite and game object
			if (isChunked)
			{
				InitializeChunkedWorldSprite(sprite.UID, sprite.gameObject, sprite, destroyCallback);
				return true;
			}

			InitializeWorldSprite(sprite.UID, sprite.gameObject, sprite, destroyCallback);
			return true;
		}

		/// <summary> Create and initialize a chunk object. </summary>
		private void CreateChunkObject(Vector3 pos)
		{
			// pos.x += Assets.PathChunkPrefabs[id].XOffset;

			// get chunk from pool		
			WorldChunk chunk;
			bool poolSuccess = PrimaryChunkPooler.GetFromPool(out chunk);
			if (!poolSuccess)
			{
				Debug.LogError($"World CreateChunkObject failed. GetFromPool call returned false.");
				return;
			}

			InitializeGameObject(chunk.gameObject, pos, ActiveChunkParent);
			InitializeChunkObject(chunk, DestroyChunk);
		}

		/// <summary> Create and initialize an ice path at position given width id. </summary>
		private WorldIcePath CreateIcePath(Vector3 pos, string id, bool starting = false)
		{
			if (!Assets.IcePathPrefabs.ContainsKey(id))
			{
				Debug.LogError($"Cannot CreateChunk(). Chunk {id} does not exist.");
				return null;
			}

			WorldIcePath pathPrefab = Assets.IcePathPrefabs[id];
			if (starting)
			{
				pathPrefab = Assets.IcePathStartingPrefabs[id];
			}
			pos.x += pathPrefab.XOffset;

			WorldIcePath newPath = Instantiate(pathPrefab, pos, Quaternion.identity, ActiveChunkParent);
			InitializeIcePathObject(newPath, DestroyIcePath);
			return newPath;
		}

		private void CreateObstacle(string obstacleID, Vector3 pos)
		{
			// create new obstacle data and sprite props
			Obstacle obstacle = Assets.ObstacleIDs[obstacleID];
			Vector3 finalPos = pos + obstacle.Offset;

			// check for extended obstacle
			if (obstacle.RightExtendedObstacle)
			{
				for (int i = 0; i < obstacle.RightExtendedObstacles.Count; i++)
				{
					float colPosX = finalPos.x + (i + 1);

					if (_currActiveChunkFlags.ContainsKey(colPosX) &&
						_currActiveChunkFlags[colPosX] != ChunkFlagTypes.Empty)
					{
						continue;
					}

					CreateObstacle(obstacle.RightExtendedObstacles[i], new Vector3(colPosX, finalPos.y, finalPos.z));
					if (Assets.ObstacleIDs[obstacle.RightExtendedObstacles[i]].Obstrusive)
					{
						// add obstacle x position
						_currActiveChunkFlags[colPosX] = ChunkFlagTypes.Obstacle;
					}
				}
			}

			WorldSpriteProps objProps = new(finalPos, Sprites.GetSpriteAnimation(obstacle.SpriteID), SpriteSortData.LayerData[SpriteLayerTypes.Obstacle], _activeChunks[_lastChunkID].transform);

			// try create sprite
			WorldSprite sprite;
			bool spriteSuccess = TryCreateWorldSprite(objProps, DestroyChunkedObstacleObject, out sprite, isChunked: true);

			if (!spriteSuccess)
			{
				Debug.LogError($"World CreateObstacle failed. CreateWorldObject call returned false.");
				return;
			}

			// set the sprite obstacle data
			sprite.SetObstacleData(obstacle);

			// subscribe sprite events
			sprite.PlaySFX = CreateSFX;
			sprite.CreatePFX = CreatePFXSprite;
			sprite.ParentChunk.AddWorldSprite(sprite);

			if (obstacle.Impassable)
			{
				sprite.gameObject.layer = LayerMask.NameToLayer("Impassable");
			}

			if (obstacle.TopExtendedObstacle)
			{
				// TODO: TOP EXTENDED OBSTACLES
			}

			// add obstacle x position
			_currActiveChunkFlags[pos.x] = ChunkFlagTypes.Obstacle;
		}

		#endregion
	}
}
