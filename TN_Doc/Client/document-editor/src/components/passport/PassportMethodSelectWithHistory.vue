<template>
  <div class="method-with-history">
    <PassportMethodSelect
      :parameter="parameter"
      :isElisFilled="isElisFilled"
      :hideEditButton="true"
      @update:method="handleChange"
      @manual-method="handleManualMethod"
    />

    <!-- Кнопка редактирования (перемещена на уровень method-with-history) -->
    <button
      class="edit-method-btn-external"
      type="button"
      @click="handleManualMethod"
      title="Создать ручной метод"
    >
      <i class="pi pi-pencil"></i>
    </button>

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      :rightOffset="6"
      @mouseenter="(event) => onIndicatorHover(event)"
      @mouseleave="onIndicatorLeave"
    />

    <!-- Popup с историей -->
    <FieldHistoryPopup
      ref="historyPopup"
      :history="fieldHistory"
      :fieldLabel="`${parameter.name} (Метод испытаний)`"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

const props = defineProps<{
  parameter: PassportQualityParameter;
}>();

const emit = defineEmits<{
  'update:method': [method: MethodOption | null];
  'manual-method': [];
}>();

const {
  getFieldHistory,
  getLastSource,
  trackManualChange
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();
let hideTimeout: ReturnType<typeof setTimeout> | null = null;

/**
 * Ключ для истории: method.{parameterKey}
 */
const historyKey = computed(() => `method.${props.parameter.key}`);

/**
 * История поля
 */
const fieldHistory = computed(() => {
  return getFieldHistory(historyKey.value);
});

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(historyKey.value);
});

const isElisFilled = computed(() => lastSource.value === DataSource.ELIS);

/**
 * Обработка изменения метода
 */
const handleChange = (newMethod: MethodOption | null) => {
  // Отслеживаем ручное изменение
  const newValue = newMethod?.name || '';
  const previousValue = props.parameter.method.selected || '';
  trackManualChange(historyKey.value, newValue, previousValue);

  // Передаем изменение дальше
  emit('update:method', newMethod);
};

const handleManualMethod = () => {
  emit('manual-method');
};

/**
 * Обработчик наведения на индикатор
 */
const onIndicatorHover = (event: MouseEvent) => {
  // Отменяем таймер скрытия, если он был запущен
  if (hideTimeout) {
    clearTimeout(hideTimeout);
    hideTimeout = null;
  }

  // Показываем popup, передавая событие для правильного позиционирования
  historyPopup.value?.show(event);
};

/**
 * Обработчик ухода курсора с индикатора
 */
const onIndicatorLeave = () => {
  // Запускаем таймер скрытия с задержкой 300ms
  hideTimeout = setTimeout(() => {
    historyPopup.value?.hide();
    hideTimeout = null;
  }, 300);
};
</script>

<style scoped>
.method-with-history {
  position: relative;
}

/* Кнопка редактирования на уровне method-with-history */
.edit-method-btn-external {
  position: absolute;
  right: 40px; /* Карандаш левее */
  top: 50%;
  transform: translateY(-50%);
  width: 28px;
  height: 28px;
  border: 1px solid transparent !important;
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  z-index: 2; /* Выше индикатора истории */
}

.edit-method-btn-external:hover {
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
}

.edit-method-btn-external:active {
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
}
</style>
