class DicomUtils {
    static GetDateTimeFromTags(date: string, time?: string): Date {
        if (!date) {
            return null;
        }
        
        date = date.replace(/[^0-9]/g, '');

        if (date.length !== 8) {
            return null;
        }

        var outDate = new Date(Number(date.substr(0, 4)), Number(date.substr(4, 2)) - 1, Number(date.substr(6, 2)));

        if (time) {
            time = time.replace(/[^0-9\.]/g, '');

            if (time.length >= 4) {
                outDate.setHours(Number(time.substr(0, 2)), Number(time.substr(2, 2)));

                if (time.length >= 6) {
                    outDate.setSeconds(Number(time.substr(4, 2)));
                }

                if (time.length > 7) {
                    // Get milliseconds out
                    let fracPart = time.substr(7);
                    if (fracPart.length > 3) {
                        // Don't care about sub-milliseconds
                        fracPart = fracPart.substr(0, 3);
                    }
                    while (fracPart.length < 3) {
                        fracPart += '0';
                    }
                    outDate.setMilliseconds(Number(fracPart));
                }
            }
        }

        return outDate;
    }

    static getDwordFromBuffer(buffer: Uint8Array, index: number) {
        return buffer[index] | (buffer[index + 1] << 8) | (buffer[index + 2] << 16) | (buffer[index + 3] << 24);
    }

    static padLeft(str: string, num: number) {
        while (str.length < num) {
            str = '0' + str;
        }
        return str;
    }

    static MakeTag(group: number, elem: number): string {
        return 'x' + this.padLeft(group.toString(16), 4) + this.padLeft(elem.toString(16), 4);
    }
}

export = DicomUtils;
