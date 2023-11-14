import * as SyncTasks from 'synctasks';

interface AjaxClientOptions {
    responseType?: XMLHttpRequestResponseType;
    streamCallback?: (chunk: Uint8Array) => void;
    bytesDownloadedCallback?: (bytesDownloaded: number) => void;
    overrideMimeType?: string;
}

export default class AjaxClient {
    constructor(private _baseUrl = '') {
    }

    public getJSON<T>(path: string): SyncTasks.Promise<T> {
        return this._internalPerform<T>('GET', this._baseUrl + path, null, { responseType: 'json' });
    }
    public getArrayBuffer(path: string): SyncTasks.Promise<ArrayBuffer> {
        return this._internalPerform<ArrayBuffer>('GET', this._baseUrl + path, null, { responseType: 'arraybuffer' });
    }
    public streamArrayBuffer(path: string, streamCallback: (chunk: Uint8Array) => void): SyncTasks.Promise<void> {
        return this._internalPerform<void>('GET', this._baseUrl + path, null,
            {
                streamCallback: streamCallback,
                overrideMimeType: 'text/plain; charset=x-user-defined'
            });
    }

    public postJSON<T>(path: string, postObj?: any): SyncTasks.Promise<T> {
        return this._internalPerform<T>('POST', this._baseUrl + path, postObj, { responseType: 'json' });
    }

    public postJSONGetArrayBuffer<T>(path: string, postObj?: any, progressCallback?: (bytesDownloaded: number) => void)
            : SyncTasks.Promise<ArrayBuffer> {
        return this._internalPerform<ArrayBuffer>('POST', this._baseUrl + path, postObj,
            { responseType: 'arraybuffer', bytesDownloadedCallback: progressCallback });
    }

    public putJSON<T>(path: string, putObj?: any): SyncTasks.Promise<T> {
        return this._internalPerform<T>('PUT', this._baseUrl + path, putObj, { responseType: 'json' });
    }

    public deleteAction<T>(path: string): SyncTasks.Promise<void> {
        return this._internalPerform<void>('DELETE', this._baseUrl + path);
    }

    private _internalPerform<T>(method: string, path: string, postObj?: any, options?: AjaxClientOptions): SyncTasks.Promise<T> {
        var deferred = SyncTasks.Defer<T>();
        var xhr = new XMLHttpRequest();
        xhr.open(method, this._baseUrl + path, true);

        if (options) {
            if (options.responseType) {
                xhr.responseType = options.responseType;
            }
            if (options.overrideMimeType) {
                xhr.overrideMimeType(options.overrideMimeType);
            }

            if (options.streamCallback) {
                let offset = 0;
                xhr.onreadystatechange = (e) => {
                    if (xhr.readyState >= 3) {
                        const chunk = xhr.responseText.substr(offset);
                        const len = chunk.length;
                        if (len > 0) {
                            offset += len;

                            let ab = new ArrayBuffer(len);
                            let ui8arr = new Uint8Array(ab);
                            for (let i = 0; i < len; i++) {
                                ui8arr[i] = chunk.charCodeAt(i) & 0xff;
                            }
                            options.streamCallback(ui8arr);
                        }
                    }
                };
            }

            if (options.bytesDownloadedCallback) {
                xhr.onprogress = (e) => {
                    options.bytesDownloadedCallback(e.loaded);
                };
            }
        }

        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status <= 299) {
                if (options && options.responseType === 'json' && xhr.responseType !== options.responseType) {
                    // IE10 hack
                    let obj: T = null;
                    try {
                        obj = JSON.parse(xhr.response) as T;
                    } catch (e) {
                        // oh well.
                    }
                    deferred.resolve(obj);
                } else {
                    deferred.resolve(xhr.response as T);
                }
            } else {
                deferred.reject(xhr.response);
            }
        };
        xhr.onabort = xhr.onerror = (e) => {
            deferred.reject(e.toString());
        };

        if (postObj) {
            xhr.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
            xhr.send(JSON.stringify(postObj));
        } else {
            xhr.send();
        }

        return deferred.promise();
    }
}
