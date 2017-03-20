declare class TextDecoder {
    decode(dv: DataView): string
};

class StringUtils {
    static getAgeString(refDate: Date, birthDate: Date): string {
        if (!refDate || refDate.getFullYear() < 1000 || !birthDate || birthDate.getFullYear() < 1000) {
            return null;
        }
        const dateDiff = new Date(refDate.getTime() - birthDate.getTime());
        const yearDiff = dateDiff.getFullYear() - 1970;
        const monthDiff = dateDiff.getMonth();
        const dayDiff = dateDiff.getDate() - 1;

        if (yearDiff >= 2) {
            return yearDiff + 'Y';
        }
        if (monthDiff >= 2) {
            return monthDiff + 'M';
        }
        return dayDiff + 'D';
    }

    static padZeroes(inNum: number, numDigits: number) {
        let inStr = inNum.toString();
        while (inStr.length < numDigits) {
            inStr = '0' + inStr;
        }
        return inStr;
    }

    static formatName(name: string) {
        return name.replace(/\^*$/g, '').replace(/\^/g, ',');
    }

    static arrayToString(arr: Uint8Array, start: number, length: number): string {
        if (typeof DataView !== 'undefined' && typeof TextDecoder !== 'undefined') {
            const dataView = new DataView(arr.buffer, start, length);
            const decoder = new TextDecoder();
            return decoder.decode(dataView);
        }

        // Fall back to the lame way -- against all rational analysis, this is faster than using String.fromCharCode.apply with larger
        // chunks, by quite a bit.
        let jsonStr = '';
        for (let i = start; i < start + length; i ++) {
            jsonStr += String.fromCharCode(arr[i]);
        }
        return jsonStr;
    }
}

export = StringUtils;
