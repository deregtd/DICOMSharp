import md5 from 'blueimp-md5';
import * as _ from 'lodash';
import * as React from 'react';
import { ComponentBase } from 'resub';

import AuthStore from '../Stores/AuthStore';
import ModalPopupStore from '../Stores/ModalPopupStore';
import PSApiClient from '../Utils/PSApiClient';
import SearchPane from '../Components/SearchPane';
import ServerSettingsPanel from './ServerSettingsPanel';

// Force webpack to build LESS files.
require('../../less/UserSettingsPanel.less');

interface UserSettingsPanelProps extends React.PropsWithChildren {
}

interface UserSettingsPanelState {
    userInfo?: UserInfo;
}

export default class UserSettingsPanel extends ComponentBase<UserSettingsPanelProps, UserSettingsPanelState> {
    static showPopup() {
        ModalPopupStore.pushModal(<UserSettingsPanel />, true, true);
    }

    protected /* virtual */ _buildState(props: {}, initialBuild: boolean): UserSettingsPanelState {
        return {
            userInfo: AuthStore.getUser()
        };
    }

    render() {
        const userInfo = this.state.userInfo.realname + ' (' + this.state.userInfo.username + ')';
        const permissionsString = _.compact([
            this.state.userInfo.access & UserAccessFlags.Reader ? 'Reader' : undefined,
            this.state.userInfo.access & UserAccessFlags.ServerAdmin ? 'Server Administrator' : undefined,
            this.state.userInfo.access & UserAccessFlags.StudySend ? 'Study Sender' : undefined,
            this.state.userInfo.access & UserAccessFlags.StudyDelete ? 'Study Deleter' : undefined
        ]).join(', ');

        const serverAdmin = this.state.userInfo.access & UserAccessFlags.ServerAdmin ?
            <div className="UserSettingsPanel-command" onClick={ this._openServerSettings.bind(this) }>Server Settings</div> :
            undefined;

        const studyManagement = (this.state.userInfo.access & (UserAccessFlags.StudySend | UserAccessFlags.StudyDelete)) > 0 ?
            <div className="UserSettingsPanel-command" onClick={ this._openStudyManagement.bind(this) }>Study Management</div> :
            undefined;

        return (
            <div className="UserSettingsPanel">
                <div className="UserSettingsPanel-head">Settings</div>

                <div className="UserSettingsPanel-userInfo">{ userInfo }</div>
                <div className="UserSettingsPanel-access">Access: { permissionsString }</div>
                { studyManagement }
                { serverAdmin }
                <div className="UserSettingsPanel-command" onClick={ this._changePassword.bind(this) }>Change Password</div>
                <div className="UserSettingsPanel-command" onClick={ this._logoff.bind(this) }>Logoff</div>
            </div>
        );
    }

    private _openServerSettings(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        ModalPopupStore.popModal();

        ServerSettingsPanel.showPopup();
    }

    private _openStudyManagement(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        ModalPopupStore.popModal();

        SearchPane.showPopup(true);
    }

    private _changePassword(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        const pass1 = prompt('Enter Password');
        if (!pass1) {
            alert('Password cannot be blank');
            return;
        }

        const pass2 = prompt('Confirm Password');
        if (pass1 !== pass2) {
            alert('Passwords did not match, try again');
            return;
        }

        const newPassMd5 = md5(pass1).toUpperCase();
        PSApiClient.changePassword(newPassMd5).then(() => {
            alert('Password Changed');
        }, () => {
            alert('Error Changing Password');
        });
    }

    private _logoff(e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) {
        // Detect right-clicks. React doesn't know about these so we need
        // to use the native event and cast to any.
        const nativeEvent: any = e.nativeEvent;
        if (nativeEvent.which === 3) {
            return;
        }

        e.preventDefault();

        AuthStore.logoff();
    }
}
