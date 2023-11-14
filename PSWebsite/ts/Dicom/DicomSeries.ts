import DicomImage from './DicomImage';
import * as DicomTags from '../Utils/DicomTags';
import * as DicomUtils from '../Utils/DicomUtils';
import Point2D from '../Utils/Point2D';
import Point3D from '../Utils/Point3D';

interface CongruentImage {
    dicomImage: DicomImage;
    imageIndex: number;
    zOffsetFromFirst: number;
}

interface CongruentSet {
    congruentImages: CongruentImage[];
    zOffsetMin: number;
    zOffsetMinSetIndex: number;
    zOffsetMax: number;
    zOffsetMaxSetIndex: number;
}

export default class DicomSeries {
    dicomImages: DicomImage[] = [];
    congruentSets: CongruentSet[] = [];

    addImage(image: DicomImage) {
        this.dicomImages.push(image);

        if (image.hasPositionData()) {
            const imageCorner = image.getImagePosition();
            const imageNormal = image.getNormalVector();
            for (let i = 0; i < this.congruentSets.length; i++) {
                // Make sure that the normals are close enough
                if (imageNormal.dot(this.congruentSets[i].congruentImages[0].dicomImage.getNormalVector()) < 0.999) {
                    continue;
                }

                // Normals are close enough, see how offset the x/y are
                const offsetPoint = this.congruentSets[i].congruentImages[0].dicomImage.absoluteIntoImage(imageCorner);
                if (Math.abs(offsetPoint.xPos) > 0.01 || Math.abs(offsetPoint.yPos) > 0.01) {
                    continue;
                }

                // Looks congruent with this set!
                this.congruentSets[i].congruentImages.push({
                    dicomImage: image,
                    imageIndex: this.dicomImages.length - 1,
                    zOffsetFromFirst: offsetPoint.zPos
                });

                if (offsetPoint.zPos > this.congruentSets[i].zOffsetMax) {
                    this.congruentSets[i].zOffsetMax = offsetPoint.zPos;
                    this.congruentSets[i].zOffsetMaxSetIndex = this.congruentSets[i].congruentImages.length - 1;
                } else if (offsetPoint.zPos < this.congruentSets[i].zOffsetMin) {
                    this.congruentSets[i].zOffsetMin = offsetPoint.zPos;
                    this.congruentSets[i].zOffsetMinSetIndex = this.congruentSets[i].congruentImages.length - 1;
                }

                return;
            }

            this.congruentSets.push({
                congruentImages: [{
                    dicomImage: image,
                    imageIndex: this.dicomImages.length - 1,
                    zOffsetFromFirst: 0
                }],
                zOffsetMin: 0,
                zOffsetMinSetIndex: 0,
                zOffsetMax: 0,
                zOffsetMaxSetIndex: 0
            });
        }
    }

    getClosestImageIndexToPointForPanel(point: Point3D): { index: number, setIndex: number, indexInSet: number, relativePoint: Point3D } {
        // Figure out what image to scroll to
        let closestSet = -1, closestIndexInSet = -1, closestDistance: number, relativePoint: Point3D;
        for (let i = 0; i < this.congruentSets.length; i++) {
            const set = this.congruentSets[i];

            let imageCoords = set.congruentImages[0].dicomImage.absoluteIntoImage(point);
            let closestSetIndex = -1;
            if (imageCoords.zPos > set.zOffsetMax) {
                imageCoords.zPos -= set.zOffsetMax;
                closestSetIndex = set.zOffsetMaxSetIndex;
            } else if (imageCoords.zPos < set.zOffsetMin) {
                imageCoords.zPos -= set.zOffsetMin;
                closestSetIndex = set.zOffsetMinSetIndex;
            } else {
                // We actually landed within the set -- find the closest image within the set and break out
                closestIndexInSet = -1;
                for (let h = 0; h < set.congruentImages.length; h++) {
                    const image = set.congruentImages[h];
                    const thisDist = Math.abs(imageCoords.zPos - image.zOffsetFromFirst);

                    if (closestIndexInSet === -1 || (thisDist < closestDistance)) {
                        closestIndexInSet = h;
                        closestDistance = thisDist;
                    }
                }
                relativePoint = new Point3D(imageCoords.xPos, imageCoords.yPos, imageCoords.zPos - set.congruentImages[closestIndexInSet].zOffsetFromFirst);
                return {
                    index: set.congruentImages[closestIndexInSet].imageIndex,
                    setIndex: i,
                    indexInSet: closestIndexInSet,
                    relativePoint: relativePoint
                };
            }

            const thisDist = Math.abs(imageCoords.zPos);
            if (closestIndexInSet === -1 || (thisDist < closestDistance)) {
                closestSet = i;
                closestIndexInSet = closestSetIndex;
                closestDistance = thisDist;
                relativePoint = imageCoords;
            }
        }
        return {
            index: this.congruentSets[closestSet].congruentImages[closestIndexInSet].imageIndex,
            indexInSet: closestIndexInSet,
            setIndex: closestSet,
            relativePoint: relativePoint
        };
    }

    // Convert the points back into image space (z = slice count)
    absoluteIntoImage(point: Point3D): Point3D {
        const closest = this.getClosestImageIndexToPointForPanel(point);
        const set = this.congruentSets[closest.setIndex];
        if (closest.relativePoint.zPos === 0 || set.congruentImages.length === 1) {
            // Not multiple images in the congruent set, so just return the exact image index
            return new Point3D(closest.relativePoint.xPos, closest.relativePoint.yPos, closest.index);
        }

        if (closest.indexInSet === 0) {
            // First image in set -- check bounds
            if (closest.relativePoint.zPos * set.congruentImages[1].zOffsetFromFirst < 0) {
                // Relative is backwards from the first image z Offset, so the point is outside the bounds of the set,
                // so just give the first index.
                return new Point3D(closest.relativePoint.xPos, closest.relativePoint.yPos, closest.index);
            }

            return new Point3D(closest.relativePoint.xPos, closest.relativePoint.yPos,
                closest.index + closest.relativePoint.zPos / set.congruentImages[1].zOffsetFromFirst);
        }

        if (closest.indexInSet === set.congruentImages.length - 1) {
            const lastZDist = set.congruentImages[set.congruentImages.length - 1].zOffsetFromFirst - set.congruentImages[set.congruentImages.length - 2].zOffsetFromFirst;
            // Last image in set -- check bounds
            if (closest.relativePoint.zPos * lastZDist > 0) {
                // Relative is forwards from the last image z Offset, so the point is outside the bounds of the set,
                // so just give the last index.
                return new Point3D(closest.relativePoint.xPos, closest.relativePoint.yPos, closest.index);
            }

            return new Point3D(closest.relativePoint.xPos, closest.relativePoint.yPos,
                closest.index + closest.relativePoint.zPos / lastZDist);
        }

        // Image is in the middle of the set
        // TODO: Trust the z distance?
        const forwardZDist = set.congruentImages[closest.indexInSet + 1].zOffsetFromFirst - set.congruentImages[closest.indexInSet].zOffsetFromFirst;
        return new Point3D(closest.relativePoint.xPos, closest.relativePoint.yPos,
            closest.index + closest.relativePoint.zPos / forwardZDist);
    }

    imageIntoAbsolute(point: Point3D): Point3D {
        if (this.dicomImages[point.zPos]) {
            return this.dicomImages[point.zPos].imageIntoAbsolute(new Point2D(point.xPos, point.yPos));
        }

        // TODO: Fix this to use congruent sets

        // Treat it as a stack and calc based on the first image
        if (this.dicomImages.length >= 2) {
            const seriesNormal = this.dicomImages[1].getImagePosition().subtractPoint(this.dicomImages[0].getImagePosition());
            const convPoint = this.dicomImages[0].imageIntoAbsolute(new Point2D(point.xPos, point.yPos));
            return convPoint.addPoint(seriesNormal.multiplyBy(point.zPos));
        }

        console.error('No valid _imageIntoAbsolute results...');
        return new Point3D(0, 0, 0);
    }
}
