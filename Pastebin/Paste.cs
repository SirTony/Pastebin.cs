using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    /// <summary>
    /// A class that defines an existing paste.
    /// </summary>
    public sealed class Paste
    {
        private const string RawUrl = "http://pastebin.com/raw/{0}";
        private const string DeleteOption = "delete";

        private readonly WebAgent agent;
        private readonly string key;
        private readonly long timestamp;
        private readonly string title;
        private readonly long size;
        private readonly long expireTimestamp;
        private readonly PasteExposure exposure;
        private readonly string formatName;
        private readonly string formatId;
        private readonly string url;
        private readonly long views;
        private string text;

        /// <summary>
        /// The unique ID of the paste.
        /// </summary>
        public string Id => this.key;

        /// <summary>
        /// An instance of <see cref="DateTime" /> representing when the paste was created in UTC.
        /// </summary>
        public DateTime Submitted => DateTimeOffset.FromUnixTimeSeconds( this.timestamp ).ToUniversalTime().DateTime;

        /// <summary>
        /// The raw Unix timestamp representing when the paste was created.
        /// </summary>
        public long Timestamp => this.timestamp;

        /// <summary>
        /// The title of the paste as it appears on the page.
        /// </summary>
        public string Title => this.title;

        /// <summary>
        /// The size (in bytes) of the paste text.
        /// </summary>
        public long Size => this.size;

        /// <summary>
        /// The date the paste is due to expire in UTC, or null if it never expires.
        /// </summary>
        public DateTime? Expires => this.expireTimestamp == 0 ? null as DateTime? : DateTimeOffset.FromUnixTimeSeconds( this.expireTimestamp ).ToUniversalTime().DateTime;

        /// <summary>
        /// The paste's visibility.
        /// </summary>
        public PasteExposure Exposure => this.exposure;

        /// <summary>
        /// The display name of the paste's language.
        /// </summary>
        public string LanguageName => this.formatName;

        /// <summary>
        /// The internal ID of the paste's language.
        /// </summary>
        public string LanguageId => this.formatId;

        /// <summary>
        /// The URL to the paste.
        /// </summary>
        public string Url => this.url;

        /// <summary>
        /// The number of time the paste has been viewed.
        /// </summary>
        public long Views => this.views;

        /// <summary>
        /// The contents of the paste.
        /// </summary>
        public string Text => this.text ?? ( this.text = this.agent.Get( String.Format( RawUrl, this.key ), null ) );

        internal Paste( WebAgent agent, XElement paste )
        {
            this.agent = agent;
            this.key = paste.Value( "paste_key" );
            this.timestamp = paste.Value( "paste_date", Int64.Parse );
            this.title = paste.Value( "paste_title" );
            this.size = paste.Value( "paste_size", Int64.Parse );
            this.expireTimestamp = paste.Value( "paste_expire_date", Int64.Parse );
            this.exposure = (PasteExposure)paste.Value( "paste_private", Int32.Parse );
            this.formatName = paste.ValueOrDefault( "paste_format_long" );
            this.formatId = paste.ValueOrDefault( "paste_format_short" );
            this.url = paste.Value( "paste_url" );
            this.views = paste.Value( "paste_hits", Int64.Parse );
        }

        /// <summary>
        /// Deletes the current paste. Only available if the paste belongs to the currently logged in user.
        /// </summary>
        /// <exception cref="WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public void Delete()
        {
            var parameters = new Dictionary<string, object>
            {
                { "api_paste_key", this.key },
            };

            this.agent.Post( DeleteOption, parameters );
        }
    }
}