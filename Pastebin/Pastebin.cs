using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        /// <exception cref="Pastebin.PastebinException">Thrown when a bad API request is made.</exception>
        public ReadOnlyCollection<Paste> TrendingPastes
        {
            get
            {
                var pastes = this.agent.RequestXml( TrendingOption ).Element( "result" ).Elements( "paste" );
                var list = new List<Paste>();

                foreach( var element in pastes )
                    list.Add( new Paste( this.agent, element ) );

                return list.AsReadOnly();
            }
        }

        /// <summary>
        /// The currently logged in user.
        /// </summary>
        /// <exception cref="Pastebin.PastebinException">Thrown when the user is not logged in.</exception>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="Pastebin.PastebinException">Thrown when a bad API request is made.</exception>
        public User User
        {
            get
            {
                if( !this.agent.Authenticated )
                    throw new PastebinException( "User not logged in." );

                if( this.user == null )
                {
                    var document = this.agent.RequestXml( UserOption, authenticated: true );
                    this.user = new User( this.agent, document.Element( "result" ).Element( "user" ) );
                }

                return this.user;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Pastebin.Pastebin"/> with the specified API key.
        /// </summary>
        /// <param name="apiKey">The API key to make requests with. An API key can be obtained by logging into pastebin.com and going to http://pastebin.com/api </param>
        public Pastebin( string apiKey )
        {
            if( apiKey == null )
                throw new ArgumentNullException( "apiKey" );

            this.agent = new WebAgent( apiKey );
        }

        /// <summary>
        /// Logs in to Pastebin and returns a <see cref="Pastebin.User"/> instance representing the logged in user.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="username"/> or <paramref name="password"/> is null.</exception>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="Pastebin.PastebinException">Thrown when a bad API request is made.</exception>
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
        /// <returns>The newly created paste on success.</returns>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="code"/> is null.</exception>
        /// <exception cref="Pastebin.PastebinException">Thrown when a bad API request is made.</exception>
        public Paste CreatePaste( string title, string languageId, string code, PasteExposure exposure = PasteExposure.Public, PasteExpiration expiration = PasteExpiration.Never )
        {
            return Pastebin.CreatePasteImpl( this.agent, false, title, languageId, code, exposure, expiration );
        }

        internal static Paste CreatePasteImpl( WebAgent agent, bool authenticated, string title, string languageId, string code, PasteExposure exposure, PasteExpiration expiration )
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

            var root = agent.RequestXml( PasteOption, parameters, authenticated ).Element( "result" );
            return new Paste( agent, root.Element( "paste" ) );
        }
    }
}