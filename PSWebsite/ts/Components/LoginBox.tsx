import md5 from 'blueimp-md5';
import * as React from 'react';
import { ComponentBase } from 'resub';

import AuthStore from '../Stores/AuthStore';
import ModalPopupStore from '../Stores/ModalPopupStore';
import PSApiClient from '../Utils/PSApiClient';
import SearchPane from './SearchPane';

// Force webpack to build LESS files.
require('../../less/LoginBox.less');

interface LoginBoxState {
    usernameInput?: string;
    passwordInput?: string;
    errorMessage?: string;
}

export default class LoginBox extends ComponentBase<{}, LoginBoxState> {
    protected _buildState(props: {}, initialBuild: boolean): LoginBoxState {
        if (initialBuild) {
            return {
                usernameInput: '',
                passwordInput: ''
            };
        }
    }

    render() {
        return (
            <div className="LoginBox">
                <div className="LoginBox-head">Login to MyFreePACS</div>

                <div className="LoginBox-fieldTitle">Username</div>
                <div className="LoginBox-field"><input ref="username" className="LoginBox-fieldInput" type="text" maxLength={32} value={ this.state.usernameInput } onInput={ this._onUsernameInput.bind(this) } onKeyPress={ this._onKeyPress.bind(this) } /></div>

                <div className="LoginBox-fieldTitle">Password</div>
                <div className="LoginBox-field"><input className="LoginBox-fieldInput" type="password" value={ this.state.passwordInput } onInput={ this._onPasswordInput.bind(this) } onKeyPress={ this._onKeyPress.bind(this) } /></div>

                <div className="LoginBox-error">{ this.state.errorMessage }</div>

                <div className="LoginBox-buttons">
                    <input className="LoginBox-button" type="button" value="Login" onClick={ this._login.bind(this) } />
                    <input className="LoginBox-button" type="button" value="Clear" onClick={ this._clear.bind(this) } />
                </div>
            </div>
        );
    }

    protected _componentDidRender() {
        let usernameBox = this.refs['username'] as HTMLInputElement;
        if (!usernameBox.value) {
            usernameBox.focus();
        }
    }

    private _clear() {
        this.setState(this._buildState(undefined, true));

        let usernameBox = this.refs['username'] as HTMLInputElement;
        usernameBox.focus();
    }

    private _login() {
        const md5Pass = md5(this.state.passwordInput).toUpperCase();

        PSApiClient.loginAsync(this.state.usernameInput, md5Pass).done(loginResult => {
            if (loginResult.success) {
                AuthStore.loggedIn(loginResult.userInfo);
                ModalPopupStore.popModal();

                // Go to the search box now like a normal login!
                SearchPane.showPopup();
            } else {
                this.setState({
                    errorMessage: loginResult.errorMessage
                });
            }
        });
    }

    private _onUsernameInput(event: React.FormEvent<HTMLInputElement>) {
        var newState: LoginBoxState = { usernameInput: event.currentTarget.value };
        this.setState(newState);
    }

    private _onPasswordInput(event: React.FormEvent<HTMLInputElement>) {
        var newState: LoginBoxState = { passwordInput: event.currentTarget.value };
        this.setState(newState);
    }

    private _onKeyPress(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.charCode === 13) {
            // Enter key
            this._login();
        }
    }
}
