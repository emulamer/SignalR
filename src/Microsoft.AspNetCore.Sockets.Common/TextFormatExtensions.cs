using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Sockets
{
    internal static class TextFormatExtensions
    {
        public static bool IsTextMessageFormat(this TextFormat format)
        {
            return format == Message.TextFormat;
        }

        public static bool IsBinaryMessageFormat(this TextFormat format)
        {
            return format == Message.BinaryFormat;
        }
    }
}
