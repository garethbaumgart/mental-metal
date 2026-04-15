/**
 * Shared helpers for the dashboard widgets. Keeping them here (rather than
 * sprinkled across widget files) makes it obvious that every widget
 * honours the same isolation contract: loading / error / empty / data
 * states, each widget fails independently, and each filters/sorts its
 * own slice of data.
 */

/** Local-time "today" as a YYYY-MM-DD string. */
export function todayLocalIso(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * True if an ISO date-only or date-time string falls on or before today
 * (local). Used by the "today's commitments" widget to include anything
 * overdue alongside the items due today.
 */
export function isOnOrBeforeToday(isoDate: string | null | undefined): boolean {
  if (!isoDate) return false;
  const today = todayLocalIso();
  return isoDate.slice(0, 10) <= today;
}

/** True if the calendar day portion of an ISO string is today (local). */
export function isToday(isoDate: string | null | undefined): boolean {
  if (!isoDate) return false;
  return isoDate.slice(0, 10) === todayLocalIso();
}
