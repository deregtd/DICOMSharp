import * as _ from 'lodash';
import { StoreBase, AutoSubscribeStore, autoSubscribe, key } from 'resub';

import LayoutStore from './LayoutStore';
import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from './ResponsiveDesignStore';

interface PanelLayout {
    x: number;
    y: number;
    width: number;
    height: number;
}

@AutoSubscribeStore
class ViewerPanelLayoutStore extends StoreBase {
    private _layouts: PanelLayout[] = [];

    constructor() {
        super();

        LayoutStore.subscribe(this._updateLayout);
        ResponsiveDesignStore.subscribe(this._updateLayout, ResponsiveDesignStoreTriggerKeys.ViewerSize);

        this._updateLayout();
    }

    @autoSubscribe
    getPanelLayout(@key panelIndex: number): PanelLayout {
        return this._layouts[panelIndex];
    }

    private _updateLayout = () => {
        const layout = LayoutStore.getLayout();
        const viewerArea = ResponsiveDesignStore.getViewerArea();

        for (let i = 0; i < layout.cols * layout.rows; i++) {
            const thisX = Math.round(viewerArea.width * (i % layout.cols) / layout.cols);
            const nextX = Math.round(viewerArea.width * (1 + (i % layout.cols)) / layout.cols);
            const thisY = Math.round(viewerArea.height * Math.floor(i / layout.cols) / layout.rows);
            const nextY = Math.round(viewerArea.height * Math.floor(1 + (i / layout.cols)) / layout.rows);

            const newLayout: PanelLayout = {
                x: thisX,
                y: thisY,
                width: nextX - thisX,
                height: nextY - thisY
            };

            if (i >= this._layouts.length) {
                this._layouts.push(newLayout);
                this.trigger(i);
            } else if (!_.isEqual(newLayout, this._layouts[i])) {
                this._layouts[i] = newLayout;
                this.trigger(i);
            }
        }

        // Cut off any windows we don't need to know about anymore
        if (this._layouts.length > layout.cols * layout.rows) {
            this._layouts.splice(layout.cols * layout.rows);
        }
    };
}

export default new ViewerPanelLayoutStore();
