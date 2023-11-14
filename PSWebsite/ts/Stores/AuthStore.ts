import { StoreBase, AutoSubscribeStore, autoSubscribe } from 'resub';

import PSApiClient from '../Utils/PSApiClient';

declare var givenUser: UserInfo;

@AutoSubscribeStore
class AuthStoreImpl extends StoreBase {
    private _user: UserInfo = null;

    constructor() {
        super();

        // Load any given user
        if (typeof givenUser !== 'undefined') {
            this._user = {
                username: givenUser['username'],
                realname: givenUser['realname'],
                access: givenUser['access']
            };
        }
    }

    @autoSubscribe
    getUser(): UserInfo {
        return this._user;
    }

    loggedIn(user: UserInfo) {
        this._user = user;
        this.trigger();
    }

    logoff() {
        PSApiClient.logoffAsync().then(() => {
            // Refresh the page just to re-kick the process
            document.location.reload();
        });
    }
}

export default new AuthStoreImpl();
