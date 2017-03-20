import React = require('react');
import { ComponentBase } from 'resub';

import ImageDownloadStore = require('../Stores/ImageDownloadStore');

// Force webpack to build LESS files.
require('../../less/DownloadProgress.less');

interface DownloadProgressState {
    downloads?: StudyDownloadInfo[];
}

class DownloadProgress extends ComponentBase<{}, DownloadProgressState> {
    protected /* virtual */ _buildState(props: {}, initialBuild: boolean): DownloadProgressState {
        return {
            downloads: ImageDownloadStore.getDownloads()
        };
    }

    render(): JSX.Element {
        if (this.state.downloads.length === 0) {
            return null;
        }

        return <div className='DownloadProgress'>
                { this.state.downloads.map((download, index) => {
                    let styles: React.CSSProperties = { width: (100 * (download.downloadedImages + download.partialImage) / download.totalImages) + '%' };
                    return <div key={ 'download_' + index } className='DownloadProgress-row' style={ styles }></div>;
                }) }
            </div>;
    }
}

export = DownloadProgress;
