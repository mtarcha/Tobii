using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JobManagement;
using JobManagement.Jobs;
using NUnit.Framework;

namespace JobManagementTests
{
    [TestFixture]
    public class JobManagerTests
    {
        private JobManager _jobManager;

        [SetUp]
        public void Setup()
        {
            _jobManager = new JobManager();
        }

        [TearDown]
        public void Teardown()
        {
            _jobManager.Dispose();
        }

        [Test]
        public void PushForExecution_JobsInKnownOrder_JobsExecutedInRightOrder()
        {
            var resultBuiler = new StringBuilder();
            var jobs = CreateJobs(10, (index, token) =>
            {
                resultBuiler.Append(index);
            });

            ExecuteAll(jobs);

            Assert.AreEqual("0123456789", resultBuiler.ToString());
            Assert.IsTrue(jobs.All(job => job.Status == JobStatus.Success));
        }

        [Test]
        public void PushForExecution_SeveralJobsPushed_OneJobAtTimeExecuted()
        {
            var runningTasksCount = 0;
            var jobs = CreateJobs(10,
                () =>
                {
                    Interlocked.Increment(ref runningTasksCount);
                    Thread.Sleep(10); 

                    return runningTasksCount;
                },
                () => { Interlocked.Decrement(ref runningTasksCount); });

            ExecuteAll(jobs);

            Assert.AreEqual(0, runningTasksCount);
            Assert.IsTrue(jobs.All(job => job.Result == 1));
            Assert.IsTrue(jobs.All(job => job.Status == JobStatus.Success));
        }

        [Test]
        public void PushForExecution_PushJobsInParallel_AllJobsExecuted()
        {
            var runJobsCount = 0;
            var jobs = CreateJobs(10, (_, __) => { Interlocked.Increment(ref runJobsCount); });

            Parallel.ForEach(jobs, job => _jobManager.PushForExecution(job));

            WaitForCompletion();

            Assert.AreEqual(10, runJobsCount);
            Assert.IsTrue(jobs.All(job => job.Status == JobStatus.Success));
        }

        [Test]
        public void PushForExecution_JobsThrowsExceptions_JobsCompletedWithException()
        {
            var jobs = CreateJobs(3, (_, __) => { throw new AccessViolationException(); });

            ExecuteAll(jobs);

            Assert.IsTrue(jobs.All(job => job.Status == JobStatus.Failed));
            CollectionAssert.AllItemsAreInstancesOfType(jobs.Select(job => job.Exception), typeof(AccessViolationException));
        }
        
        [Test]
        public void PushForExecution_NullJob_ArgumentNullExceptionThrown()
        {
            Assert.Throws<ArgumentNullException>(() => _jobManager.PushForExecution(null));
        }

        [Test]
        public void Dispose_JobsHandleCancellationToken_JobsCancelled()
        {
            var jobs = CreateJobs(10, (index, token) =>
            {
                Thread.Sleep(100);
                token.ThrowIfCancellationRequested();
            });

            foreach (var job in jobs)
            {
                _jobManager.PushForExecution(job);
            }

            Thread.Sleep(10);
            _jobManager.Dispose();

            Assert.IsTrue(jobs.All(job => job.Status == JobStatus.Cancelled));
        }

        [Test]
        public void Dispose_JobDoNotHandleCancellationToken_JobIsSuccessfullyCompleted()
        {
            var job = new LambdaJob(() => { Thread.Sleep(100); });

            _jobManager.PushForExecution(job);

            Thread.Sleep(10);
            _jobManager.Dispose();

           Assert.AreEqual(JobStatus.Success, job.Status);
        }

        [Test]
        public void Dispose_OneJobRunningOthersPending_ActiveJobCompletedOthersCancelled()
        {
            var activeJob = new LambdaJob(() => { Thread.Sleep(100); });
            var pendingJobs = CreateJobs(10, (_, __) => { });

            _jobManager.PushForExecution(activeJob);

            foreach (var job in pendingJobs)
            {
                _jobManager.PushForExecution(job);
            }

            Thread.Sleep(10);
            _jobManager.Dispose();

            Assert.AreEqual(JobStatus.Success, activeJob.Status);
            Assert.IsTrue(pendingJobs.All(job => job.Status == JobStatus.Cancelled));
        }

        private static LambdaJob[] CreateJobs(int count, Action<int, CancellationToken> internalAction)
        {
            var jobs = new LambdaJob[count];
            for (var i = 0; i < count; i++)
            {
                var index = i;
                jobs[i] = new LambdaJob(token => { internalAction(index, token); });
            }

            return jobs;
        }

        private static LambdaJob<T>[] CreateJobs<T>(int count, Func<T> internalAction, Action onCompleted)
        {
            var jobs = new LambdaJob<T>[count];
            for (var i = 0; i < count; i++)
            {
                var job = new LambdaJob<T>(internalAction);
                job.StatusChanged += (_, args) =>
                {
                    if (args.Status.IsCompleted())
                    {
                        onCompleted();
                    }
                }; 
                jobs[i] = job;
            }

            return jobs;
        }

        private void ExecuteAll(IEnumerable<JobBase> jobs)
        {
            var awaiter = new AutoResetEvent(false);
            jobs.Last().StatusChanged += (_, args) =>
            {
                if (args.Status.IsCompleted())
                {
                    awaiter.Set();
                }
            };

            foreach (var job in jobs)
            {
                _jobManager.PushForExecution(job);
            }

            awaiter.WaitOne();
        }

        private void WaitForCompletion() 
        {
            var awaiter = new AutoResetEvent(false);
            var lastJob = new LambdaJob(() => { });
            lastJob.StatusChanged += (_, args) =>
            {
                if (args.Status.IsCompleted())
                {
                    awaiter.Set();
                }
            };

            _jobManager.PushForExecution(lastJob);
            awaiter.WaitOne();
        }
    }
}