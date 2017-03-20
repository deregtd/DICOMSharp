import _ = require('lodash');
import React = require('react');
import { ComponentBase } from 'resub';
import SyncTasks = require('synctasks');

import ModalPopupStore = require('../Stores/ModalPopupStore');
import PSApiClient = require('../Utils/PSApiClient');

// Force webpack to build LESS files.
require('../../less/SelectEntityPanel.less');

interface SelectEntityPanelProps extends React.Props<any> {
    onSelect: (entity: PSEntity) => void;
    requiredFlags: PSEntityFlagsMask;
}

interface SelectEntityPanelState {
    loading?: boolean;
    entities?: PSEntity[];
}

class SelectEntityPanel extends ComponentBase<SelectEntityPanelProps, SelectEntityPanelState> {
    static selectEntity(requiredFlags = PSEntityFlagsMask.None): SyncTasks.Promise<PSEntity> {
        const deferred = SyncTasks.Defer<PSEntity>();
        ModalPopupStore.pushModal(<SelectEntityPanel requiredFlags={ requiredFlags } onSelect={ (entity) => deferred.resolve(entity) } />, false, false);
        return deferred.promise();
    }

    protected /* virtual */ _buildState(props: SelectEntityPanelProps, initialBuild: boolean): SelectEntityPanelState {
        let newState: SelectEntityPanelState = {};
        if (initialBuild) {
            newState.loading = true;
            PSApiClient.getEntities().then(entities => {
                this.setState({
                    loading: false,
                    entities: entities
                });
            });
        }
        return newState;
    }

    render() {
        const entities = this.state.loading ? 'Loading' : this.state.entities.filter(entity => (entity.flags & this.props.requiredFlags) === this.props.requiredFlags).map(entity =>
            <div key={ entity.title } className="SelectEntityPanel-command" onClick={ this._pickEntity.bind(this, entity) }>{ entity.comment ? entity.comment + ' (' + entity.title + ')' : entity.title }</div>); 

        return (
            <div className="SelectEntityPanel">
                <div className="SelectEntityPanel-head">Select an Entity</div>

                <div className="SelectEntityPanel-entities">
                    { entities }
                </div>

                <div className="SelectEntityPanel-command" onClick={ this._pickEntity.bind(this, undefined) }>Cancel</div>
            </div>
        );
    }

    private _pickEntity = (entity: PSEntity|undefined, e: React.TouchEvent<HTMLDivElement> | React.MouseEvent<HTMLDivElement>) => {
        ModalPopupStore.popModal();

        this.props.onSelect(entity);
    };
}

export = SelectEntityPanel;
