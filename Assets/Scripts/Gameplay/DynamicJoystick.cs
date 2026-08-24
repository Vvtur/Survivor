using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 动态跟随摇杆（点哪背景 BG 去哪，拖动 Joy 即输出方向）。
    ///
    /// 用法：
    /// 1. 在 Canvas 下新建一个透明 Image 作为“全屏接收层”，把本脚本挂上去（会自动加 Image）。
    ///    把它的 RectTransform 拉伸覆盖可操作区域（例如左半屏：anchor (0,0)-(0.5,1)；全屏：(0,0)-(1,1)）。
    /// 2. 把 Inspector 里的 background 拖成你的 BG，handle 拖成你的 Joy。
    /// 3. 禁用（取消勾选）Joy 上原来的 OnScreenStick 组件，避免两套逻辑抢同一个 leftStick。
    ///
    /// controlPath 默认 <Gamepad>/leftStick，与原来的 OnScreenStick 一致，
    /// 因此 PlayerController 中 input.Player.Move.ReadValue<Vector2>() 无需任何改动。
    /// </summary>
    [AddComponentMenu("Input/Dynamic Joystick")]
    [RequireComponent(typeof(Image))]
    public class DynamicJoystick : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("摇杆背景 BG（本脚本会把它移动到手指按下处）")]
        public RectTransform background;

        [Tooltip("摇杆手柄 Joy（必须是 background 的子物体，拖动时相对 background 中心移动）")]
        public RectTransform handle;

        [Tooltip("手柄可偏离背景中心的最大像素距离")]
        public float movementRange = 50f;

        [Tooltip("松手后背景是否回到初始位置；false 则停留在松手处")]
        public bool returnBackgroundToOrigin = true;

        [Tooltip("可响应触摸的屏幕宽度比例(0.1~1)，1=全屏，0.5=仅左半屏")]
        [Range(0.1f, 1f)]
        public float activeArea = 1f;

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
        Vector2 m_BgOriginPos;       // 背景初始位置
        Vector2 m_PointerDownLocal;  // 按下点在 Canvas 局部坐标
        bool m_Active;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Canvas = GetComponentInParent<Canvas>();
            m_UiCam = (m_Canvas != null && m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? m_Canvas.worldCamera : null;
            if (background != null)
                m_BgOriginPos = background.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 只响应 activeArea 范围内的按下（按屏幕横向比例限制）
            if (eventData.position.x > Screen.width * activeArea)
                return;

            var canvasRT = (RectTransform)m_Canvas.transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, eventData.position, m_UiCam, out var local))
                return;

            m_PointerDownLocal = local;

            // 把背景移动到手指按下处（换算到 Canvas 局部坐标，并扣除背景自身锚点偏移）
            if (background != null)
            {
                var anchor = new Vector2(
                    canvasRT.rect.width * (background.anchorMin.x - canvasRT.pivot.x),
                    canvasRT.rect.height * (background.anchorMin.y - canvasRT.pivot.y));
                background.anchoredPosition = local - anchor;
            }

            // 手柄归位到背景中心
            if (handle != null)
                handle.anchoredPosition = Vector2.zero;

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

            // 相对按下点计算偏移，并限制在 movementRange 内
            var delta = Vector2.ClampMagnitude(local - m_PointerDownLocal, movementRange);
            if (handle != null)
                handle.anchoredPosition = delta;

            // 归一化后输出（摇杆方向），范围 [-1,1]
            SendValueToControl(delta / movementRange);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_Active)
                return;
            m_Active = false;

            if (handle != null)
                handle.anchoredPosition = Vector2.zero;

            if (returnBackgroundToOrigin && background != null)
                background.anchoredPosition = m_BgOriginPos;

            SendValueToControl(Vector2.zero);
        }
    }
}
