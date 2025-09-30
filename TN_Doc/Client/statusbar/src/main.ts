import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}

function initApp() {
  const container = document.getElementById('status-bar');
  if (!container) {
    console.warn('Status bar container not found');
    return;
  }
  const app = createApp(App);
  const pinia = createPinia();
  app.use(pinia);
  app.mount(container);
}


