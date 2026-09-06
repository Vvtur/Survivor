using UnityEngine;
using UnityEngine.UI;

namespace QFramework.UI
{
	/// <summary>
	/// Canvas 宽高比自适应：挂在场景的 Canvas 上（GameStart / MainGame）。
	///
	/// 项目参考分辨率 1280x720（16:9），CanvasScaler 默认 match=0（匹配高度）。
	/// 匹配高度在「比参考更宽」的屏幕上没问题（横向留白，边缘锚点元素自然撑开），
	/// 但在「比参考更窄」的屏幕上会横向裁切内容，因为：
	///     实际参考宽 = 720 * 屏幕宽高比
	///     16:10 → 1152（内容 1280，溢出 128）
	///     4:3   →  960（内容 1280，溢出 320）
	/// 所以当屏幕比参考更窄时切换成匹配宽度，保证任何比例下内容都不被裁掉。
	///
	/// 只改 CanvasScaler 的参数，不动层级、不动锚点，对已有 UI 零侵入。
	/// </summary>
	[RequireComponent(typeof(Canvas))]
	public class UICanvasAdapter : MonoBehaviour
	{
		private CanvasScaler mScaler;

		private void Awake()
		{
			mScaler = GetComponent<CanvasScaler>();
		}

		private void OnEnable()
		{
			Apply();
		}

		// 分辨率变化、安全区变化、设备转向都会触发
		private void OnRectTransformDimensionsChange()
		{
			Apply();
		}

		private void Apply()
		{
			if (mScaler == null) mScaler = GetComponent<CanvasScaler>();
			if (mScaler == null) return;
			if (mScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return;

			var refRes = mScaler.referenceResolution;
			if (refRes.y <= 0f) return;

			float refAspect = refRes.x / refRes.y;
			float curAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : refAspect;

			// 比参考更窄 → 匹配宽度（防裁切）；否则保持匹配高度（横向留白）
			mScaler.matchWidthOrHeight = curAspect < refAspect ? 1f : 0f;
		}
	}

	/// <summary>
	/// 刘海 / 圆角 / Home 指示条避让：挂在面板根节点上，随预制体走，
	/// 场景直挂（PlayerInfoPanel）与 UIKit 打开（各弹窗）都生效。
	///
	/// 做法：把自己的 anchorMin / anchorMax 设成 Screen.safeArea 的归一化矩形，offset 归零。
	/// 注意只缩进面板自己，不要缩 Canvas——缩 Canvas 会露出黑边。
	/// 带全屏遮罩底的弹窗慎用：遮罩会一起缩进露出白边，那种面板应拆成
	/// 「全屏底 + 内容根节点」，把本组件挂在内容根节点上。
	/// </summary>
	public class UISafeAreaFitter : MonoBehaviour
	{
		private RectTransform mRect;
		private Rect mLastSafeArea;
		private Vector2 mLastResolution;

		private void Awake()
		{
			mRect = GetComponent<RectTransform>();
		}

		private void OnEnable()
		{
			Apply(true);
		}

		private void OnRectTransformDimensionsChange()
		{
			Apply(false);
		}

		private void Apply(bool force)
		{
			if (mRect == null) mRect = GetComponent<RectTransform>();
			if (mRect == null) return;

			var safe = Screen.safeArea;
			var res = new Vector2(Screen.width, Screen.height);

			// 非强制时做脏检查：安全区和分辨率都没变就不重算
			if (!force && safe == mLastSafeArea && res == mLastResolution) return;
			mLastSafeArea = safe;
			mLastResolution = res;

			if (res.x <= 0f || res.y <= 0f) return;

			Vector2 min = safe.position;
			Vector2 max = safe.position + safe.size;

			mRect.anchorMin = new Vector2(min.x / res.x, min.y / res.y);
			mRect.anchorMax = new Vector2(max.x / res.x, max.y / res.y);
			mRect.offsetMin = Vector2.zero;
			mRect.offsetMax = Vector2.zero;
		}
	}
}
