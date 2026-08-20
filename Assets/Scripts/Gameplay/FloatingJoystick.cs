using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 可移动动态摇杆：手指在屏幕左侧任意位置按下，摇杆（底座+手柄）出现在手指位置并开始控制。
	/// 挂载在一个透明的全屏接收层（Image）上，底座和手柄是该接收层的子物体。
	/// </summary>
	[AddComponentMenu("Input/Dynamic Floating Joystick")]
	public class FloatingJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		[Tooltip("摇杆可响应的屏幕宽度比例（0~1，0.5 表示只响应左半屏）")]
		[Range(0.1f, 1f)]
		public float activeArea = 0.5f;

		[Tooltip("手柄可移动的最大半径（像素）")]
		public float movementRange = 50f;

		[Tooltip("松手后摇杆回到初始位置")]
		public bool returnToOrigin = true;

		[Tooltip("摇杆底座（背景图），整组随手指出现")]
		public RectTransform background;

		[Tooltip("摇杆手柄（跟随手指移动的图）")]
		public RectTransform handle;

		[InputControl(layout = "Vector2")]
		[SerializeField]
		private string m_ControlPath = "<Gamepad>/leftStick";

		protected override string controlPathInternal
		{
			get => m_ControlPath;
			set => m_ControlPath = value;
		}

		Canvas m_Canvas;
		Camera m_UiCam;
		Vector2 m_BgOriginPos;      // 底座初始位置
		Vector2 m_PointerDownPos;   // 按下时指针在 Canvas 局部坐标
		bool m_Active;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_Canvas = GetComponentInParent<Canvas>();
			m_UiCam = m_Canvas != null && m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay
				? m_Canvas.worldCamera
				: null;
			if (background != null)
				m_BgOriginPos = background.anchoredPosition;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// 只响应屏幕左侧 activeArea 区域
			if (eventData.position.x > Screen.width * activeArea)
				return;

			var canvasRT = (RectTransform)m_Canvas.transform;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
				    canvasRT, eventData.position, m_UiCam, out var local))
				return;

			m_PointerDownPos = local;

			// 底座移到按下位置（anchoredPosition 相对自身锚点，换算到 Canvas 局部坐标）
			if (background != null)
			{
				var anchor = new Vector2(
					canvasRT.rect.width * (background.anchorMin.x - canvasRT.pivot.x),
					canvasRT.rect.height * (background.anchorMin.y - canvasRT.pivot.y));
				background.anchoredPosition = local - anchor;
			}

			ResetHandle();
			SendValueToControl(Vector2.zero);
			m_Active = true;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!m_Active)
				return;

			var canvasRT = (RectTransform)m_Canvas.transform;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
				    canvasRT, eventData.position, m_UiCam, out var local))
				return;

			var delta = Vector2.ClampMagnitude(local - m_PointerDownPos, movementRange);
			if (handle != null)
				handle.anchoredPosition = delta;

			SendValueToControl(delta / movementRange);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!m_Active)
				return;
			m_Active = false;

			ResetHandle();

			// 松手回到初始位置
			if (returnToOrigin && background != null)
				background.anchoredPosition = m_BgOriginPos;

			SendValueToControl(Vector2.zero);
		}

		void ResetHandle()
		{
			if (handle != null)
				handle.anchoredPosition = Vector2.zero;
		}
	}
}
