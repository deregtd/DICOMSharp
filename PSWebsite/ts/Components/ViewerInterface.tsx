import * as React from 'react';
import { ComponentBase } from 'resub';

import DownloadProgress from './DownloadProgress';
import LayoutStore from '../Stores/LayoutStore';
import MasterToolbar from './MasterToolbar';
import ModalPopup from './ModalPopup';
import ModalPopupStore from '../Stores/ModalPopupStore';
import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from '../Stores/ResponsiveDesignStore';
import ViewerPanel from './ViewerPanel';

// Force webpack to build LESS files.
require('../../less/ViewerInterface.less');

interface ViewerInterfaceState {
    numPanels?: number;
    modalPopupData?: ModalInfo;
    reservedToolbarHeight?: number;
}

export default class ViewerInterface extends ComponentBase<{}, ViewerInterfaceState> {
    protected /* virtual */ _buildState(props: {}, initialBuild: boolean): ViewerInterfaceState {
        const layout = LayoutStore.getLayout();

        let newState: ViewerInterfaceState = {
            numPanels: layout.cols * layout.rows,
            modalPopupData: ModalPopupStore.getTopModalContents()
        }

        return newState;
    }

    render() {
        let optionalModalPopup: JSX.Element = null;
        if (this.state.modalPopupData) {
            optionalModalPopup = <ModalPopup fullScreenOnResponsive={ this.state.modalPopupData.fullScreenOnResponsive } canClickOut={ this.state.modalPopupData.canClickOut } >
                { this.state.modalPopupData.element }
            </ModalPopup>;
        }

        let viewers: JSX.Element[] = [];
        for (let i = 0; i < this.state.numPanels; i++) {
            viewers.push(<ViewerPanel key={ "panel_" + i } panelIndex={ i } />);
        }

        return (
            <div className="ViewerInterface">
                <MasterToolbar />

                <div className="ViewerInterface-arrayContainer">
                    { viewers }
                </div>

                <DownloadProgress />
                { optionalModalPopup }
            </div>
        );
    }
}
