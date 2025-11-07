/**
 * Композабл для интеграции с ELIS (Единая Лабораторная Информационная Система)
 *
 * Обрабатывает данные протокола испытаний из ELIS и применяет их к форме редактирования паспорта качества.
 * Поддерживает fallback механизм для маппинга полей через массив алиасов.
 */

import { onMounted, onUnmounted } from 'vue';
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
  if (!elisAlias || elisAlias.length === 0) {
    return undefined;
  }

  // Определить корневой объект для поиска
  let searchRoot: any = elisData;

  if (searchPath) {
    // Поддержка вложенных путей через точку (например, "signers.laboratory")
    const pathParts = searchPath.split('.');
    for (const part of pathParts) {
      searchRoot = searchRoot?.[part];
      if (!searchRoot) {
        console.warn(`[ELIS] Путь поиска "${searchPath}" не найден в данных ELIS`);
        return undefined;
      }
    }
  }

  // Перебрать все алиасы и найти первое существующее значение
  for (const alias of elisAlias) {
    const value = searchRoot[alias];
    if (value !== undefined && value !== null) {
      console.log(`[ELIS] Найдено значение по алиасу "${alias}" в "${searchPath || 'root'}":`, value);
      return value;
    }
  }

  console.warn(
    `[ELIS] Не найдено значение для алиасов: [${elisAlias.join(', ')}] в "${searchPath || 'root'}"`
  );
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
        console.log(`[ELIS] Распознано пороговое значение: "${trimmed}" → ${limitValue} (${operator})`);
        return {
          limitValue,
          operator,
          limitValueString: trimmed,
        };
      }
    }
  }

  console.warn(`[ELIS] Не удалось распознать формат: "${trimmed}"`);
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
 * @param elisData - исходные данные ELIS
 * @returns обогащенные данные ELIS
 */
export function enrichElisData(elisData: ElisPassportData): ElisPassportData {
  const enriched = { ...elisData };

  // Форматировать ФИО представителя лаборатории
  if (elisData.signers?.laboratory) {
    const lab = elisData.signers.laboratory;
    const shortSign = formatShortName(lab.givenName, lab.middleName, lab.familyName);

    // Добавить сформированные поля в корень для удобства поиска
    if (!enriched.labInfo) {
      enriched.labInfo = {};
    }

    if (shortSign) {
      (enriched as any).chiefLabShortSign = shortSign;
      enriched.labInfo.chiefLabShortSign = shortSign;
    }

    if (lab.post) {
      (enriched as any).chiefLabPosition = lab.post;
      enriched.labInfo.chiefLabPosition = lab.post;
    }

    if (lab.company) {
      (enriched as any).chiefLabOrganization = lab.company;
      enriched.labInfo.chiefLabOrganization = lab.company;
    }

    console.log('[ELIS] Данные обогащены автоматически сформированными полями:', {
      chiefLabShortSign: shortSign,
      chiefLabPosition: lab.post,
      chiefLabOrganization: lab.company,
    });
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
 *   console.log('Получены данные ELIS:', elisData);
 *   // Применить данные к форме
 * });
 */
export function useElisIntegration(onElisDataReceived: (data: ElisPassportData) => void) {
  const handleMessage = (event: MessageEvent) => {
    // В production следует проверять event.origin для безопасности
    // if (event.origin !== window.location.origin) return;

    if (event.data && event.data.type === 'ELIS_DATA') {
      console.log('[useElisIntegration] Получены данные ELIS из главного окна:', event.data.payload);

      // Обогатить данные автоматически сформированными полями
      const enrichedData = enrichElisData(event.data.payload);

      // Вызвать callback с обогащенными данными
      onElisDataReceived(enrichedData);
    }
  };

  onMounted(() => {
    window.addEventListener('message', handleMessage);
    console.log('[useElisIntegration] Слушатель postMessage зарегистрирован');
  });

  onUnmounted(() => {
    window.removeEventListener('message', handleMessage);
    console.log('[useElisIntegration] Слушатель postMessage удалён');
  });
}
