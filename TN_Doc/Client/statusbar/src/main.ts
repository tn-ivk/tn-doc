import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';

/**
 * Инициализация StatusBar приложения
 * Интегрируется в существующую страницу TN_Doc
 */
function initStatusBar() {
  const container = document.getElementById('status-bar');

  if (!container) {
    console.warn('[StatusBar] Container element not found. Status bar will not be initialized.');
    return;
  }

  try {
    const app = createApp(App);
    const pinia = createPinia();

    app.use(pinia);
    app.mount(container);

    console.info('[StatusBar] Application initialized successfully');
  } catch (error) {
    console.error('[StatusBar] Failed to initialize:', error);
  }
}

// Wait for DOM to be ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initStatusBar);
} else {
  initStatusBar();
}