using System.Threading;
using JobManagement;
using JobManagement.Jobs;
using NUnit.Framework;

namespace JobManagementTests
{
    [TestFixture]
    public class JobTests
    {
        [Test]
        public void Cancel_JobsCancelledBeforeRunning_JobInCanceledStatus()
        {
            var job = new LambdaJob(() => { });
            
            job.Cancel();
            job.Start().Wait();
            
            Assert.AreEqual(JobStatus.Cancelled, job.Status);
        }

        [Test]
        public void Cancel_JobHandlesCancellationToken_JobInCancelledStatus()
        {
            var job = new LambdaJob<int>(token =>
            {
                token.ThrowIfCancellationRequested();

                return 0;
            });
           
            job.StatusChanged += (sender, args) =>
            {
                if (args.Status == JobStatus.InProgress)
                {
                    job.Cancel();
                }
            };

            job.Start().Wait();
            
            Assert.AreEqual(JobStatus.Cancelled, job.Status);
        }

        [Test]
        public void Cancel_JobIgnoresCancellationToken_JobInSuccessStatus()
        {
            var job = new LambdaJob(() => { Thread.Sleep(100); });
            
            job.StatusChanged += (sender, args) =>
            {
                if (args.Status == JobStatus.InProgress)
                {
                    job.Cancel();
                }
            };

            job.Start().Wait();
            
            Assert.AreEqual(JobStatus.Success, job.Status);
        }
    }
}