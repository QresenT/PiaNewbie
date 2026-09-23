(function () {
  const canvas = document.getElementById('rollCanvas');
  const ctx = canvas.getContext('2d', { alpha: false });
  const hintEl = document.getElementById('homeRowHint');
  const judgmentEl = document.getElementById('judgmentBanner');
  const judgmentMainEl = document.getElementById('judgmentMain');
  const judgmentSubEl = document.getElementById('judgmentSub');
  const HOME_LABELS = ['a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', "'"];

  let state = {
    guideNotes: [],
    backgroundNotes: [],
    mode: 'flow',
    minPitch: 48,
    maxPitch: 72,
    currentGuideIndex: 0,
    playbackTime: 0,
    isRhythmMode: false,
    highlightPitch: null,
    pressableKeyIndices: [],
    freezePlayback: false,
    judgmentText: '',
    judgmentSubtext: '',
    judgmentKind: '',
    scrollSpeed: 1
  };

  let dirty = true;
  let syncPlaybackTime = 0;
  let syncWallMs = 0;
  let hintDirty = true;
  let lastHintKey = '';

  const COLORS = {
    bg: '#0b0b12',
    guide: '#5b8cff',
    guideNext: '#7eb8ff',
    guideCurrent: '#b8e0ff',
    guideGlow: '#ffffff',
    guideNextGlow: '#a8d4ff',
    background: '#3d3d4a',
    playLine: '#ffffff',
    whiteKey: '#e6e8f0',
    blackKey: '#1a1a22',
    keyHighlight: 'rgba(91, 140, 255, 0.55)',
    arrow: '#ffcc66',
    grid: 'rgba(255,255,255,0.06)',
    pianoWell: '#0b0b12'
  };

  function applyTheme(theme) {
    if (!theme) return;
    if (theme.guide) COLORS.guide = theme.guide;
    if (theme.guideNext) COLORS.guideNext = theme.guideNext;
    if (theme.guideCurrent) COLORS.guideCurrent = theme.guideCurrent;
    if (theme.keyHighlight) COLORS.keyHighlight = theme.keyHighlight;
    COLORS.guideGlow = '#ffffff';
    COLORS.guideNextGlow = theme.guideNext || COLORS.guideNext;

    const root = document.documentElement;
    if (theme.pressableBorder)
      root.style.setProperty('--key-pressable-border', theme.pressableBorder);
    if (theme.pressableBg)
      root.style.setProperty('--key-pressable-bg', theme.pressableBg);
    if (theme.pressableGlow)
      root.style.setProperty('--key-pressable-glow', theme.pressableGlow);
    markDirty();
    hintDirty = true;
  }

  const PX_PER_SEC = 140;

  function pixelsPerSecond() {
    return PX_PER_SEC * (state.scrollSpeed > 0 ? state.scrollSpeed : 1);
  }
  const PIANO_HEIGHT_RATIO = 0.24;
  const BG_VISIBLE_SEC = 10;

  function resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const w = canvas.clientWidth;
    const h = canvas.clientHeight;
    canvas.width = Math.floor(w * dpr);
    canvas.height = Math.floor(h * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    markDirty();
  }

  window.addEventListener('resize', resize);
  resize();

  function markDirty() { dirty = true; }

  function isBlack(pitch) {
    return [1, 3, 6, 8, 10].includes(pitch % 12);
  }

  function pitchX(pitch, w, minP, maxP) {
    const range = Math.max(1, maxP - minP);
    const keyW = w / range;
    return ((pitch - minP) / range) * (w - keyW);
  }

  function noteWidth(w, minP, maxP) {
    return Math.max(8, (w / Math.max(1, maxP - minP)) * 0.82);
  }

  function getPlaybackTime() {
    if (state.freezePlayback) return state.playbackTime;
    if (!syncWallMs) return state.playbackTime;
    return syncPlaybackTime + (performance.now() - syncWallMs) / 1000;
  }

  function noteY(note, playY, playbackTime) {
    return playY - (note.startTime - playbackTime) * pixelsPerSecond();
  }

  function roundRect(x, y, w, h, r) {
    if (ctx.roundRect) {
      ctx.beginPath();
      ctx.roundRect(x, y, w, h, r);
      return;
    }
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
  }

  function drawGrid(w, playY, minP, maxP) {
    ctx.strokeStyle = COLORS.grid;
    ctx.lineWidth = 1;
    const range = Math.max(1, maxP - minP);
    for (let i = 0; i <= range; i++) {
      const x = (i / range) * w;
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, playY);
      ctx.stroke();
    }
  }

  /** 건반 뒤로 넘어간 노트(검은 건반 아래 빈 영역 포함)를 가림 */
  function drawPianoWellMask(w, h, pianoTop) {
    ctx.fillStyle = COLORS.pianoWell;
    ctx.fillRect(0, pianoTop, w, h - pianoTop);
    ctx.fillStyle = 'rgba(0, 0, 0, 0.35)';
    ctx.fillRect(0, pianoTop, w, 2);
  }

  function drawPiano(w, h, pianoTop, minP, maxP) {
    const range = Math.max(1, maxP - minP);
    const keyW = w / range;
    for (let p = minP; p <= maxP; p++) {
      const x = pitchX(p, w, minP, maxP);
      const black = isBlack(p);
      ctx.fillStyle = black ? COLORS.blackKey : COLORS.whiteKey;
      const kh = black ? (h - pianoTop) * 0.62 : (h - pianoTop);
      ctx.fillRect(x, pianoTop, keyW - 1, kh);
      if (p === state.highlightPitch) {
        ctx.fillStyle = COLORS.keyHighlight;
        ctx.fillRect(x, pianoTop, keyW - 1, kh);
      }
    }
  }

  function drawNoteBlock(x, y, nw, nh, color, glow) {
    const top = y - nh;
    const grad = ctx.createLinearGradient(x, top, x, top + nh);
    grad.addColorStop(0, color);
    grad.addColorStop(1, shade(color, -30));
    ctx.fillStyle = grad;
    roundRect(x, top, nw, nh, Math.min(6, nw * 0.25));
    ctx.fill();
    if (glow) {
      ctx.strokeStyle = glow;
      ctx.lineWidth = 2;
      ctx.stroke();
    }
  }

  function shade(hex, amount) {
    const n = parseInt(hex.slice(1), 16);
    let r = (n >> 16) + amount;
    let g = ((n >> 8) & 0xff) + amount;
    let b = (n & 0xff) + amount;
    r = Math.max(0, Math.min(255, r));
    g = Math.max(0, Math.min(255, g));
    b = Math.max(0, Math.min(255, b));
    return `rgb(${r},${g},${b})`;
  }

  function drawNote(note, playY, w, minP, maxP, playbackTime) {
    const y = noteY(note, playY, playbackTime);
    const nh = Math.max(8, note.duration * pixelsPerSecond());
    if (y + nh < -40 || y - nh > canvas.clientHeight + 40) return;

    const x = pitchX(note.pitch, w, minP, maxP);
    const nw = noteWidth(w, minP, maxP);
    const isGuide = note.index >= 0;
    const isCurrent = isGuide && note.index === state.currentGuideIndex;
    const isNext = isGuide && note.index === state.currentGuideIndex + 1;

    let color = COLORS.background;
    if (isGuide) {
      if (isCurrent) color = COLORS.guideCurrent;
      else if (isNext) color = COLORS.guideNext;
      else color = COLORS.guide;
    }

    drawNoteBlock(x, y, nw, nh, color, isCurrent ? COLORS.guideGlow : (isNext ? COLORS.guideNextGlow : null));
  }

  function drawFrame() {
    const w = canvas.clientWidth;
    const h = canvas.clientHeight;
    if (w < 10 || h < 10) return;

    const pianoH = h * PIANO_HEIGHT_RATIO;
    const pianoTop = h - pianoH;
    const playY = pianoTop - 12;
    const minP = state.minPitch;
    const maxP = state.maxPitch;
    const t = getPlaybackTime();

    ctx.fillStyle = COLORS.bg;
    ctx.fillRect(0, 0, w, h);

    drawGrid(w, playY, minP, maxP);

    const t0 = t - 1.5;
    const t1 = t + BG_VISIBLE_SEC;

    for (let i = 0; i < state.backgroundNotes.length; i++) {
      const n = state.backgroundNotes[i];
      if (n.startTime + n.duration < t0 || n.startTime > t1) continue;
      drawNote(n, playY, w, minP, maxP, t);
    }

    for (let i = 0; i < state.guideNotes.length; i++) {
      const n = state.guideNotes[i];
      if (n.startTime + n.duration < t0 || n.startTime > t1) continue;
      drawNote({ ...n, index: i }, playY, w, minP, maxP, t);
    }

    ctx.strokeStyle = COLORS.playLine;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(0, playY);
    ctx.lineTo(w, playY);
    ctx.stroke();

    drawPianoWellMask(w, h, pianoTop);
    drawPiano(w, h, pianoTop, minP, maxP);
  }

  function updateJudgmentBanner() {
    if (!judgmentEl) return;

    const text = state.judgmentText || '';
    const subtext = state.judgmentSubtext || '';
    const kind = state.judgmentKind || '';
    judgmentEl.classList.remove('hit', 'miss', 'countdown', 'hidden');

    if (!text || !kind) {
      if (judgmentMainEl) judgmentMainEl.textContent = '';
      if (judgmentSubEl) judgmentSubEl.textContent = '';
      judgmentEl.classList.add('hidden');
      return;
    }

    if (judgmentMainEl) judgmentMainEl.textContent = text;
    if (judgmentSubEl) judgmentSubEl.textContent = subtext;
    judgmentEl.classList.add(kind);
  }

  function updateHomeRowHint() {
    const key = (state.pressableKeyIndices || []).join(',');
    if (!hintDirty && key === lastHintKey) return;
    lastHintKey = key;
    hintDirty = false;

    const pressable = new Set(state.pressableKeyIndices || []);
    hintEl.innerHTML = '';

    HOME_LABELS.forEach((label, i) => {
      const el = document.createElement('span');
      el.className = 'key-cap';
      el.textContent = label;
      if (pressable.has(i)) el.classList.add('pressable');
      hintEl.appendChild(el);
    });
  }

  function applyMessage(msg) {
    if (!msg || !msg.type) return;
    if (msg.type === 'init') {
      state.guideNotes = msg.guideNotes || [];
      state.backgroundNotes = msg.backgroundNotes || [];
      state.mode = msg.mode || 'flow';
      state.minPitch = msg.minPitch ?? 48;
      state.maxPitch = msg.maxPitch ?? 72;
      state.isRhythmMode = state.mode === 'rhythm';
      state.scrollSpeed = msg.scrollSpeed > 0 ? msg.scrollSpeed : 1;
      applyTheme(msg.theme);
      state.judgmentText = '';
      state.judgmentSubtext = '';
      state.judgmentKind = '';
      hintDirty = true;
    } else if (msg.type === 'update') {
      state.currentGuideIndex = msg.currentGuideIndex ?? 0;
      state.playbackTime = msg.playbackTime ?? 0;
      state.isRhythmMode = !!msg.isRhythmMode;
      state.highlightPitch = msg.highlightPitch ?? null;
      state.freezePlayback = !!msg.freezePlayback;
      const nextPressable = msg.pressableKeyIndices || [];
      const pressableKey = nextPressable.join(',');
      if (pressableKey !== (state.pressableKeyIndices || []).join(',')) {
        state.pressableKeyIndices = nextPressable;
        hintDirty = true;
      }
      state.judgmentText = msg.judgmentText || '';
      state.judgmentSubtext = msg.judgmentSubtext || '';
      state.judgmentKind = msg.judgmentKind || '';
      syncPlaybackTime = state.playbackTime;
      syncWallMs = state.freezePlayback ? 0 : performance.now();
    }
    markDirty();
    updateJudgmentBanner();
    updateHomeRowHint();
  }

  function loop() {
    if (!state.freezePlayback) markDirty();
    else if (dirty) {
      drawFrame();
      dirty = false;
    }
    if (dirty) {
      drawFrame();
      dirty = false;
    }
    requestAnimationFrame(loop);
  }

  window.piaRoll = { onMessage: applyMessage };

  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', e => {
      applyMessage(typeof e.data === 'string' ? JSON.parse(e.data) : e.data);
    });
  }

  updateJudgmentBanner();
  updateHomeRowHint();
  requestAnimationFrame(loop);
})();
