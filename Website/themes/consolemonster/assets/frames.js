// ConsoleMonster frames: the tier switch on the widget gallery, width controls on the tables page and the glue that mounts
// ConsoleMonster's own replay player. Every frame comes from Website/tools/SiteFrames; this file only swaps pre-rendered
// frames and hands recorded ones to the engine's player.
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
        if (key !== state.color + '-' + state.glyphs) return;
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
        if (key !== state.color + '-' + state.glyphs) return;
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
    var selectedWidth;
    var options = radios(group, function (option) {
      var width = option.getAttribute('data-width');
      selectedWidth = width;
      load(base + width + '.json').then(function (bundle) {
        if (width !== selectedWidth) return;
        scope.querySelectorAll('[data-cm-frame]').forEach(function (holder) {
          var frame = bundle[holder.getAttribute('data-cm-frame')];
          var win = holder.querySelector('.cm-window');
          if (!frame || !win) return;
          show(win, frame);
          chips(win, [frame.w + '×' + frame.h]);
        });
        if (output) output.textContent = option.getAttribute('data-note') || '';
      }, function () {
        if (width === selectedWidth && output) output.textContent = 'This width could not be loaded. The frames are unchanged.';
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

  // Replays play in ConsoleMonster's own player (window.ConsoleMonsterPlayer from /assets/player/player.js, copied from
  // the engine at build time); the data is the engine's player format. The player redraws only changed rows, keeps its
  // clock to once a second, pauses off-screen and in hidden tabs, autoplays one player per page and respects reduced
  // motion. This glue only does what is site-specific: it mounts a replay when it comes near the viewport (so pages
  // fetch only the replays a reader reaches), picks the phone rendering when the narrow poster is the one showing,
  // starts at the poster's moment, and puts the player's screen in the site's window where the poster was.
  function mountReplay(replay) {
    if (replay.cmMounting || !window.ConsoleMonsterPlayer) return;
    replay.cmMounting = true;
    var posters = Array.prototype.slice.call(replay.querySelectorAll('pre.cm-screen'));
    var poster = posters.filter(function (p) { return getComputedStyle(p).display !== 'none'; })[0] || posters[0];
    var narrow = !!(poster && poster.classList.contains('cm-screen--narrow') && replay.hasAttribute('data-compact'));
    var url = narrow ? replay.getAttribute('data-compact') : replay.getAttribute('data-cm-replay');
    var start = Number(replay.getAttribute(narrow ? 'data-compact-start' : 'data-start')) || 0;
    load(url).then(function (data) {
      if (replay.cmCancelled) return;
      if (replay.hasAttribute('data-autoplay')) replay.setAttribute('data-cm-autoplay', '');
      if (replay.hasAttribute('data-loop')) replay.setAttribute('data-cm-loop', '');
      var player = window.ConsoleMonsterPlayer.mount(replay, data);
      if (!player) return;
      if (start) player.seek(start);
      if (replay.hasAttribute('data-cm-play')) player.play();
      var screen = replay.querySelector('.cm-player > pre.cm-screen');
      if (screen && poster) {
        screen.className = poster.className.replace(/\s*cm-screen--(wide|narrow)\b/g, '');
        screen.style.cssText = poster.style.cssText;
        screen.style.setProperty('--cols', data.width);
        poster.parentNode.insertBefore(screen, poster);
        posters.forEach(function (p) { p.parentNode.removeChild(p); });
      }
      replay.classList.add('is-mounted');
    }, function () { replay.cmMounting = false; });
  }
  var replays = Array.prototype.slice.call(document.querySelectorAll('[data-cm-replay]'));
  var phone = window.matchMedia('(max-width: 700px)');
  replays.forEach(function (replay) {
    if (!replay.hasAttribute('data-compact')) return;
    var template = replay.cloneNode(true);
    var current = replay;
    phone.addEventListener('change', function () {
      current.cmCancelled = true;
      if (current.__cmPlayer) current.__cmPlayer.destroy();
      var replacement = template.cloneNode(true);
      current.parentNode.replaceChild(replacement, current);
      current = replacement;
      mountReplay(current);
    });
  });
  if (replays.length && 'IntersectionObserver' in window) {
    var near = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        near.unobserve(entry.target);
        mountReplay(entry.target);
      });
    }, { rootMargin: '600px 0px' });
    replays.forEach(function (replay) { near.observe(replay); });
  } else {
    replays.forEach(mountReplay);
  }
})();
