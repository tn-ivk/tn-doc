import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import Tooltip from 'primevue/tooltip'
import App from './App.vue'
import type { FormEditorConfig } from './types/field.types'

// Расширяем интерфейс Window для глобальной функции
declare global {
  interface Window {
    initFormEditor: (config: FormEditorConfig) => void
  }
}

// Глобальная функция инициализации для вызова из C# кода
window.initFormEditor = (config: FormEditorConfig) => {
  console.log('Инициализация Form Editor с конфигурацией:', config)

  const app = createApp(App, { config })
  const pinia = createPinia()

  app.use(pinia)
  app.use(PrimeVue, {
    theme: {
      preset: Aura
    }
  })
  app.directive('tooltip', Tooltip)

  app.mount('#form-editor-root')

  console.log('Form Editor успешно смонтирован')
}
