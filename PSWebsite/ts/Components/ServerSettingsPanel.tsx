import md5 = require('blueimp-md5');
import _ = require('lodash');
import React = require('react');
import { ComponentBase } from 'resub';
import SyncTasks = require('synctasks');

import AuthStore = require('../Stores/AuthStore');
import ModalPopupStore = require('../Stores/ModalPopupStore');
import PSApiClient = require('../Utils/PSApiClient');

// Force webpack to build LESS files.
require('../../less/ServerSettingsPanel.less');

interface ServerSettingsPanelProps extends React.Props<any> {
}

interface ServerSettingsPanelState {
    isLoading?: boolean;
    isSaving?: boolean;

    serverSettings?: ServerSettingsResult;

    saveEnabled?: boolean;

    userInfo?: UserInfo;

    dicomServerSectionChanged?: boolean;
    entityListSectionChanged?: boolean;
    userListSectionChanged?: boolean;

    localDicomServerSettings?: DicomServerSettings;
    localEntityList?: PSEntity[];
    localUserList?: PSUserExtended[];
}

class ServerSettingsPanel extends ComponentBase<ServerSettingsPanelProps, ServerSettingsPanelState> {
    static showPopup() {
        ModalPopupStore.pushModal(<ServerSettingsPanel />, true, true);
    }

    protected /* virtual */ _buildState(props: ServerSettingsPanelProps, initialBuild: boolean): ServerSettingsPanelState {
        if (initialBuild) {
            this._fetchServerSettings();

            return {
                isLoading: true,
                isSaving: false,

                userInfo: AuthStore.getUser(),

                serverSettings: null,

                saveEnabled: false
            };
        }
    }

    private _fetchServerSettings() {
        PSApiClient.getServerSettings().then(settings => {
            this.setState(_.extend({
                isLoading: false,
                serverSettings: settings
            }, this._getSettingsReset(settings)));
        });
    }

    render() {
        let content: JSX.Element = null;
        if (this.state.isSaving) {
            content = <div className="ServerSettingsPanel-close">Saving Changes...</div>;
        } else if (this.state.isLoading) {
            content = <div className="ServerSettingsPanel-close">Loading Settings...</div>;
        } else {
            let entityRows: JSX.Element[] = [
                <div key="header" className="ServerSettingsPanel-content-section-contents-row">
                    <div className="ServerSettingsPanel-content-section-contents-row-ae-title">AE Title</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-ae-address">Address</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-ae-port">Port</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-ae-note">Note</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-ae-flag">Send Destination</div>
                </div>
            ];
            this.state.localEntityList.forEach((ae, index) => {
                entityRows.push(<div key={ 'ent_' + index.toString() } className="ServerSettingsPanel-content-section-contents-row">
                        <div className="ServerSettingsPanel-content-section-contents-row-ae-title">
                            <input type="text" maxLength={16} onChange={ this._changed_entity_aeTitle.bind(this, index) } value={ ae.title } style={ { width: '100%' } }/>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-ae-address">
                            <input type="text" maxLength={64} onChange={ this._changed_entity_address.bind(this, index) } value={ ae.address } style={ { width: '100%' } } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-ae-port">
                            <input type="number" maxLength={5} onChange={ this._changed_entity_port.bind(this, index) } value={ ae.port.toString() } style={ { width: '100%' } } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-ae-note">
                            <input type="text" maxLength={64} onChange={ this._changed_entity_note.bind(this, index) } value={ ae.comment } style={ { width: '100%' } } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-ae-flag">
                            <input type="checkbox" onChange={ this._changed_entity_flag_sendDestination.bind(this, index) } defaultChecked={ (ae.flags & PSEntityFlagsMask.SendDestination) > 0 } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-ae-del" onClick={ this._changed_entity_delete.bind(this, index) } >
                            Delete
                        </div>
                    </div>);
            });

            let userRows: JSX.Element[] = [
                <div key="header" className="ServerSettingsPanel-content-section-contents-row">
                    <div className="ServerSettingsPanel-content-section-contents-row-user-username">Username</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-user-realname">Real Name</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-user-access">Reader</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-user-access">Admin</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-user-access">Study<br />Send</div>
                    <div className="ServerSettingsPanel-content-section-contents-row-user-access">Study<br />Delete</div>
                </div>
            ];
            this.state.localUserList.forEach((user, index) => {
                if (!user) {
                    return;
                }
                userRows.push(<div key={ 'user_' + index.toString() } className="ServerSettingsPanel-content-section-contents-row">
                        <div className="ServerSettingsPanel-content-section-contents-row-user-username">
                            <input type="text" maxLength={16} onChange={ this._changed_user_username.bind(this, index) } value={ user.username } style={ { width: '100%' } }/>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-user-realname">
                            <input type="text" maxLength={32} onChange={ this._changed_user_realname.bind(this, index) } value={ user.realname } style={ { width: '100%' } } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-user-access">
                            <input type="checkbox" onChange={ this._changed_user_access.bind(this, index, UserAccessFlags.Reader) } checked={ !!(user.access & UserAccessFlags.Reader) } style = { { width: '100%' } } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-user-access">
                            { user.username === this.state.userInfo.username ? 'X' :
                            <input type="checkbox" onChange={ this._changed_user_access.bind(this, index, UserAccessFlags.ServerAdmin) } checked={ !!(user.access & UserAccessFlags.ServerAdmin) } style = { { width: '100%' } } /> }
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-user-access">
                            <input type="checkbox" onChange={ this._changed_user_access.bind(this, index, UserAccessFlags.StudySend) } checked={ !!(user.access & UserAccessFlags.StudySend) } style = { { width: '100%' } } />
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row-user-access">
                            <input type="checkbox" onChange={ this._changed_user_access.bind(this, index, UserAccessFlags.StudyDelete) } checked={ !!(user.access & UserAccessFlags.StudyDelete) } style = { { width: '100%' } } />
                        </div>
                        { user.username === this.state.userInfo.username ? '' :
                        <div className="ServerSettingsPanel-content-section-contents-row-user-del" onClick={ this._changed_user_delete.bind(this, index) } >
                            Delete
                        </div> }
                        <div className="ServerSettingsPanel-content-section-contents-row-user-resetpw" onClick={ this._changed_user_resetpass.bind(this, index) } >
                            Reset Pass
                        </div>
                    </div>);
            });

            content = <div className="ServerSettingsPanel-content">
                <div className="ServerSettingsPanel-content-section">
                    <div className="ServerSettingsPanel-content-section-head">DICOM Server</div>
                    <div className="ServerSettingsPanel-content-section-contents">
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Enabled</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="checkbox" onChange={ this._changed_listeningEnabled.bind(this) } checked={ this.state.localDicomServerSettings.listeningEnabled } />
                            </div>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">AE Title</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="text" maxLength={16} onChange={ this._changed_aeTitle.bind(this) } value={ this.state.localDicomServerSettings.aeTitle } />
                            </div>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Port</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="number" maxLength={5} onChange={ this._changed_listenPort.bind(this) } value={ this.state.localDicomServerSettings.listenPort.toString() } />
                            </div>
                        </div>

                        <div className="ServerSettingsPanel-content-section-contents-divider" />

                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Image Storage Path</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="text" onChange={ this._changed_imageStoragePath.bind(this) } value={ this.state.localDicomServerSettings.imageStoragePath } />
                            </div>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Storage Hard Limit (MB)</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="number" onChange={ this._changed_imageStorageSizeMB.bind(this) } value={ this.state.localDicomServerSettings.imageStorageSizeMB.toString() } />
                            </div>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Data Stored (MB) </div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">{ Math.ceil(this.state.serverSettings.storedImagesKB / 1024) }</div>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Decompress Before Storage</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="checkbox" onChange={ this._changed_autoDecompress.bind(this) } checked={ this.state.localDicomServerSettings.autoDecompress } />
                            </div>
                        </div>
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Store Metadata-Only Files</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="checkbox" onChange={ this._changed_storeMetadataOnlyFiles.bind(this) } checked={ this.state.localDicomServerSettings.storeMetadataOnlyFiles } />
                            </div>
                        </div>

                        <div className="ServerSettingsPanel-content-section-contents-divider" />

                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Verbose Logging</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="checkbox" onChange={ this._changed_verboseLogging.bind(this) } checked={ this.state.localDicomServerSettings.verboseLogging } />
                            </div>
                        </div>
                    </div>
                </div>

                <div className="ServerSettingsPanel-content-section">
                    <div className="ServerSettingsPanel-content-section-head">Entity List</div>
                    <div className="ServerSettingsPanel-content-section-contents">
                        <div className="ServerSettingsPanel-content-section-contents-row">
                            <div className="ServerSettingsPanel-content-section-contents-row-left">Promiscuous Mode</div>
                            <div className="ServerSettingsPanel-content-section-contents-row-right">
                                <input type="checkbox" onChange={ this._changed_promiscuousMode.bind(this) } checked={ this.state.localDicomServerSettings.promiscuousMode } />
                            </div>
                        </div>

                        { this.state.localDicomServerSettings.promiscuousMode ? null : <div className="ServerSettingsPanel-content-section-contents-divider" /> }

                        { this.state.localDicomServerSettings.promiscuousMode ? null : entityRows }

                        { this.state.localDicomServerSettings.promiscuousMode ? null : <div className="ServerSettingsPanel-content-section-contents-addnew" onClick={ this._addNewEntity.bind(this) }>Add New</div> }
                    </div>
                </div>

                <div className="ServerSettingsPanel-content-section">
                    <div className="ServerSettingsPanel-content-section-head">User List</div>
                    <div className="ServerSettingsPanel-content-section-contents">
                        { userRows }

                        <div className="ServerSettingsPanel-content-section-contents-addnew" onClick={ this._addNewUser.bind(this) }>Add New</div>
                    </div>
                </div>
                
                <div className="ServerSettingsPanel-content-buttons">
                    <input type="button" value="Reset" onClick={ this._resetSettings.bind(this) } disabled={ !this.state.saveEnabled } />
                    <input type="button" value="Save" onClick={ this._saveSettings.bind(this) } disabled={ !this.state.saveEnabled } />
                </div>
            </div>;
        }

        return (
            <div className="ServerSettingsPanel">
                <div className="ServerSettingsPanel-head">Server Settings</div>

                { content }

                <div className="ServerSettingsPanel-close" onClick={ () => { ModalPopupStore.popModal(); } }>Close</div>
            </div>
        );
    }

    // Dicom server settings
    private _changed_listeningEnabled(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.listeningEnabled = e.currentTarget.checked;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_aeTitle(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.aeTitle = e.currentTarget.value;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_listenPort(e: React.FormEvent<HTMLInputElement>) {
        if (isNaN(Number(e.currentTarget.value))) { return; }
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.listenPort = Number(e.currentTarget.value);
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_imageStoragePath(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.imageStoragePath = e.currentTarget.value;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_imageStorageSizeMB(e: React.FormEvent<HTMLInputElement>) {
        if (isNaN(Number(e.currentTarget.value))) { return; }
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.imageStorageSizeMB = Number(e.currentTarget.value);
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_autoDecompress(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.autoDecompress = e.currentTarget.checked;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_storeMetadataOnlyFiles(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.storeMetadataOnlyFiles = e.currentTarget.checked;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_verboseLogging(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.verboseLogging = e.currentTarget.checked;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }
    private _changed_promiscuousMode(e: React.FormEvent<HTMLInputElement>) {
        let newState = _.cloneDeep(this.state.localDicomServerSettings);
        newState.promiscuousMode = e.currentTarget.checked;
        this.setState(_.extend({ localDicomServerSettings: newState }, this._getNewSaveButton(newState, this.state.localEntityList, this.state.localUserList)));
    }

    // Entities
    private _changed_entity_aeTitle(index: number, e: React.FormEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localEntityList);
        newList[index].title = e.currentTarget.value;
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }
    private _changed_entity_address(index: number, e: React.FormEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localEntityList);
        newList[index].address = e.currentTarget.value;
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }
    private _changed_entity_port(index: number, e: React.FormEvent<HTMLInputElement>) {
        if (isNaN(Number(e.currentTarget.value))) { return; }
        let newList = _.cloneDeep(this.state.localEntityList);
        newList[index].port = Number(e.currentTarget.value);
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }
    private _changed_entity_note(index: number, e: React.FormEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localEntityList);
        newList[index].comment = e.currentTarget.value;
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }
    private _changed_entity_flag_sendDestination(index: number, e: React.MouseEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localEntityList);
        newList[index].flags = (newList[index].flags & ~PSEntityFlagsMask.SendDestination) | (e.currentTarget.checked ? PSEntityFlagsMask.SendDestination : 0);
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }
    private _changed_entity_delete(index: number, e: React.MouseEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localEntityList);
        newList.splice(index, 1);
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }
    private _addNewEntity() {
        let newList = _.cloneDeep(this.state.localEntityList);
        newList.push({
            title: 'NEW',
            address: 'address',
            port: 4006,
            comment: '',
            flags: PSEntityFlagsMask.None
        });
        this.setState(_.extend({ localEntityList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, newList, this.state.localUserList)));
    }

    // Users
    private _changed_user_username(index: number, e: React.FormEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localUserList);
        if (newList[index].oldUsername) {
            if (newList[index].oldUsername === e.currentTarget.value) {
                // They're resetting it back again, so drop the change track
                delete newList[index].oldUsername;
            }
        } else {
            // Track the original username
            newList[index].oldUsername = newList[index].username;
        }
        newList[index].username = e.currentTarget.value;
        this.setState(_.extend({ localUserList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, this.state.localEntityList, newList)));
    }
    private _changed_user_realname(index: number, e: React.FormEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localUserList);
        newList[index].realname = e.currentTarget.value;
        this.setState(_.extend({ localUserList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, this.state.localEntityList, newList)));
    }
    private _changed_user_access(index: number, accessFlag: UserAccessFlags, e: React.FormEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localUserList);
        newList[index].access = newList[index].access ^ accessFlag;
        this.setState(_.extend({ localUserList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, this.state.localEntityList, newList)));
    }
    private _changed_user_delete(index: number, e: React.MouseEvent<HTMLInputElement>) {
        let newList = _.cloneDeep(this.state.localUserList);
        newList[index] = null;  // Just mark it as deleted so we can calc the delta later
        this.setState(_.extend({ localUserList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, this.state.localEntityList, newList)));
    }
    private _changed_user_resetpass(index: number, e: React.MouseEvent<HTMLInputElement>) {
        var newPass = prompt('Enter Password for new User:');
        if (!newPass) {
            return;
        }

        let updatedUser = _.cloneDeep(this.state.localUserList[index]);
        updatedUser.password = md5(newPass).toUpperCase();
        PSApiClient.updateUser(updatedUser.username, updatedUser);
    }
    private _addNewUser() {
        var newPass = prompt('Enter Password for new User:');
        if (!newPass) {
            return;
        }

        let newList = _.cloneDeep(this.state.localUserList);
        newList.push({
            username: 'newuser',
            realname: 'New User',
            password: md5(newPass).toUpperCase(),
            access: UserAccessFlags.Reader
        });
        this.setState(_.extend({ localUserList: newList }, this._getNewSaveButton(this.state.localDicomServerSettings, this.state.localEntityList, newList)));
    }

    private _resetSettings(e: React.MouseEvent<HTMLDivElement>) {
        if (this.state.isSaving || this.state.isLoading) {
            return;
        }

        this.setState(this._getSettingsReset(this.state.serverSettings));
    }

    private _saveSettings(e: React.MouseEvent<HTMLDivElement>) {
        if (this.state.isSaving || this.state.isLoading) {
            return;
        }

        // Quick validation checks...

        let usernameCheck = {};
        for (let i = 0; i < this.state.localUserList.length; i++) {
            if (!this.state.localUserList[i]) {
                continue;
            }
            if (usernameCheck[this.state.localUserList[i].username]) {
                alert('Error: Username "' + this.state.localUserList[i].username + '" exists twice in the user list.  Usernames must be unique.');
                return;
            }
            usernameCheck[this.state.localUserList[i].username] = true;
        }

        let promises: SyncTasks.Promise<void>[] = [];
        if (this.state.dicomServerSectionChanged) {
            promises.push(PSApiClient.saveDicomServerSettings(this.state.localDicomServerSettings));
        }
        if (this.state.entityListSectionChanged) {
            promises.push(PSApiClient.saveEntities(this.state.localEntityList));
        }
        if (this.state.userListSectionChanged) {
            for (let i = 0; i < this.state.localUserList.length; i++) {
                if (i >= this.state.serverSettings.users.length) {
                    // Into new user territory
                    if (this.state.localUserList[i]) {
                        promises.push(PSApiClient.insertUser(this.state.localUserList[i]));
                    } else {
                        // Added and deleted, just ignore
                    }
                } else {
                    // Changing/deleting existing users
                    if (this.state.localUserList[i]) {
                        // Make sure something changed
                        if (this.state.localUserList[i].oldUsername || this.state.localUserList[i].realname !== this.state.serverSettings.users[i].realname ||
                            this.state.localUserList[i].access !== this.state.serverSettings.users[i].access) {
                            promises.push(PSApiClient.updateUser(this.state.localUserList[i].oldUsername || this.state.localUserList[i].username, this.state.localUserList[i]));
                        }
                    } else {
                        promises.push(PSApiClient.deleteUser(this.state.serverSettings.users[i].username));
                    }
                }
            }
        }

        this.setState({
            isSaving: true
        });

        SyncTasks.all(promises).then(() => {
            this.setState({
                isSaving: false,
                isLoading: true
            });

            this._fetchServerSettings();
        });
    }

    private _getSettingsReset(settings: ServerSettingsResult): ServerSettingsPanelState {
        return {
            saveEnabled: false,

            dicomServerSectionChanged: false,
            entityListSectionChanged: false,
            userListSectionChanged: false,

            localDicomServerSettings: _.cloneDeep(settings.dicomServerSettings),
            localEntityList: _.cloneDeep(settings.dicomServerEntities),
            localUserList: _.cloneDeep(settings.users)
        };
    }

    private _getNewSaveButton(newDicomServerSettings: DicomServerSettings, newEntityList: PSEntity[], newUserList: PSUser[]) {
        let newButtonState: ServerSettingsPanelState = {
            saveEnabled: false,
            dicomServerSectionChanged: false,
            entityListSectionChanged: false,
            userListSectionChanged: false
        };

        if (!_.isEqual(newDicomServerSettings, this.state.serverSettings.dicomServerSettings)) {
            newButtonState.saveEnabled = true;
            newButtonState.dicomServerSectionChanged = true;
        }

        let newEntityListSectionChanged = false;
        if (!_.isEqual(newEntityList, this.state.serverSettings.dicomServerEntities)) {
            newButtonState.saveEnabled = true;
            newButtonState.entityListSectionChanged = true;
        }

        let newUserListSectionChanged = false;
        if (!_.isEqual(newUserList, this.state.serverSettings.users)) {
            newButtonState.saveEnabled = true;
            newButtonState.userListSectionChanged = true;
        }

        return newButtonState;
    }
}

export = ServerSettingsPanel;
