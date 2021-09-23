using System;
using static SevenZip.Compression.LZMA.DataHelper;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface
{
    /// <summary>
    /// An envelope for sending data to streams.
    /// Contains the type of the data being sent and the data itself.
    /// </summary>
    [Serializable]
    public class Envelope
    {
        /// <summary>
        /// Empty constructor. Only for serialization, do not use.
        /// </summary>
        public Envelope()
        {
        }

        /// <summary>
        /// Base constructor for requests.
        /// </summary>
        /// <param name="dataTypeHint">The type hint of the data.</param>
        /// <param name="data">The data to be contained in this event.</param>
        /// <remarks>
        /// The data type hint is a hint for the recipient on how to process the data.
        /// </remarks>
        public Envelope(string dataTypeHint, object data)
        {
            Data = Pack(data);
            DataType = data.GetType();
            CorrelationId = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow;
            Type = dataTypeHint;
        }

        /// <summary>
        /// The correlation ID of this request.
        /// </summary>
        /// <remarks>
        /// Relevant for logging and for correlating responses with requests if necessary.
        /// </remarks>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// The data in this event.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The type of data in this event.
        /// </summary>
        public Type DataType { get; set; }

        /// <summary>
        /// The envelope type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The timestamp of this event.
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}