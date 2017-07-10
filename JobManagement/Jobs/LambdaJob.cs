using System;
using System.Threading;

namespace JobManagement.Jobs
{
    public sealed class LambdaJob : JobBase
    {
        private readonly Action<CancellationToken> _action;

        public LambdaJob(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _action = token => action();
        }

        public LambdaJob(Action<CancellationToken> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _action = action;
        }

        protected override void InternalAction(CancellationToken cancellationToken)
        {
            _action(cancellationToken);
        }
    }
}