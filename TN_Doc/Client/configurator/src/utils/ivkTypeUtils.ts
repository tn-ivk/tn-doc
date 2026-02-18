import { IvkType } from '../types/config.types';
import type { IvkTypePreset, Device } from '../types/config.types';

export const IVK_PRESETS: Record<IvkType, IvkTypePreset> = {
  [IvkType.TN01]: {
    database: 'IVK_TN_01',
    opcUaPrefix: 'IVK_TN_01',
    opcDaSuffix: 'IVK_TN_01'
  },
  [IvkType.TN02]: {
    database: 'ivk_tn',
    opcUaPrefix: 'IVK_TN_02',
    opcDaSuffix: 'IVK_TN_02'
  }
};

/**
 * Определяет тип ИВК по косвенным признакам (значениям всех устройств).
 * Возвращает null если устройства неконсистентны или тип не определён.
 */
export function detectIvkType(devices: Device[]): IvkType | null {
  if (!devices || devices.length === 0) return null;

  for (const type of Object.values(IvkType)) {
    const preset = IVK_PRESETS[type];
    const allMatch = devices.every(device => {
      const dbMatch = device.DBConnectionStrings.every(
        conn => conn.Database === preset.database
      );
      if (!dbMatch) return false;

      const uaPrefix = device.OpcConnectionSettings?.UaSettings?.StartPrefix;
      if (uaPrefix !== preset.opcUaPrefix) return false;

      const daPrefix = device.OpcConnectionSettings?.DaSettings?.StartPrefix;
      if (!daPrefix || !daPrefix.includes('.')) return false;

      const lastSegment = daPrefix.substring(daPrefix.lastIndexOf('.') + 1);
      if (lastSegment !== preset.opcDaSuffix) return false;

      return true;
    });

    if (allMatch) return type;
  }

  return null;
}

/**
 * Применяет пресет типа ИВК ко всем устройствам.
 * Для OPC DA StartPrefix сохраняет PLC-часть, заменяя только последний сегмент.
 */
export function applyIvkType(devices: Device[], type: IvkType): void {
  const preset = IVK_PRESETS[type];

  for (const device of devices) {
    for (const conn of device.DBConnectionStrings) {
      conn.Database = preset.database;
    }

    if (device.OpcConnectionSettings?.UaSettings) {
      device.OpcConnectionSettings.UaSettings.StartPrefix = preset.opcUaPrefix;
    }

    if (device.OpcConnectionSettings?.DaSettings) {
      const currentPrefix = device.OpcConnectionSettings.DaSettings.StartPrefix;
      if (currentPrefix && currentPrefix.includes('.')) {
        const parts = currentPrefix.split('.');
        parts[parts.length - 1] = preset.opcDaSuffix;
        device.OpcConnectionSettings.DaSettings.StartPrefix = parts.join('.');
      } else {
        device.OpcConnectionSettings.DaSettings.StartPrefix = `Root.PLC1.${preset.opcDaSuffix}`;
      }
    }
  }
}
