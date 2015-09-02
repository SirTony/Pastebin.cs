using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    /// <summary>
    /// A class representing an authenticated user.
    /// </summary>
    public sealed class User
    {
        private const string ListOption = "list";

        private readonly WebAgent agent;
        private readonly string username;
        private readonly string defaultFormat;
        private readonly string defaultExpiration;
        private readonly string avatarUrl;
        private readonly string website;
        private readonly string email;
        private readonly string location;
        private readonly PasteExposure defaultExposure;
        private readonly AccountType accountType;

        /// <summary>
        /// The display name of the current user.
        /// </summary>
        public string Username { get { return this.username; } }

        /// <summary>
        /// The default paste language for the current user.
        /// </summary>
        public string DefaultFormat { get { return this.defaultFormat; } }

        /// <summary>
        /// The default paste expiration for the current user.
        /// </summary>
        public string DefaultExpiration { get { return this.defaultExpiration; } }

        /// <summary>
        /// The URL pointing to the current user's avatar.
        /// </summary>
        public string AvatarUrl { get { return this.avatarUrl; } }

        /// <summary>
        /// The current user's website.
        /// </summary>
        public string Website { get { return this.website; } }

        /// <summary>
        /// The current user's email address.
        /// </summary>
        public string Email { get { return this.email; } }

        /// <summary>
        /// The current user's location.
        /// </summary>
        public string Location { get { return this.location; } }

        /// <summary>
        /// The default paste visibility for the current user.
        /// </summary>
        public PasteExposure DefaultExposure { get { return this.defaultExposure; } }

        /// <summary>
        /// The current user's account type.
        /// </summary>
        public AccountType AccountType { get { return this.accountType; } }

        internal User( WebAgent agent, XElement user )
        {
            this.agent = agent;
            this.username = user.Value( "user_name" );
            this.defaultFormat = user.Value( "user_format_short" );
            this.defaultExpiration = user.Value( "user_expiration" );
            this.avatarUrl = user.Value( "user_avatar_url" );
            this.website = user.Value( "user_website" );
            this.email = user.Value( "user_email" );
            this.location = user.Value( "user_location" );
            this.defaultExposure = (PasteExposure)user.Value( "user_private", Int32.Parse );
            this.accountType = (AccountType)user.Value( "user_account_type", Int32.Parse );
        }

        /// <summary>
        /// Creates a new paste under the current user.
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
            return Pastebin.CreatePasteImpl( this.agent, true, title, languageId, code, exposure, expiration );
        }

        /// <summary>
        /// Lists all the pastes for the current user.
        /// </summary>
        /// <param name="limit">Optional paste limit. Minimum value = 1. Maximum value = 1000.</param>
        /// <returns>A read-only collection containing the user's pastes.</returns>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public ReadOnlyCollection<Paste> GetPastes( int limit = 50 )
        {
            if( limit < 1 || limit > 1000 )
                throw new ArgumentOutOfRangeException( "limit", "Limit must be between 1 and 1000 (inclusive)." );

            var parameters = new Dictionary<string, object>
            {
                { "api_results_limit", limit },
            };

            var pastes = this.agent.PostAndReturnXml( ListOption, parameters ).Element( "result" ).Elements( "paste" );
            return pastes.Select( x => new Paste( this.agent, x ) ).ToList().AsReadOnly();
        }
    }
}