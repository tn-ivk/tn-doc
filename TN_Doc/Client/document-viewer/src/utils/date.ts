export function toApiDate(value: string | Date): string {
  const date = typeof value === 'string' ? new Date(value) : value;
  const day = `${date.getDate()}`.padStart(2, '0');
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const year = date.getFullYear();
  return `${day}.${month}.${year}`;
}

export function todayRange() {
  const now = new Date();
  const tomorrow = new Date(now);
  tomorrow.setDate(now.getDate() + 1);
  return {
    begin: now.toISOString().substring(0, 10),
    end: tomorrow.toISOString().substring(0, 10)
  };
}
