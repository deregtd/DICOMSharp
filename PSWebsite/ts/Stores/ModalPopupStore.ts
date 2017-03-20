import _ = require('lodash');
import { StoreBase, AutoSubscribeStore, autoSubscribe } from 'resub';

@AutoSubscribeStore
class ModalPopupStoreImpl extends StoreBase {
    private _modalStack: ModalInfo[] = [];

    @autoSubscribe
    getTopModalContents(): ModalInfo {
        return _.last(this._modalStack);
    }

    popModal() {
        this._modalStack.pop();
        this.trigger();
    }

    pushModal(modalContent: JSX.Element, canClickOut: boolean = true, fullScreenOnResponsive: boolean = false) {
        this._modalStack.push({ element: modalContent, canClickOut: canClickOut, fullScreenOnResponsive: fullScreenOnResponsive });
        this.trigger();
    }
}

export = new ModalPopupStoreImpl();
