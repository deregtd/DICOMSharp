﻿export function getPatientInfoFromSearchResult(result: PSStudyBrowserSearchResult): DicomPatientInfo {
    return {
        patId: result.patID,
        patName: result.patName,
        patBirthDate: new Date(result.patBirthDate),
        patSex: result.patSex
    };
}

export function getStudyInfoFromSearchResult(result: PSStudyBrowserSearchResult): DicomStudyInfo {
    return {
        accessionNumber: result.accessionNum,
        modality: result.modality,
        studyDateTime: new Date(result.stuDateTime),
        studyDescription: result.stuDesc,
        studyID: result.stuID,
        studyInstanceUID: result.stuInstID,
        studyLocation: null
    };
}
