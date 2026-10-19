/* Author: OpenAI-Codex. Apply only validated preferences; dark and English are intentional defaults. */
(() => {
  let theme = 'dark';
  let language = 'en';
  try {
    const savedTheme = localStorage.getItem('cs1-modpack-theme');
    const savedLanguage = localStorage.getItem('cs1-modpack-language');
    if (savedTheme === 'light' || savedTheme === 'dark') theme = savedTheme;
    if (savedLanguage === 'en' || savedLanguage === 'zh-Hant') language = savedLanguage;
  } catch { /* Preferences remain usable when storage is unavailable. */ }
  document.documentElement.dataset.theme = theme;
  document.documentElement.dataset.language = language;
})();
