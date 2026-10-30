// ConsoleMonster site behaviour: the theme switch, header navigation, code blocks (copy and highlighting) and tabs.
// Frames, tier switches, width controls and replays live in frames.js.
(function () {
  'use strict';

  var root = document.documentElement;

  // <lang-choice> Which language a switch shows: the reader's choice when the example has it, otherwise the language the
  // example has; the languages it lacks cannot be chosen. The reader's choice itself is kept for the next example that
  // has it. Kept free of the page so tools/SiteFrames.Tests can run it.
  function langChoice(preferred, available) {
    var has = function (name) { return available.indexOf(name) >= 0; };
    var lang = has(preferred) || !available.length ? preferred : available[0];
    return { lang: lang, unavailable: { powershell: available.length > 0 && !has('powershell'), csharp: available.length > 0 && !has('csharp'), 'csharp-fluent': available.length > 0 && !has('csharp-fluent') } };
  }
  // </lang-choice>
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
  var ALIAS = { cs: 'csharp', 'c#': 'csharp', ps1: 'powershell', pwsh: 'powershell', sh: 'bash', shell: 'bash', terminal: 'bash', html: 'markup', xml: 'markup', yml: 'yaml' };
  var NEEDS = { csharp: ['clike'], 'xml-doc': ['markup'] };
  var PLAIN = { text: true, none: true, plaintext: true, txt: true };
  // Prism's PowerShell grammar has no token for parameters (-Title, -Scope); ConsoleMonster's own highlighter colours
  // them as keys. Operators such as -eq and -join keep their own token.
  var EXTEND = {
    // Prism's bash grammar colours flags and builtins, but not other commands (dotnet, git): the first word of a command
    // line or after && ; | is coloured as a function, like a cmdlet in PowerShell.
    bash: function (p) {
      if (p.languages.bash && !p.languages.bash['cm-command']) {
        p.languages.insertBefore('bash', 'function', {
          'cm-command': { pattern: /(^|[;&|]\s*|&&\s*)[A-Za-z][\w.-]*(?=\s|$)/m, lookbehind: true, alias: 'function' }
        });
      }
    },
    powershell: function (p) {
      if (p.languages.powershell && !p.languages.powershell.parameter) {
        p.languages.insertBefore('powershell', 'operator', {
          parameter: { pattern: /(^|[\s(\[{,;|=])-(?![ci]?(?:and|x?or|as|not(?:contains|in|like|match)?|contains|in|like|match|eq|ge|gt|le|lt|ne|is(?:not)?|band|bnot|bor|bxor|join|split|replace|f|shl|shr)\b)[a-z]\w*/i, lookbehind: true }
        });
        // A keyword is a whole word: Prism's \b also matches inside hyphenated names, colouring filter in fleet-filter
        // and process in process-monitor.
        var keyword = p.languages.powershell.keyword;
        if (keyword instanceof RegExp) {
          p.languages.powershell.keyword = { pattern: new RegExp('(^|[^\\w$-])(?:' + keyword.source + ')(?![\\w-])', keyword.flags), lookbehind: true };
        }
      }
    }
  };
  var prismReady = null;
  var grammars = {};
  // Prism components load by fixed URLs, so they carry the page's content version (meta cm-runtime) like frame bundles.
  var runtime = document.querySelector('meta[name="cm-runtime"]');
  var runtimeVersion = runtime ? runtime.getAttribute('content') : '';
  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var script = document.createElement('script');
      script.src = runtimeVersion ? src + (src.indexOf('?') < 0 ? '?' : '&') + 'v=' + runtimeVersion : src;
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

  // Identifiers in tables (CONSOLEMONSTER_AMBIGUOUS_WIDTH) may break only after an underscore, never inside a word.
  document.querySelectorAll('.cm-prose td > code, .cm-prose th > code').forEach(function (code) {
    var text = code.textContent;
    if (text.indexOf('_') < 1 || code.children.length || /\s/.test(text)) return;
    code.textContent = '';
    text.split('_').forEach(function (part, i, parts) {
      code.appendChild(document.createTextNode(part + (i < parts.length - 1 ? '_' : '')));
      if (i < parts.length - 1) code.appendChild(document.createElement('wbr'));
    });
    code.classList.add('cm-ident');
  });
  // Markdown fences become the shared code block: a caption with the language and a copy action.
  // dotnet commands (with the cd that goes with them) are fenced as terminal and labelled Terminal; CodeGuard.cs checks the dotnet part.
  var names = { csharp: 'C#', cs: 'C#', powershell: 'PowerShell', ps1: 'PowerShell', json: 'JSON', bash: 'Shell', terminal: 'Terminal', text: 'Text', xml: 'XML', yaml: 'YAML' };
  // Prose code and the examples on the reference pages are set in the shared code block (language, copy).
  document.querySelectorAll('.cm-prose pre > code, .pf-api-docs .type-examples pre > code').forEach(function (code) {
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
    // textContent, not innerText: the full program sits in a collapsed <details>, and innerText of unrendered content is empty.
    var text = code ? code.textContent.replace(/\n$/, '') : '';
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
    if (text && navigator.clipboard && navigator.clipboard.writeText) {
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
      tab.cmSelect = select;
      tab.addEventListener('click', function () { select(tab, false); });
      tab.addEventListener('keydown', function (event) {
        var step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
        if (event.key === 'Home') { event.preventDefault(); select(tabs[0], true); }
        else if (event.key === 'End') { event.preventDefault(); select(tabs[tabs.length - 1], true); }
        else if (step) { event.preventDefault(); select(tabs[(index + step + tabs.length) % tabs.length], true); }
      });
    });
  });

  // The reader's source style, PowerShell, C# or C# fluent: one choice for every page, kept in <html data-lang> (set before the page
  // draws by partials/head.html) and in localStorage when it can be. Try-it switches ([data-cm-lang-switch], a radio
  // group) show the chosen language's code through CSS; tabs that name a language (data-cm-lang, such as the home
  // page's install line) select the matching tab.
  var langs = { powershell: true, csharp: true, 'csharp-fluent': true };
  // A PowerShell excerpt followed by its C# twin (.cm-lang blocks on docs pages) gets a switch of its own.
  document.querySelectorAll('.cm-lang[data-lang="powershell"]').forEach(function (ps) {
    var cs = ps.nextElementSibling;
    if (!cs || !cs.matches('.cm-lang[data-lang="csharp"]')) return;
    var fluent = cs.nextElementSibling;
    if (!fluent || !fluent.matches('.cm-lang[data-lang="csharp-fluent"]')) fluent = null;
    var pair = document.createElement('div');
    pair.className = 'cm-langpair';
    pair.innerHTML = '<div class="cm-try__bar"><div class="cm-seg cm-try__lang" role="radiogroup" aria-label="Language" data-cm-lang-switch hidden>' +
      '<button type="button" role="radio" aria-checked="true" data-lang="powershell" style="--key: var(--cm-s3)">PowerShell</button>' +
      '<button type="button" role="radio" aria-checked="false" data-lang="csharp" style="--key: var(--cm-s5)">C#</button>' +
      (fluent ? '<button type="button" role="radio" aria-checked="false" data-lang="csharp-fluent" style="--key: var(--cm-s5)">C# fluent</button>' : '') + '</div></div>';
    ps.parentNode.insertBefore(pair, ps);
    pair.appendChild(ps);
    pair.appendChild(cs);
    if (fluent) pair.appendChild(fluent);
  });
  var currentLang = function () { return langs[root.dataset.lang] ? root.dataset.lang : 'powershell'; };
  var langTabs = Array.prototype.slice.call(document.querySelectorAll('[role="tab"][data-cm-lang]'));
  // The languages the switch's example has: a Try it block's shown example names them (data-langs); a demo's source or
  // a pair of excerpts has both.
  var sourceScopeSelector = '.cm-langpair, .cm-try__scenario, .cm-demo__source, .cm-reel-sec';
  var sourcePaneSelector = '.cm-lang[data-lang], .cm-try__code[data-lang], .cm-reel__cmd[data-lang], .cm-reel-shortcut__cmds[data-lang]';
  var panesOf = function (scope) {
    return Array.prototype.filter.call(scope.querySelectorAll(sourcePaneSelector), function (pane) {
      return pane.closest(sourceScopeSelector) === scope;
    });
  };
  var languagesOf = function (group) {
    var block = group.closest('.cm-try');
    var shown = block ? Array.prototype.filter.call(block.querySelectorAll('.cm-try__scenario[data-langs]'), function (s) { return !s.hidden; })[0] : null;
    if (shown) return shown.getAttribute('data-langs').split(' ').filter(Boolean);
    var pair = group.closest('.cm-langpair');
    var scope = pair || group.closest(sourceScopeSelector);
    return scope ? panesOf(scope).map(function (pane) { return pane.getAttribute('data-lang'); }) : ['powershell', 'csharp', 'csharp-fluent'];
  };
  var syncLang = function () {
    var lang = currentLang();
    document.querySelectorAll('[data-cm-lang-switch]').forEach(function (group) {
      group.hidden = false;
      var choice = langChoice(lang, languagesOf(group));
      group.querySelectorAll('[role="radio"]').forEach(function (option) {
        var name = option.getAttribute('data-lang');
        var on = name === choice.lang;
        option.setAttribute('aria-checked', on ? 'true' : 'false');
        option.tabIndex = on ? 0 : -1;
        option.disabled = !!choice.unavailable[name];
        if (option.disabled) option.setAttribute('aria-disabled', 'true'); else option.removeAttribute('aria-disabled');
      });
    });
    // Resolve each source independently. Installation snippets may have fewer styles than a complete demo;
    // a remembered fluent choice must not leave those existing snippets blank or alter the stored preference.
    document.querySelectorAll(sourceScopeSelector).forEach(function (scope) {
      var panes = panesOf(scope);
      var choice = langChoice(lang, panes.map(function (pane) { return pane.getAttribute('data-lang'); }));
      panes.forEach(function (pane) {
        var selected = pane.getAttribute('data-lang') === choice.lang;
        pane.hidden = !selected;
        pane.style.display = selected ? (pane.matches('.cm-try__code, .cm-reel-steps, .cm-reel-shortcut__cmds') ? 'grid' : 'block') : 'none';
      });
    });
    langTabs.forEach(function (tab) {
      // C# fluent selects its own tab where a tab set has one (the home page's program), and the C# tab where it does not.
      var fluentTab = lang === 'csharp-fluent' && tab.parentNode.querySelector('[role="tab"][data-cm-lang="csharp-fluent"]');
      var wanted = lang === 'csharp-fluent' && !fluentTab ? 'csharp' : lang;
      if (tab.getAttribute('data-cm-lang') === wanted && tab.getAttribute('aria-selected') !== 'true' && tab.cmSelect) tab.cmSelect(tab, false);
    });
  };
  var setLang = function (lang) {
    if (!langs[lang]) return;
    root.dataset.lang = lang;
    try { localStorage.setItem('cm-lang', lang); } catch (e) { /* storage unavailable: the choice lasts for this page */ }
    syncLang();
    // Code that was hidden is measured again (scroll hints), as after a tab change.
    document.dispatchEvent(new CustomEvent('cm:tab', { detail: null }));
  };
  document.querySelectorAll('[data-cm-lang-switch]').forEach(function (group) {
    var options = Array.prototype.slice.call(group.querySelectorAll('[role="radio"]'));
    options.forEach(function (option, index) {
      // The option already shown does nothing: in an example with one language it is shown without being the reader's choice.
      option.addEventListener('click', function () { if (option.getAttribute('aria-checked') !== 'true') setLang(option.getAttribute('data-lang')); });
      option.addEventListener('keydown', function (event) {
        var step = /^(ArrowRight|ArrowDown)$/.test(event.key) ? 1 : /^(ArrowLeft|ArrowUp)$/.test(event.key) ? -1 : 0;
        if (!step && event.key !== 'Home' && event.key !== 'End') return;
        event.preventDefault();
        var enabled = options.filter(function (candidate) { return !candidate.disabled; });
        var position = enabled.indexOf(option);
        var next = event.key === 'Home' ? enabled[0] : event.key === 'End' ? enabled[enabled.length - 1] : enabled[(position + step + enabled.length) % enabled.length];
        if (!next) return;
        setLang(next.getAttribute('data-lang'));
        next.focus();
      });
    });
  });
  langTabs.forEach(function (tab) {
    tab.addEventListener('click', function () { if (currentLang() !== tab.getAttribute('data-cm-lang')) setLang(tab.getAttribute('data-cm-lang')); });
    tab.addEventListener('keyup', function (event) {
      if (/^(ArrowLeft|ArrowRight|Home|End)$/.test(event.key) && tab.getAttribute('aria-selected') === 'true' && currentLang() !== tab.getAttribute('data-cm-lang')) setLang(tab.getAttribute('data-cm-lang'));
    });
  });
  syncLang();
  // Choosing another example in a Try it block changes which languages its switch offers.
  document.addEventListener('cm:tab', function (event) { if (event.detail) syncLang(); });

  // Download on a code block saves the whole program under its file name, built from the code on the page, so the file
  // is always the code shown. A PowerShell script gets a byte order mark, which Windows PowerShell 5.1 needs for
  // characters outside ASCII.
  document.querySelectorAll('[data-cm-download]').forEach(function (button) { button.hidden = !window.Blob || !window.URL; });
  document.addEventListener('click', function (event) {
    var button = event.target.closest('[data-cm-download]');
    if (!button) return;
    var figure = button.closest('.cm-code');
    var full = figure.querySelector('.cm-code__full pre');
    var code = full || figure.querySelector('pre');
    var name = button.getAttribute('data-cm-download');
    var text = (code ? code.textContent : '').replace(/\n?$/, '\n');
    if (/\.ps1$/i.test(name)) text = '\ufeff' + text.replace(/\r?\n/g, '\r\n');
    var link = document.createElement('a');
    link.href = URL.createObjectURL(new Blob([text], { type: 'text/plain;charset=utf-8' }));
    link.download = name;
    document.body.appendChild(link);
    link.click();
    setTimeout(function () { URL.revokeObjectURL(link.href); link.remove(); }, 1500);
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
    // Only the address the page opened with counts as not a click; a later change of the hash (a link on the page) is.
    var fromHash = function (initial) {
      var id = (location.hash || '').replace('#', '');
      var tab = tabs.filter(function (t) { return t.getAttribute('data-cm-path') === id; })[0];
      if (tab && tab.getAttribute('aria-selected') !== 'true') { fromUrl = initial === true; tab.click(); fromUrl = false; }
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
        if (tab === chosen) { if (replay.cmPlay) replay.cmPlay(); else p.play(); } else { p.pause(); }
      });
      var hash = '#' + chosen.getAttribute('data-cm-path');
      if (location.hash !== hash && history.replaceState) history.replaceState(null, '', hash);
    });
    window.addEventListener('hashchange', function () { fromHash(false); });
    fromHash(true);
  });

  // Code never wraps (base.css). A block whose lines run past its width says so in its bar (is-wide), and its right edge
  // fades out until it is scrolled to the end (is-end); is-tall keeps a vertical scrollbar out of the fade. Blocks that
  // skip rendering off screen are measured when they come near, where the browser says so.
  var deferred = !!(Element.prototype.checkVisibility && ('oncontentvisibilityautostatechange' in window));
  function atEnd(pre) { pre.classList.toggle('is-end', pre.scrollLeft + pre.clientWidth >= pre.scrollWidth - 2); }
  function markCode(scope) {
    (scope && scope.querySelectorAll ? scope : document).querySelectorAll('.cm-code').forEach(function (block) {
      // The whole program when it is open (the short sample is hidden then), otherwise the sample.
      var pre = block.querySelector(':scope > .cm-code__full[open] > pre') || block.querySelector(':scope > pre');
      if (!pre || (deferred && !pre.checkVisibility({ contentVisibilityAuto: true }))) return;
      block.classList.toggle('is-wide', pre.scrollWidth > pre.clientWidth + 2);
      pre.classList.toggle('is-tall', pre.scrollHeight > pre.clientHeight + 2);
      atEnd(pre);
    });
  }
  var codeTimer = 0;
  var remark = function () { markCode(document); };
  document.addEventListener('scroll', function (event) {
    var pre = event.target;
    if (pre && pre.matches && pre.matches('.cm-code pre')) atEnd(pre);
  }, { capture: true, passive: true });
  document.addEventListener('contentvisibilityautostatechange', function (event) { if (!event.skipped) markCode(event.target); }, true);
  document.addEventListener('toggle', function (event) { if (event.target.matches && event.target.matches('.cm-code__full')) remark(); }, true);
  document.addEventListener('cm:tab', remark);
  window.addEventListener('resize', function () { clearTimeout(codeTimer); codeTimer = setTimeout(remark, 150); });
  window.addEventListener('load', remark);
  if (document.fonts && document.fonts.ready) document.fonts.ready.then(remark);
  remark();
  // Demos page: the category filter (a radio group) shows the cards of one category; without JavaScript every card shows.
  document.querySelectorAll('[data-cm-reel-filter]').forEach(function (group) {
    var grid = group.closest('[data-cm-reels]');
    var options = Array.prototype.slice.call(group.querySelectorAll('[role="radio"]'));
    var pick = function (option, focus) {
      var category = option.getAttribute('data-category');
      options.forEach(function (o) { var on = o === option; o.setAttribute('aria-checked', on ? 'true' : 'false'); o.tabIndex = on ? 0 : -1; });
      grid.querySelectorAll('.cm-reels__grid > li').forEach(function (card) {
        card.hidden = category !== 'all' && card.getAttribute('data-category') !== category;
      });
      // A section left with no cards (showcases or starters) hides with its heading.
      grid.querySelectorAll('.cm-reels__tier').forEach(function (tier) {
        tier.hidden = !tier.querySelector('.cm-reels__grid > li:not([hidden])');
      });
      if (focus) option.focus();
    };
    options.forEach(function (option, index) {
      option.addEventListener('click', function () { pick(option, false); });
      option.addEventListener('keydown', function (event) {
        var step = event.key === 'ArrowRight' || event.key === 'ArrowDown' ? 1 : event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 0;
        if (step) { event.preventDefault(); pick(options[(index + step + options.length) % options.length], true); }
      });
    });
    group.hidden = false;
  });
})();