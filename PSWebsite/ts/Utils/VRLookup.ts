import * as DicomTags from './DicomTags';

let vrLookup: _.Dictionary<string> = {};
vrLookup[DicomTags.SOPInstanceUID] = 'UI';
vrLookup[DicomTags.StudyDate] = 'DA';
vrLookup[DicomTags.SeriesDate] = 'DA';
vrLookup[DicomTags.AcquisitionDate] = 'DA';
vrLookup[DicomTags.StudyTime] = 'TM';
vrLookup[DicomTags.SeriesTime] = 'TM';
vrLookup[DicomTags.AcquisitionTime] = 'TM';
vrLookup[DicomTags.AccessionNumber] = 'SH';
vrLookup[DicomTags.Modality] = 'CS';
vrLookup[DicomTags.InstitutionName] = 'LO';
vrLookup[DicomTags.StudyDescription] = 'LO';
vrLookup[DicomTags.SeriesDescription] = 'LO';

vrLookup[DicomTags.PatientName] = 'PN';
vrLookup[DicomTags.PatientID] = 'LO';
vrLookup[DicomTags.PatientBirthDate] = 'DA';
vrLookup[DicomTags.PatientBirthTime] = 'TM';
vrLookup[DicomTags.PatientSex] = 'CS';

vrLookup[DicomTags.SliceThickness] = 'DS';
vrLookup[DicomTags.KVP] = 'DS';
vrLookup[DicomTags.RepetitionTime] = 'DS';
vrLookup[DicomTags.EchoTime] = 'DS';
vrLookup[DicomTags.EchoTrainLength] = 'IS';
vrLookup[DicomTags.GantryDetectorTilt] = 'DS';
vrLookup[DicomTags.ExposureTime] = 'IS';
vrLookup[DicomTags.XRayTubeCurrent] = 'IS';

vrLookup[DicomTags.StudyInstanceUID] = 'UI';
vrLookup[DicomTags.SeriesInstanceUID] = 'UI';
vrLookup[DicomTags.StudyID] = 'SH';
vrLookup[DicomTags.SeriesNumber] = 'IS';
vrLookup[DicomTags.AcquisitionNumber] = 'IS';
vrLookup[DicomTags.InstanceNumber] = 'IS';
vrLookup[DicomTags.ImagePosition] = 'DS';
vrLookup[DicomTags.ImagePositionPatient] = 'DS';
vrLookup[DicomTags.ImageOrientation] = 'DS';
vrLookup[DicomTags.ImageOrientationPatient] = 'DS';
vrLookup[DicomTags.SliceLocation] = 'DS';

vrLookup[DicomTags.PhotometricInterpretation] = 'CS';
vrLookup[DicomTags.PlanarConfiguration] = 'US';
vrLookup[DicomTags.NumberOfFrames] = 'IS';
vrLookup[DicomTags.Rows] = 'US';
vrLookup[DicomTags.Columns] = 'US';
vrLookup[DicomTags.PixelSpacing] = 'DS';
vrLookup[DicomTags.BitsAllocated] = 'US';
vrLookup[DicomTags.BitsStored] = 'US';
vrLookup[DicomTags.PixelRepresentation] = 'US';
vrLookup[DicomTags.PixelIntensityRelationship] = 'CS';
vrLookup[DicomTags.PixelIntensityRelationshipSign] = 'SS';
vrLookup[DicomTags.WindowCenter] = 'DS';
vrLookup[DicomTags.WindowWidth] = 'DS';
vrLookup[DicomTags.RescaleIntercept] = 'DS';
vrLookup[DicomTags.RescaleSlope] = 'DS';
vrLookup[DicomTags.RescaleType] = 'LO';
vrLookup[DicomTags.RedPaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.GreenPaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.BluePaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.AlphaPaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.LargeRedPaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.LargeGreenPaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.LargeBluePaletteColorLookupTableDescriptor] = 'US';
vrLookup[DicomTags.PaletteColorLookupTableUID] = 'UI';
vrLookup[DicomTags.GrayLookupTableData] = 'OW';
vrLookup[DicomTags.RedPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.GreenPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.BluePaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.AlphaPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.LargeRedPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.LargeGreenPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.LargeBluePaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.LargePaletteColorLookupTableUID] = 'UI';
vrLookup[DicomTags.SegmentedRedPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.SegmentedGreenPaletteColorLookupTableData] = 'OW';
vrLookup[DicomTags.SegmentedBluePaletteColorLookupTableData] = 'OW';

vrLookup[DicomTags.PixelData] = 'OB';

export function LookupVR(tag: string): string {
    return vrLookup[tag] || 'UT';
} 
