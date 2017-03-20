using System.Collections.Generic;
using System.Reflection;
using DICOMSharp.Data;

namespace DICOMSharp.Network.Abstracts
{
    /// <summary>
    /// Public enum (basically) of abstract syntaxes available for use and supported by DICOMSharp.
    /// These are outlined in a little more detail in the DICOM spec in PS 3.6, pages 120+
    /// </summary>
    public class AbstractSyntaxes
    {
        private AbstractSyntaxes() { }

        static AbstractSyntaxes()
        {
            //Use reflection to make a lookup array for the transfer syntaxes
            syntaxLookup = new Dictionary<string, AbstractSyntax>();

            foreach (FieldInfo field in typeof(AbstractSyntaxes).GetFields())
            {
                object val = field.GetValue(null);
                if (val is AbstractSyntax)
                {
                    AbstractSyntax ts = (AbstractSyntax)val;
                    syntaxLookup[ts.UidStr] = ts;
                    Uid.MasterLookup[ts.UidStr] = ts;
                }
            }
        }

        private static Dictionary<string, AbstractSyntax> syntaxLookup;

        /// <summary>
        /// Looks up an abstract syntax detail object by UID
        /// </summary>
        /// <param name="uid">The Abstract syntax UID to look up</param>
        /// <returns>A AbstractSyntax object from the dictionary with more info about the specified syntax, if available, otherwise a generic "unknown" syntax.</returns>
        public static AbstractSyntax Lookup(string uid)
        {
            if (syntaxLookup.ContainsKey(uid))
                return syntaxLookup[uid];
            return new AbstractSyntax(uid, uid);
        }

        internal static void Init() { }

#pragma warning disable 1591
        public static AbstractSyntax VerificationSOPClass = new AbstractSyntax("1.2.840.10008.1.1", "Verification SOP Class");
        public static AbstractSyntax MediaStorageDirectoryStorage = new AbstractSyntax("1.2.840.10008.1.3.10", "Media Storage Directory Storage");
        public static AbstractSyntax BasicStudyContentNotificationSOPClassRetired = new AbstractSyntax("1.2.840.10008.1.9", "Basic Study Content Notification SOP Class (Retired)");
        public static AbstractSyntax StorageCommitmentPushModelSOPClass = new AbstractSyntax("1.2.840.10008.1.20.1", "Storage Commitment Push Model SOP Class");
        public static AbstractSyntax StorageCommitmentPullModelSOPClassRetired = new AbstractSyntax("1.2.840.10008.1.20.2", "Storage Commitment Pull Model SOP Class (Retired)");
        public static AbstractSyntax ProceduralEventLoggingSOPClass = new AbstractSyntax("1.2.840.10008.1.40", "Procedural Event Logging SOP Class");
        public static AbstractSyntax SubstanceAdministrationLoggingSOPClass = new AbstractSyntax("1.2.840.10008.1.42", "Substance Administration Logging SOP Class");
        public static AbstractSyntax DetachedPatientManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.3.1.2.1.1", "Detached Patient Management SOP Class (Retired)");
        public static AbstractSyntax DetachedVisitManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.3.1.2.2.1", "Detached Visit Management SOP Class (Retired)");
        public static AbstractSyntax DetachedStudyManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.3.1.2.3.1", "Detached Study Management SOP Class (Retired)");
        public static AbstractSyntax StudyComponentManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.3.1.2.3.2", "Study Component Management SOP Class (Retired)");
        public static AbstractSyntax ModalityPerformedProcedureStepSOPClass = new AbstractSyntax("1.2.840.10008.3.1.2.3.3", "Modality Performed Procedure Step SOP Class");
        public static AbstractSyntax ModalityPerformedProcedureStepRetrieveSOPClass = new AbstractSyntax("1.2.840.10008.3.1.2.3.4", "Modality Performed Procedure Step Retrieve SOP Class");
        public static AbstractSyntax ModalityPerformedProcedureStepNotificationSOPClass = new AbstractSyntax("1.2.840.10008.3.1.2.3.5", "Modality Performed Procedure Step Notification SOP Class");
        public static AbstractSyntax DetachedResultsManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.3.1.2.5.1", "Detached Results Management SOP Class (Retired)");
        public static AbstractSyntax DetachedInterpretationManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.3.1.2.6.1", "Detached Interpretation Management SOP Class (Retired)");
        public static AbstractSyntax BasicFilmSessionSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.1", "Basic Film Session SOP Class");
        public static AbstractSyntax BasicFilmBoxSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.2", "Basic Film Box SOP Class");
        public static AbstractSyntax BasicGrayscaleImageBoxSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.4", "Basic Grayscale Image Box SOP Class");
        public static AbstractSyntax BasicColorImageBoxSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.4.1", "Basic Color Image Box SOP Class");
        public static AbstractSyntax ReferencedImageBoxSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.4.2", "Referenced Image Box SOP Class (Retired)");
        public static AbstractSyntax PrintJobSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.14", "Print Job SOP Class");
        public static AbstractSyntax BasicAnnotationBoxSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.15", "Basic Annotation Box SOP Class");
        public static AbstractSyntax PrinterSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.16", "Printer SOP Class");
        public static AbstractSyntax PrinterConfigurationRetrievalSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.16.376", "Printer Configuration Retrieval SOP Class");
        public static AbstractSyntax PrinterSOPInstance = new AbstractSyntax("1.2.840.10008.5.1.1.17", "Printer SOP Instance");
        public static AbstractSyntax BasicColorPrintManagementMetaSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.18", "Basic Color Print Management Meta SOP Class");
        public static AbstractSyntax VOILUTBoxSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.22", "VOI LUT Box SOP Class");
        public static AbstractSyntax PresentationLUTSOPClass = new AbstractSyntax("1.2.840.10008.5.1.1.23", "Presentation LUT SOP Class");
        public static AbstractSyntax ImageOverlayBoxSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.24", "Image Overlay Box SOP Class (Retired)");
        public static AbstractSyntax BasicPrintImageOverlayBoxSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.24.1", "Basic Print Image Overlay Box SOP Class (Retired)");
        public static AbstractSyntax PrintQueueManagementSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.26", "Print Queue Management SOP Class (Retired)");
        public static AbstractSyntax StoredPrintStorageSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.27", "Stored Print Storage SOP Class (Retired)");
        public static AbstractSyntax HardcopyGrayscaleImageStorageSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.29", "Hardcopy Grayscale Image Storage SOP Class (Retired)");
        public static AbstractSyntax HardcopyColorImageStorageSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.30", "Hardcopy Color Image Storage SOP Class (Retired)");
        public static AbstractSyntax PullPrintRequestSOPClassRetired = new AbstractSyntax("1.2.840.10008.5.1.1.31", "Pull Print Request SOP Class (Retired)");
        public static AbstractSyntax MediaCreationManagementSOPClassUID = new AbstractSyntax("1.2.840.10008.5.1.1.33", "Media Creation Management SOP Class UID");
        public static AbstractSyntax ComputedRadiographyImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1", "Computed Radiography Image Storage");
        public static AbstractSyntax DigitalXRayImageStorageForPresentation = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1.1", "Digital X-Ray Image Storage – For Presentation");
        public static AbstractSyntax DigitalXRayImageStorageForProcessing = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1.1.1", "Digital X-Ray Image Storage – For Processing");
        public static AbstractSyntax DigitalMammographyXRayImageStorageForPresentation = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1.2", "Digital Mammography X-Ray Image Storage – For Presentation");
        public static AbstractSyntax DigitalMammographyXRayImageStorageForProcessing = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1.2.1", "Digital Mammography X-Ray Image Storage – For Processing");
        public static AbstractSyntax DigitalIntraoralXRayImageStorageForPresentation = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1.3", "Digital Intra-oral X-Ray Image Storage – For Presentation");
        public static AbstractSyntax DigitalIntraoralXRayImageStorageForProcessing = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.1.3.1", "Digital Intra-oral X-Ray Image Storage – For Processing");
        public static AbstractSyntax CTImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.2", "CT Image Storage");
        public static AbstractSyntax EnhancedCTImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.2.1", "Enhanced CT Image Storage");
        public static AbstractSyntax UltrasoundMultiframeImageStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.3", "Ultrasound Multi-frame Image Storage (Retired)");
        public static AbstractSyntax UltrasoundMultiframeImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.3.1", "Ultrasound Multi-frame Image Storage");
        public static AbstractSyntax MRImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.4", "MR Image Storage");
        public static AbstractSyntax EnhancedMRImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.4.1", "Enhanced MR Image Storage");
        public static AbstractSyntax MRSpectroscopyStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.4.2", "MR Spectroscopy Storage");
        public static AbstractSyntax EnhancedMRColorImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.4.3", "Enhanced MR Color Image Storage");
        public static AbstractSyntax NuclearMedicineImageStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.5", "Nuclear Medicine Image Storage (Retired)");
        public static AbstractSyntax UltrasoundImageStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.6", "Ultrasound Image Storage (Retired)");
        public static AbstractSyntax UltrasoundImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.6.1", "Ultrasound Image Storage");
        public static AbstractSyntax EnhancedUSVolumeStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.6.2", "Enhanced US Volume Storage");
        public static AbstractSyntax SecondaryCaptureImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.7", "Secondary Capture Image Storage");
        public static AbstractSyntax MultiframeSingleBitSecondaryCaptureImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.7.1", "Multi-frame Single Bit Secondary Capture Image Storage");
        public static AbstractSyntax MultiframeGrayscaleByteSecondaryCaptureImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.7.2", "Multi-frame Grayscale Byte Secondary Capture Image Storage");
        public static AbstractSyntax MultiframeGrayscaleWordSecondaryCaptureImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.7.3", "Multi-frame Grayscale Word Secondary Capture Image Storage");
        public static AbstractSyntax MultiframeTrueColorSecondaryCaptureImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.7.4", "Multi-frame True Color Secondary Capture Image Storage");
        public static AbstractSyntax StandaloneOverlayStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.8", "Standalone Overlay Storage (Retired)");
        public static AbstractSyntax StandaloneCurveStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9", "Standalone Curve Storage (Retired)");
        public static AbstractSyntax WaveformStorageTrialRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.1", "Waveform Storage - Trial (Retired)");
        public static AbstractSyntax TwelveLeadECGWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.1.1", "12-lead ECG Waveform Storage");
        public static AbstractSyntax GeneralECGWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.1.2", "General ECG Waveform Storage");
        public static AbstractSyntax AmbulatoryECGWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.1.3", "Ambulatory ECG Waveform Storage");
        public static AbstractSyntax HemodynamicWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.2.1", "Hemodynamic Waveform Storage");
        public static AbstractSyntax CardiacElectrophysiologyWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.3.1", "Cardiac Electrophysiology Waveform Storage");
        public static AbstractSyntax BasicVoiceAudioWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.4.1", "Basic Voice Audio Waveform Storage");
        public static AbstractSyntax GeneralAudioWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.4.2", "General Audio Waveform Storage");
        public static AbstractSyntax ArterialPulseWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.5.1", "Arterial Pulse Waveform Storage");
        public static AbstractSyntax RespiratoryWaveformStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.9.6.1", "Respiratory Waveform Storage");
        public static AbstractSyntax StandaloneModalityLUTStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.10", "Standalone Modality LUT Storage (Retired)");
        public static AbstractSyntax StandaloneVOILUTStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.11", "Standalone VOI LUT Storage (Retired)");
        public static AbstractSyntax GrayscaleSoftcopyPresentationStateStorageSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.11.1", "Grayscale Softcopy Presentation State Storage SOP Class");
        public static AbstractSyntax ColorSoftcopyPresentationStateStorageSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.11.2", "Color Softcopy Presentation State Storage SOP Class");
        public static AbstractSyntax PseudoColorSoftcopyPresentationStateStorageSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.11.3", "Pseudo-Color Softcopy Presentation State Storage SOP Class");
        public static AbstractSyntax BlendingSoftcopyPresentationStateStorageSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.11.4", "Blending Softcopy Presentation State Storage SOP Class");
        public static AbstractSyntax XAXRFGrayscaleSoftcopyPresentationStateStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.11.5", "XA/XRF Grayscale Softcopy Presentation State Storage");
        public static AbstractSyntax XRayAngiographicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.12.1", "X-Ray Angiographic Image Storage");
        public static AbstractSyntax EnhancedXAImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.12.1.1", "Enhanced XA Image Storage");
        public static AbstractSyntax XRayRadiofluoroscopicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.12.2", "X-Ray Radiofluoroscopic Image Storage");
        public static AbstractSyntax EnhancedXRFImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.12.2.1", "Enhanced XRF Image Storage");
        public static AbstractSyntax XRay3DAngiographicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.13.1.1", "X-Ray 3D Angiographic Image Storage");
        public static AbstractSyntax XRay3DCraniofacialImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.13.1.2", "X-Ray 3D Craniofacial Image Storage");
        public static AbstractSyntax BreastTomosynthesisImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.13.1.3", "Breast Tomosynthesis Image Storage");
        public static AbstractSyntax XRayAngiographicBiPlaneImageStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.12.3", "X-Ray Angiographic Bi-Plane Image Storage (Retired)");
        public static AbstractSyntax NuclearMedicineImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.20", "Nuclear Medicine Image Storage");
        public static AbstractSyntax RawDataStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.66", "Raw Data Storage");
        public static AbstractSyntax SpatialRegistrationStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.66.1", "Spatial Registration Storage");
        public static AbstractSyntax SpatialFiducialsStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.66.2", "Spatial Fiducials Storage");
        public static AbstractSyntax DeformableSpatialRegistrationStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.66.3", "Deformable Spatial Registration Storage");
        public static AbstractSyntax SegmentationStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.66.4", "Segmentation Storage");
        public static AbstractSyntax SurfaceSegmentationStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.66.5", "Surface Segmentation Storage");
        public static AbstractSyntax RealWorldValueMappingStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.67", "Real World Value Mapping Storage");
        public static AbstractSyntax VLEndoscopicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.1", "VL Endoscopic Image Storage");
        public static AbstractSyntax VideoEndoscopicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.1.1", "Video Endoscopic Image Storage");
        public static AbstractSyntax VLMicroscopicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.2", "VL Microscopic Image Storage");
        public static AbstractSyntax VideoMicroscopicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.2.1", "Video Microscopic Image Storage");
        public static AbstractSyntax VLSlideCoordinatesMicroscopicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.3", "VL Slide-Coordinates Microscopic Image Storage");
        public static AbstractSyntax VLPhotographicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.4", "VL Photographic Image Storage");
        public static AbstractSyntax VideoPhotographicImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.4.1", "Video Photographic Image Storage");
        public static AbstractSyntax OphthalmicPhotography8BitImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.5.1", "Ophthalmic Photography 8 Bit Image Storage");
        public static AbstractSyntax OphthalmicPhotography16BitImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.5.2", "Ophthalmic Photography 16 Bit Image Storage");
        public static AbstractSyntax StereometricRelationshipStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.5.3", "Stereometric Relationship Storage");
        public static AbstractSyntax OphthalmicTomographyImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.77.1.5.4", "Ophthalmic Tomography Image Storage");
        public static AbstractSyntax LensometryMeasurementsStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.78.1", "Lensometry Measurements Storage");
        public static AbstractSyntax AutorefractionMeasurementsStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.78.2", "Autorefraction Measurements Storage");
        public static AbstractSyntax KeratometryMeasurementsStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.78.3", "Keratometry Measurements Storage");
        public static AbstractSyntax SubjectiveRefractionMeasurementsStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.78.4", "Subjective Refraction Measurements Storage");
        public static AbstractSyntax VisualAcuityMeasurements = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.78.5", "Visual Acuity Measurements");
        public static AbstractSyntax SpectaclePrescriptionReportsStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.78.6", "Spectacle Prescription Reports Storage");
        public static AbstractSyntax MacularGridThicknessandVolumeReportStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.79.1", "Macular Grid Thickness and Volume Report Storage");
        public static AbstractSyntax TextSRStorageTrialRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.1", "Text SR Storage – Trial (Retired)");
        public static AbstractSyntax AudioSRStorageTrialRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.2", "Audio SR Storage – Trial (Retired)");
        public static AbstractSyntax DetailSRStorageTrialRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.3", "Detail SR Storage – Trial (Retired)");
        public static AbstractSyntax ComprehensiveSRStorageTrialRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.4", "Comprehensive SR Storage – Trial (Retired)");
        public static AbstractSyntax BasicTextSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.11", "Basic Text SR Storage");
        public static AbstractSyntax EnhancedSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.22", "Enhanced SR Storage");
        public static AbstractSyntax ComprehensiveSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.33", "Comprehensive SR Storage");
        public static AbstractSyntax ProcedureLogStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.40", "Procedure Log Storage");
        public static AbstractSyntax MammographyCADSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.50", "Mammography CAD SR Storage");
        public static AbstractSyntax KeyObjectSelectionDocumentStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.59", "Key Object Selection Document Storage");
        public static AbstractSyntax ChestCADSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.65", "Chest CAD SR Storage");
        public static AbstractSyntax XRayRadiationDoseSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.67", "X-Ray Radiation Dose SR Storage");
        public static AbstractSyntax ColonCADSRStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.88.69", "Colon CAD SR Storage");
        public static AbstractSyntax EncapsulatedPDFStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.104.1", "Encapsulated PDF Storage");
        public static AbstractSyntax EncapsulatedCDAStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.104.2", "Encapsulated CDA Storage");
        public static AbstractSyntax PositronEmissionTomographyImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.128", "Positron Emission Tomography Image Storage");
        public static AbstractSyntax StandalonePETCurveStorageRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.129", "Standalone PET Curve Storage (Retired)");
        public static AbstractSyntax EnhancedPETImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.130", "Enhanced PET Image Storage");
        public static AbstractSyntax BasicStructuredDisplayStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.131", "Basic Structured Display Storage");
        public static AbstractSyntax RTImageStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.1", "RT Image Storage");
        public static AbstractSyntax RTDoseStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.2", "RT Dose Storage");
        public static AbstractSyntax RTStructureSetStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.3", "RT Structure Set Storage");
        public static AbstractSyntax RTBeamsTreatmentRecordStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.4", "RT Beams Treatment Record Storage");
        public static AbstractSyntax RTPlanStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.5", "RT Plan Storage");
        public static AbstractSyntax RTBrachyTreatmentRecordStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.6", "RT Brachy Treatment Record Storage");
        public static AbstractSyntax RTTreatmentSummaryRecordStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.7", "RT Treatment Summary Record Storage");
        public static AbstractSyntax RTIonPlanStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.8", "RT Ion Plan Storage");
        public static AbstractSyntax RTIonBeamsTreatmentRecordStorage = new AbstractSyntax("1.2.840.10008.5.1.4.1.1.481.9", "RT Ion Beams Treatment Record Storage");
        
        public static AbstractSyntax PatientRootQueryRetrieveInformationModelFIND = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.1.1", "Patient Root QueryRetrieve Information Model – FIND");
        public static AbstractSyntax PatientRootQueryRetrieveInformationModelMOVE = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.1.2", "Patient Root QueryRetrieve Information Model – MOVE");
        public static AbstractSyntax PatientRootQueryRetrieveInformationModelGET = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.1.3", "Patient Root QueryRetrieve Information Model – GET");
        public static AbstractSyntax StudyRootQueryRetrieveInformationModelFIND = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.2.1", "Study Root QueryRetrieve Information Model – FIND");
        public static AbstractSyntax StudyRootQueryRetrieveInformationModelMOVE = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.2.2", "Study Root QueryRetrieve Information Model – MOVE");
        public static AbstractSyntax StudyRootQueryRetrieveInformationModelGET = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.2.3", "Study Root QueryRetrieve Information Model – GET");
        public static AbstractSyntax PatientStudyOnlyQueryRetrieveInformationModelFINDRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.3.1", "PatientStudy Only QueryRetrieve Information Model - FIND (Retired)");
        public static AbstractSyntax PatientStudyOnlyQueryRetrieveInformationModelMOVERetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.3.2", "PatientStudy Only QueryRetrieve Information Model - MOVE (Retired)");
        public static AbstractSyntax PatientStudyOnlyQueryRetrieveInformationModelGETRetired = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.3.3", "PatientStudy Only QueryRetrieve Information Model - GET (Retired)");
        
        public static AbstractSyntax CompositeInstanceRootRetrieveMOVE = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.4.2", "Composite Instance Root Retrieve - MOVE");
        public static AbstractSyntax CompositeInstanceRootRetrieveGET = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.4.3", "Composite Instance Root Retrieve - GET");
        public static AbstractSyntax CompositeInstanceRetrieveWithoutBulkDataGET = new AbstractSyntax("1.2.840.10008.5.1.4.1.2.5.3", "Composite Instance Retrieve Without Bulk Data - GET");
        public static AbstractSyntax ModalityWorklistInformationModelFIND = new AbstractSyntax("1.2.840.10008.5.1.4.31", "Modality Worklist Information Model – FIND");
        public static AbstractSyntax GeneralPurposeWorklistInformationModelFIND = new AbstractSyntax("1.2.840.10008.5.1.4.32.1", "General Purpose Worklist Information Model – FIND");
        
        public static AbstractSyntax GeneralPurposeScheduledProcedureStepSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.32.2", "General Purpose Scheduled Procedure Step SOP Class");
        public static AbstractSyntax GeneralPurposePerformedProcedureStepSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.32.3", "General Purpose Performed Procedure Step SOP Class");
        public static AbstractSyntax InstanceAvailabilityNotificationSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.33", "Instance Availability Notification SOP Class");
        public static AbstractSyntax RTBeamsDeliveryInstructionStorageSupplement74FrozenDraft = new AbstractSyntax("1.2.840.10008.5.1.4.34.1", "RT Beams Delivery Instruction Storage (Supplement 74 Frozen Draft)");
        public static AbstractSyntax RTConventionalMachineVerificationSupplement74FrozenDraft = new AbstractSyntax("1.2.840.10008.5.1.4.34.2", "RT Conventional Machine Verification (Supplement 74 Frozen Draft)");
        public static AbstractSyntax RTIonMachineVerificationSupplement74FrozenDraft = new AbstractSyntax("1.2.840.10008.5.1.4.34.3", "RT Ion Machine Verification (Supplement 74 Frozen Draft)");
        public static AbstractSyntax UnifiedProcedureStepPushSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.34.4.1", "Unified Procedure Step – Push SOP Class");
        public static AbstractSyntax UnifiedProcedureStepWatchSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.34.4.2", "Unified Procedure Step – Watch SOP Class");
        public static AbstractSyntax UnifiedProcedureStepPullSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.34.4.3", "Unified Procedure Step – Pull SOP Class");
        public static AbstractSyntax UnifiedProcedureStepEventSOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.34.4.4", "Unified Procedure Step – Event SOP Class");
        public static AbstractSyntax GeneralRelevantPatientInformationQuery = new AbstractSyntax("1.2.840.10008.5.1.4.37.1", "General Relevant Patient Information Query");
        public static AbstractSyntax BreastImagingRelevantPatientInformationQuery = new AbstractSyntax("1.2.840.10008.5.1.4.37.2", "Breast Imaging Relevant Patient Information Query");
        public static AbstractSyntax CardiacRelevantPatientInformationQuery = new AbstractSyntax("1.2.840.10008.5.1.4.37.3", "Cardiac Relevant Patient Information Query");
        public static AbstractSyntax HangingProtocolStorage = new AbstractSyntax("1.2.840.10008.5.1.4.38.1", "Hanging Protocol Storage");
        public static AbstractSyntax HangingProtocolInformationModelFIND = new AbstractSyntax("1.2.840.10008.5.1.4.38.2", "Hanging Protocol Information Model – FIND");
        public static AbstractSyntax HangingProtocolInformationModelMOVE = new AbstractSyntax("1.2.840.10008.5.1.4.38.3", "Hanging Protocol Information Model – MOVE");
        public static AbstractSyntax HangingProtocolInformationModelGET = new AbstractSyntax("1.2.840.10008.5.1.4.38.4", "Hanging Protocol Information Model - GET");
        public static AbstractSyntax ProductCharacteristicsQuerySOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.41", "Product Characteristics Query SOP Class");
        public static AbstractSyntax SubstanceApprovalQuerySOPClass = new AbstractSyntax("1.2.840.10008.5.1.4.42", "Substance Approval Query SOP Class");

        /// <summary>
        /// Helpful list of all possible Query/Retrieve syntaxes to make it easier to add it to your <see cref="DICOMSharp.Network.Connections.DICOMConnection.SupportedAbstractSyntaxes"/> list
        /// </summary>
        public static HashSet<AbstractSyntax> QueryRetrieveSyntaxes = new HashSet<AbstractSyntax>(new AbstractSyntax[] {
            PatientRootQueryRetrieveInformationModelFIND,
            PatientRootQueryRetrieveInformationModelMOVE,
            PatientRootQueryRetrieveInformationModelGET,
            StudyRootQueryRetrieveInformationModelFIND,
            StudyRootQueryRetrieveInformationModelMOVE,
            StudyRootQueryRetrieveInformationModelGET,
            PatientStudyOnlyQueryRetrieveInformationModelFINDRetired,
            PatientStudyOnlyQueryRetrieveInformationModelMOVERetired,
            PatientStudyOnlyQueryRetrieveInformationModelGETRetired,
            
            ModalityWorklistInformationModelFIND
        });

        /// <summary>
        /// Helpful list of all possible Image Storage syntaxes to make it easier to add it to your <see cref="DICOMSharp.Network.Connections.DICOMConnection.SupportedAbstractSyntaxes"/> list
        /// </summary>
        public static HashSet<AbstractSyntax> StorageSyntaxes = new HashSet<AbstractSyntax>(new AbstractSyntax[] {
            CTImageStorage,
            EnhancedCTImageStorage,
            ComputedRadiographyImageStorage,
            DigitalXRayImageStorageForPresentation,
            DigitalXRayImageStorageForProcessing,
            DigitalMammographyXRayImageStorageForPresentation,
            DigitalMammographyXRayImageStorageForProcessing,
            DigitalIntraoralXRayImageStorageForPresentation,
            DigitalIntraoralXRayImageStorageForProcessing,
            UltrasoundMultiframeImageStorageRetired,
            UltrasoundMultiframeImageStorage,
            MRImageStorage,
            EnhancedMRImageStorage,
            MRSpectroscopyStorage,
            EnhancedMRColorImageStorage,
            NuclearMedicineImageStorageRetired,
            UltrasoundImageStorageRetired,
            UltrasoundImageStorage,
            EnhancedUSVolumeStorage,
            SecondaryCaptureImageStorage,
            MultiframeSingleBitSecondaryCaptureImageStorage,
            MultiframeGrayscaleByteSecondaryCaptureImageStorage,
            MultiframeGrayscaleWordSecondaryCaptureImageStorage,
            MultiframeTrueColorSecondaryCaptureImageStorage,
            StandaloneOverlayStorageRetired,
            StandaloneCurveStorageRetired,
            WaveformStorageTrialRetired,
            TwelveLeadECGWaveformStorage,
            GeneralECGWaveformStorage,
            AmbulatoryECGWaveformStorage,
            HemodynamicWaveformStorage,
            CardiacElectrophysiologyWaveformStorage,
            BasicVoiceAudioWaveformStorage,
            GeneralAudioWaveformStorage,
            ArterialPulseWaveformStorage,
            RespiratoryWaveformStorage,
            StandaloneModalityLUTStorageRetired,
            StandaloneVOILUTStorageRetired,
            GrayscaleSoftcopyPresentationStateStorageSOPClass,
            ColorSoftcopyPresentationStateStorageSOPClass,
            PseudoColorSoftcopyPresentationStateStorageSOPClass,
            BlendingSoftcopyPresentationStateStorageSOPClass,
            XAXRFGrayscaleSoftcopyPresentationStateStorage,
            XRayAngiographicImageStorage,
            EnhancedXAImageStorage,
            XRayRadiofluoroscopicImageStorage,
            EnhancedXRFImageStorage,
            XRay3DAngiographicImageStorage,
            XRay3DCraniofacialImageStorage,
            BreastTomosynthesisImageStorage,
            XRayAngiographicBiPlaneImageStorageRetired,
            NuclearMedicineImageStorage,
            RawDataStorage,
            SpatialRegistrationStorage,
            SpatialFiducialsStorage,
            DeformableSpatialRegistrationStorage,
            SegmentationStorage,
            SurfaceSegmentationStorage,
            RealWorldValueMappingStorage,
            VLEndoscopicImageStorage,
            VideoEndoscopicImageStorage,
            VLMicroscopicImageStorage,
            VideoMicroscopicImageStorage,
            VLSlideCoordinatesMicroscopicImageStorage,
            VLPhotographicImageStorage,
            VideoPhotographicImageStorage,
            OphthalmicPhotography8BitImageStorage,
            OphthalmicPhotography16BitImageStorage,
            StereometricRelationshipStorage,
            OphthalmicTomographyImageStorage,
            LensometryMeasurementsStorage,
            AutorefractionMeasurementsStorage,
            KeratometryMeasurementsStorage,
            SubjectiveRefractionMeasurementsStorage,
            VisualAcuityMeasurements,
            SpectaclePrescriptionReportsStorage,
            MacularGridThicknessandVolumeReportStorage,
            TextSRStorageTrialRetired,
            AudioSRStorageTrialRetired,
            DetailSRStorageTrialRetired,
            ComprehensiveSRStorageTrialRetired,
            BasicTextSRStorage,
            EnhancedSRStorage,
            ComprehensiveSRStorage,
            ProcedureLogStorage,
            MammographyCADSRStorage,
            KeyObjectSelectionDocumentStorage,
            ChestCADSRStorage,
            XRayRadiationDoseSRStorage,
            ColonCADSRStorage,
            EncapsulatedPDFStorage,
            EncapsulatedCDAStorage,
            PositronEmissionTomographyImageStorage,
            StandalonePETCurveStorageRetired,
            EnhancedPETImageStorage,
            BasicStructuredDisplayStorage,
            RTImageStorage,
            RTDoseStorage,
            RTStructureSetStorage,
            RTBeamsTreatmentRecordStorage,
            RTPlanStorage,
            RTBrachyTreatmentRecordStorage,
            RTTreatmentSummaryRecordStorage,
            RTIonPlanStorage,
            RTIonBeamsTreatmentRecordStorage
        });
    }
}
