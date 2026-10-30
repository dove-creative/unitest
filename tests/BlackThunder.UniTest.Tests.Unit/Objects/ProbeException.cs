using System;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class ProbeException : Exception
    {
        public ProbeException()
            : base() { }
        public ProbeException(string message)
            : base(message) { }
    }
}
