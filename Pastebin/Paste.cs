using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Pastebin
{
    /// <summary>
    ///     A class that defines an existing paste.
    /// </summary>
    public sealed class Paste
    {
        private const string RawPrivateUrl = "https://pastebin.com/api/api_raw.php";
        private const string RawPublicUrl = "https://pastebin.com/raw/{0}";
        private const string DeleteOption = "delete";
        private const string RawOption = "show_paste";

        private readonly HttpWebAgent _agent;
        private readonly long _expireTimestamp;
        private string _text;

        /// <summary>
        ///     The unique ID of the paste.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     An instance of <see cref="DateTime" /> representing when the paste was created in UTC.
        /// </summary>
        public DateTime Submitted =>
                DateTimeOffset.FromUnixTimeSeconds( this.Timestamp )
                              .ToUniversalTime()
                              .DateTime;

        /// <summary>
        ///     The raw Unix timestamp representing when the paste was created.
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        ///     The title of the paste as it appears on the page.
        /// </summary>
        public string Title { get; }

        /// <summary>
        ///     The size (in bytes) of the paste text.
        /// </summary>
        public long Size { get; }

        /// <summary>
        ///     The date the paste is due to expire in UTC, or null if it never expires.
        /// </summary>
        public DateTime? Expires =>
                this._expireTimestamp == 0
                    ? null as DateTime?
                    : DateTimeOffset.FromUnixTimeSeconds( this._expireTimestamp )
                                    .ToUniversalTime()
                                    .DateTime;

        /// <summary>
        ///     The paste's visibility.
        /// </summary>
        public PasteExposure Exposure { get; }

        /// <summary>
        ///     The display name of the paste's language.
        /// </summary>
        public string LanguageName { get; }

        /// <summary>
        ///     The internal ID of the paste's language.
        /// </summary>
        public string LanguageId { get; }

        /// <summary>
        ///     The URL to the paste.
        /// </summary>
        public string Url { get; }

        /// <summary>
        ///     The number of time the paste has been viewed.
        /// </summary>
        public long Views { get; }

        [SuppressMessage( "ReSharper", "PossibleNullReferenceException" )]
        internal Paste( HttpWebAgent agent, XContainer paste )
        {
            this._agent = agent;
            this.Id = paste.Element( "paste_key" ).Value;
            this.Timestamp = Int64.Parse( paste.Element( "paste_date" ).Value );
            this.Title = paste.Element( "paste_title" ).Value;
            this.Size = Int64.Parse( paste.Element( "paste_size" ).Value );
            this._expireTimestamp = Int64.Parse( paste.Element( "paste_expire_date" ).Value );
            this.Exposure = (PasteExposure)Int32.Parse( paste.Element( "paste_private" ).Value );
            this.LanguageName = paste.Element( "paste_format_long" )?.Value;
            this.LanguageId = paste.Element( "paste_format_short" )?.Value;
            this.Url = paste.Element( "paste_url" ).Value;
            this.Views = Int64.Parse( paste.Element( "paste_hits" ).Value );
        }

        /// <summary>
        ///     Retreive the raw text data of the paste.
        /// </summary>
        public async Task<string> GetTextAsync()
        {
            if( !( this._agent.Authenticated && ( this.Exposure == PasteExposure.Private ) ) )
            {
                var parameters = new Dictionary<string, object>
                {
                    ["api_paste_key"] = this.Id,
                    ["api_option"] = Paste.RawOption
                };

                this._text = await this._agent.GetAsync( String.Format( Paste.RawPublicUrl, this.Id ), parameters )
                                       .ConfigureAwait( false );
            }
            else
            {
                var parameters = new Dictionary<string, object>
                {
                    ["api_paste_key"] = this.Id
                };

                this._text = await this._agent.CreateAndExecuteAsync( Paste.RawPrivateUrl, "POST", parameters )
                                       .ConfigureAwait( false );
            }

            return this._text;
        }

        /// <summary>
        ///     Deletes the current paste. Only available if the paste belongs to the currently logged in user.
        /// </summary>
        /// <exception cref="WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public async Task DeleteAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                ["api_paste_key"] = this.Id
            };

            await this._agent.PostAsync( Paste.DeleteOption, parameters ).ConfigureAwait( false );
        }
    }
}
