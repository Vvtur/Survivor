// 云函数：load —— 存档下载（新用户无记录时返回 data:null，不算错误）
const cloud = require('wx-server-sdk')
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV })
const db = cloud.database()

exports.main = async (event) => {
  const { OPENID } = cloud.getWXContext()
  const userId = OPENID || event.openid

  try {
    const res = await db.collection('user_save').doc(userId).get()
    const record = res.data || {}
    return { code: 0, data: record.saveData || null, ts: record.ts || 0 }
  } catch (e) {
    // doc(userId).get() 记录不存在时会抛错 → 新玩家，客户端走本地存档分支
    return { code: 0, data: null, ts: 0 }
  }
}
