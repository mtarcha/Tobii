using System;
using System.Threading;
using System.Threading.Tasks;

namespace JobManagement.Jobs
{
    public abstract class JobBase : IDisposable
    {
        private bool _isDisposed;
        private Task _worker;
        private readonly object _lock;
        private readonly CancellationTokenSource _cancellationTokenSource;

        protected JobBase()
            : this(new DummyLogger())
        {
        }

        protected JobBase(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Logger = logger;
            Id = Guid.NewGuid();

            _isDisposed = false;
            _lock = new object();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        ~JobBase()
        {
            Dispose(false);
        }

        public Guid Id { get; }

        public Exception Exception { get; private set; }

        public JobStatus Status { get; private set; }

        protected ILogger Logger { get; }

        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        public void Cancel()
        {
            ThrowIfDisposed();
            
            if (Status == JobStatus.Pending || Status == JobStatus.InProgress)
            {
                lock (_lock)
                {
                    Logger.Warn($"Cancel job with Id: {Id}.");

                    if (Status == JobStatus.Pending)
                    {
                        ChangeStatus(JobStatus.Cancelled);
                    }

                    if (Status == JobStatus.InProgress)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void InternalAction(CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Cancel();
                _worker?.Wait();
                _worker?.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            _isDisposed = true;
        }

        internal Task Start()
        {
            _worker = Task.Run(() => Execute(), _cancellationTokenSource.Token);

            return _worker;
        }

        private void Execute()
        {
            lock (_lock)
            {
                if (Status == JobStatus.Pending)
                {
                    ChangeStatus(JobStatus.InProgress);
                }
                else
                {
                    return;
                }
            }

            try
            {
                InternalAction(_cancellationTokenSource.Token);
                ChangeStatus(JobStatus.Success);
            }
            catch (OperationCanceledException)
            {
                ChangeStatus(JobStatus.Cancelled);
            }
            catch (Exception e)
            {
                Exception = e;
                Logger.Error($"Failed to execute job with Id: {Id}", e);
                ChangeStatus(JobStatus.Failed);
            }
        }
        
        private void ChangeStatus(JobStatus newStatus)
        {
            Logger.Info($"Changing job status from '{Status}' to '{newStatus}'. Job Id: {Id}");

            Status = newStatus;

            var handlers = StatusChanged;
            handlers?.Invoke(this, new StatusChangedEventArgs(newStatus));
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                var exception = new ObjectDisposedException(GetType().FullName);
                Logger.Error($"Job with Id: {Id} is disposed.", exception);
                throw exception;
            }
        }
    }
}