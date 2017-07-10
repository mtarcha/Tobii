using System;
using System.Threading;

namespace JobManagement.Jobs
{
    public sealed class LambdaJob<TResult> : JobBase
    {
        private readonly Func<CancellationToken, TResult> _func;

        public LambdaJob(Func<TResult> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            _func = token => func();
        }

        public LambdaJob(Func<CancellationToken, TResult> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            _func = func;
        }

        public TResult Result { get; private set; }

        protected override void InternalAction(CancellationToken cancellationToken)
        {
            Result = _func(cancellationToken);
        }
    }
}