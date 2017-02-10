using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Sockets
{
    public static class MessageBatchFormatter
    {
        public static bool TryFormatMessages(IEnumerable<Message> messages, Span<byte> buffer, out int bytesWritten, TextFormat format, EncodingData encoding = default(EncodingData))
        {
            if(!format.IsTextMessageFormat() && !format.IsBinaryMessageFormat())
            {
                throw new FormatException($"Unknown Message format: {format}");
            }

            if(buffer.Length < 1)
            {
                bytesWritten = 0;
                return false;
            }

            // Write the format discriminator
            buffer[0] = (byte)format.Symbol;
            buffer = buffer.Slice(1);

            // Write messages
            var writtenSoFar = 1;
            foreach(var message in messages)
            {
                if(!message.TryFormat(buffer, out int writtenForMessage, format, encoding))
                {
                    bytesWritten = 0;
                    return false;
                }
                writtenSoFar += writtenForMessage;
            }

            bytesWritten = writtenSoFar;
            return true;
        }

        public static bool TryParseMessages(ReadOnlySpan<byte> buffer, out IList<Message> messages, out int bytesConsumed, EncodingData encoding)
        {
            var readMessages = new List<Message>();
            if(buffer.Length < 1)
            {
                // Batch is missing the prefix
                bytesConsumed = 0;
                messages = new List<Message>();
                return false;
            }

            TextFormat messageFormat;
            if(buffer[0] == Message.TextFormat.Symbol)
            {
                messageFormat = Message.TextFormat;
            }
            else if(buffer[0] == Message.BinaryFormat.Symbol)
            {
                messageFormat = Message.BinaryFormat;
            }

            var consumedSoFar = 1;
            buffer = buffer.Slice(1);

            while(Message.TryParse(buffer, out Message message, out int consumedForMessage, messageFormat, EncodingData.InvariantUtf8))
            {
                readMessages.Add(message);
                consumedSoFar += consumedForMessage;
                buffer = buffer.Slice(consumedForMessage);
            }

            if(buffer.Length < 1 || buffer[0] != (byte)';')
            {
                // Batch is invalid, missing the terminator
                bytesConsumed = 0;
                messages = new List<Message>();
                return false;
            }

            bytesConsumed = consumedSoFar + 1;
            messages = readMessages;
            return true;
        }
    }
}
