export function normalizeDecimalValue(
  value: string | number | null | undefined,
  roundValue: number | null | undefined
): string {
  if (value === null || value === undefined) {
    return '';
  }

  const stringValue = String(value);
  const trimmedValue = stringValue.trim();
  if (trimmedValue === '') {
    return '';
  }

  const numericPattern = /^[+-]?\d+(?:[.,]\d+)?$/;
  if (!numericPattern.test(trimmedValue)) {
    return stringValue;
  }

  const normalized = trimmedValue.replace(',', '.');
  const numValue = parseFloat(normalized);
  if (isNaN(numValue)) {
    return stringValue;
  }

  if (!roundValue || roundValue <= 0) {
    return normalized.replace('.', ',');
  }

  const parts = normalized.split('.');
  const currentDecimalPlaces = parts.length > 1 ? parts[1].length : 0;

  if (currentDecimalPlaces < roundValue) {
    return numValue.toFixed(roundValue).replace('.', ',');
  }

  return normalized.replace('.', ',');
}
