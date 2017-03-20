import DicomParser = require('dicom-parser');
import _ = require('lodash');

import DicomTags = require('../Utils/DicomTags');
import DicomUtils = require('../Utils/DicomUtils');
import Point2D = require('../Utils/Point2D');
import Point3D = require('../Utils/Point3D');
import StringUtils = require('../Utils/StringUtils');
import VRLookup = require('../Utils/VRLookup');

class DicomImage {
    valid = false;

    frameData: ArrayLike<number>[] = [];

    private _dataSet: DicomParser.DPDicomDataSet;

    // Returns the number of bytes read from the buffer
    parseFromBuffer(buffer: Uint8Array, offset: number = 0): number {
        let ptr = offset;

        // The whole length of this image block, including this 4 byte length field.
        const wholeLength = DicomUtils.getDwordFromBuffer(buffer, ptr);
        ptr += 4;

        const imageByteArray = new Uint8Array(buffer.buffer, ptr, wholeLength - 4);
        try {
            this._dataSet = DicomParser.parseDicom(imageByteArray, { vrCallback: VRLookup.LookupVR.bind(VRLookup) });
            
            this._parseFrameData();

            this.valid = this.frameData && this.frameData.length > 0;
        } catch (e) {
            console.error('DICOM Parse Error: ', e);
        }

        return wholeLength;
    }

    private _parseFrameData() {
        const pixelData = this._dataSet.elements[DicomTags.PixelData];
        if (!pixelData) {
            return;
        }

        const width = this.getNumberOrDefault(DicomTags.Columns, 0);
        const height = this.getNumberOrDefault(DicomTags.Rows, 0);
        const imageBits = this.getNumberOrDefault(DicomTags.BitsAllocated, 8);
        const photoInterp = this.getDisplayOrDefault(DicomTags.PhotometricInterpretation);
        const rgb = photoInterp === 'RGB';
        
        let bytesPerFrame = width * height * (imageBits > 8 ? 2 : 1) * (rgb ? 3 : 1);
        let numFrames = this.getNumberOrDefault(DicomTags.NumberOfFrames, 1);

        // Make sure we don't overflow
        if (bytesPerFrame * numFrames > pixelData.length) {
            if (numFrames > 1) {
                numFrames = Math.floor(pixelData.length / bytesPerFrame);
                if (numFrames === 0) {
                    numFrames = 1;
                    bytesPerFrame = pixelData.length; 
                }
            } else {
                bytesPerFrame = pixelData.length; 
            }
        }

        const pixelRep = this.getNumberOrDefault(DicomTags.PixelRepresentation, 0);

        this.frameData = [];
        for (let i = 0; i < numFrames; i++) {
            let offset: number;
            if (pixelData.encapsulatedPixelData) {
                offset = pixelData.fragments[i].position;
            } else {
                offset = pixelData.dataOffset + bytesPerFrame * i;
            }

            if (imageBits <= 8) {
                if (pixelRep === 0) {
                    this.frameData.push(new Uint8Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + offset, bytesPerFrame));
                } else {
                    this.frameData.push(new Int8Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + offset, bytesPerFrame));
                }
            } else {
                if (pixelRep === 0) {
                    this.frameData.push(new Uint16Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + offset, bytesPerFrame / 2));
                } else {
                    this.frameData.push(new Int16Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + offset, bytesPerFrame / 2));
                }
            }
        }
    }

    getPatientInfo(): DicomPatientInfo {
        return {
            patId: this.getDisplayOrDefault(DicomTags.PatientID).trim(),
            patBirthDate: DicomUtils.GetDateTimeFromTags(this.getDisplayOrDefault(DicomTags.PatientBirthDate),
                this.getDisplayOrDefault(DicomTags.PatientBirthTime)),
            patName: this.getDisplayOrDefault(DicomTags.PatientName, '').trim(),
            patSex: this.getDisplayOrDefault(DicomTags.PatientSex, '').trim()
        };
    }

    getStudyInfo(): DicomStudyInfo {
        return {
            studyInstanceUID: this.getDisplayOrDefault(DicomTags.StudyInstanceUID),
            studyID: this.getDisplayOrDefault(DicomTags.StudyID, '').trim(),
            accessionNumber: this.getDisplayOrDefault(DicomTags.AccessionNumber, '').trim(),
            studyLocation: this.getDisplayOrDefault(DicomTags.InstitutionName, '').trim(),
            modality: this.getDisplayOrDefault(DicomTags.Modality, '').trim(),
            studyDateTime: DicomUtils.GetDateTimeFromTags(this.getDisplayOrDefault(DicomTags.StudyDate),
                this.getDisplayOrDefault(DicomTags.StudyTime)),
            studyDescription: this.getDisplayOrDefault(DicomTags.StudyDescription, '').trim()
        };
    }

    getSeriesInfo(): DicomSeriesInfo {
        return {
            studyInstanceUID: this.getDisplayOrDefault(DicomTags.StudyInstanceUID),
            seriesInstanceUID: this.getDisplayOrDefault(DicomTags.SeriesInstanceUID),
            seriesNumber: Number(this.getDisplayOrDefault(DicomTags.SeriesNumber)),
            seriesDateTime: DicomUtils.GetDateTimeFromTags(this.getDisplayOrDefault(DicomTags.SeriesDate),
                this.getDisplayOrDefault(DicomTags.SeriesTime)),
            seriesDescription: this.getDisplayOrDefault(DicomTags.SeriesDescription, '').trim()
        };
    }

    private _getRawValue(elem: DicomParser.DPDicomElement, index?: number): string|number|Uint8Array|Uint16Array|undefined {
        switch (elem.vr || VRLookup.LookupVR(elem.tag)) {
            case 'UL':
                return this._dataSet.uint32(elem.tag, index);
            case 'SL':
                return this._dataSet.int32(elem.tag, index);
            case 'US':
                return this._dataSet.uint16(elem.tag, index);
            case 'SS':
                return this._dataSet.int16(elem.tag, index);
            case 'FL':
                return this._dataSet.float(elem.tag, index);
            case 'FD':
                return this._dataSet.double(elem.tag, index);
            case 'OB':
                return new Uint8Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length);
            case 'OW':
                return new Uint16Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 2);
            case 'AE':
            case 'CS':
            case 'SH':
            case 'LO':
            case 'PN':
            case 'UI':
            case 'IS':
            case 'DS':
            case 'DT':
            case 'DA':
            case 'TM':
                return this._dataSet.string(elem.tag, index);
            case 'UT':
            case 'ST':
            case 'LT':
                return this._dataSet.text(elem.tag, index);
            default:
                console.log('Unhandled VR in _getRawValue: ' + elem.vr + ' for tag ' + elem.tag);
                return undefined;
        };
    }

    getDisplayOrDefault(tag: string, defaultVal: string = ''): string {
        const elem = this._dataSet.elements[tag];
        if (!elem) {
            return defaultVal;
        }
        if (elem.vr === 'SQ') {
            return 'Data Sequence';
        }
        const rawData = this._getRawValue(elem);
        if (typeof rawData === 'number') {
            return rawData.toString();
        }
        if (typeof rawData === 'object') {
            const arr = rawData as ArrayLike<number>;
            return 'List: {' + _.take(arr, 5).join(',') +
                (arr.length > 5 ? ',[' + (arr.length - 5) + ' more]}' : '}');
        }

        return rawData as string;
    }

    getNumberOrDefault(tag: string, defaultVal: number = 0): number {
        const elem = this._dataSet.elements[tag];
        if (!elem) {
            return defaultVal;
        }
        const rawData = this._getRawValue(elem);
        if (typeof rawData === 'number') {
            return rawData as number;
        }
        if (typeof rawData === 'object') {
            return (rawData as ArrayLike<number>)[0];
        }
        // String... Might be a multi-elem value, separated by \.  Just get the first one.
        let str = rawData as string;
        const slashIndex = str.indexOf('\\');
        if (slashIndex >= 0) {
            str = str.substr(0, slashIndex);
        }
        return Number(str);
    }

    getNumberArray(tag: string): ArrayLike<number> {
        const elem = this._dataSet.elements[tag];
        if (!elem) {
            return undefined;
        }
        switch (elem.vr || VRLookup.LookupVR(tag)) {
            case 'UL':
                return new Uint32Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 4);
            case 'SL':
                return new Int32Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 4);
            case 'US':
                return new Uint16Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 2);
            case 'SS':
                return new Int16Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 2);
            case 'FL':
                return new Float32Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 4);
            case 'FD':
                return new Float64Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 8);
            case 'OB':
                return new Uint8Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length);
            case 'OW':
                return new Uint16Array(this._dataSet.byteArray.buffer, this._dataSet.byteArray.byteOffset + elem.dataOffset, elem.length / 2);
            case 'AE':
            case 'CS':
            case 'SH':
            case 'LO':
            case 'PN':
            case 'UI':
            case 'IS':
            case 'DS':
            case 'DT':
            case 'DA':
            case 'TM':
            case 'UT':
            case 'ST':
            case 'LT':
                return this._dataSet.text(tag).split('\\').map(val => Number(val));

            default:
                console.log('Unhandled VR in getNumberArray: ' + elem.vr);
                return undefined;
        };        
    }

    getWindowCenter() {
        if (this._dataSet.elements[DicomTags.WindowCenter]) {
            return this.getNumberOrDefault(DicomTags.WindowCenter);
        }

        // Calculate it
        const minMax = this.getPixelDataMinMax();
        return Math.round((minMax[1] - minMax[0]) / 2);
    }

    getWindowWidth() {
        if (this._dataSet.elements[DicomTags.WindowWidth]) {
            return this.getNumberOrDefault(DicomTags.WindowWidth);
        }

        // Calculate it
        const minMax = this.getPixelDataMinMax();
        return minMax[1] - minMax[0];
    }

    getWidth(): number {
        return this.getNumberOrDefault(DicomTags.Columns);
    }

    getHeight(): number {
        return this.getNumberOrDefault(DicomTags.Rows);
    }

    getSize(): Point2D {
        return new Point2D(this.getWidth(), this.getHeight());
    }

    private _minMaxCache: number[] = null;
    getPixelDataMinMax(): number[] {
        if (!this._minMaxCache) {
            // This function toook ~0.14ms to run on a 256x256 image
            let min = 0, max = 0;
            this.frameData.forEach(frame => {
                for (var i = 0; i < frame.length; i++) {
                    if (frame[i] > max) {
                        max = frame[i];
                    } else if (frame[i] < min) {
                        min = frame[i];
                    }
                }
            });
            this._minMaxCache = [min, max];
        }
        return this._minMaxCache;
    }

    buildLUT(invert: boolean, windowCenter: number, windowWidth: number): { lut: Uint8Array, offset: number } {
        let adjM = this.getNumberOrDefault(DicomTags.RescaleSlope, 1.0);
        let adjB = this.getNumberOrDefault(DicomTags.RescaleIntercept, 0);
        let bRescaling = (adjM !== 1.0 || adjB !== 0);

        if (this._dataSet.elements[DicomTags.PixelIntensityRelationship]) {
            const intensityRelationship = this.getDisplayOrDefault(DicomTags.PixelIntensityRelationship).trim().toUpperCase();
            if (intensityRelationship == "LIN") {
                if (this._dataSet.elements[DicomTags.PixelIntensityRelationshipSign]) {
                    const newAdj = this.getNumberOrDefault(DicomTags.PixelIntensityRelationshipSign, 1);
                    if (newAdj == -1) {
                        adjM *= newAdj;

                        const bitsStored = this.getNumberOrDefault(DicomTags.BitsStored);
                        if (bitsStored == 16) {
                            adjB += 65536;
                        }
                        else if (bitsStored == 12) {
                            adjB += 4096;
                        }
                        else if (bitsStored == 8) {
                            adjB += 256;
                        }

                        bRescaling = true;
                    }
                }
            }
        }

        const photoInterp = this.getDisplayOrDefault(DicomTags.PhotometricInterpretation).trim();
        let bInvert = (photoInterp === "MONOCHROME1");
        if (invert) {
            bInvert = !bInvert;
        }

        // TODO: Back-calc the zero point and shortcut the front of the calc
        const minMax = this.getPixelDataMinMax();
        let lut = new Uint8Array(minMax[1] - minMax[0] + 1);
        const lutZeroOffset = -minMax[0];

        let hitMax = false;
        for (let val = minMax[0]; val <= minMax[1]; val++) {
            if (hitMax) {
                lut[val + lutZeroOffset] = bInvert ? 0 : 255;
            } else {
                let src = val;

                if (bRescaling) {
                    src = (src * adjM + adjB + 0.5) << 0;   // Math.round
                }

                src = ((src - windowCenter) * (255 / windowWidth) + 128) << 0;	// Math.floor
                if (src < 0) src = 0; else if (src > 255) src = 255;

                // If we have a negative slope, don't try to optimize this yet.  Maybe someday, but they're so rare it's probably not
                // worth it.
                if (src === 255 && adjM > 0) {
                    hitMax = true;
                }

                if (bInvert) {
                    src ^= 0xff;
                }

                lut[val + lutZeroOffset] = src;
            }
        }

        return {
            lut: lut,
            offset: lutZeroOffset
        };
    }

    private static _reshape(inset: ArrayLike<number>): number[] {
        let outArr: number[] = Array(inset.length);
        for (let i = 0; i < outArr.length; i++) {
            outArr[i] = ((inset[i] & 0xFF) << 8) | ((inset[i] & 0xFF00) >> 8);
        }
        return outArr;
    }

    buildPalettes() {
        if (!this._dataSet.elements[DicomTags.RedPaletteColorLookupTableData] ||
            !this._dataSet.elements[DicomTags.GreenPaletteColorLookupTableData] ||
            !this._dataSet.elements[DicomTags.BluePaletteColorLookupTableData]) {
            // TODO: Handle this better.
            throw 'Missing a Palette Lookup field on a PALETTE COLORed image, can\'t render properly.';
        }
        if (!this._dataSet.elements[DicomTags.RedPaletteColorLookupTableDescriptor] ||
            !this._dataSet.elements[DicomTags.GreenPaletteColorLookupTableDescriptor] ||
            !this._dataSet.elements[DicomTags.BluePaletteColorLookupTableDescriptor]) {
            // TODO: Handle this better.
            throw 'Missing a Palette Descriptor field on a PALETTE COLORed image, can\'t render.';
        }

        //PS 3, Page 417-418
        const redPaletteDescriptor = this.getNumberArray(DicomTags.RedPaletteColorLookupTableDescriptor);
        const palette16 = redPaletteDescriptor[2] === 16;
        let paletteNumEntries = redPaletteDescriptor[0];
        if (paletteNumEntries === 0) {
            paletteNumEntries = 65536;
        }
        const paletteFirstEntry = redPaletteDescriptor[1];

        let palettes: ArrayLike<number>[];
        if (palette16) {
            palettes = [
                DicomImage._reshape(this.getNumberArray(DicomTags.RedPaletteColorLookupTableData)),
                DicomImage._reshape(this.getNumberArray(DicomTags.GreenPaletteColorLookupTableData)),
                DicomImage._reshape(this.getNumberArray(DicomTags.BluePaletteColorLookupTableData))
            ];
        } else {
            palettes = [
                this.getNumberArray(DicomTags.RedPaletteColorLookupTableData),
                this.getNumberArray(DicomTags.GreenPaletteColorLookupTableData),
                this.getNumberArray(DicomTags.BluePaletteColorLookupTableData)
            ];
        }

        // TODO: figure out supplemental palette color LUT at some point

        return {
            palettes: palettes,
            numEntries: paletteNumEntries,
            firstEntry: paletteFirstEntry
        };
    }

    getPixelSpacing(): Point2D {
        const spacingElem = this.getNumberArray(DicomTags.PixelSpacing);
        if (!spacingElem || spacingElem.length === 0) {
            return null;
        }

        if (spacingElem.length === 1) {
            return new Point2D(spacingElem[0], spacingElem[0]);
        }

        return new Point2D(spacingElem[0], spacingElem[1]);
    }

    getSliceLocation(): number {
        return this.getNumberOrDefault(DicomTags.SliceLocation, 0);
    }

    hasPositionData(): boolean {
        return !!((this._dataSet.elements[DicomTags.ImagePositionPatient] || this._dataSet.elements[DicomTags.ImagePosition]) &&
            (this._dataSet.elements[DicomTags.ImageOrientationPatient] || this._dataSet.elements[DicomTags.ImageOrientation]));
    }

    getImagePosition(): Point3D {
        const positionElem = this.getNumberArray(DicomTags.ImagePositionPatient) || this.getNumberArray(DicomTags.ImagePosition);
        if (!positionElem || positionElem.length < 3) {
            return null;
        }

        return new Point3D(positionElem[0], positionElem[1], positionElem[2]);
    }

    getImageVectorRight(): Point3D {
        const orientationElem = this.getNumberArray(DicomTags.ImageOrientationPatient) || this.getNumberArray(DicomTags.ImageOrientation);
        if (!orientationElem || orientationElem.length < 3) {
            return null;
        }

        return new Point3D(orientationElem[0], orientationElem[1], orientationElem[2]);
    }

    getImageVectorDown(): Point3D {
        const orientationElem = this.getNumberArray(DicomTags.ImageOrientationPatient) || this.getNumberArray(DicomTags.ImageOrientation);
        if (!orientationElem || orientationElem.length < 6) {
            return null;
        }

        return new Point3D(orientationElem[3], orientationElem[4], orientationElem[5]);
    }

    private _normalVector: Point3D = null;
    getNormalVector(): Point3D {
        if (!this._normalVector) {
            const vectorR = this.getImageVectorRight();
            const vectorD = this.getImageVectorDown();
            this._normalVector = vectorR.cross(vectorD);
        }

        return this._normalVector;
    }

    // Convert a local pixel coordinate into an absolute 3d point
    imageIntoAbsolute(point: Point2D): Point3D {
        const pixelSpacing = this.getPixelSpacing();
        const imageLoc = this.getImagePosition();
        const vectorR = this.getImageVectorRight();
        const vectorD = this.getImageVectorDown();

        if (!pixelSpacing || !imageLoc || !vectorR || !vectorD) {
            return null;
        }

        return imageLoc.addPoint(vectorR.multiplyBy(point.xPos * pixelSpacing.xPos)).addPoint(vectorD.multiplyBy(point.yPos * pixelSpacing.yPos));
    }

    // Convert an absolute 3d point into local pixel coordinate space.  The z coordinate is orthogonal distance from the slice in
    // absolute 3d coordinate units.
    absoluteIntoImage(p4: Point3D): Point3D {
        if (!this.hasPositionData()) {
            return null;
        }

        const rawPoint = p4.getPositionRelativeToPlane(this.getImagePosition(), this.getImageVectorRight(), this.getImageVectorDown());

        const pixelSpacing = this.getPixelSpacing();

        return new Point3D(rawPoint.xPos / pixelSpacing.xPos, rawPoint.yPos / pixelSpacing.yPos, rawPoint.zPos);
    }

    absoluteIntoThisImageWithIndex(point: Point3D, imageIndex: number): Point3D {
        if (!this.hasPositionData()) {
            return null;
        }

        const closestPoint = this.absoluteIntoImage(point);
        return new Point3D(closestPoint.xPos, closestPoint.yPos, imageIndex);
    }

    getAbsoluteCenter(): Point3D {
        return this.imageIntoAbsolute(new Point2D(this.getWidth() / 2, this.getHeight() / 2));
    }

    dumpHeaders(): DicomParser.DPDicomElement[] {
        return _.values(this._dataSet.elements);
    }
}

export = DicomImage;
