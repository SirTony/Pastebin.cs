using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    /// <summary>
    /// A class for interacting with the Pastebin API.
    /// </summary>
    public sealed class Pastebin
    {
        private const string UserOption = "userdetails";
        private const string TrendingOption = "trends";
        private const string PasteOption = "paste";

        private readonly WebAgent agent;
        private User user;

        /// <summary>
        /// Gets the current API key to use in requests.
        /// </summary>
        public string ApiKey { get { return this.agent.apiKey; } }

        /// <summary>
        /// Returns a collection of the current trending pastes.
        /// </summary>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public ReadOnlyCollection<Paste> TrendingPastes
        {
            get
            {
                var pastes = this.agent.PostAndReturnXml( TrendingOption ).Element( "result" ).Elements( "paste" );
                var list = new List<Paste>();

                foreach( var element in pastes )
                    list.Add( new Paste( this.agent, element ) );

                return list.AsReadOnly();
            }
        }

        /// <summary>
        /// The currently logged in user.
        /// </summary>
        /// <exception cref="PastebinException">Thrown when the user is not logged in.</exception>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public User User
        {
            get
            {
                if( !this.agent.Authenticated )
                    throw new PastebinException( "User not logged in." );

                if( this.user == null )
                {
                    var document = this.agent.PostAndReturnXml( UserOption );
                    this.user = new User( this.agent, document.Element( "result" ).Element( "user" ) );
                }

                return this.user;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Pastebin.Pastebin"/> with the specified API key.
        /// </summary>
        /// <param name="apiKey">The API key to make requests with. An API key can be obtained by logging into pastebin.com and going to http://pastebin.com/api </param>
        /// <param name="rateLimitMode">Specifies how rate limiting should be handled See <see cref="RateLimitMode" /> for more details.</param>
        public Pastebin( string apiKey, RateLimitMode rateLimitMode = RateLimitMode.None )
        {
            if( apiKey == null )
                throw new ArgumentNullException( "apiKey" );

            this.agent = new WebAgent( apiKey, rateLimitMode );
        }

        /// <summary>
        /// Logs in to Pastebin and returns a <see cref="Pastebin.User"/> instance representing the logged in user.
        /// </summary>
        /// <param name="username">Ther username of the account to authenticate as.</param>
        /// <param name="password">The password for the account to authenticate as.</param>
        /// <returns>An object representing the newly authenticated user.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="username"/> or <paramref name="password"/> is null.</exception>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public User LogIn( string username, string password )
        {
            if( username == null )
                throw new ArgumentNullException( "username" );

            if( password == null )
                throw new ArgumentNullException( "password" );

            if( this.agent.Authenticated )
                this.user = null;

            this.agent.Authenticate( username, password );
            return this.User;
        }

        /// <summary>
        /// Creates a new anonymous paste. Private pastes are not allowed when pasting anonymously.
        /// </summary>
        /// <param name="title">The title of the paste as it will appear on the page.</param>
        /// <param name="languageId">The the language ID of the paste's content. A full list of language IDs can be found at http://pastebin.com/api#5 </param>
        /// <param name="code">The contents of the paste.</param>
        /// <param name="exposure">The visibility of the paste (private, public, or unlisted).</param>
        /// <param name="expiration">The duration of time the paste will be available before expiring.</param>
        /// <returns>The URL for the newly created paste.</returns>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="code"/> is null.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public string CreatePaste( string title, string languageId, string code, PasteExposure exposure = PasteExposure.Public, PasteExpiration expiration = PasteExpiration.Never )
        {
            return Pastebin.CreatePasteImpl( this.agent, false, title, languageId, code, exposure, expiration );
        }

        internal static string CreatePasteImpl( WebAgent agent, bool authenticated, string title, string languageId, string code, PasteExposure exposure, PasteExpiration expiration )
        {
            if( code == null )
                throw new ArgumentNullException( "code" );

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

                default:
                    throw new NotImplementedException();
            }

            var parameters = new Dictionary<string, object>
            {
                { "api_paste_code", code },
                { "api_paste_name", title },
                { "api_paste_format", languageId },
                { "api_paste_private", (int)exposure },
                { "api_paste_expire_date", expirationString },
            };

            return agent.Post( PasteOption, parameters );
        }
    }
}