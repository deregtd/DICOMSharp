using DICOMSharp.Data;
using DICOMSharp.Data.Tags;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Logging;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Network.Connections;
using DICOMSharp.Network.QueryRetrieve;
using DICOMSharp.Network.Workers;
using DICOMSharp.Util;
using Newtonsoft.Json;
using PSCommon.Models;
using PSCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PSCommon.Dicom
{
    public class DicomServerSettings
    {
        public bool ListeningEnabled;
        public string AETitle;
        public int ListenPort;
        public string ImageStoragePath;
        public long ImageStorageSizeMB;
        public bool AutoDecompress;
        public bool StoreMetadataOnlyFiles;
        public bool VerboseLogging;
        public bool PromiscuousMode;
    }

    public class TaskInfo
    {
        public int ProgressCount, ProgressTotal;
        public Task Task;
        public CancellationTokenSource Token;
    }

    public class DicomServer
    {
        private ILogger _logger = null;
        private DICOMListener _listener = null;
        private PSDatabase _db = null;
        private bool _isStarted = false;

        private string _tildePathReplace = null;

        private DicomServerSettings _settings = null;

        private Dictionary<uint, TaskInfo> _tasksInProcess = new Dictionary<uint, TaskInfo>();

        public const string Option_PruneDBSizeMB = "PruneDBSizeMB";
        public const string Option_ListeningEnabled = "ListeningEnabled";
        public const string Option_AETitle = "AETitle";
        public const string Option_ListenPort = "ListenPort";
        public const string Option_ImageStoragePath = "ImageStoragePath";
        public const string Option_AutoDecompress = "AutoDecompress";
        public const string Option_StoreMetadataOnlyFiles = "StoreMetadataOnlyFiles";
        public const string Option_PromiscuousMode = "PromiscuousMode";
        public const string Option_VerboseLogging = "VerboseLogging";

        public DicomServer(ILogger logger, PSDatabase db, string tildePathReplace = null)
        {
            this._logger = logger;
            this._db = db;
            this._tildePathReplace = tildePathReplace;

            this._listener = new DICOMListener(this._logger, false);

            this._listener.StoreRequest += new DICOMListener.StoreRequestHandler(listener_StoreRequest);
            this._listener.AssociationRequest += new DICOMListener.BasicConnectionHandler(listener_AssociationRequest);
            this._listener.FindRequest += new DICOMListener.QRRequestHandler(listener_FindRequest);
            this._listener.GetRequest += new DICOMListener.QRRequestHandler(listener_GetRequest);
            this._listener.MoveRequest += new DICOMListener.QRRequestHandler(listener_MoveRequest);
            this._listener.EntityLookup += new DICOMListener.EntityLookupHandler(listener_EntityLookup);
        }

        public void LoadSettings()
        {
            if (this._db.IsSetup)
            {
                LoadSettingsFromDatabase();
            }
            else
            {
                LoadDefaultSettings();
            }
        }

        public void LoadDefaultSettings()
        {
            var settings = new DicomServerSettings()
            {
                ListeningEnabled = false,
                AETitle = "PSDICOM",
                ListenPort = 4006,
                ImageStoragePath = @"c:\dicom",
                ImageStorageSizeMB = 1024,
                AutoDecompress = false,
                PromiscuousMode = false,
                StoreMetadataOnlyFiles = false,
                VerboseLogging = false
            };

            this.UseSettings(settings);
        }

        public void LoadSettingsFromDatabase()
        {
            var settings = new DicomServerSettings()
            {
                ListeningEnabled = this._db.GetOptionBool(Option_ListeningEnabled, false),
                AETitle = this._db.GetOption(Option_AETitle, "PSDICOM"),
                ListenPort = this._db.GetOptionInt(Option_ListenPort, 4006),
                ImageStoragePath = this._db.GetOption(Option_ImageStoragePath, @"c:\dicom"),
                ImageStorageSizeMB = this._db.GetOptionInt(Option_PruneDBSizeMB, 1024),
                AutoDecompress = this._db.GetOptionBool(Option_AutoDecompress, false),
                PromiscuousMode = this._db.GetOptionBool(Option_PromiscuousMode, false),
                StoreMetadataOnlyFiles = this._db.GetOptionBool(Option_StoreMetadataOnlyFiles, false),
                VerboseLogging = this._db.GetOptionBool(Option_VerboseLogging, false)
            };

            this.UseSettings(settings);
        }

        public void UseSettings(DicomServerSettings settings)
        {
            this._settings = settings;

            if (this._db.IsSetup)
            {
                // Always re-save to DB, even if we just loaded it, in case we've transformed data or we're saving defaults
                this._db.SetOption(Option_ListeningEnabled, settings.ListeningEnabled);
                this._db.SetOption(Option_AETitle, settings.AETitle);
                this._db.SetOption(Option_ListenPort, settings.ListenPort);
                this._db.SetOption(Option_ImageStoragePath, settings.ImageStoragePath);
                this._db.SetOption(Option_PruneDBSizeMB, (int)settings.ImageStorageSizeMB);
                this._db.SetOption(Option_AutoDecompress, settings.AutoDecompress);
                this._db.SetOption(Option_PromiscuousMode, settings.PromiscuousMode);
                this._db.SetOption(Option_StoreMetadataOnlyFiles, settings.StoreMetadataOnlyFiles);
                this._db.SetOption(Option_VerboseLogging, settings.VerboseLogging);
            }

            this._listener.VerboseLogging = this._settings.VerboseLogging;

            this._db.PruneDBSizeMB = (uint)this._settings.ImageStorageSizeMB;
        }

        public void StartServer()
        {
            if (!this._db.IsConnected || !this._db.IsSetup)
            {
                this._logger.Log(LogLevel.Error, "Database either not connected or not setup");
                return;
            }

            if (this._settings == null)
            {
                this._logger.Log(LogLevel.Error, "DicomServer not set up");
                return;
            }

            if (this._settings.ListeningEnabled)
            {
                try
                {
                    this._listener.StartListening((ushort)this._settings.ListenPort);
                }
                catch (Exception e)
                {
                    this._logger.Log(LogLevel.Error, "Couldn't start listening: " + e.Message);
                    return;
                }
            }

            this._isStarted = true;

            var taskQueueItems = this._db.GetTaskQueue();
            taskQueueItems.ForEach(item => this.StartTask(item));
        }

        public void StopServer()
        {
            this._isStarted = false;

            if (this._listener != null)
            {
                this._listener.StopListening();
            }

            foreach (var pair in this._tasksInProcess)
            {
                pair.Value.Token.Cancel();
            }
            this._tasksInProcess.Clear();
        }

        private void StartTask(PSTaskQueueItem task)
        {
            Debug.Assert(task.TaskId != 0, "Should always have a task id");

            TaskInfo taskTracking = null;
            switch (task.TaskType)
            {
                case TaskType.Import:
                    var importData = JsonConvert.DeserializeObject<TaskDataImport>(task.TaskDataSerialized);
                    taskTracking = ImportFromPath(importData.RootPath);
                    break;
                case TaskType.SendStudy:
                    var sendData = JsonConvert.DeserializeObject<TaskDataSendStudy>(task.TaskDataSerialized);
                    taskTracking = SendStudy(sendData.StudyInstanceUID, sendData.AETarget);
                    break;
                case TaskType.DeleteStudy:
                    var deleteData = JsonConvert.DeserializeObject<TaskDataDeleteStudy>(task.TaskDataSerialized);
                    taskTracking = DeleteStudy(deleteData.StudyInstanceUID);
                    break;
                default:
                    // No idea what this is....
                    this._logger.Log(LogLevel.Warning, "Unknown task type: " + task.TaskType + ", Ending Id " + task.TaskId);
                    this._db.CompleteTaskQueueItem(task.TaskId);
                    return;
            }

            Debug.Assert(taskTracking != null, "Should always have a tracking task");

            taskTracking.Task.ContinueWith(taskCompleted =>
            {
                this._db.CompleteTaskQueueItem(task.TaskId);
                this._tasksInProcess.Remove(task.TaskId);
            });

            this._tasksInProcess[task.TaskId] = taskTracking;
        }

        private void TrackTask(PSTaskQueueItem task)
        {
            if (!this._db.IsSetup)
            {
                this._logger.Log(LogLevel.Error, "Can't track task -- DB not setup");
                return;
            }

            // Will fill in the db ID
            this._db.AddTaskQueueItem(task);

            if (this._isStarted)
            {
                this.StartTask(task);
            }
        }

        public void TrackImportTask(string rootPath)
        {
            this.TrackTask(new PSTaskQueueItem(
                taskType: TaskType.Import,
                description: "Importing from " + rootPath,
                taskData: new TaskDataImport()
                {
                    RootPath = rootPath
                }));
        }

        public void TrackSendStudyTask(string studyInstanceUID, string aeTarget)
        {
            this.TrackTask(new PSTaskQueueItem(
                taskType: TaskType.SendStudy,
                description: "Sending Study to " + aeTarget + ": " + studyInstanceUID,
                taskData: new TaskDataSendStudy()
                {
                    AETarget = aeTarget,
                    StudyInstanceUID = studyInstanceUID
                }));
        }

        public void TrackDeleteStudyTask(string studyInstanceUID)
        {
            this.TrackTask(new PSTaskQueueItem(
                taskType: TaskType.DeleteStudy,
                description: "Deleting Study: " + studyInstanceUID,
                taskData: new TaskDataDeleteStudy()
                {
                    StudyInstanceUID = studyInstanceUID
                }));
        }

        private void listener_AssociationRequest(DICOMConnection conn)
        {
            //make sure we have the calling AE in our list...
            ApplicationEntity entity = this._db.GetEntity(conn.CallingAE);
            if (this._settings.PromiscuousMode)
            {
                conn.SendAssociateAC();
            }
            else if (entity != null)
            {
                if (conn.CalledAE.Trim() != this._settings.AETitle.Trim())
                {
                    this._logger.Log(LogLevel.Error, "Rejecting Association: Called AE (" + conn.CalledAE + ") doesn't match our AE (" + this._settings.AETitle + ")");
                    conn.SendAssociateRJ(AssociateRJResults.RejectedPermanent, AssociateRJSources.DICOMULServiceProviderPresentation, AssociateRJReasons.CalledAENotRecognized);
                }
                else if (conn.RemoteEndPoint.Address.ToString() != entity.Address)
                {
                    this._logger.Log(LogLevel.Error, "Rejecting Association: Remote Address (" + conn.RemoteEndPoint.Address.ToString() + ") doesn't match AE (" + conn.CallingAE + ")'s Address (" + entity.Address + ")");
                    conn.SendAssociateRJ(AssociateRJResults.RejectedPermanent, AssociateRJSources.DICOMULServiceProviderPresentation, AssociateRJReasons.CallingAENotRecognized);
                }
                else
                {
                    conn.SendAssociateAC();
                }
            }
            else
            {
                this._logger.Log(LogLevel.Error, "Rejecting Association: Couldn't find entity in list with AE title (" + conn.CallingAE + ")");
                conn.SendAssociateRJ(AssociateRJResults.RejectedPermanent, AssociateRJSources.DICOMULServiceProviderPresentation, AssociateRJReasons.CallingAENotRecognized);
            }
        }

        private void listener_StoreRequest(DICOMConnection conn, DICOMData data)
        {
            try
            {
                if (!this._settings.StoreMetadataOnlyFiles && !data.Elements.ContainsKey(DICOMTags.PixelData))
                {
                    this._logger.Log(LogLevel.Info, "Data set has no image data (only metadata). Metadata storing is disabled, so image will not be persisted.");
                    conn.SendCSTORERSP(CommandStatus.Error_MissingAttribute);
                    return;
                }

                if (this._settings.AutoDecompress && data.TransferSyntax.Compression != DICOMSharp.Data.Compression.CompressionInfo.None)
                {
                    this._logger.Log(LogLevel.Info, "Image is compressed, decompressing before storage!");
                    if (!data.Uncompress())
                    {
                        this._logger.Log(LogLevel.Warning, "Image decompression failed! Storing compressed image.");
                    }
                }

                string postName = FileUtil.GenerateFilenameFromImage(data, this._logger);

                //form full file path
                string diskPath = _db.FixImagePath(this._settings.ImageStoragePath);
                if (!diskPath.EndsWith("\\")) diskPath += "\\";
                diskPath += postName;

                data.WriteFile(diskPath, this._logger);

                // Db path can save a ~ path, so recalc without MapPath
                string dbPath = this._settings.ImageStoragePath;
                if (!dbPath.EndsWith("\\")) dbPath += "\\";
                dbPath += postName;

                this._db.PersistImage(data, diskPath, dbPath);

                conn.SendCSTORERSP(CommandStatus.Success);
            }
            catch (Exception e)
            {
                this._logger.Log(LogLevel.Error, "Error in StoreRequest: " + e.ToString());
                conn.SendCSTORERSP(CommandStatus.Error_UnrecognizedOperation);
            }
        }

        private QRResponseData listener_FindRequest(DICOMConnection conn, QRRequestData request)
        {
            return this._db.GetQRResponse(request, false);
        }

        private QRResponseData listener_GetRequest(DICOMConnection conn, QRRequestData request)
        {
            return this._db.GetQRResponse(request, true);
        }

        private QRResponseData listener_MoveRequest(DICOMConnection conn, QRRequestData request)
        {
            return this._db.GetQRResponse(request, true);
        }

        private ApplicationEntity listener_EntityLookup(string aeTitle)
        {
            return this._db.GetEntity(aeTitle);
        }

        private TaskInfo ImportFromPath(string basePath)
        {
            var taskInfo = new TaskInfo()
            {
                Token = new CancellationTokenSource()
            };
            taskInfo.Task = Task.Run(() => 
            {
                taskInfo.Token.Token.ThrowIfCancellationRequested();

                this._logger.Log(LogLevel.Info, "Importing from Path: " + basePath);

                DirectoryInfo di = new DirectoryInfo(basePath);
                if (!di.Exists)
                {
                    this._logger.Log(LogLevel.Error, "Directory not found!");
                    return;
                }
                else
                {
                    FileUtil.ImageAddedHandler callback = (DICOMData data, string path) =>
                    {
                        taskInfo.ProgressCount++;
                        taskInfo.ProgressTotal++;

                        if (!data.Elements.ContainsKey(DICOMTags.PixelData) && !this._settings.StoreMetadataOnlyFiles)
                        {
                            this._logger.Log(LogLevel.Info, "Data set has no image data (only metadata). Metadata storing is disabled, so image will not be persisted.");
                            return;
                        }

                        if (this._settings.AutoDecompress && data.TransferSyntax.Compression != DICOMSharp.Data.Compression.CompressionInfo.None)
                        {
                            this._logger.Log(LogLevel.Info, "Image is compressed, decompressing in place!");

                            DICOMData fullData = new DICOMData();
                            if (fullData.ParseFile(path, true, this._logger))
                            {
                                if (fullData.Uncompress())
                                {
                                    if (fullData.WriteFile(path, this._logger))
                                    {
                                        // Stuff the new decompressed and written file into the DB instead
                                        data = fullData;
                                    }
                                    else
                                    {
                                        this._logger.Log(LogLevel.Warning, "Failed to write out decompressed file! Storing compressed image.");
                                    }
                                }
                                else
                                {
                                    this._logger.Log(LogLevel.Warning, "Image decompression failed! Storing compressed image.");
                                }
                            }
                            else
                            {
                                this._logger.Log(LogLevel.Warning, "Couldn't parse full image data! Storing compressed image.");
                            }
                        }

                        this._db.PersistImage(data, path, path);
                    };

                    FileUtil.ParseAndLoadImagesFromDirectoryRecursively(di, callback, this._logger, taskInfo.Token.Token);
                }

                this._logger.Log(LogLevel.Info, "Done Importing from Path: " + basePath);
            }, taskInfo.Token.Token);

            return taskInfo;
        }

        private TaskInfo SendStudy(string studyInstanceUid, string aeTarget)
        {
            var taskInfo = new TaskInfo()
            {
                Token = new CancellationTokenSource()
            };
            taskInfo.Task = Task.Run(() =>
            {
                taskInfo.Token.Token.ThrowIfCancellationRequested();

                this._logger.Log(LogLevel.Info, "Sending study to " + aeTarget + ": " + studyInstanceUid);

                var remoteAe = this._db.GetEntity(aeTarget);
                if (remoteAe == null)
                {
                    this._logger.Log(LogLevel.Error, "Unknown send target AE: " + aeTarget);
                    return;
                }

                var images = this._db.FetchStudyImages(studyInstanceUid);

                taskInfo.ProgressCount = 0;
                taskInfo.ProgressTotal = images.Count;

                TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
                var sender = new DICOMSender(this._logger, aeTarget + "/" + studyInstanceUid, this._settings.VerboseLogging);
                sender.SCUFinished += (DICOMSCU scu, bool success) =>
                {
                    source.SetResult(success);
                };
                sender.SendUpdate += (DICOMSender senderx, ushort remaining, ushort completed, ushort warned, ushort failed) =>
                {
                    taskInfo.ProgressCount = completed + warned + failed;
                };
                sender.Send(this.GetHostingAE(), remoteAe, images.Select(image => new SendableImage
                    {
                        FilePath = _db.FixImagePath(image.Path),
                        AbstractSyntax = AbstractSyntaxes.Lookup(image.SOPClassID),
                        TransferSyntax = TransferSyntaxes.Lookup(image.TransferSyntaxID)
                    }));

                source.Task.Wait();

                this._logger.Log(LogLevel.Info, "Done sending study to " + aeTarget + ": " + studyInstanceUid);
            }, taskInfo.Token.Token);

            return taskInfo;
        }

        private TaskInfo DeleteStudy(string studyInstanceUid)
        {
            var taskInfo = new TaskInfo()
            {
                Token = new CancellationTokenSource()
            };
            taskInfo.Task = Task.Run(() =>
            {
                taskInfo.Token.Token.ThrowIfCancellationRequested();

                this._logger.Log(LogLevel.Info, "Deleting Study: " + studyInstanceUid);

                var images = this._db.FetchStudyImages(studyInstanceUid);
                taskInfo.ProgressCount = 0;
                taskInfo.ProgressTotal = images.Count;

                foreach (var image in images)
                {
                    File.Delete(_db.FixImagePath(image.Path));
                    this._db.DeleteImage(image);

                    taskInfo.ProgressCount++;
                }

                // TODO: Clean up empty directories when done someday..?

                this._logger.Log(LogLevel.Info, "Done Deleting Study: " + studyInstanceUid);
            }, taskInfo.Token.Token);

            return taskInfo;
        }

        // Note: Dangerously returns mutable object
        public DicomServerSettings GetSettings()
        {
            return this._settings;
        }

        public ApplicationEntity GetHostingAE()
        {
            var aeTitle = this._settings.AETitle;
            var port = (ushort)this._settings.ListenPort;
            return new ApplicationEntity(aeTitle, port);
        }

        public bool IsListening()
        {
            return this._listener.IsListening;
        }
    }
}
