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
              <!-- Динамические строки: обычные поля, группы дат и группы подписантов в исходном порядке -->
              <template v-for="row in displayRows" :key="row.type === 'field' ? row.field.key : row.type === 'dateRange' ? 'dateRange' : row.group.prefix">
                <!-- Обычное поле -->
                <tr v-if="row.type === 'field'">
                  <td class="editor-label-cell">
                    <div class="label-wrapper">
                      <span class="label-text">{{ row.field.label }}</span>
                    </div>
                  </td>
                  <td class="editor-input-cell">
                    <FormFieldWithHistory
                      :field="row.field"
                      :modelValue="store.formData[row.field.key]"
                      :hide-label="true"
                      :invalidChars="store.config?.invalidChars || []"
                      @update:modelValue="(value) => store.updateField(row.field.key, value)"
                    />
                  </td>
                </tr>

                <!-- Группа дат (диапазон) -->
                <tr v-else-if="row.type === 'dateRange'">
                  <td class="editor-label-cell">
                    <div class="label-wrapper">
                      <span class="label-text">{{ row.group.label }}</span>
                    </div>
                  </td>
                  <td class="editor-input-cell">
                    <DateRangeFieldGroup
                      :beginField="row.group.begin"
                      :endField="row.group.end"
                      :beginValue="store.formData[row.group.begin.key]"
                      :endValue="store.formData[row.group.end.key]"
                      :invalidChars="store.config?.invalidChars || []"
                      @update:begin="(value) => store.updateField(row.group.begin.key, value)"
                      @update:end="(value) => store.updateField(row.group.end.key, value)"
                    />
                  </td>
                </tr>

                <!-- Группа подписантов -->
                <tr v-else-if="row.type === 'signerGroup'">
                  <td class="editor-label-cell">
                    <div class="label-wrapper">
                      <span class="label-text">{{ row.group.label }}</span>
                    </div>
                  </td>
                  <td class="editor-input-cell">
                    <SignerFieldGroup
                      :iof="row.group.iof"
                      :post="row.group.post"
                      :factory="row.group.factory"
                      :iofValue="store.formData[row.group.iof.key]"
                      :postValue="store.formData[row.group.post.key]"
                      :factoryValue="store.formData[row.group.factory.key]"
                      :invalidChars="store.config?.invalidChars || []"
                      @update:iof="(value) => handleSignerIofUpdate(row.group.iof.key, value)"
                      @update:iof-label="(label) => handleSignerIofLabelUpdate(row.group.iof.key, label)"
                      @update:post="(value) => store.updateField(row.group.post.key, value)"
                      @update:factory="(value) => store.updateField(row.group.factory.key, value)"
                    />
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Таблица качественных показателей (Edit) -->
      <PassportQualityTable
        v-if="hasQualityParameters"
        :parameters="qualityParameters"
        :isElisUsed="isElisUsed"
        :editConfigFilePath="editConfigFilePath"
        :onMethodAddedToDictionary="addMethodToLocalDictionary"
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
import DateRangeFieldGroup from '@/components/DateRangeFieldGroup.vue';
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
import type { FormField, UserData } from '@/types/document.types';
import {normalizeValue} from "@/utils/passport-utils.ts";

const route = useRoute();

// Получаем функции для отслеживания загрузки из ELIS
const { trackElisLoad, trackElisMissing, trackAutoFill } = useFieldHistory();

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
 * Группа полей даты и времени (диапазон)
 */
interface DateRangeGroup {
  label: string;
  begin: FormField;
  end: FormField;
  required: boolean;
}

/**
 * Строка для отображения в таблице (обычное поле, группа подписантов или группа дат)
 */
type DisplayRow =
  | { type: 'field'; field: FormField }
  | { type: 'signerGroup'; group: SignerGroup }
  | { type: 'dateRange'; group: DateRangeGroup };

/**
 * Объединенный массив полей для отображения в правильном порядке
 * Сохраняет порядок из конфигурации, но группирует поля подписантов
 */
const displayRows = computed((): DisplayRow[] => {
  const rows: DisplayRow[] = [];
  const processedKeys = new Set<string>();

  for (const field of store.fields) {
    // Пропускаем уже обработанные поля
    if (processedKeys.has(field.key)) {
      continue;
    }

    // Проверяем, является ли это началом диапазона дат
    if (field.key === 'PassportPeriodDT.Begin') {
      const beginField = field;
      const endField = store.fields.find(f => f.key === 'PassportPeriodDT.End');

      if (beginField && endField) {
        rows.push({
          type: 'dateRange',
          group: {
            label: 'Дата и время отбора пробы', // Очищенный label без уточнений
            begin: beginField,
            end: endField,
            required: beginField.required || endField.required || false
          }
        });
        processedKeys.add(beginField.key);
        processedKeys.add(endField.key);
        continue;
      }
    }

    // Проверяем, является ли это первым полем группы подписантов
    // (порядок в конфигурации: Post, Factory, IOF)
    if (field.key.endsWith('_Post')) {
      const prefix = field.key.replace('_Post', '');
      const postField = field;
      const factoryField = store.fields.find(f => f.key === `${prefix}_Factory`);
      const iofField = store.fields.find(f => f.key === `${prefix}_IOF`);

      // Если нашли все три поля, создаем группу
      if (postField && factoryField && iofField) {
        rows.push({
          type: 'signerGroup',
          group: {
            prefix,
            label: cleanSignerLabel(iofField.label), // Используем label из IOF, очищаем от "(ИОФ)"
            iof: iofField,
            post: postField,
            factory: factoryField,
            required: iofField.required || false
          }
        });

        // Отмечаем все три поля как обработанные
        processedKeys.add(postField.key);
        processedKeys.add(factoryField.key);
        processedKeys.add(iofField.key);
        continue;
      }
    }

    // Если это не часть группы подписантов и не PassportPeriodDT.End, добавляем как обычное поле
    if (!isSignerField(field.key) && field.key !== 'PassportPeriodDT.End') {
      rows.push({
        type: 'field',
        field
      });
      processedKeys.add(field.key);
    }
  }

  return rows;
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
  editConfigFilePath,
  handleMeasurementUpdate,
  handleMethodUpdate,
  handleResultUpdate,
  addMethodToLocalDictionary
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

/**
 * Извлекает UserData из ЕЛИС данных для поля Laboratory_IOF
 * @param elisData - данные ЕЛИС
 * @returns UserData или undefined, если данные отсутствуют
 */
const extractUserDataFromElis = (elisData: ElisPassportData): UserData | undefined => {
  const laboratory = elisData.signers?.laboratory;
  if (!laboratory) {
    return undefined;
  }

  // Формируем полное ФИО для поля fio
  const fullName = [
    laboratory.familyName,
    laboratory.givenName,
    laboratory.middleName
  ].filter(Boolean).join(' ');

  return {
    factory: laboratory.company || '',
    post: laboratory.post || '',
    fio: fullName || undefined
  };
};

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

          // Для Laboratory_IOF извлекаем UserData из ЕЛИС (post, company)
          const userData = field.key === 'Laboratory_IOF'
            ? extractUserDataFromElis(elisData)
            : undefined;

          matchingOption = {
            value: newId,
            label: value,
            selected: false,
            data: userData  // Добавляем UserData для автозаполнения Post/Factory
          };

          // Добавляем новую опцию в список
          field.options.push(matchingOption);
        }

        // Устанавливаем выбранное значение (либо найденное, либо только что созданное)
        updates[field.key] = matchingOption.value; // Используем ID пользователя
        updates[`${field.key}__label`] = matchingOption.label; // ИСПРАВЛЕНИЕ: Обновляем label
        updates[`${field.key}__elisFilled`] = true;
        updates[`${field.key}__elisOriginal`] = matchingOption.value; // Сохраняем оригинал для восстановления флага

        // Создать запись истории для ELIS
        trackElisLoad(field.key, matchingOption.value, elisData.protocolNumber);
      } else {
        // Для обычных полей (text, number, date, datetime-local) просто сохраняем значение
        updates[field.key] = value;
        updates[`${field.key}__elisFilled`] = true;
        updates[`${field.key}__elisOriginal`] = value; // Сохраняем оригинал для восстановления флага

        // Создать запись истории для ELIS
        trackElisLoad(field.key, value, elisData.protocolNumber);
      }
    } else {
      // Значение ожидалось из ELIS (есть elisAlias), но не было найдено
      updates[`${field.key}__elisMissing`] = true;

      // Создать запись истории для ELIS Missing
      trackElisMissing(field.key, elisData.protocolNumber);
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
      // Slave-параметры не заполняются из ELIS (значение вычисляется от Master)
      // Для них не устанавливаем ни elisFilled, ни elisMissing
      if (param.role === 'Slave') {
        return;
      }

      const elisAlias = param.elisData?.elisAlias;
      if (!elisAlias || elisAlias.length === 0) {
        return; // Пропустить параметры без ELIS интеграции
      }

      // Ключи полей параметра
      const valueKey = `value.${param.key}`;
      const methodKey = `method.${param.key}`;
      const resultKey = `result.${param.key}`;
      const documentKey = `document.${param.key}`;

      // Искать параметр в elisData.parameters (русские полные названия)
      const elisParam = findElisValue(elisData, elisAlias, 'parameters') as ElisParameter | undefined;

      if (elisParam) {
        // Параметр найден в ELIS, обрабатываем отдельные поля

        // Поддержка как camelCase (roundValue), так и PascalCase (RoundValue) для совместимости с бэкендом
        const roundValue = param.roundValue ?? (param as any).RoundValue ?? 0;

        // Заполнить measurement (value)
        if (elisParam.value !== undefined && elisParam.value !== null) {
          let valueStr = elisParam.value.toString();

          // Нормализовать значение согласно roundValue
          if (roundValue > 0) {
            const numValue = parseFloat(valueStr.replace(',', '.'));
            if (!isNaN(numValue)) {
              // Получаем текущее количество знаков после запятой
              const normalizedStr = valueStr.replace(',', '.');
              const parts = normalizedStr.split('.');
              const currentDecimalPlaces = parts.length > 1 ? parts[1].length : 0;

              // Если знаков меньше - дополняем нулями
              // Если знаков больше - оставляем как есть (для показа ошибки валидации)
              if (currentDecimalPlaces < roundValue) {
                valueStr = numValue.toFixed(roundValue).replace('.', ',');
              } else {
                // Оставляем оригинальное значение, но с запятой
                valueStr = normalizedStr.replace('.', ',');
              }
            }
          }

          updates[valueKey] = valueStr;
          updates[`${valueKey}__elisFilled`] = true;
          updates[`${valueKey}__elisOriginal`] = valueStr; // Сохраняем оригинал для восстановления флага

          console.log(`[handleElisData] Сохранен elisOriginal для ${valueKey}:`, valueStr, 'normalized:', normalizeValue(valueStr));

          // Создать запись истории для ELIS
          trackElisLoad(valueKey, valueStr, elisData.protocolNumber);
        } else {
          // value ожидалось, но не пришло
          updates[`${valueKey}__elisMissing`] = true;
          trackElisMissing(valueKey, elisData.protocolNumber);
        }

        // Заполнить result (valueString)
        if (elisParam.valueString) {
          let resultStr = elisParam.valueString.toString();

          // Для ballast параметров нормализовать result так же как measurement
          // Это предотвращает ложное срабатывание watcher'а из-за разницы "1,6" vs "1,6000"
          const isBallast = param.isBallast ?? (param as any).IsBallast ?? false;
          if (isBallast && roundValue > 0) {
            const numValue = parseFloat(resultStr.replace(',', '.'));
            if (!isNaN(numValue)) {
              const normalizedStr = resultStr.replace(',', '.');
              const parts = normalizedStr.split('.');
              const currentDecimalPlaces = parts.length > 1 ? parts[1].length : 0;

              if (currentDecimalPlaces < roundValue) {
                resultStr = numValue.toFixed(roundValue).replace('.', ',');
              } else {
                resultStr = normalizedStr.replace('.', ',');
              }
            }
          }

          updates[resultKey] = resultStr;

          if (isBallast) {
            // Балластный: автозаполнение без индикатора ЕЛИС
            updates[`${resultKey}__elisFilled`] = false;
            trackAutoFill(resultKey, resultStr);
          } else if(elisParam.valueString && (normalizeValue(elisParam.value) !== normalizeValue(elisParam.valueString))) {
            updates[`${resultKey}__elisFilled`] = true;
            updates[`${resultKey}__elisOriginal`] = resultStr; // Сохраняем оригинал для восстановления флага
            trackElisLoad(resultKey, resultStr, elisData.protocolNumber);
          }
          else {
            updates[`${resultKey}__elisFilled`] = false;
            trackAutoFill(resultKey, resultStr);
          }
        } else {
          // result ожидалось, но не пришло
          updates[`${resultKey}__elisMissing`] = false;
          trackElisMissing(resultKey, elisData.protocolNumber);
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
            const methodJson = JSON.stringify(matchingMethod);
            updates[methodKey] = methodJson;
            updates[`${methodKey}__elisFilled`] = true;
            updates[`${methodKey}__elisOriginal`] = matchingMethod.name; // Сохраняем оригинал (name) для восстановления флага
            updates[`${methodKey}__elisOption`] = matchingMethod; // Сохраняем полный объект метода для возврата к нему после выбора другого

            // Создать запись истории для ELIS (сохраняем только name, а не весь объект)
            trackElisLoad(methodKey, matchingMethod.name, elisData.protocolNumber);
          } else {
            // Метод не удалось создать
            updates[`${methodKey}__elisMissing`] = true;
            trackElisMissing(methodKey, elisData.protocolNumber);
          }
        } else {
          // method ожидалось, но не пришло
          updates[`${methodKey}__elisMissing`] = true;
          trackElisMissing(methodKey, elisData.protocolNumber);
        }

        // Заполнить документ (LabDocumentInfo)
        const documentPayload = buildDocumentPayload(elisParam);

        if (documentPayload) {
          updates[documentKey] = documentPayload;
          updates[`${documentKey}__elisFilled`] = true;
          updates[`${documentKey}__elisOriginal`] = documentPayload; // Сохраняем оригинал для восстановления флага

          // Создать запись истории для ELIS
          trackElisLoad(documentKey, documentPayload, elisData.protocolNumber);
        } else {
          // document ожидалось, но не пришло
          updates[`${documentKey}__elisMissing`] = true;
          trackElisMissing(documentKey, elisData.protocolNumber);
        }
      } else {
        // Параметр не найден в ELIS - все поля помечаем как missing
        updates[`${valueKey}__elisMissing`] = true;
        updates[`${methodKey}__elisMissing`] = true;
        updates[`${resultKey}__elisMissing`] = false;
        updates[`${documentKey}__elisMissing`] = true;

        trackElisMissing(valueKey, elisData.protocolNumber);
        trackElisMissing(methodKey, elisData.protocolNumber);
        trackElisMissing(documentKey, elisData.protocolNumber);
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

/**
 * DEBUG: Обработчик обновления IOF подписанта с логированием
 */
function handleSignerIofUpdate(key: string, value: any) {
  logger.debug('[DocumentPassportEditor] handleSignerIofUpdate', {
    key,
    value,
    currentStoreValue: store.formData[key]
  });
  store.updateField(key, value);
}

/**
 * DEBUG: Обработчик обновления label подписанта с логированием
 */
function handleSignerIofLabelUpdate(key: string, label: string) {
  const labelKey = `${key}__label`;
  logger.debug('[DocumentPassportEditor] handleSignerIofLabelUpdate', {
    key,
    labelKey,
    label,
    currentStoreLabelValue: store.formData[labelKey]
  });
  store.updateField(labelKey, label);
}

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

// Предупреждение перед закрытием отключено:
// При переключении в режим просмотра форма закрывается без предупреждений
// (потеря несохранённых данных - ожидаемое поведение)
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
