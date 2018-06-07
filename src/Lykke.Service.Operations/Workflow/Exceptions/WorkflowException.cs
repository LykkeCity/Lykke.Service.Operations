using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Operations.Workflow.Exceptions
{
    public class WorkflowException : Exception
    {
        public string Code { get; }

        public WorkflowException(string code, string message) : base(message)
        {
            Code = code;
        }

        public static string GetExceptionCode(Exception e)
        {
            if (e is WorkflowException wex)
                return wex.Code;

            return "InternalError";
        }
    }
}
