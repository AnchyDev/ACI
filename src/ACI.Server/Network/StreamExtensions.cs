using System.Net.Sockets;
using System.Text;

namespace ACI.Server.Network
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads a string from a NetworkStream.
        /// </summary>
        /// <param name="stream">The network stream to read from.</param>
        /// <param name="encoding">The encoding used to encode the string data. Default is UTF8 if null.</param>
        /// <returns>The encoded string.</returns>
        public static async Task<string> ReadStringAsync(this NetworkStream stream, Encoding? encoding = null)
        {
            var sizeBuf = new byte[sizeof(uint)];
            await stream.ReadAsync(sizeBuf);
            var stringSize = BitConverter.ToUInt32(sizeBuf);

            var msgBuf = new byte[stringSize];
            await stream.ReadAsync(msgBuf);

            if(encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(msgBuf);
        }

        public static async Task<string> ReadStringAsync(this NetworkStream stream, uint length, Encoding? encoding = null)
        {
            var msgBuf = new byte[length];
            await stream.ReadAsync(msgBuf);

            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(msgBuf);
        }

        public static async Task<uint> ReadUInt32Async(this NetworkStream stream)
        {
            var buf = new byte[sizeof(uint)];
            await stream.ReadAsync(buf);

            return BitConverter.ToUInt32(buf);
        }
    }
}
