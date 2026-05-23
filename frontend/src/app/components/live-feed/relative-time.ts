const MINUTE = 60_000;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;

export function formatRelativePast(target: Date, now: Date = new Date()): string {
    const diff = now.getTime() - target.getTime();
    if (diff < MINUTE) return 'just now';
    if (diff < HOUR) return `${Math.floor(diff / MINUTE)}m ago`;
    if (diff < DAY) return `${Math.floor(diff / HOUR)}h ago`;
    return `${Math.floor(diff / DAY)}d ago`;
}

export function formatRelativeFuture(target: Date, now: Date = new Date()): string {
    const diff = target.getTime() - now.getTime();
    if (diff <= 0) return 'now';
    if (diff < HOUR) return `in ${Math.ceil(diff / MINUTE)} min`;
    if (diff < DAY) return `in ${Math.floor(diff / HOUR)}h`;
    return `in ${Math.floor(diff / DAY)}d`;
}

export function formatClock(target: Date): string {
    return target.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
}
