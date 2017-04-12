using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Pastebin
{
    /// <summary>
    ///     A class representing an authenticated user.
    /// </summary>
    public sealed class User
    {
        private const string ListOption = "list";

        private readonly HttpWebAgent _agent;

        /// <summary>
        ///     The display name of the current user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     The default paste language for the current user.
        /// </summary>
        public string DefaultFormat { get; }

        /// <summary>
        ///     The default paste expiration for the current user.
        /// </summary>
        public string DefaultExpiration { get; }

        /// <summary>
        ///     The URL pointing to the current user's avatar.
        /// </summary>
        public string AvatarUrl { get; }

        /// <summary>
        ///     The current user's website.
        /// </summary>
        public string Website { get; }

        /// <summary>
        ///     The current user's email address.
        /// </summary>
        public string Email { get; }

        /// <summary>
        ///     The current user's location.
        /// </summary>
        public string Location { get; }

        /// <summary>
        ///     The default paste visibility for the current user.
        /// </summary>
        public PasteExposure DefaultExposure { get; }

        /// <summary>
        ///     The current user's account type.
        /// </summary>
        public AccountType AccountType { get; }

        [SuppressMessage( "ReSharper", "PossibleNullReferenceException" )]
        internal User( HttpWebAgent agent, XContainer user )
        {
            this._agent = agent;
            this.Username = user.Element( "user_name" ).Value;
            this.DefaultFormat = user.Element( "user_format_short" ).Value;
            this.DefaultExpiration = user.Element( "user_expiration" ).Value;
            this.AvatarUrl = user.Element( "user_avatar_url" ).Value;
            this.Website = user.Element( "user_website" ).Value;
            this.Email = user.Element( "user_email" ).Value;
            this.Location = user.Element( "user_location" ).Value;
            this.DefaultExposure = (PasteExposure)Int32.Parse( user.Element( "user_private" ).Value );
            this.AccountType = (AccountType)Int32.Parse( user.Element( "user_account_type" ).Value );
        }

        /// <summary>
        ///     Creates a new paste under the current user.
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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="code" /> is null.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public string CreatePaste(
            string title,
            string languageId,
            string code,
            PasteExposure exposure = PasteExposure.Public,
            PasteExpiration expiration = PasteExpiration.Never )
            => PastebinClient.CreatePasteImpl( this._agent, true, title, languageId, code, exposure, expiration );

        /// <summary>
        ///     Creates a new paste under the current user.
        /// </summary>
        /// <param name="title">The title of the paste as it will appear on the page.</param>
        /// <param name="languageId">
        ///     The the language ID of the paste's content. A full list of language IDs can be found at
        ///     
        /// https://pastebin.com/api#5
        /// </param>
        /// <param name="code">The contents of the paste.</param>
        /// <param name="exposure">The visibility of the paste (private, public, or unlisted).</param>
        /// <param name="expiration">The duration of time the paste will be available before expiring.</param>
        /// <returns>The URL for the newly created paste.</returns>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="code" /> is null.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public string CreatePaste(
            string title,
            string languageId,
            string code,
            PasteExposure? exposure = null,
            PasteExpiration expiration = PasteExpiration.Never )
            => this.CreatePaste( title, languageId, code, exposure ?? this.DefaultExposure, expiration );

        /// <summary>
        ///     Lists all the pastes for the current user.
        /// </summary>
        /// <param name="limit">Optional paste limit. Minimum value = 1. Maximum value = 1000.</param>
        /// <returns>A read-only collection containing the user's pastes.</returns>
        /// <exception cref="System.Net.WebException">Thrown when the underlying HTTP client encounters an error.</exception>
        /// <exception cref="PastebinException">Thrown when a bad API request is made.</exception>
        public ReadOnlyCollection<Paste> GetPastes( int limit = 50 )
        {
            if( ( limit < 1 ) || ( limit > 1000 ) )
            {
                throw new ArgumentOutOfRangeException(
                    nameof( limit ),
                    "Limit must be between 1 and 1000 (inclusive)"
                );
            }

            var parameters = new Dictionary<string, object>
            {
                { "api_results_limit", limit }
            };

            // ReSharper disable once PossibleNullReferenceException
            var pastes = this._agent
                             .PostAndReturnXml( User.ListOption, parameters )
                             .Element( "result" )
                             .Elements( "paste" );

            return pastes.Select( x => new Paste( this._agent, x ) )
                         .ToList()
                         .AsReadOnly();
        }
    }
}
