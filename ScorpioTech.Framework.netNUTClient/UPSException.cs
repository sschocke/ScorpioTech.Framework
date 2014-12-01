using System;

namespace ScorpioTech.Framework.netNUTClient
{
    /// <summary>
    /// Exception class for errors raised by the UPSD daemon
    /// </summary>
    public class UPSException : Exception
    {
        /// <summary>
        /// List of possible error codes returned by UPSD
        /// </summary>
        public enum ErrorCode
        {
            /// <summary>
            /// The client’s host and/or authentication details (username, password) are not sufficient to execute the requested command.
            /// </summary>
            ACCESS_DENIED,
            /// <summary>
            /// The UPS specified in the request is not known to upsd. This usually means that it didn’t match anything in ups.conf.
            /// </summary>
            UNKNOWN_UPS,
            /// <summary>
            /// The specified UPS doesn’t support the variable in the request.
            /// This is also sent for unrecognized variables which are in a space which is handled by upsd, such as server.*.
            /// </summary>
            VAR_NOT_SUPPORTED,
            /// <summary>
            /// The specified UPS doesn’t support the instant command in the request.
            /// </summary>
            CMD_NOT_SUPPORTED,
            /// <summary>
            /// The client sent an argument to a command which is not recognized or is otherwise invalid in this context. This is typically caused by sending a valid command like GET with an invalid subcommand.
            /// </summary>
            INVALID_ARGUMENT,
            /// <summary>
            /// upsd failed to deliver the instant command request to the driver. No further information is available to the client. This typically indicates a dead or broken driver.
            /// </summary>
            INSTCMD_FAILED,
            /// <summary>
            /// upsd failed to deliver the set request to the driver. This is just like INSTCMD-FAILED above.
            /// </summary>
            SET_FAILED,
            /// <summary>
            /// The requested variable in a SET command is not writable.
            /// </summary>
            READONLY,
            /// <summary>
            /// The requested value in a SET command is too long.
            /// </summary>
            TOO_LONG,
            /// <summary>
            /// This instance of upsd does not support the requested feature. This is only used for TLS/SSL mode (STARTTLS) at the moment.
            /// </summary>
            FEATURE_NOT_SUPPORTED,
            /// <summary>
            /// This instance of upsd hasn’t been configured properly to allow the requested feature to operate. This is also limited to STARTTLS for now.
            /// </summary>
            FEATURE_NOT_CONFIGURED,
            /// <summary>
            /// TLS/SSL mode is already enabled on this connection, so upsd can’t start it again.
            /// </summary>
            ALREADY_SSL_MODE,
            /// <summary>
            /// upsd can’t perform the requested command, since the driver for that UPS is not connected. This usually means that the driver is not running, or if it is, the ups.conf is misconfigured.
            /// </summary>
            DRIVER_NOT_CONNECTED,
            /// <summary>
            /// upsd is connected to the driver for the UPS, but that driver isn’t providing regular updates or has specifically marked the data as stale. upsd refuses to provide variables on stale units to avoid false readings.
            /// This generally means that the driver is running, but it has lost communications with the hardware. Check the physical connection to the equipment.
            /// </summary>
            DATA_STALE,
            /// <summary>
            /// The client already sent LOGIN for a UPS and can’t do it again. There is presently a limit of one LOGIN record per connection.
            /// </summary>
            ALREADY_LOGGED_IN,
            /// <summary>
            /// The client sent an invalid PASSWORD - perhaps an empty one.
            /// </summary>
            INVALID_PASSWORD,
            /// <summary>
            /// The client already set a PASSWORD and can’t set another. This also should never happen with normal NUT clients.
            /// </summary>
            ALREADY_SET_PASSWORD,
            /// <summary>
            /// The client sent an invalid USERNAME.
            /// </summary>
            INVALID_USERNAME,
            /// <summary>
            ///  The client has already set a USERNAME, and can’t set another. This should never happen with normal NUT clients.
            /// </summary>
            ALREADY_SET_USERNAME,
            /// <summary>
            /// The requested command requires a username for authentication, but the client hasn’t set one.
            /// </summary>
            USERNAME_REQUIRED,
            /// <summary>
            ///  The requested command requires a passname for authentication, but the client hasn’t set one.
            /// </summary>
            PASSWORD_REQUIRED,
            /// <summary>
            /// upsd doesn’t recognize the requested command.
            /// This can be useful for backwards compatibility with older versions of upsd. Some NUT clients will try GET and fall back on REQ after receiving this response.
            /// </summary>
            UNKNOWN_COMMAND,
            /// <summary>
            ///  The value specified in the request is not valid. This usually applies to a SET of an ENUM type which is using a value which is not in the list of allowed values.
            /// </summary>
            INVALID_VALUE,
            /// <summary>
            /// The upsd server returned an error code that could not be matched .
            /// </summary>
            UNKNOWN_ERROR = 0xFF
        }

        /// <summary>
        /// UPSD Error Code
        /// </summary>
        public ErrorCode Code { get; private set; }
        /// <summary>
        /// Get a more Human Readable error description
        /// </summary>
        public string Description
        {
            get
            {
                // TODO: Still need to add friendly messages for all the error codes returned by UPSD
                switch(this.Code)
                {
                    case ErrorCode.ACCESS_DENIED:
                        return "Access Denied";
                    case ErrorCode.UNKNOWN_UPS:
                        return "Unknown UPS";
                    case ErrorCode.VAR_NOT_SUPPORTED:
                        return "Variable not supported by UPS";
                    case ErrorCode.CMD_NOT_SUPPORTED:
                        return "Instant Command not supported by UPS";
                    default:
                        return this.Code.ToString();
                }
            }
        }

        /// <summary>
        /// Create a new UPSException
        /// </summary>
        /// <param name="code">UPSD Error Code</param>
        /// <param name="message">Possible error message returned by UPSD</param>
        public UPSException(ErrorCode code, string message)
            :base(message)
        {
            this.Code = code;
        }
    }
}
