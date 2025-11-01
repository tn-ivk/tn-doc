import { logger } from '@tn-doc/shared';
<template>
  <!-- Редактируемая ячейка результата -->
  <InputText
    v-if="isEditable"
    :modelValue="parameter.values.result"
    :class="{ 'elis-filled': parameter.elisFlags.result }"
    type="text"
    class="result-cell-input"
    @update:modelValue="handleValueChange"
  />

  <!-- Нередактируемая ячейка результата (просто текст) -->
  <span
    v-else
    :class="{ 'elis-filled-text': parameter.elisFlags.result }"
    class="result-cell-readonly"
  >
    {{ displayValue }}
  </span>
</template>

<script setup lang="ts">
import { logger } from '@tn-doc/shared';
import { computed } from 'vue';
import InputText from 'primevue/inputtext';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
  isEditable: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:result': [value: string];
}>();

const displayValue = computed(() => {
  if (!props.parameter.values.result) return '-';
  return props.parameter.values.result.replace('.', ',');
});

function handleValueChange(value: string | undefined) {
  const stringValue = value ?? '';
  emit('update:result', stringValue);
  logger.debug(`[PassportResultCell] Result изменено: ${props.parameter.key} -> ${stringValue}`);
}
</script>

<style scoped>
.result-cell-input {
  width: 100%;
  border: none;
  background: transparent;
  text-align: center;
  font-family: inherit;
  font-size: 15px;
  padding: 2px;
  outline: none;
  box-sizing: border-box;
  margin: 0;
}

.result-cell-input:focus {
  outline: none;
  background: #f8f9fa;
}

.result-cell-readonly {
  display: block;
  width: 100%;
  text-align: center;
  font-size: 15px;
}

/* ELIS подсветка */
.elis-filled {
  background-color: #8fd19e !important;
}

.elis-filled-text {
  background-color: #8fd19e;
  display: inline-block;
  width: 100%;
  padding: 2px;
}
</style>
