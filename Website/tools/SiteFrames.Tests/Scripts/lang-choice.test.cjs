// Contracts for the PowerShell | C# | C# fluent switch (langChoice in themes/consolemonster/assets/site.js) and the Try it markup
// that feeds it: node lang-choice.test.cjs <Website folder>. Exits 1 and lists the failures when a contract breaks.
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const website = process.argv[2] || path.resolve(__dirname, '..', '..', '..');
const site = fs.readFileSync(path.join(website, 'themes', 'consolemonster', 'assets', 'site.js'), 'utf8');
const start = site.indexOf('// <lang-choice>');
const end = site.indexOf('// </lang-choice>');
if (start < 0 || end < start) throw new Error('site.js no longer marks langChoice with <lang-choice>.');
const context = {};
vm.createContext(context);
vm.runInContext(site.slice(start, end) + '\nthis.langChoice = langChoice;', context);
const langChoice = context.langChoice;

const checks = [];
function check(name, fn) {
    try { fn(); checks.push({ name, pass: true }); } catch (error) { checks.push({ name, pass: false, error: error.message }); }
}
function same(actual, expected, what) {
    if (JSON.stringify(actual) !== JSON.stringify(expected)) throw new Error(what + ': expected ' + JSON.stringify(expected) + ', got ' + JSON.stringify(actual));
}

check('an example in both languages shows the reader\'s choice and offers both', () => {
    same(langChoice('powershell', ['powershell', 'csharp']), { lang: 'powershell', unavailable: { powershell: false, csharp: false, 'csharp-fluent': true } }, 'powershell');
    same(langChoice('csharp', ['powershell', 'csharp']), { lang: 'csharp', unavailable: { powershell: false, csharp: false, 'csharp-fluent': true } }, 'csharp');
});

check('a C#-only example shows C# and does not offer PowerShell', () => {
    same(langChoice('powershell', ['csharp']), { lang: 'csharp', unavailable: { powershell: true, csharp: false, 'csharp-fluent': true } }, 'choice');
});

check('a PowerShell-only example shows PowerShell and does not offer C#', () => {
    same(langChoice('csharp', ['powershell']), { lang: 'powershell', unavailable: { powershell: false, csharp: true, 'csharp-fluent': true } }, 'choice');
});

check('the option shown in a one-language example is not stored as the reader\'s choice', () => {
    // Clicking it again would otherwise make C# the choice for every page just by looking at a C#-only example.
    if (!/option\.addEventListener\('click', function \(\) \{ if \(option\.getAttribute\('aria-checked'\) !== 'true'\) setLang\(/.test(site)) throw new Error('clicking the option already shown changes the stored language.');
    same(langChoice('powershell', ['csharp']).lang, 'csharp', 'shown in the C#-only example');
    same(langChoice('powershell', ['powershell', 'csharp']).lang, 'powershell', 'shown in the next example with both');
});

check('every Try it example says which languages it has, and the switch reads them', () => {
    const tryHtml = fs.readFileSync(path.join(website, 'themes', 'consolemonster', 'partials', 'try.html'), 'utf8');
    if (!/class="cm-try__scenario" data-langs="\{\{ langs \}\}"/.test(tryHtml)) throw new Error('try.html does not write data-langs on each example.');
    if (!site.includes("'.cm-try__scenario[data-langs]'") || !site.includes('langChoice(lang, languagesOf(group))')) throw new Error('site.js does not choose the language from the shown example.');
    if (!/option\.disabled = !!choice\.unavailable\[name\]/.test(site)) throw new Error('site.js does not disable the languages an example lacks.');
    if (!/document\.addEventListener\('cm:tab'[^\n]*syncLang\(\)/.test(site)) throw new Error('site.js does not update the switch when another example is chosen.');
    // Once, when the page starts: inside setLang it would only be added after the reader's first choice (and again on each).
    const setLang = site.slice(site.indexOf('var setLang = function'), site.indexOf('\n  };', site.indexOf('var setLang = function')));
    if (setLang.includes('addEventListener')) throw new Error('site.js adds the example listener inside setLang.');
});

check('all three styles honor the remembered choice', () => {
    for (const preferred of ['powershell', 'csharp', 'csharp-fluent']) {
        same(langChoice(preferred, ['powershell', 'csharp', 'csharp-fluent']), { lang: preferred, unavailable: { powershell: false, csharp: false, 'csharp-fluent': false } }, preferred);
    }
    same(langChoice('csharp-fluent', ['powershell', 'csharp']).lang, 'powershell', 'fallback keeps PowerShell first');
});

check('the first paint defaults to PowerShell when storage is empty, corrupt or denied', () => {
    const head = fs.readFileSync(path.join(website, 'themes', 'consolemonster', 'partials', 'head.html'), 'utf8');
    const preload = head.match(/<script>([\s\S]*?)<\/script>/)[1];
    for (const saved of [null, 'invalid', 'csharp-fluent', 'denied']) {
        const root = { classList: { add() {} }, dataset: {} };
        vm.runInNewContext(preload, {
            document: { documentElement: root }, matchMedia: () => ({ matches: false }),
            localStorage: { getItem() { if (saved === 'denied') throw new Error('Storage denied'); return saved; } }
        });
        same(root.dataset.lang, saved === 'csharp-fluent' ? saved : 'powershell', 'initial style for ' + saved);
    }
});

check('a denied storage write still switches styles on the current page', () => {
    const begin = site.indexOf('var setLang = function');
    const end = site.indexOf('\n  };', begin) + '\n  };'.length;
    const root = { dataset: { lang: 'powershell' } };
    let synced = 0, dispatched = 0;
    const scope = { root, langs: { powershell: true, csharp: true, 'csharp-fluent': true },
        localStorage: { setItem() { throw new Error('Storage denied'); } }, syncLang() { synced++; },
        document: { dispatchEvent() { dispatched++; } }, CustomEvent: function () {} };
    vm.runInNewContext(site.slice(begin, end) + '\nsetLang("csharp-fluent");', scope);
    same([root.dataset.lang, synced, dispatched], ['csharp-fluent', 1, 1], 'current-page selection');
});

const failed = checks.filter(c => !c.pass);
for (const c of checks) console.log((c.pass ? 'pass ' : 'FAIL ') + c.name + (c.error ? ': ' + c.error : ''));
process.exitCode = failed.length ? 1 : 0;
