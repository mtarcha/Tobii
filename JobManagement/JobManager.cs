using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JobManagement.Jobs;

namespace JobManagement
{
    public sealed class JobManager : IDisposable
    {
        private readonly BlockingCollection<JobBase> _jobs;
        private readonly ILogger _logger;
        private readonly Task _worker;

        private bool _isDisposed;
        private JobBase _activeJob;

        public JobManager()
            : this(new DummyLogger())
        {
        }

        public JobManager(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            
            _logger = logger;
            _isDisposed = false;
            _jobs = new BlockingCollection<JobBase>();
            _worker = Task.Run(async () => await HandleQueue());
        }

        public void PushForExecution(JobBase job)
        {
            ThrowIfDisposed();

            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            _jobs.Add(job);

            _logger.Info($"Job with Id: {job.Id} was pushed to queue.");
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            
            _jobs.CompleteAdding();
            foreach (var job in _jobs)
            {
                job.Dispose();
            }

            _activeJob?.Dispose();

            _worker.Wait();
            _worker.Dispose();
            _jobs.Dispose();
        }

        private async Task HandleQueue()
        {
            foreach (var job in _jobs.GetConsumingEnumerable())
            {
                _activeJob = job;

                try
                {
                    _logger.Info($"Starting the execution of job with id: {_activeJob.Id}");

                    await _activeJob.Start();
                }
                catch (Exception e)
                {
                    _logger.Error($"The exception was thrown during handling job with Id: {_activeJob.Id}", e);
                }
                finally
                {
                    _activeJob = null;
                }

                if (_isDisposed)
                {
                    break;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                var exception = new ObjectDisposedException(GetType().FullName);
                _logger.Error(exception.Message);
                throw exception;
            }
        }
    }
}