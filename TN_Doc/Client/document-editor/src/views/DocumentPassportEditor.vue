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
import type { PassportEditConfig, MethodOption } from '@/types/passport.types';

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

  const number = elisParam.documentNumber || elisParam.testMethodName || '';
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
  logger.info('[DocumentPassportEditor] Получены данные ELIS, начинаем заполнение формы');

  // Проверить, что конфигурация загружена
  if (!store.config || store.fields.length === 0) {
    logger.warn('[ELIS] Конфигурация документа ещё не загружена, сохраняем данные для отложенной обработки', {
      hasConfig: !!store.config,
      fieldsCount: store.fields.length
    });
    pendingElisData = elisData; // Сохранить для обработки после загрузки
    return;
  }

  // Подготовить объект для bulk update
  const updates: Record<string, any> = {};

  // ========================================
  // ОТЛАДОЧНЫЕ ЛОГИ - Структура данных ELIS
  // ========================================

  logger.info('[ELIS DEBUG] ========== НАЧАЛО АНАЛИЗА ДАННЫХ ==========');

  // 1. Вывести все ключи верхнего уровня ELIS данных
  logger.info('[ELIS DEBUG] Ключи на корневом уровне elisData:', {
    keys: Object.keys(elisData),
    values: Object.keys(elisData).reduce((acc, key) => {
      const value = elisData[key as keyof typeof elisData];
      acc[key] = typeof value === 'object' && value !== null ? `[object: ${Object.keys(value).length} keys]` : value;
      return acc;
    }, {} as Record<string, any>)
  });

  // 2. Детально вывести labInfo
  if (elisData.labInfo) {
    logger.info('[ELIS DEBUG] Содержимое labInfo:', {
      keys: Object.keys(elisData.labInfo),
      data: elisData.labInfo
    });
  } else {
    logger.warn('[ELIS DEBUG] labInfo отсутствует в данных');
  }

  // 3. Вывести превью parameters
  if (elisData.parameters) {
    const paramKeys = Object.keys(elisData.parameters);
    logger.info('[ELIS DEBUG] Параметры качества:', {
      total: paramKeys.length,
      keys: paramKeys,
      firstParameter: paramKeys[0] ? {
        key: paramKeys[0],
        value: elisData.parameters[paramKeys[0]]
      } : null
    });
  } else {
    logger.warn('[ELIS DEBUG] parameters отсутствуют в данных');
  }

  // 4. Вывести signers
  if (elisData.signers?.laboratory) {
    logger.info('[ELIS DEBUG] Подписанты (signers.laboratory):', elisData.signers.laboratory);
  } else {
    logger.warn('[ELIS DEBUG] signers.laboratory отсутствует в данных');
  }

  // 5. Найти все поля с ELIS интеграцией
  const fieldsWithElis = store.fields.filter(f => f.elisAlias && f.elisAlias.length > 0);
  logger.info('[ELIS DEBUG] Поля с ElisAlias в конфигурации:', {
    totalFieldsCount: store.fields.length,
    fieldsWithElisCount: fieldsWithElis.length,
    fields: fieldsWithElis.map(f => ({
      key: f.key,
      label: f.label,
      elisAlias: f.elisAlias,
      type: f.type
    }))
  });

  logger.info('[ELIS DEBUG] ========== КОНЕЦ АНАЛИЗА ДАННЫХ ==========');

  // 1. Заполнить поля AdditionalInfo
  logger.info('[ELIS DEBUG] ========== НАЧАЛО ЗАПОЛНЕНИЯ ADDITIONALINFO ==========');

  let successCount = 0;
  let failedFields: any[] = [];

  logger.info(`[ELIS DEBUG] Начинаем обработку ${store.fields.length} полей из store.fields`);

  store.fields.forEach((field, index) => {
    logger.info(`[ELIS DEBUG] Поле #${index}: "${field.key}"`, {
      key: field.key,
      label: field.label,
      hasElisAlias: !!field.elisAlias,
      elisAlias: field.elisAlias,
      elisAliasType: typeof field.elisAlias,
      elisAliasLength: field.elisAlias?.length
    });

    if (!field.elisAlias || field.elisAlias.length === 0) {
      logger.info(`[ELIS DEBUG] Поле "${field.key}" ПРОПУЩЕНО (elisAlias пустой или отсутствует)`);
      return; // Пропустить поля без ELIS интеграции
    }

    logger.info(`[ELIS DEBUG] Поле "${field.key}" ИМЕЕТ elisAlias [${field.elisAlias}], продолжаем обработку`);
    logger.info(`[ELIS DEBUG] Обработка поля "${field.key}" (${field.label})`, {
      elisAlias: field.elisAlias,
      type: field.type
    });

    // Искать значение в данных ELIS (fallback по массиву алиасов)
    // Порядок поиска: корень → labInfo → signers.laboratory
    let value: any;
    let foundIn: string | null = null;

    logger.info(`[ELIS DEBUG] ПЕРЕД ВЫЗОВОМ findElisValue для поля "${field.key}"`, {
      fieldKey: field.key,
      fieldLabel: field.label,
      elisAlias: field.elisAlias,
      elisAliasType: typeof field.elisAlias,
      elisAliasLength: field.elisAlias?.length
    });

    // Сначала искать в корне (здесь находятся labName, protocolNumber, pointDeliveryName и т.д.)
    logger.info(`[ELIS DEBUG] Вызываем findElisValue(elisData, [${field.elisAlias}], undefined)`);
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
      logger.info(`[ELIS DEBUG] 🔍 Проверка типа поля "${field.key}"`, {
        fieldType: field.type,
        hasOptions: !!field.options,
        optionsLength: field.options?.length ?? 0,
        elisValue: value
      });

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

          logger.info(`[ELIS DEBUG] ➕ Значение "${value}" не найдено в combobox "${field.key}", добавлена новая опция`, {
            elisValue: value,
            newOptionValue: newId,
            previousOptionsCount: field.options.length - 1,
            availableOptions: field.options.map(o => o.label)
          });
        }

        // Устанавливаем выбранное значение (либо найденное, либо только что созданное)
        updates[field.key] = matchingOption.value; // Используем ID пользователя
        updates[`${field.key}__elisFilled`] = true;
        successCount++;
        logger.info(`[ELIS DEBUG] ✅ Поле "${field.key}" (combobox) успешно заполнено`, {
          foundIn: foundIn,
          elisAlias: field.elisAlias,
          elisValue: value,
          selectedOptionValue: matchingOption.value,
          selectedOptionLabel: matchingOption.label
        });
      } else {
        // Для обычных полей (text, number, date, datetime-local) просто сохраняем значение
        updates[field.key] = value;
        updates[`${field.key}__elisFilled`] = true; // Флаг для подсветки
        successCount++;
        logger.info(`[ELIS DEBUG] ✅ Поле "${field.key}" успешно заполнено`, {
          foundIn: foundIn,
          elisAlias: field.elisAlias,
          value: typeof value === 'string' && value.length > 100 ? `${value.substring(0, 100)}...` : value
        });
      }
    } else {
      failedFields.push({
        key: field.key,
        label: field.label,
        elisAlias: field.elisAlias,
        searchedIn: ['root', 'labInfo', 'signers.laboratory']
      });
      logger.warn(`[ELIS DEBUG] ❌ Поле "${field.key}" не найдено в данных ELIS`, {
        elisAlias: field.elisAlias,
        searchedIn: ['root', 'labInfo', 'signers.laboratory']
      });
    }
  });

  logger.info('[ELIS DEBUG] Результат заполнения AdditionalInfo:', {
    successCount: successCount,
    failedCount: failedFields.length,
    failedFields: failedFields
  });

  logger.info('[ELIS DEBUG] ========== КОНЕЦ ЗАПОЛНЕНИЯ ADDITIONALINFO ==========');

  // 2. Заполнить параметры качества (Parameters)
  if (store.config && store.config.docType === 'Passport') {
    logger.info('[ELIS DEBUG] ========== НАЧАЛО ЗАПОЛНЕНИЯ PARAMETERS ==========');

    const passportConfig = store.config as PassportEditConfig;
    const parametersSchema = passportConfig.qualityParametersSchema || [];

    logger.info('[ELIS DEBUG] 🔍 Проверка конфигурации Passport:', {
      hasConfig: !!store.config,
      docType: store.config?.docType,
      hasQualityParametersSchema: !!passportConfig.qualityParametersSchema,
      parametersSchemaLength: parametersSchema.length
    });

    if (parametersSchema.length === 0) {
      logger.warn('[ELIS DEBUG] ⚠️ qualityParametersSchema ПУСТОЙ - параметры не будут заполняться!');
      logger.info('[ELIS DEBUG] ========== КОНЕЦ ЗАПОЛНЕНИЯ PARAMETERS (пропущено) ==========');
      return;
    }

    logger.info('[ELIS DEBUG] Схема параметров качества:', {
      totalParameters: parametersSchema.length,
      parametersWithElis: parametersSchema.filter(p => p.elisData?.elisAlias && p.elisData.elisAlias.length > 0).length
    });

    // Логировать каждый параметр отдельно для детального анализа
    logger.info('[ELIS DEBUG] 📋 Детальный список параметров схемы:');
    parametersSchema.forEach((param, idx) => {
      logger.info(`[ELIS DEBUG]   Параметр #${idx}: key="${param.key}", name="${param.name}", elisData.elisAlias=${JSON.stringify(param.elisData?.elisAlias)}, methodOptionsCount=${param.methodOptions?.length || 0}`);
    });

    let paramsSuccessCount = 0;
    let paramsFailedFields: any[] = [];

    parametersSchema.forEach((param, paramIndex) => {
      const elisAlias = param.elisData?.elisAlias;
      if (!elisAlias || elisAlias.length === 0) {
        logger.info(`[ELIS DEBUG] 🔸 Параметр #${paramIndex}: "${param.key}" ПРОПУЩЕН (нет elisAlias)`);
        return; // Пропустить параметры без ELIS интеграции
      }

      logger.info(`[ELIS DEBUG] 🔸 Параметр #${paramIndex}: "${param.key}" (${param.name})`, {
        elisAlias: elisAlias,
        methodOptionsCount: param.methodOptions?.length || 0
      });

      // Искать параметр в elisData.parameters (русские полные названия)
      logger.info(`[ELIS DEBUG]   → Вызов findElisValue(elisData, [${elisAlias}], 'parameters')`);
      const elisParam = findElisValue(elisData, elisAlias, 'parameters') as ElisParameter | undefined;

      if (!elisParam) {
        logger.warn(`[ELIS DEBUG]   ❌ findElisValue вернул undefined для "${param.key}"`);
      } else {
        logger.info(`[ELIS DEBUG]   ✅ findElisValue нашёл параметр:`, {
          value: elisParam.value,
          valueString: elisParam.valueString,
          testMethodName: elisParam.testMethodName
        });
      }

      if (elisParam) {
        logger.info(`[ELIS DEBUG] ✅ Параметр "${param.key}" найден в ELIS данных:`, elisParam);

        // Заполнить measurement (value)
        if (elisParam.value !== undefined && elisParam.value !== null) {
          const valueKey = `value.${param.key}`;
          updates[valueKey] = elisParam.value.toString();
          updates[`${valueKey}__elisFilled`] = true;
          paramsSuccessCount++;
          logger.info(`[ELIS DEBUG] ✅ Measurement заполнен: ${param.key} = ${elisParam.value}`);
        }

        // Заполнить result (valueString)
        if (elisParam.valueString) {
          const resultKey = `result.${param.key}`;
          updates[resultKey] = elisParam.valueString.toString();
          updates[`${resultKey}__elisFilled`] = true;
          logger.info(`[ELIS DEBUG] ✅ Result заполнен: ${param.key} = ${elisParam.valueString}`);
        }

        // Создать метод испытаний из ELIS данных
        if (elisParam.testMethodName) {
          const elisMethod = createMethodFromElisData(elisParam);
          if (elisMethod) {
            logger.info(`[ELIS DEBUG] Создан метод из ELIS данных:`, elisMethod);

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

              logger.info(`[ELIS DEBUG] ➕ Метод "${elisMethod.name}" не найден в списке, будет добавлен динамически`, {
                paramKey: param.key,
                elisMethodName: elisMethod.name
              });
            } else {
              logger.info(`[ELIS DEBUG] ✅ Метод найден в списке: ${matchingMethod.name}`);
            }

            // Сохранить метод как JSON string (теперь ВСЕГДА заполняем)
            const methodKey = `method.${param.key}`;
            updates[methodKey] = JSON.stringify(matchingMethod);
            updates[`${methodKey}__elisFilled`] = true;
          } else {
            logger.warn(`[ELIS DEBUG] Не удалось создать метод из ELIS данных для "${param.key}"`);
          }
        }

        // Заполнить документ (LabDocumentInfo)
        const documentKey = `document.${param.key}`;
        const documentPayload = buildDocumentPayload(elisParam);

        if (documentPayload) {
          updates[documentKey] = documentPayload;
          updates[`${documentKey}__elisFilled`] = true;
          logger.info(`[ELIS DEBUG] ✅ Документ заполнен: ${param.key}`, {
            payload: documentPayload
          });
        } else {
          logger.warn(`[ELIS DEBUG] ⚠️ Документ для "${param.key}" не заполнен: отсутствуют documentNumber и testMethodName`);
        }
      } else {
        paramsFailedFields.push({
          key: param.key,
          name: param.name,
          elisAlias: elisAlias
        });
        logger.warn(`[ELIS DEBUG] ❌ Параметр "${param.key}" не найден в elisData.parameters`, {
          elisAlias: elisAlias,
          availableKeys: elisData.parameters ? Object.keys(elisData.parameters) : []
        });
      }
    });

    logger.info('[ELIS DEBUG] Результат заполнения Parameters:', {
      successCount: paramsSuccessCount,
      failedCount: paramsFailedFields.length,
      failedFields: paramsFailedFields
    });

    logger.info('[ELIS DEBUG] ========== КОНЕЦ ЗАПОЛНЕНИЯ PARAMETERS ==========');
  }

  // 3. Применить все обновления bulk операцией
  logger.info('[ELIS DEBUG] ========== ИТОГОВЫЕ РЕЗУЛЬТАТЫ ==========');

  const totalUpdates = Object.keys(updates).length;
  const elisFilledCount = Object.keys(updates).filter(k => k.includes('__elisFilled')).length;
  const actualFieldsUpdated = totalUpdates - elisFilledCount;

  logger.info('[ELIS DEBUG] Подготовленные обновления:', {
    totalUpdates: totalUpdates,
    actualFieldsUpdated: actualFieldsUpdated,
    elisFilledFlagsCount: elisFilledCount,
    updateKeys: Object.keys(updates)
  });

  if (totalUpdates > 0) {
    store.bulkUpdateFields(updates);
    logger.info('[ELIS DEBUG] ✅ УСПЕШНО: Обновления применены к store', {
      updatesCount: actualFieldsUpdated
    });
  } else {
    logger.error('[ELIS DEBUG] ❌ ОШИБКА: Не найдено ни одного поля для заполнения из данных ELIS!');
  }

  logger.info('[ELIS DEBUG] ========== КОНЕЦ ОБРАБОТКИ ELIS ДАННЫХ ==========');
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
