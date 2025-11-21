<template>
  <div class="signer-group">
    <!-- ИОФ (ComboBox) -->
    <div class="signer-field signer-iof">
      <FormFieldWithHistory
        :field="iof"
        :modelValue="iofValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:iof', value)"
      />
    </div>

    <!-- Должность (TextInput) -->
    <div class="signer-field signer-post">
      <FormFieldWithHistory
        :field="postField"
        :modelValue="postValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:post', value)"
      />
    </div>

    <!-- Предприятие (TextInput) -->
    <div class="signer-field signer-factory">
      <FormFieldWithHistory
        :field="factoryField"
        :modelValue="factoryValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:factory', value)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import FormFieldWithHistory from './FormFieldWithHistory.vue';
import type { FormField } from '@/types/document.types';

const props = defineProps<{
  iof: FormField;
  post: FormField;
  factory: FormField;
  iofValue: any;
  postValue: any;
  factoryValue: any;
  invalidChars?: string[];
}>();

const emit = defineEmits<{
  (e: 'update:iof', value: any): void;
  (e: 'update:post', value: any): void;
  (e: 'update:factory', value: any): void;
}>();

// Создаем поля с placeholder для должности и предприятия
const postField: FormField = {
  ...props.post,
  label: 'Должность' // Placeholder будет отображаться в пустом поле
};

const factoryField: FormField = {
  ...props.factory,
  label: 'Предприятие' // Placeholder будет отображаться в пустом поле
};
</script>

<style scoped>
.signer-group {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  width: 100%;
}

.signer-field {
  flex: 1;
  min-width: 0; /* Для корректного flex shrink */
}

.signer-iof {
  flex: 2; /* ИОФ занимает больше места */
}

.signer-post {
  flex: 1.5; /* Должность чуть больше предприятия */
}

.signer-factory {
  flex: 1.5; /* Предприятие */
}
</style>
