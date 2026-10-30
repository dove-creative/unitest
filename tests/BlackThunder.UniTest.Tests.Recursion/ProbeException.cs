using System;

namespace BlackThunder.UniTest.Tests.Recursion
{
    public class ProbeException : Exception
    {
        public ProbeException()
            : base() { }
        public ProbeException(string message)
            : base(message) { }
    }
}
