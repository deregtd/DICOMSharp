﻿import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Options } from 'resub';
import * as SyncTasks from 'synctasks';

import AuthStore from './Stores/AuthStore';
import DebugUtils from './Utils/DebugUtils';
import LoginBox from './Components/LoginBox';
import ModalPopupStore from './Stores/ModalPopupStore';
import SearchPane from './Components/SearchPane';
import ViewerInterface from './Components/ViewerInterface';

// Force webpack to build LESS files.
// require('../less/Global.less');

declare const releaseBuild: boolean;

class WebViewerApp {
    constructor() {
        // Make sure that the browser is pointing at a full directory trailing in a slash, otherwise routing to images/apis might
        // get messed up.
        if (document.location.pathname.lastIndexOf('/') !== document.location.pathname.length - 1) {
            document.location.href = document.location.origin + document.location.pathname + '/' + document.location.search;
            return;
        }

        let queryString = document.location.search;
        if (queryString.indexOf('?') === 0) {
            queryString = queryString.substr(1);
        }
        let urlParams: {[key:string]:string } = {};
        queryString.split('&').forEach(item => {
            const spl = item.split('=');
            urlParams[spl[0]] = spl[1];
        });

        if (urlParams['debug'] === '1') {
            DebugUtils.DebugModeEnabled = true;
        }

        SyncTasks.config.catchExceptions = releaseBuild ? true : false;
        Options.development = !releaseBuild;

        // Fix iOS homescreen app behavior to trap touchmoves from anywhere other than modal popups
        document.addEventListener('touchmove', e => {
            let target = e.target as HTMLElement;
            while (target) {
                if (target.className === 'modalPopup-content' || target.className === 'modalPopup-fullscreencontent') {
                    return;
                }
                target = target.parentElement;
            }
            e.preventDefault();
        });
        
        let container = document.createElement('div');
        container.className = 'WebViewerApp';
        document.body.appendChild(container);
        ReactDOM.render(
            <ViewerInterface />,
            container
        );

        if (AuthStore.getUser()) {
            // Logged in!
            SearchPane.showPopup();
        } else {
            // Show the login box...
            ModalPopupStore.pushModal(<LoginBox />, false);
        }
    }
}

// Create Viewer when page is loaded
const start = () => {
    new WebViewerApp();
    document.removeEventListener('DOMContentLoaded', start);
};
document.addEventListener('DOMContentLoaded', start);