#!/usr/bin/env node
// Rendering cost of consolemonster.dev. Drives the pages people interact with (tier switches, width switches, the home
// page's two paths, a docs page with replays, an API page) and leaves pages alone to see what they cost at rest, then
// reports per scenario:
//   - frame intervals (p50/p95/max), long animation frames and long tasks;
//   - CPU time of the renderer, GPU and browser processes (CDP SystemInfo.getProcessInfo);
//   - requestAnimationFrame, setTimeout and setInterval calls made by the page;
//   - paints (count, largest and total area) and layout/style time from a timeline trace;
//   - the composited layer tree (CDP LayerTree): layer count, the largest layer, total layer area, and which layers
//     repainted, with their compositing reasons.
// With --budget it fails when a scenario goes over its limits; a failing scenario is retried (--attempts) so one noisy
// run does not fail the build.
//
//   node Website/build/perf/site-perf.cjs --base http://localhost:8080 --out perf-results [--budget Website/build/perf/budget.json]
//        [--only widgets,home] [--attempts 3] [--idle-seconds 30] [--width 1900 --height 1900] [--scale 1.5] [--cpu 1]
//        [--channel msedge] [--assets <dir>] [--trace] [--layers] [--headed]
//
// --assets serves /themes/consolemonster/assets/<file> from a local folder instead (an A/B run of other CSS/JS against
// the same build). The script lowers its own priority (and so the browser's) to below normal and runs headless, one
// browser at a time, so measuring does not load the machine it runs on. Needs playwright (CI) or playwright-core;
// --channel msedge uses an installed Edge instead of a downloaded Chromium. The browser runs with its accessibility tree
// on (--force-renderer-accessibility), as it does on any machine with a screen reader, password manager or other UI
// Automation client: that is where exposing every terminal cell to assistive technology cost a core per frame swap.
'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');

try {
    os.setPriority(0, os.constants.priority.PRIORITY_BELOW_NORMAL);
} catch (e) {
    // Not every platform lets a process lower itself; the measurements still stand.
}

function load() {
    for (const name of ['playwright', 'playwright-core']) {
        try {
            return require(name);
        } catch (e) {
            // Try the next package name.
        }
    }
    throw new Error('Install playwright (npm install --no-save playwright) or put playwright-core on NODE_PATH.');
}

function args() {
    const options = { base: 'http://localhost:8080', out: 'perf-results', budget: null, channel: null, only: null, attempts: 3, trace: false, layers: false, headed: false,
        width: 1900, height: 1900, scale: 1.5, cpu: 1, idleSeconds: 30, assets: null };
    const argv = process.argv.slice(2);
    for (let i = 0; i < argv.length; i++) {
        const key = argv[i].replace(/^--/, '');
        if (key === 'trace' || key === 'layers' || key === 'headed') { options[key] = true; continue; }
        const value = argv[++i];
        if (key === 'only') options.only = value.split(',').map(s => s.trim()).filter(Boolean);
        else if (key === 'idle-seconds') options.idleSeconds = Math.max(1, parseFloat(value) || 30);
        else if (['attempts', 'width', 'height', 'scale', 'cpu'].includes(key)) options[key] = Math.max(1, parseFloat(value) || 1);
        else options[key] = value;
    }
    return options;
}

// Installed before any page script: a frame loop that runs only while a scenario is measured, observers for long frames
// and tasks, and counters for the page's own animation frames and timers.
const instrument = `(() => {
  const raf = window.requestAnimationFrame.bind(window);
  const p = window.__cmPerf = { frames: [], loaf: [], tasks: [], running: false, loop: false, raf: 0, timeout: 0, interval: 0 };
  let last = 0;
  function tick(t) { if (!p.running || !p.loop) { last = 0; return; } if (last) p.frames.push(t - last); last = t; raf(tick); }
  p.start = function (loop) { p.running = true; p.loop = loop; p.raf = p.timeout = p.interval = 0; if (loop) raf(tick); };
  const wrap = (name, key) => { const original = window[name]; window[name] = function () { if (p.running) p[key]++; return original.apply(window, arguments); }; };
  wrap('requestAnimationFrame', 'raf'); wrap('setTimeout', 'timeout'); wrap('setInterval', 'interval');
  try { new PerformanceObserver(l => { for (const e of l.getEntries()) if (p.running) p.loaf.push({ d: e.duration, b: e.blockingDuration || 0 }); }).observe({ type: 'long-animation-frame', buffered: false }); } catch (e) {}
  try { new PerformanceObserver(l => { for (const e of l.getEntries()) if (p.running) p.tasks.push(e.duration); }).observe({ type: 'longtask', buffered: false }); } catch (e) {}
})();`;

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));

// Clicks in the page itself: Playwright's own click (actionability checks, synthetic input) costs browser-process CPU
// that would hide what the page costs.
const press = handle => handle.evaluate(element => element.click());

async function tierSwitches(page, count, pause) {
    const colours = await page.$$('[data-cm-tiers] [data-color]');
    const glyphs = await page.$$('[data-cm-tiers] [data-glyphs]');
    for (let i = 0; i < count; i++) {
        const set = i % 2 === 0 ? colours : glyphs;
        // Start one past the defaults (24-bit, Full): pressing the selected option changes nothing.
        await press(set[((i >> 1) + 1) % set.length]);
        await wait(pause);
    }
}

async function wheel(page, steps, delta, pause) {
    for (let i = 0; i < steps; i++) { await page.mouse.wheel(0, delta); await wait(pause); }
    for (let i = 0; i < steps; i++) { await page.mouse.wheel(0, -delta); await wait(pause); }
}

// The result poster also opens at the end: observe playback before accepting a stopped state.
async function monsterPlaying(page) {
    await page.waitForFunction(() => {
        const host = document.querySelector('[data-cm-replay*="demo-app"]');
        const player = host && host.__cmPlayer;
        if (!player || !player.state) return false;
        const s = player.state();
        return s.playing && s.wanted && s.time < s.duration;
    }, null, { timeout: 15000, polling: 100 });
}

// Waits until the Monster hero's player has played to its results and stopped (it plays once).
async function monsterEnded(page) {
    await monsterPlaying(page);
    await page.waitForFunction(() => {
        const host = document.querySelector('[data-cm-replay*="demo-app"]');
        const player = host && host.__cmPlayer;
        if (!player || !player.state) return false;
        const s = player.state();
        return !s.playing && !s.wanted && s.time >= s.duration && host.querySelector('.cm-screen').textContent.includes('Fleet check finished');
    }, null, { timeout: 90000, polling: 500 });
    await wait(500);
}

// Idle scenarios leave a page alone: the target is next to nothing when nothing plays.
function idle(url, extra) {
    return Object.assign({ url, idle: true, async run(page, options) { await wait(options.idleSeconds * 1000); } }, extra || {});
}

const scenarios = {
    widgets: {
        url: '/widgets/',
        async before(page) { await page.evaluate(() => window.scrollTo(0, 1800)); await wait(600); },
        async run(page) { await tierSwitches(page, 20, 350); }
    },
    tables: {
        url: '/tables/',
        async run(page) {
            const widths = await page.$$('[data-cm-widths] [data-width]');
            await widths[0].scrollIntoViewIfNeeded();
            for (let i = 0; i < 16; i++) { await press(widths[i % widths.length]); await wait(350); }
        }
    },
    home: {
        url: '/',
        async before() { await wait(1500); },
        async run(page) {
            for (let i = 0; i < 10; i++) {
                await press(await page.$(i % 2 === 0 ? '[data-cm-path="monster"]' : '[data-cm-path="everyday"]'));
                await wait(700);
            }
        }
    },
    'widgets-phone': {
        url: '/widgets/',
        viewport: { width: 400, height: 860 },
        mobile: true,
        async before(page) { await page.evaluate(() => window.scrollTo(0, 2400)); await wait(600); },
        async run(page) { await tierSwitches(page, 12, 400); }
    },
    docs: { url: '/docs/live-runs/', async run(page) { await wheel(page, 30, 350, 90); } },
    api: { url: '/api/consolemonster-widgets-table/', async run(page) { await wheel(page, 30, 400, 80); } },
    // At rest. The home page's Everyday hero (one player, in view) holds its final screen six seconds, plays once and stops;
    // everything else should cost about nothing.
    'idle-home': idle('/'),
    // The Monster hero plays once when opened (from the beginning to the results) and then stops: measured while it
    // plays, and at rest once it has ended, which must cost about nothing.
    'playing-home-monster': idle('/#monster', { async before(page) { await monsterPlaying(page); } }),
    'idle-home-monster': idle('/#monster', { async before(page) { await monsterEnded(page); } }),
    'idle-home-below': idle('/', { async before(page) { await wait(2500); await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight)); await wait(1000); } }),
    'idle-widgets': idle('/widgets/'),
    'idle-tables': idle('/tables/'),
    'idle-docs': idle('/docs/live-runs/'),
    'idle-api': idle('/api/consolemonster-widgets-table/')
};

function percentile(values, p) {
    if (!values.length) return 0;
    const sorted = values.slice().sort((a, b) => a - b);
    return sorted[Math.min(sorted.length - 1, Math.floor(p / 100 * sorted.length))];
}

const round = v => Math.round(v * 10) / 10;

function metrics(list) {
    const map = {};
    for (const m of list.metrics) map[m.name] = m.value;
    return map;
}

// CPU seconds per process type (renderer, GPU, browser, utility) from the browser's own process list.
async function processCpu(browserSession) {
    const info = await browserSession.send('SystemInfo.getProcessInfo');
    const sums = {};
    for (const p of info.processInfo) sums[p.type] = (sums[p.type] || 0) + p.cpuTime;
    return sums;
}

function cpuDelta(before, after, seconds) {
    const out = {};
    for (const type of Object.keys(after)) {
        const ms = ((after[type] || 0) - (before[type] || 0)) * 1000;
        out[type] = { ms: round(ms), percent: round(ms / (seconds * 10)) };
    }
    return out;
}

// Paint count and area and layout/style time from the timeline trace.
function summarizeTrace(events, viewportArea) {
    const sums = { paints: 0, paintMs: 0, paintArea: 0, largestPaint: 0, rasterMs: 0, layoutMs: 0, styleMs: 0, drawFrames: 0 };
    for (const e of events) {
        if (e.name === 'DrawFrame') { sums.drawFrames++; continue; }
        if (typeof e.dur !== 'number') continue;
        if (e.name === 'Paint') {
            sums.paints++;
            sums.paintMs += e.dur / 1000;
            const clip = e.args && e.args.data && e.args.data.clip;
            if (clip && clip.length >= 8) {
                const xs = [clip[0], clip[2], clip[4], clip[6]], ys = [clip[1], clip[3], clip[5], clip[7]];
                const area = (Math.max(...xs) - Math.min(...xs)) * (Math.max(...ys) - Math.min(...ys));
                sums.paintArea += area;
                sums.largestPaint = Math.max(sums.largestPaint, area);
            }
        } else if (e.name === 'RasterTask') sums.rasterMs += e.dur / 1000;
        else if (e.name === 'Layout') sums.layoutMs += e.dur / 1000;
        else if (e.name === 'UpdateLayoutTree') sums.styleMs += e.dur / 1000;
    }
    return {
        paints: sums.paints, paintMs: round(sums.paintMs), rasterMs: round(sums.rasterMs), layoutMs: round(sums.layoutMs), styleMs: round(sums.styleMs), drawFrames: sums.drawFrames,
        // Paint record area in viewports. A small layer records with a cull rect thousands of pixels wide, so this is a
        // hint for comparisons, not the area rasterised.
        paintViewports: round(sums.paintArea / viewportArea)
    };
}

async function readStream(session, handle) {
    const chunks = [];
    for (;;) {
        const chunk = await session.send('IO.read', { handle });
        chunks.push(chunk.base64Encoded ? Buffer.from(chunk.data, 'base64').toString('utf8') : chunk.data);
        if (chunk.eof) break;
    }
    await session.send('IO.close', { handle });
    return chunks.join('');
}

// The composited layer tree: count, the largest layer and the total area, in megapixels of CSS px. The document's own
// layer (the root scroller, as tall as the page and drawn in tiles near the viewport) is counted in the total but is not
// "the largest layer": that is the largest layer anything on the page created.
function layerStats(layers, rootNode) {
    const drawn = layers.filter(l => l.drawsContent && !l.invisible);
    let largest = null, total = 0;
    for (const l of drawn) {
        const area = l.width * l.height;
        total += area;
        if (rootNode && l.backendNodeId === rootNode) continue;
        if (!largest || area > largest.width * largest.height) largest = l;
    }
    return { count: layers.length, drawn: drawn.length, totalMP: round(total / 1e6), largest: largest ? { width: Math.round(largest.width), height: Math.round(largest.height), MP: round(largest.width * largest.height / 1e6) } : null };
}

async function describeLayers(session, layers, before, limit) {
    const paints = new Map(before.map(l => [l.layerId, l.paintCount || 0]));
    const rows = layers.filter(l => l.drawsContent && !l.invisible)
        .map(l => ({ l, repaints: (l.paintCount || 0) - (paints.get(l.layerId) || 0) }))
        .sort((a, b) => b.l.width * b.l.height - a.l.width * a.l.height)
        .slice(0, limit);
    const out = [];
    for (const { l, repaints } of rows) {
        let reasons = [];
        try { reasons = (await session.send('LayerTree.compositingReasons', { layerId: l.layerId })).compositingReasonIds || []; } catch (e) { /* gone */ }
        let node = '';
        if (l.backendNodeId) {
            try {
                const n = (await session.send('DOM.describeNode', { backendNodeId: l.backendNodeId })).node;
                const attrs = {};
                for (let i = 0; i + 1 < (n.attributes || []).length; i += 2) attrs[n.attributes[i]] = n.attributes[i + 1];
                node = (n.localName || n.nodeName) + (attrs.id ? '#' + attrs.id : '') + (attrs.class ? '.' + attrs.class.trim().split(/\s+/).join('.') : '');
            } catch (e) { /* gone */ }
        }
        out.push({ node, width: Math.round(l.width), height: Math.round(l.height), MP: round(l.width * l.height / 1e6), repaints, reasons });
    }
    return out;
}

async function measure(browser, browserSession, options, name) {
    const scenario = scenarios[name];
    const viewport = scenario.viewport || { width: options.width, height: options.height };
    const scale = scenario.mobile ? Math.max(2, options.scale) : options.scale;
    const context = await browser.newContext({ viewport, deviceScaleFactor: scale, isMobile: !!scenario.mobile, hasTouch: !!scenario.mobile, colorScheme: 'dark', reducedMotion: 'no-preference' });
    if (options.assets) {
        await context.route('**/themes/consolemonster/assets/*', async route => {
            const file = path.join(options.assets, path.basename(new URL(route.request().url()).pathname));
            if (!fs.existsSync(file)) return route.continue();
            await route.fulfill({ body: fs.readFileSync(file), contentType: file.endsWith('.js') ? 'text/javascript' : 'text/css' });
        });
    }
    const page = await context.newPage();
    await page.addInitScript(instrument);
    const session = await context.newCDPSession(page);
    await session.send('Performance.enable');
    await session.send('DOM.enable');
    if (options.cpu > 1) await session.send('Emulation.setCPUThrottlingRate', { rate: options.cpu });
    let layers = [];
    let rootNode = 0;
    let peak = { count: 0, totalMP: 0, largestMP: 0 };
    session.on('LayerTree.layerTreeDidChange', event => {
        if (!event.layers) return;
        layers = event.layers;
        const stats = layerStats(layers, rootNode);
        peak = { count: Math.max(peak.count, stats.count), totalMP: Math.max(peak.totalMP, stats.totalMP), largestMP: Math.max(peak.largestMP, stats.largest ? stats.largest.MP : 0) };
    });
    await session.send('LayerTree.enable');
    await page.goto(options.base.replace(/\/$/, '') + scenario.url, { waitUntil: 'load' });
    rootNode = (await session.send('DOM.getDocument', { depth: 0 })).root.backendNodeId;
    await wait(1200);
    if (scenario.before) await scenario.before(page);
    await wait(300);
    const layersBefore = layers.slice();
    peak = { count: 0, totalMP: 0, largestMP: 0 };
    const before = metrics(await session.send('Performance.getMetrics'));
    await session.send('Tracing.start', { categories: 'devtools.timeline,disabled-by-default-devtools.timeline,disabled-by-default-devtools.timeline.frame', transferMode: 'ReturnAsStream' });
    // CPU is read inside the traced window, so it includes the (small, constant) cost of tracing and the layer tree.
    const cpuBefore = await processCpu(browserSession);
    const traceDone = new Promise(resolve => session.once('Tracing.tracingComplete', resolve));
    await page.evaluate(loop => { window.__cmPerf.start(loop); }, !scenario.idle);
    const started = Date.now();
    await scenario.run(page, options);
    if (!scenario.idle) await wait(500);
    const seconds = (Date.now() - started) / 1000;
    const sample = await page.evaluate(() => { const p = window.__cmPerf; p.running = false; return { frames: p.frames, loaf: p.loaf, tasks: p.tasks, raf: p.raf, timeout: p.timeout, interval: p.interval, heap: performance.memory ? performance.memory.usedJSHeapSize : 0,
        // Terminal screens exposed cell by cell to assistive technology (each should be one role=img with a name).
        cellScreens: document.querySelectorAll('pre.cm-screen:not([role=img]):not([aria-hidden=true])').length }; });
    const cpuAfter = await processCpu(browserSession);
    await session.send('Tracing.end');
    const raw = await readStream(session, (await traceDone).stream);
    let events = [];
    try { const parsed = JSON.parse(raw); events = Array.isArray(parsed) ? parsed : parsed.traceEvents || []; } catch (e) { /* a truncated trace leaves the trace numbers empty */ }
    if (options.trace) fs.writeFileSync(path.join(options.out, name + '.trace.json'), raw);
    const after = metrics(await session.send('Performance.getMetrics'));
    await wait(300);
    const finalLayers = layers.slice();
    const stats = layerStats(finalLayers, rootNode);
    const top = await describeLayers(session, finalLayers, layersBefore, options.layers ? 25 : 6);
    await context.close();
    const loafs = sample.loaf.filter(l => l.d >= 50);
    return {
        scenario: name,
        url: scenario.url,
        viewport: viewport.width + 'x' + viewport.height + '@' + scale,
        seconds: round(seconds),
        frameP50: round(percentile(sample.frames, 50)),
        frameP95: round(percentile(sample.frames, 95)),
        frameMax: round(sample.frames.length ? Math.max(...sample.frames) : 0),
        longAnimationFrames: loafs.length,
        longAnimationFrameMs: round(loafs.reduce((s, l) => s + l.d, 0)),
        longTasks: sample.tasks.length,
        cellScreens: sample.cellScreens,
        pageCalls: { raf: sample.raf, timeout: sample.timeout, interval: sample.interval },
        cpu: cpuDelta(cpuBefore, cpuAfter, seconds),
        heapMB: round(sample.heap / 1048576),
        layoutCount: after.LayoutCount - before.LayoutCount,
        trace: summarizeTrace(events, viewport.width * viewport.height),
        layers: { count: stats.count, drawn: stats.drawn, totalMP: stats.totalMP, largest: stats.largest, maxCount: Math.max(peak.count, stats.count), maxTotalMP: Math.max(peak.totalMP, stats.totalMP), maxLargestMP: Math.max(peak.largestMP, stats.largest ? stats.largest.MP : 0), top }
    };
}

function withinBudget(result, budget) {
    const limit = Object.assign({}, budget.default || {}, budget[result.scenario] || {});
    const problems = [];
    const check = (value, max, label) => { if (max !== undefined && value > max) problems.push(label + ' ' + value + ' > ' + max); };
    check(result.frameP95, limit.frameP95, 'p95 frame ms');
    check(result.cellScreens, limit.cellScreens, 'terminal screens exposed cell by cell');
    check(result.longAnimationFrames, limit.longAnimationFrames, 'long animation frames');
    check(result.layers.maxCount, limit.layers, 'composited layers');
    check(result.layers.maxLargestMP, limit.largestLayerMP, 'largest layer MP');
    check(result.layers.maxTotalMP, limit.totalLayerMP, 'total layer MP');
    check(round(result.pageCalls.raf / result.seconds), limit.rafPerSecond, 'requestAnimationFrame calls/s');
    check(round((result.pageCalls.timeout + result.pageCalls.interval) / result.seconds), limit.timersPerSecond, 'timer calls/s');
    check(result.cpu.renderer ? result.cpu.renderer.percent : 0, limit.rendererCpuPercent, 'renderer CPU %');
    check(result.cpu.browser ? result.cpu.browser.percent : 0, limit.browserCpuPercent, 'browser process CPU %');
    check(result.cpu.GPU ? result.cpu.GPU.percent : 0, limit.gpuCpuPercent, 'GPU process CPU %');
    return problems;
}

function line(result, problems) {
    const cpu = type => result.cpu[type] ? result.cpu[type].percent + '%' : '-';
    const L = result.layers;
    return [result.scenario.padEnd(18), 'p95 ' + result.frameP95, 'max ' + result.frameMax, 'LoAF ' + result.longAnimationFrames + '/' + result.longAnimationFrameMs + 'ms',
        'cpu r ' + cpu('renderer') + ' g ' + cpu('GPU') + ' b ' + cpu('browser'),
        'raf ' + result.pageCalls.raf + ' t ' + (result.pageCalls.timeout + result.pageCalls.interval),
        'paints ' + result.trace.paints + ' (' + result.trace.paintMs + 'ms)',
        'layout ' + result.trace.layoutMs + ' style ' + result.trace.styleMs,
        'layers ' + L.count + '/' + L.maxCount + ' largest ' + L.maxLargestMP + 'MP total ' + L.maxTotalMP + 'MP',
        problems.length ? 'OVER BUDGET: ' + problems.join('; ') : ''].join('  ');
}

async function main() {
    const options = args();
    fs.mkdirSync(options.out, { recursive: true });
    const budget = options.budget ? JSON.parse(fs.readFileSync(options.budget, 'utf8')) : null;
    const { chromium } = load();
    const names = Object.keys(scenarios).filter(n => options.only ? options.only.includes(n) : true);
    const results = [];
    let failed = false;
    // One browser at a time, closed after each scenario, so nothing from one run carries into the next.
    for (const name of names) {
        let result = null;
        let problems = [];
        for (let attempt = 1; attempt <= (budget ? options.attempts : 1); attempt++) {
            const browser = await chromium.launch(Object.assign({ headless: !options.headed, args: ['--force-renderer-accessibility'] }, options.channel ? { channel: options.channel } : {}));
            try {
                const browserSession = await browser.newBrowserCDPSession();
                result = await measure(browser, browserSession, options, name);
                problems = budget ? withinBudget(result, budget) : [];
            } catch (error) {
                // A scenario that cannot run (a missing control, a page that fails to load) is a failure, recorded with the rest.
                result = { scenario: name, url: scenarios[name].url, error: String(error && error.stack || error) };
                problems = ['scenario failed: ' + String(error && error.message || error)];
            } finally {
                await browser.close();
            }
            result.attempt = attempt;
            if (!problems.length) break;
        }
        result.problems = problems;
        if (problems.length) failed = true;
        results.push(result);
        console.log(result.error ? name.padEnd(18) + '  FAILED: ' + problems.join('; ') : line(result, problems));
    }
    fs.writeFileSync(path.join(options.out, 'results.json'), JSON.stringify({ base: options.base, assets: options.assets, channel: options.channel || 'chromium', viewport: options.width + 'x' + options.height + '@' + options.scale, cpuThrottle: options.cpu, date: new Date().toISOString(), results }, null, 2));
    if (failed) {
        console.error('Rendering cost is over budget; see ' + path.join(options.out, 'results.json'));
        process.exit(1);
    }
}

main().catch(error => { console.error(error); process.exit(2); });
