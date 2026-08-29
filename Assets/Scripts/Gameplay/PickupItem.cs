using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 拾取物抽象基类（模板方法模式）：统一"玩家碰到 → 生效 → 音效 → 消失"的表现流程。
    /// 子类只负责定义"生效"（OnPickup，通常发送一条 Command 写 Model），
    /// 新增道具 = 新增一个子类 + DropType 枚举值 + DropConfig 条目，无需改动任何现有逻辑。
    ///
    /// QF 规范：本类属于表现层（IController），只发 Command 不直写 Model。
    /// </summary>
    public abstract class PickupItem : ViewController, IController
    {
        public IArchitecture GetArchitecture() => GameArchitecture.Interface;

        /// <summary>拾取音效（子类可覆盖更换）</summary>
        protected virtual string PickupSound => AudioNames.Pickup;

        /// <summary>能否被磁铁收集（金币/宝石为 true，功能道具为 false，防止磁铁连锁吞掉其他磁铁/血包）</summary>
        public virtual bool CollectibleByMagnet => false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            OnPickup();
            OnPickedUp();
        }

        /// <summary>生效逻辑：子类实现（发送对应 Command）</summary>
        protected abstract void OnPickup();

        /// <summary>拾取后的表现收尾：播音效 + 消失（子类一般无需覆盖）</summary>
        protected virtual void OnPickedUp()
        {
            AudioKit.PlaySound(PickupSound);
            Destroy(gameObject);
        }

        /// <summary>
        /// 被磁铁收集：静默生效（不播自身音效，由磁铁统一播一次），供 Magnet 子类遍历调用。
        /// 走 OnPickup 保证与手动拾取完全同一条 Command 数据链路。
        /// </summary>
        public void CollectByMagnet()
        {
            if (!CollectibleByMagnet || this == null) return;
            OnPickup();
            Destroy(gameObject);
        }
    }
}
