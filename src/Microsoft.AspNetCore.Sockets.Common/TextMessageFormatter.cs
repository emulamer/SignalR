using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Sockets
{
    public static class TextMessageFormatter
    {
        public static bool TryFormatMessage(Message message, Span<byte> buffer, out int bytesWritten)
        {
            // Write the length as a string
            int written = 0;
            if(!message.Payload.Buffer.Length.TryFormat(buffer, out int lengthLen, default(TextFormat), EncodingData.InvariantUtf8))
            {
                bytesWritten = 0;
                return false;
            }
            written += lengthLen;
            buffer = buffer.Slice(lengthLen);

            // We need at least 4 more characters of space (':', type flag, ':', and eventually the terminating ';')
            // We'll still need to double-check that we have space for the terminator after we write the payload,
            // but this way we can exit early if the buffer is way too small.
            if(buffer.Length < 4)
            {
                // Discard everything we've written. We don't need to clear it though, we just return 0 bytes written.
                bytesWritten = 0;
                return false;
            }
            buffer[0] = (byte)':';
            buffer[1] = GetTypeCharacter(message.Type);
            buffer[2] = (byte)':';
            buffer = buffer.Slice(3);
            written += 3;

            // Payload
            if(message.Type == MessageType.Binary)
            {
                // TODO: After https://github.com/aspnet/CoreCLR/pull/150 is merged we can use the Span-aware Base64 encoder
                var payload = Convert.ToBase64String(message.Payload.Buffer.ToArray());
                if(!TextEncoder.Utf8.TryEncodeString(payload, buffer, out int payloadWritten))
                {
                    bytesWritten = 0;
                    return false;
                }
                written += payloadWritten;
                buffer = buffer.Slice(payloadWritten);
            } else
            {
                if(buffer.Length < message.Payload.Buffer.Length)
                {
                    bytesWritten = 0;
                    return false;
                }
                message.Payload.Buffer.CopyTo(buffer.Slice(0, message.Payload.Buffer.Length));
                written += message.Payload.Buffer.Length;
                buffer = buffer.Slice(message.Payload.Buffer.Length);
            }

            // Terminator
            if(buffer.Length < 1)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = (byte)';';
            bytesWritten = written + 1;
            return true;
        }

        internal static bool TryParseMessage(ReadOnlySpan<byte> buffer, out Message message, out int bytesConsumed)
        {
            throw new NotImplementedException();
        }

        private static PreservedBuffer ParsePayload(ReadOnlySpan<byte> data, int length, MessageType messageType, ref int cursor)
        {
            int start = cursor;

            // We know exactly where the end is. The last byte is cursor + length
            cursor += length;

            // Verify the length and trailer
            if (cursor >= data.Length)
            {
                throw new FormatException("Unexpected end-of-message while reading Payload field.");
            }
            if (data[cursor] != ';')
            {
                throw new FormatException("Payload is missing trailer character ';'.");
            }

            // Read the data into a buffer
            var buffer = new byte[length];
            data.Slice(start, length).CopyTo(buffer);

            // If the message is binary, we need to convert from Base64
            if (messageType == MessageType.Binary)
            {
                // TODO: Use System.Binary.Base64 to handle this with less allocation

                // Parse the data as Base64
                var str = Encoding.UTF8.GetString(buffer);
                buffer = Convert.FromBase64String(str);
            }

            return ReadableBuffer.Create(buffer).Preserve();
        }

        private static MessageType ParseType(ReadOnlySpan<byte> data, ref int cursor)
        {
            int start = cursor;

            // Scan to a ':'
            cursor = IndexOf((byte)':', data, cursor);
            if (cursor >= data.Length)
            {
                throw new FormatException("Unexpected end-of-message while reading Type field.");
            }

            if (cursor - start != 1)
            {
                throw new FormatException("Type field must be exactly one byte long.");
            }

            switch (data[cursor - 1])
            {
                case (byte)'T': return MessageType.Text;
                case (byte)'B': return MessageType.Binary;
                case (byte)'C': return MessageType.Close;
                case (byte)'E': return MessageType.Error;
                default: throw new FormatException($"Unknown Type value: '{(char)data[cursor - 1]}'.");
            }
        }

        private static int ParseLength(ReadOnlySpan<byte> data, ref int cursor)
        {
            int start = cursor;

            // Scan to a ':'
            cursor = IndexOf((byte)':', data, cursor);
            if (cursor >= data.Length)
            {
                throw new FormatException("Unexpected end-of-message while reading Length field.");
            }

            // Parse the length
            int length = 0;
            for (int i = start; i < cursor; i++)
            {
                if (data[i] < '0' || data[i] > '9')
                {
                    throw new FormatException("Invalid length.");
                }
                length = (length * 10) + (data[i] - '0');
            }

            return length;
        }


        private static byte GetTypeCharacter(MessageType type)
        {
            switch (type)
            {
                case MessageType.Text: return (byte)'T';
                case MessageType.Binary: return (byte)'B';
                case MessageType.Close: return (byte)'C';
                case MessageType.Error: return (byte)'E';
                default: throw new InvalidOperationException($"Unknown message type: {type}");
            }
        }
    }
}