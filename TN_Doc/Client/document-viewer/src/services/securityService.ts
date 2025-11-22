import { logger } from '@tn-doc/shared';
import type { Permissions } from '../types';
import { readOpcCachedTag } from './opcService';
import { isSecurityEnabled } from '../api/homeApi';

export const defaultPermissions: Permissions = {
  showEditAndSave: true,
  allowEditAndSave: true,
  showPrint: true,
  allowPrint: true,
  showExport: true,
  allowExport: true,
  showDictionaries: true,
  allowDictionaries: true
};

const securityTags: Record<keyof Permissions, string> = {
  showEditAndSave: 'root.ARM.Reports.ShowEditAndSave',
  allowEditAndSave: 'root.ARM.Reports.AllowEditAndSave',
  showPrint: 'root.ARM.Reports.ShowPrint',
  allowPrint: 'root.ARM.Reports.AllowPrint',
  showExport: 'root.ARM.Reports.ShowExport',
  allowExport: 'root.ARM.Reports.AllowExport',
  showDictionaries: 'root.ARM.Reports.ShowEditDictionaries',
  allowDictionaries: 'root.ARM.Reports.AllowEditDictionaries'
};

export async function loadPermissions(): Promise<Permissions> {
  const useSecurity = await isSecurityEnabled();
  if (!useSecurity) {
    return { ...defaultPermissions };
  }

  try {
    const entries = await Promise.all(
      Object.entries(securityTags).map(async ([key, tag]) => {
        const value = await readOpcCachedTag('ARM', tag, 1, 0);
        return [key, Boolean(value)] as [keyof Permissions, boolean];
      })
    );

    const permissions = { ...defaultPermissions };
    entries.forEach(([key, value]) => {
      permissions[key] = value;
    });

    return permissions;
  } catch (error: any) {
    logger.warn('[securityService] Ошибка загрузки прав, используются значения по умолчанию', {
      error: error?.message || error?.toString()
    });
    return { ...defaultPermissions };
  }
}

export function applySecurityUpdate(tagName: string, tagValue: any, permissions: Permissions): Permissions {
  const entry = Object.entries(securityTags).find(([, tag]) => tag === tagName);
  if (!entry) {
    return permissions;
  }

  const [key] = entry as [keyof Permissions, string];
  return {
    ...permissions,
    [key]: Boolean(tagValue)
  };
}
