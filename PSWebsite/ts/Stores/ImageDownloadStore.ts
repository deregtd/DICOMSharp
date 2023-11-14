import * as _ from 'lodash';
import { StoreBase, AutoSubscribeStore, autoSubscribe } from 'resub';
import * as SyncTasks from 'synctasks';

import DicomImage from '../Dicom/DicomImage';
import DicomSeries from '../Dicom/DicomSeries';
import DicomSeriesStore from './DicomSeriesStore';
import * as DicomTags from '../Utils/DicomTags';
import * as DicomUtils from '../Utils/DicomUtils';
import PSApiClient from '../Utils/PSApiClient';

function concatenateBuffers(buffer: Uint8Array, newBuffer: Uint8Array) {
    let joinedBuffer = new Uint8Array(buffer.length + newBuffer.length);
    joinedBuffer.set(buffer, 0);
    joinedBuffer.set(newBuffer, buffer.length);
    return joinedBuffer;
}

@AutoSubscribeStore
class ImageDownloadStoreImpl extends StoreBase {
    private _downloads: StudyDownloadInfo[] = [];

    @autoSubscribe
    getDownloads(): StudyDownloadInfo[] {
        return _.cloneDeep(this._downloads);
    }

    retrieveSeries(seriesInstanceUID: string, totalImages: number) {
        if (_.find(this._downloads, download => download.seriesInstanceUID === seriesInstanceUID)) {
            // Already downloading
            return;
        }

        if (DicomSeriesStore.hasSeries(seriesInstanceUID)) {
            // Already downloaded.
            return;
        }

        let download: StudyDownloadInfo = {
            seriesInstanceUID: seriesInstanceUID,
            downloadedImages: 0,
            partialImage: 0,
            totalImages: totalImages
        };
        this._downloads.push(download);
        this.trigger();

        this._downloadSeries(seriesInstanceUID, (dicomImage) => {
            DicomSeriesStore.addImage(dicomImage);
            download.downloadedImages++;
            download.partialImage = 0;
            this.trigger();
        }, (progress) => {
            download.partialImage = progress;
            this.trigger();
        }).then(() => {
            // Remove it from the downloads list
            this._downloads = _.filter(this._downloads, d => d != download);
            this.trigger();
        });
    }

    // Downloading method that streams images and processes them as they download.  It tries to use a fast window.fetch-based
    // method, but if that fails it falls back to a very slow xmlhttprequest-with-string-parsing method, which is too slow for
    // any usage of downloading large file sets.
    private _downloadSeries(seriesInstanceUID: string, imageCallback: (image: DicomImage) => void,
        partialProgressReporter: (partialImages: number) => void): SyncTasks.Promise<void> {

        if (window.fetch) {
            return this._streamImages(PSApiClient.streamSeriesImagesAsync.bind(PSApiClient, seriesInstanceUID),
                imageCallback, partialProgressReporter);
        }

        // Fall back to fetching images a few at a time
        return this._batchFetchImages(PSApiClient.getSeriesImageListAsync.bind(PSApiClient, seriesInstanceUID),
            imageCallback, partialProgressReporter);
    }

    private _streamImages(streamingFunction: (streamCallback: (chunk: Uint8Array) => void) => SyncTasks.Promise<void>,
        imageCallback: (image: DicomImage) => void,
        partialProgressReporter: (partialImages: number) => void): SyncTasks.Promise<void> {

        let saveBetweenChunksBuffer: Uint8Array = null;
        let pendingImage: Uint8Array = null;
        let storedImageBytes = 0;

        return streamingFunction((chunk: Uint8Array) => {
            // Make "chunk" into all the bytes that we have sitting around, if we have any still
            if (saveBetweenChunksBuffer) {
                chunk = concatenateBuffers(saveBetweenChunksBuffer, chunk);
                saveBetweenChunksBuffer = null;
            }

            while (true) {
                if (chunk.length === 0) {
                    return;
                }

                if (!pendingImage) {
                    // Need at least 4 bytes before we know a length to make a pending image out of
                    if (chunk.length < 4) {
                        // If we have 0-3 bytes, save it for next time
                        saveBetweenChunksBuffer = chunk;
                        return;
                    }

                    // Pull the total count out
                    const pendingBytes = DicomUtils.getDwordFromBuffer(chunk, 0);

                    if (chunk.length >= pendingBytes) {
                        // Small image that fits entirely within the chunk
                        var dicomImage = new DicomImage();
                        dicomImage.parseFromBuffer(chunk);
                        imageCallback(dicomImage);

                        if (chunk.length === pendingBytes) {
                            // No more bytes
                            return;
                        }
                        // There's more data still -- slice out the used part of the chunk and loop around to try again
                        chunk = chunk.subarray(pendingBytes);
                        continue;
                    }

                    pendingImage = new Uint8Array(pendingBytes);
                    storedImageBytes = 0;
                }

                if (storedImageBytes + chunk.length < pendingImage.length) {
                    // Fits inside the image without completing it
                    pendingImage.set(chunk, storedImageBytes);
                    storedImageBytes += chunk.length;
                    // All done, no more data to process
                    partialProgressReporter(storedImageBytes / pendingImage.length);
                    return;
                } else {
                    // Can finish off an image!
                    const bytesRemaining = pendingImage.length - storedImageBytes;
                    pendingImage.set(chunk.subarray(0, bytesRemaining), storedImageBytes);

                    var dicomImage = new DicomImage();
                    dicomImage.parseFromBuffer(pendingImage);
                    imageCallback(dicomImage);

                    pendingImage = null;
                    storedImageBytes = 0;

                    if (chunk.length === bytesRemaining) {
                        // No more bytes
                        return;
                    }
                    // There's more data still -- slice out the used part of the chunk and loop around to try again
                    chunk = chunk.subarray(bytesRemaining);
                }
            }
        });
    }

    // Downloads all the image instance UIDs in a set and then starts grabbing the images in 3MB chunks, processing them at the end
    // of each chunk.
    private _batchFetchImages(imageListFetcher: () => SyncTasks.Promise<ImageInfoResp[]>, imageCallback: (image: DicomImage) => void,
            partialProgressReporter: (partialImages: number) => void): SyncTasks.Promise<void> {
        return imageListFetcher().then(imageInfos => {
            // Break the images into 3MB chunks (or a single image, at minimum)
            let imageSets: ImageInfoResp[][] = [];
            let currentSet: ImageInfoResp[] = [];
            let currentKB = 0;
            let firstFetch = true;

            imageInfos.forEach(imageInfo => {
                currentKB += imageInfo.fileSizeKB;
                currentSet.push(imageInfo);
                // Make sure the first image is fetched as a standalone image
                if (firstFetch || currentKB > 3000) {
                    firstFetch = false;
                    imageSets.push(currentSet);
                    currentKB = 0;
                    currentSet = [];
                }
            });
            // Tack on any partial stragglers
            if (currentSet.length > 0) {
                imageSets.push(currentSet);
            }

            return this._batchFetchImagesNextBatch(imageSets, imageCallback, partialProgressReporter);
        });
    }

    private _batchFetchImagesNextBatch(imageSets: ImageInfoResp[][], imageCallback: (image: DicomImage) => void,
            partialProgressReporter: (partialImages: number) => void): SyncTasks.Promise<void> {
        const batch = imageSets.shift();
        if (batch) {
            let totalBytes = 0;
            batch.forEach(im => {
                totalBytes += im.fileSizeKB * 1024;
            });
            return PSApiClient.getImagesAsync(batch.map(info => info.imageInstanceUID), (totalBytesDownloaded: number) => {
                partialProgressReporter(batch.length * totalBytesDownloaded / totalBytes);
            }).then(buffer => {
                const arr = new Uint8Array(buffer);
                let ptr = 0;
                while (ptr < buffer.byteLength) {
                    var dicomImage = new DicomImage();
                    ptr += dicomImage.parseFromBuffer(arr, ptr);
                    imageCallback(dicomImage);
                }

                return this._batchFetchImagesNextBatch(imageSets, imageCallback, partialProgressReporter);
            });
        }
    }
}

export default new ImageDownloadStoreImpl();
