// ConsoleMonster site behaviour: the theme switch, header navigation, code blocks (copy and highlighting) and tabs.
// Frames, tier switches, width controls and replays live in frames.js.
(function () {
  'use strict';

  var root = document.documentElement;

  document.querySelectorAll('[data-cm-theme-toggle]').forEach(function (button) {
    button.addEventListener('click', function () {
      var next = root.dataset.theme === 'dark' ? 'light' : 'dark';
      root.dataset.theme = next;
      try { localStorage.setItem('cm-theme', next); } catch (e) { /* storage unavailable */ }
    });
  });

  // Header navigation: each group button discloses its panel. On wide screens one panel is open at a time, and Escape,
  // a click outside or tabbing away closes it; arrow keys move between the destinations. The phone sheet shows every group.
  var header = document.querySelector('[data-cm-header]');
  var compact = window.matchMedia('(max-width: 960px)');
  if (header) {
    var groups = Array.prototype.slice.call(header.querySelectorAll('[data-cm-navgroup]'));
    var triggerOf = function (group) { return group.querySelector('.cm-navgroup__trigger'); };
    var setOpen = function (group, open) { triggerOf(group).setAttribute('aria-expanded', open ? 'true' : 'false'); };
    var closeAll = function (except) { groups.forEach(function (group) { if (group !== except) setOpen(group, false); }); };
    groups.forEach(function (group) {
      var trigger = triggerOf(group);
      var links = Array.prototype.slice.call(group.querySelectorAll('.cm-navitem__link'));
      trigger.addEventListener('click', function () {
        if (compact.matches) return;
        var open = trigger.getAttribute('aria-expanded') !== 'true';
        if (!compact.matches) closeAll(group);
        setOpen(group, open);
      });
      trigger.addEventListener('keydown', function (event) {
        if (event.key !== 'ArrowDown' || compact.matches || !links.length) return;
        event.preventDefault();
        closeAll(group);
        setOpen(group, true);
        links[0].focus();
      });
      group.addEventListener('keydown', function (event) {
        var index = links.indexOf(document.activeElement);
        if (event.key === 'Escape' && !compact.matches && trigger.getAttribute('aria-expanded') === 'true') {
          setOpen(group, false);
          trigger.focus();
        } else if (index >= 0 && !compact.matches && /^Arrow(Left|Right|Up|Down)$/.test(event.key)) {
          event.preventDefault();
          var step = event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 1;
          links[(index + step + links.length) % links.length].focus();
        }
      });
      group.addEventListener('focusout', function (event) {
        if (!compact.matches && event.relatedTarget && !group.contains(event.relatedTarget)) setOpen(group, false);
      });
    });
    document.addEventListener('click', function (event) {
      if (!compact.matches && !header.contains(event.target)) closeAll(null);
    });
    var reset = function () { groups.forEach(function (group) {
      setOpen(group, compact.matches);
      triggerOf(group).disabled = compact.matches;
    }); };
    if (compact.addEventListener) compact.addEventListener('change', reset);
    reset();
    var toggle = document.getElementById('cm-nav-toggle');
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && toggle && toggle.checked) { toggle.checked = false; toggle.focus(); }
    });
  }

  // Syntax highlighting with the self-hosted Prism, loaded the first time a page has code. Each block's grammar and the
  // grammars it extends are loaded explicitly, in order, before the block is highlighted (the autoloader's deferred
  // re-highlight is not relied on: it never ran for C#, whose grammar first needs clike).
  var PRISM = '/assets/prism/';
  var ALIAS = { cs: 'csharp', 'c#': 'csharp', ps1: 'powershell', pwsh: 'powershell', sh: 'bash', shell: 'bash', html: 'markup', xml: 'markup', yml: 'yaml' };
  var NEEDS = { csharp: ['clike'], 'xml-doc': ['markup'] };
  var PLAIN = { text: true, none: true, plaintext: true, txt: true };
  // Prism's PowerShell grammar has no token for parameters (-Title, -Scope); ConsoleMonster's own highlighter colours
  // them as keys. Operators such as -eq and -join keep their own token.
  var EXTEND = {
    powershell: function (p) {
      if (p.languages.powershell && !p.languages.powershell.parameter) {
        p.languages.insertBefore('powershell', 'operator', {
          parameter: { pattern: /(^|[\s(\[{,;|=])-(?![ci]?(?:and|x?or|as|not(?:contains|in|like|match)?|contains|in|like|match|eq|ge|gt|le|lt|ne|is(?:not)?|band|bnot|bor|bxor|join|split|replace|f|shl|shr)\b)[a-z]\w*/i, lookbehind: true }
        });
      }
    }
  };
  var prismReady = null;
  var grammars = {};
  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var script = document.createElement('script');
      script.src = src;
      script.onload = resolve;
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }
  function prism() {
    if (!prismReady) {
      window.Prism = window.Prism || {};
      window.Prism.manual = true;
      prismReady = window.Prism.highlightElement ? Promise.resolve(window.Prism) : loadScript(PRISM + 'prism-core.js').then(function () { return window.Prism; });
    }
    return prismReady;
  }
  function grammar(lang) {
    if (!grammars[lang]) {
      grammars[lang] = prism().then(function (p) {
        if (p.languages[lang]) return p;
        return Promise.all((NEEDS[lang] || []).map(grammar)).then(function () {
          return p.languages[lang] ? p : loadScript(PRISM + 'components/prism-' + lang + '.min.js').then(function () { return p; });
        });
      });
    }
    return grammars[lang];
  }
  function highlight(code) {
    if (!code || code.dataset.cmHighlighted) return;
    var match = /\blang(?:uage)?-([\w#-]+)/.exec(code.className);
    if (!match) return;
    var lang = ALIAS[match[1].toLowerCase()] || match[1].toLowerCase();
    if (PLAIN[lang]) return;
    code.dataset.cmHighlighted = '1';
    if (lang !== match[1]) code.className = code.className.replace(match[0], 'language-' + lang);
    grammar(lang).then(function (p) {
      if (EXTEND[lang]) EXTEND[lang](p);
      // highlightElement re-reads the text, so a block another script highlighted first is redone with the extension.
      if (p.languages[lang]) p.highlightElement(code);
    }, function () { /* an unknown language stays plain, readable text */ });
  }  window.cmHighlight = highlight;

  // Markdown fences become the shared code block: a caption with the language and a copy action.
  var names = { csharp: 'C#', cs: 'C#', powershell: 'PowerShell', ps1: 'PowerShell', json: 'JSON', bash: 'Shell', text: 'Text', xml: 'XML', yaml: 'YAML' };
  document.querySelectorAll('.cm-prose pre > code').forEach(function (code) {
    var pre = code.parentElement;
    if (pre.closest('.cm-code') || pre.classList.contains('cm-screen')) return;
    var match = /language-([\w-]+)/.exec(code.className || pre.className || '');
    var lang = match ? match[1] : 'text';
    if (!match) code.classList.add('language-text');
    var figure = document.createElement('figure');
    figure.className = 'cm-code';
    figure.dataset.lang = lang;
    var bar = document.createElement('figcaption');
    bar.className = 'cm-code__bar';
    var label = document.createElement('span');
    label.className = 'cm-code__lang';
    label.textContent = names[lang] || lang;
    var copy = document.createElement('button');
    copy.type = 'button';
    copy.className = 'cm-code__copy';
    copy.setAttribute('data-cm-copy', '');
    copy.setAttribute('aria-live', 'polite');
    copy.textContent = 'Copy';
    bar.appendChild(label);
    bar.appendChild(copy);
    pre.parentNode.insertBefore(figure, pre);
    figure.appendChild(bar);
    figure.appendChild(pre);
  });
  document.querySelectorAll('pre > code[class*="language-"], pre > code[class*="lang-"]').forEach(highlight);

  document.addEventListener('click', function (event) {
    var button = event.target.closest('[data-cm-copy]');
    if (!button) return;
    var figure = button.closest('.cm-code');
    var full = figure.querySelector('.cm-code__full');
    // A shortened sample copies its full program, which is what compiles and runs.
    var code = full ? full.querySelector('pre') : figure.querySelector('pre');
    var text = code ? code.innerText.replace(/\n$/, '') : '';
    var done = function (label) { button.textContent = label; setTimeout(function () { button.textContent = 'Copy'; }, 1600); };
    // Without clipboard access (an insecure origin, a refused permission, an older browser) the code is selected, so
    // Ctrl+C or the context menu copies it; a shortened sample opens its full program first.
    var fallback = function () {
      if (full && !full.open) full.open = true;
      var selection = window.getSelection && window.getSelection();
      if (code && selection) {
        var range = document.createRange();
        range.selectNodeContents(code);
        selection.removeAllRanges();
        selection.addRange(range);
      }
      var copied = false;
      try { copied = document.execCommand && document.execCommand('copy'); } catch (e) { copied = false; }
      done(copied ? 'Copied' : 'Selected: copy with your keyboard');
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () { done('Copied'); }, fallback);
    } else {
      fallback();
    }
  });

  // Tabs: [data-cm-tabs] holds a .cm-seg with role="tab" buttons (aria-controls) and the panels they show.
  document.querySelectorAll('[data-cm-tabs]').forEach(function (set) {
    // Only this set's own tabs: a tab set can hold another (the hero's paths hold code tabs).
    var tabs = Array.prototype.slice.call(set.querySelectorAll('[role="tab"]')).filter(function (tab) { return tab.closest('[data-cm-tabs]') === set; });
    var select = function (tab, focus) {
      tabs.forEach(function (other) {
        var on = other === tab;
        other.setAttribute('aria-selected', on ? 'true' : 'false');
        other.tabIndex = on ? 0 : -1;
        var panel = document.getElementById(other.getAttribute('aria-controls'));
        if (panel) panel.hidden = !on;
      });
      if (focus) tab.focus();
      set.dispatchEvent(new CustomEvent('cm:tab', { detail: tab, bubbles: true }));
    };
    tabs.forEach(function (tab, index) {
      tab.addEventListener('click', function () { select(tab, false); });
      tab.addEventListener('keydown', function (event) {
        var step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
        if (event.key === 'Home') { event.preventDefault(); select(tabs[0], true); }
        else if (event.key === 'End') { event.preventDefault(); select(tabs[tabs.length - 1], true); }
        else if (step) { event.preventDefault(); select(tabs[(index + step + tabs.length) % tabs.length], true); }
      });
    });
  });

  // The hero's two paths (Everyday and Monster) keep the choice in the URL hash, so a link can open either. The player
  // of the chosen path plays; the other one pauses.
  document.querySelectorAll('[data-cm-paths]').forEach(function (set) {
    var tabs = Array.prototype.slice.call(set.querySelectorAll('[data-cm-path]')).filter(function (tab) { return tab.closest('[data-cm-paths]') === set; });
    var replayOf = function (tab) {
      var panel = document.getElementById(tab.getAttribute('aria-controls'));
      return panel && panel.querySelector('[data-cm-replay]');
    };
    var player = function (tab) {
      var replay = replayOf(tab);
      return replay && replay.__cmPlayer;
    };
    // A path opened from the URL is not a click: its replay asks to autoplay, and the player decides (one per page, none
    // with reduced motion). A click on the switch plays the chosen path.
    var fromUrl = false;
    var fromHash = function () {
      var id = (location.hash || '').replace('#', '');
      var tab = tabs.filter(function (t) { return t.getAttribute('data-cm-path') === id; })[0];
      if (tab && tab.getAttribute('aria-selected') !== 'true') { fromUrl = true; tab.click(); fromUrl = false; }
    };
    set.addEventListener('cm:tab', function (event) {
      var chosen = event.detail;
      if (tabs.indexOf(chosen) < 0) return;
      tabs.forEach(function (tab) {
        var p = player(tab);
        var replay = replayOf(tab);
        // A replay that has not loaded yet (its panel was hidden) starts once it mounts.
        if (!p) {
          if (replay && tab === chosen) replay.setAttribute(fromUrl ? 'data-autoplay' : 'data-cm-play', '');
          if (replay && tab !== chosen) { replay.removeAttribute('data-autoplay'); replay.removeAttribute('data-cm-play'); }
          return;
        }
        if (fromUrl) { if (tab !== chosen) p.pause(); return; }
        if (tab === chosen) { p.play(); } else { p.pause(); }
      });
      var hash = '#' + chosen.getAttribute('data-cm-path');
      if (location.hash !== hash && history.replaceState) history.replaceState(null, '', hash);
    });
    window.addEventListener('hashchange', fromHash);
    fromHash();
  });
})();