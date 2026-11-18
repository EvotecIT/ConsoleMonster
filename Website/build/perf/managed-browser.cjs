'use strict';

const { execFile, spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { setTimeout: wait } = require('node:timers/promises');
const { promisify } = require('node:util');
const { chromium } = require('playwright-core');

const run = promisify(execFile);
const isWindows = process.platform === 'win32';

async function killHostTree(pid) {
    if (isWindows) {
        await run('taskkill.exe', ['/PID', String(pid), '/T', '/F'], { windowsHide: true, timeout: 5000 });
    } else {
        process.kill(-pid, 'SIGKILL');
    }
}

async function openBrowser({ channel, headed }) {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'consolemonster-perf-browser-'));
    const argumentsForHost = [
        '-NoLogo', '-NoProfile', '-NonInteractive',
        '-File', path.join(__dirname, 'Start-PerfBrowser.ps1'),
        '-ProfileDirectory', profile
    ];
    if (channel) argumentsForHost.push('-Channel', channel);
    if (headed) argumentsForHost.push('-Visible');

    const host = spawn('pwsh', argumentsForHost, {
        stdio: ['pipe', 'inherit', 'inherit'], windowsHide: true, detached: !isWindows
    });
    // An early host exit can close the pipe before the client finishes connecting.
    host.stdin.on('error', () => {});
    let result;
    const exited = new Promise(resolve => {
        host.once('error', error => { result = { error }; resolve(result); });
        host.once('close', (code, signal) => { result = { code, signal }; resolve(result); });
    });

    async function stopHost() {
        host.stdin.end();
        try {
            const graceful = await Promise.race([exited, wait(10000, null, { ref: false })]);
            if (graceful) return graceful;
            let terminationError;
            try {
                await killHostTree(host.pid);
            } catch (error) {
                terminationError = error;
            }
            const terminated = await Promise.race([exited, wait(5000, null, { ref: false })]);
            if (!terminated) {
                throw terminationError ?? new Error('HtmlTinkerX browser host did not exit after termination.');
            }
            return terminated;
        } finally {
            if (result) fs.rmSync(profile, { recursive: true, force: true, maxRetries: 3, retryDelay: 100 });
        }
    }

    try {
        const portFile = path.join(profile, 'managed-browser-port');
        const deadline = Date.now() + 180000;
        while (Date.now() < deadline) {
            if (result) throw result.error || new Error(`HtmlTinkerX browser host exited before connecting (${result.code ?? result.signal}).`);
            if (fs.existsSync(portFile)) {
                const port = Number(fs.readFileSync(portFile, 'utf8'));
                if (Number.isInteger(port) && port > 0 && port <= 65535) {
                    const browser = await chromium.connectOverCDP(`http://127.0.0.1:${port}`);
                    return {
                        browser,
                        async close() {
                            try {
                                await browser.close();
                            } finally {
                                const state = await stopHost();
                                if (state.error || state.code !== 0) {
                                    throw state.error || new Error(`HtmlTinkerX browser host failed during shutdown (${state.code ?? state.signal}).`);
                                }
                            }
                        }
                    };
                }
            }
            await wait(100);
        }
        throw new Error('Timed out waiting for the HtmlTinkerX browser host.');
    } catch (error) {
        try {
            await stopHost();
        } catch (cleanupError) {
            throw new AggregateError([error, cleanupError], error.message);
        }
        throw error;
    }
}

module.exports = { openBrowser };
