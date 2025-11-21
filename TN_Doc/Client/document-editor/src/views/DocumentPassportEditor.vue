import { logger } from '@tn-doc/shared';
<template>
  <div class="passport-editor">
    <!-- Индикатор загрузки -->
    <div v-if="store.isLoading" class="loading-container">
      <ProgressSpinner />
      <p>Загрузка формы редактирования паспорта качества...</p>
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
              <!-- Обычные поля -->
              <tr v-for="field in regularFields" :key="field.key">
                <td class="editor-label-cell">
                  <div class="label-wrapper">
                    <span class="label-text">{{ field.label }}</span>
                    <span v-if="field.required" class="required-mark">*</span>
                  </div>
                </td>
                <td class="editor-input-cell">
                  <FormFieldWithHistory
                    :field="field"
                    :modelValue="store.formData[field.key]"
                    :hide-label="true"
                    :invalidChars="store.config?.invalidChars || []"
                    @update:modelValue="(value) => store.updateField(field.key, value)"
                  />
                </td>
              </tr>

              <!-- Группы подписантов -->
              <tr v-for="group in signerGroups" :key="group.prefix">
                <td class="editor-label-cell">
                  <div class="label-wrapper">
                    <span class="label-text">{{ group.label }}</span>
                    <span v-if="group.required" class="required-mark">*</span>
                  </div>
                </td>
                <td class="editor-input-cell">
                  <SignerFieldGroup
                    :iof="group.iof"
                    :post="group.post"
                    :factory="group.factory"
                    :iofValue="store.formData[group.iof.key]"
                    :postValue="store.formData[group.post.key]"
                    :factoryValue="store.formData[group.factory.key]"
                    :invalidChars="store.config?.invalidChars || []"
                    @update:iof="(value) => store.updateField(group.iof.key, value)"
                    @update:post="(value) => store.updateField(group.post.key, value)"
                    @update:factory="(value) => store.updateField(group.factory.key, value)"
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
import FormFieldWithHistory from '@/components/FormFieldWithHistory.vue';
import SignerFieldGroup from '@/components/SignerFieldGroup.vue';
import PassportQualityTable from '@/components/passport/PassportQualityTable.vue';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { useDocumentEditor } from '@/composables/useDocumentEditor';
import { usePassportEditor } from '@/composables/usePassportEditor';
import { usePassportAutoFill } from '@/composables/usePassportAutoFill';
import { useElisIntegration, findElisValue, createMethodFromElisData } from '@/composables/useElisIntegration';
import { useFieldHistory } from '@/composables/useFieldHistory';
import type { ElisPassportData, ElisParameter } from '@/types/elis.types';
import type { PassportEditConfig, MethodOption } from '@/types/passport.types';
import type { FormField } from '@/types/document.types';

const route = useRoute();

// Получаем функцию для отслеживания загрузки из ELIS
const { trackElisLoad } = useFieldHistory();

// Вспомогательные функции для работы с полями подписантов
/**
 * Проверяет, является ли поле полем подписанта
 */
const isSignerField = (key: string): boolean => {
  return key.endsWith('_IOF') || key.endsWith('_Post') || key.endsWith('_Factory');
};

/**
 * Очищает label от уточнений в скобках
 * "Представитель испытательной лаборатории (ИОФ)" -> "Представитель испытательной лаборатории"
 */
const cleanSignerLabel = (label: string): string => {
  return label.replace(/\s*\((?:ИОФ|должность|предприятие)\)\s*$/i, '').trim();
};

/**
 * Группа полей подписанта
 */
interface SignerGroup {
  prefix: string;
  label: string;
  iof: FormField;
  post: FormField;
  factory: FormField;
  required: boolean;
}

/**
 * Обычные поля (не подписанты)
 */
const regularFields = computed(() => {
  return store.fields.filter(field => !isSignerField(field.key));
});

/**
 * Группы полей подписантов
 */
const signerGroups = computed((): SignerGroup[] => {
  const groups: SignerGroup[] = [];
  const signerPrefixes = ['Laboratory', 'Delive', 'Receive'];

  signerPrefixes.forEach(prefix => {
    const iofField = store.fields.find(f => f.key === `${prefix}_IOF`);
    const postField = store.fields.find(f => f.key === `${prefix}_Post`);
    const factoryField = store.fields.find(f => f.key === `${prefix}_Factory`);

    if (iofField && postField && factoryField) {
      groups.push({
        prefix,
        label: cleanSignerLabel(iofField.label), // Очищаем от "(ИОФ)"
        iof: iofField,
        post: postField,
        factory: factoryField,
        required: iofField.required || false
      });
    }
  });

  return groups;
});

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

type LabDocumentInfoPayload = {
  Number: string;
  Type?: string;
  Date?: string;
};

// Хранилище для отложенной обработки ELIS данных
let pendingElisData: ElisPassportData | null = null;

const buildDocumentPayload = (elisParam?: ElisParameter): string => {
  if (!elisParam) {
    return '';
  }

  const number = elisParam.documentNumber || '';
  if (!number) {
    return '';
  }

  const payload: LabDocumentInfoPayload = {
    Number: number
  };

  if (elisParam.documentType) {
    payload.Type = elisParam.documentType;
  }

  if (elisParam.documentDate) {
    payload.Date = elisParam.documentDate;
  }

  return JSON.stringify(payload);
};

// Функция обработки данных ELIS
const handleElisData = (elisData: ElisPassportData) => {
  // Проверить, что конфигурация загружена
  if (!store.config || store.fields.length === 0) {
    pendingElisData = elisData; // Сохранить для обработки после загрузки
    return;
  }

  // Подготовить объект для bulk update
  const updates: Record<string, any> = {};

  // ВАЖНО: Сохранить полную структуру ELIS протокола для передачи на бэкенд
  // Используем ключ с __ (двойное подчеркивание), который пропускается при создании CorrectionData,
  // но извлекается напрямую в методе DocUpdate на бэкенде
  updates['__elisProtocol'] = JSON.stringify(elisData);

  // 1. Заполнить поля AdditionalInfo

  store.fields.forEach((field, index) => {
    if (!field.elisAlias || field.elisAlias.length === 0) {
      return; // Пропустить поля без ELIS интеграции
    }

    // Искать значение в данных ELIS (fallback по массиву алиасов)
    // Порядок поиска: корень → labInfo → signers.laboratory
    let value: any;
    let foundIn: string | null = null;

    // Сначала искать в корне (здесь находятся labName, protocolNumber, pointDeliveryName и т.д.)
    value = findElisValue(elisData, field.elisAlias);
    if (value !== undefined) {
      foundIn = 'root';
    }

    // Если не найдено, искать в labInfo (здесь находятся accreditationNumber, ownerName)
    if (value === undefined) {
      value = findElisValue(elisData, field.elisAlias, 'labInfo');
      if (value !== undefined) {
        foundIn = 'labInfo';
      }
    }

    // Если не найдено, искать в signers.laboratory (здесь находятся post, company)
    if (value === undefined) {
      value = findElisValue(elisData, field.elisAlias, 'signers.laboratory');
      if (value !== undefined) {
        foundIn = 'signers.laboratory';
      }
    }

    if (value !== undefined && value !== null) {
      // Для полей типа "list" (combobox) нужно найти соответствующий элемент в field.options
      if ((field.type === 'list' || field.type === 'select') && field.options && field.options.length > 0) {
        // value - это строка ФИО (например, "А. А. Богданов")
        // Ищем пользователя с совпадающим label
        let matchingOption = field.options.find(opt => opt.label === value);

        if (!matchingOption) {
          // Если значение из ELIS не найдено в списке, создаем новую опцию
          const maxId = Math.max(0, ...field.options.map(opt => {
            const id = parseInt(opt.value, 10);
            return isNaN(id) ? 0 : id;
          }));
          const newId = (maxId + 1).toString();

          matchingOption = {
            value: newId,
            label: value,
            selected: false
          };

          // Добавляем новую опцию в список
          field.options.push(matchingOption);
        }

        // Устанавливаем выбранное значение (либо найденное, либо только что созданное)
        updates[field.key] = matchingOption.value; // Используем ID пользователя
        updates[`${field.key}__label`] = matchingOption.label; // ИСПРАВЛЕНИЕ: Обновляем label
        updates[`${field.key}__elisFilled`] = true;

        // Создать запись истории для ELIS
        trackElisLoad(field.key, matchingOption.value, elisData.protocolNumber);
      } else {
        // Для обычных полей (text, number, date, datetime-local) просто сохраняем значение
        updates[field.key] = value;
        updates[`${field.key}__elisFilled`] = true;

        // Создать запись истории для ELIS
        trackElisLoad(field.key, value, elisData.protocolNumber);
      }
    }
  });

  // 2. Заполнить параметры качества (Parameters)
  if (store.config && store.config.docType === 'Passport') {
    const passportConfig = store.config as PassportEditConfig;
    const parametersSchema = passportConfig.qualityParametersSchema || [];

    if (parametersSchema.length === 0) {
      return;
    }

    parametersSchema.forEach((param, paramIndex) => {
      const elisAlias = param.elisData?.elisAlias;
      if (!elisAlias || elisAlias.length === 0) {
        return; // Пропустить параметры без ELIS интеграции
      }

      // Искать параметр в elisData.parameters (русские полные названия)
      const elisParam = findElisValue(elisData, elisAlias, 'parameters') as ElisParameter | undefined;

      if (elisParam) {

        // Заполнить measurement (value)
        if (elisParam.value !== undefined && elisParam.value !== null) {
          const valueKey = `value.${param.key}`;
          const valueStr = elisParam.value.toString();
          updates[valueKey] = valueStr;
          updates[`${valueKey}__elisFilled`] = true;

          // Создать запись истории для ELIS
          trackElisLoad(valueKey, valueStr, elisData.protocolNumber);
        }

        // Заполнить result (valueString)
        if (elisParam.valueString) {
          const resultKey = `result.${param.key}`;
          updates[resultKey] = elisParam.valueString.toString();
          updates[`${resultKey}__elisFilled`] = true;

          // Создать запись истории для ELIS
          trackElisLoad(resultKey, elisParam.valueString.toString(), elisData.protocolNumber);
        }

        // Создать метод испытаний из ELIS данных
        if (elisParam.testMethodName) {
          const elisMethod = createMethodFromElisData(elisParam);
          if (elisMethod) {

            // Найти метод в списке доступных методов параметра
            // param.methodOptions содержит MethodOption[] с названиями методов
            let matchingMethod = param.methodOptions.find(
              (method) => method.name === elisMethod.name
            );

            if (!matchingMethod) {
              // Метод не найден - создаём новый MethodOption из ElisMethodData
              // ВАЖНО: не добавляем в param.methodOptions, т.к. это бесполезно
              // usePassportEditor автоматически добавит выбранный метод в список опций (строки 68-71)
              const maxId = Math.max(0, ...param.methodOptions.map(m => m.id));
              matchingMethod = {
                id: maxId + 1,
                use: true,
                idParameter: param.id,
                name: elisMethod.name,
                isDefault: false,
                limitValueActivate: !!elisMethod.limitValue,
                limitValue: elisMethod.limitValue,
                limitValueString: elisMethod.limitValueString
              };

            }

            // Сохранить метод как JSON string (теперь ВСЕГДА заполняем)
            const methodKey = `method.${param.key}`;
            const methodJson = JSON.stringify(matchingMethod);
            updates[methodKey] = methodJson;
            updates[`${methodKey}__elisFilled`] = true;

            // Создать запись истории для ELIS (сохраняем только name, а не весь объект)
            trackElisLoad(methodKey, matchingMethod.name, elisData.protocolNumber);
          }
        }

        // Заполнить документ (LabDocumentInfo)
        const documentKey = `document.${param.key}`;
        const documentPayload = buildDocumentPayload(elisParam);

        if (documentPayload) {
          updates[documentKey] = documentPayload;
          updates[`${documentKey}__elisFilled`] = true;

          // Создать запись истории для ELIS
          trackElisLoad(documentKey, documentPayload, elisData.protocolNumber);
        }
      }
    });
  }

  // 3. Применить все обновления bulk операцией
  if (Object.keys(updates).length > 0) {
    store.bulkUpdateFields(updates);
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

.editor-input-cell {
  width: auto;
  min-width: 300px; /* Минимальная ширина для размещения трёх полей подписанта */
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
