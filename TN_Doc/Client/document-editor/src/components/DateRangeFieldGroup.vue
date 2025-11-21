<template>
  <div class="date-range-group">
    <!-- Начало периода -->
    <div class="date-field date-begin">
      <FormFieldWithHistory
        :field="beginField"
        :modelValue="beginValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:begin', value)"
      />
    </div>

    <!-- Разделитель -->
    <span class="date-separator">-</span>

    <!-- Окончание периода -->
    <div class="date-field date-end">
      <FormFieldWithHistory
        :field="endField"
        :modelValue="endValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:end', value)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import FormFieldWithHistory from './FormFieldWithHistory.vue';
import type { FormField } from '@/types/document.types';

const props = defineProps<{
  beginField: FormField;
  endField: FormField;
  beginValue: any;
  endValue: any;
  invalidChars?: string[];
}>();

const emit = defineEmits<{
  (e: 'update:begin', value: any): void;
  (e: 'update:end', value: any): void;
}>();
</script>

<style scoped>
.date-range-group {
  display: flex;
  align-items: center;
  gap: 1rem;
  width: 100%;
}

.date-field {
  flex: 1;
  min-width: 0; /* Для корректного flex shrink */
}

.date-separator {
  color: var(--md-gray-700, #666);
  font-size: 1.2rem;
  font-weight: 500;
  padding: 0 0.5rem;
  user-select: none;
}
</style>
