import * as DicomParser from 'dicom-parser';
import * as _ from 'lodash';
import * as React from 'react';
import { ComponentBase } from 'resub';

import DicomImage from '../Dicom/DicomImage';
import DisplaySettingsStore from '../Stores/DisplaySettingsStore';
import DicomSeriesStore from '../Stores/DicomSeriesStore';
import ModalPopupStore from '../Stores/ModalPopupStore';
import PatientContextStore from '../Stores/PatientContextStore';
import SeriesPicker from './SeriesPicker';
import ToolbarButton, { ToolbarButtonDivider } from './ToolbarButton';

// Force webpack to build LESS files.
require('../../less/ViewerPanelToolbar.less');
require('../../less/ToolbarButton.less');

interface ViewerPanelToolbarProps extends React.PropsWithChildren {
    panelIndex: number;
}

interface ViewerPanelToolbarState {
    hasLoadedContext?: boolean;
    hasImages?: boolean;
    studyInfo?: PSStudySnapshot;
    seriesInfo?: PSSeriesSnapshot;
    isPriorStudy?: boolean;
    linkedScrolling?: boolean;
    expanded?: boolean;
    rootStudy?: PSStudySnapshot;
}

const FileNameSanitizerRegex = /[^\w., \-]/g;

export default class ViewerPanelToolbar extends ComponentBase<ViewerPanelToolbarProps, ViewerPanelToolbarState> {
    protected _buildState(props: ViewerPanelToolbarProps): ViewerPanelToolbarState {
        const displaySettings = DisplaySettingsStore.getDisplaySettings(props.panelIndex);

        const rootStudyUID = PatientContextStore.getRootStudyUID();

        let state: ViewerPanelToolbarState = {
            hasLoadedContext: !!rootStudyUID,
            hasImages: !!DisplaySettingsStore.getDisplaySettings(props.panelIndex).seriesInstanceUID,
            studyInfo: displaySettings.studyInstanceUID ?
                PatientContextStore.getStudyInfo(displaySettings.studyInstanceUID) : null,
            seriesInfo: displaySettings.seriesInstanceUID ?
                PatientContextStore.getSeriesInfo(displaySettings.studyInstanceUID, displaySettings.seriesInstanceUID) : null,
            isPriorStudy: displaySettings.studyInstanceUID && displaySettings.studyInstanceUID !== rootStudyUID,
            linkedScrolling: displaySettings.linkScrolling,
            rootStudy: PatientContextStore.getStudyInfo(rootStudyUID)
        };

        return state;
    }

    render() {
        if (!this.state.hasLoadedContext) {
            return <div className="ViewerPanelToolbar" />;
        }

        let serInfo: JSX.Element = null;
        if (this.state.seriesInfo) {
            let serName = this.state.seriesInfo.serNum ? this.state.seriesInfo.serNum + '. ' : '';
            serName += this.state.seriesInfo.serDesc || 'No Description';
            if (this.state.isPriorStudy) {
                const stuDate = new Date(this.state.studyInfo.stuDateTime);
                const rootStuDate = new Date(this.state.rootStudy.stuDateTime);
                serInfo = <div className="ViewerPanelToolbar-info">
                        <div className="ViewerPanelToolbar-infoPriorStudy">
                            { (stuDate <= rootStuDate ? 'Prior: ' : 'Future: ') + stuDate.toLocaleDateString() + ' - ' + this.state.studyInfo.stuDesc }
                        </div>
                        <div className="ViewerPanelToolbar-infoPriorSeries">
                            { serName }
                        </div>
                    </div>;
            } else {
                serInfo = <div className="ViewerPanelToolbar-info">
                        <div className="ViewerPanelToolbar-infoCurrentSeries">
                            { serName }
                        </div>
                    </div>;
            }
        }

        let rightTools: JSX.Element[] = [];
        let toolRow: JSX.Element = null;

        if (this.state.seriesInfo) {
            rightTools = [
                <ToolbarButton
                    key="ToolbarButton--link"
                    className="link"
                    selected={ this.state.linkedScrolling }
                    src={ require('../../images/icons/Link.svg') }
                    onMouseDown={ this._toggleLink.bind(this) }
                    onTouchStart={ this._toggleLink.bind(this) } />,

                <ToolbarButtonDivider className="viewerPanel" key="ToolbarButtonDivider-1" />,

                <ToolbarButton
                    key="ToolbarButton--more"
                    className="more"
                    selected={ this.state.expanded }
                    src={ require('../../images/icons/More.svg') }
                    onMouseDown={ this._moreTools.bind(this) }
                    onTouchStart={ this._moreTools.bind(this) } />
            ];

            if (this.state.expanded) {
                let extendedTools: JSX.Element[] = [];
                let toolSets = this.state.hasImages ?
                    [this._getViewSectionContents(), this._getCommandSectionContents()] : [];
                toolSets.forEach((toolSet, i) => {
                    toolSet.forEach(tool => {
                        extendedTools.push(tool);
                    });
                    if (i < toolSets.length - 1) {
                        extendedTools.push(<ToolbarButtonDivider className="viewerPanel" key={ 'ToolbarButtonDivider-' + i } />);
                    }
                });
                toolRow = <div className="ViewerPanelToolbar-extendedTools">{ extendedTools }</div>;
            }
        }

        let styles: React.CSSProperties = {
            height: this.state.expanded ? 'auto' : '100%',
            backgroundColor: this.state.isPriorStudy ? '#450093' : '#777'
        };
        return (
            <div className="ViewerPanelToolbar" style={ styles }>
                <div className="ViewerPanelToolbar-mainRow">
                    <div className="ViewerPanelToolbar-rightTools">
                        { rightTools }
                    </div>
                    <div className="ViewerPanelToolbar-mainRowSeriesinfo"
                        onMouseDown={ this._changeSeries.bind(this) }
                        onTouchStart={ this._changeSeries.bind(this) }>
                        <ToolbarButton
                            className="changeSeries"
                            src={ require('../../images/icons/List.svg') } />
                        { serInfo }
                    </div>
                </div>
                { toolRow }
            </div>
        );
    }

    private _moreTools(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        e.preventDefault();

        this.setState({
            expanded: !this.state.expanded
        });
    }

    private _getCommandSectionContents(): JSX.Element[] {
        return [
            <ToolbarButton
                key="ToolbarButton--saveJpg"
                className="saveJpg"
                src={ require('../../images/icons/JPG.svg') }
                onMouseDown={ this._saveJpeg.bind(this) }
                onTouchStart={ this._saveJpeg.bind(this) } />,
            <ToolbarButton
                key="ToolbarButton--dicom"
                className="dicom"
                src={ require('../../images/icons/DICOM.svg') }
                onMouseDown={ this._dumpDicom.bind(this) }
                onTouchStart={ this._dumpDicom.bind(this) } />
        ];
    }

    private _saveJpeg(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        e.preventDefault();

        // TODO: Make this more react-y
        const canvas = document.getElementById('canvas-' + this.props.panelIndex) as HTMLCanvasElement;
        if (canvas) {
            const context = canvas.getContext('2d') as CanvasRenderingContext2D;
            var downloadUrl = canvas.toDataURL('image/png');
            var downloadLink = document.createElement('a');
            downloadLink.href = downloadUrl;
                
            var imageNameParts: string[] = [];
            if (this.state.seriesInfo.serDateTime) {
                var imageDate = new Date(this.state.seriesInfo.serDateTime);
                // Format date yyyyMMdd
                imageNameParts.push(imageDate.getFullYear() + ('0' + (imageDate.getMonth() + 1)).slice(-2) + ('0' + imageDate.getDate()).slice(-2));
            }
            var patientInfo = PatientContextStore.getPatientInfo();
            if (patientInfo) {
                imageNameParts.push(PatientContextStore.getPatientInfo().patName || PatientContextStore.getPatientInfo().patId);
            }
            if (this.state.seriesInfo.serDesc) {
                imageNameParts.push(this.state.seriesInfo.serDesc);
            } else {
                imageNameParts.push(this.state.seriesInfo.serNum.toString());
            }

            var displaySettingInfo = DisplaySettingsStore.getDisplaySettings(this.props.panelIndex);
            if (displaySettingInfo) {
                // 0 pad the series number to 4 digits.  It it happens to be longer, allow a non-uniform seriesNumber
                var imageIndexString = (displaySettingInfo.imageIndexInSeries + 1).toString();
                imageNameParts.push(('0000' + imageIndexString).slice(-Math.max(4, imageIndexString.length)));
            }

            var imagePath = imageNameParts.join('-').replace(FileNameSanitizerRegex, '');

            downloadLink.setAttribute('download', imagePath + '.png')
            if (document.createEvent) {
                var event = document.createEvent('MouseEvents');
                event.initEvent('click', true, true);
                downloadLink.dispatchEvent(event);
            }
            else {
                downloadLink.click();
            }
        } else {
            console.error('Canvas Not Found!');
        }
    }

    private _dumpDicom(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        e.preventDefault();

        const dispSettings = DisplaySettingsStore.getDisplaySettings(this.props.panelIndex);
        if (!dispSettings || !dispSettings.seriesInstanceUID) {
            return;
        }

        const series = DicomSeriesStore.getSeriesImages(dispSettings.seriesInstanceUID);
        if (!series) {
            return;
        }

        const image = series.dicomImages[dispSettings.imageIndexInSeries];
        if (!image) {
            return;
        }

        ModalPopupStore.pushModal(this._dumpDicomSequence(image, image.dumpHeaders()));
    }

    private _dumpDicomSequence(image: DicomImage, dicomElems: DicomParser.Element[]): JSX.Element {
        let elems: JSX.Element[] = [];
        _.each(dicomElems, element => {
            const group = element.tag.substr(1, 4);
            const elem = element.tag.substr(5, 4);

            elems.push(
                <div key={ element.tag } className="dicomdumprow">
                    { '(' + group + ',' + elem + ') [' + element.length + ',' + element.vr + '] ' + image.getDisplayOrDefault(element.tag) }
                </div>
            );

            if (element.vr === 'SQ') {
                _.each(element.items, (sqItem, i) => {
                    elems.push(
                        <div key={ element.tag + '_sqi_' + i.toString() } className="dicomdumpsqitem">
                            { 'SQ Item ' + (i + 1).toString() + ':' }
                            <div className="dicomdumpsqcontainer">
                                { this._dumpDicomSequence(image, _.values(sqItem.dataSet.elements)) }
                            </div>
                        </div>
                    );
                });
            }
        });

        return <div className="dicomdumpcontainer">{ elems }</div>;
    }

    private _toggleLink(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        e.preventDefault();

        DisplaySettingsStore.setLinkedScrolling(this.props.panelIndex, !this.state.linkedScrolling);
    }

    private _getViewSectionContents(): JSX.Element[] {
        return [
            <ToolbarButton
                key="ToolbarButton--reset"
                className="reset"
                src={ require('../../images/icons/Reset.svg') }
                onMouseDown={ this._resetView.bind(this) }
                onTouchStart={ this._resetView.bind(this) } />,
            <ToolbarButton
                key="ToolbarButton--invert"
                className="invert"
                src={ require('../../images/icons/Invert.svg') }
                onMouseDown={ this._invertView.bind(this) }
                onTouchStart={ this._invertView.bind(this) } />,
            <ToolbarButton
                key="ToolbarButton--rotateClockwise"
                className="rotateClockwise"
                src={ require('../../images/icons/RotateClockwise.svg') }
                onMouseDown={ this._rotateView.bind(this, true) }
                onTouchStart={ this._rotateView.bind(this, true) } />,
            <ToolbarButton
                key="ToolbarButton--rotateCounterClockwise"
                className="rotateCounterClockwise"
                src={ require('../../images/icons/RotateCounterClockwise.svg') }
                onMouseDown={ this._rotateView.bind(this, false) }
                onTouchStart={ this._rotateView.bind(this, false) } />,
            <ToolbarButton
                key="ToolbarButton--flipHorizontal"
                className="flipHorizontal"
                src={ require('../../images/icons/FlipHorizontal.svg') }
                onMouseDown={ this._flipView.bind(this, true) }
                onTouchStart={ this._flipView.bind(this, true) } />,
            <ToolbarButton
                key="ToolbarButton--flipVertical"
                className="flipVertical"
                src={ require('../../images/icons/FlipVertical.svg') }
                onMouseDown={ this._flipView.bind(this, false) }
                onTouchStart={ this._flipView.bind(this, false) } />
        ];
    }

    private _changeSeries(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        SeriesPicker.showPopup(this.props.panelIndex);
    }

    private _resetView(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        DisplaySettingsStore.resetDefaults(this.props.panelIndex);
    }

    private _invertView(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        DisplaySettingsStore.invertView(this.props.panelIndex);
    }

    private _rotateView(clockwise: boolean, e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        DisplaySettingsStore.rotateView(this.props.panelIndex, clockwise);
    }

    private _flipView(horizontal: boolean, e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        DisplaySettingsStore.flipView(this.props.panelIndex, horizontal);
    }
}
