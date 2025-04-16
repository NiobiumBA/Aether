using System;

namespace Aether
{
	public class InvalidNetworkDataException : Exception
	{
		public InvalidNetworkDataException() : base("Invalid network data") { }
		public InvalidNetworkDataException(string message) : base(message) { }
		public InvalidNetworkDataException(string message, Exception inner) : base(message, inner) { }
	}
}
