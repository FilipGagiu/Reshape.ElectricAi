/**
 * Convert a PascalCase / camelCase enum value into space-separated words.
 * Examples:
 *   "AcidHouse"   → "Acid House"
 *   "HipHop"      → "Hip Hop"
 *   "DrumAndBass" → "Drum And Bass"
 *   "RAndB"       → "R And B"
 *   "LoFi"        → "Lo Fi"
 *   "Other"       → "Other"
 */
export function humanizeEnum(value: string): string {
    if (!value) return value;
    return value
        // Split consecutive uppercase before an uppercase+lowercase pair: "URLPath" → "URL Path", "RAndB" → "R And B"
        .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
        // Split lowercase/digit before uppercase: "AcidHouse" → "Acid House"
        .replace(/([a-z\d])([A-Z])/g, '$1 $2')
        .trim();
}
