import _ = require('lodash');
import { StoreBase, AutoSubscribeStore, autoSubscribe, key, disableWarnings } from 'resub';

import DicomImage = require('../Dicom/DicomImage');
import DicomSeriesStore = require('./DicomSeriesStore');
import ImageDownloadStore = require('./ImageDownloadStore');
import LayoutStore = require('./LayoutStore');
import PatientContextStore = require('./PatientContextStore');
import Point2D = require('../Utils/Point2D');
import Point3D = require('../Utils/Point3D');

@AutoSubscribeStore
class DisplaySettingsStoreImpl extends StoreBase {
    private _settingsPerPanel: DisplaySettings[] = [];

    private _showLines: boolean = false;

    private _anyButtonDown: boolean = false;

    private _rootStudyUID: string = null;

    constructor() {
        super();

        PatientContextStore.subscribe(this._patientContextChange);

        LayoutStore.subscribe(this._layoutChanged);
        this._layoutChanged();
    }

    private _patientContextChange = () => {
        if (this._rootStudyUID !== PatientContextStore.getRootStudyUID()) {
            this._rootStudyUID = PatientContextStore.getRootStudyUID();

            // Wipe
            this.clearLayout();
        }

        this._layoutCurrentStudy();
    };

    private _layoutCurrentStudy() {
        const availableStudies = PatientContextStore.getAvailableStudies();

        // Figure out how to hang the study
        // TODO: Hanging protocols. For now, just fill available windows with series from the original study.
        const initialStudy = _.find(availableStudies, study => study.stuInstID === this._rootStudyUID);
        if (initialStudy) {
            // May not have the study info yet.  Wait until we do.

            for (let i = 0; i < initialStudy.series.length; i++) {
                let foundSeries = false;
                for (let h = 0; h < this._settingsPerPanel.length; h++) {
                    if (this._settingsPerPanel[h].seriesInstanceUID === initialStudy.series[i].serInstID) {
                        foundSeries = true;
                        break;
                    }
                }
                if (foundSeries) {
                    continue;
                }

                const vacantPanel = this.getFirstVacantPanel();
                if (vacantPanel === null) {
                    break;
                }

                const series = initialStudy.series[i];
                this.setSeriesToPanel(i, initialStudy.stuInstID, series.serInstID);
            }
        }
    }

    private _layoutChanged = () => {
        const layout = LayoutStore.getLayout();
        const numMax = layout.rows * layout.cols;
        if (this._settingsPerPanel.length > numMax) {
            this._settingsPerPanel.splice(numMax);
            // Don't emit since the panels are going to be dead or new anyway
        } else {
            for (let i = this._settingsPerPanel.length; i < numMax; i++) {
                this._settingsPerPanel.push(this._getDefaultDisplaySettings());
                this.trigger(i);
            }

            // Re-layout study
            this._layoutCurrentStudy();
        }
    };

    private _getFreshPanelDisplaySettings(): DisplaySettings {
        let ds = this._getDefaultDisplaySettings();
        ds.seriesInstanceUID = null;
        ds.studyInstanceUID = null;
        ds.selectedPanel = false;
        return ds;
    }

    private _getDefaultDisplaySettings(): DisplaySettings {
        return {
            zoom: 1.0,
            centerX: 0,
            centerY: 0,

            forceInvert: false,
            forceFlipH: false,
            forceFlipV: false,
            forceRotateCCW: false,

            defaultWindowLevel: true,

            reticleDisplayPoint: null,

            linkScrolling: false
        };
    }

    setSeriesToPanel(panelIndex: number, studyInstanceUID: string, seriesInstanceUID: string, mprDef?: MPRDefinition) {
        let settings = this._settingsPerPanel[panelIndex];
        if (settings.studyInstanceUID !== studyInstanceUID || settings.seriesInstanceUID !== seriesInstanceUID) {
            settings.studyInstanceUID = studyInstanceUID;
            settings.seriesInstanceUID = seriesInstanceUID;
            settings.imageIndexInSeries = 0;
            settings.imageFrame = 0;
            settings.mprDefinition = mprDef;

            if (!DicomSeriesStore.getSeriesInfo(seriesInstanceUID)) {
                // Start fetch
                const ser = PatientContextStore.getSeriesInfo(studyInstanceUID, seriesInstanceUID);
                if (ser) {
                    ImageDownloadStore.retrieveSeries(seriesInstanceUID, ser.numImages);
                }
            }

            this.trigger(panelIndex);
        }
    }

    @autoSubscribe
    isVacant(@key panelIndex: number): boolean {
        return this._settingsPerPanel[panelIndex] && !this._settingsPerPanel[panelIndex].seriesInstanceUID;
    }

    @autoSubscribe
    getFirstVacantPanel(): number {
        for (let i = 0; i < this._settingsPerPanel.length; i++) {
            if (this.isVacant(i)) {
                return i;
            }
        }
        return null;
    }

    @autoSubscribe
    getDisplaySettings(@key panelIndex: number) {
        if (panelIndex >= this._settingsPerPanel.length) {
            console.error('Out of bounds on getdisplaysettings');
            return null;
        }

        return _.clone(this._settingsPerPanel[panelIndex]);
    }

    clearLayout() {
        for (let i = 0; i < this._settingsPerPanel.length; i++) {
            this._settingsPerPanel[i] = this._getFreshPanelDisplaySettings();
            this.trigger(i);
        }
    }

    resetDefaults(panelIndex: number) {
        this._settingsPerPanel[panelIndex] = _.extend(this._settingsPerPanel[panelIndex], this._getDefaultDisplaySettings());
        this.trigger(panelIndex);
    }

    @autoSubscribe
    getNumberOfPanels() {
        return this._settingsPerPanel.length;
    }

    setActivePanel(panelIndex: number) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.selectedPanel) {
            // Select the new panel
            settings.selectedPanel = true;
            this.trigger(panelIndex);

            // Deselect the other panel
            this._settingsPerPanel.forEach((panelSettings, index) => {
                if (index !== panelIndex && panelSettings.selectedPanel) {
                    panelSettings.selectedPanel = false;
                    this.trigger(index);
                }
            });

            if (this._showLines) {
                this._recalcOverlays();
            }
        }
    }

    @autoSubscribe
    getActivePanelIndex(): number{
        for (let i = 0; i < this._settingsPerPanel.length; i++) {
            if (this._settingsPerPanel[i].selectedPanel) {
                return i;
            }
        }
        return null;
    }

    @autoSubscribe
    getActivePanel(): DisplaySettings {
        const index = this.getActivePanelIndex();
        if (index !== null) {
            return this._settingsPerPanel[index];
        }
        return null;
    }

    panelScroll(panelIndex: number, scrollBy: number) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        const seriesImages = DicomSeriesStore.getSeriesImages(settings.seriesInstanceUID).dicomImages;

        if (settings.mprDefinition) {
            // Now get the normal to the plane and multiply it by the distance we want to scroll
            const rVector = settings.mprDefinition.topRight.subtractPoint(settings.mprDefinition.topLeft).normalized();
            const dVector = settings.mprDefinition.bottomLeft.subtractPoint(settings.mprDefinition.topLeft).normalized();
            const normalVector = rVector.cross(dVector);
            const sliceSpacing = seriesImages[1].getImagePosition().subtractPoint(seriesImages[0].getImagePosition()).distanceFromOrigin();
            const adjustedNormal = normalVector.multiplyBy(scrollBy*sliceSpacing);

            // Translate all 4 corners by the new adjustedNormal amount
            settings.mprDefinition = {
                topLeft: settings.mprDefinition.topLeft.addPoint(adjustedNormal),
                topRight: settings.mprDefinition.topRight.addPoint(adjustedNormal),
                bottomLeft: settings.mprDefinition.bottomLeft.addPoint(adjustedNormal),
                bottomRight: settings.mprDefinition.bottomRight.addPoint(adjustedNormal)
            };

            this.trigger(panelIndex);

            this._recalcOverlays();
        } else {
            const beforeImage = settings.imageIndexInSeries;
            let newImage = beforeImage;
            const beforeFrame = settings.imageFrame;
            let newFrame = beforeFrame;

            if (scrollBy > 0) {
                if (beforeFrame < seriesImages[beforeImage].frameData.length - 1) {
                    // Same image, next frame
                    newFrame = beforeFrame + 1;
                } else if (beforeImage < seriesImages.length - 1) {
                    // Next image
                    newImage = beforeImage + 1;
                    newFrame = 0;
                } else {
                    // Wrap back to first image!
                    newImage = 0;
                    newFrame = 0;
                }
            } else if (scrollBy < 0) {
                if (beforeFrame > 0) {
                    // Same image, previous frame
                    newFrame = beforeFrame - 1;
                } else if (beforeImage > 0) {
                    // Previous image
                    newImage = beforeImage - 1;
                    newFrame = seriesImages[beforeImage - 1].frameData.length - 1;
                } else {
                    // Wrap to last frame of last image!
                    newImage = seriesImages.length - 1;
                    newFrame = seriesImages[seriesImages.length - 1].frameData.length - 1;
                }
            }

            if (newImage !== beforeImage || newFrame !== beforeFrame) {
                settings.imageIndexInSeries = newImage;
                settings.imageFrame = newFrame;
                this.trigger(panelIndex);

                if (settings.linkScrolling) {
                    const imageInstance = seriesImages[newImage];
                    const centerPoint = imageInstance.getAbsoluteCenter();
                    _.each(this._settingsPerPanel, (otherSettings, index) => {
                        if (!otherSettings.linkScrolling || index === panelIndex) {
                            return;
                        }

                        if (otherSettings.studyInstanceUID !== settings.studyInstanceUID) {
                            // If we don't have offset linking active, only scroll other series from the same study
                            // TODO Ed: Not sure if this is the right behavior or not...
                            return;
                        }

                        // TODO Ed: Should we limit linked scrolling to only other sets near the same axis as the series being scrolled?

                        const dicomSeries = DicomSeriesStore.getSeriesImages(otherSettings.seriesInstanceUID);
                        const closestImage = dicomSeries.getClosestImageIndexToPointForPanel(centerPoint);

                        if (closestImage.index !== -1 && closestImage.index !== otherSettings.imageIndexInSeries) {
                            otherSettings.imageIndexInSeries = closestImage.index;
                            otherSettings.imageFrame = 0;
                            this.trigger(index);
                        }
                    });
                }

                this._recalcOverlays();
            }
        }
    }

    deltaWindowLevel(panelIndex: number, widthDelta: number, centerDelta: number) {
        const settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        let oldWidth = settings.overrideWindowWidth;
        let oldCenter = settings.overrideWindowCenter;

        if (settings.defaultWindowLevel) {
            // Going to override -- have to look up the current values
            const seriesImages = DicomSeriesStore.getSeriesImages(settings.seriesInstanceUID).dicomImages;
            const image = seriesImages[settings.imageIndexInSeries];

            oldCenter = image.getWindowCenter();
            oldWidth = image.getWindowWidth();
        }

        let newWidth = oldWidth + widthDelta;
        let newCenter = oldCenter + centerDelta;
        if (newWidth < 1) {
            newWidth = 1;
        }

        if (newWidth !== oldWidth || newCenter !== oldCenter) {
            // Chain through to setter
            this.setWindowLevel(panelIndex, newWidth, newCenter);
        }
    }

    setWindowLevel(panelIndex: number, newWidth: number, newCenter: number) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        settings.defaultWindowLevel = false;
        settings.overrideWindowWidth = newWidth;
        settings.overrideWindowCenter = newCenter;

        this.trigger(panelIndex);

        // TODO Ed: Linked window handling -- I seem to remember this being a thing for W/L.  Confirm.
    }

    setZoomPan(panelIndex: number, newZoom: number, newCenterX: number, newCenterY: number) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        settings.zoom = newZoom;
        settings.centerX = newCenterX;
        settings.centerY = newCenterY;

        this.trigger(panelIndex);
    }

    invertView(panelIndex: number) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        settings.forceInvert = !settings.forceInvert;

        this.trigger(panelIndex);
    }

    rotateView(panelIndex: number, clockwise: boolean) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        // Rotation 90 degrees at a time all 4 rotations around the circle can actually be simplified to a single state of rotated
        // 90 degrees and then using horizontal and vertical flipping.  For example, a 180 degree rotation is the same as
        // just flipping horizontally and vertically.

        if (settings.forceRotateCCW) {
            settings.forceFlipH = !settings.forceFlipH;
            settings.forceFlipV = !settings.forceFlipV;
        }

        if (clockwise) {
            settings.forceFlipH = !settings.forceFlipH;
            settings.forceFlipV = !settings.forceFlipV;
        }

        settings.forceRotateCCW = !settings.forceRotateCCW;

        this.trigger(panelIndex);
    }

    flipView(panelIndex: number, horizontal: boolean) {
        let settings = this._settingsPerPanel[panelIndex];
        if (!settings.seriesInstanceUID) {
            return;
        }

        // If we're rotated, flip the command
        if (settings.forceRotateCCW) {
            horizontal = !horizontal;
        }

        if (horizontal) {
            settings.forceFlipH = !settings.forceFlipH;
        } else {
            settings.forceFlipV = !settings.forceFlipV;
        }

        this.trigger(panelIndex);
    }

    startMPR(panelIndex: number, mprDef: MPRDefinition) {
        let settingsSrc = this._settingsPerPanel[panelIndex];
        if (!settingsSrc.seriesInstanceUID) {
            return;
        }

        const dicomSeries = DicomSeriesStore.getSeriesImages(settingsSrc.seriesInstanceUID);
        if (dicomSeries.dicomImages.length < 3) {
            // Need at least 3 images for MPR to do anything (really need a lot more than that...)
            return;
        }                        

        // Create the mpr series for another panel
        for (let i = 0; i < this._settingsPerPanel.length; i++) {
            let settings = this._settingsPerPanel[i];
            if (!settings.seriesInstanceUID) {
                // Found an empty!  Add it!
                this.setSeriesToPanel(i, settingsSrc.studyInstanceUID, settingsSrc.seriesInstanceUID, mprDef);

                this._recalcOverlays();

                // Only add it to one, so break out of the for loop
                break;
            }
        }
    }

    localize(panelIndex: number, localizeTo: Point3D) {
        let settingsSrc = this._settingsPerPanel[panelIndex];
        if (!settingsSrc.studyInstanceUID) {
            return;
        }

        this._settingsPerPanel.forEach((settings, index) => {
            // TODO: Same rule question as with linked scrolling -- localize across all studies?  Only with offsets?...
            if (settings.studyInstanceUID !== settingsSrc.studyInstanceUID) {
                return;
            }

            const dicomSeries = DicomSeriesStore.getSeriesImages(settings.seriesInstanceUID);
            if (!dicomSeries) {
                return;
            }

            settings.reticleDisplayPoint = localizeTo;

            if (settings.mprDefinition) {
                // Calculate how far away from the plane of the MPR imageset this point is
                const rVector = settings.mprDefinition.topRight.subtractPoint(settings.mprDefinition.topLeft).normalized();
                const dVector = settings.mprDefinition.bottomLeft.subtractPoint(settings.mprDefinition.topLeft).normalized();
                const relativePoint = localizeTo.getPositionRelativeToPlane(settings.mprDefinition.topLeft, rVector, dVector);

                // Now get the normal to the plane and multiply it by the distance
                const normalVector = rVector.cross(dVector);
                const adjustedNormal = normalVector.multiplyBy(relativePoint.zPos);

                // Translate all 4 corners by the new adjustedNormal amount
                settings.mprDefinition = {
                    topLeft: settings.mprDefinition.topLeft.addPoint(adjustedNormal),
                    topRight: settings.mprDefinition.topRight.addPoint(adjustedNormal),
                    bottomLeft: settings.mprDefinition.bottomLeft.addPoint(adjustedNormal),
                    bottomRight: settings.mprDefinition.bottomRight.addPoint(adjustedNormal)
                };

                this.trigger(index);
            } else {
                // Figure out what image to scroll to
                let closestImage = dicomSeries.getClosestImageIndexToPointForPanel(localizeTo);
                if (closestImage.index !== -1) {
                    // Found the closest one -- scroll to it
                    if (index !== panelIndex) {
                        settings.imageIndexInSeries = closestImage.index;
                        settings.imageFrame = 0;
                    }

                    this.trigger(index);
                }
            }
        });

        this._recalcOverlays();
    }

    private _recalcOverlays() {
        const selPanelIndex = this.getActivePanelIndex();
        const selPanelSettings = this._settingsPerPanel[selPanelIndex];

        this._settingsPerPanel.forEach((settings, index) => {
            let changed = false;
            if (this._showLines && !settings.selectedPanel && selPanelSettings && selPanelSettings.seriesInstanceUID) {
                if (selPanelSettings.mprDefinition) {
                    const newMPRs: any = { [selPanelIndex]: selPanelSettings.mprDefinition };
                    if (!_.isEqual(settings.overlayMPRs, newMPRs)) {
                        settings.overlayMPRs = newMPRs;
                        changed = true;
                    }
                    if (settings.overlaySeries && !_.isEmpty(settings.overlaySeries)) {
                        settings.overlaySeries = null;
                        changed = true;
                    }
                } else {
                    const newSeries: any = {
                        [selPanelIndex]: {
                            seriesInstanceUID: selPanelSettings.seriesInstanceUID,
                            selectedImage: selPanelSettings.imageIndexInSeries
                        }
                    };
                    if (!_.isEqual(settings.overlaySeries, newSeries)) {
                        settings.overlaySeries = newSeries;
                        changed = true;
                    }
                    if (settings.overlayMPRs && !_.isEmpty(settings.overlayMPRs)) {
                        settings.overlayMPRs = null;
                        changed = true;
                    }
                }
            } else {
                // Clear lines, since either showLines is disabled or it's the selected panel
                if (settings.overlayMPRs && !_.isEmpty(settings.overlayMPRs)) {
                    settings.overlayMPRs = null;
                    changed = true;
                }
                if (settings.overlaySeries && !_.isEmpty(settings.overlaySeries)) {
                    settings.overlaySeries = null;
                    changed = true;
                }
            }
            if (changed) {
                this.trigger(index);
            }
        });
    }

    setLinkedScrolling(panelIndex: number, linked: boolean) {
        this._settingsPerPanel[panelIndex].linkScrolling = linked;
        this.trigger(panelIndex);
    }

    @autoSubscribe
    isShowingLines() {
        return this._showLines;
    }

    setShowLines(show: boolean) {
        if (this._showLines === show) {
            return;
        }

        this._showLines = show;
        this._recalcOverlays();
    }

    startButtonDown(panelIndex: number) {
        this._anyButtonDown = true;
        this.setActivePanel(panelIndex);
        this.trigger();
    }

    endButtonDown(panelIndex: number) {
        this._anyButtonDown = false;
        this.trigger();
    }

    // TODO: Make this less convoluted
    @disableWarnings
    getFilterNextRender() {
        return !this._anyButtonDown;
    }
}

export = new DisplaySettingsStoreImpl();
