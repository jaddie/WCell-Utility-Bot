using System;
using System.Collections.Specialized;
using System.Text;
using OpenPOP.MIME.Decode;
using OpenPOP.MIME.Header;

namespace OpenPOP.MIME
{
	public class Attachment : IComparable<Attachment>
	{
		#region Member Variables
        private const string _defaultMIMEFileName = "body.eml";
        private const string _defaultReportFileName = "report.htm";
        private const string _defaultFileName = "body.htm";
	    #endregion

		#region Properties
        /// <summary>
        /// Headers for this Attachment
        /// </summary>
        public MessageHeader Headers { get; private set; }

	    /// <summary>
	    /// Content File Name
	    /// </summary>
	    public string ContentFileName { get; private set; }

	    /// <summary>
	    /// Raw Content
	    /// Full Attachment, with headers and everything.
	    /// The raw string used to create this attachment
	    /// </summary>
	    public string RawContent { get; private set; }

	    /// <summary>
	    /// Raw Attachment Content (headers removed if was specified at creation)
	    /// </summary>
	    public string RawAttachment { get; private set; }
		#endregion

        #region Constructors
        /// <summary>
        /// Used to create a new attachment internally to avoid any
        /// duplicate code for setting up an attachment
        /// </summary>
        /// <param name="strFileName">file name</param>
        private Attachment(string strFileName)
        {
            // Setup defaults
            RawAttachment = null;
            RawContent = null;

            // Setup parameters
            ContentFileName = strFileName;
        }

		/// <summary>
		/// Create an Attachment from byte contents. These are NOT parsed in any way, but assumed to be correct.
		/// This is used for MS-TNEF attachments
		/// </summary>
		/// <param name="bytAttachment">attachment bytes content</param>
		/// <param name="strFileName">file name</param>
		/// <param name="strContentType">content type</param>
		public Attachment(byte[] bytAttachment, string strFileName, string strContentType)
            : this(strFileName)
		{
            string bytesInAString = Encoding.Default.GetString(bytAttachment);
		    RawContent = bytesInAString;
		    RawAttachment = bytesInAString;
		    Headers = new MessageHeader(HeaderFieldParser.ParseContentType(strContentType));
		}

	    /// <summary>
	    /// Create an attachment from a string, with some headers use from the message it is inside
	    /// </summary>
	    /// <param name="strAttachment">attachment content</param>
	    /// <param name="headersFromMessage">The attachments headers defaults to some of the message headers, this is the headers from the message</param>
	    public Attachment(string strAttachment, MessageHeader headersFromMessage)
            : this("")
		{
            if (strAttachment == null)
                throw new ArgumentNullException("strAttachment");

            RawContent = strAttachment;

            string rawHeaders;
            NameValueCollection headers;
            HeaderExtractor.ExtractHeaders(strAttachment, out rawHeaders, out headers);

            Headers = new MessageHeader(headers, headersFromMessage.ContentType, headersFromMessage.ContentTransferEncoding);

            // If we parsed headers, as we just did, the RawAttachment is found by removing the headers and trimming
		    RawAttachment = Utility.ReplaceFirstOccurrance(strAttachment, rawHeaders, "");

            // Set the filename
	        ContentFileName = FigureOutFilename(Headers);
		}
        #endregion

        /// <summary>
        /// This method is responsible for picking a good name for an Attachment
        /// based on the headers of it
        /// </summary>
        /// <param name="headers">The headers that can be used to give a reasonable name</param>
        /// <returns>A name to use for an Attachment with the headers given</returns>
        private static string FigureOutFilename(MessageHeader headers)
        {
            // There is a name field in the ContentType
            if (!string.IsNullOrEmpty(headers.ContentType.Name))
                return headers.ContentType.Name;
            
            // There is a FileName in the ContentDisposition
            if(headers.ContentDisposition != null)
                return headers.ContentDisposition.FileName;

            // We could not find any given name. Instead we will try
            // to give a name based on the MediaType
            if(headers.ContentType.MediaType != null)
            {
                if (headers.ContentType.MediaType.ToLower().Contains("report"))
                    return _defaultReportFileName;

                if (headers.ContentType.MediaType.ToLower().Contains("multipart/"))
                    return _defaultMIMEFileName;

                if(headers.ContentType.MediaType.ToLower().Contains("message/rfc822"))
                    return _defaultMIMEFileName;
            }

            // If it was not possible with the MediaType, use the ContentID as a name
            if(headers.ContentID != null)
                return headers.ContentID;

            // If everything else fails, just use the default name
            return _defaultFileName;
		}

	    /// <summary>
		/// Decode the attachment to text
		/// </summary>
		/// <returns>Decoded attachment text</returns>
		public string DecodeAsText()
		{
            if (!string.IsNullOrEmpty(Headers.ContentType.MediaType) && Headers.ContentType.MediaType.ToLower().Equals("message/rfc822"))
                return EncodedWord.Decode(RawAttachment);

            return Utility.DoDecode(RawAttachment, Headers.ContentTransferEncoding, Headers.ContentType.CharSet);
		}

		/// <summary>
		/// Decode attachment to be a message object
		/// </summary>
		/// <param name="blnRemoveHeaderBlankLine"></param>
		/// <param name="blnUseRawContent"></param>
		/// <returns>new message object</returns>
		public Message DecodeAsMessage(bool blnRemoveHeaderBlankLine, bool blnUseRawContent)
		{
		    string strContent = blnUseRawContent ? RawContent : RawAttachment;

            if (blnRemoveHeaderBlankLine && strContent.StartsWith("\r\n"))
                strContent = strContent.Substring(2, strContent.Length - 2);
		    return new Message(false, strContent, false);
		}

		/// <summary>
		/// Decode the attachment to bytes
		/// </summary>
		/// <returns>Decoded attachment bytes</returns>
		public byte[] DecodedAsBytes()
		{
            return Encoding.Default.GetBytes(DecodeAsText());
		}

        /// <summary>
        /// Save this Attachment to a file
        /// </summary>
        /// <param name="strFileName">File to write Attachment to</param>
        /// <returns>true if save was successfull, false if save failed</returns>
        public bool SaveToFile(string strFileName)
        {
            return Utility.SaveByteContentToFile(strFileName, DecodedAsBytes());
        }

        public int CompareTo(Attachment attachment)
        {
            return RawAttachment.CompareTo(attachment.RawAttachment);
        }

        /// <summary>
        /// Verify if the attachment is an RFC822 message.
        /// </summary>
        /// <returns>true if Attachment is a RFC822 message, false otherwise</returns>
        public bool IsMIMEMailFile()
        {
            return (Headers.ContentType.MediaType != null &&
                    Headers.ContentType.MediaType.ToLower().Contains("message/rfc822")) ||
                   ContentFileName.ToLower().EndsWith(".eml");
        }
	}
}