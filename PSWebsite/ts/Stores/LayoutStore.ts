import { StoreBase, AutoSubscribeStore, autoSubscribe } from 'resub';

import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from './ResponsiveDesignStore';

@AutoSubscribeStore
class LayoutStoreImpl extends StoreBase {
    private _numRows = 1;
    private _numCols = 1;

    constructor() {
        super();

        // Subscribe to max images changes.
        ResponsiveDesignStore.subscribe(this._maxRowsColsChanged, ResponsiveDesignStoreTriggerKeys.MaxRowsCols);

        // Use 1x1 by default when there isn't much width available
        if (ResponsiveDesignStore.getCurrentBreakpoint() === ResponsiveBreakpoint.Sub1000) {
            this._numRows = 1;
            this._numCols = 1;
        } else {
            this._numRows = 1;
            this._numCols = 2;
        }
    }

    setRowsCols(rows: number, cols: number) {
        const maxRowsCols = ResponsiveDesignStore.getMaxRowsCols();
        
        rows = Math.min(rows, maxRowsCols.rows);
        cols = Math.min(cols, maxRowsCols.cols);

        if (rows !== this._numRows || cols !== this._numCols) {
            this._numRows = rows;
            this._numCols = cols;
            this.trigger();
        }
    }

    @autoSubscribe
    getLayout() {
        return {
            rows: this._numRows,
            cols: this._numCols
        };
    }

    private _maxRowsColsChanged = () => {
        const maxRowsCols = ResponsiveDesignStore.getMaxRowsCols();

        if (maxRowsCols.rows < this._numRows || maxRowsCols.cols < this._numCols) {
            this.setRowsCols(
                Math.min(this._numRows, maxRowsCols.rows),
                Math.min(this._numCols, maxRowsCols.cols)
            );
        }
    };
}

export default new LayoutStoreImpl();
