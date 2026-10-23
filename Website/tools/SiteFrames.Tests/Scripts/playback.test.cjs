// Contracts for the replay playback glue in themes/consolemonster/assets/frames.js (the autoplay function), run on a
// controlled clock: node playback.test.cjs <Website folder>. Exits 1 and lists the failures when a contract breaks.
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const website = process.argv[2] || path.resolve(__dirname, '..', '..', '..');
const source = fs.readFileSync(path.join(website, 'themes', 'consolemonster', 'assets', 'frames.js'), 'utf8');
const start = source.indexOf('  var reduceMotion =');
const end = source.indexOf('  function mountReplay(');
if (start < 0 || end < start) throw new Error('frames.js no longer has the autoplay section this test runs.');
const glue = source.slice(start, end);
const HOLD = Number(/var HOLD = (\d+)/.exec(glue)[1]);
const HERO = 2400;
const DURATION = 14400;

function setup(attrs, options) {
    options = options || {};
    let now = 0, ids = 0;
    const calls = [], timers = new Map(), observers = [], tabListeners = [];
    const motion = { matches: !!options.reduced };
    const document = {
        hidden: false,
        addEventListener(name, fn) { if (name === 'visibilitychange') tabListeners.push(fn); },
        removeEventListener(name, fn) { const i = tabListeners.indexOf(fn); if (i >= 0) tabListeners.splice(i, 1); }
    };
    const events = {};
    const replay = {
        isConnected: true, cmCancelled: false,
        hasAttribute: name => attrs.includes(name),
        addEventListener(name, fn) { events[name] = fn; },
        closest: () => null
    };
    const state = { time: options.at === undefined ? DURATION : options.at, duration: DURATION, wanted: false, playing: false };
    const player = {
        state: () => state,
        play() { calls.push('play@' + state.time); state.wanted = true; },
        pause() { state.wanted = false; },
        seek(ms) { calls.push('seek' + ms); state.time = ms; }
    };
    function IntersectionObserver(fn) {
        this.fn = fn;
        this.on = false;
        observers.push(this);
        // Like the real observer, it reports the current state as soon as it starts: out of view here.
        this.observe = () => { this.on = true; fn([{ isIntersecting: false, intersectionRatio: 0, intersectionRect: { height: 0 } }]); };
        this.disconnect = () => { this.on = false; };
    }
    const context = {
        window: { matchMedia: () => motion, innerHeight: 900, IntersectionObserver },
        IntersectionObserver, document,
        setTimeout(fn, ms) { const id = ++ids; timers.set(id, { fn, at: now + ms }); return id; },
        clearTimeout(id) { timers.delete(id); },
        setInterval() { throw new Error('the playback glue must not poll'); },
        requestAnimationFrame() { throw new Error('the playback glue must not run a frame loop'); }
    };
    vm.createContext(context);
    vm.runInContext(glue + '\nthis.autoplay = autoplay;', context);
    context.autoplay(replay, player, HERO);
    const view = on => observers.filter(o => o.on).forEach(o => o.fn([{ isIntersecting: on, intersectionRatio: on ? 1 : 0, intersectionRect: { height: on ? 600 : 0 } }]));
    return {
        calls, timers, motion, replay, state, observers,
        view() { view(true); },
        leave() { view(false); },
        tab(hidden) { document.hidden = hidden; tabListeners.slice().forEach(fn => fn()); },
        touch() { events.click({ target: { closest: () => true } }); },
        key(name) { events.keydown({ key: name }); },
        // The run reaches its end: the player stops on its final screen.
        finish() { state.time = DURATION; state.wanted = false; },
        tick(ms) {
            const until = now + ms;
            for (;;) {
                const due = [...timers].filter(([, t]) => t.at <= until).sort((a, b) => a[1].at - b[1].at)[0];
                if (!due) break;
                now = due[1].at;
                timers.delete(due[0]);
                due[1].fn();
            }
            now = until;
        }
    };
}

const checks = [];
function check(name, fn) {
    try { fn(); checks.push({ name, pass: true }); } catch (error) { checks.push({ name, pass: false, error: error.message }); }
}
function same(actual, expected, what) {
    if (JSON.stringify(actual) !== JSON.stringify(expected)) throw new Error(what + ': expected ' + JSON.stringify(expected) + ', got ' + JSON.stringify(actual));
}

check('a final poster holds, plays once from the hero frame and then schedules nothing', () => {
    const r = setup(['data-autoplay']);
    r.view();
    r.tick(HOLD - 1);
    same(r.calls, [], 'calls during the hold');
    r.tick(1);
    same(r.calls, ['seek' + HERO, 'play@' + HERO], 'calls after the hold');
    r.finish();
    r.tick(60000);
    same(r.calls.length, 2, 'plays after the run ended');
    same(r.timers.size, 0, 'timers once it has ended');
});

check('a poster in the middle of a run plays on from there at once', () => {
    const r = setup(['data-autoplay'], { at: 5000 });
    r.view();
    same(r.calls, ['play@5000'], 'calls');
});

check('a replay nobody asked to play watches nothing and schedules nothing', () => {
    const r = setup([]);
    same(r.observers.length, 0, 'observers');
    r.tick(60000);
    same(r.calls, [], 'calls');
    same(r.timers.size, 0, 'timers');
});

check('reduced motion from the start keeps the poster', () => {
    const r = setup(['data-autoplay'], { reduced: true });
    r.view();
    r.tick(60000);
    same(r.calls, [], 'calls');
});

check('reduced motion turned on before the replay comes into view keeps the poster', () => {
    const r = setup(['data-autoplay']);
    r.motion.matches = true;
    r.view();
    r.tick(60000);
    same(r.calls, [], 'calls');
    same(r.timers.size, 0, 'timers');
});

check('reduced motion turned on during the hold cancels the start', () => {
    const r = setup(['data-autoplay']);
    r.view();
    r.tick(HOLD / 2);
    r.motion.matches = true;
    r.tick(60000);
    same(r.calls, [], 'calls');
    same(r.timers.size, 0, 'timers');
});

check('touching the controls during the hold hands playback to the reader', () => {
    const r = setup(['data-autoplay']);
    r.view();
    r.tick(HOLD / 2);
    r.touch();
    r.tick(60000);
    same(r.calls, [], 'calls');
    same(r.timers.size, 0, 'timers');
});

check('leaving the view during the hold cancels it, and coming back holds again', () => {
    const r = setup(['data-autoplay']);
    r.view();
    r.tick(HOLD / 2);
    r.leave();
    same(r.timers.size, 0, 'timers out of view');
    r.tick(60000);
    same(r.calls, [], 'calls out of view');
    r.view();
    r.tick(HOLD - 1);
    same(r.calls, [], 'calls during the second hold');
    r.tick(1);
    same(r.calls, ['seek' + HERO, 'play@' + HERO], 'calls after the second hold');
});

check('a hidden tab cancels the hold, and the tab coming back holds again', () => {
    const r = setup(['data-autoplay']);
    r.view();
    r.tick(HOLD / 2);
    r.tab(true);
    same(r.timers.size, 0, 'timers while hidden');
    r.tick(60000);
    same(r.calls, [], 'calls while hidden');
    r.tab(false);
    r.tick(HOLD);
    same(r.calls, ['seek' + HERO, 'play@' + HERO], 'calls after the tab returned');
});

check('a path chosen before its replay mounted plays once it is in view, from the hero frame, without a hold', () => {
    const r = setup(['data-cm-play']);
    r.tick(60000);
    same(r.calls, [], 'calls before it is in view');
    r.view();
    same(r.calls, ['seek' + HERO, 'play@' + HERO], 'calls in view');
    r.finish();
    r.tick(60000);
    same(r.timers.size, 0, 'timers once it has ended');
});

check('a path chosen before its replay mounted follows reduced motion, the controls and a hidden tab', () => {
    const reduced = setup(['data-cm-play'], { reduced: true });
    reduced.view();
    same(reduced.calls, [], 'calls with reduced motion');
    const touched = setup(['data-cm-play']);
    touched.touch();
    touched.view();
    same(touched.calls, [], 'calls after the controls were used');
    const hidden = setup(['data-cm-play']);
    hidden.tab(true);
    hidden.view();
    same(hidden.calls, [], 'calls in a hidden tab');
    hidden.tab(false);
    same(hidden.calls, ['seek' + HERO, 'play@' + HERO], 'calls when the tab came back');
});

check('a path chosen after its replay mounted (cmPlay) follows the same rules', () => {
    const r = setup([]);
    r.motion.matches = true;
    r.replay.cmPlay();
    r.view();
    same(r.calls, [], 'calls with reduced motion');
    r.motion.matches = false;
    r.replay.cmPlay();
    same(r.calls, ['seek' + HERO, 'play@' + HERO], 'calls once motion is allowed');
});

check('a page that asks to loop goes round after the hold, also when the path was chosen before mounting', () => {
    for (const attrs of [['data-autoplay', 'data-loop'], ['data-cm-play', 'data-loop']]) {
        const r = setup(attrs);
        r.view();
        r.tick(attrs[0] === 'data-autoplay' ? HOLD : 0);
        same(r.calls.filter(c => c.startsWith('play')).length, 1, attrs.join(' ') + ': first run');
        r.tick(DURATION - HERO);
        r.finish();
        r.tick(60000);
        if (r.calls.filter(c => c.startsWith('play')).length < 2) throw new Error(attrs.join(' ') + ': no second round');
        r.leave();
        same(r.timers.size, 0, attrs.join(' ') + ': timers out of view');
    }
});

check('the home switch choosing a path that is not in view yet plays it once it is, without a hold', () => {
    const r = setup([]);
    r.replay.cmPlay();
    same(r.calls, [], 'calls before it is in view');
    same(r.timers.size, 0, 'timers before it is in view');
    r.view();
    same(r.calls, ['seek' + HERO, 'play@' + HERO], 'calls in view');
});

check('a run that has ended does not play again when it comes back into view', () => {
    const r = setup(['data-autoplay']);
    r.view();
    r.tick(HOLD);
    r.finish();
    r.leave();
    r.view();
    r.tick(60000);
    same(r.calls.filter(c => c.startsWith('play')).length, 1, 'plays');
    same(r.timers.size, 0, 'timers');
});

check('moving through the controls with Tab does not stop the start, using a key does', () => {
    const tab = setup(['data-autoplay']);
    tab.view();
    tab.key('Tab');
    tab.tick(HOLD);
    same(tab.calls, ['seek' + HERO, 'play@' + HERO], 'calls after Tab');
    const space = setup(['data-autoplay']);
    space.view();
    space.key(' ');
    space.tick(60000);
    same(space.calls, [], 'calls after Space');
});
const failed = checks.filter(c => !c.pass);
for (const c of checks) console.log((c.pass ? 'pass ' : 'FAIL ') + c.name + (c.error ? ': ' + c.error : ''));
process.exitCode = failed.length ? 1 : 0;
