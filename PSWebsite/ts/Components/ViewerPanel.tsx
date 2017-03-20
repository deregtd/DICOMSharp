import React = require('react');
import { ComponentBase } from 'resub';

import DisplaySettingsStore = require('../Stores/DisplaySettingsStore');
import LightBox = require('./LightBox');
import ViewerPanelLayoutStore = require('../Stores/ViewerPanelLayoutStore');
import ViewerPanelToolbar = require('./ViewerPanelToolbar');

// Force webpack to build LESS files.
require('../../less/ViewerPanel.less');

interface ViewerPanelProps extends React.Props<ViewerPanel> {
    panelIndex: number;
}

interface ViewerPanelState {
    selectedPanel?: boolean;

    // Window positioning
    x?: number;
    y?: number;
    width?: number;
    height?: number;
}

class ViewerPanel extends ComponentBase<ViewerPanelProps, ViewerPanelState> {
    protected _buildState(props: ViewerPanelProps, initialBuild: boolean): ViewerPanelState {
        const layout = ViewerPanelLayoutStore.getPanelLayout(this.props.panelIndex);

        return {
            selectedPanel: DisplaySettingsStore.getDisplaySettings(this.props.panelIndex).selectedPanel,
            x: layout.x,
            y: layout.y,
            width: layout.width,
            height: layout.height
        };
    }

    render() {
        const borderColor = this.state.selectedPanel ? '#46b' : '#444'

        let styles: React.CSSProperties = {
            left: this.state.x + 'px',
            top: this.state.y + 'px',
            width: this.state.width + 'px',
            height: this.state.height + 'px',
            borderColor: borderColor
        };

        const toolbarHeight = 44;

        return <div className="ViewerPanel" style={ styles }>
                <div className="ViewerPanel-viewPanelToolbarContainer">
                    <ViewerPanelToolbar panelIndex={ this.props.panelIndex } />
                </div>
                <div className="ViewerPanel-lightBoxContainer">
                    <LightBox
                        panelIndex={ this.props.panelIndex }
                        xPosition={ this.state.x + 1 }
                        yPosition={ this.state.y + 1 + toolbarHeight }
                        panelWidth={ this.state.width - 2 }
                        panelHeight={ this.state.height - 2 - toolbarHeight }
                    />
                </div>
            </div>;
    }
}

export = ViewerPanel;
