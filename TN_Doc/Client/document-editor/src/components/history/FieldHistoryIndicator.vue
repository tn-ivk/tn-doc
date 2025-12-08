<template>
  <div
    v-if="source !== DataSource.Unknown && source !== DataSource.Auto"
    class="field-history-indicator"
    :style="{ right: `${rightOffset}px` }"
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

const props = withDefaults(defineProps<{
  /** Источник данных */
  source: DataSource;
  /** Отступ справа (в пикселях) */
  rightOffset?: number;
}>(), {
  rightOffset: 4
});

const displayConfig = computed(() => {
  return SOURCE_DISPLAY_CONFIG[props.source];
});
</script>

<style scoped>
.field-history-indicator {
  position: absolute;
  top: 4px;
  /* right управляется через проп rightOffset */
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  padding: 2px 2px;
  border-radius: 3px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.indicator-text {
  font-size: 9px !important;
  font-weight: 700 !important;
  letter-spacing: 0 !important;
  line-height: 1;
}

.indicator-icon {
  font-size: 14px;
  line-height: 1;
}
</style>
