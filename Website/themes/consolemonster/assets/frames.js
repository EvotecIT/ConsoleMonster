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
    radios(group, function (option) {
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
  });

  // Replays. A timeline is { w, h, duration, frames: [{ t, rows: [[row, html], ...] }] }: the first frame lists every
  // row, later frames only the rows that changed. Without JavaScript the page shows the final frame.
  var reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
  function format(ms) {
    var s = Math.max(0, Math.round(ms / 1000));
    return Math.floor(s / 60) + ':' + ('0' + (s % 60)).slice(-2);
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
    var timeline = null;
    var rows = [];
    var index = -1;
    var playing = false;
    var started = 0;
    var offset = 0;
    var raf = 0;
    var hold = 0;
    var loop = player.hasAttribute('data-loop');

    var paint = function () { pre.innerHTML = rows.join('\n'); };
    var seekIndex = function (target) {
      if (target < index) { index = -1; rows = []; }
      for (var i = index + 1; i <= target; i++) {
        timeline.frames[i].rows.forEach(function (row) { rows[row[0]] = row[1]; });
      }
      index = target;
      paint();
    };
    var frameAt = function (ms) {
      var lo = 0, hi = timeline.frames.length - 1;
      while (lo < hi) {
        var mid = (lo + hi + 1) >> 1;
        if (timeline.frames[mid].t <= ms) lo = mid; else hi = mid - 1;
      }
      return lo;
    };
    var setTime = function (ms) {
      seekIndex(frameAt(ms));
      scrub.value = String(Math.round(ms));
      scrub.setAttribute('aria-valuetext', format(ms) + ' of ' + format(timeline.duration));
      if (clock) clock.textContent = format(ms) + ' / ' + format(timeline.duration);
    };
    var setPlaying = function (on) {
      playing = on;
      button.setAttribute('aria-pressed', on ? 'true' : 'false');
      button.setAttribute('aria-label', on ? 'Pause the replay' : 'Play the replay');
      player.classList.toggle('is-playing', on);
      cancelAnimationFrame(raf);
      clearTimeout(hold);
      if (on) {
        if (offset >= timeline.duration) offset = 0;
        started = performance.now() - offset;
        raf = requestAnimationFrame(tick);
      }
    };
    var tick = function (now) {
      offset = now - started;
      if (offset >= timeline.duration) {
        setTime(timeline.duration);
        if (loop && !reduced.matches) {
          hold = setTimeout(function () { if (playing) { offset = 0; started = performance.now(); raf = requestAnimationFrame(tick); } }, 2400);
          return;
        }
        offset = timeline.duration;
        setPlaying(false);
        return;
      }
      setTime(offset);
      raf = requestAnimationFrame(tick);
    };
    var ready = function () {
      if (timeline) return Promise.resolve(timeline);
      return load(url).then(function (data) {
        timeline = data;
        pre.style.setProperty('--cols', data.w);
        var win = player.querySelector('.cm-window');
        if (win) chips(win, [data.w + '×' + data.h]);
        scrub.max = String(data.duration);
        scrub.disabled = false;
        return data;
      });
    };

    button.addEventListener('click', function () {
      ready().then(function () {
        if (!playing && index < 0) { offset = 0; setTime(0); }
        setPlaying(!playing);
      }, function () { controls.hidden = true; });
    });
    scrub.addEventListener('input', function () {
      ready().then(function () {
        offset = Number(scrub.value);
        setTime(offset);
        if (playing) started = performance.now() - offset;
      });
    });
    player.addEventListener('keydown', function (event) {
      if (event.target === scrub || !timeline) return;
      if (event.key === 'k' || (event.key === ' ' && event.target === player)) { event.preventDefault(); button.click(); }
    });

    // Timelines load on demand: when the viewer plays or scrubs, or when an autoplaying player scrolls into view.
    var total = Number(scrub.max) || 0;
    if (clock) clock.textContent = format(total) + ' / ' + format(total);
    scrub.disabled = false;
    if (player.hasAttribute('data-autoplay') && !reduced.matches && 'IntersectionObserver' in window) {
      var seen = false;
      new IntersectionObserver(function (entries, observer) {
        if (seen || !entries[0].isIntersecting) return;
        seen = true;
        observer.disconnect();
        ready().then(function () {
          offset = Math.min(Number(player.getAttribute('data-start')) || 0, timeline.duration);
          setTime(offset);
          setPlaying(true);
        }, function () { controls.hidden = true; });
      }, { threshold: 0.35 }).observe(player);
    }    document.addEventListener('visibilitychange', function () { if (document.hidden && playing) setPlaying(false); });
    document.addEventListener('cm:tab', function () { if (playing && player.closest('[hidden]')) setPlaying(false); });
  });
})();
