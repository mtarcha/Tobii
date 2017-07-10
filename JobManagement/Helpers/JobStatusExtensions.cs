namespace JobManagement
{
    public static class JobStatusExtensions
    {
        public static bool IsCompleted(this JobStatus status)
        {
            return status == JobStatus.Cancelled
                   || status == JobStatus.Failed
                   || status == JobStatus.Success;
        }
    }
}