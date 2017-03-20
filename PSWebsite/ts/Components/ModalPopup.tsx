import React = require('react');
import { ComponentBase } from 'resub';

import ModalPopupStore = require('../Stores/ModalPopupStore');
import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from '../Stores/ResponsiveDesignStore';

// Force webpack to build LESS files.
require('../../less/ModalPopup.less');

interface ModalPopupProps extends React.Props<ModalPopup> {
    canClickOut: boolean;
    fullScreenOnResponsive: boolean;
}

interface ModalPopupState {
    fullScreen?: boolean;
}

class ModalPopup extends ComponentBase<ModalPopupProps, ModalPopupState> {
    protected _buildState(props: ModalPopupProps, initialBuild: boolean): ModalPopupState {
        return {
            fullScreen: this.props.fullScreenOnResponsive &&
                ResponsiveDesignStore.getCurrentBreakpoint() === ResponsiveBreakpoint.Sub1000
        };
    }

    render(): JSX.Element {
        const closer = this.props.canClickOut ? <div
            className="ModalPopup-clickOut"
            onMouseDown={ this._onMaskClick.bind(this) }
            onTouchStart={ this._onMaskClick.bind(this) }>
                X
            </div> : null;

        if (this.state.fullScreen) {
            return <div className='ModalPopup-fullscreen'>
                { closer }
                <div className='ModalPopup-fullscreenContent'
                    onMouseDown={ this._onPopupClick.bind(this) }
                    onTouchStart={ this._onPopupClick.bind(this) }>
                    { this.props.children }
                </div>
            </div>;
        } else {
            return <div
                className='ModalPopup-mask'
                onMouseDown={ this._onMaskClick.bind(this) }
                onTouchStart={ this._onMaskClick.bind(this) }>
                { closer }
                <div
                    className='ModalPopup-content'
                    onMouseDown={ this._onPopupClick.bind(this) }
                    onTouchStart={ this._onPopupClick.bind(this) }>
                    { this.props.children }
                </div>
            </div>;
        }
    }

    private _onPopupClick(e: React.MouseEvent<HTMLDivElement> | React.TouchEvent<HTMLDivElement>) {
        e.stopPropagation();
    }

    private _onMaskClick(e: React.MouseEvent<HTMLDivElement> | React.TouchEvent<HTMLDivElement>) {
        if (this.props.canClickOut) {
            ModalPopupStore.popModal();
        }
        e.stopPropagation();
        e.preventDefault();
    }
}

export = ModalPopup;
