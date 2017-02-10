// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;
using System.Text.Formatting;

namespace Microsoft.AspNetCore.Sockets
{
    public struct Message : IDisposable, IBufferFormattable
    {
        public static readonly TextFormat BinaryFormat = new TextFormat('B');
        public static readonly TextFormat TextFormat = new TextFormat('T');

        public bool EndOfMessage { get; }
        public MessageType Type { get; }
        public PreservedBuffer Payload { get; }

        public Message(PreservedBuffer payload, MessageType type)
            : this(payload, type, endOfMessage: true)
        {

        }

        public Message(PreservedBuffer payload, MessageType type, bool endOfMessage)
        {
            Type = type;
            EndOfMessage = endOfMessage;
            Payload = payload;
        }

        public void Dispose()
        {
            Payload.Dispose();
        }

        public bool TryFormat(Span<byte> buffer, out int written, TextFormat format, EncodingData encoding)
        {
            // REVIEW: We only support UTF-8 encoding, so throw if `format` is text and `encoding` != UTF-8?
            return format.IsBinaryMessageFormat() ?
                throw new NotImplementedException("Binary format not yet implemented") :
                TextMessageFormatter.TryFormatMessage(this, buffer, out written);
        }

        internal static bool TryParse(ReadOnlySpan<byte> buffer, out Message message, out int bytesConsumed, TextFormat format, EncodingData encoding)
        {
            // REVIEW: We only support UTF-8 encoding, so throw if `format` is text and `encoding` != UTF-8?
            return format.IsBinaryMessageFormat() ?
                throw new NotImplementedException("Binary format not yet implemented") :
                TextMessageFormatter.TryParseMessage(buffer, out message, out bytesConsumed);
        }
    }
}
