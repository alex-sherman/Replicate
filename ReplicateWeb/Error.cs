using Replicate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate.Web
{
    [ReplicateType]
    public class ErrorData
    {
        public ErrorData Inner { get; }
        public string Message { get; }
        public string Stack { get; }
        public ErrorData(Exception exception)
        {
            if (exception.InnerException != null)
                Inner = new ErrorData(exception.InnerException);
            Message = exception.Message;
            // TODO: Turn off stack traces in production probably eventually
            Stack = exception.StackTrace;
        }
    }
    public class HTTPError : Exception
    {
        public int Status = 500;
        public HTTPError(string message, int status = 500) : base(message) { Status = status; }
    }
}
