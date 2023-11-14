import * as _ from 'lodash';
import { StoreBase, AutoSubscribeStore, autoSubscribe } from 'resub';

import PSApiClient from '../Utils/PSApiClient';

@AutoSubscribeStore
class PatientContextStoreImpl extends StoreBase {
    private _patInfo: DicomPatientInfo = null;
    private _rootStudyUID: string = null;
    private _availableStudies: PSStudySnapshotExtended[] = null;

    @autoSubscribe
    getPatientInfo() {
        return this._patInfo;
    }

    @autoSubscribe
    getAvailableStudies() {
        return this._availableStudies;
    }

    @autoSubscribe
    getStudyInfo(studyInstanceUID: string): PSStudySnapshot {
        // TODO: Clone only the relevant bits (not the series) -- for now includes uncloned series list hidden by interface magic
        return _.clone(_.find(this._availableStudies, study => study.stuInstID === studyInstanceUID));
    }

    @autoSubscribe
    getSeriesInfo(studyInstanceUID: string, seriesInstanceUID: string) {
        const stu = _.find(this._availableStudies, study => study.stuInstID === studyInstanceUID);
        if (!stu) {
            return null;
        }
        return _.clone(_.find(stu.series, series => series.serInstID === seriesInstanceUID));
    }

    @autoSubscribe
    getRootStudyUID() {
        return this._rootStudyUID;
    }

    setPatient(patInfo: DicomPatientInfo, initialStudyUID: string) {
        if (this._rootStudyUID === initialStudyUID) {
            // NOOP
            return;
        }

        this._patInfo = patInfo;
        this._rootStudyUID = initialStudyUID;
        this._availableStudies = [];
        this.trigger();

        PSApiClient.getPatientSnapshot(this._patInfo.patId, 100).then(data => {
            // TODO: Cache result

            this._availableStudies = data;
            this._availableStudies.sort((a, b) => {
                // Sort first by time if they're different
                if (a.stuDateTime > b.stuDateTime) {
                    return -1;
                }
                if (a.stuDateTime < b.stuDateTime) {
                    return 1;
                }
                // If they're not different, sort on the root study coming first
                if (a.stuInstID === this._rootStudyUID) {
                    return -1;
                }
                if (b.stuInstID === this._rootStudyUID) {
                    return 1;
                }
                // Nope, equal.  That sucks.
                return 0;
            });
            this.trigger();
        });
    }
}

export default new PatientContextStoreImpl();
