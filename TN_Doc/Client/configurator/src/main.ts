import { createApp } from 'vue';
import { createPinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import { definePreset } from '@primevue/themes';
import ToastService from 'primevue/toastservice';
import { logger } from '@tn-doc/shared';
import App from './App.vue';

import 'primeicons/primeicons.css';

// Устанавливаем глобальный контекст для всех логов конфигуратора
logger.setGlobalContext({
  app: 'Configurator'
});

logger.info('Configurator: инициализация приложения');

// Глобальный перехват необработанных JS-ошибок
window.onerror = (message, source, lineno, colno, error) => {
  logger.error('Configurator: глобальная ошибка JS', {
    message: String(message),
    source,
    lineno,
    colno,
    stack: error?.stack
  });
};

// Глобальный перехват необработанных Promise rejection
window.onunhandledrejection = (event: PromiseRejectionEvent) => {
  const reason = event.reason;
  logger.error('Configurator: необработанный Promise rejection', {
    message: reason?.message || String(reason),
    stack: reason?.stack
  });
};

const app = createApp(App);

// Перехват ошибок рендеринга Vue-компонентов
app.config.errorHandler = (err, instance, info) => {
  const error = err instanceof Error ? err : new Error(String(err));
  const componentName = instance?.$options?.name || instance?.$options?.__name || 'Unknown';
  logger.error(`Configurator: ошибка Vue [${componentName}] ${info}`, {
    message: error.message,
    stack: error.stack,
    component: componentName,
    lifecycleHook: info
  });
};

// Перехват предупреждений Vue (полезно для диагностики проблем с компонентами)
app.config.warnHandler = (msg, instance, trace) => {
  const componentName = instance?.$options?.name || instance?.$options?.__name || 'Unknown';
  logger.warn(`Configurator: предупреждение Vue [${componentName}]`, {
    message: msg,
    component: componentName,
    trace
  });
};

const CustomPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{blue.50}',
      100: '{blue.100}',
      200: '{blue.200}',
      300: '{blue.300}',
      400: '{blue.400}',
      500: '{blue.500}',
      600: '{blue.600}',
      700: '{blue.700}',
      800: '{blue.800}',
      900: '{blue.900}',
      950: '{blue.950}'
    }
  }
});

app.use(createPinia());
app.use(PrimeVue, {
  theme: {
    preset: CustomPreset,
    options: {
      darkModeSelector: false
    }
  }
});
app.use(ToastService);

logger.info('Configurator: монтирование приложения');
app.mount('#app');
logger.info('Configurator: приложение смонтировано');
