// 开放数据域入口：好友存活时间排行榜
//
// 微信的隐私沙盒设计：好友的昵称/头像/成绩只能在开放数据域里读取和绘制，
// 原始数据不允许传回主域（Unity）——主域只能拿到 sharedCanvas 这张"图"。
//
// 消息协议（与主域 unity-sdk/open-data.js 约定）：
//   主域调 WX.ShowOpenData → 发 {type:'WXRender', x, y, width, height, devicePixelRatio}
//   主域调 WX.HideOpenData → 发 {type:'WXDestroy'}（无需处理，Unity 侧停止纹理刷新）
//
// 成绩格式：纯数字字符串（秒），与 C# 侧 WXLeaderboard.RankKey("bestTime") 上报对应。
// （注意：微信转换工具每次转换会用官方模板覆盖本目录——恢复方式见 WX/restore-custom.ps1）
//
// 排查日志约定（vConsole 里按 [open-data] 过滤）：
//   booted        → 子域脚本已加载（没这条 = 子域根本没启动）
//   msg:WXRender  → 收到主域的显示消息（没这条 = 消息没到达）
//   friends:N     → 拉到 N 条有成绩的记录（0 = 还没人上报过成绩）

const sharedCanvas = wx.getSharedCanvas();
const ctx = sharedCanvas.getContext('2d');

const RANK_KEY = 'bestTime';

let lastList = [];      // 最近一次拉到的好友数据（头像异步加载完成后用它重绘，不重复拉接口）
let avatarCache = {};   // avatarUrl -> Image（头像缓存，整局生命周期复用）

console.log('[open-data] booted, canvas default size:', sharedCanvas.width, 'x', sharedCanvas.height);

wx.onMessage((msg) => {
    console.log('[open-data] msg:', msg && msg.type);
    if (msg.type === 'WXRender') {
        console.log('[open-data] render rect:', msg.x, msg.y, msg.width, msg.height,
            '| canvas now:', sharedCanvas.width, 'x', sharedCanvas.height);
        drawLoading();
        fetchAndDraw();
    }
});

// 拉取"也玩本游戏的好友"的托管数据（自己也在返回列表里）
function fetchAndDraw() {
    avatarCache = {}; // 每次拉取重建头像缓存：授权后 avatarUrl 会变成真实的，避免一直画旧的脱敏头像
    wx.getFriendCloudStorage({
        keyList: [RANK_KEY],
        success: (res) => {
            const raw = res.data || [];
            // 诊断：打印接口原始返回的身份字段（vConsole 按 [open-data] 过滤）
            //   o=openId, n=nickName, a=avatarUrl 前24字符
            //   若两行 o 相同/为空 → 缓存 key 冲突；若 a 相同 → 接口返回同一头像（平台数据问题）
            console.log('[open-data] raw identities:', raw.map(i => ({ o: i.openId, n: i.nickName, a: (i.avatarUrl || '').slice(0, 24) })));
            lastList = parseAndSort(raw);
            console.log('[open-data] friends with record:', lastList.length);
            draw(lastList);
        },
        fail: (err) => {
            console.error('[open-data] getFriendCloudStorage fail:', err && err.errMsg);
            drawTip('排行榜加载失败: ' + ((err && err.errMsg) || '未知错误'));
        },
    });
}

// 提取 KVData 里的成绩并按存活时间降序；从未上报过成绩的好友跳过
function parseAndSort(rawList) {
    return rawList
        .map((item) => {
            const kv = (item.KVDataList || []).find((k) => k.key === RANK_KEY);
            return {
                openId: item.openId,
                nickName: item.nickName || '神秘玩家',
                avatarUrl: item.avatarUrl,
                score: kv ? (parseInt(kv.value, 10) || 0) : -1,
            };
        })
        .filter((item) => item.score >= 0)
        .sort((a, b) => b.score - a.score);
}

// ---------- 绘制（横屏布局，所有尺寸按画布高度等比缩放） ----------

function drawLoading() {
    const W = sharedCanvas.width;
    const H = sharedCanvas.height;
    if (W <= 0 || H <= 0) return; // 尺寸未就绪（WXRender 处理中）
    ctx.clearRect(0, 0, W, H); // 透明底：露出 Unity 面板的背景图
    ctx.fillStyle = '#a8abc4';
    ctx.font = Math.round(H / 20) + 'px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('加载中...', W / 2, H / 2);
}

function draw(list) {
    const W = sharedCanvas.width;
    const H = sharedCanvas.height;
    const unit = H / 100; // 1% 画布高度为一个基准单位

    // 透明底（clearRect）：不遮 Unity 面板的背景图，样式由 Unity 侧负责。
    // 想改回画布自带底色：ctx.fillStyle = '#2a2b3d'; ctx.fillRect(0, 0, W, H);
    ctx.clearRect(0, 0, W, H);

    // 标题
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold ' + Math.round(unit * 7) + 'px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('好友存活榜', W / 2, unit * 8);

    if (!list.length) {
        drawTip('暂无好友成绩，快去创造第一个纪录吧');
        return;
    }

    const top = unit * 15;
    const rowH = unit * 11;
    const maxRows = Math.floor((H - top) / rowH);

    list.slice(0, maxRows).forEach((item, i) => {
        drawRow(item, i + 1, top + i * rowH, rowH, unit, W);
    });
}

function drawRow(item, rank, y, rowH, unit, W) {
    // 隔行底色
    if (rank % 2 === 1) {
        ctx.fillStyle = 'rgba(255,255,255,0.05)';
        ctx.fillRect(unit * 4, y, W - unit * 8, rowH);
    }

    ctx.textBaseline = 'middle';

    // 名次（前三金色）
    ctx.fillStyle = rank <= 3 ? '#ffd700' : '#a8abc4';
    ctx.font = 'bold ' + Math.round(unit * 5) + 'px sans-serif';
    ctx.textAlign = 'left';
    ctx.fillText(String(rank), unit * 8, y + rowH / 2);

    // 头像
    const avatarSize = rowH * 0.7;
    const avatarX = unit * 15;
    const avatarY = y + (rowH - avatarSize) / 2;
    drawAvatar(item, avatarX, avatarY, avatarSize);

    // 昵称
    ctx.fillStyle = '#ffffff';
    ctx.font = Math.round(unit * 4.5) + 'px sans-serif';
    ctx.textAlign = 'left';
    ctx.fillText(item.nickName, avatarX + avatarSize + unit * 3, y + rowH / 2);

    // 成绩（右侧，绿色高亮）
    ctx.fillStyle = '#7ee787';
    ctx.font = 'bold ' + Math.round(unit * 4.5) + 'px sans-serif';
    ctx.textAlign = 'right';
    ctx.fillText(formatTime(item.score), W - unit * 8, y + rowH / 2);
}

// 头像：已加载则圆形裁剪绘制；未加载先画占位圆并触发加载，加载完成后整榜重绘。
// 缓存 key 用 avatarUrl 而不是 openId：匿名/隐私状态下 openId 可能为空或重复，
// 用 openId 做 key 会让不同玩家挤进同一缓存槽，画出同一张头像。
function drawAvatar(item, x, y, size) {
    const cacheKey = item.avatarUrl;
    const img = cacheKey ? avatarCache[cacheKey] : null;
    if (img && img.__loaded) {
        ctx.save();
        ctx.beginPath();
        ctx.arc(x + size / 2, y + size / 2, size / 2, 0, Math.PI * 2);
        ctx.clip();
        ctx.drawImage(img, x, y, size, size);
        ctx.restore();
        return;
    }

    // 占位圆
    ctx.fillStyle = '#4a4c66';
    ctx.beginPath();
    ctx.arc(x + size / 2, y + size / 2, size / 2, 0, Math.PI * 2);
    ctx.fill();

    // 只触发一次加载，完成后用已缓存的数据重绘（不重复调 getFriendCloudStorage）
    if (!img && cacheKey) {
        const image = wx.createImage();
        image.__loaded = false;
        image.onload = () => {
            image.__loaded = true;
            draw(lastList);
        };
        image.src = cacheKey;
        avatarCache[cacheKey] = image;
    }
}

function drawTip(text) {
    const W = sharedCanvas.width;
    const H = sharedCanvas.height;
    ctx.fillStyle = '#a8abc4';
    ctx.font = Math.round(H / 20) + 'px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(text, W / 2, H / 2);
}

// 秒 → "分:秒"（如 312 → 5:12）
function formatTime(sec) {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return m + ':' + (s < 10 ? '0' : '') + s;
}
