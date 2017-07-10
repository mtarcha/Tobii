using System;
using System.Runtime.CompilerServices;

namespace JobManagement
{
    public class DummyLogger : ILogger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warn(string message)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, Exception exception)
        {
        }
    }
}