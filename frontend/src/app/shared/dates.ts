/**
 * Due dates are calendar dates ("yyyy-MM-dd") with no time zone. `new Date("2026-11-14")` would read them as
 * midnight UTC, which in Bogotá (UTC-5) is the previous day; these helpers always work with the local calendar.
 */

/** Today's date in the browser's local calendar, as "yyyy-MM-dd". */
export function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/** "2026-11-14" → "Nov 14, 2026", without any time zone shift. */
export function formatDueDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}
