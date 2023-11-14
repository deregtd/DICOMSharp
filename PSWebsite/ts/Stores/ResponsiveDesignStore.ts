import * as _ from 'lodash';
import { StoreBase, AutoSubscribeStore, autoSubscribeWithKey } from 'resub';

const MIN_IMAGE_WIDTH = 300;
const MIN_IMAGE_HEIGHT = 300;

export const TriggerKeys = {
    MouseDetected: 'mousedetected',
    Orientation: 'orientation',
    Breakpoint: 'breakpoint',
    WindowSize: 'windowsize',
    ViewerSize: 'viewersize',
    MaxRowsCols: 'maxrowscols'
};

@AutoSubscribeStore
class ResponsiveDesignStoreImpl extends StoreBase {
    private _mouseDetected = false;
    private _touchDetected = false;
    private _lastTouchStart = 0;
    private _mouseMoveTimeout = 0;

    private _lastWidth = 0;
    private _lastHeight = 0;

    private _lastMaxRows = 1;
    private _lastMaxCols = 1;

    private _currentBreakpoint: ResponsiveBreakpoint = null;

    constructor() {
        super();

        document.addEventListener('DOMContentLoaded', this._onDomLoaded);

        window.onorientationchange = () => {
            this.trigger(TriggerKeys.Orientation);
        };

        // Store initial size and trap resize
        this._lastWidth = window.innerWidth;
        this._lastHeight = window.innerHeight
        this._currentBreakpoint = this._calcBreakpoint();

        window.onresize = this._onResize;

        // Fire an initial resize event to handle iOS scaling
        _.defer(this._onResize, 10);
    }
        
    private _onResize = () => {
        const newWidth = window.innerWidth;
        const newHeight = window.innerHeight;

        let keysChanged: string[] = [];

        if (newWidth !== this._lastWidth || newHeight !== this._lastHeight) {
            // It changed -- tell the world!
            this._lastWidth = newWidth;
            this._lastHeight = newHeight;

            const newBP = this._calcBreakpoint();
            if (newBP !== this._currentBreakpoint) {
                this._currentBreakpoint = newBP;
                keysChanged.push(TriggerKeys.Breakpoint);
            }

            keysChanged.push(TriggerKeys.WindowSize);
            keysChanged.push(TriggerKeys.ViewerSize);
        }

        // Check if the max images that fit on the screen has changed.
        const viewerArea = this.getViewerArea();
        const newMaxRows = Math.floor(viewerArea.height / MIN_IMAGE_HEIGHT);
        const newMaxCols = Math.floor(viewerArea.width / MIN_IMAGE_WIDTH);

        if (newMaxRows !== this._lastMaxRows || newMaxCols !== this._lastMaxCols) {
            this._lastMaxRows = newMaxRows;
            this._lastMaxCols = newMaxCols;

            keysChanged.push(TriggerKeys.MaxRowsCols);
        }

        if (keysChanged.length > 0) {
            this.trigger(keysChanged);
        }
    };

    private _onDomLoaded = () => {
        document.removeEventListener('DOMContentLoaded', this._onDomLoaded);
        // Attempt to track whether a mouse exists by tracking whether there's a touchstart event within 1000ms of getting a
        // mousemove event.  Ugly hack because touch events on browsers also trigger mousedown/mousemove/mouseup/click events. :(
        let touchStartHandler = () => {
            this._touchDetected = true;
            this._lastTouchStart = Date.now();
            if (this._mouseMoveTimeout) {
                clearTimeout(this._mouseMoveTimeout);
                this._mouseMoveTimeout = 0;
            }
            if (this._mouseDetected) {
                // Don't need to track anything anymore -- both were detected
                document.body.removeEventListener('touchstart', touchStartHandler);
            }
        };
        let mouseMoveHandler = () => {
            if (!this._mouseMoveTimeout) {
                if (Math.abs(Date.now() - this._lastTouchStart) > 1000) {
                    // No touchstart in the last second
                    this._mouseMoveTimeout = setTimeout(() => {
                        this._mouseMoveTimeout = 0;
                        this._mouseDetected = true;
                        document.body.removeEventListener('mousemove', mouseMoveHandler);
                        this.trigger(TriggerKeys.MouseDetected);
                    }, 1000) as unknown as number;
                }
            }
        };
        document.body.addEventListener('touchstart', touchStartHandler);
        document.body.addEventListener('mousemove', mouseMoveHandler);
    };

    @autoSubscribeWithKey(TriggerKeys.MouseDetected)
    getMouseDetected() {
        return this._mouseDetected;
    }

    @autoSubscribeWithKey(TriggerKeys.Orientation)
    getPortraitMode() {
        return Math.abs(Number(window.orientation)) !== 90;
    }

    @autoSubscribeWithKey(TriggerKeys.Breakpoint)
    getCurrentBreakpoint(): ResponsiveBreakpoint {
        return this._currentBreakpoint;
    }

    private _calcBreakpoint() {
        const size = this.getWindowSize();
        if (size.width < 1000) {
            return ResponsiveBreakpoint.Sub1000;
        }
        return ResponsiveBreakpoint.Large;
    }

    @autoSubscribeWithKey(TriggerKeys.WindowSize)
    getWindowSize() {
        return {
            width: this._lastWidth,
            height: this._lastHeight
        };
    }

    @autoSubscribeWithKey(TriggerKeys.ViewerSize)
    getViewerArea() {
        return {
            width: this._lastWidth,
            height: this._lastHeight - this.getReservedToolbarHeight()
        };
    }

    @autoSubscribeWithKey(TriggerKeys.Breakpoint)
    getReservedToolbarHeight() {
        if (this._currentBreakpoint === ResponsiveBreakpoint.Sub1000) {
            return 24;
        }
        return 44;
    }

    @autoSubscribeWithKey(TriggerKeys.MaxRowsCols)
    getMaxRowsCols() {
        return {
            rows: this._lastMaxRows,
            cols: this._lastMaxCols
        };
    }
}

export default new ResponsiveDesignStoreImpl();
