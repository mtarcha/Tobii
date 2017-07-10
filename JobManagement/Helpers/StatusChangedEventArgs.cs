namespace JobManagement
{
    public class StatusChangedEventArgs
    {
        public StatusChangedEventArgs(JobStatus status)
        {
            Status = status;
        }

        public JobStatus Status { get; }
    }
}