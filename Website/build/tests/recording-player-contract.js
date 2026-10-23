/* Browser contract for an exported standalone recording at 1440x900.
 * Run verifyRecordingPlayer() through HtmlTinkerX or another browser test host after navigating to the export.
 * Repeat in light/dark; use { lateFontUrl: '/fonts/jetbrains-mono-latin.woff2' } to check a font arriving after mount,
 * and { fullWidth: true } for an exported 88-column line ending in Z. No browser installation or launch lives here.
 */
async function verifyRecordingPlayer(options = {}) {
    const assert = (condition, message) => { if (!condition) throw new Error(message); };
    assert(innerWidth === 1440 && innerHeight === 900, 'Run the desktop contract at 1440x900');
    const paint = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    await document.fonts.ready;
    const host = document.querySelector('.cm-standalone');
    assert(host && host.__cmPlayer, 'Standalone player must be mounted');
    const player = host.__cmPlayer;
    player.pause();
    const data = JSON.parse(document.querySelector(host.getAttribute('data-cm-recording')).textContent);
    const screen = host.querySelector('.cm-screen');
    if (options.lateFontUrl) {
        const face = new FontFace('RecordingContractFont', `url(${options.lateFontUrl})`);
        document.fonts.add(face);
        screen.style.fontFamily = 'RecordingContractFont, monospace';
        await document.fonts.load(`${getComputedStyle(screen).fontSize} RecordingContractFont`);
        await document.fonts.ready;
    }
    await paint();
    const css = getComputedStyle(screen);
    assert(parseFloat(css.fontSize) >= 13, 'Desktop demo text must be at least 13px');
    const rowHasText = data.rows.map(html => {
        const row = document.createElement('span');
        row.innerHTML = html;
        return row.textContent.trim().length > 0;
    });
    let tallest = 1;
    for (const frame of data.frames) {
        frame[1].forEach((row, y) => { if (rowHasText[row]) tallest = Math.max(tallest, y + 1); });
    }
    assert(data.height === tallest, `Export has ${data.height} rows, tallest state uses ${tallest}`);
    const inset = parseFloat(css.paddingTop) + parseFloat(css.paddingBottom);
    const expectedHeight = tallest * parseFloat(css.lineHeight) + inset;
    let rightmostCells = 0, fullWidthLines = 0, progressFrames = 0;
    let maxScrollWidth = 0;
    for (const frame of data.frames) {
        player.seek(frame[0]);
        await paint();
        assert(screen.scrollWidth <= screen.clientWidth,
            `Overflow at ${frame[0]}ms: ${screen.scrollWidth} > ${screen.clientWidth}`);
        assert(Math.abs(screen.clientHeight - expectedHeight) <= 1,
            `Unexpected terminal height at ${frame[0]}ms: ${screen.clientHeight} != ${expectedHeight}`);
        assert(host.querySelector('.cm-scroll-hint').hidden, `Desktop scroll hint at ${frame[0]}ms`);
        const right = screen.getBoundingClientRect().right - parseFloat(css.paddingRight);
        for (const line of screen.querySelectorAll('.cm-line')) {
            const text = line.textContent;
            if (!text.trim()) continue;
            if (/Step \d+ of 20/.test(text)) progressFrames++;
            if (text.length === data.width && text.endsWith('Z')) fullWidthLines++;
            const walker = document.createTreeWalker(line, NodeFilter.SHOW_TEXT);
            let last = null, node;
            while ((node = walker.nextNode())) { if (node.textContent.length) last = node; }
            assert(last, 'Visible line has no text node');
            const range = document.createRange();
            range.setStart(last, last.textContent.length - 1);
            range.setEnd(last, last.textContent.length);
            assert(range.getBoundingClientRect().right <= right + 0.05,
                `Rightmost cell is clipped at ${frame[0]}ms: ${text}`);
            rightmostCells++;
        }
        maxScrollWidth = Math.max(maxScrollWidth, screen.scrollWidth);
    }
    if (options.fullWidth) assert(fullWidthLines > 0, 'Fixture must contain a full-width recorded line ending in Z');
    else assert(progressFrames > 0, 'Real demo must exercise its full-width progress line');
    return {
        frames: data.frames.length, rightmostCells, fullWidthLines, progressFrames, tallestRows: tallest,
        font: css.fontSize, terminalWidth: screen.clientWidth, maxScrollWidth, terminalHeight: screen.clientHeight,
        lateFont: !!options.lateFontUrl
    };
}
