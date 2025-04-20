// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

namespace Eggnine.Common;
public class InvalidHashAndSaltStringException : System.Exception
{
    public InvalidHashAndSaltStringException() : base("The hash and salt string was shorter than the specified salt length") { }
    public InvalidHashAndSaltStringException(string message) : base(message) { }
    public InvalidHashAndSaltStringException(string message, System.Exception inner) : base(message, inner) { }
}
