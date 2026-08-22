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

const sharedCanvas = wx.getSharedCanvas();
const ctx = sharedCanvas.getContext('2d');

const RANK_KEY = 'bestTime';

let lastList = [];      // 最近一次拉到的好友数据（头像异步加载完成后用它重绘，不重复拉接口）
let avatarCache = {};   // openId -> Image（头像缓存，整局生命周期复用）

wx.onMessage((msg) => {
    if (msg.type === 'WXRender') {
        fetchAndDraw();
    }
});

// 拉取"也玩本游戏的好友"的托管数据（自己也在返回列表里）
function fetchAndDraw() {
    wx.getFriendCloudStorage({
        keyList: [RANK_KEY],
        success: (res) => {
            lastList = parseAndSort(res.data || []);
            draw(lastList);
        },
        fail: (err) => {
            drawTip('排行榜加载失败: ' + (err.errMsg || '未知错误'));
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

function draw(list) {
    const W = sharedCanvas.width;
    const H = sharedCanvas.height;
    const unit = H / 100; // 1% 画布高度为一个基准单位

    // 背景
    ctx.fillStyle = '#2a2b3d';
    ctx.fillRect(0, 0, W, H);

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

// 头像：已加载则圆形裁剪绘制；未加载先画占位圆并触发加载，加载完成后整榜重绘
function drawAvatar(item, x, y, size) {
    const img = avatarCache[item.openId];
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
    if (!img && item.avatarUrl) {
        const image = wx.createImage();
        image.__loaded = false;
        image.onload = () => {
            image.__loaded = true;
            draw(lastList);
        };
        image.src = item.avatarUrl;
        avatarCache[item.openId] = image;
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
