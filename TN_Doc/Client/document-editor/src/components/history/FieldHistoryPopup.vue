<template>
  <OverlayPanel
    ref="overlayPanel"
    :dismissable="true"
    class="field-history-popup"
  >
    <div class="history-header">
      <h3 class="history-title">История изменений</h3>
      <p class="history-field-name">{{ fieldLabel }}</p>
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
        <!-- Иконка источника -->
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

        <!-- Информация -->
        <div class="entry-content">
          <div class="entry-description">
            {{ SOURCE_DISPLAY_CONFIG[entry.source].description }}
          </div>

          <div class="entry-meta">
            <span class="entry-date">{{ formatDate(entry.modifiedAt) }}</span>
            <span class="entry-separator">•</span>
            <span class="entry-author">{{ entry.modifiedBy }}</span>
          </div>

          <div v-if="entry.previousValue" class="entry-change">
            <span class="change-old">{{ entry.previousValue }}</span>
            <i class="pi pi-arrow-right change-arrow" />
            <span class="change-new">{{ entry.value }}</span>
          </div>
          <div v-else class="entry-value">
            Значение: <strong>{{ entry.value }}</strong>
          </div>

          <div v-if="entry.comment" class="entry-comment">
            {{ entry.comment }}
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
 * Форматирование даты
 */
const formatDate = (isoDate: string): string => {
  const date = new Date(isoDate);
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
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
}

.history-field-name {
  margin: 4px 0 0 0;
  font-size: 13px;
  color: var(--md-text-secondary);
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
  gap: 12px;
  padding: 12px;
  border-radius: var(--md-radius);
  background: var(--md-surface);
  border: 1px solid var(--md-border-light);
}

.entry-icon {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.05);
}

.entry-icon-text {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.entry-icon .pi {
  font-size: 16px;
}

.entry-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.entry-description {
  font-size: 14px;
  font-weight: 500;
  color: var(--md-text);
}

.entry-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--md-text-secondary);
}

.entry-separator {
  opacity: 0.5;
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
}

.entry-value strong {
  color: var(--md-text);
}

.entry-comment {
  font-size: 12px;
  font-style: italic;
  color: var(--md-text-muted);
  padding: 4px 8px;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 4px;
  border-left: 2px solid var(--md-border);
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
