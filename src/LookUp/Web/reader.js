// Injected into every top-level Cambridge page. Reports what kind of page loaded and,
// for entry pages, reduces the page to the entry plus its source line.
// All Cambridge-specific selectors live here so a site redesign only touches this file.
(() => {
  if (window !== window.top || location.hostname !== 'dictionary.cambridge.org') return;

  const post = msg => window.chrome.webview.postMessage(msg);
  const SVG = 'http://www.w3.org/2000/svg';

  function svg(tag, attributes = {}) {
    const el = document.createElementNS(SVG, tag);
    for (const [name, value] of Object.entries(attributes)) el.setAttribute(name, value);
    return el;
  }

  // Registration mark for the plate's top corners.
  function crosshair(className) {
    const mark = svg('svg', { class: `lu-cross ${className}`, viewBox: '0 0 18 18', 'aria-hidden': 'true' });
    mark.append(svg('path', { d: 'M9 0V18M0 9H18' }), svg('circle', { cx: 9, cy: 9, r: 4.5 }));
    return mark;
  }

  // The notebook seal, stamped half over the plate's top-right corner. Called by the app:
  // __lookupSeal({ category, date, count, animate }) to stamp, __lookupSeal(null) to lift it.
  window.__lookupSeal = seal => {
    const root = document.getElementById('lu-root');
    if (!root) return;
    root.querySelector('.lu-seal')?.remove();
    root.classList.toggle('lu-sealed', !!seal);
    if (!seal) return;

    const stamp = svg('svg', { class: 'lu-seal', viewBox: '0 0 92 92', role: 'img',
                               'aria-label': `In notebook: ${seal.category}` });
    const ring = svg('path', { id: 'lu-seal-ring', d: 'M46 46 m-34 0 a34 34 0 1 1 68 0 a34 34 0 1 1 -68 0', fill: 'none' });
    const text = svg('text', { class: 'ring' });
    // textLength spreads or tightens the letters so the ring always closes on itself.
    const path = svg('textPath', { href: '#lu-seal-ring', startOffset: '0', textLength: 211, lengthAdjust: 'spacing' });
    const category = seal.category.length > 14 ? `${seal.category.slice(0, 13)}…` : seal.category;
    path.textContent = `Notebook · ${category} · ${seal.date} · `;
    text.append(path);
    const count = svg('text', { class: 'count', x: 46, y: 54 });
    count.textContent = String(Math.max(seal.count, 1)).padStart(2, '0');
    stamp.append(svg('circle', { cx: 46, cy: 46, r: 45 }), ring, text, count);
    if (seal.animate) stamp.classList.add('stamp');
    root.append(stamp);
  };

  function apply() {
    if (window._cf_chl_opt) {
      post({ type: 'challenge' });
      return;
    }

    if (location.pathname.startsWith('/spellcheck/')) {
      const suggestions = [...document.querySelectorAll('.hul-u a[href*="/direct/?q="]')]
        .map(a => a.textContent.trim())
        .filter(Boolean)
        .slice(0, 8);
      post({ type: 'noresult', suggestions });
      return;
    }

    const entry = document.querySelector('.di-body .entry-body');
    if (!entry) {
      post({ type: 'other' });
      return;
    }

    const root = document.createElement('main');
    root.id = 'lu-root';
    root.append(crosshair('lu-cross-l'), crosshair('lu-cross-r'), entry);
    const source = document.querySelector('.definition-src');
    if (source) root.append(source);
    document.body.append(root);

    const style = document.createElement('style');
    style.textContent = __LU_CSS__;
    document.head.append(style);
    document.documentElement.classList.add('lu-reader');

    // A phrase search can land on the head word's entry ("be subject to" → subject):
    // point at the matching phrase instead of the top of the entry.
    const phrase = findPhrase(entry);
    if (phrase) phrase.classList.add('lu-match');
    holdScroll(phrase);

    post({ type: 'entry', summary: phrase ? summarizePhrase(entry, phrase) : summarize(entry) });
  }

  function findPhrase(entry) {
    const query = (new URLSearchParams(location.search).get('q') || '').toLowerCase().trim();
    const words = query.split(/\s+/);
    if (words.length < 2) return null;

    for (const block of entry.querySelectorAll('.phrase-block')) {
      const title = textOf(block.querySelector('.phrase-title')).toLowerCase();
      const titleWords = title.split(/[\s()/,]+/);
      if (title.includes(query) || words.every(w => titleWords.includes(w))) return block;
    }
    return null;
  }

  // Page scripts may scroll after load. Hold the start position until the user scrolls.
  function holdScroll(target) {
    history.scrollRestoration = 'manual';
    let userMoved = false;
    for (const type of ['wheel', 'keydown', 'mousedown', 'touchstart'])
      addEventListener(type, () => { userMoved = true; }, { capture: true, once: true });
    const pin = () => {
      if (!userMoved) window.scrollTo(0, target ? target.getBoundingClientRect().top + window.scrollY - 16 : 0);
    };
    pin();
    addEventListener('load', pin);
    for (const ms of [150, 400, 800, 1500]) setTimeout(pin, ms);
  }

  const textOf = el => el ? el.textContent.replace(/\s+/g, ' ').trim() : '';

  // First sense of the entry, for the notebook.
  function summarize(entry) {
    const text = selector => textOf(entry.querySelector(selector));
    return {
      headword: text('.headword'),
      partOfSpeech: text('.posgram .pos') || text('.pos'),
      ipa: text('.uk .ipa') || text('.ipa'),
      chinese: text('.def-body > .trans'),
      definition: text('.def.ddef_d').replace(/:$/, ''),
      example: text('.examp .eg'),
      exampleChinese: text('.examp .trans'),
      sourceUrl: location.origin + location.pathname,
    };
  }

  function summarizePhrase(entry, phrase) {
    const text = selector => textOf(phrase.querySelector(selector));
    return {
      ...summarize(entry),
      headword: text('.phrase-title'),
      partOfSpeech: 'phrase',
      chinese: text('.def-body > .trans'),
      definition: text('.def.ddef_d').replace(/:$/, ''),
      example: text('.examp .eg'),
      exampleChinese: text('.examp .trans'),
    };
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', apply, { once: true });
  else apply();
})();
