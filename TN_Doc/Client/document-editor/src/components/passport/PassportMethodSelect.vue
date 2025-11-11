import { logger } from '@tn-doc/shared';
              <template>
  <div class="method-field">
    <Select
      :modelValue="selectedMethodOption"
      :options="methodOptions"
      optionLabel="name"
      :class="[
        { 'p-invalid': !isValid },
        { 'elis-filled': parameter.elisFlags.method }
      ]"
      placeholder="Метод не выбран"
      showClear
      class="method-select"
      @update:modelValue="handleMethodChange"
    />

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      {{ validationMessage }}
    </small>
  </div>
</template>

<script setup lang="ts">
import { logger } from '@tn-doc/shared';
import { computed, onMounted } from 'vue';
import Select from 'primevue/select';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [method: MethodOption | null];
}>();

onMounted(() => {
  logger.debug('PassportMethodSelect: монтирование', { paramKey: props.parameter.key });
  logger.debug('PassportMethodSelect: выбранный метод', { selected: props.parameter.method.selected });
  logger.debug('PassportMethodSelect: количество опций', { count: methodOptions.value.length });
});

/**
 * Опции для Select компонента
 * Только стандартная фильтрация пустых опций от бэкенда
 */
const methodOptions = computed(() => {
  return props.parameter.method.options.filter(option => option.name && option.name.trim() !== '');
});

const selectedMethodOption = computed(() => {
  if (!props.parameter.method.selected) return null;
  return methodOptions.value.find(option => option.name === props.parameter.method.selected) || null;
});

// Валидация поля метода
const isValid = computed(() => {
  // Если метод испытаний обязателен для заполнения, проверяем что он выбран
  if (props.parameter.method.requiredFill) {
    return !!props.parameter.method.selected && props.parameter.method.selected.trim() !== '';
  }
  return true;
});

// Сообщение об ошибке валидации
const validationMessage = computed(() => {
  if (!isValid.value) {
    return `Необходимо выбрать метод испытаний`;
  }
  return '';
});

/**
 * Обработчик изменения метода
 */
function handleMethodChange(method: MethodOption | null) {
  emit('update:method', method);
}
</script>

<style scoped>
.method-field {
  width: 100%;
}

.method-select {
  width: 100%;
  font-size: 15px;
}

:deep(.method-field .p-select) {
  width: 100%;
}

/* Валидация - красная рамка при ошибке */
.method-select.p-invalid,
.method-select.p-invalid:deep(.p-select),
.method-select.p-invalid:deep(.p-select-label) {
  border-color: var(--md-error, #dc3545) !important;
  box-shadow: none !important;
}

/* ELIS подсветка */
:deep(.p-select.elis-filled) {
  background-color: #8fd19e !important;
}

:deep(.p-select.elis-filled .p-select-dropdown-icon),
:deep(.p-select.elis-filled .p-select-clear-icon) {
  color: var(--md-text, #212121) !important;
}
  
  /* Сообщение об ошибке */
.p-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: var(--md-error, #dc3545);
  line-height: 1.2;
}
</style>
