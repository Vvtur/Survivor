using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class BGController : ViewController
	{
		[SerializeField]
		private float tileWidth = 30f;   // 单块宽
		[SerializeField]
		private float tileHeight = 15f;  // 单块高

		// 9 个背景块（3×3），[row, col]
		private Transform[,] mBgs = new Transform[3, 3];

		// 中心块在数组中的逻辑坐标（玩家所在的块）
		private int mCenterRow = 1;
		private int mCenterCol = 1;

		// 中心块的世界位置（玩家相对它判定越界）
		private Vector3 mCenterPos;

		void Start()
		{
			// 收集 9 个子背景块（排除自身），按行优先存入 3×3
			var children = new Transform[9];
			int index = 0;
			foreach (Transform child in transform)
			{
				if (index >= 9) break;
				children[index++] = child;
			}

			for (int row = 0; row < 3; row++)
			{
				for (int col = 0; col < 3; col++)
				{
					mBgs[row, col] = children[row * 3 + col];
				}
			}

			// 初始中心块 = 数组 [1,1]（中间那块）
			mCenterPos = mBgs[mCenterRow, mCenterCol].position;
		}

		void Update()
		{
			if (Player == null) return;

			// 玩家相对中心块的世界偏移
			var offset = Player.transform.position - mCenterPos;

			// 水平：玩家相对中心块越出半块宽（tileWidth/2）→ 换一列
			while (offset.x > tileWidth * 0.5f)
			{
				ColumnChange(false);      // 玩家向右 → 左列移到右侧
				offset.x -= tileWidth;    // 中心位置随换位平移一整块
			}
			while (offset.x < -tileWidth * 0.5f)
			{
				ColumnChange(true);       // 玩家向左 → 右列移到左侧
				offset.x += tileWidth;
			}

			// 垂直：玩家相对中心块越出半块高（tileHeight/2）→ 换一行
			while (offset.y > tileHeight * 0.5f)
			{
				RowChange(false);         // 玩家向上 → 底行移到上方
				offset.y -= tileHeight;
			}
			while (offset.y < -tileHeight * 0.5f)
			{
				RowChange(true);          // 玩家向下 → 顶行移到下方
				offset.y += tileHeight;
			}
		}

		/// <summary>
		/// 列换位：玩家横向越出半块宽时触发。
		/// RightToLeft = true → 玩家向左，右列平移到左侧（3 块距离）；
		/// RightToLeft = false → 玩家向右，左列平移到右侧。
		/// 关键：数组下标必须随物理位置整体轮转一格，
		/// 否则 [row,src] 残留旧引用（指向已搬走的块），下一次换位会搬错块。
		/// </summary>
		private void ColumnChange(bool RightToLeft)
		{
			// 水平位移：3 块宽的距离（向右移出的块搬到右侧，向左反之）
			float dx = RightToLeft ? -tileWidth * 3 : tileWidth * 3;

			for (int row = 0; row < 3; row++)
			{
				// 玩家向右：搬走最左列(0)，数组 [0]←[1]、[1]←[2]、[2]←搬走的块
				// 玩家向左：搬走最右列(2)，数组 [2]←[1]、[1]←[0]、[0]←搬走的块
				Transform tile;
				if (RightToLeft)
				{
					tile = mBgs[row, 2];
					mBgs[row, 2] = mBgs[row, 1];
					mBgs[row, 1] = mBgs[row, 0];
					mBgs[row, 0] = tile;
				}
				else
				{
					tile = mBgs[row, 0];
					mBgs[row, 0] = mBgs[row, 1];
					mBgs[row, 1] = mBgs[row, 2];
					mBgs[row, 2] = tile;
				}

				tile.position += new Vector3(dx, 0, 0); // 物理平移到对侧
			}

			// 中心块位置同步平移一整块（保持判定基准）
			mCenterPos += new Vector3(RightToLeft ? -tileWidth : tileWidth, 0, 0);
		}

		/// <summary>
		/// 行换位：玩家纵向越出半块高时触发（与 ColumnChange 同构，含数组整体轮转）。
		/// UpToDown = true → 玩家向下，顶行平移到下方；
		/// UpToDown = false → 玩家向上，底行平移到上方。
		/// </summary>
		private void RowChange(bool UpToDown)
		{
			// 垂直位移：3 块高的距离
			float dy = UpToDown ? -tileHeight * 3 : tileHeight * 3;

			for (int col = 0; col < 3; col++)
			{
				// 玩家向下：搬走最顶行(0)，数组 [0]←[1]、[1]←[2]、[2]←搬走的块
				// 玩家向上：搬走最底行(2)，数组 [2]←[1]、[1]←[0]、[0]←搬走的块
				Transform tile;
				if (UpToDown)
				{
					tile = mBgs[0, col];
					mBgs[0, col] = mBgs[1, col];
					mBgs[1, col] = mBgs[2, col];
					mBgs[2, col] = tile;
				}
				else
				{
					tile = mBgs[2, col];
					mBgs[2, col] = mBgs[1, col];
					mBgs[1, col] = mBgs[0, col];
					mBgs[0, col] = tile;
				}

				tile.position += new Vector3(0, dy, 0); // 物理平移到对侧
			}

			// 中心块位置同步平移一整块（保持判定基准）
			mCenterPos += new Vector3(0, UpToDown ? -tileHeight : tileHeight, 0);
		}
	}
}
