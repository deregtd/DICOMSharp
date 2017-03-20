import DicomUtils = require('./DicomUtils');

class DicomTags {
    static SOPInstanceUID = DicomUtils.MakeTag(0x0008, 0x0018);
    static StudyDate = DicomUtils.MakeTag(0x0008, 0x0020);
    static SeriesDate = DicomUtils.MakeTag(0x0008, 0x0021);
    static AcquisitionDate = DicomUtils.MakeTag(0x0008, 0x0022);
    static StudyTime = DicomUtils.MakeTag(0x0008, 0x0030);
    static SeriesTime = DicomUtils.MakeTag(0x0008, 0x0031);
    static AcquisitionTime = DicomUtils.MakeTag(0x0008, 0x0032);
    static AccessionNumber = DicomUtils.MakeTag(0x0008, 0x0050);
    static Modality = DicomUtils.MakeTag(0x0008, 0x0060);
    static InstitutionName = DicomUtils.MakeTag(0x0008, 0x0080);
    static StudyDescription = DicomUtils.MakeTag(0x0008, 0x1030);
    static SeriesDescription = DicomUtils.MakeTag(0x0008, 0x103E);

    static PatientName = DicomUtils.MakeTag(0x0010, 0x0010);
    static PatientID = DicomUtils.MakeTag(0x0010, 0x0020);
    static PatientBirthDate = DicomUtils.MakeTag(0x0010, 0x0030);
    static PatientBirthTime = DicomUtils.MakeTag(0x0010, 0x0032);
    static PatientSex = DicomUtils.MakeTag(0x0010, 0x0040);

    static SliceThickness = DicomUtils.MakeTag(0x0018, 0x0050);
    static KVP = DicomUtils.MakeTag(0x0018, 0x0060);
    static RepetitionTime = DicomUtils.MakeTag(0x0018, 0x0080);
    static EchoTime = DicomUtils.MakeTag(0x0018, 0x0081);
    static EchoTrainLength = DicomUtils.MakeTag(0x0018, 0x0091);
    static GantryDetectorTilt = DicomUtils.MakeTag(0x0018, 0x1120);
    static ExposureTime = DicomUtils.MakeTag(0x0018, 0x1150);
    static XRayTubeCurrent = DicomUtils.MakeTag(0x0018, 0x1151);

    static StudyInstanceUID = DicomUtils.MakeTag(0x0020, 0x000D);
    static SeriesInstanceUID = DicomUtils.MakeTag(0x0020, 0x000E);
    static StudyID = DicomUtils.MakeTag(0x0020, 0x0010);
    static SeriesNumber = DicomUtils.MakeTag(0x0020, 0x0011);
    static AcquisitionNumber = DicomUtils.MakeTag(0x0020, 0x0012);
    static InstanceNumber = DicomUtils.MakeTag(0x0020, 0x0013);
    static ImagePosition = DicomUtils.MakeTag(0x0020, 0x0030);
    static ImagePositionPatient = DicomUtils.MakeTag(0x0020, 0x0032);
    static ImageOrientation = DicomUtils.MakeTag(0x0020, 0x0035);
    static ImageOrientationPatient = DicomUtils.MakeTag(0x0020, 0x0037);
    static SliceLocation = DicomUtils.MakeTag(0x0020, 0x1041);

    static PhotometricInterpretation = DicomUtils.MakeTag(0x0028, 0x0004);
    static PlanarConfiguration = DicomUtils.MakeTag(0x0028, 0x0006);
    static NumberOfFrames = DicomUtils.MakeTag(0x0028, 0x0008);
    static Rows = DicomUtils.MakeTag(0x0028, 0x0010);
    static Columns = DicomUtils.MakeTag(0x0028, 0x0011);
    static PixelSpacing = DicomUtils.MakeTag(0x0028, 0x0030);
    static BitsAllocated = DicomUtils.MakeTag(0x0028, 0x0100);
    static BitsStored = DicomUtils.MakeTag(0x0028, 0x0101);
    static PixelRepresentation = DicomUtils.MakeTag(0x0028, 0x0103);
    static PixelIntensityRelationship = DicomUtils.MakeTag(0x0028, 0x1040);
    static PixelIntensityRelationshipSign = DicomUtils.MakeTag(0x0028, 0x1041);
    static WindowCenter = DicomUtils.MakeTag(0x0028, 0x1050);
    static WindowWidth = DicomUtils.MakeTag(0x0028, 0x1051);
    static RescaleIntercept = DicomUtils.MakeTag(0x0028, 0x1052);
    static RescaleSlope = DicomUtils.MakeTag(0x0028, 0x1053);
    static RescaleType = DicomUtils.MakeTag(0x0028, 0x1054);
    static RedPaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1101);
    static GreenPaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1102);
    static BluePaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1103);
    static AlphaPaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1104);
    static LargeRedPaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1111);
    static LargeGreenPaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1112);
    static LargeBluePaletteColorLookupTableDescriptor = DicomUtils.MakeTag(0x0028, 0x1113);
    static PaletteColorLookupTableUID = DicomUtils.MakeTag(0x0028, 0x1199);
    static GrayLookupTableData = DicomUtils.MakeTag(0x0028, 0x1200);
    static RedPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1201);
    static GreenPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1202);
    static BluePaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1203);
    static AlphaPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1204);
    static LargeRedPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1211);
    static LargeGreenPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1212);
    static LargeBluePaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1213);
    static LargePaletteColorLookupTableUID = DicomUtils.MakeTag(0x0028, 0x1214);
    static SegmentedRedPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1221);
    static SegmentedGreenPaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1222);
    static SegmentedBluePaletteColorLookupTableData = DicomUtils.MakeTag(0x0028, 0x1223);

    static PixelData = DicomUtils.MakeTag(0x7FE0, 0x0010);
}

export = DicomTags;
