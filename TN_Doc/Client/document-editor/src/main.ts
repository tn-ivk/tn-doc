import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import router from './router'
import App from './App.vue'

// Создаем приложение
const app = createApp(App)

// Pinia store
const pinia = createPinia()
app.use(pinia)

// Vue Router
app.use(router)

// PrimeVue с темой Aura
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      prefix: 'p',
      darkModeSelector: '.dark-mode',
      cssLayer: false
    }
  }
})

// Монтируем приложение
app.mount('#app')

console.log('Document Editor mounted successfully')
