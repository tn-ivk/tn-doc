<template>
  <div
    v-if="source !== DataSource.Unknown"
    class="field-history-indicator"
    @mouseenter="handleMouseEnter"
    @mouseleave="handleMouseLeave"
  >
    <!-- Текстовая метка (для ELIS и ИВК) -->
    <span
      v-if="displayConfig.text"
      class="indicator-text"
      :style="{ color: displayConfig.color }"
    >
      {{ displayConfig.text }}
    </span>

    <!-- Иконка (для Manual и Unknown) -->
    <i
      v-else
      :class="['pi', displayConfig.icon, 'indicator-icon']"
      :style="{ color: displayConfig.color }"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { DataSource, SOURCE_DISPLAY_CONFIG } from '@/types/history.types';
import { logger } from '@tn-doc/shared';

const props = defineProps<{
  /** Источник данных */
  source: DataSource;
}>();

const emit = defineEmits<{
  (e: 'mouseenter'): void;
  (e: 'mouseleave'): void;
}>();

const displayConfig = computed(() => {
  return SOURCE_DISPLAY_CONFIG[props.source];
});

/**
 * Обработчик наведения курсора
 */
const handleMouseEnter = () => {
  logger.debug(`[FieldHistoryIndicator] mouseenter - источник: ${props.source}`);
  emit('mouseenter');
};

/**
 * Обработчик ухода курсора
 */
const handleMouseLeave = () => {
  logger.debug(`[FieldHistoryIndicator] mouseleave - источник: ${props.source}`);
  emit('mouseleave');
};
</script>

<style scoped>
.field-history-indicator {
  position: absolute;
  top: 4px;
  right: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 10;
  padding: 2px 4px;
  border-radius: 3px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.field-history-indicator:hover {
  transform: scale(1.05);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
}

.indicator-text {
  font-size: 7px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: 1;
}

.indicator-icon {
  font-size: 14px;
  line-height: 1;
}
</style>
