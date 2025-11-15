<template>
  <OverlayPanel
    ref="overlayPanel"
    :dismissable="true"
    class="field-history-popup"
  >
    <div class="history-header">
      <h3 class="history-title">История изменений</h3>
      <div class="history-field-name">{{ fieldLabel }}</div>
    </div>

    <div v-if="history.length === 0" class="history-empty">
      <i class="pi pi-info-circle" />
      <p>История изменений отсутствует</p>
    </div>

    <div v-else class="history-list">
      <div
        v-for="(entry, index) in reversedHistory"
        :key="index"
        class="history-entry"
        :class="`source-${entry.source.toLowerCase()}`"
      >
        <!-- Строка 1: иконка + описание + дата/время -->
        <div class="entry-line1">
          <div class="entry-icon">
            <span
              v-if="SOURCE_DISPLAY_CONFIG[entry.source].text"
              class="entry-icon-text"
              :style="{ color: SOURCE_DISPLAY_CONFIG[entry.source].color }"
            >
              {{ SOURCE_DISPLAY_CONFIG[entry.source].text }}
            </span>
            <i
              v-else
              :class="['pi', SOURCE_DISPLAY_CONFIG[entry.source].icon]"
              :style="{ color: SOURCE_DISPLAY_CONFIG[entry.source].color }"
            />
          </div>
          <span class="entry-description">
            {{ SOURCE_DISPLAY_CONFIG[entry.source].description }}
          </span>
          <span class="entry-date">{{ formatDate(entry.modifiedAt) }}</span>
        </div>

        <!-- Строка 2: старое → новое значение -->
        <div class="entry-line2">
          <div v-if="entry.previousValue" class="entry-change">
            <span class="change-old">{{ entry.previousValue }}</span>
            <i class="pi pi-arrow-right change-arrow" />
            <span class="change-new">{{ entry.value }}</span>
          </div>
          <div v-else class="entry-value">
            Значение: <strong>{{ entry.value }}</strong>
          </div>
        </div>
      </div>
    </div>
  </OverlayPanel>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import OverlayPanel from 'primevue/overlaypanel';
import type { FieldHistoryEntry } from '@/types/history.types';
import { SOURCE_DISPLAY_CONFIG } from '@/types/history.types';
import { logger } from '@tn-doc/shared';

const props = defineProps<{
  /** История изменений поля */
  history: FieldHistoryEntry[];
  /** Название поля */
  fieldLabel: string;
}>();

const overlayPanel = ref<InstanceType<typeof OverlayPanel>>();

/**
 * История в обратном порядке (последнее изменение сверху)
 */
const reversedHistory = computed(() => {
  return [...props.history].reverse();
});

/**
 * Форматирование даты (с секундами)
 */
const formatDate = (isoDate: string): string => {
  const date = new Date(isoDate);
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
};

/**
 * Показать popup
 */
const show = (event: Event) => {
  logger.debug(`[FieldHistoryPopup] show() вызван для поля: ${props.fieldLabel}`);
  logger.debug(`[FieldHistoryPopup] История (${props.history.length} записей): ${JSON.stringify(props.history)}`);
  logger.debug(`[FieldHistoryPopup] overlayPanel.value: ${overlayPanel.value ? 'определен' : 'undefined'}`);
  logger.debug(`[FieldHistoryPopup] event: ${event ? 'передан' : 'undefined'}`);

  if (overlayPanel.value) {
    logger.debug('[FieldHistoryPopup] Вызов overlayPanel.show()');
    overlayPanel.value.show(event);
  } else {
    logger.error('[FieldHistoryPopup] overlayPanel.value is undefined!');
  }
};

/**
 * Скрыть popup
 */
const hide = () => {
  logger.debug(`[FieldHistoryPopup] hide() вызван для поля: ${props.fieldLabel}`);
  overlayPanel.value?.hide();
};

defineExpose({ show, hide });
</script>

<style scoped>
.field-history-popup {
  max-width: 400px;
  max-height: 450px;
  overflow: auto;
}

.history-header {
  padding-bottom: 12px;
  border-bottom: 1px solid var(--md-border-light);
  margin-bottom: 12px;
}

.history-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--md-text);
  text-align: center;
}

.history-field-name {
  margin: 4px 0 0 0;
  font-size: 13px;
  color: var(--md-text-secondary);
  text-align: center;
}

.history-empty {
  text-align: center;
  padding: 24px;
  color: var(--md-text-muted);
}

.history-empty .pi {
  font-size: 32px;
  margin-bottom: 8px;
  opacity: 0.5;
}

.history-empty p {
  margin: 0;
  font-size: 14px;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-entry {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
  border-radius: var(--md-radius);
  background: var(--md-surface);
  border: 1px solid var(--md-border-light);
}

/* Строка 1: иконка + описание + дата/время */
.entry-line1 {
  display: flex;
  align-items: center;
  gap: 8px;
}

.entry-icon {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.05);
}

.entry-icon-text {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 0;
}

.entry-icon .pi {
  font-size: 14px;
}

.entry-description {
  flex: 1;
  font-size: 14px;
  font-weight: 500;
  color: var(--md-text);
}

.entry-date {
  font-size: 12px;
  color: var(--md-text-secondary);
  white-space: nowrap;
}

/* Строка 2: старое → новое значение */
.entry-line2 {
  padding-left: 32px;
}

.entry-change {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  background: rgba(0, 0, 0, 0.03);
  border-radius: 4px;
  font-size: 13px;
}

.change-old {
  color: var(--md-error);
  text-decoration: line-through;
}

.change-arrow {
  font-size: 12px;
  color: var(--md-text-muted);
}

.change-new {
  color: var(--green-600);
  font-weight: 500;
}

.entry-value {
  font-size: 13px;
  color: var(--md-text-secondary);
  padding: 6px 8px;
  background: rgba(0, 0, 0, 0.03);
  border-radius: 4px;
}

.entry-value strong {
  color: var(--md-text);
}

/* Цветовая индикация для разных источников */
.history-entry.source-elis {
  border-left: 3px solid #4CAF50;
}

.history-entry.source-manual {
  border-left: 3px solid #2196F3;
}

.history-entry.source-ivk {
  border-left: 3px solid #FF9800;
}

.history-entry.source-unknown {
  border-left: 3px solid #9E9E9E;
}
</style>
