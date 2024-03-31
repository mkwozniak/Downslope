using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	public sealed partial class PlayerControl : MonoBehaviour
	{
		#region Events

		// events
		public event Action<float> OnUpdatedSpeed;
		public event Action<Vector3, string> OnCreateTrail;
		public event Action OnSpeedUp;
		public event Action OnStartedMovement;
		public event Action OnStoppedMovement;
		public event Action<Vector3> OnHitIce;
		public event Action<Vector3> OnHitPowder;
		public event Action<Vector3> OnLandAirJump;
		public event Action<float> OnHealthUpdated;
		public event Action OnGameOver;


		// actions
		public Func<string, bool> CreateSFX;
		public Action<string, Vector3> CreatePFX;
		public Action<float, float> CheckAIReachedGenThreshold;
		public Func<float> GetKMH;

		#endregion

		#region Public Members

		[Header("Core")]
		public bool IsAI = false;
		public bool Active = true;
		public PlayerControl AIOpponent;
		public Animator Anim;
		public Animator SnowFXAnimator;
		public Animator BlockedAnimator;
		public BoxCollider2D Collider;
		public SpriteRenderer Rend;
		public SpriteRenderer ShadowRenderer;
		public SpriteRenderer StunnedFXRenderer;
		public int MaxHealth = 100;
		public int DamageWhenStunned = 5;
		public float DamageSpeedMultiplier = 1.125f;

		public float AIPlayerDistanceGeneration;

		[Header("Forward Movement")]
		public float DefaultForwardSpeed = 0.18f;
		public float MinimumForwardAcceleration = 0.1f;
		public float AccelerationSpeed = 0.0075f;
		public float DeccelerationSpeed = 0.005f;
		public float PowderForwardPenalty = 0.50f;
		public float PowderFastForwardPenalty = 0.80f;
		public float PowderPenaltyKMHThreshold = 20f;
		public Vector3 FrontDetectionOffset;
		public float FrontDetectionDistance = 1f;
		public Vector3 BodyDetectionOffset;
		public float BodyDetectionDistance;
		public float BodyDetectionWidth;
		public Vector3 RightDetectionOffset;
		public float RightDetectionDistance = 1f;
		public Vector3 LeftDetectionOffset;
		public float LeftDetectionDistance = 1f;
		public float SendItForwardBonus = 1.5f;

		[Header("Braking")]
		public float BrakingSpeed = 0.005f;
		public float ManualBreakBonus = 1.5f;

		[Header("Carving & Horizontal")]
		public float PlayerHorizontalSpeed = 0.1f;
		public float FastCarveHorizontalBonus = 1.1f;
		public float FastCarveForwardPenalty = 0.85f;
		public float CarveAirDragPenalty = 0.2f;
		public float CarveAirDragIntensity = 1.0f;
		public float CarveAirRecovery = 1.0f;
		public float CarveForwardHorizontalModifier = 1.0f;
		public float CarveMinimumSpeed = 0.1f;

		[Header("Air Jumping")]
		public float AirInitialForwardBonus = 1.5f;
		public float AirInitialForwardTime = 0.1f;
		public float AirForwardBonus = 1.25f;
		public float SpeedInAirMultiplier = 1f;
		public float JumpSpriteMax = 0.5f;
		public float JumpSpriteRiseSpeed = 0.5f;
		public float JumpSpriteLowerSpeed = 0.5f;
		public float AirSnapUpThreshold = 0.75f;
		public float AirSnapDownThreshold = 0.75f;
		public float DefaultWindVolume = 0.2f;
		public float JumpWindVolume = 0.5f;
		public float SoftJumpSoundKMHThreshold = 10;
		public List<HeightShadowThreshold> AirShadowThresholds;

		[Header("Stun")]
		public float StunThresholdKMH = 10f;
		public float StunTime = 0.25f;

		[Header("Trail")]
		public float StraightTrailTime = 1f;
		public float CarveTrailTime = 1f;

		public Vector3 StraightTrailOffset = new Vector3(0, 0.08f, 0);
		public Vector3 CarveTrailOffset = new Vector3(0, 0.08f, 0);

		[Header("Misc")]
		public LayerMask IceLayerMask;
		public LayerMask ImpassableLayerMask;
		public float AnimatorSpeedModifier;
		public float SnowFXAnimatorSpeedModifier;
		public float DefaultMaxHealth = 100f;
		public Vector3 SpriteOffset;

		[Header("Audio")]
		public AudioClip ClipLoopSkiPath;
		public AudioClip ClipLoopSkiPathMed;
		public AudioClip ClipLoopSkiPathSlow;
		public AudioClip ClipLoopCarvingPath;
		public AudioClip ClipLoopCarvingPowder;
		public AudioClip ClipLoopJumpWind;
		public ParticleSystem SkiPowderFX;
		public AudioSource SkiAmbientSource;
		public AudioSource SkiCarvingSource;
		public AudioSource SkiWindSource;

		public float CurrentForwardSpeed
		{
			get
			{
				return _currForwardSpeed;
			}
		}

		public PlayerStates State
		{
			get
			{
				return _state;
			}
		}

		#endregion

		#region Private Members

		[Header("Inputs")]
		[ReadOnly][SerializeField] private bool _inputStartMoving = false;
		[ReadOnly][SerializeField] private bool _inputBrake = false;
		[ReadOnly][SerializeField] private bool[] _inputHorizontal = new bool[2];

		[Header("States")]
		[ReadOnly][SerializeField] private PlayerStates _state;
		[ReadOnly][SerializeField] private TerrainTypes _currTerrainType;
		[ReadOnly][SerializeField] private TrailTypes _currTrailType;
		[ReadOnly][SerializeField] private bool _hidden = false;
		[ReadOnly][SerializeField] private bool _carving = false;
		[ReadOnly][SerializeField] private bool _moving = false;
		[ReadOnly][SerializeField] private bool _accelerating = false;
		[ReadOnly][SerializeField] private bool _decelerating = false;
		[ReadOnly][SerializeField] private bool _sendingIt = false;
		[ReadOnly][SerializeField] private bool _fastCarvingEffects = false;
		[ReadOnly][SerializeField] private bool _updateForwardSpeed = false;
		[ReadOnly][SerializeField] private bool _braking = false;
		[ReadOnly][SerializeField] private bool _airJumping = false;
		[ReadOnly][SerializeField] private bool _stunned = false;
		[ReadOnly][SerializeField] private bool _softJump = false;
		[ReadOnly][SerializeField] private bool _collidingFront = false;
		[ReadOnly][SerializeField] private bool _collidingRight = false;
		[ReadOnly][SerializeField] private bool _collidingLeft = false;
		[ReadOnly][SerializeField] private bool _collidingBody = false;
		[ReadOnly][SerializeField] private bool _trails = true;
		[ReadOnly][SerializeField] private bool _firstAfterCarve = false;
		[ReadOnly][SerializeField] private bool _initialAirBoost = false;

		[Header("Current Values")]
		[ReadOnly][SerializeField] private Vector3 _currHorizontalVelocity = Vector3.zero;
		[ReadOnly][SerializeField] private int _currHorizontalDir = 0;
		[ReadOnly][SerializeField] private float _currForwardSpeed = 0f;
		[ReadOnly][SerializeField] private float _currAcceleration = 0f;
		[ReadOnly][SerializeField] private float _currForwardMovePenalty = 1f;
		[ReadOnly][SerializeField] private float _currForwardCarvePenalty = 1f;
		[ReadOnly][SerializeField] private float _currForwardTerrainPenalty = 1f;
		[ReadOnly][SerializeField] private float _currForwardBrakePenalty = 1f;
		[ReadOnly][SerializeField] private float _currForwardSendItPenalty = 1f;
		[ReadOnly][SerializeField] private float _currHorizontalMoveBonus = 1f;
		[ReadOnly][SerializeField] private float _currHorizontalForwardPenalty = 1f;
		[ReadOnly][SerializeField] private float _currMaxForwardSpeed = 0f;
		[ReadOnly][SerializeField] private float _currBrakingPower = 0f;
		[ReadOnly][SerializeField] private float _currTrailTime = 0f;
		[ReadOnly][SerializeField] private float _currCarveAirDrag = 0f;
		[ReadOnly][SerializeField] private float _currMaxAirHeight = 0f;
		[ReadOnly][SerializeField] private float _currAirHeightSpeed = 0f;
		[ReadOnly][SerializeField] private float _currAirTime = 0f;
		[ReadOnly][SerializeField] private float _currAirPercentage = 0f;
		[ReadOnly][SerializeField] private float _currAirUp = 0f;
		[ReadOnly][SerializeField] private float _currGlobalAnimationSpeed = 0f;
		[ReadOnly][SerializeField] private float _currHealth = 0f;

		[Header("Timers")]
		[ReadOnly][SerializeField] private float _trailTimer = 0f;
		[ReadOnly][SerializeField] private float _airTimer = 0f;
		[ReadOnly][SerializeField] private float _stunnedTimer = 0f;
		[ReadOnly][SerializeField] private float _initialAirBoostTimer = 0f;

		private Dictionary<PlayerStates, Action> _stateCallbacks = new Dictionary<PlayerStates, Action>();

		private RaycastHit2D _iceRayHit;
		private RaycastHit2D _frontHit;
		private RaycastHit2D _rightHit;
		private RaycastHit2D _leftHit;
		private RaycastHit2D[] _bodyHits;

		private bool _canPlayIceHitSound = false;
		private float _iceHitSoundTime = 0.0f;
		private float _iceHitSoundDelay = 1.5f;

		#endregion

		#region Public Methods

		public void GamePaused(bool paused)
		{
			if(paused)
			{
				_currGlobalAnimationSpeed = 0f;
				SkiCarvingSource.Pause();
				SkiAmbientSource.Pause();
				Debug.Log("PlayerControl: GamePaused true.");
				UpdateAnimators();
				return;
			}

			_currGlobalAnimationSpeed = 1f;
			SkiCarvingSource.Play();
			SkiAmbientSource.Play();
			UpdateAnimators();
		}

		public void MenuModeStart()
		{
			Hide();
			SetMenuMoveMode();
			RecoverFromStunned();
			SkiAmbientSource.clip = null;
			SkiCarvingSource.clip = null;
			SkiCarvingSource.Stop();
			SkiAmbientSource.Stop();

			if (!IsAI)
			{
				AIOpponent.MenuModeStart();
			}
		}

		public void ArcadeModeStart()
		{
			StopMovement();
			RecoverFromStunned();

			if(!Active)
			{
				return;
			}

			_currHealth = MaxHealth;
			OnHealthUpdated(1);
			Show();

			if (!IsAI)
			{
				AIOpponent.ArcadeModeStart();
			}
		}

		public void TutorialStart()
		{
			StopMovement();
			Show();
		}

		public void StartInitialMovement()
		{
			if (_moving || _hidden)
				return;

			if(!IsAI)
			{
				AIOpponent.StartInitialMovement();
			}

			_moving = true;
			_currTerrainType = TerrainTypes.Powder;
			_updateForwardSpeed = true;

			_currTrailTime = StraightTrailTime;
			_currAcceleration = MinimumForwardAcceleration;
			_currMaxForwardSpeed = DefaultForwardSpeed;
			_currGlobalAnimationSpeed = 1f;

			_state = PlayerStates.Moving;

			EnableTrails(true);
			SkiAmbientSource.clip = ClipLoopSkiPath;
			SkiAmbientSource.volume = _currAcceleration + .5f;
			SkiAmbientSource.Play();

			OnStartedMovement.Invoke();
		}

		public void StopAllMovement()
		{
			StopMovement();
		}

		public void EnableTrails(bool val)
		{
			_trails = val;
		}

		public float GetXPosition()
		{
			return transform.position.x;
		}

		#endregion

		#region Private Methods

		private void InitializeEvents()
		{
			// initialize events with lambda so they are not null
			OnUpdatedSpeed = (f) => { };
			OnStartedMovement = () => { };
			OnStoppedMovement = () => { };
			OnHitIce = (pos) => { };
			OnHitPowder = (pos) => { };
			OnLandAirJump = (pos) => { };
			OnCreateTrail = (pos, id) => { };
			OnHealthUpdated = (f) => { };
			CreateSFX = (id) => { return false; };
			CreatePFX = (id, pos) => { };
			GetKMH = () => { return 0; };
			CheckAIReachedGenThreshold = (y, thres) => { };

			_stateCallbacks[PlayerStates.Hidden] = () => { };
			_stateCallbacks[PlayerStates.Stopped] = () => { };
			_stateCallbacks[PlayerStates.Moving] = () => { };
			_stateCallbacks[PlayerStates.Stunned] = () => { };
			_stateCallbacks[PlayerStates.Airborne] = () => { };

			_stateCallbacks[PlayerStates.Hidden] += UpdateAcceleration;

			_stateCallbacks[PlayerStates.Moving] += UpdateAcceleration;
			_stateCallbacks[PlayerStates.Moving] += UpdateAccelerationSounds;
			_stateCallbacks[PlayerStates.Moving] += UpdateCarving;
			_stateCallbacks[PlayerStates.Moving] += UpdateFastCarving;
			_stateCallbacks[PlayerStates.Moving] += UpdateTrail;
			_stateCallbacks[PlayerStates.Moving] += UpdateBraking;
			_stateCallbacks[PlayerStates.Moving] += CheckHitTerrainType;

			_stateCallbacks[PlayerStates.Stunned] += UpdateStunned;

			_stateCallbacks[PlayerStates.Airborne] += UpdateInAir;
			_stateCallbacks[PlayerStates.Airborne] += UpdateAcceleration;
		}

		private void EnableCollision(bool enable)
		{
			Collider.enabled = enable;
		}

		private void SetMenuMoveMode()
		{
			_moving = true;
			_currMaxForwardSpeed = Game.PLAYER_DEFAULT_MENU_SPEED;
			_currAcceleration = 1f;
			_currBrakingPower = DeccelerationSpeed;
			_state = PlayerStates.Hidden;
			_updateForwardSpeed = true;
			OnStartedMovement.Invoke();
		}

		private void Hide()
		{
			StopMovement();
			EnableCollision(false);
			Anim.enabled = false;
			Rend.enabled = false;
			SkiAmbientSource.Stop();
			SkiWindSource.Stop();
			_trails = false;
			_hidden = true;
			_state = PlayerStates.Hidden;
			Util.Log("Hide", this.name);
		}

		private void Show()
		{
			EnableCollision(true);
			Anim.enabled = true;
			Rend.enabled = true;
			_trails = true;
			_hidden = false;
			_state = PlayerStates.Stopped;
			Util.Log("Show", this.name);
		}

		private void StopMovement()
		{
			_moving = false;
			_currMaxForwardSpeed = 0f;
			_currAcceleration = 0f;
			_updateForwardSpeed = true;
			_state = PlayerStates.Stopped;
			OnStoppedMovement();
			Util.Log("Stopping Movement", this.name);
		}

		private void StopBraking()
		{
			_currBrakingPower = DeccelerationSpeed;
			_currForwardBrakePenalty = 1f;
			StopCarvingSounds();
			Anim.SetBool("Braking", false);
			_braking = false;
		}

		private void RecoverFromStunned()
		{
			_stunned = false;
			_stunnedTimer = 0;
			StunnedFXRenderer.enabled = false;
			_currMaxForwardSpeed = DefaultForwardSpeed;
		}

		private void ModifyForwardSpeed(float percentageModifier)
		{
			_currAcceleration = _currAcceleration * percentageModifier;
			_updateForwardSpeed = true;
		}

		private void CheckHitTerrainType()
		{
			if (_airJumping)
				return;

			_iceRayHit = Physics2D.Raycast(transform.position, Vector3.down, 1, IceLayerMask);
			_frontHit = Physics2D.Raycast(transform.position + FrontDetectionOffset, Vector3.down, FrontDetectionDistance, ImpassableLayerMask);
			_rightHit = Physics2D.Raycast(transform.position + RightDetectionOffset, Vector3.right, RightDetectionDistance, ImpassableLayerMask);
			_leftHit = Physics2D.Raycast(transform.position + LeftDetectionOffset, Vector3.left, LeftDetectionDistance, ImpassableLayerMask);
			_bodyHits = new RaycastHit2D[3];
			Vector3 pos = transform.position + BodyDetectionOffset;
			pos.x -= (BodyDetectionWidth);

			_collidingBody = false;
			for (int i = 0; i < 3; i++)
			{
				_bodyHits[i] = Physics2D.Raycast(pos, Vector3.down, BodyDetectionDistance, ImpassableLayerMask);
				pos.x += BodyDetectionWidth;
				if (_bodyHits[i].transform != null)
				{
					_collidingBody = true;
				}
			}
	
			_collidingFront = _frontHit.transform == null ? false : true;
			_collidingRight = _rightHit.transform == null ? false : true;
			_collidingLeft = _leftHit.transform == null ? false : true;

			// if no ice hit
			if (!_iceRayHit.transform)
			{
				// disable on ice and hit powder
				if (_currTerrainType == TerrainTypes.Ice)
				{
					_currTerrainType = TerrainTypes.Powder;
					CreatePFX.Invoke("PowderFX", transform.position);
					HitPowder(_iceRayHit.point);
				}

				// check powder penalty and return
				if (GetKMH.Invoke() <= PowderPenaltyKMHThreshold)
				{
					_currForwardTerrainPenalty = PowderForwardPenalty;
					return;
				}

				_currForwardTerrainPenalty = PowderFastForwardPenalty;
				return;
			}

			// enable on ice
			if (_currTerrainType != TerrainTypes.Ice)
			{
				_currTerrainType = TerrainTypes.Ice;
				HitIce(_iceRayHit.point);
				StopPowderSounds();
			}

			// disable penalty
			_currForwardTerrainPenalty = 1;
		}

		private bool CheckObstacleDirectHit(Vector3 playerPos, Vector3 obstaclePos, float hitDist)
		{
			playerPos.y = 0;
			obstaclePos.y = 0;
			float dist = Vector2.Distance(playerPos, obstaclePos);
			//Debug.Log($"Player Obstacle Direct Hit Distance: {dist}. Distance Needed: {hitDist}.");

			if (Vector2.Distance(playerPos, obstaclePos) < hitDist)
			{
				return true;
			}

			return false;
		}

		private bool CheckObstacleShouldCollide(Obstacle obstacle)
		{
			// return true if obstacle is air collidable in any way and is air jumping
			if ((obstacle.OnlyAirCollidable || obstacle.AirCollidable) && _airJumping)
			{
				StopAirJump();
				return true;
			}

			// return false if is air jumping and obstacle is not air collidable in any way
			if (_airJumping && !obstacle.AirCollidable && !obstacle.OnlyAirCollidable)
			{
				return false;
			}

			// return true, should collide
			return true;
		}

		private void EnableSendItEffects()
		{
			if(_carving || _stunned || _airJumping || _braking)
			{
				return;
			}

			_currForwardSendItPenalty = SendItForwardBonus;
			_sendingIt = true;
			Anim.SetBool("SendIt", true);
		}

		private void DisableSendItEffects()
		{
			_currForwardSendItPenalty = 1;
			_sendingIt = false;
			Anim.SetBool("SendIt", false);
		}

		private void EnableFastCarvingEffects()
		{
			_currForwardCarvePenalty = FastCarveForwardPenalty;
			_currHorizontalMoveBonus = FastCarveHorizontalBonus;
			_fastCarvingEffects = true;
		}

		private void DisableFastCarvingEffects()
		{
			_currForwardCarvePenalty = 1;
			_currHorizontalMoveBonus = 1;
			_fastCarvingEffects = false;
		}

		private void StartCarvingSounds(bool pauseAmbientAfter = true)
		{
			SkiAmbientSource.Pause();
			SkiCarvingSource.clip = ClipLoopCarvingPath;

			if (_currTerrainType != TerrainTypes.Ice)
			{
				SkiCarvingSource.clip = ClipLoopCarvingPowder;
			}

			SkiCarvingSource.Play();
			if (pauseAmbientAfter)
				SkiAmbientSource.Pause();
		}

		private void StopCarvingSounds(bool playAmbientAfter = true)
		{
			SkiCarvingSource.Pause();
			if (playAmbientAfter)
				SkiAmbientSource.Play();
		}

		private void HitIce(Vector3 pos)
		{
			StartIceSounds();
			OnHitIce(pos);
		}

		private void HitPowder(Vector3 pos)
		{
			SkiPowderFX.Play();
			OnHitPowder(pos);
		}

		private void StopPowderSounds()
		{
			SkiPowderFX.Stop();
		}

		private void StartIceSounds()
		{
			if(_canPlayIceHitSound)
			{
				_canPlayIceHitSound = false;
				CreateSFX("PlayerHitIceTerrain0");
			}
		}

		private void StartAirJump(float rampPower, float verticalPower, float verticalMax)
		{
			Rend.sortingOrder = 4;
			ShadowRenderer.gameObject.SetActive(true);
			StopBraking();

			string jumpSFXId = "PlayerHitJump0";
			if (GetKMH() < SoftJumpSoundKMHThreshold || _softJump)
			{
				jumpSFXId = "PlayerHitJumpSoft0";
			}

			SkiAmbientSource.Pause();
			CreateSFX(jumpSFXId);

			SkiWindSource.volume = JumpWindVolume;
			StopCarvingSounds(false);

			_currAirTime = _currForwardSpeed * SpeedInAirMultiplier * rampPower;
			_currAirHeightSpeed = _currAirTime * verticalPower;
			_currMaxAirHeight = verticalMax;
			_currForwardTerrainPenalty = AirInitialForwardBonus;

			_airTimer = 0f;
			_airJumping = true;
			_currAirUp = 0f;
			_initialAirBoost = true;
			_initialAirBoostTimer = 0f;
			_state = PlayerStates.Airborne;
		}

		private void StopAirJump()
		{
			Rend.sortingOrder = 3;

			string landSFXId = "PlayerLandJump0";
			if (GetKMH.Invoke() < SoftJumpSoundKMHThreshold || _softJump)
			{
				landSFXId = "PlayerLandJumpSoft0";
			}
			else
			{
				if (_currTerrainType == TerrainTypes.Powder)
					landSFXId = "PlayerHitPowder0";
			}

			CreateSFX.Invoke(landSFXId);

			SkiWindSource.volume = DefaultWindVolume;

			Anim.transform.localPosition = SpriteOffset;
			ShadowRenderer.gameObject.SetActive(false);
			CreatePFX("PowderFX", transform.position);
			_airJumping = false;
			_initialAirBoost = false;
			_state = PlayerStates.Moving;
		}

		private void StunFromHit()
		{
			if (!_stunned)
			{
				_currHorizontalVelocity = Vector3.zero;
				_accelerating = false;
				_currMaxForwardSpeed = 0f;
				_currAcceleration = 0f;
				_decelerating = false;

				StunnedFXRenderer.enabled = true;

				StopBraking();

				_carving = false;
				_fastCarvingEffects = false;
				_stunned = true;
				_state = PlayerStates.Stunned;
				_currHealth -= DamageWhenStunned + (_currForwardSpeed * DamageSpeedMultiplier);
				OnHealthUpdated(_currHealth / MaxHealth);
				Debug.Log($"PlayerControl: Stunning Player for {StunTime} seconds.");
				if(_currHealth <= 0)
				{
					OnGameOver();
				}
			}
		}

		private void UpdateAirShadow(float threshold)
		{
			// loop through shadows and check their thresholds
			for(int i = 0; i < AirShadowThresholds.Count; i++)
			{
				if(i + 1 == AirShadowThresholds.Count - 1)
				{
					if (threshold >= AirShadowThresholds[i].Threshold)
					{
						ShadowRenderer.sprite = AirShadowThresholds[i].ShadowSprite;
						return;
					}

					continue;
				}

				if (threshold >= AirShadowThresholds[i].Threshold && threshold < AirShadowThresholds[i + 1].Threshold)
				{
					ShadowRenderer.sprite = AirShadowThresholds[i].ShadowSprite;
					return;
				}
			}
		}

		private void CollideWorldSprite(WorldSprite sprite)
		{
			Obstacle obstacle;

			// get the obstacle data
			if (!sprite.TryGetObstacleData(out obstacle))
			{
				return;
			}

			// check if collidable
			if(!CheckObstacleShouldCollide(obstacle))
			{
				return;
			}

			// check if obstacle is a ramp and jump if it is
			bool playerAboveRamp = transform.position.y >= sprite.transform.position.y;
			if (obstacle.IsRamp && !_airJumping)
			{
				_softJump = obstacle.IsSoftJump;
				StartAirJump(obstacle.ForwardRampPower, obstacle.VerticalRampPower, obstacle.VerticalRampMax);
				return;
			}

			bool playerIsAbove = transform.position.y >= sprite.transform.position.y;

			// check if the obstacle can and is a direct hit
			if (obstacle.IsCenterCollidable)
			{
				bool directHit = CheckObstacleDirectHit(transform.position, sprite.transform.position, obstacle.CenterCollisionDistance);
				if (directHit && playerIsAbove)
				{
					// modify the speed, play sound and stun the player if need be
					ModifyForwardSpeed(obstacle.CenterCollisionSpeedPenalty);

					// react sprite to collision
					sprite.CollidePlayerCenter();

					if (obstacle.StunOnCenterCollision && GetKMH.Invoke() >= StunThresholdKMH)
					{
						StunFromHit();
					}

					return;
				}
			}

			// check for regular collision penalty and play sound
			if (obstacle.HasCollisionSpeedPenalty)
			{
				Debug.Log($"{obstacle.Name} Colliding With Player");
				ModifyForwardSpeed(obstacle.CollisionSpeedPenalty);
			}

			// react sprite to collision
			sprite.CollidePlayer();
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			Anim.transform.localPosition = SpriteOffset;
		}

		private void Start()
		{
			InitializeEvents();
			if (!IsAI)
			{
				InitializeInputEvents();
			}
			SkiWindSource.volume = DefaultWindVolume;
		}

		private void Update()
		{
			if (Game.IS_PAUSED)
				return;

			/// main update loop

			// check input
			UpdateInput();

			// current state callback
			_stateCallbacks[_state].Invoke();

			// check speed update
			if(_updateForwardSpeed)
			{
				UpdateSpeed();
			}

			// update animator
			UpdateAnimators();

			// debug
			DrawDebugRays();
		}

		private void FixedUpdate()
		{
			UpdateMovement(DownslopeTime.TimeScale);
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			WorldSprite hit;
			bool isWorldSprite = collision.TryGetComponent(out hit);

			// check if its even a world sprite
			if (!isWorldSprite)
			{
				return;
			}

			// obstacle is a valid collision
			CollideWorldSprite(hit);
		}

		private void DrawDebugRays()
		{
			if (_collidingFront)
			{
				Debug.DrawLine(transform.position, _frontHit.point, Color.red);
			}
			else
			{
				Debug.DrawRay(transform.position + FrontDetectionOffset, Vector3.down * FrontDetectionDistance, Color.green);
			}

			if (_collidingRight)
			{
				Debug.DrawLine(transform.position + RightDetectionOffset, _rightHit.point, Color.red);
			}
			else
			{
				Debug.DrawRay(transform.position + RightDetectionOffset, Vector3.right * RightDetectionDistance, Color.green);
			}

			if (_collidingLeft)
			{
				Debug.DrawLine(transform.position + LeftDetectionOffset, _leftHit.point, Color.red);
			}
			else
			{
				Debug.DrawRay(transform.position + LeftDetectionOffset, Vector3.left * LeftDetectionDistance, Color.green);
			}

			if (_bodyHits != null)
			{
				Vector3 pos = transform.position + BodyDetectionOffset;
				pos.x -= (BodyDetectionWidth);
				for (int i = 0; i < 3; i++)
				{
					Vector3 end = pos;
					end.y -= BodyDetectionDistance;
					Debug.DrawLine(pos, end, Color.red);
					pos.x += (BodyDetectionWidth);
				}
			}
		}

		#endregion
	}
}


