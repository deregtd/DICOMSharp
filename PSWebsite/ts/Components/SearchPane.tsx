import * as _ from 'lodash';
import * as React from 'react';
import { ComponentBase } from 'resub';

import PatientContextStore from '../Stores/PatientContextStore';
import ModalPopupStore from '../Stores/ModalPopupStore';
import PSApiClient from '../Utils/PSApiClient';
import * as PSUtils from '../Utils/PSUtils';
import SelectEntityPanel from '../Components/SelectEntityPanel';
import * as StringUtils from '../Utils/StringUtils';

// Force webpack to build LESS files.
require('../../less/SearchPane.less');

interface SearchPaneProps extends React.PropsWithChildren {
    adminMode?: boolean;
}

interface SearchPaneState {
    searchAccessionNum?: string;
    searchPatientId?: string;
    searchPatientName?: string;
    searchStartDate?: string;
    searchEndDate?: string;
    searchDescription?: string;
    searchResponse?: PSStudyBrowserSearchResult[];
    searchMaxResults?: number;
    
    checkedItems?: _.Dictionary<boolean>;
}

interface SearchData {
    startDate: string;
    endDate: string;
    description: string;
    patName: string;
    patId: string;
    accession: string;
}

export default class SearchPane extends ComponentBase<SearchPaneProps, SearchPaneState> {
    static showPopup(adminMode?: boolean) {
        ModalPopupStore.pushModal(<SearchPane adminMode={ adminMode } />, true, true);
    }

    protected _buildState(props: SearchPaneProps, initialBuild: boolean): SearchPaneState {
        if (initialBuild) {
            // Make it run a search on pop
            _.defer(() => { this._search(); });

            return {
                searchAccessionNum: '',
                searchPatientId: '',
                searchPatientName: '',
                searchStartDate: '',
                searchEndDate: '',
                searchDescription: '',
                searchResponse: null,
                searchMaxResults: null,

                checkedItems: {}
            };
        }
    }

    private _resetSearch = () => {
        this.setState(this._buildState(undefined, true));
    };
        
    private _search(maxResults: number = 50) {
        PSApiClient.searchAsync(this.state.searchPatientId, this.state.searchPatientName, this.state.searchStartDate,
            this.state.searchEndDate, this.state.searchAccessionNum, this.state.searchDescription, maxResults).then(data => {
            this.setState({ searchResponse: data, searchMaxResults: maxResults });
        });
    }

    private _onKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.charCode === 13) {
            // Enter key
            this._search();
        }
    };

    private _onCheckChange = (stuInstId: string, e: React.FormEvent<HTMLInputElement>) => {
        let newChecked = _.cloneDeep(this.state.checkedItems);
        if (newChecked[stuInstId]) {
            delete newChecked[stuInstId];
        } else {
            newChecked[stuInstId] = true;
        }
        this.setState({ checkedItems: newChecked });
    };

    private _cancelPropagate = (e: React.MouseEvent<HTMLTableDataCellElement>) => {
        e.stopPropagation();
        return false;
    };

    render(): JSX.Element {
        var results: JSX.Element[] = null;
        if (this.state.searchResponse) {
            results = this.state.searchResponse.map(result => {
                const resDate = new Date(result.stuDateTime);
                const dateShort = StringUtils.padZeroes(resDate.getMonth() + 1, 2) + '/' + StringUtils.padZeroes(resDate.getDate(), 2) +
                    '/' + StringUtils.padZeroes(resDate.getFullYear() % 100, 2);
                const time24 = StringUtils.padZeroes(resDate.getHours(), 2) + ':' + StringUtils.padZeroes(resDate.getMinutes(), 2) +
                    ':' + StringUtils.padZeroes(resDate.getSeconds(), 2);
                const downloader = this.props.adminMode ? (): void => undefined : () => {
                    ModalPopupStore.popModal();

                    PatientContextStore.setPatient(PSUtils.getPatientInfoFromSearchResult(result), result.stuInstID);
                };
                const checkbox = this.props.adminMode ? <td className="SearchTable-resultRowEntry" onClick={ this._cancelPropagate }>
                        <input className="SearchTable-checkbox" type="checkbox" onChange={ this._onCheckChange.bind(this, result.stuInstID) } />
                    </td> : undefined;
                return <tr key={ result.stuInstID } className="SearchTable-resultRow" onClick={ downloader }>
                    { checkbox }
                    <td className="SearchTable-resultRowEntry">{ dateShort + ' - ' + time24}</td>
                    <td className="SearchTable-resultRowEntry">{ result.accessionNum }</td>
                    <td className="SearchTable-resultRowEntry">{ result.patID }</td>
                    <td className="SearchTable-resultRowEntry">{ result.patName}</td>
                    <td className="SearchTable-resultRowEntry">{ result.modality + (result.stuDesc ? ' - ' + result.stuDesc : '')}</td>
                    <td className="SearchTable-resultRowEntry">{ result.numImages + ' Image' + (result.numImages === 1 ? '' : 's')}</td>
                </tr>
            });
        }

        let commandRow: JSX.Element = undefined;
        if (this.props.adminMode) {
            const anySelected = !_.isEmpty(this.state.checkedItems);
            commandRow = <div className="SearchPane_AdminCommands">
                    <div className={ anySelected ? 'SearchPage_AdminCommand' : 'SearchPage_AdminCommand_disabled' } onClick={ anySelected ? this._sendSelected : undefined }>Send</div>
                    <div className={ anySelected ? 'SearchPage_AdminCommand' : 'SearchPage_AdminCommand_disabled' } onClick={ anySelected ? this._deleteSelected : undefined }>Delete</div>
                </div>;
        }

        return <div className="SearchPane">
            { commandRow }
            <table className="SearchTable" cellSpacing="0" cellPadding="0" frame={false} border={0}>
                <thead>
                    <tr>
                        { this.props.adminMode ? <td className="SearchTable-head">&nbsp;</td> : undefined }
                        <td className="SearchTable-head">
                            <div className="SearchTable-dateButtons">
                                <div className="SearchTable-dateButton" onClick={ this._setDates.bind(this, 0) }>Today</div>
                            </div>
                            Study Date
                            </td>
                        <td className="SearchTable-head">Accession</td>
                        <td className="SearchTable-head">Patient ID</td>
                        <td className="SearchTable-head">Patient Name</td>
                        <td className="SearchTable-head">Study Description</td>
                    </tr>
                    <tr>
                        { this.props.adminMode ? <td className="SearchTable-entry">&nbsp;</td> : undefined }
                        <td className="SearchTable-entry">
                            <input className="SearchTable-entryInputHalf" type="text" maxLength={10} value={ this.state.searchStartDate } onInput={ ev => { this.setState({ searchStartDate: ev.currentTarget.value }); } } onKeyPress={ this._onKeyPress } />
                            <input className="SearchTable-entryInputHalf" type="text" maxLength={10} value={ this.state.searchEndDate } onInput={ ev => { this.setState({ searchEndDate: ev.currentTarget.value }); } } onKeyPress={ this._onKeyPress } />
                        </td>
                        <td className="SearchTable-entry"><input className="SearchTable-entryInput" type="text" maxLength={16} value={ this.state.searchAccessionNum } onInput={ ev => { this.setState({ searchAccessionNum: ev.currentTarget.value }); } } onKeyPress={ this._onKeyPress } /></td>
                        <td className="SearchTable-entry"><input className="SearchTable-entryInput" type="text" maxLength={64} value={ this.state.searchPatientId } onInput={ ev => { this.setState({ searchPatientId: ev.currentTarget.value }); } } onKeyPress={ this._onKeyPress } /></td>
                        <td className="SearchTable-entry"><input className="SearchTable-entryInput" type="text" maxLength={64} value={ this.state.searchPatientName } onInput={ ev => { this.setState({ searchPatientName: ev.currentTarget.value }); } } onKeyPress={ this._onKeyPress } /></td>
                        <td className="SearchTable-entry"><input className="SearchTable-entryInput" type="text" maxLength={64} value={ this.state.searchDescription } onInput={ ev => { this.setState({ searchDescription: ev.currentTarget.value }); } } onKeyPress={ this._onKeyPress } /></td>
                        <td className="SearchTable-entry">
                            <input className="seachtableentrybutton" type="button" value="Search" onClick={ () => { this._search(); } } />
                            <input className="seachtableentrybutton" type="button" value="Clear" onClick={ this._resetSearch } />
                        </td>
                    </tr>
                </thead>
                <tbody>
                    { results }
                </tbody>
            </table>
        </div>;
    }

    private _setDates(daysAgo: number) {
        const newDate = new Date(Date.now() - 86400000 * daysAgo);
        this.setState({ searchStartDate: newDate.toLocaleDateString(), searchEndDate: newDate.toLocaleDateString() });
    };

    private _sendSelected = () => {
        const studyUIDs = _.keys(this.state.checkedItems);
        SelectEntityPanel.selectEntity(PSEntityFlagsMask.SendDestination).then(entity => {
            if (!entity) {
                return;
            }

            PSApiClient.sendStudies(studyUIDs, entity.title);
        });
    };

    private _deleteSelected = () => {
        const studyUIDs = _.keys(this.state.checkedItems);
        PSApiClient.deleteStudies(studyUIDs);
    };
}
