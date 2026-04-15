/**
 * Lightweight, dependency-free platform detection used to pick the right
 * primary keyboard modifier (Cmd on macOS, Ctrl elsewhere) for global
 * shortcuts. Keep this util free of UI concerns so directives and
 * components can share it without coupling.
 */
export function isMac(): boolean {
  if (typeof navigator === 'undefined') return false;
  const platform =
    (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData?.platform ??
    navigator.platform ??
    navigator.userAgent;
  return /mac/i.test(platform);
}
