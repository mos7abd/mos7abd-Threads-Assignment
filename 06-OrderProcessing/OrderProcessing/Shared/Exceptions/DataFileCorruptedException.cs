using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public class DataFileCorruptedException : Exception
    {
        public DataFileCorruptedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}
