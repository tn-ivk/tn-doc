<template>
  <div class="passport-editor">
    <!-- Индикатор загрузки -->
    <div v-if="store.isLoading" class="loading-container">
      <ProgressSpinner />
      <p>Загрузка паспорта качества...</p>
    </div>

    <!-- Сообщение об ошибке -->
    <Message v-else-if="store.error" severity="error" :closable="false">
      {{ store.error }}
    </Message>

    <!-- Редактор паспорта -->
    <div v-else-if="store.isReady" class="editor-wrapper">
      <!-- Оверлей при сохранении -->
      <Transition name="fade">
        <div v-if="store.isSaving" class="saving-overlay-wrapper">
          <div class="saving-spinner">
            <ProgressSpinner style="width: 50px; height: 50px" />
            <p class="saving-text">Сохранение...</p>
          </div>
        </div>
      </Transition>

      <!-- Таблица AdditionalInfo (дополнительная информация) -->
      <div class="editor-container additional-info-section">
        <div class="editor-table-wrapper">
          <table class="editor-table">
            <tbody>
              <tr v-for="field in store.fields" :key="field.key">
                <td class="editor-label-cell">
                  <div class="label-wrapper">
                    <span class="label-text">{{ field.label }}</span>
                    <span v-if="field.required" class="required-mark">*</span>
                  </div>
                </td>
                <td class="editor-input-cell">
                  <FormField
                    :field="field"
                    :modelValue="store.formData[field.key]"
                    :hide-label="true"
                    :invalidChars="store.config?.invalidChars || []"
                    @update:modelValue="(value) => store.updateField(field.key, value)"
                  />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Таблица качественных показателей (Edit) -->
      <PassportQualityTable
        v-if="hasQualityParameters"
        :parameters="qualityParameters"
        :isElisUsed="isElisUsed"
        @update:method="handleMethodUpdate"
        @update:measurement="handleMeasurementUpdate"
        @update:result="handleResultUpdate"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import FormField from '@/components/FormField.vue';
import PassportQualityTable from '@/components/passport/PassportQualityTable.vue';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { useDocumentEditor } from '@/composables/useDocumentEditor';
import { usePassportEditor } from '@/composables/usePassportEditor';
import { usePassportAutoFill } from '@/composables/usePassportAutoFill';

const route = useRoute();

// Используем общую логику редактирования документов
const {
  store,
  loadDocument,
  exposeSaveDoc,
  notifyParentAboutSaveState,
  setupBeforeUnloadHandler
} = useDocumentEditor();

// Используем специфичную логику для Passport
const {
  qualityParameters,
  isElisUsed,
  hasQualityParameters,
  handleMeasurementUpdate,
  handleMethodUpdate,
  handleResultUpdate
} = usePassportEditor();

// Используем логику автозаполнения для Паспортов
const { setupAutoFillWatchers } = usePassportAutoFill();

// Загружаем документ при монтировании
onMounted(async () => {
  const { deviceId, id } = route.params;
  const docType = 'Passport'; // Для этого компонента тип документа всегда Passport

  if (!deviceId || !id) {
    store.error = 'Отсутствуют обязательные параметры маршрута';
    console.error('[DocumentPassportEditor] Отсутствуют параметры маршрута');
    return;
  }

  await loadDocument(
    parseInt(deviceId as string, 10),
    docType,
    parseInt(id as string, 10)
  );

  // Настраиваем автозаполнение связанных полей для Паспортов
  setupAutoFillWatchers();
});

// Экспонируем SaveDoc() для главного окна
exposeSaveDoc();

// Отслеживаем валидацию и уведомляем главное окно о состоянии кнопки
watch(() => store.canSave, (canSave) => {
  notifyParentAboutSaveState(canSave);
}, { immediate: true });

// Предупреждение перед закрытием с несохранёнными изменениями
setupBeforeUnloadHandler();
</script>

<style scoped>
.passport-editor {
  background-color: var(--md-surface);
  font-family: 'Segoe UI', 'PT Astra Sans', 'Helvetica Neue', Arial, sans-serif;
  height: 100%;
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  padding: 0;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  gap: 1rem;
}

.editor-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-height: 0;
  position: relative;
}

/* Секция дополнительной информации */
.additional-info-section {
  flex-shrink: 0;
}

.editor-container {
  width: 100%;
  background: #ffffff;
  border: 1px solid var(--md-outline);
  border-radius: 8px;
  box-shadow: 0 2px 6px rgba(33, 33, 33, 0.04);
  display: flex;
  min-height: 0;
}

.editor-table-wrapper {
  overflow: auto;
  flex: 1;
}

.editor-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: auto;
  font-size: 15px;
  color: var(--md-text);
}

.editor-table td {
  border: 1px solid var(--md-outline);
  padding: 0.2rem 0.5rem;
  vertical-align: middle;
}

.editor-label-cell {
  width: 1%;
  max-width: 50%;
  background-color: transparent;
  font-weight: var(--md-font-weight-medium);
  color: var(--md-text);
  white-space: nowrap;
  padding-right: 0.75rem;
}

.label-text {
  display: inline-block;
  line-height: 1.3;
}

.required-mark {
  margin-left: 4px;
  color: var(--md-error);
  font-weight: 600;
}

.editor-table tr:first-child td:first-child {
  border-top-left-radius: 8px;
}

.editor-table tr:first-child td:last-child {
  border-top-right-radius: 8px;
}

.editor-table tr:last-child td:first-child {
  border-bottom-left-radius: 8px;
}

.editor-table tr:last-child td:last-child {
  border-bottom-right-radius: 8px;
}

/* Оверлей при сохранении */
.saving-overlay-wrapper {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 999;
}

.saving-spinner {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  padding: 2rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.saving-text {
  margin: 0;
  color: var(--md-text);
  font-size: 16px;
  font-weight: 500;
}

/* Анимация появления/исчезновения */
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>
