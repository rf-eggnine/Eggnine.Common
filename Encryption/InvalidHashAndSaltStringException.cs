// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Eggnine.Common;
public class InvalidHashAndSaltStringException : System.Exception
{
    public InvalidHashAndSaltStringException() : base("The hash and salt string was shorter than the specified salt length") { }
    public InvalidHashAndSaltStringException(string message) : base(message) { }
    public InvalidHashAndSaltStringException(string message, System.Exception inner) : base(message, inner) { }
}
