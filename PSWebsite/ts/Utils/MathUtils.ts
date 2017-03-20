export function roundTo(num: number, places: number) {
    const mult = Math.pow(10, places);
    return Math.round(num * mult) / mult;
}
