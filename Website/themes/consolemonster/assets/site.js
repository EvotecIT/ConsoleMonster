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
    var reset = function () { groups.forEach(function (group) { setOpen(group, compact.matches); }); };
    if (compact.addEventListener) compact.addEventListener('change', reset);
    reset();
    var toggle = document.getElementById('cm-nav-toggle');
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && toggle && toggle.checked) { toggle.checked = false; toggle.focus(); }
    });
  }

  // Syntax highlighting with the self-hosted Prism, loaded the first time a page has code.
  var PRISM = '/assets/prism/';
  var prismReady = null;
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
    if (window.Prism && window.Prism.highlightElement && window.Prism.plugins && window.Prism.plugins.autoloader) return Promise.resolve(window.Prism);
    if (!prismReady) {
      window.Prism = window.Prism || {};
      window.Prism.manual = true;
      prismReady = loadScript(PRISM + 'prism-core.js')
        .then(function () { return loadScript(PRISM + 'prism-autoloader.js'); })
        .then(function () { window.Prism.plugins.autoloader.languages_path = PRISM + 'components/'; return window.Prism; });
    }
    return prismReady;
  }
  function highlight(code) {
    if (!code || code.dataset.cmHighlighted) return;
    code.dataset.cmHighlighted = '1';
    prism().then(function (p) { p.highlightElement(code); }, function () { /* plain text stays readable */ });
  }
  window.cmHighlight = highlight;

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
    copy.textContent = 'Copy';
    bar.appendChild(label);
    bar.appendChild(copy);
    pre.parentNode.insertBefore(figure, pre);
    figure.appendChild(bar);
    figure.appendChild(pre);
  });
  document.querySelectorAll('.cm-code code[class*="language-"]').forEach(function (code) {
    if (!/language-text/.test(code.className)) highlight(code);
  });

  document.addEventListener('click', function (event) {
    var button = event.target.closest('[data-cm-copy]');
    if (!button) return;
    var code = button.closest('.cm-code').querySelector('pre');
    var text = code ? code.innerText.replace(/\n$/, '') : '';
    var done = function (label) { button.textContent = label; setTimeout(function () { button.textContent = 'Copy'; }, 1600); };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () { done('Copied'); }, function () { done('Select and copy'); });
    } else {
      done('Select and copy');
    }
  });

  // Tabs: [data-cm-tabs] holds a .cm-seg with role="tab" buttons (aria-controls) and the panels they show.
  document.querySelectorAll('[data-cm-tabs]').forEach(function (set) {
    var tabs = Array.prototype.slice.call(set.querySelectorAll('[role="tab"]'));
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
})();
