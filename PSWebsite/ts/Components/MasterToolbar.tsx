import _ = require('lodash');
import React = require('react');
import { ComponentBase } from 'resub';

import DicomSeriesStore = require('../Stores/DicomSeriesStore');
import DisplaySettingsStore = require('../Stores/DisplaySettingsStore');
import LayoutPicker = require('./LayoutPicker');
import ModalPopupStore = require('../Stores/ModalPopupStore');
import PatientContextStore = require('../Stores/PatientContextStore');
import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from '../Stores/ResponsiveDesignStore';
import SearchPane = require('./SearchPane');
import SelectedToolStore = require('../Stores/SelectedToolStore');
import StringUtils = require('../Utils/StringUtils');
import ToolbarButton = require('./ToolbarButton');
import UserSettingsPanel = require('./UserSettingsPanel');

// Force webpack to build LESS files.
require('../../less/MasterToolbar.less');
require('../../less/ToolbarButton.less');

interface MasterToolbarState {
    selectedWindowAvailable?: boolean;

    showLines?: boolean;

    responsiveBreakpoint?: ResponsiveBreakpoint;

    leftButtonTool?: Tool;
    middleButtonTool?: Tool;
    rightButtonTool?: Tool;
    wheelTool?: Tool;

    mouseDetected?: boolean;

    windowHeight?: number;

    patientInfo?: DicomPatientInfo;

    maxRowsCols: { rows: number, cols: number };
}

class MasterToolbar extends ComponentBase<{}, MasterToolbarState> {
    protected /* virtual */ _buildState(props: {}, initialBuild: boolean): MasterToolbarState {
        const panel = DisplaySettingsStore.getActivePanel();
        return {
            selectedWindowAvailable: !!panel && !!panel.seriesInstanceUID,
            showLines: DisplaySettingsStore.isShowingLines(),
            responsiveBreakpoint: ResponsiveDesignStore.getCurrentBreakpoint(),
            leftButtonTool: SelectedToolStore.getButtonTool(MouseButton.Left),
            middleButtonTool: SelectedToolStore.getButtonTool(MouseButton.Middle),
            rightButtonTool: SelectedToolStore.getButtonTool(MouseButton.Right),
            wheelTool: SelectedToolStore.getButtonTool(MouseButton.Wheel),
            mouseDetected: ResponsiveDesignStore.getMouseDetected(),
            windowHeight: ResponsiveDesignStore.getWindowSize().height,
            patientInfo: PatientContextStore.getPatientInfo(),
            maxRowsCols: ResponsiveDesignStore.getMaxRowsCols()
        };
    }

    render() {
        // TODO: Responsive design again

        let patientInfo: string = null;
        if (this.state.patientInfo) {
            const ageString = StringUtils.getAgeString(new Date(), this.state.patientInfo.patBirthDate);
            const demoString = (ageString ? ageString + ' ' : '') + this.state.patientInfo.patSex;
            const parts = [this.state.patientInfo.patId, StringUtils.formatName(this.state.patientInfo.patName), demoString];
            patientInfo = _.compact(parts).join(' - ');
        } else {
            patientInfo = 'No Patient Loaded';
        }

        let changeLayoutSection: JSX.Element[] = null;
        if (this.state.maxRowsCols.rows > 1 || this.state.maxRowsCols.cols > 1) {
            changeLayoutSection = [
                <ToolbarButton.ToolbarButton
                    className="changeLayout"
                    src={ require<string>('../../images/icons/Grid.svg') }
                    onMouseDown={ this._changeLayout.bind(this) }
                    onTouchStart={ this._changeLayout.bind(this) }
                    key="changeLayoutButton"
                />,
                <ToolbarButton.ToolbarButtonDivider key="changeLayoutDivider" className= "master" />
            ];
        }

        return <div className="MasterToolbar">
                <div className="MasterToolbar-patientInfo">
                    <div className="MasterToolbar-patientInfoInner"
                        onMouseDown={ this._openSearch.bind(this) }
                        onTouchStart={ this._openSearch.bind(this) }>
                        <ToolbarButton.ToolbarButton
                            className="patientInfo"
                            src={ require<string>('../../images/icons/SelectPatient.svg') } />
                        <div className="MasterToolbar-patientInfoText">
                            { patientInfo }
                        </div>
                    </div>
                </div>

                <div className="MasterToolbar-centerTools">
                    { changeLayoutSection }

                    { this._getToolsSectionContents() }

                    <ToolbarButton.ToolbarButtonDivider className="master" />,

                    <ToolbarButton.ToolbarButton
                        className="lines"
                        selected={ this.state.showLines }
                        src={ require<string>('../../images/icons/Lines.svg') }
                        onMouseDown={ this._toggleShowLines.bind(this) }
                        onTouchStart={ this._toggleShowLines.bind(this) } />
                </div>

                <div className="MasterToolbar-settings">
                    <ToolbarButton.ToolbarButton
                        className="settings"
                        src={ require<string>('../../images/icons/Settings.svg') }
                        onMouseDown={ this._openSettings.bind(this) }
                        onTouchStart={ this._openSettings.bind(this) } />
                </div>
            </div>;
    }

    private _getToolsSectionContents(): JSX.Element[] {
        let tools: JSX.Element[] = [];

        this._getSupportedTools().forEach(tool => {
            let letters: string[] = [];
            if (this.state.mouseDetected) {
                if (this.state.leftButtonTool === tool) {
                    letters.push('L');
                }
                if (this.state.middleButtonTool === tool) {
                    letters.push('M');
                }
                if (this.state.rightButtonTool === tool) {
                    letters.push('R');
                }
                if (this.state.wheelTool === tool) {
                    letters.push('W');
                }
            }

            tools.push(
                <ToolbarButton.ToolbarButton
                    key={ 'ToolbarButton--' + tool }
                    className={ this._getToolClassNameSuffix(tool) }
                    src={ this._getSrcForTool(tool) }
                    innerText={ letters.join(' ') }
                    onMouseDown={ (e) => { this._pickTool(e.button, tool, e); } }
                    onTouchStart={ (e) => { this._pickTool(0, tool, e); } }
                    onContextMenu={ (e) => { e.preventDefault(); return false; } }
                    onWheel={ () => { SelectedToolStore.setButtonTool(MouseButton.Wheel, tool); } } />
            );
        });

        return tools;
    }

    private _openSearch(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        SearchPane.showPopup();
    }

    private _openSettings(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        UserSettingsPanel.showPopup();
    }

    private _changeLayout(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        ModalPopupStore.pushModal(<LayoutPicker />);
    }

    private _clearLayout(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        DisplaySettingsStore.clearLayout();
    }

    private _toggleShowLines(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }
        
        e.preventDefault();

        DisplaySettingsStore.setShowLines(!this.state.showLines);
    }

    private _getSupportedTools(): Tool[] {
        return [
            Tool.Scroll,
            Tool.WindowLevel,
            Tool.Pan,
            Tool.Zoom,
            Tool.Localizer,
            Tool.MPR
        ];
    }

    private _pickTool(buttonRaw: number, tool: Tool, e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        e.preventDefault();

        let button: MouseButton = null;
        if (buttonRaw == 0) button = MouseButton.Left;
        if (buttonRaw == 1) button = MouseButton.Middle;
        if (buttonRaw == 2) button = MouseButton.Right;

        SelectedToolStore.setButtonTool(button, tool);
    }

    private _getSrcForTool(tool: Tool) {
        if (tool === Tool.Scroll) {
            return require<string>('../../images/icons/Series.svg');
        } else if (tool === Tool.WindowLevel) {
            return require<string>('../../images/icons/Contrast.svg');
        } else if (tool === Tool.Pan) {
            return require<string>('../../images/icons/Move.svg');
        } else if (tool === Tool.Zoom) {
            return require<string>('../../images/icons/Zoom.svg');
        } else if (tool === Tool.Localizer) {
            return require<string>('../../images/icons/3DLoc.svg');
        } else if (tool === Tool.MPR) {
            return require<string>('../../images/icons/MPR.svg');
        } else {
            return null;
        }
    }

    private _getToolClassNameSuffix(tool: Tool) {
        if (tool === Tool.Scroll) {
            return 'series';
        } else if (tool === Tool.WindowLevel) {
            return 'windowLevel';
        } else if (tool === Tool.Pan) {
            return 'pan';
        } else if (tool === Tool.Zoom) {
            return 'zoom';
        } else if (tool === Tool.Localizer) {
            return 'localizer';
        } else if (tool === Tool.MPR) {
            return 'mpr';
        } else {
            return '';
        }
    }
}

export = MasterToolbar;
