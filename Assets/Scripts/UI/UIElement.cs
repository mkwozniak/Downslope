using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Text = TMPro.TextMeshProUGUI;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(RectTransform))]
	public class UIElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		#region Public Members

		[HideInInspector] public bool HasButton;
		[HideInInspector] public Button Btn;

		[HideInInspector] public bool HasImage;
		[HideInInspector] public Image SourceImage;

		[HideInInspector] public bool HasHoverEvents;
		[HideInInspector] public UnityEvent OnHoverEvents;

		[HideInInspector] public bool HasHoverExitEvents;
		[HideInInspector] public UnityEvent OnHoverExitEvents;

		[HideInInspector] public bool HasHoverImageChange;
		[HideInInspector] public Sprite HoverSprite;

		[HideInInspector] public bool HasText;
		[HideInInspector] public bool HasHoverTextChange;
		[HideInInspector] public Text Txt;
		[HideInInspector] public string HoverMessage;
		[HideInInspector] public bool HasTextMsgLoop;

		[HideInInspector] public bool HasHoverTextColorChange;
		[HideInInspector] public Color HoverTextColor;

		[HideInInspector] public bool HasHoverScaleChange;
		[HideInInspector] public Vector3 DefaultScale;
		[HideInInspector] public Vector3 HoverScale;

		[HideInInspector] public bool HasFloatingEffects;
		[HideInInspector] public bool AutoActivateFloatingEffects;
		[HideInInspector] public float FloatingSpeed;
		[HideInInspector] public Vector2 FloatingDirection;
		[HideInInspector] public bool FloatLoop;
		[HideInInspector] public float FloatLoopMaxTime;
		[HideInInspector] public List<string> FloatLoopMessages;
		[HideInInspector] public bool FloatFadeTextOverTime;
		[HideInInspector] public bool FloatFadeFromTransparent;
		[HideInInspector] public float FloatFadeSpeed;

		#endregion

		#region Private Members

		private RectTransform _transform;

		// cached original values
		private Sprite _originalSprite;
		private Vector2 _originalPosition;
		private Color _originalImgColor;
		private string _originalButtonMessage;
		private Color _originalTextColor;
		private Color _originalHoverTextColor;
		private Color _originalButtonTextColor;

		// curr core values
		private bool _hovering;
		private Color _currImgColor;

		// floating effect values
		private bool _floatingActive = false;
		private float _currFloatLoopTime;
		private int _currFloatLoopMessage;
		private Color _currFloatTextColor;

		#endregion

		#region Public Methods

		public void SetParent(RectTransform t)
		{
			_transform.parent = t;
		}

		public void HoverEnter()
		{
			_hovering = true;

			if (HasHoverScaleChange)
			{
				_transform.localScale = HoverScale;
			}

			if (HasHoverImageChange)
			{
				SourceImage.sprite = HoverSprite;
			}

			if (HasText)
			{
				if (HasHoverTextChange)
				{
					Txt.text = HoverMessage;
				}

				if (HasHoverTextColorChange)
				{
					Txt.color = HoverTextColor;
				}
			}

			if (HasHoverEvents && OnHoverEvents != null)
			{
				OnHoverEvents.Invoke();
			}
		}

		public void HoverExit()
		{
			_hovering = false;

			if (HasHoverScaleChange)
			{
				_transform.localScale = DefaultScale;
			}

			if (HasHoverImageChange)
			{
				SourceImage.sprite = _originalSprite;
			}

			if (HasText)
			{
				if (HasHoverTextChange)
				{
					Txt.text = _originalButtonMessage;
				}

				if (HasHoverTextColorChange)
				{
					Txt.color = _originalButtonTextColor;
				}
			}

			if(HasHoverExitEvents && OnHoverExitEvents != null)
			{
				OnHoverExitEvents.Invoke();
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			HoverEnter();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			HoverExit();
		}

		public void ActivateFloatingEffects(bool activate)
		{
			if (activate)
			{
				if (FloatLoop && FloatLoopMessages.Count > 0 && HasText)
				{
					_currFloatLoopMessage = 0;
					Txt.text = FloatLoopMessages[_currFloatLoopMessage];
				}

				if (FloatFadeFromTransparent && HasText)
				{
					_currFloatTextColor = Txt.color;
					_currFloatTextColor.a = 0;
					_currImgColor.a = _currFloatTextColor.a;
					Txt.color = _currFloatTextColor;
					if (HasImage)
					{
						SourceImage.color = _currImgColor;
					}
				}

				_floatingActive = true;
				return;
			}

			_transform.anchoredPosition = _originalPosition;
			if(HasText)
			{
				Txt.color = _originalTextColor;
			}

			if (HasImage)
			{
				SourceImage.color = _originalImgColor;
			}

			_currFloatLoopTime = 0;
			_floatingActive = false;
		}

		#endregion

		#region Private Methods

		private void UpdateFloatingTextFade(bool fixedUpdate = false)
		{
			if (!FloatFadeTextOverTime || !HasText)
			{
				return;
			}

			_currFloatTextColor = Txt.color;
			if (FloatFadeFromTransparent && Txt.color.a < 1)
			{
				_currFloatTextColor.a += FloatFadeSpeed * Time.deltaTime;
			}

			if (Txt.color.a > 0 && !FloatFadeFromTransparent)
			{
				_currFloatTextColor.a -= FloatFadeSpeed * Time.deltaTime;
			}

			Txt.color = _currFloatTextColor;
			_currImgColor.a = _currFloatTextColor.a;

			if (HasImage)
			{
				SourceImage.color = _currImgColor;
			}
		}

		private void UpdateFloatingLoop(bool fixedUpdate = false)
		{
			if (!FloatLoop)
			{
				return;
			}

			_currFloatLoopTime += Time.deltaTime;
			if (_currFloatLoopTime < FloatLoopMaxTime)
			{
				return;
			}

			_transform.anchoredPosition = _originalPosition;
			_currFloatLoopTime = 0;

			if (HasText)
			{
				SetLoopTextColor();
				Txt.color = _currFloatTextColor;
				if (HasImage)
				{
					SourceImage.color = _currImgColor;
				}
			}

			if (FloatLoopMessages.Count > 0)
			{
				IncrementLoopMessage();
			}
		}

		private void SetLoopTextColor()
		{
			if (FloatFadeFromTransparent)
			{
				_currFloatTextColor.a = 0;
				_currImgColor.a = _currFloatTextColor.a;
				return;
			}

			if(HasHoverTextColorChange)
			{
				_currFloatTextColor = !_hovering ? _originalTextColor : _originalHoverTextColor;
			}
			else
			{
				_currFloatTextColor = _originalTextColor;
			}

			_currImgColor.a = _currFloatTextColor.a;
		}

		private void IncrementLoopMessage()
		{
			_currFloatLoopMessage += 1;
			if (_currFloatLoopMessage >= FloatLoopMessages.Count)
			{
				_currFloatLoopMessage = 0;
			}

			if(HasText)
			{
				Txt.text = FloatLoopMessages[_currFloatLoopMessage];
			}
		}

		#endregion

		#region Unity Methods

		private void Start()
		{
			_transform = GetComponent<RectTransform>();
			if(!_transform)
			{
				Debug.LogError("Failed to initialize UIElement. Could not find RectTransform component.");
				return;
			}

			_originalPosition = _transform.anchoredPosition;

			if (SourceImage)
			{
				_originalSprite = SourceImage.sprite;
				_originalImgColor = SourceImage.color;
				_currImgColor = SourceImage.color;
			}
			else
			{
				HasHoverImageChange = false;
				HasImage = false;
			}

			if (Txt)
			{
				_originalButtonMessage = Txt.text;
				_originalButtonTextColor = Txt.color;
				_originalTextColor = Txt.color;
				_originalHoverTextColor = HoverTextColor;
			}
			else
			{
				HasText = false;
			}

			if(HasFloatingEffects && AutoActivateFloatingEffects)
			{
				ActivateFloatingEffects(true);
			}

			HoverExit();
		}

		private void Awake()
		{

		}

		private void Update()
		{
			if (_floatingActive)
			{
				_transform.anchoredPosition += FloatingDirection * FloatingSpeed * Time.deltaTime;
				UpdateFloatingTextFade();
				UpdateFloatingLoop();
			}
		}

		#endregion
	}
}

