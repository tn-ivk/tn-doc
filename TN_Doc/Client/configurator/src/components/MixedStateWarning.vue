<template>
  <div class="mixed-state-warning">
    <i class="pi pi-exclamation-triangle"></i>
    <span>У выбранных устройств разные значения</span>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.mixed-state-warning {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  background-color: var(--yellow-50);
  border: 1px solid var(--yellow-200);
  border-radius: 4px;
  color: var(--yellow-900);
  font-size: 0.875rem;
}

.mixed-state-warning i {
  color: var(--yellow-600);
}
</style>
