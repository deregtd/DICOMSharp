import React = require('react');
import { ComponentBase } from 'resub';

import PatientContextStore = require('../Stores/PatientContextStore');
import DisplaySettingsStore = require('../Stores/DisplaySettingsStore');
import ModalPopupStore = require('../Stores/ModalPopupStore');
import StringUtils = require('../Utils/StringUtils');

// Force webpack to build LESS files.
require('../../less/SeriesPicker.less');

interface SeriesPickerProps extends React.Props<any> {
    panelIndex: number;
}

interface SeriesPickerState {
    studies?: PSStudySnapshotExtended[];
    rootStudyUID?: string;
    studyInfo?: PSStudySnapshot;
}

class SeriesPicker extends ComponentBase<SeriesPickerProps, SeriesPickerState> {
    static showPopup(panelIndex: number) {
        ModalPopupStore.pushModal(<SeriesPicker panelIndex={ panelIndex } />);
    }

    protected /* virtual */ _buildState(props: {}, initialBuild: boolean): SeriesPickerState {
        const rootStudyUID = PatientContextStore.getRootStudyUID();
        return {
            studies: PatientContextStore.getAvailableStudies(),
            rootStudyUID: rootStudyUID,
            studyInfo: PatientContextStore.getStudyInfo(rootStudyUID)
        };
    }

    render() {
        const rootStuDate = new Date(this.state.studyInfo.stuDateTime);
        let studies: JSX.Element[] = this.state.studies.map(study => {
            let stuDesc = new Date(study.stuDateTime).toLocaleDateString() + ': ' + (study.accessionNum || '[No Accession]') + ' - ' +
                (study.stuDesc || '[No Description]');

            const isRootStudy = study.stuInstID === this.state.rootStudyUID;
            const stuDate = new Date(study.stuDateTime);
            if (!isRootStudy) {
                stuDesc = (stuDate <= rootStuDate ? 'Prior: ' : 'Future: ') + stuDesc;
            }

            let series: JSX.Element[] = study.series.map(ser => {
                return <div key={ 'ser_' + ser.serInstID.replace('/\./g', '') } className="SeriesPicker-series"
                    onClick={ () => {
                        DisplaySettingsStore.setSeriesToPanel(this.props.panelIndex, study.stuInstID, ser.serInstID);
                        ModalPopupStore.popModal();
                    } }>
                    { (ser.serNum ? ser.serNum + '. ' : '') + (ser.serDesc || '[No Description]') +
                        ' (' + ser.numImages + ' Image' + (ser.numImages === 1 ? ')' : 's)') }
                    </div>;
            });
            return (
                <div key={ 'stu_' + study.stuInstID.replace('/\./g', '') }
                    className={ isRootStudy ? "SeriesPicker-study SeriesPicker-study--selected" : "SeriesPicker-study"} >
                    <div className='SeriesPicker-studyInfo'>{ stuDesc }</div>
                    <div className="SeriesPicker-seriesList">{ series }</div>
                </div>
            );
        });
        return (
            <div className="SeriesPicker">
                <div className="SeriesPicker-head">Choose a new series to display</div>
                <div className="SeriesPicker-clear" onClick={ () => {
                    DisplaySettingsStore.setSeriesToPanel(this.props.panelIndex, null, null);
                    ModalPopupStore.popModal();
                } }>Clear Panel</div>
                { studies }
            </div>
        );
    }
}

export = SeriesPicker;
