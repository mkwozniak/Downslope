using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	public sealed partial class PlayerControl : MonoBehaviour
	{
		#region Private Members

		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		private void UpdateMovement(float timeScale)
		{
			// apply delta horizontal movement
			transform.position += _currHorizontalVelocity * timeScale;

			/*
			if (_stunned)
			{
				_stunnedTimer += DownslopeTime.DeltaTime;
				if (_stunnedTimer > StunTime)
				{
					RecoverFromStunned();
					_state = PlayerState.Moving;
				}
			}
			*/

			// don't update forward movement velocity if an object is right infront
			if (_collidingFront)
				return;

			// apply acceleration if not carving and trigger update
			if (_accelerating && !_carving)
			{
				_currAcceleration += AccelerationSpeed;
				_updateForwardSpeed = true;
			}

			// apply deceleration and trigger update
			if (_decelerating && _currAcceleration > MinimumForwardAcceleration)
			{
				_currAcceleration -= _currBrakingPower;
				_updateForwardSpeed = true;
			}
		}

		private void UpdateSpeed()
		{
			// update speed and invoke event that speed has changed 
			_currForwardSpeed = _currAcceleration * _currMaxForwardSpeed;
			OnUpdatedSpeed(_currForwardSpeed);
			_updateForwardSpeed = false;
		}

		private void UpdateInput()
		{
			if (IsAI)
				return;

			// check all input states from PlayerInput
			_inputHorizontal[0] = Input.GetKey(KeyCode.A);
			_inputHorizontal[1] = Input.GetKey(KeyCode.D);
			_inputBrake = Input.GetKey(KeyCode.Space);
			_inputStartMoving = Input.GetKeyDown(KeyCode.Space);
		}

		private void UpdateAnimators()
		{
			// update animator speeds
			Anim.speed = AnimatorSpeedModifier * _currGlobalAnimationSpeed;
			SnowFXAnimator.speed = SnowFXAnimatorSpeedModifier * _currForwardSpeed * _currGlobalAnimationSpeed;

			// update animator states
			Anim.SetBool("Accelerating", _carving ? false : _accelerating);

			// update blocked sprite animation 
			bool blockedAnim = _collidingFront;
			if (_stunned)
			{
				blockedAnim = false;
			}

			if(BlockedAnimator.isActiveAndEnabled)
			{
				BlockedAnimator.SetBool("Blocked", blockedAnim);
			}
		}

		private void UpdateAcceleration()
		{
			// if accelerating and colliding body, stop the player  
			if (_collidingBody && _accelerating && _collidingFront)
			{
				_accelerating = false;
				_currAcceleration = 0f;
				_updateForwardSpeed = true;
			}

			// stop accel/decel
			if (!_moving || _collidingFront)
				return;

			SkiAmbientSource.volume = _currAcceleration + .5f;
			SkiCarvingSource.volume = _currAcceleration + .5f;

			// speed up player if slower than min accel and jumping
			if (_currAcceleration <= MinimumForwardAcceleration && _airJumping)
			{
				_currAcceleration = MinimumForwardAcceleration;
				_accelerating = true;
			}

			// if accelerating and reached max, stop accel
			if (_accelerating)
			{
				if (_currAcceleration >= _currForwardMovePenalty)
				{
					_currAcceleration = _currForwardMovePenalty;
					_accelerating = false;
					return;
				}
			}

			// if decelerating and reached lowest, stop decel
			if (_decelerating)
			{
				if (_currAcceleration <= _currForwardMovePenalty)
				{
					_currAcceleration = _currForwardMovePenalty;
					_decelerating = false;
					return;
				}
			}

			// determine personal penalty for braking, or sending it
			float personalPenalty = _currForwardBrakePenalty;
			if(_sendingIt)
			{
				personalPenalty = _currForwardSendItPenalty;
			}

			// calculate the forward movement penalty for the player based on terrain, carving, and personal penalty
			_currForwardMovePenalty = _currForwardTerrainPenalty * _currForwardCarvePenalty * personalPenalty;

			// player should be accelerating if accel is less than penalty and not stunned
			if (_currAcceleration < _currForwardMovePenalty && !_stunned)
			{
				_accelerating = true;
				_decelerating = false;
			}

			// player should be decelerating if accel is greater than penalty
			if (_currAcceleration > _currForwardMovePenalty)
			{
				_decelerating = true;
				_accelerating = false;
			}
		}

		private void UpdateCarving()
		{
			if (_stunned)
				return;

			// apply horizontal carving penalty
			_currHorizontalForwardPenalty = (_currForwardSpeed * CarveForwardHorizontalModifier) + CarveMinimumSpeed;

			// clamp it
			if (_currHorizontalForwardPenalty > 1)
			{
				_currHorizontalForwardPenalty = 1;
			}

			// calculate current horizontal velocity
			_currHorizontalVelocity = (Vector3.right * PlayerHorizontalSpeed * _currHorizontalMoveBonus * _currHorizontalForwardPenalty) * _currHorizontalDir;

			if (_airJumping)
			{
				// limit horizontal movement while flying
				if (_currHorizontalMoveBonus > CarveAirDragPenalty)
				{
					_currHorizontalMoveBonus -= DownslopeTime.DeltaTime * CarveAirDragIntensity;
				}
				else
				{
					_currHorizontalMoveBonus = CarveAirDragPenalty;
				}

				// dont continue any other carving logic
				return;
			}

			// recover carve penalty from air jumping
			if (!_airJumping && !_fastCarvingEffects && _currHorizontalMoveBonus < 1)
			{
				_currHorizontalMoveBonus += DownslopeTime.DeltaTime * CarveAirRecovery;
				if (_currHorizontalMoveBonus >= 1)
				{
					_currHorizontalMoveBonus = 1;
				}
			}

			// no horizontal input or sending it
			if ((!_inputHorizontal[0] && !_inputHorizontal[1]) || _sendingIt)
			{
				_currHorizontalDir = 0;
				Anim.SetFloat("Horizontal", 0);

				// stop carving
				if (_carving)
				{
					StopCarvingSounds();
					_firstAfterCarve = true;
					_currTrailTime = StraightTrailTime;
					_carving = false;
				}

				// stop fast carving if was
				if (_fastCarvingEffects)
				{
					DisableFastCarvingEffects();
				}

				return;
			}

			// horizontal input, start carving
			if (!_carving)
			{
				_trailTimer = 0f;
				_currTrailTime = CarveTrailTime;
				_carving = true;
			}

			if (!SkiCarvingSource.isPlaying)
			{
				StartCarvingSounds();
			}

			// stop horizontal movement if colliding left or right
			bool canLeft = !_collidingLeft;
			bool canRight = !_collidingRight;

			// if the player is also colliding front while left and right
			// allow horizontal movement anyway
			if (_collidingFront)
			{
				if (_collidingLeft)
				{
					canLeft = true;
				}

				if (_collidingRight)
				{
					canRight = true;
				}
			}

			// apply horizontal movement
			if (_inputHorizontal[0] && canLeft)
			{
				_currHorizontalDir = -1;
				Anim.SetFloat("Horizontal", -1);
				return;
			}
			else if(_inputHorizontal[1] && canRight)
			{
				_currHorizontalDir = 1;
				Anim.SetFloat("Horizontal", 1);
				return;
			}

			// no horizontal
			_currHorizontalDir = 0;
			Anim.SetFloat("Horizontal", 0);
		}

		private void UpdateFastCarving()
		{
			// dont fast carve if not carving or air jumping
			if (!_carving || _airJumping)
				return;

			// if brake input is false and applied effects, reset effects and return
			if (!_braking)
			{
				if (_fastCarvingEffects)
				{
					DisableFastCarvingEffects();
				}

				return;
			}

			// braking input is active, if didnt apply effects then do so
			if (!_fastCarvingEffects)
			{
				EnableFastCarvingEffects();
			}
		}

		private void UpdateBraking()
		{
			if(_sendingIt)
			{
				return;
			}

			// stop braking if was before jumping and now in air
			if (_airJumping)
			{
				_currForwardBrakePenalty = 1f;
				Anim.SetBool("Braking", false);
				_braking = false;
				return;
			}

			// check for brake and start
			if (_inputBrake && !_braking)
			{
				Anim.SetBool("Braking", true);
				_currBrakingPower = DeccelerationSpeed * BrakingSpeed;
				_currForwardBrakePenalty = 0f;
				StartCarvingSounds();
				_braking = true;
				return;
			}

			// stop braking
			if (!_inputBrake && _braking)
			{
				StopBraking();
			}
		}

		private void UpdateAccelerationSounds()
		{
			if (!_canPlayIceHitSound)
			{
				_iceHitSoundTime += Time.deltaTime;
				if(_iceHitSoundTime > _iceHitSoundDelay)
				{
					_canPlayIceHitSound = true;
					_iceHitSoundTime = 0.0f;
				}
			}

			if (!_moving || _carving || _airJumping || SkiAmbientSource.clip == null)
				return;

			/// update ski sounds based on speed
			if (_currForwardSpeed >= 0.18f && SkiAmbientSource.clip.name != ClipLoopSkiPath.name)
			{
				SkiAmbientSource.clip = ClipLoopSkiPath;
			}

			if (_currForwardSpeed < 0.18f && _currForwardSpeed >= 0.09f
				&& SkiAmbientSource.clip.name != ClipLoopSkiPathMed.name)
			{
				SkiAmbientSource.clip = ClipLoopSkiPathMed;
			}

			if (_currForwardSpeed < 0.09f
				&& SkiAmbientSource.clip.name != ClipLoopSkiPathSlow.name)
			{
				SkiAmbientSource.clip = ClipLoopSkiPathSlow;
			}

			if (!SkiAmbientSource.isPlaying)
			{
				SkiAmbientSource.Play();
			}
		}

		private void UpdateTrail()
		{
			if (_airJumping || !_moving || !_trails)
				return;

			_trailTimer += DownslopeTime.DeltaTime * _currForwardSpeed;
			if (_trailTimer < _currTrailTime)
				return;

			Vector3 pos = UpdateTrailTypeAndGetOffset();
			OnCreateTrail.Invoke(pos, SpriteID.TRAIL_IDS[_currTerrainType][_currTrailType]);
			_trailTimer = 0f;
		}

		private Vector3 UpdateTrailTypeAndGetOffset()
		{
			Vector3 pos;
			if (!_carving)
			{
				pos = transform.position + StraightTrailOffset;
				_trailTimer = 0f;

				if (_firstAfterCarve)
				{
					_currTrailType = TrailTypes.AfterCarve;
					_firstAfterCarve = false;
					return pos;
				}

				_currTrailType = TrailTypes.Straight;
				return pos;
			}

			pos = transform.position + CarveTrailOffset;
			_currTrailType = TrailTypes.Carve;
			return pos;
		}

		private void UpdateInAir()
		{
			if (!_airJumping)
				return;

			// update air timers
			_airTimer += DownslopeTime.DeltaTime;
			_currAirPercentage = _airTimer / _currAirTime;

			// apply forward boost when launching
			if(_initialAirBoost)
			{
				_initialAirBoostTimer += DownslopeTime.DeltaTime;

				if (_initialAirBoostTimer >= AirInitialForwardTime)
				{
					_currForwardTerrainPenalty = AirForwardBonus;
					_initialAirBoost = false;
					_initialAirBoostTimer = 0f;
				}
			}

			if (_currAirPercentage < AirSnapUpThreshold
				&& _currAirUp < _currMaxAirHeight && _currAirUp < JumpSpriteMax)
			{
				// rise player to maximum y
				float up = JumpSpriteRiseSpeed * DownslopeTime.DeltaTime * _currAirHeightSpeed;
				Anim.transform.localPosition += new Vector3(0, up, 0);
				_currAirUp += up;
			}
			else if (_currAirPercentage >= AirSnapDownThreshold
				&& Anim.transform.localPosition.y > SpriteOffset.y)
			{
				// lower player back down to default sprite level
				float down = JumpSpriteLowerSpeed * DownslopeTime.DeltaTime;
				Anim.transform.localPosition -= new Vector3(0, down, 0);
				_currAirUp -= down;
			}

			// update the air shadow below
			UpdateAirShadow(_currAirUp);

			// officially stop air jumping
			if (_airTimer >= _currAirTime || _stunned || Anim.transform.localPosition.y <= SpriteOffset.y)
			{
				StopAirJump();
			}
		}

		private void UpdateStunned()
		{
			if (!_stunned)
			{
				return;
			}

			// update stun timer and stop all acceleration
			_stunnedTimer += DownslopeTime.DeltaTime;
			_currAcceleration = 0;
			_currMaxForwardSpeed = 0;

			// recover from stun
			if (_stunnedTimer > StunTime)
			{
				RecoverFromStunned();
				_state = PlayerStates.Moving;
			}
		}

		#endregion
	}


}
