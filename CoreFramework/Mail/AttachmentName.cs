using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Mail
{
    public static class AttachmentName
    {
        public const string tranferEncodingMarker = "B";
        public const string encodingMarker = "UTF-8";
        public const int maxChunkLength = 30;
        public const string softbreak = "?=";
        public static string GetEncodingToken(
            string encodingMarker = AttachmentName.encodingMarker, 
            string tranferEncodingMarker = AttachmentName.tranferEncodingMarker)
        {
            return String.Format("=?{0}?{1}?", encodingMarker, tranferEncodingMarker);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodingtoken"></param>
        /// <param name="softbreak"></param>
        /// <param name="maxChunkLength"></param>
        /// <param name="encodedName">Encoded to base64 attachment name</param>
        /// <returns></returns>
        public static string GetEncodedAttachmentName(string encodingtoken, string softbreak, int maxChunkLength, string encodedName)
        {
            if (encodingtoken == null)
                throw new ArgumentNullException(nameof(encodingtoken));

            if (softbreak == null)
                throw new ArgumentNullException(nameof(softbreak));

            if (encodedName == null)
                return null;

            int splitLength = maxChunkLength - encodingtoken.Length - (softbreak.Length * 2);
            IEnumerable<string> parts = encodedName.SplitByLength(splitLength);

            string encodedAttachmentName = encodingtoken;

            foreach (string part in parts)
            {
                encodedAttachmentName += part + softbreak + encodingtoken;
            }

            encodedAttachmentName = encodedAttachmentName.Remove(encodedAttachmentName.Length - encodingtoken.Length, encodingtoken.Length);

            return encodedAttachmentName;
        }

        public static string GetAttachmentName(string encodingtoken, string softbreak, int maxChunkLength, string attachmentName)
        {
            return GetEncodedAttachmentName(encodingtoken, softbreak, maxChunkLength,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(attachmentName))));
        }
    }
}
