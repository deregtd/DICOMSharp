declare module 'dicom-parser' {
    export interface DicomParserOptions {
        untilTag?: string;
        vrCallback?: (vr: string) => string|undefined;
        inflater?: (array: Uint8Array, offset: number) => Uint8Array;
    }

    export function parseDicom(array: Uint8Array, options?: DicomParserOptions): DPDicomDataSet;

    export interface DPDicomDataSet {
        byteArray: Uint8Array;
        elements: {[tag: string]: DPDicomElement };

        uint16(tag: string, index?: number): number;
        int16(tag: string, index?: number): number;
        uint32(tag: string, index?: number): number;
        int32(tag: string, index?: number): number;
        float(tag: string, index?: number): number;
        double(tag: string, index?: number): number;

        numStringValues(tag: string): number;
        string(tag: string, index?: number): string;
        text(tag: string, index?: number): string;

        floatString(tag: string, index?: number): number;
        intString(tag: string, index?: number): number;
    }

    export interface DPDicomElement {
        tag: string;
        vr?: string;

        dataOffset: number;
        length: number;
        hadUndefinedLength?: boolean;

        items?: DPSQElement[];

        encapsulatedPixelData?: boolean;
        basicOffsetTable?: number[];
        fragments: DPImageFragment[];
    }

    export interface DPSQElement {
        tag: string;
        length: number;
        dataOffset: number;
        dataSet: DPDicomDataSet;
    }

    export interface DPImageFragment {
        length: number;
        offset: number;
        position: number;
    }
}
