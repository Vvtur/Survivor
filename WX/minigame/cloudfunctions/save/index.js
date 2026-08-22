// 云函数：save —— 存档上传（upsert：一个玩家一条记录，doc id = OPENID）
const cloud = require('wx-server-sdk')
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV })
const db = cloud.database()

exports.main = async (event) => {
  // 真机调用：OPENID 由微信网关自动注入；云端测试面板没有玩家身份，退回 event.openid
  const { OPENID } = cloud.getWXContext()
  const userId = OPENID || event.openid

  const saveData = event.data // SaveData 整档（嵌套对象，原样透传入库）
  const ts = event.ts         // 客户端时间戳（秒），Last-Write-Win 仲裁在客户端做

  await db.collection('user_save').doc(userId).set({
    data: {
      saveData: saveData,
      ts: ts,
      updated_at: Date.now(), // 服务端毫秒时间戳，仅排查用
    },
  })

  return { code: 0 }
}
