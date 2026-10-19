/* Author: OpenAI-Codex. Accessible controls without third-party dependencies. */
(() => {
  const html = document.documentElement;
  const languageControl = document.querySelector('#language');
  const themeControl = document.querySelector('#theme');
  const save = (key, value) => { try { localStorage.setItem(key, value); } catch { /* Session-only fallback. */ } };
  function updateThemeControl() {
    const light = html.dataset.theme === 'light';
    const label = html.lang === 'zh-Hant' ? (light ? '深色模式' : '淺色模式') : (light ? 'Dark mode' : 'Light mode');
    themeControl.textContent = label;
    themeControl.setAttribute('aria-label', html.lang === 'zh-Hant' ? `切換至${label}` : `Switch to ${label.toLowerCase()}`);
  }
  function localize(language) {
    html.lang = language;
    html.dataset.language = language;
    const chinese = language === 'zh-Hant';
    document.querySelectorAll('[data-en][data-zh]').forEach(el => { el.textContent = chinese ? el.dataset.zh : el.dataset.en; });
    document.querySelectorAll('[data-en-aria][data-zh-aria]').forEach(el => { el.setAttribute('aria-label', chinese ? el.dataset.zhAria : el.dataset.enAria); });
    document.title = `${chinese ? document.body.dataset.titleZh : document.body.dataset.titleEn} | CS1 Modpack`;
    document.querySelector('meta[name="description"]').content = chinese ? '探索 Cities: Skylines 1 模組集：AI Improve、自動公共運輸規劃與 AI Gaming。' : 'Explore the Cities: Skylines 1 Modpack: AI Improve, Automated Public Transport Planning, and AI Gaming.';
    languageControl.value = language;
    updateThemeControl();
  }
  languageControl.addEventListener('change', () => { localize(languageControl.value); save('cs1-modpack-language', languageControl.value); });
  themeControl.addEventListener('click', () => {
    html.dataset.theme = html.dataset.theme === 'dark' ? 'light' : 'dark';
    save('cs1-modpack-theme', html.dataset.theme);
    updateThemeControl();
  });
  localize(html.dataset.language || 'en');
  const tabs = [...document.querySelectorAll('[data-tab]')];
  const panels = [...document.querySelectorAll('[data-panel]')];
  if (tabs.length) {
    document.querySelector('.project-tabs').setAttribute('role', 'tablist');
    function activate(index, focus = false) {
      tabs.forEach((tab, i) => {
        tab.setAttribute('role', 'tab');
        tab.setAttribute('aria-selected', String(i === index));
        tab.tabIndex = i === index ? 0 : -1;
        panels[i].setAttribute('role', 'tabpanel');
        panels[i].setAttribute('aria-labelledby', tab.id);
        panels[i].tabIndex = 0;
        panels[i].hidden = i !== index;
      });
      if (focus) tabs[index].focus();
    }
    tabs.forEach((tab, index) => {
      tab.addEventListener('click', () => activate(index));
      tab.addEventListener('keydown', event => {
        let next;
        if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
        if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
        if (event.key === 'Home') next = 0;
        if (event.key === 'End') next = tabs.length - 1;
        if (next !== undefined) { event.preventDefault(); activate(next, true); }
      });
    });
    activate(0);
  }
  document.querySelectorAll('[data-enhance]').forEach(el => { el.hidden = false; });
  if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
    const entrance = (el, index) => {
      el.style.setProperty('--arrival-delay', `${index % 4 * 70}ms`);
      el.classList.add('arrive');
    };
    document.querySelectorAll('.hero-copy > *, .hero-art').forEach(entrance);
    if ('IntersectionObserver' in window) {
      const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
          if (!entry.isIntersecting) return;
          [...entry.target.children].forEach(entrance);
          observer.unobserve(entry.target);
        });
      }, { threshold: 0.1 });
      document.querySelectorAll('.intro-section, .section-heading, .documentation, .site-footer, .project-overview, .project-resources').forEach(el => observer.observe(el));
    }
  }
})();
