// Injected into every top-level Cambridge page. Reports what kind of page loaded and,
// for entry pages, reduces the page to the entry plus its source line.
// All Cambridge-specific selectors live here so a site redesign only touches this file.
(() => {
  if (window !== window.top || location.hostname !== 'dictionary.cambridge.org') return;

  const post = msg => window.chrome.webview.postMessage(msg);

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
    root.append(entry);
    const source = document.querySelector('.definition-src');
    if (source) root.append(source);
    document.body.append(root);

    const style = document.createElement('style');
    style.textContent = __LU_CSS__;
    document.head.append(style);
    document.documentElement.classList.add('lu-reader');
    window.scrollTo(0, 0);

    post({ type: 'entry' });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', apply, { once: true });
  else apply();
})();
