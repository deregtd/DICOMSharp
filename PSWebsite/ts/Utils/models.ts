interface PSStudySnapshot {
    stuInstID: string;
    stuDateTime: string;
    accessionNum: string;
    modality: string;
    stuDesc: string;
}

interface PSStudySnapshotExtended extends PSStudySnapshot {
    series: PSSeriesSnapshot[];
}

interface PSSeriesSnapshot {
    serInstID: string;
    numImages: number;
    serDateTime: string;
    serNum: number;
    modality: string;
    serDesc: string;
    bodyPart: string;
}

interface PSStudyBrowserSearchResult {
    patID: string;
    patName: string;
    patBirthDate: string;
    patSex: string;

    stuInstID: string;
    accessionNum: string;

    stuID: string;
    stuDateTime: string;
    modality: string;
    refPhysician: string;
    stuDesc: string;
    deptName: string;

    numSeries: number;
    numImages: number;
    stuSizeKB: number;
}

const enum UserAccessFlags {
    None = 0,
    Reader = 1,
    ServerAdmin = 2,
    StudySend = 4,
    StudyDelete = 8
}

interface UserInfo {
    username: string;
    realname: string;
    access: UserAccessFlags;
}

const enum Tool {
    Scroll,
    WindowLevel,
    Pan,
    Zoom,
    Localizer,
    MPR
}

const enum MouseButton {
    Left = 1,
    Middle = 2,
    Right = 3,
    Wheel = 4,
    Touch = 100
}

const enum ResponsiveBreakpoint {
    Sub1000 = 1,
    Large = 2
};

interface MPRDefinition {
    topLeft: IPoint3D;
    topRight: IPoint3D;
    bottomLeft: IPoint3D;
    bottomRight: IPoint3D;
}

interface SeriesOverlay {
    seriesInstanceUID: string;
    selectedImage: number;
}

interface DisplaySettings {
    selectedPanel?: boolean;

    // Behavioral
    linkScrolling?: boolean;

    // Zoom/pan parameters
    zoom?: number;
    centerX?: number;
    centerY?: number;

    // Invert/flip parameters
    forceInvert?: boolean;
    forceFlipH?: boolean;
    forceFlipV?: boolean;
    forceRotateCCW?: boolean;

    // Window/level tracking
    defaultWindowLevel?: boolean;
    overrideWindowCenter?: number;
    overrideWindowWidth?: number;

    // Data tracking
    studyInstanceUID?: string;
    seriesInstanceUID?: string;
    mprDefinition?: MPRDefinition;
    imageIndexInSeries?: number;
    imageFrame?: number;

    // Overlays
    reticleDisplayPoint?: IPoint3D;
    overlayMPRs?: { [panelIndex: string]: MPRDefinition };
    overlaySeries?: { [panelIndex: string]: SeriesOverlay };
}

interface IPoint<T> {
    xPos: number;
    yPos: number;
    addPoint(point: T): T;
    subtractPoint(point: T): T;
    multiplyBy(factor: number): T;
    distanceFromOrigin(): number;
    normalized(): T;
}

interface IPoint2D extends IPoint<IPoint2D> {
}

interface IPoint3D extends IPoint<IPoint3D> {
    zPos: number;

    clonePoint(): IPoint3D;
    divideBy(factor: number): IPoint3D;
    cross(point: IPoint3D): IPoint3D;
    dot(point: IPoint3D): number;
    getPositionRelativeToPlane(planeOrigin: IPoint3D, planeR: IPoint3D, planeD: IPoint3D): IPoint3D;
}

interface StudyDownloadInfo {
    studyInstanceUID?: string;
    seriesInstanceUID?: string;
    downloadedImages: number;
    partialImage: number;
    totalImages: number;
}

interface DicomExtendedPatientInfo extends DicomPatientInfo {
    studies: DicomExtendedStudyInfo[];
}

interface DicomExtendedStudyInfo extends DicomStudyInfo {
    series: DicomSeriesInfo[];
}

interface DicomElement {
    group: number;
    elem: number;
    vr: string;
    desc: string;

    len?: number;
    data?: string | number | number[];
}

interface DicomPatientInfo {
    patId: string;
    patName: string;
    patBirthDate: Date;
    patSex: string;
}

interface DicomStudyInfo {
    studyInstanceUID: string;
    studyID: string;
    studyDateTime: Date;
    studyLocation: string;
    accessionNumber: string;
    studyDescription: string;
    modality: string;
}

interface DicomSeriesInfo {
    studyInstanceUID: string;
    seriesInstanceUID: string;
    seriesDateTime: Date;
    seriesDescription: string;
    seriesNumber: number;
}

interface LoginResult {
    success: boolean;

    userInfo?: UserInfo;
    errorMessage?: string;
}

interface ImageInfoResp {
    imageInstanceUID: string;
    fileSizeKB: number;
}

interface ModalInfo {
    element: JSX.Element;
    canClickOut: boolean;
    fullScreenOnResponsive: boolean;
}

interface SendStudiesModel {
    studyInstanceUIDs: string[];
    targetAE: string;
}

interface DeleteStudiesModel {
    studyInstanceUIDs: string[];
}

interface ServerSettingsResult {
    dicomServerSettings: DicomServerSettings;
    dicomServerEntities: PSEntity[];
    users: PSUser[];
    storedImagesKB: number;
}

interface DicomServerSettings {
    listeningEnabled: boolean;
    aeTitle: string;
    listenPort: number;
    imageStoragePath: string;
    imageStorageSizeMB: number;
    autoDecompress: boolean;
    storeMetadataOnlyFiles: boolean;
    verboseLogging: boolean;
    promiscuousMode: boolean;
}

const enum PSEntityFlagsMask {
    None = 0,
    SendDestination = 1
}

interface PSEntity {
    title: string;
    address: string;
    port: number;
    comment: string;
    flags: number;  //PSEntityFlagsMask
}

interface PSUser {
    username: string;
    password?: string;
    realname: string;
    access: UserAccessFlags;

    lastIP?: string;
    lastAction?: string;
}

interface PSUserExtended extends PSUser {
    oldUsername?: string;
}
