// ConsoleMonster frames: the tier switch on the widget gallery, width controls on the tables page and the glue that mounts
// ConsoleMonster's own replay player. Every frame comes from Website/tools/SiteFrames; this file only swaps pre-rendered
// frames and hands recorded ones to the engine's player.
(function () {
  'use strict';

  // Frame bundles and replays are fetched by fixed URLs, which asset hashing cannot rename: the page's content version
  // (meta cm-runtime, from the site tool) goes on each request, so a new page never meets an old cached bundle.
  var runtime = document.querySelector('meta[name="cm-runtime"]');
  var version = runtime ? runtime.getAttribute('content') : '';
  var bundles = {};
  function load(url) {
    if (!bundles[url]) {
      bundles[url] = fetch(version ? url + (url.indexOf('?') < 0 ? '?' : '&') + 'v=' + version : url, { credentials: 'same-origin' }).then(function (response) {
        if (!response.ok) throw new Error(response.status + ' ' + url);
        return response.json();
      });
      bundles[url].catch(function () { delete bundles[url]; });
    }
    return bundles[url];
  }

  // Bundles the reader is likely to ask for next are fetched while the browser is idle, so switching feels instant.
  function idle(fn, timeout) {
    if (window.requestIdleCallback) window.requestIdleCallback(fn, { timeout: timeout || 4000 });
    else setTimeout(fn, timeout ? 16 : 1200);
  }

  // Puts a frame ({ w, h, dark, light }) into the screens inside a window.
  function show(win, frame) {
    win.querySelectorAll('pre.cm-screen').forEach(function (pre) {
      var light = pre.classList.contains('cm-screen--light');
      pre.innerHTML = light ? (frame.light || frame.dark) : frame.dark;
      if (pre.style.getPropertyValue('--cols') !== String(frame.w)) pre.style.setProperty('--cols', frame.w);
    });
  }

  function text(node, value) { if (node && value != null && node.textContent !== String(value)) node.textContent = value; }

  function chips(win, values) {
    var list = win.querySelectorAll('.cm-window__meta .cm-chip');
    values.forEach(function (value, index) { text(list[index], value); });
  }

  // Swaps many frames without stalling the page: the windows on or near the screen change in this frame, the rest in
  // idle time a few at a time (they skip rendering while off screen anyway). A newer swap from the same control cancels
  // the rest of its older one. Every position is read before anything is written, so the writes cost one layout.
  function swap(control, jobs) {
    if (control.cmPending) control.cmPending.cancelled = true;
    var batch = control.cmPending = { cancelled: false };
    var height = window.innerHeight || document.documentElement.clientHeight;
    var near = [], later = [];
    jobs.forEach(function (job) {
      var box = job.el.getBoundingClientRect();
      (box.bottom > -height / 2 && box.top < height * 1.5 ? near : later).push(job);
    });
    near.forEach(function (job) { job.run(); });
    var done = function () { if (control.cmPending === batch) control.cmPending = null; };
    var next = function (deadline) {
      if (batch.cancelled) return;
      do { var job = later.shift(); job.run(); markScrollable(job.el); } while (later.length && deadline && deadline.timeRemaining && deadline.timeRemaining() > 4);
      if (later.length) idle(next, 250); else done();
    };
    if (later.length) idle(next, 250); else done();
    // The scroll hints are read after the browser has laid out the new frames, not in the middle of writing them.
    requestAnimationFrame(function () { setTimeout(markScrollable, 0); });
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
        var colourLabel = labels.color[state.color], glyphLabel = labels.glyphs[state.glyphs];
        var jobs = [];
        document.querySelectorAll('[data-cm-widget]').forEach(function (article) {
          var frame = bundle[article.getAttribute('data-cm-widget')];
          var win = article.querySelector('.cm-window');
          if (!frame || !win) return;
          jobs.push({ el: article, run: function () {
            show(win, frame);
            chips(win, [frame.w + '×' + frame.h, colourLabel, glyphLabel]);
            text(article.querySelector('[data-cm-size]'), frame.w + ' × ' + frame.h);
          } });
        });
        swap(tiers, jobs);
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
        var jobs = [];
        scope.querySelectorAll('[data-cm-frame]').forEach(function (holder) {
          var frame = bundle[holder.getAttribute('data-cm-frame')];
          var win = holder.querySelector('.cm-window');
          if (!frame || !win) return;
          jobs.push({ el: holder, run: function () { show(win, frame); chips(win, [frame.w + '×' + frame.h]); } });
        });
        swap(control, jobs);
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
      // The player opens at the poster's moment, so its first frame is the one the poster showed (drawn once, not the
      // first frame and then a seek), and autoplay still applies.
      var player = window.ConsoleMonsterPlayer.mount(replay, data, { start: start });
      if (!player) return;
      if (replay.hasAttribute('data-cm-play')) player.play();
      var screen = replay.querySelector('.cm-player > pre.cm-screen');
      if (screen && poster) {
        screen.className = poster.className.replace(/\s*cm-screen--(wide|narrow)\b/g, '');
        screen.style.cssText = poster.style.cssText;
        screen.style.setProperty('--cols', data.width);
        if (poster.hasAttribute('aria-label')) screen.setAttribute('aria-label', poster.getAttribute('aria-label'));
        poster.parentNode.insertBefore(screen, poster);
        posters.forEach(function (p) { p.parentNode.removeChild(p); });
      }
      replay.classList.add('is-mounted');
      markScrollable();
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
  // A frame wider than its window at the smallest font scrolls inside the window; the window says so in its bar, and so does
  // a code block with long lines. Those that skip rendering off screen are measured when they come near (measuring them now
  // would render them all).
  // Deferred only where the browser also says when a skipped window comes near; otherwise every window and code block is measured now.
  var deferred = !!(Element.prototype.checkVisibility && ('oncontentvisibilityautostatechange' in window));
  function markScrollable(root) {
    var scope = root && root.querySelectorAll ? root : document;
    scope.querySelectorAll('.cm-window').forEach(function (win) {
      if (deferred && !win.checkVisibility({ contentVisibilityAuto: true })) return;
      var body = win.querySelector('.cm-window__body');
      if (body) win.classList.toggle('is-scrollable', body.scrollWidth > body.clientWidth + 2);
    });
    // Code blocks too: one whose lines run past its width says so in its bar.
    scope.querySelectorAll('.cm-code').forEach(function (block) {
      // The whole program when it is open (the short sample is hidden then), otherwise the sample.
      var pre = block.querySelector(':scope > .cm-code__full[open] > pre') || block.querySelector(':scope > pre');
      if (!pre || (deferred && !pre.checkVisibility({ contentVisibilityAuto: true }))) return;
      block.classList.toggle('is-wide', pre.scrollWidth > pre.clientWidth + 2);
    });
  }
  document.addEventListener('contentvisibilityautostatechange', function (event) {
    if (!event.skipped) markScrollable(event.target);
  }, true);
  // Side-by-side figures: all frames in one row when each fits at a readable size, otherwise all stacked.
  function layoutStrips() {
    document.querySelectorAll('.cm-figure--strip').forEach(function (figure) {
      var frames = figure.querySelector('.cm-figure__frames');
      if (!frames) return;
      var count = frames.children.length;
      var cols = parseFloat(getComputedStyle(figure).getPropertyValue('--cm-strip-cols')) || 40;
      var need = count * (cols * 7.2 + 34) + (count - 1) * 14;
      frames.style.setProperty('--cm-strip-count', count);
      frames.classList.toggle('is-row', frames.clientWidth >= need);
    });
  }
  var hintTimer = 0;
  var hint = function () { layoutStrips(); markScrollable(); };
  window.addEventListener('resize', function () { clearTimeout(hintTimer); hintTimer = setTimeout(hint, 150); });
  window.addEventListener('load', hint);
  document.addEventListener('cm:tab', hint);
  document.addEventListener('toggle', function (event) { if (event.target.matches && event.target.matches('.cm-code__full')) hint(); }, true);
  if (document.fonts && document.fonts.ready) document.fonts.ready.then(hint);
  hint();
})();
