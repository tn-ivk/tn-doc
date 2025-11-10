/**
 * Композабл для интеграции с ELIS (Единая Лабораторная Информационная Система)
 *
 * Обрабатывает данные протокола испытаний из ELIS и применяет их к форме редактирования паспорта качества.
 * Поддерживает fallback механизм для маппинга полей через массив алиасов.
 */

import { onMounted, onUnmounted } from 'vue';
import { logger } from '@tn-doc/shared';
import type { ElisPassportData, ElisParameter, ElisMethodData } from '@/types/elis.types';

/**
 * Ищет значение в данных ELIS по массиву алиасов (fallback механизм)
 *
 * @param elisData - данные из ELIS
 * @param elisAlias - массив возможных ключей для поиска
 * @param searchPath - опциональный путь для поиска (например, 'labInfo', 'parameters')
 * @returns найденное значение или undefined
 *
 * @example
 * // AdditionalInfo (поиск в корневом объекте и labInfo)
 * const labName = findElisValue(elisData, ["labName", "laboratoryName"]);
 *
 * @example
 * // Parameters (поиск в parameters с русскими названиями)
 * const waterContent = findElisValue(
 *   elisData,
 *   ["Массовая доля воды(%)", "Массовая концентрация воды(%)"],
 *   "parameters"
 * );
 */
export function findElisValue(
  elisData: ElisPassportData,
  elisAlias?: string[],
  searchPath?: string
): any {
  // КРИТИЧНЫЙ ЛОГ: Функция вызвана
  logger.error('[ELIS DEBUG] findElisValue() ВЫЗВАНА', {
    elisAlias,
    searchPath,
    elisDataKeys: elisData ? Object.keys(elisData) : 'elisData is null/undefined'
  });

  if (!elisAlias || elisAlias.length === 0) {
    logger.warn('[ELIS DEBUG] findElisValue: elisAlias пустой или undefined');
    return undefined;
  }

  // Определить корневой объект для поиска
  let searchRoot: any = elisData;

  if (searchPath) {
    // Поддержка вложенных путей через точку (например, "signers.laboratory")
    const pathParts = searchPath.split('.');
    logger.info(`[ELIS DEBUG] findElisValue: Поиск по пути "${searchPath}"`, {
      pathParts,
      elisAlias
    });

    for (const part of pathParts) {
      searchRoot = searchRoot?.[part];
      if (!searchRoot) {
        logger.warn('[ELIS DEBUG] findElisValue: Путь поиска не найден в данных ELIS', {
          searchPath,
          failedAt: part,
          availableKeys: searchRoot ? Object.keys(searchRoot) : 'searchRoot is undefined'
        });
        return undefined;
      }
    }

    // Логировать доступные ключи в найденном объекте
    if (searchRoot && typeof searchRoot === 'object') {
      logger.info(`[ELIS DEBUG] findElisValue: Объект поиска найден по пути "${searchPath}"`, {
        availableKeys: Object.keys(searchRoot),
        keysCount: Object.keys(searchRoot).length
      });
    }
  } else {
    // Поиск в корне
    logger.info(`[ELIS DEBUG] findElisValue: Поиск в корневом объекте`, {
      elisAlias,
      availableRootKeys: Object.keys(elisData),
      rootKeysCount: Object.keys(elisData).length
    });
  }

  // Перебрать все алиасы и найти первое существующее значение
  logger.info(`[ELIS DEBUG] Начинаем перебор ${elisAlias.length} алиасов`, { elisAlias });

  for (const alias of elisAlias) {
    logger.info(`[ELIS DEBUG] Проверяем алиас "${alias}" в searchRoot`, {
      alias,
      searchRootType: typeof searchRoot,
      hasProperty: searchRoot && typeof searchRoot === 'object' ? alias in searchRoot : false
    });

    const value = searchRoot[alias];
    if (value !== undefined && value !== null) {
      logger.info(`[ELIS DEBUG] ✅ findElisValue: Найден "${alias}" в "${searchPath || 'root'}"`, {
        alias,
        searchPath: searchPath || 'root',
        valueType: typeof value,
        valuePreview: typeof value === 'string' && value.length > 50 ? `${value.substring(0, 50)}...` : value
      });
      return value;
    } else {
      logger.info(`[ELIS DEBUG] ⚠️ findElisValue: Алиас "${alias}" не найден в "${searchPath || 'root'}"`);
    }
  }

  logger.warn(`[ELIS DEBUG] ❌ findElisValue: Ни один алиас не найден`, {
    elisAlias,
    searchPath: searchPath || 'root',
    searchedAliases: elisAlias,
    availableKeysInSearchRoot: searchRoot && typeof searchRoot === 'object' ? Object.keys(searchRoot) : 'not an object'
  });
  return undefined;
}

/**
 * Форматирует ФИО из ELIS данных в формат "И. О. Фамилия"
 *
 * @param givenName - имя
 * @param middleName - отчество
 * @param familyName - фамилия
 * @returns отформатированное ФИО или пустая строка
 *
 * @example
 * formatShortName("Иван", "Петрович", "Сидоров") // "И. П. Сидоров"
 */
export function formatShortName(
  givenName?: string,
  middleName?: string,
  familyName?: string
): string {
  if (!givenName || !middleName || !familyName) {
    return '';
  }

  const firstInitial = givenName.charAt(0).toUpperCase();
  const middleInitial = middleName.charAt(0).toUpperCase();

  return `${firstInitial}. ${middleInitial}. ${familyName}`;
}

/**
 * Парсит текстовое представление результата ELIS для извлечения limitValue и operator
 *
 * Поддерживаемые форматы:
 * - "Менее 4,0" → limitValue: 4.0, operator: 'less'
 * - "Более 10,5" → limitValue: 10.5, operator: 'more'
 * - "Не более 5" → limitValue: 5.0, operator: 'less_equal'
 * - "Не менее 3,5" → limitValue: 3.5, operator: 'more_equal'
 * - "До 8" → limitValue: 8.0, operator: 'less'
 * - "От 2" → limitValue: 2.0, operator: 'more_equal'
 *
 * @param valueString - текстовое представление из ELIS
 * @returns объект с limitValue, operator и limitValueString или null
 */
export function parseElisValueString(valueString: string): {
  limitValue: number;
  operator: 'less' | 'more' | 'less_equal' | 'more_equal';
  limitValueString: string;
} | null {
  if (!valueString) {
    return null;
  }

  const trimmed = valueString.trim();

  // Паттерны для парсинга (порядок важен!)
  const patterns = [
    { regex: /не\s+более\s+([0-9,\.]+)/i, operator: 'less_equal' as const },
    { regex: /не\s+менее\s+([0-9,\.]+)/i, operator: 'more_equal' as const },
    { regex: /менее\s+([0-9,\.]+)/i, operator: 'less' as const },
    { regex: /более\s+([0-9,\.]+)/i, operator: 'more' as const },
    { regex: /до\s+([0-9,\.]+)/i, operator: 'less' as const },
    { regex: /от\s+([0-9,\.]+)/i, operator: 'more_equal' as const },
  ];

  for (const { regex, operator } of patterns) {
    const match = trimmed.match(regex);
    if (match && match[1]) {
      // Заменить запятую на точку для парсинга числа
      const numberStr = match[1].replace(',', '.');
      const limitValue = parseFloat(numberStr);

      if (!isNaN(limitValue)) {
        logger.info('[ELIS] Распознано пороговое значение из текстового представления', {
          originalString: trimmed,
          limitValue,
          operator
        });
        return {
          limitValue,
          operator,
          limitValueString: trimmed,
        };
      }
    }
  }

  logger.warn('[ELIS] Не удалось распознать формат текстового представления', {
    valueString: trimmed
  });
  return null;
}

/**
 * Создает объект метода испытаний из данных ELIS параметра
 *
 * @param elisParam - параметр качества из ELIS
 * @returns объект метода или null
 */
export function createMethodFromElisData(elisParam: ElisParameter): ElisMethodData | null {
  if (!elisParam.testMethodName) {
    return null;
  }

  const methodData: ElisMethodData = {
    name: elisParam.testMethodName,
  };

  // Попытаться распарсить valueString для получения limitValue
  if (elisParam.valueString) {
    const parsed = parseElisValueString(elisParam.valueString);
    if (parsed) {
      methodData.limitValue = parsed.limitValue;
      methodData.operator = parsed.operator;
      methodData.limitValueString = parsed.limitValueString;
    }
  }

  return methodData;
}

/**
 * Обогащает данные ELIS автоматически сформированными полями
 * (например, chiefLabShortSign из givenName, middleName, familyName)
 *
 * Функция выполняет:
 * 1. Форматирование ФИО представителя лаборатории (givenName, middleName, familyName → "И. О. Фамилия")
 * 2. Копирование полей из signers.laboratory в корень и labInfo
 * 3. Копирование labName в labInfo (если отсутствует)
 *
 * @param elisData - исходные данные ELIS
 * @returns обогащенные данные ELIS
 */
export function enrichElisData(elisData: ElisPassportData): ElisPassportData {
  logger.info('[ELIS DEBUG] enrichElisData: Начало обогащения данных ELIS', {
    rootKeys: Object.keys(elisData),
    hasLabInfo: !!elisData.labInfo,
    hasSigners: !!elisData.signers,
    hasSignersLaboratory: !!elisData.signers?.laboratory,
    hasParameters: !!elisData.parameters,
    parametersCount: elisData.parameters ? Object.keys(elisData.parameters).length : 0
  });

  const enriched = { ...elisData };

  // Инициализировать labInfo, если отсутствует
  if (!enriched.labInfo) {
    logger.warn('[ELIS DEBUG] enrichElisData: labInfo отсутствует, создаем пустой объект');
    enriched.labInfo = {};
  } else {
    logger.info('[ELIS DEBUG] enrichElisData: labInfo присутствует', {
      labInfoKeys: Object.keys(enriched.labInfo)
    });
  }

  // Копировать labName в labInfo для единообразия (если отсутствует в labInfo)
  if (elisData.labName && !enriched.labInfo.labName) {
    enriched.labInfo.labName = elisData.labName;
    logger.info(`[ELIS DEBUG] enrichElisData: Скопировано labName в labInfo = "${elisData.labName}"`);
  }

  // Форматировать ФИО представителя лаборатории
  if (elisData.signers?.laboratory) {
    const lab = elisData.signers.laboratory;
    const shortSign = formatShortName(lab.givenName, lab.middleName, lab.familyName);

    if (shortSign) {
      enriched.chiefLabShortSign = shortSign;
      enriched.labInfo.chiefLabShortSign = shortSign;
      logger.info(`[ELIS DEBUG] enrichElisData: Добавлено chiefLabShortSign = "${shortSign}"`);
    }

    if (lab.post) {
      enriched.chiefLabPosition = lab.post;
      enriched.labInfo.chiefLabPosition = lab.post;
      logger.info(`[ELIS DEBUG] enrichElisData: Добавлено chiefLabPosition = "${lab.post}"`);
    }

    if (lab.company) {
      enriched.chiefLabOrganization = lab.company;
      enriched.labInfo.chiefLabOrganization = lab.company;
      logger.info(`[ELIS DEBUG] enrichElisData: Добавлено chiefLabOrganization = "${lab.company}"`);
    }

    logger.info('[ELIS DEBUG] enrichElisData: Данные обогащены автоматически сформированными полями', {
      chiefLabShortSign: shortSign,
      chiefLabPosition: lab.post,
      chiefLabOrganization: lab.company
    });
  } else {
    logger.warn('[ELIS DEBUG] enrichElisData: signers.laboratory отсутствует, автоматические поля не добавлены');
  }

  if (elisData.labName && !enriched.labInfo.labName) {
    logger.info(`[ELIS DEBUG] enrichElisData: Скопирован labName в labInfo = "${elisData.labName}"`);
  }

  return enriched;
}

/**
 * Композабл для приёма данных ELIS через postMessage
 *
 * @param onElisDataReceived - callback для обработки полученных данных
 *
 * @example
 * useElisIntegration((elisData) => {
 *   logger.info('Получены данные ELIS:', elisData);
 *   // Применить данные к форме
 * });
 */
export function useElisIntegration(onElisDataReceived: (data: ElisPassportData) => void) {
  const handleMessage = (event: MessageEvent) => {
    // В production следует проверять event.origin для безопасности
    // if (event.origin !== window.location.origin) return;

    if (event.data && event.data.type === 'ELIS_DATA') {
      logger.info('[useElisIntegration] Получены данные ELIS из главного окна', {
        payloadKeys: Object.keys(event.data.payload || {})
      });

      // Обогатить данные автоматически сформированными полями
      const enrichedData = enrichElisData(event.data.payload);

      // Вызвать callback с обогащенными данными
      onElisDataReceived(enrichedData);
    }
  };

  onMounted(() => {
    window.addEventListener('message', handleMessage);
    logger.info('[useElisIntegration] Слушатель postMessage зарегистрирован');
  });

  onUnmounted(() => {
    window.removeEventListener('message', handleMessage);
    logger.info('[useElisIntegration] Слушатель postMessage удалён');
  });
}
