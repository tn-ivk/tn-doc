<template>
  <div class="error-page">
    <Message severity="error" :closable="false">
      <h2>Ошибка</h2>
      <p>{{ errorMessage }}</p>
    </Message>
    <Button label="На главную" icon="pi pi-home" @click="goHome" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Message from 'primevue/message';
import Button from 'primevue/button';

const route = useRoute();
const router = useRouter();

const errorMessage = computed(() => {
  return (route.query.message as string) || 'Произошла неизвестная ошибка';
});

function goHome() {
  window.history.back();
}
</script>

<style scoped>
.error-page {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  padding: 2rem;
  gap: 2rem;
}

.error-page h2 {
  margin: 0 0 0.5rem 0;
}

.error-page p {
  margin: 0;
}
</style>
