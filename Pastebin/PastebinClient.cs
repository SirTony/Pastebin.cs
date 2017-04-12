using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pastebin
{
    /// <summary>
    ///     A class for interacting with the Pastebin API.
    /// </summary>
    public sealed class PastebinClient
    {
        private const string UserOption = "userdetails";
        private const string TrendingOption = "trends";
        private const string PasteOption = "paste";

        private readonly HttpWebAgent _agent;
        private User _user;

        /// <summary>
        ///     Gets the current API key to use in requests.
        /// </summary>
        public string ApiKey => this._agent.ApiKey;

        /// <summary>
        ///     Returns a collection of the current trending pastes.
        /// </summary>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public ReadOnlyCollection<Paste> TrendingPastes
        {
            get
            {
                // ReSharper disable once PossibleNullReferenceException
                var pastes = this._agent
                                 .PostAndReturnXml( PastebinClient.TrendingOption )
                                 .Element( "result" )
                                 .Elements( "paste" );

                return pastes.Select( element => new Paste( this._agent, element ) )
                             .ToList()
                             .AsReadOnly();
            }
        }

        /// <summary>
        ///     The currently logged in user.
        /// </summary>
        /// <exception cref="PastebinException">Thrown when the user is not logged in.</exception>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public User User
        {
            get
            {
                if( !this._agent.Authenticated )
                    throw new PastebinException( "User not logged in." );

                if( this._user != null ) return this._user;

                var document = this._agent.PostAndReturnXml( PastebinClient.UserOption );
                // ReSharper disable once PossibleNullReferenceException
                this._user = new User( this._agent, document.Element( "result" ).Element( "user" ) );

                return this._user;
            }
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="PastebinClient" /> with the specified API key.
        /// </summary>
        /// <param name="apiKey">
        ///     The API key to make requests with. An API key can be obtained by logging into https://pastebin.com
        ///     and going to https://pastebin.com/api
        /// </param>
        /// <param name="rateLimitMode">
        ///     Specifies how rate limiting should be handled See <see cref="RateLimitMode" /> for more
        ///     details.
        /// </param>
        public PastebinClient( string apiKey, RateLimitMode rateLimitMode = RateLimitMode.None )
        {
            if( apiKey == null )
                throw new ArgumentNullException( nameof( apiKey ) );

            this._agent = new HttpWebAgent( apiKey, rateLimitMode );
        }

        /// <summary>
        ///     Logs in to Pastebin and returns a <see cref="Pastebin.User" /> instance representing the logged in user.
        /// </summary>
        /// <param name="username">Ther username of the account to authenticate as.</param>
        /// <param name="password">The password for the account to authenticate as.</param>
        /// <returns>An object representing the newly authenticated user.</returns>
        /// <exception cref="System.ArgumentNullException">
        ///     Thrown when <paramref name="username" /> or <paramref name="password" />
        ///     is null.
        /// </exception>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public User LogIn( string username, string password )
        {
            if( username == null )
                throw new ArgumentNullException( nameof( username ) );

            if( password == null )
                throw new ArgumentNullException( nameof( password ) );

            if( this._agent.Authenticated )
                this._user = null;

            this._agent.Authenticate( username, password );
            return this.User;
        }

        /// <summary>
        ///     Creates a new anonymous paste. Private pastes are not allowed when pasting anonymously.
        /// </summary>
        /// <param name="title">The title of the paste as it will appear on the page.</param>
        /// <param name="languageId">
        ///     The the language ID of the paste's content. A full list of language IDs can be found at
        ///     https://pastebin.com/api#5
        /// </param>
        /// <param name="code">The contents of the paste.</param>
        /// <param name="exposure">The visibility of the paste (private, public, or unlisted).</param>
        /// <param name="expiration">The duration of time the paste will be available before expiring.</param>
        /// <returns>The URL for the newly created paste.</returns>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="code" /> is null.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public string CreatePaste(
            string title,
            string languageId,
            string code,
            PasteExposure exposure = PasteExposure.Public,
            PasteExpiration expiration = PasteExpiration.Never
        ) =>
            PastebinClient.CreatePasteImpl(
            this._agent,
            false,
            title,
            languageId,
            code,
            exposure,
            expiration
        );

        internal static string CreatePasteImpl(
            HttpWebAgent agent,
            bool authenticated,
            string title,
            string languageId,
            string code,
            PasteExposure exposure,
            PasteExpiration expiration )
        {
            if( code == null )
                throw new ArgumentNullException( nameof( code ) );

            if( title == null )
                title = "Untitled";

            if( languageId == null )
                languageId = "text";

            string expirationString;
            switch( expiration )
            {
                case PasteExpiration.Never:
                    expirationString = "N";
                    break;

                case PasteExpiration.OneDay:
                    expirationString = "1D";
                    break;

                case PasteExpiration.OneHour:
                    expirationString = "1H";
                    break;

                case PasteExpiration.OneMonth:
                    expirationString = "1M";
                    break;

                case PasteExpiration.OneWeek:
                    expirationString = "1W";
                    break;

                case PasteExpiration.TenMinutes:
                    expirationString = "10M";
                    break;

                case PasteExpiration.TwoWeeks:
                    expirationString = "2W";
                    break;

                default: throw new NotSupportedException();
            }

            var parameters = new Dictionary<string, object>
            {
                { "api_paste_code", code },
                { "api_paste_name", title },
                { "api_paste_format", languageId },
                { "api_paste_private", (int)exposure },
                { "api_paste_expire_date", expirationString }
            };

            return agent.Post( PastebinClient.PasteOption, parameters );
        }
    }
}
