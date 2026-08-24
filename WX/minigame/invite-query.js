// ============================================================
// invite-query.js - Invite link query stash (injected by restore-custom)
//
// WHY: The Unity WebGL transform plugin delivers wx.onShow callback
//      results to C# via JsonUtility, which CANNOT deserialize
//      Dictionary fields - so OnShowListenerResult.query always
//      arrives as an EMPTY dict in C# (hot-start invite tracking
//      silently breaks).
//
// HOW: This script runs at game.js import time (before Unity wasm
//      boots). It uses native wx APIs to serialize the launch query
//      and every onShow query into local storage. The C# side
//      (InviteSystem) reads it back via WX.StorageGetStringSync.
//      JS listener registered here fires BEFORE the plugin's C#
//      dispatch (registered later when Unity boots), so the stash
//      is always fresh when C# reads it.
// ============================================================
;(() => {
  const KEY = 'invite_launch_query';
  const stash = (q) => {
    try {
      wx.setStorageSync(KEY, JSON.stringify(q || {}));
    } catch (e) {
      console.error('[invite-query] stash failed', e);
    }
  };

  // Cold start: capture launch query at boot
  try {
    const opt = wx.getLaunchOptionsSync();
    stash(opt && opt.query);
    console.log('[invite-query] boot stash:', wx.getStorageSync(KEY));
  } catch (e) {
    console.error('[invite-query] getLaunchOptionsSync failed', e);
  }

  // Hot start: every onShow overwrites the stash
  // (no-card foreground = {} -> clears stale invite query)
  wx.onShow((res) => {
    stash((res || {}).query);
    console.log('[invite-query] onShow stash:', wx.getStorageSync(KEY));
  });
})();
