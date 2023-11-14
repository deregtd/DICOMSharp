import * as _ from 'lodash';
import * as React from 'react';
import { ComponentBase, Types as ReSubTypes } from 'resub';

import AuthStore from '../Stores/AuthStore';
import DebugUtils from '../Utils/DebugUtils';
import DicomImage from '../Dicom/DicomImage';
import DicomSeries from '../Dicom/DicomSeries';
import DicomSeriesStore from '../Stores/DicomSeriesStore';
import * as DicomTags from '../Utils/DicomTags';
import * as DicomUtils from '../Utils/DicomUtils';
import DisplaySettingsStore from '../Stores/DisplaySettingsStore';
import * as MathUtils from '../Utils/MathUtils';
import Point2D from '../Utils/Point2D';
import Point3D from '../Utils/Point3D';
import ResponsiveDesignStore, { TriggerKeys as ResponsiveDesignStoreTriggerKeys } from '../Stores/ResponsiveDesignStore';
import SelectedToolStore from '../Stores/SelectedToolStore';
import * as StringUtils from '../Utils/StringUtils';

// Force webpack to build LESS files.
require('../../less/LightBox.less');

interface LightBoxProps extends React.PropsWithChildren {
    panelIndex: number;

    // Window positioning
    xPosition: number;
    yPosition: number;
    panelWidth: number;
    panelHeight: number;
}

interface LightBoxState extends DisplaySettings {
    // Temporary MPR line while drawing it, in screen coords
    tempMPRStart?: Point2D;
    tempMPREnd?: Point2D;

    // CSS Cursor to use
    cursorStyle?: string;
}

export default class LightBox extends ComponentBase<LightBoxProps, LightBoxState> {
    // Store sub Tracking
    private _dispSetStoSub: number;
    private _dicomSerStoSub: number;
    private _selToolStoSub: number;

    // Dirty bits
    private _needReZero = false;
    private _needRecalcRenderParameters = false;
    private _needRedrawImage = false;
    private _needRecalcWLandLUT = false;
    private _lastRenderFiltered = true;

    // Mouse/touch tracking
    private _activeButton: MouseButton = null;
    private _activeTool: Tool = null;
    private _preventContext = false;
    private _clickStart: Point2D = null;
    private _lastCursor: Point2D = null;

    // Off screen rendering surfaces
    private _offScreenCanvas: HTMLCanvasElement = null;
    private _offScreenContext: CanvasRenderingContext2D = null
    private _offScreenImage: ImageData = null;
    private _offScreenImageData32: Uint32Array = null;

    // On screen rendering surfaces
    private _liveCanvas: HTMLCanvasElement = null;
    private _liveContext: CanvasRenderingContext2D = null;

    // Current view info -- cache here for perf/access across the component
    private _imageSeries: DicomSeries = null;
    private _renderImage: DicomImage = null;
    private _windowWidth: number = null;
    private _windowCenter: number = null;
    // The number of image pixels for each canvas pixel
    private _pixelPitch: number = null;
    private _virtualImageSize: Point2D = null;
    private _photoInterp: string = null;

    // Cached LookUp Table for translating raw dicom to viewable pixels
    private _lut: Uint8Array = null;
    private _lutZeroOffset: number = null;

    // Cached Palette Info
    private _palettes: ArrayLike<number>[] = null;
    private _paletteNumEntries: number = null;
    private _paletteFirstEntry: number = null;

    // Render parameters
    private _leftX: number;
    private _rightX: number;
    private _topY: number;
    private _bottomY: number;
    private _imageAtTopLeft: Point3D;
    private _imagePitchRightward: Point3D;
    private _imagePitchDownward: Point3D;

    // Notes on coordinate systems:
    // Point2D: This is going to always be screen coordinates
    // Point3D: If the variable references "image", then it's in image coordinates -- x/y are pixel counts on a raw source image
    //          somewhere, and z is a slice index.  You can't take absolute lengths of these points since z doesn't match x/y.
    //          Otherwise it's in absolute 3D coordinates and can be used appropriately by standard 3D math.

    constructor(props: LightBoxProps) {
        super(props);

        // TODO: Fix this at some point, it doesn't work with prop updates for displaysettingsstore
        this._dispSetStoSub = DisplaySettingsStore.subscribe(this._displaySettingsUpdated.bind(this), props.panelIndex);
        this._dicomSerStoSub = DicomSeriesStore.subscribe(this._dicomSeriesUpdated.bind(this));
        this._selToolStoSub = SelectedToolStore.subscribe(this._recalcCursor.bind(this));

        // Do it here after state and the initial variable values are initted
        this._rebuildOffscreenSurface(this.props.panelWidth, this.props.panelHeight);
    }

    componentWillUnmount() {
        // TODO: Fix this at some point, it doesn't work with prop updates for displaysettingsstore
        DisplaySettingsStore.unsubscribe(this._dispSetStoSub);
        DicomSeriesStore.unsubscribe(this._dicomSerStoSub);
        SelectedToolStore.unsubscribe(this._selToolStoSub);
    }

    componentWillReceiveProps(newProps: LightBoxProps): void {
        if (newProps.panelHeight > 0 && newProps.panelWidth > 0 &&
            (newProps.panelWidth !== this.props.panelWidth || newProps.panelHeight !== this.props.panelHeight)) {
            this._rebuildOffscreenSurface(newProps.panelWidth, newProps.panelHeight);

            // If width/height changes, also needs to recalc the pixel pitch
            this._recalcPixelPitch(newProps.panelWidth, newProps.panelHeight, this.state);

            this._needRecalcRenderParameters = true;
            this._needReZero = true;
            this._needRedrawImage = true;
        }
    }

    protected _buildState(props: LightBoxProps, initialBuild: boolean): LightBoxState {
        if (initialBuild) {
            let state: LightBoxState = {
                cursorStyle: this._getCursor()
            };
            state = _.extend(state, DisplaySettingsStore.getDisplaySettings(this.props.panelIndex));
            return state;
        }
    }

    private _rebuildOffscreenSurface(width: number, height: number) {
        if (!this._offScreenCanvas) {
            this._offScreenCanvas = document.createElement('canvas');
            this._offScreenContext = this._offScreenCanvas.getContext('2d', { alpha: false }) as CanvasRenderingContext2D;
        }

        this._offScreenCanvas.width = width;
        this._offScreenCanvas.height = height;

        this._offScreenImage = this._offScreenContext.createImageData(width, height);
        // Turns out, the imagedata element is just a Uint8ClampedArray everywhere I tested (IE11, Edge, and Chrome so far)
        this._offScreenImageData32 = new Uint32Array((this._offScreenImage.data as any as Uint8ClampedArray).buffer);

        this._needReZero = true;
        this._needRedrawImage = true;
    }

    private _getCursorForTool(tool: Tool, active: boolean): string {
        if (tool === Tool.Scroll) {
            return 'ns-resize';
        } else if (tool === Tool.WindowLevel) {
            return 'all-scroll';
        } else if (tool === Tool.Pan) {
            return 'move';
        } else if (tool === Tool.Zoom) {
            return 'zoom-in';
        } else if (tool === Tool.Localizer) {
            return (this._renderImage && this._renderImage.hasPositionData()) ? 'crosshair' : 'not-allowed';
        } else if (tool === Tool.MPR) {
            return (this._renderImage && this._renderImage.hasPositionData()) ? 'crosshair' : 'not-allowed';
        }
        return 'default';
    }

    private _getCursor(): string {
        if (!this.state || !this.state.seriesInstanceUID) {
            return 'not-allowed';
        } else if (this._activeTool) {
            return this._getCursorForTool(this._activeTool, true);
        } else {
            return this._getCursorForTool(SelectedToolStore.getButtonTool(MouseButton.Left), false);
        }
    }

    private _recalcCursor() {
        const newCursor = this._getCursor();
        if (this.state.cursorStyle !== newCursor) {
            this.setState({ cursorStyle: newCursor });
        }
    }

    private _forceUpdateTimer: number = null;
    private _dicomSeriesUpdated(seriesInstanceUIDs: string[]) {
        if (_.includes(seriesInstanceUIDs, this.state.seriesInstanceUID)) {
            if (!this._imageSeries || !this._renderImage) {
                // Got an image for a series we've been assigned
                this._newRenderImage(this.state);
                this._recalcPixelPitch(this.props.panelWidth, this.props.panelHeight, this.state);
                this._needRedrawImage = true;
                this._needRecalcWLandLUT = true;
                this._needRecalcRenderParameters = true;
                this._recalcCursor();
                this.forceUpdate();
            } else {
                if (!this._forceUpdateTimer) {
                    this._forceUpdateTimer = setTimeout(() => {
                        this._forceUpdateTimer = null;

                        this.forceUpdate();
                    }, 500) as unknown as number;
                }
            }
        }
    }

    private _newRenderImage(displaySettings: DisplaySettings) {
        this._imageSeries = DicomSeriesStore.getSeriesImages(displaySettings.seriesInstanceUID);
        if (this._imageSeries) {
            this._renderImage = this._imageSeries.dicomImages[displaySettings.imageIndexInSeries];
            if (this._renderImage) {
                this._photoInterp = this._renderImage.getDisplayOrDefault(DicomTags.PhotometricInterpretation).trim();

                if (this._photoInterp === "PALETTE COLOR") {
                    const paletteInfo = this._renderImage.buildPalettes();
                    this._palettes = paletteInfo.palettes;
                    this._paletteFirstEntry = paletteInfo.firstEntry;
                    this._paletteNumEntries = paletteInfo.numEntries;
                }
            }
        } else {
            this._renderImage = null;
            this._needReZero = true;
        }
    }

    private _displaySettingsUpdated() {
        let displaySettings = DisplaySettingsStore.getDisplaySettings(this.props.panelIndex) as LightBoxState;

        if (this.state.seriesInstanceUID !== displaySettings.seriesInstanceUID ||
            this.state.imageIndexInSeries !== displaySettings.imageIndexInSeries ||
            this.state.mprDefinition !== displaySettings.mprDefinition) {

            this._newRenderImage(displaySettings);
        }

        if (this.state.seriesInstanceUID !== displaySettings.seriesInstanceUID ||
            this.state.imageIndexInSeries !== displaySettings.imageIndexInSeries ||
            this.state.imageFrame !== displaySettings.imageFrame ||
            this.state.mprDefinition !== displaySettings.mprDefinition) {

            this._needRedrawImage = true;
        }

        // First update the virtual image size if needed, since many other calcs depend on it
        if (this.state.seriesInstanceUID !== displaySettings.seriesInstanceUID ||
            this.state.imageIndexInSeries !== displaySettings.imageIndexInSeries ||
            this.state.forceRotateCCW !== displaySettings.forceRotateCCW ||
            this.state.zoom !== displaySettings.zoom ||
            this.state.mprDefinition !== displaySettings.mprDefinition) {

            this._recalcPixelPitch(this.props.panelWidth, this.props.panelHeight, displaySettings);
            this._needRedrawImage = true;
        }

        if (this.state.seriesInstanceUID !== displaySettings.seriesInstanceUID ||
            this.state.imageIndexInSeries !== displaySettings.imageIndexInSeries ||
            this.state.defaultWindowLevel !== displaySettings.defaultWindowLevel ||
            this.state.overrideWindowCenter !== displaySettings.overrideWindowCenter ||
            this.state.overrideWindowWidth !== displaySettings.overrideWindowWidth ||
            this.state.forceInvert !== displaySettings.forceInvert) {

            this._needRecalcWLandLUT = true;
        }

        // Something that may have adjusted the render region of the image has changed
        if (this.state.zoom !== displaySettings.zoom ||
            this.state.centerX !== displaySettings.centerX ||
            this.state.centerY !== displaySettings.centerY) {

            // A center/zoom changed, so we definitely need to rezero
            this._needReZero = true;
        }

        if (this.state.seriesInstanceUID !== displaySettings.seriesInstanceUID ||
            this.state.imageIndexInSeries !== displaySettings.imageIndexInSeries ||
            this.state.forceRotateCCW !== displaySettings.forceRotateCCW ||
            this.state.forceFlipH !== displaySettings.forceFlipH ||
            this.state.forceFlipV !== displaySettings.forceFlipV ||
            this.state.zoom !== displaySettings.zoom ||
            this.state.centerX !== displaySettings.centerX ||
            this.state.centerY !== displaySettings.centerY ||
            this.state.mprDefinition !== displaySettings.mprDefinition) {

            // Something that may have adjusted the pixel pitches and/or image content has changed
            this._needRecalcRenderParameters = true;
            this._needRedrawImage = true;
        }

        if (DisplaySettingsStore.getFilterNextRender() && !this._lastRenderFiltered) {
            // Going from unfiltered to filter, and the last render wasn't filtered, so we need to rerender with a filter
            this._needRedrawImage = true;
        }

        this.setState(displaySettings);
    }

    render() {
        return <div className="LightBox">
                <canvas
                    className="LightBox-canvas"
                    style={ { cursor: this.state.cursorStyle } }
                    width={ this.props.panelWidth }
                    height={ this.props.panelHeight }
                    ref="canvas"
                    id={ 'canvas-' + this.props.panelIndex }
                    onTouchStart={ this._onTouchStartBind }
                    onMouseDown={ this._onMouseDownBind }
                    onWheel={ this._onMouseWheelBind }
                    onContextMenu={ this._onContextMenuBind }
                    />
            </div>;
    }

    shouldComponentUpdate(nextProps: LightBoxProps, nextState: LightBoxState): boolean {
        if (this._needRedrawImage) {
            // Make sure we always shortcut to "yes" if we already know it's pending
            return true;
        }

        return super.shouldComponentUpdate(nextProps, nextState, undefined);
    }

    private _onContextMenuBind = this._onContextMenu.bind(this);
    private _onContextMenu(e: MouseEvent) {
        document.removeEventListener('contextmenu', this._onContextMenuBind);

        if (this._preventContext) {
            e.preventDefault();
            return false;
        }
    }

    private _clientToPanel(point: Point2D): Point2D {
        return new Point2D(point.xPos - this.props.xPosition, point.yPos - this.props.yPosition - ResponsiveDesignStore.getReservedToolbarHeight());
    }

    private _onTouchStartBind = this._onTouchStart.bind(this);
    private _onTouchStart(e: React.TouchEvent<HTMLDivElement>) {
        this._startButtonPress(MouseButton.Touch, this._clientToPanel(new Point2D(e.touches[0].clientX, e.touches[0].clientY)));
    }

    private _onTouchMoveBind = this._onTouchMove.bind(this);
    private _onTouchMove(e: React.TouchEvent<HTMLDivElement>) {
        this._moveCursor(this._clientToPanel(new Point2D(e.touches[0].clientX, e.touches[0].clientY)));
    }

    private _onTouchEndBind = this._onTouchEnd.bind(this);
    private _onTouchEnd(e: React.TouchEvent<HTMLDivElement>) {
        if (e.touches.length == 0) {
            this._endButtonPress(MouseButton.Touch);
        }
    }

    private _onMouseDownBind = this._onMouseDown.bind(this);
    private _onMouseDown(e: React.MouseEvent<HTMLDivElement>) {
        let button: MouseButton = null;
        if (e.button == 0) button = MouseButton.Left;
        if (e.button == 1) button = MouseButton.Middle;
        if (e.button == 2) button = MouseButton.Right;

        if (button) {
            this._startButtonPress(button, this._clientToPanel(new Point2D(e.clientX, e.clientY)));
        }
    }

    private _onMouseMoveBind = this._onMouseMove.bind(this);
    private _onMouseMove(e: React.MouseEvent<HTMLDivElement>) {
        this._moveCursor(this._clientToPanel(new Point2D(e.clientX, e.clientY)));
    }

    private _onMouseUpBind = this._onMouseUp.bind(this);
    private _onMouseUp(e: React.MouseEvent<HTMLDivElement>) {
        let button: MouseButton = null;
        if (e.button == 0) button = MouseButton.Left;
        if (e.button == 1) button = MouseButton.Middle;
        if (e.button == 2) button = MouseButton.Right;

        if (button) {
            this._endButtonPress(button);
        }
    }

    private _onMouseWheelBind = this._onMouseWheel.bind(this);
    private _onMouseWheel(e: React.WheelEvent<HTMLDivElement>) {
        if (!this._activeButton) {
            // Simulate a click/move/release from the "wheel" button
            this._startButtonPress(MouseButton.Wheel, new Point2D(0, 0));

            this._moveCursor(new Point2D(e.deltaX, e.deltaY));

            this._endButtonPress(MouseButton.Wheel);
        }
    }

    private _startButtonPress(button: MouseButton, point: Point2D) {
        if (this._activeButton) {
            return;
        }

        if (!this._renderImage) {
            // Need an image for any of the commands...
            return;
        }

        // Treat touch as left click for tool selection
        const newTool = SelectedToolStore.getButtonTool(button === MouseButton.Touch ? MouseButton.Left : button);;

        if ((newTool === Tool.MPR || newTool === Tool.Localizer) && !this._renderImage.hasPositionData()) {
            // Need position data for MPR or Localizer
            return;
        }
        
        this._preventContext = false;

        this._activeButton = button;
        this._activeTool = newTool;

        this._clickStart = point;
        this._lastCursor = this._clickStart;

        this._recalcCursor();

        // Set capture
        if (button === MouseButton.Touch) {
            document.addEventListener('touchmove', this._onTouchMoveBind);
            document.addEventListener('touchend', this._onTouchEndBind);
        } else {
            document.addEventListener('mousemove', this._onMouseMoveBind);
            document.addEventListener('mouseup', this._onMouseUpBind);
        }

        DisplaySettingsStore.startButtonDown(this.props.panelIndex);

        if (this._activeTool === Tool.Localizer) {
            const absolutePoint = this._screenToAbsolute(this._clickStart);
            if (absolutePoint) {
                DisplaySettingsStore.localize(this.props.panelIndex, absolutePoint);
            }
        }

        if (this._activeTool === Tool.MPR) {
            this.setState({
                tempMPRStart: this._clickStart,
                tempMPREnd: this._clickStart
            });
        }
    }

    private _moveCursor(point: Point2D) {
        if (!this._activeButton) {
            return;
        }

        const cursorDelta = point.subtractPoint(this._lastCursor);
        this._lastCursor = point;

        // If, at any point, the cursor moves more than 5px from where the right mouse click started, don't let the context menu show on button up
        if (!this._preventContext && this._activeButton === MouseButton.Right && this._lastCursor.subtractPoint(this._clickStart).distanceFromOrigin() >= 5) {
            this._preventContext = true;
            document.addEventListener('contextmenu', this._onContextMenuBind);
        }

        if (this._activeTool === Tool.Scroll) {
            if (cursorDelta.yPos !== 0) {
                DisplaySettingsStore.panelScroll(this.props.panelIndex, cursorDelta.yPos);
            }
        } else if (this._activeTool === Tool.WindowLevel) {
            DisplaySettingsStore.deltaWindowLevel(this.props.panelIndex, cursorDelta.xPos, cursorDelta.yPos);
        } else if (this._activeTool === Tool.Pan) {
            DisplaySettingsStore.setZoomPan(this.props.panelIndex, this.state.zoom,
                this.state.centerX + cursorDelta.xPos * this._pixelPitch, this.state.centerY + cursorDelta.yPos * this._pixelPitch);
        } else if (this._activeTool === Tool.Zoom) {
            var delta = Math.max(-1, Math.min(1, cursorDelta.yPos));
            if (delta > 0) {
                DisplaySettingsStore.setZoomPan(this.props.panelIndex, this.state.zoom / 1.1, this.state.centerX, this.state.centerY);
            } else if (delta < 0) {
                DisplaySettingsStore.setZoomPan(this.props.panelIndex, this.state.zoom * 1.1, this.state.centerX, this.state.centerY);
            }
        } else if (this._activeTool === Tool.Localizer && this._renderImage) {
            const absolutePoint = this._screenToAbsolute(point);
            if (absolutePoint) {
                DisplaySettingsStore.localize(this.props.panelIndex, absolutePoint);
            }
        } else if (this._activeTool === Tool.MPR) {
            this.setState({
                tempMPREnd: point
            });
        }
    }

    private _endButtonPress(button: MouseButton) {
        if (!this._activeButton || this._activeButton !== button) {
            return;
        }

        if (this._activeTool === Tool.MPR) {
            if (this.state.tempMPREnd.subtractPoint(this.state.tempMPRStart).distanceFromOrigin() >= 20) {
                const mprStart = this._screenToAbsolute(this.state.tempMPRStart);
                const mprEnd = this._screenToAbsolute(this.state.tempMPREnd);

                if (this.state.mprDefinition) {
                    const normalVector = this.state.mprDefinition.topRight.subtractPoint(this.state.mprDefinition.topLeft).cross(
                        this.state.mprDefinition.bottomLeft.subtractPoint(this.state.mprDefinition.topLeft));

                    // TODO: Now walk the mprStart/End points until we hit what should be the end of the image volume
                } else {
                    // Calculate the normal of the image you're on
                    const normalVector = this._renderImage.getNormalVector();
                    const sliceSpacing = this._imageSeries.dicomImages[1].getImagePosition().subtractPoint(this._imageSeries.dicomImages[0].getImagePosition()).distanceFromOrigin();
                    const adjustedNormal = normalVector.multiplyBy(sliceSpacing);

                    // Form an mpr definition from the line -- take the MPR'd line but then fix up the z coords to be the top and
                    // bottom of the image stack it's based off.
                    let mprDef: MPRDefinition = {
                        topLeft: mprStart.addPoint(adjustedNormal.multiplyBy(this.state.imageIndexInSeries)),
                        topRight: mprEnd.addPoint(adjustedNormal.multiplyBy(this.state.imageIndexInSeries)),
                        bottomLeft: mprStart.subtractPoint(adjustedNormal.multiplyBy(this._imageSeries.dicomImages.length - this.state.imageIndexInSeries)),
                        bottomRight: mprEnd.subtractPoint(adjustedNormal.multiplyBy(this._imageSeries.dicomImages.length - this.state.imageIndexInSeries))
                    };
                    DisplaySettingsStore.startMPR(this.props.panelIndex, mprDef);
                }
            }
            this.setState({ tempMPRStart: null, tempMPREnd: null });
        }

        this._activeButton = null;
        this._activeTool = null;
        this._recalcCursor();

        // Clear capture
        if (button === MouseButton.Touch) {
            document.removeEventListener('touchmove', this._onTouchMoveBind);
            document.removeEventListener('touchend', this._onTouchEndBind);
        } else {
            document.removeEventListener('mousemove', this._onMouseMoveBind);
            document.removeEventListener('mouseup', this._onMouseUpBind);
        }

        DisplaySettingsStore.endButtonDown(this.props.panelIndex);
    }

    private _animationRequest: number = null;
    protected _componentDidRender() {
        const liveCanvas = this.refs['canvas'] as HTMLCanvasElement;
        if (liveCanvas !== this._liveCanvas) {
            this._liveCanvas = liveCanvas;
            this._liveContext = this._liveCanvas.getContext('2d', { alpha: false }) as CanvasRenderingContext2D;
        }

        // Only bother requesting another animation frame if we don't have one pending already
        if (!this._animationRequest) {
            this._animationRequest = window.requestAnimationFrame(this._renderAll.bind(this));
        }
    }

    private _renderAll() {
        this._animationRequest = null;

        if (this.props.panelWidth === 0 || this.props.panelHeight === 0) {
            // Don't bother
            return;
        }

        if (this._needRecalcWLandLUT) {
            if (this._renderImage) {
                this._windowWidth = this.state.defaultWindowLevel ? this._renderImage.getWindowWidth() :
                    this.state.overrideWindowWidth;
                this._windowCenter = this.state.defaultWindowLevel ? this._renderImage.getWindowCenter() :
                    this.state.overrideWindowCenter;

                // Something changed that's an input to the lookup table.  Force a recalc.
                const newLut = this._renderImage.buildLUT(this.state.forceInvert, this._windowCenter, this._windowWidth);
                this._lut = newLut.lut;
                this._lutZeroOffset = newLut.offset;

                this._needRedrawImage = true;
            }

            this._needRecalcWLandLUT = false;
        }

        if (this._needRecalcRenderParameters) {
            this._recalcRenderParameters();
            this._needRecalcRenderParameters = false;
        }

        const dirtyTime = this._needReZero ? this._clearDirtyOffscreenCanvas() : 0;
        const renderTime = this._renderImage && this._needRedrawImage ? this._renderToOffscreenCanvas() : 0;
        const putDataTime = this._moveOffscreenDataToImage();
        const overlayRenderTime = this._renderOverlays();
        const bltTime = this._renderOffscreenToLiveCanvas();

        if (DebugUtils.DebugModeEnabled && this._renderImage) {
            // Draw render details on the onscreen canvas after the fact
            this._liveContext.font = 'bold 12px arial';
            this._liveContext.strokeStyle = 'rgba(0,0,0,1)';
            this._liveContext.fillStyle = 'rgba(128,128,255,1)';
            this._liveContext.textBaseline = 'bottom';
            this._liveContext.textAlign = 'center';

            const obj: { [key: string]: number } = {
                C: dirtyTime,
                D: renderTime,
                P: putDataTime,
                O: overlayRenderTime,
                B: bltTime,
                T: dirtyTime + renderTime + putDataTime + overlayRenderTime + bltTime
            };

            const logTxt = _.map(obj, (val, key) => key + ': ' + Math.round(val * 100) / 100 + ' ms').join(', ');

            this._renderStrokedText(this._liveContext, logTxt, this.props.panelWidth/2, this.props.panelHeight);
            console.log(logTxt);
        }
    }

    private _clearDirtyOffscreenCanvas(): number {
        this._needReZero = false;

        const startTime = performance.now();

        if (this._renderImage) {
            let clearCoord = 0;
            for (let y = 0; y < this.props.panelHeight; y++) {
                if (y < this._topY || y > this._bottomY) {
                    for (let x = 0; x < this.props.panelWidth; x++) {
                        this._offScreenImageData32[clearCoord++] = 0xff000000;
                    }
                } else {
                    for (let x = 0; x < this._leftX; x++) {
                        this._offScreenImageData32[clearCoord++] = 0xff000000;
                    }
                    clearCoord += (this._rightX - this._leftX + 1);
                    for (let x = this._rightX + 1; x < this.props.panelWidth; x++) {
                        this._offScreenImageData32[clearCoord++] = 0xff000000;
                    }
                }
            }
        } else {
            // Zero the whole thing
            for (let i = 0; i < this._offScreenImageData32.length; i++) {
                this._offScreenImageData32[i] = 0xff000000;
            }
        }

        return performance.now() - startTime;
    }

    private _renderToOffscreenCanvas() {
        this._needRedrawImage = false;
        this._lastRenderFiltered = DisplaySettingsStore.getFilterNextRender();

        if (this.state.imageFrame < 0 || this._renderImage.frameData.length <= this.state.imageFrame) {
            return;
        }

        const startTime = performance.now();

        if (this.state.mprDefinition) {
            if (this._photoInterp === "RGB") {
                // TODO: Implement someday?
            } else if (this._photoInterp === "PALETTE COLOR") {
                // TODO: Implement someday?
            } else {
                if (this._lastRenderFiltered) {
                    this._renderToOffscreenCanvas_MPR_Grayscale_Filter();
                } else {
                    this._renderToOffscreenCanvas_MPR_Grayscale_NoFilter();
                }
            }
        } else {
            if (this._photoInterp === "RGB") {
                const bPlanarOne = (this._renderImage.getNumberOrDefault(DicomTags.PlanarConfiguration, 0) === 1);
                if (bPlanarOne) {
                    if (this._lastRenderFiltered) {
                        this._renderToOffscreenCanvas_RGB_PlanarOne_Filter();
                    } else {
                        this._renderToOffscreenCanvas_RGB_PlanarOne_NoFilter();
                    }
                } else {
                    if (this._lastRenderFiltered) {
                        this._renderToOffscreenCanvas_RGB_PlanarZero_Filter();
                    } else {
                        this._renderToOffscreenCanvas_RGB_PlanarZero_NoFilter();
                    }
                }
            } else if (this._photoInterp === "PALETTE COLOR") {
                if (this._lastRenderFiltered) {
                    this._renderToOffscreenCanvas_Palette_Filter();
                } else {
                    this._renderToOffscreenCanvas_Palette_NoFilter();
                }
            } else {
                if (this._lastRenderFiltered) {
                    this._renderToOffscreenCanvas_Grayscale_Filter();
                } else {
                    this._renderToOffscreenCanvas_Grayscale_NoFilter();
                }
            }
        }

        return performance.now() - startTime;
    }

    // START GENERATED SECTION
    // Change anything in here by modifying the generator.js file and C/P the generated.txt file to here
    private _renderToOffscreenCanvas_MPR_Grayscale_NoFilter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos;
        let trackingYImageY = this._imageAtTopLeft.yPos;
        let trackingYImageSlice = this._imageAtTopLeft.zPos;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const imageSeries = this._imageSeries.dicomImages;
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            let imageSlice = trackingYImageSlice;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let src = 0;
                if (imageX >= 0 && imageX < imWidth && imageY >= 0 && imageY < imHeight && imageSlice >= -0.5 && imageSlice < imageSeries.length) {
                    let pSlice = imageSlice << 0;	// Math.floor
                    let pX = imageX << 0;	// Math.floor
                    let pY = imageY << 0;	// Math.floor
                    src = imageSeries[pSlice].frameData[0][pY * imWidth + pX];
                    src = this._lut[src + this._lutZeroOffset];
                }
                let outVal = 0xff000000 | src | (src << 8) | (src << 16);
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
                imageSlice += this._imagePitchRightward.zPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
            trackingYImageSlice += this._imagePitchDownward.zPos;
        }
    }
    private _renderToOffscreenCanvas_MPR_Grayscale_Filter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos - 0.5;
        let trackingYImageY = this._imageAtTopLeft.yPos - 0.5;
        let trackingYImageSlice = this._imageAtTopLeft.zPos - 0.5;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const imageSeries = this._imageSeries.dicomImages;
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            let imageSlice = trackingYImageSlice;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let src = 0;
                if (imageX >= 0 && imageX < imWidth && imageY >= 0 && imageY < imHeight && imageSlice >= -0.5 && imageSlice < imageSeries.length) {
                    let pSlice = imageSlice << 0;	// Math.floor
                    let pX = imageX << 0;	// Math.floor
                    let pY = imageY << 0;	// Math.floor
                    let pX1 = pX + 1;
                    if (pX1 >= imWidth) {
                        pX1 = imWidth - 1;
                    }
                    let pY1 = pY + 1;
                    if (pY1 >= imHeight) {
                        pY1 = imHeight - 1;
                    }
                    let pSlice1 = pSlice + 1;
                    if (pSlice1 >= imageSeries.length) {
                        pSlice1 = imageSeries.length - 1;
                    }
                    let xf: number, yf: number;
                    if (imageX < 0) {
                        pX = 0;
                        xf = 0;
                    } else {
                        xf = imageX - pX;
                    }
                    if (imageY < 0) {
                        pY = 0;
                        yf = 0;
                    } else {
                        yf = imageY - pY;
                    }
                    let slicef: number;
                    if (imageSlice < 0) {
                        pSlice = 0;
                        slicef = 0;
                    } else {
                        slicef = imageSlice - pSlice;
                    }
                    const w1 = (1.0 - xf) * (1.0 - yf);
                    const w2 = (xf) * (1.0 - yf);
                    const w3 = (1.0 - xf) * (yf);
                    const w4 = (xf) * (yf);
                    src = imageSeries[pSlice].frameData[0][pY * imWidth + pX];
                    let src10 = imageSeries[pSlice].frameData[0][pY * imWidth + pX1];
                    let src01 = imageSeries[pSlice].frameData[0][pY1 * imWidth + pX];
                    let src11 = imageSeries[pSlice].frameData[0][pY1 * imWidth + pX1];
                    src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;	// Math.floor
                    if (pSlice !== pSlice1) {
                        let src200 = imageSeries[pSlice1].frameData[0][pY * imWidth + pX];
                        let src210 = imageSeries[pSlice1].frameData[0][pY * imWidth + pX1];
                        let src201 = imageSeries[pSlice1].frameData[0][pY1 * imWidth + pX];
                        let src211 = imageSeries[pSlice1].frameData[0][pY1 * imWidth + pX1];
                        let src2 = ((src200 * w1) + (src210 * w2) + (src201 * w3) + (src211 * w4)) << 0;	// Math.floor
                        src = (src * (1.0 - slicef) + src2 * slicef) << 0;	// Math.floor
                    }
                    src = this._lut[src + this._lutZeroOffset];
                }
                let outVal = 0xff000000 | src | (src << 8) | (src << 16);
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
                imageSlice += this._imagePitchRightward.zPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
            trackingYImageSlice += this._imagePitchDownward.zPos;
        }
    }
    private _renderToOffscreenCanvas_Grayscale_NoFilter() {
        const imWidth = this._renderImage.getWidth();
        let trackingYImageX = this._imageAtTopLeft.xPos;
        let trackingYImageY = this._imageAtTopLeft.yPos;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let src = frameData[pY * imWidth + pX];
                src = this._lut[src + this._lutZeroOffset];
                let outVal = 0xff000000 | src | (src << 8) | (src << 16);
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_Grayscale_Filter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos - 0.5;
        let trackingYImageY = this._imageAtTopLeft.yPos - 0.5;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let pX1 = pX + 1;
                if (pX1 >= imWidth) {
                    pX1 = imWidth - 1;
                }
                let pY1 = pY + 1;
                if (pY1 >= imHeight) {
                    pY1 = imHeight - 1;
                }
                let xf: number, yf: number;
                if (imageX < 0) {
                    pX = 0;
                    xf = 0;
                } else {
                    xf = imageX - pX;
                }
                if (imageY < 0) {
                    pY = 0;
                    yf = 0;
                } else {
                    yf = imageY - pY;
                }
                const w1 = (1.0 - xf) * (1.0 - yf);
                const w2 = (xf) * (1.0 - yf);
                const w3 = (1.0 - xf) * (yf);
                const w4 = (xf) * (yf);
                let src = frameData[pY * imWidth + pX];
                let src10 = frameData[pY * imWidth + pX1];
                let src01 = frameData[pY1 * imWidth + pX];
                let src11 = frameData[pY1 * imWidth + pX1];
                src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;	// Math.floor
                src = this._lut[src + this._lutZeroOffset];
                let outVal = 0xff000000 | src | (src << 8) | (src << 16);
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_RGB_PlanarZero_NoFilter() {
        const imWidth = this._renderImage.getWidth();
        let trackingYImageX = this._imageAtTopLeft.xPos;
        let trackingYImageY = this._imageAtTopLeft.yPos;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let outVal = 0xff000000;
                for (let channel = 0; channel < 3; channel++) {
                    let src = frameData[3 * (pY * imWidth + pX) + channel];
                    src = this._lut[src + this._lutZeroOffset];
                    outVal |= src << (8 * channel);
                }
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_RGB_PlanarZero_Filter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos - 0.5;
        let trackingYImageY = this._imageAtTopLeft.yPos - 0.5;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let pX1 = pX + 1;
                if (pX1 >= imWidth) {
                    pX1 = imWidth - 1;
                }
                let pY1 = pY + 1;
                if (pY1 >= imHeight) {
                    pY1 = imHeight - 1;
                }
                let xf: number, yf: number;
                if (imageX < 0) {
                    pX = 0;
                    xf = 0;
                } else {
                    xf = imageX - pX;
                }
                if (imageY < 0) {
                    pY = 0;
                    yf = 0;
                } else {
                    yf = imageY - pY;
                }
                const w1 = (1.0 - xf) * (1.0 - yf);
                const w2 = (xf) * (1.0 - yf);
                const w3 = (1.0 - xf) * (yf);
                const w4 = (xf) * (yf);
                let outVal = 0xff000000;
                for (let channel = 0; channel < 3; channel++) {
                    let src = frameData[3 * (pY * imWidth + pX) + channel];
                    let src10 = frameData[3 * (pY * imWidth + pX1) + channel];
                    let src01 = frameData[3 * (pY1 * imWidth + pX) + channel];
                    let src11 = frameData[3 * (pY1 * imWidth + pX1) + channel];
                    src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;   // Math.floor
                    src = this._lut[src + this._lutZeroOffset];
                    outVal |= src << (8 * channel);
                }
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_RGB_PlanarOne_NoFilter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos;
        let trackingYImageY = this._imageAtTopLeft.yPos;
        const frameOffset = imWidth * imHeight;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let outVal = 0xff000000;
                for (let channel = 0; channel < 3; channel++) {
                    let src = frameData[frameOffset * channel + pY * imWidth + pX];
                    src = this._lut[src + this._lutZeroOffset];
                    outVal |= src << (8 * channel);
                }
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_RGB_PlanarOne_Filter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos - 0.5;
        let trackingYImageY = this._imageAtTopLeft.yPos - 0.5;
        const frameOffset = imWidth * imHeight;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let pX1 = pX + 1;
                if (pX1 >= imWidth) {
                    pX1 = imWidth - 1;
                }
                let pY1 = pY + 1;
                if (pY1 >= imHeight) {
                    pY1 = imHeight - 1;
                }
                let xf: number, yf: number;
                if (imageX < 0) {
                    pX = 0;
                    xf = 0;
                } else {
                    xf = imageX - pX;
                }
                if (imageY < 0) {
                    pY = 0;
                    yf = 0;
                } else {
                    yf = imageY - pY;
                }
                const w1 = (1.0 - xf) * (1.0 - yf);
                const w2 = (xf) * (1.0 - yf);
                const w3 = (1.0 - xf) * (yf);
                const w4 = (xf) * (yf);
                let outVal = 0xff000000;
                for (let channel = 0; channel < 3; channel++) {
                    let src = frameData[frameOffset * channel + pY * imWidth + pX];
                    let src10 = frameData[frameOffset * channel + pY * imWidth + pX1];
                    let src01 = frameData[frameOffset * channel + pY1 * imWidth + pX];
                    let src11 = frameData[frameOffset * channel + pY1 * imWidth + pX1];
                    src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;   // Math.floor
                    src = this._lut[src + this._lutZeroOffset];
                    outVal |= src << (8 * channel);
                }
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_Palette_NoFilter() {
        const imWidth = this._renderImage.getWidth();
        let trackingYImageX = this._imageAtTopLeft.xPos;
        let trackingYImageY = this._imageAtTopLeft.yPos;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let outVal = 0xff000000;
                for (let channel = 0; channel < 3; channel++) {
                    let psrc = frameData[pY * imWidth + pX] - this._paletteFirstEntry;
                    if (psrc < 0) {
                        psrc = 0;
                    } else if (psrc >= this._paletteNumEntries) {
                        psrc = this._paletteNumEntries - 1;
                    }
                    let src = this._palettes[channel][psrc];
                    src = this._lut[src + this._lutZeroOffset];
                    outVal |= src << (8 * channel);
                }
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    private _renderToOffscreenCanvas_Palette_Filter() {
        const imWidth = this._renderImage.getWidth();
        const imHeight = this._renderImage.getHeight();
        let trackingYImageX = this._imageAtTopLeft.xPos - 0.5;
        let trackingYImageY = this._imageAtTopLeft.yPos - 0.5;
        let pDataCoord = this._topY * this.props.panelWidth + this._leftX;
        const frameData = this._renderImage.frameData[this.state.imageFrame];
        for (let y = this._topY; y <= this._bottomY; y++) {
            let imageX = trackingYImageX;
            let imageY = trackingYImageY;
            for (let x = this._leftX; x <= this._rightX; x++) {
                let pX = imageX << 0;	// Math.floor
                let pY = imageY << 0;	// Math.floor
                let pX1 = pX + 1;
                if (pX1 >= imWidth) {
                    pX1 = imWidth - 1;
                }
                let pY1 = pY + 1;
                if (pY1 >= imHeight) {
                    pY1 = imHeight - 1;
                }
                let xf: number, yf: number;
                if (imageX < 0) {
                    pX = 0;
                    xf = 0;
                } else {
                    xf = imageX - pX;
                }
                if (imageY < 0) {
                    pY = 0;
                    yf = 0;
                } else {
                    yf = imageY - pY;
                }
                const w1 = (1.0 - xf) * (1.0 - yf);
                const w2 = (xf) * (1.0 - yf);
                const w3 = (1.0 - xf) * (yf);
                const w4 = (xf) * (yf);
                let outVal = 0xff000000;
                for (let channel = 0; channel < 3; channel++) {
                    let psrc = frameData[pY * imWidth + pX] - this._paletteFirstEntry;
                    if (psrc < 0) {
                        psrc = 0;
                    } else if (psrc >= this._paletteNumEntries) {
                        psrc = this._paletteNumEntries - 1;
                    }
                    let src = this._palettes[channel][psrc];
                    let psrc10 = frameData[pY * imWidth + pX1] - this._paletteFirstEntry;
                    if (psrc10 < 0) {
                        psrc10 = 0;
                    } else if (psrc10 >= this._paletteNumEntries) {
                        psrc10 = this._paletteNumEntries - 1;
                    }
                    let src10 = this._palettes[channel][psrc10];
                    let psrc01 = frameData[pY1 * imWidth + pX] - this._paletteFirstEntry;
                    if (psrc01 < 0) {
                        psrc01 = 0;
                    } else if (psrc01 >= this._paletteNumEntries) {
                        psrc01 = this._paletteNumEntries - 1;
                    }
                    let src01 = this._palettes[channel][psrc01];
                    let psrc11 = frameData[pY1 * imWidth + pX1] - this._paletteFirstEntry;
                    if (psrc11 < 0) {
                        psrc11 = 0;
                    } else if (psrc11 >= this._paletteNumEntries) {
                        psrc11 = this._paletteNumEntries - 1;
                    }
                    let src11 = this._palettes[channel][psrc11];
                    src = ((src * w1) + (src10 * w2) + (src01 * w3) + (src11 * w4)) << 0;   // Math.floor
                    src = this._lut[src + this._lutZeroOffset];
                    outVal |= src << (8 * channel);
                }
                this._offScreenImageData32[pDataCoord++] = outVal;
                imageX += this._imagePitchRightward.xPos;
                imageY += this._imagePitchRightward.yPos;
            }
            pDataCoord += this.props.panelWidth - 1 - this._rightX + this._leftX;
            trackingYImageX += this._imagePitchDownward.xPos;
            trackingYImageY += this._imagePitchDownward.yPos;
        }
    }
    // END GENERATED SECTION

    private _renderOffscreenToLiveCanvas() {
        const startTime = performance.now();

        // Sub-millisecond on edge and chrome
        this._liveContext.drawImage(this._offScreenCanvas, 0, 0);

        return performance.now() - startTime;
    }

    private _moveOffscreenDataToImage(): number {
        const startTime = performance.now();

        // Move the bits over to the GPU
        this._offScreenContext.putImageData(this._offScreenImage, 0, 0);

        return performance.now() - startTime;
    }

    private _renderOverlays() {
        const startTime = performance.now();

        if (this._renderImage) {
            if (this.state.reticleDisplayPoint) {
                const screenCoords = this._absoluteToScreen(this.state.reticleDisplayPoint);
                if (screenCoords) {
                    this._offScreenContext.beginPath();
                    this._offScreenContext.moveTo(screenCoords.xPos - 3.5, screenCoords.yPos + 0.5);
                    this._offScreenContext.lineTo(screenCoords.xPos + 4.5, screenCoords.yPos + 0.5);
                    this._offScreenContext.moveTo(screenCoords.xPos + 0.5, screenCoords.yPos - 3.5);
                    this._offScreenContext.lineTo(screenCoords.xPos + 0.5, screenCoords.yPos + 4.5);
                    this._offScreenContext.strokeStyle = 'rgba(0, 160, 255, 1.0)';
                    this._offScreenContext.lineWidth = 1;
                    this._offScreenContext.stroke();
                }
            }

            if (this.state.tempMPRStart) {
                this._offScreenContext.beginPath();
                this._offScreenContext.moveTo(this.state.tempMPRStart.xPos, this.state.tempMPRStart.yPos);
                this._offScreenContext.lineTo(this.state.tempMPREnd.xPos, this.state.tempMPREnd.yPos);
                this._offScreenContext.strokeStyle = 'rgba(0, 160, 255, 1.0)';
                this._offScreenContext.lineWidth = 2;
                this._offScreenContext.stroke();
            }

            if (this.state.overlayMPRs) {
                _.each(this.state.overlayMPRs, (mpr, index) => {
                    this._offScreenContext.beginPath();
                    const tlCoords = this._absoluteToScreen(mpr.topLeft);
                    const trCoords = this._absoluteToScreen(mpr.topRight);
                    const brCoords = this._absoluteToScreen(mpr.bottomRight);
                    const blCoords = this._absoluteToScreen(mpr.bottomLeft);
                    if (tlCoords && trCoords && brCoords && blCoords) {
                        this._offScreenContext.moveTo(tlCoords.xPos, tlCoords.yPos);
                        this._offScreenContext.lineTo(trCoords.xPos, trCoords.yPos);
                        this._offScreenContext.lineTo(brCoords.xPos, brCoords.yPos);
                        this._offScreenContext.lineTo(blCoords.xPos, blCoords.yPos);
                        this._offScreenContext.lineTo(tlCoords.xPos, tlCoords.yPos);
                        this._offScreenContext.strokeStyle = 'rgba(0, 160, 255, 1.0)';
                        this._offScreenContext.lineWidth = 1;
                        this._offScreenContext.stroke();
                    }
                });
            }

            if (this.state.overlaySeries) {
                _.each(this.state.overlaySeries, (series, index) => {
                    const dicomSeries = DicomSeriesStore.getSeriesImages(series.seriesInstanceUID);

                    dicomSeries.dicomImages.forEach((image, imageIndex) => {
                        if (!image.hasPositionData()) {
                            return;
                        }

                        const imageRV = image.getImageVectorRight();
                        const imageDV = image.getImageVectorDown();
                        const imageWidth = image.getWidth();
                        const imageHeight = image.getHeight();
                        const pixelSpacing = image.getPixelSpacing();

                        const imageTLAbsolute = image.getImagePosition();
                        const imageTRAbsolute = imageTLAbsolute.addPoint(imageRV.multiplyBy(pixelSpacing.xPos * imageWidth));
                        const imageBLAbsolute = imageTLAbsolute.addPoint(imageDV.multiplyBy(pixelSpacing.yPos * imageHeight));
                        const imageBRAbsolute = imageBLAbsolute.addPoint(imageRV.multiplyBy(pixelSpacing.xPos * imageWidth));

                        const tlCoords = this._absoluteToScreen(imageTLAbsolute);
                        const trCoords = this._absoluteToScreen(imageTRAbsolute);
                        const blCoords = this._absoluteToScreen(imageBLAbsolute);
                        const brCoords = this._absoluteToScreen(imageBRAbsolute);

                        if (tlCoords && trCoords && blCoords && brCoords) {
                            this._offScreenContext.beginPath();
                            this._offScreenContext.moveTo(tlCoords.xPos, tlCoords.yPos);
                            this._offScreenContext.lineTo(trCoords.xPos, trCoords.yPos);
                            this._offScreenContext.lineTo(brCoords.xPos, brCoords.yPos);
                            this._offScreenContext.lineTo(blCoords.xPos, blCoords.yPos);
                            this._offScreenContext.lineTo(tlCoords.xPos, tlCoords.yPos);
                            this._offScreenContext.strokeStyle = 'rgba(0, 160, 255, 1.0)';
                            this._offScreenContext.lineWidth = imageIndex === series.selectedImage ? 3 : 1;
                            this._offScreenContext.stroke();
                        }
                    });
                });
            }

            // Calculate font size
            const fontSize = Math.min(Math.max(Math.ceil(Math.min(this.props.panelWidth, this.props.panelHeight) / 30), 6), 20);
            const margin = Math.ceil(fontSize / 6);
            this._offScreenContext.font = '500 ' + fontSize + 'px tahoma';
            this._offScreenContext.strokeStyle = 'rgba(0,0,0,1)';
            this._offScreenContext.fillStyle = 'rgba(255,255,255,1)';

            // Pre-calculate any necessary info
            const seriesInfo = this._renderImage.getSeriesInfo();
            const studyInfo = this._renderImage.getStudyInfo();
            const patientInfo = this._renderImage.getPatientInfo();
            let absoluteTL: Point3D;
            let absoluteCenter: Point3D;
            let rVector: Point3D, dVector: Point3D;
            let normalVector: Point3D;
            if (this.state.mprDefinition) {
                absoluteTL = this.state.mprDefinition.topLeft;
                const topRightAbsolute = this.state.mprDefinition.topRight;
                const bottomLeftAbsolute = this.state.mprDefinition.bottomLeft;
                absoluteCenter = topRightAbsolute.addPoint(bottomLeftAbsolute).divideBy(2);
                rVector = topRightAbsolute.subtractPoint(absoluteTL);
                dVector = bottomLeftAbsolute.subtractPoint(absoluteTL);
            } else {
                absoluteTL = this._renderImage.getImagePosition();
                absoluteCenter = this._renderImage.getAbsoluteCenter();
                rVector = this._renderImage.getImageVectorRight();
                dVector = this._renderImage.getImageVectorDown();
            }
            if (rVector && dVector) {
                normalVector = rVector.cross(dVector);
            }

            let sliceLoc: string = null;
            if (absoluteTL) {
                const axial = Math.abs(normalVector.dot(new Point3D(0, 0, 1)));
                const saggital = Math.abs(normalVector.dot(new Point3D(1, 0, 0)));
                const coronal = Math.abs(normalVector.dot(new Point3D(0, 1, 0)));
                if (axial > saggital && axial > coronal) {
                    sliceLoc = 'Ax: ' + (absoluteTL.zPos < 0 ? 'I' : 'S') + MathUtils.roundTo(absoluteTL.zPos, 1);
                    if (absoluteTL.zPos !== absoluteCenter.zPos) {
                        sliceLoc += ' UL/' + (absoluteCenter.zPos < 0 ? 'I' : 'S') + MathUtils.roundTo(absoluteCenter.zPos, 1) + ' COI';
                    }
                } else if (saggital > coronal) {
                    sliceLoc = 'Sag: ' + (absoluteTL.xPos < 0 ? 'R' : 'L') + MathUtils.roundTo(absoluteTL.xPos, 1);
                    if (absoluteTL.xPos !== absoluteCenter.xPos) {
                        sliceLoc += ' UL/' + (absoluteCenter.xPos < 0 ? 'R' : 'L') + MathUtils.roundTo(absoluteCenter.xPos, 1) + ' COI';
                    }
                } else {
                    sliceLoc = 'Cor: ' + (absoluteTL.yPos < 0 ? 'A' : 'P') + MathUtils.roundTo(absoluteTL.yPos, 1);
                    if (absoluteTL.yPos !== absoluteCenter.yPos) {
                        sliceLoc += ' UL/' + (absoluteCenter.yPos < 0 ? 'A' : 'P') + MathUtils.roundTo(absoluteCenter.yPos, 1) + ' COI';
                    }
                }
            }

            if (this.state.imageFrame < 0 || this._renderImage.frameData.length <= this.state.imageFrame) {
                this._offScreenContext.textBaseline = 'middle';
                this._offScreenContext.textAlign = 'center';
                this._renderStrokedText(this._offScreenContext, 'No Image Data', this.props.panelWidth/2, this.props.panelHeight/2);
            }

            // Drop Top Left Corner
            this._offScreenContext.textBaseline = 'top';
            this._offScreenContext.textAlign = 'left';
            let tlY = 0;
            //this._renderStrokedText(this._offScreenContext, 'Ex: ' + studyInfo.studyID, margin, tlY++ * fontSize + margin);
            //this._renderStrokedText(this._offScreenContext, 'Stu: ' + studyInfo.studyDescription, margin, tlY++ * fontSize + margin);
            //this._renderStrokedText(this._offScreenContext, 'Ser: ' + seriesInfo.seriesNumber + '. ' + seriesInfo.seriesDescription, margin, tlY++ * fontSize + margin);
            if (!this.state.mprDefinition) {
                let imText = 'Im: ' + (this.state.imageIndexInSeries + 1) + '/' + this._imageSeries.dicomImages.length;
                let numFrames = this._renderImage.getNumberOrDefault(DicomTags.NumberOfFrames, 1);
                if (numFrames > 1) {
                    imText += ', Fr: ' + (this.state.imageFrame + 1) + '/' + numFrames;
                }
                this._renderStrokedText(this._offScreenContext, imText, margin, tlY++ * fontSize + margin);
            }
            if (sliceLoc) {
                this._renderStrokedText(this._offScreenContext, sliceLoc, margin, tlY++ * fontSize + margin);
            }

            // Draw upper right corner
            this._offScreenContext.textBaseline = 'top';
            this._offScreenContext.textAlign = 'right';
            this._renderStrokedText(this._offScreenContext, studyInfo.studyLocation, this.props.panelWidth - margin, margin);
            this._renderStrokedText(this._offScreenContext, StringUtils.formatName(patientInfo.patName), this.props.panelWidth - margin, 1 * fontSize + margin);
            const ageString = StringUtils.getAgeString(studyInfo.studyDateTime, patientInfo.patBirthDate);
            this._renderStrokedText(this._offScreenContext,
                (ageString ? ageString + ' ' : '') + patientInfo.patSex + ' ' + patientInfo.patId, this.props.panelWidth - margin, 2 * fontSize + margin);
            this._renderStrokedText(this._offScreenContext, studyInfo.accessionNumber, this.props.panelWidth - margin, 3 * fontSize + margin);

            // Draw Bottom left
            this._offScreenContext.textBaseline = 'bottom';
            this._offScreenContext.textAlign = 'left';
            this._renderStrokedText(this._offScreenContext, 'W:' + this._windowWidth + ' L:' + this._windowCenter, margin, this.props.panelHeight - margin);
            switch (this._renderImage.getDisplayOrDefault(DicomTags.Modality)) {
                case 'CT':
                    this._renderStrokedText(this._offScreenContext, MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.ExposureTime) / 1000, 1) + ' s', margin, this.props.panelHeight - 1 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, 'Tilt: ' + MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.GantryDetectorTilt), 1), margin, this.props.panelHeight - 2 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.SliceThickness), 1) + ' mm', margin, this.props.panelHeight - 3 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.XRayTubeCurrent), 1) + ' mA', margin, this.props.panelHeight - 4 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.KVP), 1) + ' kV', margin, this.props.panelHeight - 5 * fontSize - margin);
                    break;
                case 'MR':
                    this._renderStrokedText(this._offScreenContext, MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.SliceThickness), 1) + ' mm thk', margin, this.props.panelHeight - 1 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, 'TE: ' + MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.EchoTime), 1), margin, this.props.panelHeight - 2 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, 'TR: ' + MathUtils.roundTo(this._renderImage.getNumberOrDefault(DicomTags.RepetitionTime), 1), margin, this.props.panelHeight - 3 * fontSize - margin);
                    this._renderStrokedText(this._offScreenContext, 'ET: ' + MathUtils.roundTo( this._renderImage.getNumberOrDefault(DicomTags.EchoTrainLength), 1), margin, this.props.panelHeight - 4 * fontSize - margin);
                    break;
            }

            // Draw Bottom Right
            this._offScreenContext.textBaseline = 'bottom';
            this._offScreenContext.textAlign = 'right';
            let lrY = 0;
            if (this.state.zoom !== 1) {
                this._renderStrokedText(this._offScreenContext, 'Mag: ' + MathUtils.roundTo(this.state.zoom, 1) + 'x', this.props.panelWidth - margin, this.props.panelHeight - margin - lrY++ * fontSize);
            }
            if (!this.state.mprDefinition) {
                this._renderStrokedText(this._offScreenContext, this._renderImage.getWidth() + ' x ' + this._renderImage.getHeight(), this.props.panelWidth - margin, this.props.panelHeight - lrY++ * fontSize - margin);
                const ackDateTime = DicomUtils.GetDateTimeFromTags(this._renderImage.getDisplayOrDefault(DicomTags.AcquisitionDate),
                    this._renderImage.getDisplayOrDefault(DicomTags.AcquisitionTime));
                const imgDateTime = ackDateTime || seriesInfo.seriesDateTime || studyInfo.studyDateTime;
                this._renderStrokedText(this._offScreenContext, imgDateTime.toLocaleDateString(), this.props.panelWidth - margin, this.props.panelHeight - lrY++ * fontSize - margin);
                this._renderStrokedText(this._offScreenContext, imgDateTime.toLocaleTimeString(), this.props.panelWidth - margin, this.props.panelHeight - lrY++ * fontSize - margin);
            }

            // Show alignment markers at the 4 edge centers
            if (absoluteTL) {
                // Calculate left/right
                let corners = ['','','',''];

                const axialR = rVector.dot(new Point3D(0, 0, 1)), axialRAbs = Math.abs(axialR);
                const sagitallR = rVector.dot(new Point3D(1, 0, 0)), sagitallRAbs = Math.abs(sagitallR);
                const coronalR = rVector.dot(new Point3D(0, 1, 0)), coronalRAbs = Math.abs(coronalR);
                if (axialRAbs > sagitallRAbs && axialRAbs > coronalRAbs) {
                    corners[0] = axialR > 0 ? 'S' : 'I';
                    corners[1] = axialR > 0 ? 'I' : 'S';
                } else if (sagitallRAbs > coronalRAbs) {
                    corners[0] = sagitallR > 0 ? 'L' : 'R';
                    corners[1] = sagitallR > 0 ? 'R' : 'L';
                } else {
                    corners[0] = coronalR > 0 ? 'P' : 'A';
                    corners[1] = coronalR > 0 ? 'A' : 'P';
                }

                // Calculate up/down
                const axialD = dVector.dot(new Point3D(0, 0, 1)), axialDAbs = Math.abs(axialD);
                const sagitallD = dVector.dot(new Point3D(1, 0, 0)), sagitallDAbs = Math.abs(sagitallD);
                const coronalD = dVector.dot(new Point3D(0, 1, 0)), coronalDAbs = Math.abs(coronalD);
                if (axialDAbs > sagitallDAbs && axialDAbs > coronalDAbs) {
                    corners[2] = axialD > 0 ? 'S' : 'I';
                    corners[3] = axialD > 0 ? 'I' : 'S';
                } else if (sagitallDAbs > coronalDAbs) {
                    corners[2] = sagitallD > 0 ? 'L' : 'R';
                    corners[3] = sagitallD > 0 ? 'R' : 'L';
                } else {
                    corners[2] = coronalD > 0 ? 'P' : 'A';
                    corners[3] = coronalD > 0 ? 'A' : 'P';
                }

                // Flip/rotate as needed
                if (this.state.forceFlipH) {
                    const tp = corners[1];
                    corners[1] = corners[0];
                    corners[0] = tp;
                }
                if (this.state.forceFlipV) {
                    const tp = corners[3];
                    corners[3] = corners[2];
                    corners[2] = tp;
                }
                if (this.state.forceRotateCCW) {
                    const tp = corners[0];
                    corners[0] = corners[2];
                    corners[2] = corners[1];
                    corners[1] = corners[3];
                    corners[3] = tp;
                }

                // TODO: Check this...  Doesn't seem right, but copied from MFPView...
                this._offScreenContext.textBaseline = 'middle';
                this._offScreenContext.textAlign = 'right';
                this._renderStrokedText(this._offScreenContext, corners[0], this.props.panelWidth - margin, this.props.panelHeight / 2);
                this._offScreenContext.textAlign = 'left';
                this._renderStrokedText(this._offScreenContext, corners[1], margin, this.props.panelHeight / 2);
                this._offScreenContext.textAlign = 'center';
                this._offScreenContext.textBaseline = 'bottom';
                this._renderStrokedText(this._offScreenContext, corners[2], this.props.panelWidth / 2, this.props.panelHeight - margin);
                this._offScreenContext.textBaseline = 'top';
                this._renderStrokedText(this._offScreenContext, corners[3], this.props.panelWidth / 2, margin);
            }
        }

        return performance.now() - startTime;
    }

    private _renderStrokedText(context: CanvasRenderingContext2D, txt: string, x: number, y: number) {
        context.strokeText(txt, x, y);
        context.fillText(txt, x, y);
    }

    private _recalcRenderParameters() {
        if (this.props.panelWidth === 0 || this.props.panelHeight === 0 || !this._renderImage) {
            // Don't bother
            return;
        }

        // Calculate what the actual image-space corners of the image are, so we can match those to the screen coords
        const rawImageTL = this.state.mprDefinition ? this._imageSeries.absoluteIntoImage(this.state.mprDefinition.topLeft) :
            new Point3D(0, 0, this.state.imageIndexInSeries);
        const rawImageTR = this.state.mprDefinition ? this._imageSeries.absoluteIntoImage(this.state.mprDefinition.topRight) :
            new Point3D(this._renderImage.getWidth() - 1, 0, this.state.imageIndexInSeries);
        const rawImageBL = this.state.mprDefinition ? this._imageSeries.absoluteIntoImage(this.state.mprDefinition.bottomLeft) :
            new Point3D(0, this._renderImage.getHeight() - 1, this.state.imageIndexInSeries);
        const rawImageBR = this.state.mprDefinition ? this._imageSeries.absoluteIntoImage(this.state.mprDefinition.bottomRight) :
            new Point3D(this._renderImage.getWidth() - 1, this._renderImage.getHeight() - 1, this.state.imageIndexInSeries);

        let transformedImageTL = this.state.forceRotateCCW ? rawImageTR : rawImageTL;
        let transformedImageTR = this.state.forceRotateCCW ? rawImageBR : rawImageTR;
        let transformedImageBL = this.state.forceRotateCCW ? rawImageTL : rawImageBL;
        let transformedImageBR = this.state.forceRotateCCW ? rawImageBL : rawImageBR;
        if (this.state.forceFlipH) {
            let holder = transformedImageTL;
            transformedImageTL = transformedImageTR;
            transformedImageTR = holder;
            holder = transformedImageBL;
            transformedImageBL = transformedImageBR;
            transformedImageBR = holder;
        }
        if (this.state.forceFlipV) {
            let holder = transformedImageTL;
            transformedImageTL = transformedImageBL;
            transformedImageBL = holder;
            holder = transformedImageTR;
            transformedImageTR = transformedImageBR;
            transformedImageBR = holder;
        }

        // Find the screen coordinates of the edge of the image space (which may be on or off the render screen/negative).
        const screenLeftX = (this.state.centerX - this._virtualImageSize.xPos / 2) / this._pixelPitch + this.props.panelWidth / 2;
        const screenRightX = (this.state.centerX + this._virtualImageSize.xPos / 2) / this._pixelPitch + this.props.panelWidth / 2;
        const screenTopY = (this.state.centerY - this._virtualImageSize.yPos / 2) / this._pixelPitch + this.props.panelHeight / 2;
        const screenBottomY = (this.state.centerY + this._virtualImageSize.yPos / 2) / this._pixelPitch + this.props.panelHeight / 2;

        // Calculate the pixel pitches in image space along the two axes
        this._imagePitchRightward = transformedImageTR.subtractPoint(transformedImageTL).divideBy(screenRightX - screenLeftX + 1);
        this._imagePitchDownward = transformedImageBL.subtractPoint(transformedImageTL).divideBy(screenBottomY - screenTopY + 1);

        // Round off to find the integer pixel edges to render from/to, and constrain them to on-screen space
        this._leftX = Math.min(Math.max((screenLeftX + 0.5) << 0, 0), this.props.panelWidth - 1);
        this._rightX = Math.min(Math.max((screenRightX + 0.5) << 0, 0), this.props.panelWidth - 1);
        this._topY = Math.min(Math.max((screenTopY + 0.5) << 0, 0), this.props.panelHeight - 1);
        this._bottomY = Math.min(Math.max((screenBottomY + 0.5) << 0, 0), this.props.panelHeight - 1);

        // Calculate what the actual image coordinate at the screen coordinate start of rendering is.  If it's on screen, just use the
        // given coordinate.  If it's off screen on either axis, then need to adjust it.
        this._imageAtTopLeft = transformedImageTL;
        if (screenLeftX < 0) {
            this._imageAtTopLeft = this._imageAtTopLeft.addPoint(this._imagePitchRightward.multiplyBy(-screenLeftX));
        }
        if (screenTopY < 0) {
            this._imageAtTopLeft = this._imageAtTopLeft.addPoint(this._imagePitchDownward.multiplyBy(-screenTopY));
        }
    }

    private _screenToAbsolute(pointIn: Point2D): Point3D {
        const imageCoord = this._imageAtTopLeft
            .addPoint(this._imagePitchRightward.multiplyBy(pointIn.xPos - this._leftX))
            .addPoint(this._imagePitchDownward.multiplyBy(pointIn.yPos - this._topY));
        return this._imageSeries.imageIntoAbsolute(imageCoord);
    }

    private _absoluteToScreen(pointIn: Point3D): Point2D {
        // For now, first convert to image space, then from image into screen
        const point = this.state.mprDefinition ?
            this._imageSeries.absoluteIntoImage(pointIn) :
            this._renderImage.absoluteIntoThisImageWithIndex(pointIn, this.state.imageIndexInSeries);

        if (!point) {
            return null;
        }

        // Going in reverse is just reversing the above equation.
        // Mathematica solved this system of equations for me (for x and y):
        // point.x = imageTL.x + pitchR.x * (x - leftX) + pitchD.x * (y - topY)
        // point.y = imageTL.y + pitchR.y * (x - leftX) + pitchD.y * (y - topY)
        // point.z = imageTL.z + pitchR.z * (x - leftX) + pitchD.z * (y - topY)

        // x and y
        const denoXY = Math.abs(this._imagePitchDownward.yPos * this._imagePitchRightward.xPos -
            this._imagePitchDownward.xPos * this._imagePitchRightward.yPos);
        const denoXZ = Math.abs(this._imagePitchDownward.zPos * this._imagePitchRightward.xPos -
            this._imagePitchDownward.xPos * this._imagePitchRightward.zPos);
        const denoYZ = Math.abs(this._imagePitchDownward.zPos * this._imagePitchRightward.yPos -
            this._imagePitchDownward.yPos * this._imagePitchRightward.zPos);

        if (denoXY > denoXZ && denoXY > denoYZ) {
            return new Point2D(
                (point.yPos * this._imagePitchDownward.xPos -
                    this._imageAtTopLeft.yPos * this._imagePitchDownward.xPos +
                    (-point.xPos + this._imageAtTopLeft.xPos) * this._imagePitchDownward.yPos) /
                (-(this._imagePitchDownward.yPos * this._imagePitchRightward.xPos) +
                    this._imagePitchDownward.xPos * this._imagePitchRightward.yPos) + this._leftX,
                (point.yPos * this._imagePitchRightward.xPos -
                    this._imageAtTopLeft.yPos * this._imagePitchRightward.xPos -
                    point.xPos * this._imagePitchRightward.yPos +
                    this._imageAtTopLeft.xPos * this._imagePitchRightward.yPos) /
                (this._imagePitchDownward.yPos * this._imagePitchRightward.xPos -
                    this._imagePitchDownward.xPos * this._imagePitchRightward.yPos) + this._topY
            );
        }

        if (denoXZ > denoYZ) {
            return new Point2D(
                (point.zPos * this._imagePitchDownward.xPos -
                    this._imageAtTopLeft.zPos * this._imagePitchDownward.xPos +
                    (-point.xPos + this._imageAtTopLeft.xPos) * this._imagePitchDownward.zPos) /
                (-(this._imagePitchDownward.zPos * this._imagePitchRightward.xPos) +
                    this._imagePitchDownward.xPos * this._imagePitchRightward.zPos) + this._leftX,
                (point.zPos * this._imagePitchRightward.xPos -
                    this._imageAtTopLeft.zPos * this._imagePitchRightward.xPos -
                    point.xPos * this._imagePitchRightward.zPos +
                    this._imageAtTopLeft.xPos * this._imagePitchRightward.zPos) /
                (this._imagePitchDownward.zPos * this._imagePitchRightward.xPos -
                    this._imagePitchDownward.xPos * this._imagePitchRightward.zPos) + this._topY
            );
        }
        return new Point2D(
            (point.zPos * this._imagePitchDownward.yPos -
                this._imageAtTopLeft.zPos * this._imagePitchDownward.yPos +
                (-point.yPos + this._imageAtTopLeft.yPos) * this._imagePitchDownward.zPos) /
            (-(this._imagePitchDownward.zPos * this._imagePitchRightward.yPos) +
                this._imagePitchDownward.yPos * this._imagePitchRightward.zPos) + this._leftX,
            (point.zPos * this._imagePitchRightward.yPos -
                this._imageAtTopLeft.zPos * this._imagePitchRightward.yPos -
                point.yPos * this._imagePitchRightward.zPos +
                this._imageAtTopLeft.yPos * this._imagePitchRightward.zPos) /
            (this._imagePitchDownward.zPos * this._imagePitchRightward.yPos -
                this._imagePitchDownward.yPos * this._imagePitchRightward.zPos) + this._topY
        );
    }

    private _recalcPixelPitch(width: number, height: number, displaySettings: DisplaySettings) {
        // Check sizes since it may not have actually changed
        const newSize = this._getVirtualImageSize(displaySettings);
        if (!this._virtualImageSize || this._virtualImageSize.xPos !== newSize.xPos || this._virtualImageSize.yPos !== newSize.yPos) {
            this._virtualImageSize = newSize;
            this._needReZero = true;
        }

        if (this._renderImage) {
            // Find the lowest base zoom for the image to viewport ratio, taking the rotation into account
            const HZoom = width / this._virtualImageSize.xPos;
            const VZoom = height / this._virtualImageSize.yPos;
            // Find the smallest and then scale to our settings
            this._pixelPitch = 1 / (Math.min(VZoom, HZoom) * displaySettings.zoom);
        }
    }

    // Returns the number of pixels in image-space of the image to be rendered.  In MPR, this is calculated, otherwise it's just the
    // raw image size.
    private _getVirtualImageSize(settings: DisplaySettings): Point2D {
        if (!settings || !settings.seriesInstanceUID || !this._renderImage) {
            return new Point2D(0, 0);
        }

        let virtualImageUnrotated: Point2D;
        if (settings.mprDefinition) {
            const pixelSpacing = this._renderImage.getPixelSpacing();

            virtualImageUnrotated = new Point2D(
                settings.mprDefinition.topRight.subtractPoint(settings.mprDefinition.topLeft).distanceFromOrigin() / pixelSpacing.xPos,
                settings.mprDefinition.bottomLeft.subtractPoint(settings.mprDefinition.topLeft).distanceFromOrigin() / pixelSpacing.yPos
            );
        } else {
            virtualImageUnrotated = this._renderImage.getSize();
        }

        if (settings.forceRotateCCW) {
            return new Point2D(virtualImageUnrotated.yPos, virtualImageUnrotated.xPos);
        } else {
            return virtualImageUnrotated;
        }
    }
}
