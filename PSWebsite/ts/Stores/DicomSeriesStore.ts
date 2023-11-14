/**
 * Tracks all downloaded DICOM images by series.  Also happens to track patient and study info, but that will likely
 * move out to separate stores at some point.
 */

import * as _ from 'lodash';
import { StoreBase, AutoSubscribeStore, autoSubscribe, key } from 'resub';

import DicomImage from '../Dicom/DicomImage';
import DicomSeries from '../Dicom/DicomSeries';
import * as DicomTags from '../Utils/DicomTags';

@AutoSubscribeStore
class DicomSeriesStoreImpl extends StoreBase {
    private _seriesInfo: { [seriesInstanceUID: string]: DicomSeriesInfo } = {};
    private _seriesImageCache: { [seriesInstanceUID: string]: DicomSeries } = {};

    private _patientInfo: { [patientId: string]: DicomExtendedPatientInfo } = {};
    private _studyInfo: { [studyInstanceUID: string]: DicomExtendedStudyInfo } = {};

    @autoSubscribe
    getSeriesInfo(@key seriesInstanceUID: string): DicomSeriesInfo {
        return this._seriesInfo[seriesInstanceUID];
    }

    @autoSubscribe
    getSeriesImages(@key seriesInstanceUID: string): DicomSeries {
        return this._seriesImageCache[seriesInstanceUID];
    }

    @autoSubscribe
    getAllPatients() {
        return _.values<DicomExtendedPatientInfo>(this._patientInfo);
    }

    @autoSubscribe
    hasSeries(@key seriesInstanceUID: string): boolean {
        return !!this._seriesInfo[seriesInstanceUID];
    }

    @autoSubscribe
    hasStudy(studyInstanceUID: string): boolean {
        return !!this._studyInfo[studyInstanceUID];
    }

    addImage(image: DicomImage) {
        const studyInstanceUID = image.getDisplayOrDefault(DicomTags.StudyInstanceUID);
        const seriesInstanceUID = image.getDisplayOrDefault(DicomTags.SeriesInstanceUID);
        if (!studyInstanceUID || !seriesInstanceUID) {
            console.error('Invalid Study (' + studyInstanceUID + ') or Series (' + seriesInstanceUID + ') instance UID');
            return;
        }

        // Cache patient info if needed
        const patientInfo = image.getPatientInfo();
        let patInfoEx = this._patientInfo[patientInfo.patId];
        if (!patInfoEx) {
            patInfoEx = <DicomExtendedPatientInfo>patientInfo;
            patInfoEx.studies = [];
            this._patientInfo[patientInfo.patId] = patInfoEx;
        }

        // Cache study info if needed
        const studyInfo = image.getStudyInfo();
        let studyInfoEx = this._studyInfo[studyInstanceUID];
        if (!studyInfoEx) {
            studyInfoEx = <DicomExtendedStudyInfo>studyInfo;
            studyInfoEx.series = [];
            this._studyInfo[studyInstanceUID] = studyInfoEx;
        }

        // Add the study info to the patient if needed
        if (!_.find(patInfoEx.studies, study => study.studyInstanceUID === studyInstanceUID)) {
            patInfoEx.studies.push(studyInfoEx);
        }

        // Add the series info to the study if needed
        const seriesInfo = image.getSeriesInfo();
        if (!_.find(studyInfoEx.series, series => series.seriesInstanceUID === seriesInstanceUID)) {
            studyInfoEx.series.push(seriesInfo);
        }

        // Cache the series info if needed
        if (!this._seriesInfo[seriesInstanceUID]) {
            this._seriesInfo[seriesInstanceUID] = seriesInfo;
        }

        // Cache the image itself
        if (!this._seriesImageCache[seriesInstanceUID]) {
            this._seriesImageCache[seriesInstanceUID] = new DicomSeries();
        }

        this._seriesImageCache[seriesInstanceUID].addImage(image);

        this.trigger(seriesInstanceUID);
    }
}

export default new DicomSeriesStoreImpl();
