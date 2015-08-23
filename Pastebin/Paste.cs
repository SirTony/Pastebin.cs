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
        private const string RawUrl = "http://pastebin.com/raw.php?i={0}";
        private const string DeleteOption = "delete";

        private readonly WebAgent agent;
        private readonly string key;
        private readonly int timestamp;
        private readonly string title;
        private readonly int size;
        private readonly int expireTimestamp;
        private readonly PasteExposure exposure;
        private readonly string formatName;
        private readonly string formatId;
        private readonly string url;
        private readonly int views;
        private string text;

        /// <summary>
        /// The unique ID of the paste.
        /// </summary>
        public string Id { get { return this.key; } }

        /// <summary>
        /// An instance of <c>System.DateTime</c> representing when the paste was created (local time).
        /// </summary>
        public DateTime Submitted { get { return Utils.FromUnixTime( this.timestamp ).ToLocalTime(); } }

        /// <summary>
        /// The raw Unix timestamp representing when the paste was created.
        /// </summary>
        public int Timestamp { get { return this.timestamp; } }

        /// <summary>
        /// The title of the paste as it appears on the page.
        /// </summary>
        public string Title { get { return this.title; } }

        /// <summary>
        /// The size (in bytes) of the paste text.
        /// </summary>
        public int Size { get { return this.size; } }

        /// <summary>
        /// The date the paste is due to expire, or null if it never expires.
        /// </summary>
        public DateTime? Expires { get { return this.expireTimestamp == 0 ? null : new DateTime?( Utils.FromUnixTime( this.expireTimestamp ) ); } }

        /// <summary>
        /// The paste's visibility.
        /// </summary>
        public PasteExposure Exposure { get { return this.exposure; } }

        /// <summary>
        /// The display name of the paste's language.
        /// </summary>
        public string LanguageName { get { return this.formatName; } }

        /// <summary>
        /// The internal ID of the paste's language.
        /// </summary>
        public string LanguageId { get { return this.formatId; } }

        /// <summary>
        /// The URL to the paste.
        /// </summary>
        public string Url { get { return this.url; } }

        /// <summary>
        /// The number of time the paste has been viewed.
        /// </summary>
        public int Views { get { return this.views; } }

        /// <summary>
        /// The contents of the paste.
        /// </summary>
        public string Text
        {
            get
            {
                if( this.text == null )
                {
                    using( var client = new WebClient() )
                    {
                        this.text = client.DownloadString( String.Format( RawUrl, this.key ) );
                    }
                }

                return this.text;
            }
        }

        internal Paste( WebAgent agent, XElement paste )
        {
            this.agent = agent;
            this.key = paste.Value( "paste_key" );
            this.timestamp = paste.Value( "paste_date", Int32.Parse );
            this.title = paste.Value( "paste_title" );
            this.size = paste.Value( "paste_size", Int32.Parse );
            this.expireTimestamp = paste.Value( "paste_expire_date", Int32.Parse );
            this.exposure = (PasteExposure)paste.Value( "paste_private", Int32.Parse );
            this.formatName = paste.ValueOrDefault( "paste_format_long" );
            this.formatId = paste.ValueOrDefault( "paste_format_short" );
            this.url = paste.Value( "paste_url" );
            this.views = paste.Value( "paste_hits", Int32.Parse );
        }

        /// <summary>
        /// Deletes the current paste. Only available if the paste belongs to the currently logged in user.
        /// </summary>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public void Delete()
        {
            var parameters = new Dictionary<string, object>
            {
                { "api_paste_key", this.key },
            };

            this.agent.Post( DeleteOption, parameters, true );
        }
    }
}