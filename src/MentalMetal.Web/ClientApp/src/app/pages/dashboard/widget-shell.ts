/**
 * Shared helpers for the dashboard widgets. Keeping them here (rather than
 * sprinkled across widget files) makes it obvious that every widget
 * honours the same isolation contract: loading / error / empty / data
 * states, each widget fails independently, and each filters/sorts its
 * own slice of data.
 */

/** Local-time "today" as a YYYY-MM-DD string. */
export function todayLocalIso(): string {
  // new Date() always yields a valid date, so this narrowing is safe.
  return toLocalDateKey(new Date()) as string;
}

/**
 * Convert an arbitrary ISO string or Date into a local YYYY-MM-DD key.
 *
 * DateOnly strings (`YYYY-MM-DD`) are returned verbatim because they
 * carry no timezone information — they are what the user typed. Full
 * ISO datetimes (`YYYY-MM-DDTHH:mm:ssZ`) are re-projected into the
 * browser's local calendar day, since slicing them raw would compare the
 * UTC calendar day instead.
 */
export function toLocalDateKey(input: string | Date | null | undefined): string | null {
  if (!input) return null;
  if (typeof input === 'string') {
    // Plain YYYY-MM-DD — treat as user-local calendar day.
    if (/^\d{4}-\d{2}-\d{2}$/.test(input)) return input;
    const parsed = new Date(input);
    if (Number.isNaN(parsed.getTime())) return null;
    return toLocalDateKey(parsed);
  }
  const y = input.getFullYear();
  const m = String(input.getMonth() + 1).padStart(2, '0');
  const day = String(input.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * True if the provided date (or datetime) falls on or before today in
 * the user's local timezone. Used to include anything overdue alongside
 * items due today.
 */
export function isOnOrBeforeToday(iso: string | null | undefined): boolean {
  const key = toLocalDateKey(iso);
  return !!key && key <= todayLocalIso();
}

/** True if the calendar day portion of the provided date is today (local). */
export function isToday(iso: string | null | undefined): boolean {
  const key = toLocalDateKey(iso);
  return key === todayLocalIso();
}
