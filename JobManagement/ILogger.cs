using System;

namespace JobManagement
{
    public interface ILogger
    {
        void Info(string message);

        void Debug(string message);

        void Warn(string message);

        void Error(string message);

        void Error(string message, Exception exception);
    }
}