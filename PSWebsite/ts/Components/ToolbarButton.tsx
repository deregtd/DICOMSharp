import * as React from 'react';
import { ComponentBase } from 'resub';

import InlineSVG from '../Utils/InlineSVG';

// Force webpack to build LESS files.
require('../../less/ToolbarButton.less');

interface ToolbarButtonProps extends React.PropsWithChildren {
    className: string;
    src: string;
    title?: string;

    selected?: boolean;
    innerText?: string;
    onMouseDown?: React.EventHandler<React.MouseEvent<any>>;
    onTouchStart?: React.EventHandler<React.TouchEvent<any>>;
    onContextMenu?: React.EventHandler<React.MouseEvent<any>>;
    onWheel?: React.EventHandler<React.WheelEvent<any>>;
}

export default class ToolbarButton extends ComponentBase<ToolbarButtonProps, {}> {
    render() {
        let inner: JSX.Element;
        if (this.props.innerText) {
            inner = <div className="ToolbarButton-lettersContainer">{ this.props.innerText }</div>;
        }
        
        let classNames = ['ToolbarButton', 'ToolbarButton--' + this.props.className];
        if (this.props.selected) {
            classNames.push('ToolbarButton--selected');
        }
        
        return (
            <div
                className={ classNames.join(' ') }
                onMouseDown={ this.props.onMouseDown }
                onTouchStart={ this.props.onTouchStart }
                onContextMenu={ this.props.onContextMenu }
                onWheel={ this.props.onWheel }
            >
                <InlineSVG src={ this.props.src } />
                { inner }
            </div>
        );
    }
}

interface ToolbarButtonDividerProps extends React.PropsWithChildren {
    className: string;
}

export class ToolbarButtonDivider extends ComponentBase<ToolbarButtonDividerProps, {}> {
    render() {
        return <div className={ 'ToolbarButtonDivider ToolbarButtonDivider--' + this.props.className } />;
    }
}
