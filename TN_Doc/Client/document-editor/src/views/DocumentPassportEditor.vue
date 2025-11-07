import { logger } from '@tn-doc/shared';
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
            <ProgressSpinner style="width: 60px; height: 60px" />
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
                    :highlightColor="store.formData[`${field.key}__elisFilled`] ? 'var(--md-elis-highlight)' : undefined"
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
import { logger } from '@tn-doc/shared';
import { computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import FormField from '@/components/FormField.vue';
import PassportQualityTable from '@/components/passport/PassportQualityTable.vue';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { useDocumentEditor } from '@/composables/useDocumentEditor';
import { usePassportEditor } from '@/composables/usePassportEditor';
import { usePassportAutoFill } from '@/composables/usePassportAutoFill';
import { useElisIntegration, findElisValue, createMethodFromElisData } from '@/composables/useElisIntegration';
import type { ElisPassportData, ElisParameter } from '@/types/elis.types';
import type { PassportEditConfig } from '@/types/passport.types';

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

// Хранилище для отложенной обработки ELIS данных
let pendingElisData: ElisPassportData | null = null;

// Функция обработки данных ELIS
const handleElisData = (elisData: ElisPassportData) => {
  logger.info('[DocumentPassportEditor] Получены данные ELIS, начинаем заполнение формы');

  // Проверить, что конфигурация загружена
  if (!store.config || store.fields.length === 0) {
    logger.warn(`[ELIS] Конфигурация документа ещё не загружена (config: ${!!store.config}, fields: ${store.fields.length}), сохраняем данные для отложенной обработки`);
    pendingElisData = elisData; // Сохранить для обработки после загрузки
    return;
  }

  // Подготовить объект для bulk update
  const updates: Record<string, any> = {};

  // 1. Заполнить поля AdditionalInfo
  store.fields.forEach((field) => {
    if (!field.elisAlias || field.elisAlias.length === 0) {
      return; // Пропустить поля без ELIS интеграции
    }

    // Искать значение в данных ELIS (fallback по массиву алиасов)
    let value: any;

    // Сначала искать в labInfo
    value = findElisValue(elisData, field.elisAlias, 'labInfo');

    // Если не найдено, искать в корне
    if (value === undefined) {
      value = findElisValue(elisData, field.elisAlias);
    }

    // Если не найдено, искать в signers.laboratory
    if (value === undefined) {
      value = findElisValue(elisData, field.elisAlias, 'signers.laboratory');
    }

    if (value !== undefined && value !== null) {
      updates[field.key] = value;
      updates[`${field.key}__elisFilled`] = true; // Флаг для подсветки
      logger.info(`[ELIS] Поле "${field.key}" заполнено значением: ${JSON.stringify(value)}`);
    }
  });

  // 2. Заполнить параметры качества (Parameters)
  if (store.config && store.config.docType === 'Passport') {
    const passportConfig = store.config as PassportEditConfig;
    const parametersSchema = passportConfig.qualityParametersSchema || [];

    parametersSchema.forEach((param) => {
      if (!param.elisAlias || param.elisAlias.length === 0) {
        return; // Пропустить параметры без ELIS интеграции
      }

      // Искать параметр в elisData.parameters (русские полные названия)
      const elisParam = findElisValue(elisData, param.elisAlias, 'parameters') as ElisParameter | undefined;

      if (elisParam) {
        // Заполнить measurement (value)
        if (elisParam.value !== undefined && elisParam.value !== null) {
          const valueKey = `value.${param.key}`;
          updates[valueKey] = elisParam.value.toString();
          updates[`${valueKey}__elisFilled`] = true;
          logger.info(`[ELIS] Параметр "${param.key}" measurement заполнен: ${elisParam.value}`);
        }

        // Заполнить result (valueString)
        if (elisParam.valueString) {
          const resultKey = `result.${param.key}`;
          updates[resultKey] = elisParam.valueString.toString();
          updates[`${resultKey}__elisFilled`] = true;
          logger.info(`[ELIS] Параметр "${param.key}" result заполнен: ${elisParam.valueString}`);
        }

        // Создать метод испытаний из ELIS данных
        if (elisParam.testMethodName) {
          const elisMethod = createMethodFromElisData(elisParam);
          if (elisMethod) {
            // Найти метод в списке доступных методов параметра
            const matchingMethod = param.methodOptions.find(
              (method) => method.name === elisMethod.name
            );

            if (matchingMethod) {
              // Использовать существующий метод
              const methodKey = `method.${param.key}`;
              updates[methodKey] = matchingMethod;
              updates[`${methodKey}__elisFilled`] = true;
              logger.info(`[ELIS] Параметр "${param.key}" method найден: ${matchingMethod.name}`);
            } else {
              // Метод не найден в списке - логировать предупреждение
              logger.warn(
                `[ELIS] Метод "${elisMethod.name}" для параметра "${param.key}" не найден в списке доступных методов`
              );
            }
          }
        }
      }
    });
  }

  // 3. Применить все обновления bulk операцией
  if (Object.keys(updates).length > 0) {
    store.bulkUpdateFields(updates);
    logger.info(`[ELIS] Применено ${Object.keys(updates).length} обновлений полей из данных ELIS`);
  } else {
    logger.warn('[ELIS] Не найдено ни одного поля для заполнения из данных ELIS');
  }
};

// Регистрируем обработчик ELIS данных
useElisIntegration(handleElisData);

// Загружаем документ при монтировании
onMounted(async () => {
  const { deviceId, id } = route.params;
  const docType = 'Passport'; // Для этого компонента тип документа всегда Passport

  if (!deviceId || !id) {
    store.error = 'Отсутствуют обязательные параметры маршрута';
    logger.error('[DocumentPassportEditor] Отсутствуют параметры маршрута');
    return;
  }

  await loadDocument(
    parseInt(deviceId as string, 10),
    docType,
    parseInt(id as string, 10)
  );

  // Настраиваем автозаполнение связанных полей для Паспортов
  setupAutoFillWatchers();

  // Обработать отложенные ELIS данные, если они были получены до загрузки конфигурации
  if (pendingElisData) {
    logger.info('[ELIS] Обрабатываем отложенные ELIS данные после загрузки конфигурации');
    handleElisData(pendingElisData);
    pendingElisData = null; // Сбросить после обработки
  }
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
  gap: 1.25rem;
  padding: 2.5rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.saving-text {
  margin: 0;
  color: var(--md-text);
  font-size: 18px;
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
