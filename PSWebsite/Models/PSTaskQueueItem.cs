using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCommon.Models
{
    public enum TaskType
    {
        Import = 1,
        SendStudy = 2,
        DeleteStudy = 3
    }
    
    public class TaskDataImport
    {
        public string RootPath;
    }

    public class TaskDataSendStudy
    {
        public string StudyInstanceUID;
        public string AETarget;
    }

    public class TaskDataDeleteStudy
    {
        public string StudyInstanceUID;
    }

    public class PSTaskQueueItem
    {
        public uint TaskId;
        public string Description;
        public TaskType TaskType;
        public string TaskDataSerialized;

        public PSTaskQueueItem()
        {
        }

        public PSTaskQueueItem(TaskType taskType, string description, object taskData)
        {
            TaskType = taskType;
            Description = description;
            TaskDataSerialized = JsonConvert.SerializeObject(taskData);
        }

        public PSTaskQueueItem(DbDataReader reader)
        {
            TaskId = Convert.ToUInt32(reader["TaskId"]);
            Description = (string)reader["Description"];
            TaskType = (TaskType) Convert.ToInt32(reader["TaskType"]);
            TaskDataSerialized = (string)reader["TaskDataSerialized"];
        }
    }
}
