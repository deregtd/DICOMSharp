import * as _ from 'lodash';
import * as SyncTasks from 'synctasks';

import AjaxClient from './AjaxClient';

function unminifySendStudiesModel(minified: SendStudiesModel): SendStudiesModel {
    return {
        'studyInstanceUIDs': minified.studyInstanceUIDs,
        'targetAE': minified.targetAE
    } as SendStudiesModel;
} 

function unminifyDeleteStudiesModel(minified: DeleteStudiesModel): DeleteStudiesModel {
    return {
        'studyInstanceUIDs': minified.studyInstanceUIDs
    } as DeleteStudiesModel;
} 

function minifyServerSettingsResult(unminified: ServerSettingsResult): ServerSettingsResult {
    return {
        storedImagesKB: unminified['storedImagesKB'],
        dicomServerSettings: minifyDicomServerSettings(unminified['dicomServerSettings']),
        dicomServerEntities: unminified['dicomServerEntities'].map(entity => minifyPSEntity(entity)),
        users: unminified['users'].map(user => minifyPSUser(user))
    } as ServerSettingsResult;
}

function minifyDicomServerSettings(unminified: DicomServerSettings): DicomServerSettings {
    return {
        aeTitle: unminified['aeTitle'],
        autoDecompress: unminified['autoDecompress'],
        imageStoragePath: unminified['imageStoragePath'],
        imageStorageSizeMB: unminified['imageStorageSizeMB'],
        listeningEnabled: unminified['listeningEnabled'],
        listenPort: unminified['listenPort'],
        promiscuousMode: unminified['promiscuousMode'],
        storeMetadataOnlyFiles: unminified['storeMetadataOnlyFiles'],
        verboseLogging: unminified['verboseLogging']
    } as DicomServerSettings;
}

function unminifyDicomServerSettings(minified: DicomServerSettings): DicomServerSettings {
    return {
        'aeTitle': minified.aeTitle,
        'autoDecompress': minified.autoDecompress,
        'imageStoragePath': minified.imageStoragePath,
        'imageStorageSizeMB': minified.imageStorageSizeMB,
        'listeningEnabled': minified.listeningEnabled,
        'listenPort': minified.listenPort,
        'promiscuousMode': minified.promiscuousMode,
        'storeMetadataOnlyFiles': minified.storeMetadataOnlyFiles,
        'verboseLogging': minified.verboseLogging
    } as DicomServerSettings;
}

function minifyPSEntity(unminified: PSEntity): PSEntity {
    return {
        address: unminified['address'],
        comment: unminified['comment'],
        flags: unminified['flags'],
        port: unminified['port'],
        title: unminified['title']
    } as PSEntity;
}

function unminifyPSEntity(minified: PSEntity): PSEntity {
    return {
        'address': minified.address,
        'comment': minified.comment,
        'flags': minified.flags,
        'port': minified.port,
        'title': minified.title
    } as PSEntity;
}

function minifyPSUser(unminified: PSUser): PSUser {
    return {
        access: unminified['access'],
        lastAction: unminified['lastAction'],
        lastIP: unminified['lastIP'],
        password: unminified['password'],
        realname: unminified['realname'],
        username: unminified['username']
    } as PSUser;
}

function unminifyPSUser(minified: PSUser): PSUser {
    return {
        'access': minified.access,
        'lastAction': minified.lastAction,
        'lastIP': minified.lastIP,
        'password': minified.password,
        'realname': minified.realname,
        'username': minified.username
    } as PSUser;
}

class PSApiClientImpl {
    private _ajaxClient = new AjaxClient();
        
    searchAsync(patientId: string, patientName: string, startDate: string, endDate: string, accessionNum: string, description: string,
        maxResults: number): SyncTasks.Promise<PSStudyBrowserSearchResult[]> {
        const searchUrl = `api/Search?startDate=${ encodeURIComponent(startDate) }&endDate=${
            encodeURIComponent(endDate) }&patName=${ encodeURIComponent(patientName)
            }&patId=${ encodeURIComponent(patientId) }&accession=${ encodeURIComponent(accessionNum)
            }&description=${ encodeURIComponent(description) }&maxResults=${ maxResults }`;
        return this._ajaxClient.getJSON<PSStudyBrowserSearchResult[]>(searchUrl).then((rawData: PSStudyBrowserSearchResult[]) => {
            let result: PSStudyBrowserSearchResult[] = [];
            _.each(rawData, (item: PSStudyBrowserSearchResult) => {
                result.push({
                    patID: item['patID'],
                    patName: item['patName'],
                    patBirthDate: item['patBirthDate'],
                    patSex: item['patSex'],

                    stuInstID: item['stuInstID'],
                    accessionNum: item['accessionNum'],

                    stuID: item['stuID'],
                    stuDateTime: item['stuDateTime'],
                    modality: item['modality'],
                    refPhysician: item['refPhysician'],
                    stuDesc: item['stuDesc'],
                    deptName: item['deptName'],

                    numSeries: item['numSeries'],
                    numImages: item['numImages'],
                    stuSizeKB: item['stuSizeKB']
                });
            });
            return result;
        });
        
    }

    streamSeriesImagesAsync(seriesInstanceUID: string, streamCallback: (chunk: Uint8Array) => void): SyncTasks.Promise<void> {
        const url = 'api/DICOM/SeriesImages?seriesInstanceUID=' + encodeURIComponent(seriesInstanceUID);
        return this._streamImagesAsync(url, streamCallback);
    }

    private _streamImagesAsync(url: string, streamCallback: (chunk: Uint8Array) => void): SyncTasks.Promise<void> {
        if (window.fetch) {
            return SyncTasks.fromThenable(window.fetch(url,
                {
                    credentials: 'same-origin'
                }).then(response => {
                    let reader = (<any>response).body.getReader();
                    function drain() {
                        return reader.read().then((result: { value: Uint8Array, done: boolean }) => {
                            if (!result.done) {
                                streamCallback(result.value);
                                return drain();
                            }
                        });
                    }
                    return drain();
                }));
        } else {
            // Fall back to sketchy xhr custom content type "streaming" which seems plenty fast, but eats up a ton of memory with
            // the responseText processing.  Will likely need to break this up into multiple chunks for larger requests.
            return this._ajaxClient.streamArrayBuffer(url, streamCallback);
        }
    }

    getSeriesImageListAsync(seriesInstanceUID: string): SyncTasks.Promise<ImageInfoResp[]> {
        return this._getImageListAsync('api/DICOM/SeriesImageList?seriesInstanceUID=' + encodeURIComponent(seriesInstanceUID));
    }

    private _getImageListAsync(url: string): SyncTasks.Promise<ImageInfoResp[]> {
        return this._ajaxClient.getJSON<ImageInfoResp[]>(url)
            .then((response: ImageInfoResp[]) => {
                var result: ImageInfoResp[] = [];
                _.each(response, (item: ImageInfoResp) => {
                    result.push({
                        imageInstanceUID: item['imageInstanceUID'],
                        fileSizeKB: item['fileSizeKB']
                    });
                });
                return result;
            });
    }

    getImagesAsync(imageInstanceUIDs: string[], progressCallback?: (totalBytesDownloaded: number) => void): SyncTasks.Promise<ArrayBuffer> {
        return this._ajaxClient.postJSONGetArrayBuffer('api/DICOM/Images', { 'imageInstanceUIDs': imageInstanceUIDs }, progressCallback);
    }

    loginAsync(username: string, password: string): SyncTasks.Promise<LoginResult> {
        return this._ajaxClient.postJSON<LoginResult>('api/Auth/Login', { 'username': username, 'password': password })
            .then((result: LoginResult) => {
                var ret: LoginResult = {
                    success: result['success'],
                    errorMessage: result['errorMessage']
                };
                if (result['userInfo']) {
                    ret.userInfo = {
                        username: result['userInfo']['username'],
                        realname: result['userInfo']['realname'],
                        access: result['userInfo']['access']
                    };
                }

                return ret;
            });
    }
    changePassword(newPassword: string): SyncTasks.Promise<void> {
        return this._ajaxClient.putJSON<void>('api/Auth/Password', { 'newPassword': newPassword });
    }
    logoffAsync(): SyncTasks.Promise<void> {
        return this._ajaxClient.getJSON<void>('api/Auth/Logoff');
    }

    getPatientSnapshot(patId: string, maxResults: number): SyncTasks.Promise<PSStudySnapshotExtended[]> {
        return this._ajaxClient.getJSON<PSStudySnapshotExtended[]>(
            `api/Search/PatientSnapshot?patId=${encodeURIComponent(patId)}&maxResults=${maxResults}`)
            .then((response: PSStudySnapshotExtended[]) => {
                var result: PSStudySnapshotExtended[] = [];
                _.each(response, (item: PSStudySnapshotExtended) => {
                    var seriesData: PSSeriesSnapshot[] = [];

                    _.each(item['series'], (seriesItem: PSSeriesSnapshot) => {
                        seriesData.push({
                            serInstID: seriesItem['serInstID'],
                            numImages: seriesItem['numImages'],
                            serDateTime: seriesItem['serDateTime'],
                            serNum: seriesItem['serNum'],
                            modality: seriesItem['modality'],
                            serDesc: seriesItem['serDesc'],
                            bodyPart: seriesItem['bodyPart']
                        });
                    });

                    var item: PSStudySnapshotExtended = {
                        accessionNum: item['accessionNum'],
                        modality: item['modality'],
                        stuDateTime: item['stuDateTime'],
                        stuDesc: item['stuDesc'],
                        stuInstID: item['stuInstID'],
                        series: seriesData
                    };
                    result.push(item);
                });
                return result;
            });
    }

    getServerSettings(): SyncTasks.Promise<ServerSettingsResult> {
        return this._ajaxClient.getJSON<ServerSettingsResult>(`api/Settings/ServerSettings`).then(settings => minifyServerSettingsResult(settings));
    }

    saveDicomServerSettings(newSettings: DicomServerSettings): SyncTasks.Promise<void> {
        return this._ajaxClient.postJSON<void>(`api/Settings/DicomServerSettings`, unminifyDicomServerSettings(newSettings));
    }

    getEntities(): SyncTasks.Promise<PSEntity[]> {
        return this._ajaxClient.getJSON<PSEntity[]>(`api/Settings/Entities`).then(entities => entities.map(entity => minifyPSEntity(entity)));
    }
    saveEntities(newList: PSEntity[]): SyncTasks.Promise<void> {
        return this._ajaxClient.postJSON<void>(`api/Settings/Entities`, { 'list': newList.map(ae => unminifyPSEntity(ae)) });
    }

    insertUser(user: PSUser): SyncTasks.Promise<void> {
        return this._ajaxClient.postJSON<void>(`api/Settings/Users`, unminifyPSUser(user));
    }
    updateUser(username: string, user: PSUser): SyncTasks.Promise<void> {
        return this._ajaxClient.putJSON<void>(`api/Settings/Users?username=${username}`, unminifyPSUser(user));
    }
    deleteUser(username: string): SyncTasks.Promise<void> {
        return this._ajaxClient.deleteAction(`api/Settings/Users?username=${username}`);
    }

    sendStudies(studyInstanceUIDs: string[], targetAE: string) {
        const model: SendStudiesModel = {
            studyInstanceUIDs: studyInstanceUIDs,
            targetAE: targetAE
        };
        return this._ajaxClient.postJSON<void>(`api/Task/SendStudies`, unminifySendStudiesModel(model));
    }
    deleteStudies(studyInstanceUIDs: string[]) {
        const model: DeleteStudiesModel = {
            studyInstanceUIDs: studyInstanceUIDs
        };
        return this._ajaxClient.postJSON<void>(`api/Task/DeleteStudies`, unminifyDeleteStudiesModel(model));
    }
    importPath(path: string) {
        return this._ajaxClient.getJSON<void>('api/Task/ImportPath?path=' + encodeURIComponent(path));
    }
}

export default new PSApiClientImpl();
