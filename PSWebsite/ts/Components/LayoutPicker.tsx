import React = require('react');
import { ComponentBase } from 'resub';

import LayoutStore = require('../Stores/LayoutStore');
import ModalPopupStore = require('../Stores/ModalPopupStore');
import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from '../Stores/ResponsiveDesignStore';

// Force webpack to build LESS files.
require('../../less/LayoutPicker.less');

interface LayoutPickerState {
    rows?: number;
    cols?: number;
    maxRows?: number;
    maxCols?: number;
    arrayMode?: boolean;
}

class LayoutPicker extends ComponentBase<{}, LayoutPickerState> {
    protected /* virtual */ _buildState(props: {}, initialBuild: boolean): LayoutPickerState {
        let newState: LayoutPickerState = LayoutStore.getLayout();

        const maxRowsCols = ResponsiveDesignStore.getMaxRowsCols();
        newState.maxRows = maxRowsCols.rows;
        newState.maxCols = maxRowsCols.cols;

        return newState;
    }

    render() {
        let rows: JSX.Element[] = [];
        for (let y = 0; y < 4; y++) {
            let items: JSX.Element[] = [];
            for (let x = 0; x < 5; x++) {
                let classNames = ['LayoutPicker-cell'];
                
                if (x < this.state.cols && y < this.state.rows) {
                    classNames.push('LayoutPicker-cell--selected');
                } else if (x >= this.state.maxCols || y >= this.state.maxRows) {
                    classNames.push('LayoutPicker-cell--disabled');
                }

                items.push(<div key={ 'item_' + y + '_' + x } className={ classNames.join(' ') } onClick={ this._pickLayout.bind(this, x + 1, y + 1) } />);
            }
            rows.push(<div className="LayoutPicker-row" key={ 'row_' + y } >{ items }</div>);
        }
        return (
            <div className="LayoutPicker">
                <div className="LayoutPicker-head">Choose a new layout</div>
                { rows }
            </div>
        );
    }

    private _pickLayout(cols: number, rows: number) {
        if (cols > this.state.maxCols && rows > this.state.maxRows) {
            return;
        }

        LayoutStore.setRowsCols(rows, cols);
        ModalPopupStore.popModal();
    }
}

export = LayoutPicker;
