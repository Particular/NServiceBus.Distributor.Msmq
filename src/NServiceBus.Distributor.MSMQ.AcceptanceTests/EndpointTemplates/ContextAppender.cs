﻿namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Diagnostics;
    using AcceptanceTesting;
    using Logging;

    public class ContextAppender : ILoggerFactory, ILog
    {
        public ContextAppender(ScenarioContext context)
        {
            this.context = context;
        }

        void Append(Exception exception)
        {
            lock (context)
            {
                context.Exceptions += exception + "/n/r";
            }
        }

        ScenarioContext context;

        public ILog GetLogger(Type type)
        {
            return this;
        }

        public ILog GetLogger(string name)
        {
            return this;
        }

        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;

        public void Debug(string message)
        {
            Trace.WriteLine(message);
        }

        public void Debug(string message, Exception exception)
        {
            Trace.WriteLine($"{message} {exception}");
            Append(exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format,args));
        }

        public void Info(string message)
        {
            Trace.WriteLine(message);
        }

        public void Info(string message, Exception exception)
        {
            Trace.WriteLine($"{message} {exception}");
            Append(exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }

        public void Warn(string message)
        {
            Trace.WriteLine(message);
        }

        public void Warn(string message, Exception exception)
        {
            Trace.WriteLine($"{message} {exception}");
            Append(exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }

        public void Error(string message)
        {
            Trace.WriteLine(message);
        }

        public void Error(string message, Exception exception)
        {
            Trace.WriteLine($"{message} {exception}");
            Append(exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }

        public void Fatal(string message)
        {
            Trace.WriteLine(message);
        }

        public void Fatal(string message, Exception exception)
        {
            Trace.WriteLine($"{message} {exception}");
            Append(exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }
    }
}