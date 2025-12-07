<script setup lang="ts">
import type { PassportEditConfig } from '@/types/passport-config.types';

interface Props {
  config: PassportEditConfig;
  configPath: string;
}

interface Emits {
  (e: 'update:config', config: PassportEditConfig): void;
}

defineProps<Props>();
defineEmits<Emits>();

// TODO: Реализация на этапе 3
</script>

<template>
  <div class="passport-config-editor">
    <p>Визуальный редактор паспорта (в разработке)</p>
  </div>
</template>

<style scoped>
.passport-config-editor {
  padding: 1rem;
}
</style>
