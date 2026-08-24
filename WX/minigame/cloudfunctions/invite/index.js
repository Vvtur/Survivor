// 云函数：invite —— 邀请有礼
// 集合约定：
//   invite_events : doc id = 被邀请者 openid，{ inviter, ts }      一个好友只给同一邀请者计一次（幂等）
//   invite_rewards: doc id = 邀请者 openid，{ ts }                 领奖记录，存在即已领取（防重复）
// action:
//   get_openid —— 返回调用者 openid（分享时拼 query 用）
//   track      —— 被邀请者进入游戏上报（防自邀、参数非法静默跳过）
//   status     —— 查询领取资格 { invited, claimed }
//   claim      —— 领奖校验：未领过 && 有邀请记录 → 写入领奖记录
// 返回码：0 成功 / 1 已领过 / 2 无邀请记录 / 3 参数非法（自邀/空 inviter）/ 4 未知 action / 9 内部错误
const cloud = require('wx-server-sdk')
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV })
const db = cloud.database()

exports.main = async (event) => {
  // 真机调用：OPENID 由微信网关自动注入；云端测试面板没有玩家身份，退回 event.openid（与 save 一致）
  const { OPENID } = cloud.getWXContext()
  const userId = OPENID || event.openid
  const action = event.action || ''

  // 1) 取自身 openid（分享拼 query 用）
  if (action === 'get_openid') {
    return { code: 0, openid: userId }
  }

  // 2) 被邀请者上报：invite_events doc(被邀请者openid) 幂等写入
  if (action === 'track') {
    const inviter = (event.inviter || '').trim()
    if (!inviter) return { code: 3, msg: 'empty inviter' }         // 参数非法：静默跳过
    if (inviter === userId) return { code: 3, msg: 'self invite' } // 防自邀
    try {
      const doc = db.collection('invite_events').doc(userId)
      const existed = await doc.get().catch(() => null)
      if (existed && existed.data) return { code: 0, dup: true }   // 已记录过：跳过（幂等）
      await doc.set({
        data: {
          inviter: inviter,
          ts: Date.now(),
        },
      })
      return { code: 0 }
    } catch (e) {
      return { code: 9, msg: e.message }
    }
  }

  // 3) 查询资格：是否有好友进入过 + 是否已领奖
  if (action === 'status') {
    try {
      const count = await db.collection('invite_events').where({ inviter: userId }).count()
      const claimed = await db.collection('invite_rewards').doc(userId).get()
        .then(() => true).catch(() => false)
      return { code: 0, invited: count.total > 0, claimed: claimed }
    } catch (e) {
      return { code: 9, msg: e.message }
    }
  }

  // 4) 领奖：校验未领过 + 有邀请记录 → 写入领奖记录（客户端凭 code:0 加金币）
  if (action === 'claim') {
    try {
      const claimedDoc = db.collection('invite_rewards').doc(userId)
      const already = await claimedDoc.get().then(() => true).catch(() => false)
      if (already) return { code: 1, msg: 'already claimed' }

      const count = await db.collection('invite_events').where({ inviter: userId }).count()
      if (count.total === 0) return { code: 2, msg: 'no invite record' }

      await claimedDoc.set({
        data: {
          ts: Date.now(),
        },
      })
      return { code: 0 }
    } catch (e) {
      return { code: 9, msg: e.message }
    }
  }

  return { code: 4, msg: 'unknown action' }
}
