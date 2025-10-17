<template>
  <div class="error-page">
    <i class="pi pi-exclamation-triangle error-icon"></i>
    <h2>Ошибка</h2>
    <p>{{ message }}</p>
    <Button label="На главную" icon="pi pi-home" @click="goHome" />
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import Button from 'primevue/button'

defineProps<{
  message?: string
}>()

const router = useRouter()

function goHome() {
  // Возвращаемся в родительское окно или на главную
  if (window.parent && window.parent !== window) {
    window.parent.postMessage('CloseEditor', '*')
  } else {
    router.push('/')
  }
}
</script>

<style scoped>
.error-page {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  padding: 40px;
  text-align: center;
}

.error-icon {
  font-size: 64px;
  color: var(--p-red-500);
  margin-bottom: 20px;
}

h2 {
  color: var(--p-text-color);
  margin-bottom: 10px;
}

p {
  color: var(--p-text-color-secondary);
  margin-bottom: 30px;
  max-width: 500px;
}
</style>
