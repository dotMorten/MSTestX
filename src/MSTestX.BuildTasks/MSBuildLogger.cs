using MSTestX.Remote;
using Task = Microsoft.Build.Utilities.Task;
namespace MSTestX.BuildTasks
{
    internal class MSBuildLogger : ILog
    {
        private Task task;
        private string? pendingMessage;

        public MSBuildLogger(Task task)
        {
            this.task = task;
        }
        public void LogError(string message, bool partial)
        {
            if (partial)
            {
                pendingMessage = message; return;
            }
            task.Log.LogError(pendingMessage + message);
            pendingMessage = null;
        }

        public void LogInfo(string message, bool partial)
        {
            if (partial)
            {
                pendingMessage = message; return;
            }
            task.Log.LogMessage(pendingMessage + message);
            pendingMessage = null;
        }

        public void LogOk(string message, bool partial = false) => LogInfo(message, partial);

        public void LogWarning(string message, bool partial)
        {
            if (partial)
            {
                pendingMessage = message; return;
            }
            task.Log.LogWarning(pendingMessage + message);
            pendingMessage = null;
        }
    }
}