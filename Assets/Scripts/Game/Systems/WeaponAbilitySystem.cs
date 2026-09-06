using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 武器能力系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IWeaponAbilitySystem : ISystem
    {
        /// <summary>玩家完成一次近战攻击节奏后调用：各已解锁武器能力在此齐发自己的投射物</summary>
        void OnPlayerAttack(Vector3 playerPos, List<Enemy> sortedTargets);
    }

    /// <summary>
    /// 武器能力系统：集中管理"哪些武器能力随玩家攻击齐发"。
    /// PlayerController 只管近战基础攻击，并在攻击后调一次 OnPlayerAttack；
    /// 新增武器能力 = 这里加一个发射分支 + GameAssetsSystem 加 Spawn 方法 + 配置资产，
    /// PlayerController 不再改动。分支升级数值由 AbilitySystem.GetWeaponStatTotal 聚合。
    /// </summary>
    public class WeaponAbilitySystem : AbstractSystem, IWeaponAbilitySystem
    {
        private IGameAssetsSystem mAssetsSystem; // 生成投射物（惰性解析，规避注册顺序问题）

        protected override void OnInit()
        {
            // 无初始化依赖：Model/System 均在首次 OnPlayerAttack 时惰性解析
        }

        public void OnPlayerAttack(Vector3 playerPos, List<Enemy> sortedTargets)
        {
            if (sortedTargets == null || sortedTargets.Count == 0) return;
            if (mAssetsSystem == null) mAssetsSystem = this.GetSystem<IGameAssetsSystem>();

            var model = this.GetModel<GameModel>();

            // 穿透剑：朝最近的敌人发射一把固定弹道飞剑（sortedTargets 已按距离升序，首个即最近）。
            // 方向按发射瞬间锁定、飞行中不追踪（怪物可走位躲开）；伤害/穿透数按分支卡等级成长。
            if (model.GetAbilityLevel(AbilitySystem.PierceSwordId) > 0)
            {
                var toNearest = sortedTargets[0].transform.position - playerPos;
                var dir = toNearest.sqrMagnitude > 0.0001f
                    ? (Vector2)toNearest.normalized
                    : Vector2.right; // 敌人与玩家重叠时的退化保护

                mAssetsSystem.SpawnPierceSword(playerPos, dir);
            }

            // 未来武器能力：在此追加 if (model.GetAbilityLevel(XxxId) > 0) { ... }
        }
    }
}
