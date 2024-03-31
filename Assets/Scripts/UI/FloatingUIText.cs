using System.Collections.Generic;
using UnityEngine;
using Text = TMPro.TextMeshProUGUI;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(RectTransform))]
	public class FloatingUIText : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members
		public Transform RootTextObject;
		public Transform RootImageObject;
		public float Speed;
		public Vector2 Direction;
		public bool UseFixedUpdate = false;
		public bool Loop;
		public float LoopMaxTime;
		public float FixedLoopMax;
		public float FixedLoopInterval;
		public List<string> LoopMessages;
		public bool ActivateOnAwake = true;

		public bool FadeTextOverTime;
		public bool FadeFromTransparent;
		public float FadeSpeed;
		public float FixedFadeStep;
		public bool HasBackgroundImage;
		public bool InitializeFromElement;
		#endregion

		#region Private Members
		private bool _active = false;
		private Vector2 _originalPosition;
		private RectTransform _transform;
		private Text _text;
		private UnityEngine.UI.Image _bgImage;
		private float _currLoopTime;
		private Color _originalTextColor;
		private Color _originalImgColor;
		private Color _currTextColor;
		private Color _currImgColor;
		private int _currLoopMessage;
		#endregion

		#region Public Methods

		public void Activate(bool activate)
		{
			if(activate)
			{
				if (Loop && LoopMessages.Count > 0)
				{
					_currLoopMessage = 0;
					_text.text = LoopMessages[_currLoopMessage];
				}

				if (FadeFromTransparent)
				{
					_currTextColor = _text.color;
					_currTextColor.a = 0;
					_currImgColor.a = _currTextColor.a;
					_text.color = _currTextColor;
					if (HasBackgroundImage)
					{
						_bgImage.color = _currImgColor;
					}
				}
				_active = true;
				return;
			}

			_transform.anchoredPosition = _originalPosition;
			_text.color = _originalTextColor;
			if(HasBackgroundImage)
			{
				_bgImage.color = _originalImgColor;
			}

			_currLoopTime = 0;
			_active = false;
		}

		public void Initialize()
		{
			_originalPosition = _transform.anchoredPosition;

			if (_text)
			{
				_originalTextColor = _text.color;
			}

			if (HasBackgroundImage)
			{
				_bgImage = RootImageObject.GetComponent<UnityEngine.UI.Image>();
				if (_bgImage)
				{
					_originalImgColor = _bgImage.color;
					_currImgColor = _bgImage.color;
				}
			}
		}

		public void Initialize(RectTransform transform, Text txt)
		{
			_text = txt;
			_transform = transform;
			Initialize();
		}

		#endregion

		#region Private Methods

		private void FindComponents()
		{
			_text = RootTextObject.GetComponent<Text>();
			_transform = GetComponent<RectTransform>();
		}

		private void UpdateTextFade(bool fixedUpdate = false)
		{
			if(!FadeTextOverTime)
			{
				return;
			}

			_currTextColor = _text.color;
			if (FadeFromTransparent && _text.color.a < 1)
			{
				if(!fixedUpdate)
				{
					_currTextColor.a += FadeSpeed * Time.deltaTime;
				}
				else
				{
					_currTextColor.a += FadeSpeed * FixedFadeStep;
				}

			}

			if (_text.color.a > 0 && !FadeFromTransparent)
			{
				if (!fixedUpdate)
				{
					_currTextColor.a -= FadeSpeed * Time.deltaTime;
				}
				else
				{
					_currTextColor.a -= FadeSpeed * FixedFadeStep;
				}
			}

			_text.color = _currTextColor;
			_currImgColor.a = _currTextColor.a;

			if (HasBackgroundImage)
			{
				_bgImage.color = _currImgColor;
			}

		}

		private void UpdateLoop(bool fixedUpdate = false)
		{
			if (!Loop)
			{
				return;
			}

			if(!fixedUpdate)
			{
				_currLoopTime += Time.deltaTime;
				if (_currLoopTime < LoopMaxTime)
				{
					return;
				}
			}
			else
			{
				_currLoopTime += FixedLoopInterval;
				if(_currLoopTime < FixedLoopMax)
				{
					return;
				}
			}


			_transform.anchoredPosition = _originalPosition;
			_currLoopTime = 0;

			if(_text != null)
			{
				SetLoopTextColor();
				_text.color = _currTextColor;
				if(HasBackgroundImage)
				{
					_bgImage.color = _currImgColor;
				}
			}


			if(LoopMessages.Count > 0)
			{
				IncrementLoopMessage();
			}
		}

		private void SetLoopTextColor()
		{
			if (FadeFromTransparent)
			{
				_currTextColor.a = 0;
				_currImgColor.a = _currTextColor.a;
				return;
			}
			_currTextColor = _originalTextColor;
			_currImgColor.a = _currTextColor.a;
		}

		private void IncrementLoopMessage()
		{
			_currLoopMessage += 1;
			if (_currLoopMessage >= LoopMessages.Count)
			{
				_currLoopMessage = 0;
			}
			_text.text = LoopMessages[_currLoopMessage];
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{

		}

		private void Start()
		{
			if(InitializeFromElement)
			{
				return;
			}

			FindComponents();
			Initialize();

			if (ActivateOnAwake)
			{
				Activate(true);
			}
		}

		private void Update()
		{
			if(UseFixedUpdate)
			{
				return;
			}

			if (!_active)
				return;

			_transform.anchoredPosition += Direction * Speed * Time.deltaTime;
			UpdateTextFade();
			UpdateLoop();
		}

		private void FixedUpdate()
		{
			if(!UseFixedUpdate)
			{
				return;
			}

			if (!_active)
				return;

			_transform.anchoredPosition += Direction * Speed;
			UpdateTextFade(true);
			UpdateLoop(true);
		}

		#endregion
	}
}

