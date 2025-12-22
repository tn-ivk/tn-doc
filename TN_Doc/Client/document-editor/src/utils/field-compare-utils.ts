import type { FormField } from '@/types/document.types';
import { normalizeDateTimeForComparison, normalizeValue } from '@/utils/passport-utils';

const LABEL_SUFFIX = '__label';

export const resolveFieldTypeForComparison = (
  fieldKey: string,
  fields: FormField[] = [],
  docType?: string
): FormField['type'] | undefined => {
  if (!fieldKey) {
    return undefined;
  }

  const baseKey = fieldKey.endsWith(LABEL_SUFFIX)
    ? fieldKey.slice(0, -LABEL_SUFFIX.length)
    : fieldKey;

  const field = fields.find(f => f.key === baseKey);
  if (field) {
    return field.type;
  }

  if (docType === 'Passport') {
    if (baseKey.startsWith('value.') || baseKey.startsWith('result.')) {
      return 'number';
    }

    if (baseKey.startsWith('method.') || baseKey.startsWith('document.')) {
      return 'text';
    }
  }

  return undefined;
};

export const normalizeForComparison = (
  fieldType: FormField['type'] | undefined,
  value: any
): string => {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  switch (fieldType) {
    case 'date':
    case 'datetime-local':
      return normalizeDateTimeForComparison(value);
    case 'number':
      return normalizeValue(value);
    case 'text':
    case 'select':
    case 'list':
      return String(value);
    default:
      return normalizeValue(value);
  }
};
