// ConsoleMonster frames: the tier switch on the widget gallery, width controls on the tables page and the replay player.
// Every frame comes from Website/tools/SiteFrames; this file only swaps pre-rendered frames and plays recorded ones.
(function () {
  'use strict';

  var bundles = {};
  function load(url) {
    if (!bundles[url]) {
      bundles[url] = fetch(url, { credentials: 'same-origin' }).then(function (response) {
        if (!response.ok) throw new Error(response.status + ' ' + url);
        return response.json();
      });
      bundles[url].catch(function () { delete bundles[url]; });
    }
    return bundles[url];
  }

  // Bundles the reader is likely to ask for next are fetched while the browser is idle, so switching feels instant.
  function idle(fn) { (window.requestIdleCallback || function (f) { return setTimeout(f, 1200); })(fn, { timeout: 4000 }); }

  // Puts a frame ({ w, h, dark, light }) into the screens inside a window.
  function show(win, frame) {
    win.querySelectorAll('pre.cm-screen').forEach(function (pre) {
      var light = pre.classList.contains('cm-screen--light');
      pre.innerHTML = light ? (frame.light || frame.dark) : frame.dark;
      pre.style.setProperty('--cols', frame.w);
    });
  }

  function chips(win, values) {
    var list = win.querySelectorAll('.cm-window__meta .cm-chip');
    values.forEach(function (value, index) { if (list[index] && value != null) list[index].textContent = value; });
  }

  // Radio groups made of .cm-seg buttons: arrows move and select, the chosen option has aria-checked.
  function radios(group, onChange) {
    var options = Array.prototype.slice.call(group.querySelectorAll('[role="radio"]'));
    var choose = function (option, focus) {
      options.forEach(function (other) {
        var on = other === option;
        other.setAttribute('aria-checked', on ? 'true' : 'false');
        other.tabIndex = on ? 0 : -1;
      });
      if (focus) option.focus();
      onChange(option);
    };
    options.forEach(function (option, index) {
      option.tabIndex = option.getAttribute('aria-checked') === 'true' ? 0 : -1;
      option.addEventListener('click', function () { choose(option, false); });
      option.addEventListener('keydown', function (event) {
        var step = /^(ArrowRight|ArrowDown)$/.test(event.key) ? 1 : /^(ArrowLeft|ArrowUp)$/.test(event.key) ? -1 : 0;
        if (!step) return;
        event.preventDefault();
        choose(options[(index + step + options.length) % options.length], true);
      });
    });
    return options;
  }

  // The widget gallery's terminal profile: colour tier and glyph tier, swapping every specimen at once.
  var tiers = document.querySelector('[data-cm-tiers]');
  if (tiers) {
    tiers.hidden = false;
    var state = { color: '24bit', glyphs: 'full' };
    var labels = { color: {}, glyphs: {} };
    var note = tiers.querySelector('[data-cm-tier-note]');
    var apply = function () {
      var key = state.color + '-' + state.glyphs;
      tiers.setAttribute('aria-busy', 'true');
      load('/frames/widgets/' + key + '.json').then(function (bundle) {
        document.querySelectorAll('[data-cm-widget]').forEach(function (article) {
          var frame = bundle[article.getAttribute('data-cm-widget')];
          var win = article.querySelector('.cm-window');
          if (!frame || !win) return;
          show(win, frame);
          chips(win, [frame.w + '×' + frame.h, labels.color[state.color], labels.glyphs[state.glyphs]]);
          var size = article.querySelector('[data-cm-size]');
          if (size) size.textContent = frame.w + ' × ' + frame.h;
        });
        var colour = state.color === 'none' ? 'no colour' : labels.color[state.color] + ' colour';
        if (note) note.textContent = 'Every frame below is drawn for ' + colour + ' and the ' + labels.glyphs[state.glyphs].toLowerCase() + ' glyph set.';
        tiers.removeAttribute('aria-busy');
      }, function () {
        if (note) note.textContent = 'This tier could not be loaded. The frames below are unchanged.';
        tiers.removeAttribute('aria-busy');
      });
    };
    idle(function () {
      tiers.querySelectorAll('[data-color]').forEach(function (c) {
        tiers.querySelectorAll('[data-glyphs]').forEach(function (g) { load('/frames/widgets/' + c.getAttribute('data-color') + '-' + g.getAttribute('data-glyphs') + '.json'); });
      });
    });
    tiers.querySelectorAll('[role="radiogroup"]').forEach(function (group) {
      var kind = group.querySelector('[data-color]') ? 'color' : 'glyphs';
      group.querySelectorAll('[role="radio"]').forEach(function (option) {
        labels[kind][option.getAttribute('data-' + kind)] = option.textContent.trim();
      });
      radios(group, function (option) {
        var value = option.getAttribute('data-' + kind);
        if (state[kind] === value) return;
        state[kind] = value;
        apply();
      });
    });
  }

  // Width controls: [data-cm-widths="/frames/tables/"] with radio buttons carrying data-width; every [data-cm-frame]
  // window under the same section shows that width's rendering.
  document.querySelectorAll('[data-cm-widths]').forEach(function (control) {
    control.hidden = false;
    var base = control.getAttribute('data-cm-widths');
    var scope = control.closest('[data-cm-width-scope]') || document;
    var group = control.querySelector('[role="radiogroup"]');
    var output = control.querySelector('[data-cm-width-note]');
    idle(function () { control.querySelectorAll('[data-width]').forEach(function (b) { load(base + b.getAttribute('data-width') + '.json'); }); });
    var options = radios(group, function (option) {
      var width = option.getAttribute('data-width');
      load(base + width + '.json').then(function (bundle) {
        scope.querySelectorAll('[data-cm-frame]').forEach(function (holder) {
          var frame = bundle[holder.getAttribute('data-cm-frame')];
          var win = holder.querySelector('.cm-window');
          if (!frame || !win) return;
          show(win, frame);
          chips(win, [frame.w + '×' + frame.h]);
        });
        if (output) output.textContent = option.getAttribute('data-note') || '';
      });
    });
    // On a phone the page opens at the widest rendering that fits the screen at the 10 px floor, so the first tables
    // show what a console that narrow prints instead of scrolling sideways.
    if (window.matchMedia('(max-width: 560px)').matches) {
      var fits = Math.floor((document.documentElement.clientWidth - 64) / 6);
      var pick = null;
      options.forEach(function (option) {
        var width = Number(option.getAttribute('data-width'));
        if (width <= fits && (!pick || width > Number(pick.getAttribute('data-width')))) pick = option;
      });
      if (pick && pick.getAttribute('aria-checked') !== 'true') pick.click();
    }
  });

  // Replays. A timeline is { w, h, duration, frames: [{ t, rows: [[row, html], ...] }] }: the first frame lists every
  // row, later frames only the rows that changed. Without JavaScript the page shows the poster frame.
  // A replay keeps one element per row and rewrites only the rows a frame changes. It wakes when the next recorded
  // frame is due (or the clock's next second), not on every display frame, and only one replay runs at a time: the one
  // the viewer started, while it is in view, or else the first autoplaying replay in view.
  var reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
  var observe = 'IntersectionObserver' in window;
  var players = [];
  var chosen = null;
  function format(ms) {
    var s = Math.max(0, Math.floor(ms / 1000));
    return Math.floor(s / 60) + ':' + ('0' + (s % 60)).slice(-2);
  }
  function schedule() {
    var run = null;
    if (!document.hidden) {
      if (chosen && chosen.intent === 'on' && chosen.visible()) run = chosen;
      for (var i = 0; !run && i < players.length; i++) {
        if (players[i].intent === 'auto' && players[i].visible()) run = players[i];
      }
    }
    players.forEach(function (p) { if (p !== run) p.suspend(); });
    if (run) run.resume();
  }

  document.querySelectorAll('[data-cm-player]').forEach(function (player) {
    var compactUrl = player.getAttribute('data-compact');
    var controls = player.querySelector('.cm-player__controls');
    var button = player.querySelector('[data-cm-play]');
    var scrub = player.querySelector('[data-cm-scrub]');
    var clock = player.querySelector('[data-cm-clock]');
    // With a narrow rendering beside the wide one, the player draws into whichever the layout shows.
    var screens = Array.prototype.slice.call(player.querySelectorAll('pre.cm-screen'));
    var pre = screens.filter(function (s) { return getComputedStyle(s).display !== 'none'; })[0] || screens[0];
    var url = compactUrl && pre && pre.classList.contains('cm-screen--narrow') ? compactUrl : player.getAttribute('data-cm-player');
    if (!controls || !button || !scrub || !pre) return;
    controls.hidden = false;
    var narrow = url === compactUrl;
    var start = Number(player.getAttribute(narrow ? 'data-compact-start' : 'data-start')) || 0;
    var loopStart = Number(player.getAttribute(narrow ? 'data-compact-loop-start' : 'data-loop-start')) || 0;
    var loop = player.hasAttribute('data-loop');
    var timeline = null;
    var rows = [];
    var cells = null;
    var shown = [];
    var index = -1;
    var running = false;
    var wanted = false;
    var started = 0;
    var offset = 0;
    var timer = 0;
    var second = -1;
    var scrubbed = -1;
    var pixel = 1;
    var self = {
      intent: player.hasAttribute('data-autoplay') && !reduced.matches && observe ? 'auto' : 'off',
      // A replay the viewer started keeps running while any of it shows; an autoplaying one needs 30% in view.
      ratio: observe ? 0 : 1,
      visible: function () { return (self.intent === 'on' ? self.ratio > 0 : self.ratio >= 0.3) && !player.closest('[hidden]'); },
      resume: function () {
        if (running || wanted) return;
        wanted = true;
        ready().then(function () {
          if (!wanted || running) return;
          if (index < 0) { offset = Math.min(start, timeline.duration); setTime(offset, true); }
          if (offset >= timeline.duration) offset = loopStart;
          setRunning(true);
        }, function () { controls.hidden = true; wanted = false; });
      },
      suspend: function () { wanted = false; if (running) setRunning(false); }
    };

    var grow = function (count) {
      if (!cells) { cells = []; pre.textContent = ''; }
      while (cells.length < count) {
        var row = document.createElement('span');
        row.className = 'cm-row';
        pre.appendChild(row);
        cells.push(row);
      }
    };
    var draw = function (row) {
      var html = rows[row] || '';
      if (shown[row] !== html) { cells[row].innerHTML = html; shown[row] = html; }
    };
    var seekIndex = function (target) {
      if (target === index) return;
      var dirty = target > index && cells ? {} : null;
      if (target < index) { index = -1; rows = []; }
      for (var i = index + 1; i <= target; i++) {
        timeline.frames[i].rows.forEach(function (row) { rows[row[0]] = row[1]; if (dirty) dirty[row[0]] = true; });
      }
      index = target;
      grow(Math.max(timeline.h || 0, rows.length));
      if (dirty) { for (var key in dirty) draw(Number(key)); } else { for (var r = 0; r < cells.length; r++) draw(r); }
    };
    var frameAt = function (ms) {
      var lo = 0, hi = timeline.frames.length - 1;
      while (lo < hi) {
        var mid = (lo + hi + 1) >> 1;
        if (timeline.frames[mid].t <= ms) lo = mid; else hi = mid - 1;
      }
      return lo;
    };
    // The clock and the accessible value change once a second; the scrub thumb when it would move by a pixel.
    var setTime = function (ms, moveThumb) {
      seekIndex(frameAt(ms));
      if (moveThumb || Math.abs(ms - scrubbed) >= pixel) { scrub.value = String(Math.round(ms)); scrubbed = ms; }
      var s = Math.floor(ms / 1000);
      if (s !== second) {
        second = s;
        var at = format(ms), of = format(timeline.duration);
        scrub.setAttribute('aria-valuetext', at + ' of ' + of);
        if (clock) clock.textContent = at + ' / ' + of;
      }
    };
    var wake = function () {
      var next = index + 1 < timeline.frames.length ? timeline.frames[index + 1].t : timeline.duration;
      var due = Math.min(next, (Math.floor(offset / 1000) + 1) * 1000, timeline.duration);
      timer = setTimeout(step, Math.max(40, due - offset));
    };
    var step = function () {
      timer = 0;
      offset = performance.now() - started;
      if (offset < timeline.duration) { setTime(offset); wake(); return; }
      setTime(timeline.duration, true);
      if (loop && !reduced.matches) {
        timer = setTimeout(function () {
          timer = 0;
          offset = loopStart;
          started = performance.now() - loopStart;
          setTime(loopStart, true);
          wake();
        }, 2400);
        return;
      }
      offset = timeline.duration;
      self.intent = 'off';
      setRunning(false);
      schedule();
    };
    var setRunning = function (on) {
      running = on;
      wanted = on;
      button.setAttribute('aria-pressed', on ? 'true' : 'false');
      button.setAttribute('aria-label', on ? 'Pause the replay' : 'Play the replay');
      player.classList.toggle('is-playing', on);
      clearTimeout(timer);
      timer = 0;
      if (on) {
        pixel = timeline.duration / Math.max(60, scrub.clientWidth || 300);
        started = performance.now() - offset;
        wake();
      }
    };
    var ready = function () {
      if (timeline) return Promise.resolve(timeline);
      return load(url).then(function (data) {
        timeline = data;
        pre.style.setProperty('--cols', data.w);
        scrub.max = String(data.duration);
        scrub.disabled = false;
        return data;
      });
    };

    button.addEventListener('click', function () {
      ready().then(function () {
        if (running) {
          self.intent = 'off';
          if (chosen === self) chosen = null;
        } else {
          players.forEach(function (p) { if (p !== self && p.intent === 'on') p.intent = 'off'; });
          self.intent = 'on';
          chosen = self;
          if (index < 0) { offset = Math.min(start, timeline.duration); setTime(offset, true); }
        }
        schedule();
      }, function () { controls.hidden = true; });
    });
    scrub.addEventListener('input', function () {
      ready().then(function () {
        offset = Number(scrub.value);
        scrubbed = offset;
        setTime(offset);
        if (running) { clearTimeout(timer); started = performance.now() - offset; wake(); }
      });
    });
    player.addEventListener('keydown', function (event) {
      if (event.target === scrub || !timeline) return;
      if (event.key === 'k' || (event.key === ' ' && event.target === player)) { event.preventDefault(); button.click(); }
    });

    // Timelines load on demand: when the viewer plays or scrubs, or when an autoplaying replay comes into view.
    var total = Number(scrub.max) || 0;
    offset = Math.min(start, total);
    scrub.value = String(offset);
    if (clock) clock.textContent = format(offset) + ' / ' + format(total);
    scrub.disabled = false;
    players.push(self);
    if (observe) {
      new IntersectionObserver(function (entries) {
        var entry = entries[entries.length - 1];
        self.ratio = entry.isIntersecting ? Math.max(entry.intersectionRatio, 0.001) : 0;
        schedule();
      }, { threshold: [0, 0.3, 0.6] }).observe(player);
    }
  });

  if (players.length) {
    document.addEventListener('visibilitychange', schedule);
    document.addEventListener('cm:tab', schedule);
    var calm = function () {
      if (!reduced.matches) return;
      players.forEach(function (p) { if (p.intent === 'auto') p.intent = 'off'; });
      schedule();
    };
    if (reduced.addEventListener) reduced.addEventListener('change', calm); else if (reduced.addListener) reduced.addListener(calm);
  }
})();