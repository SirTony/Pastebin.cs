using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace Pastebin
{
    internal sealed class WebAgent
    {
        private const string ApiUrl = "http://pastebin.com/api/api_post.php";
        private const string LoginUrl = "http://pastebin.com/api/api_login.php";

        public readonly string apiKey;
        private string userKey;

        public bool Authenticated { get { return this.userKey != null; } }

        public WebAgent( string apiKey )
        {
            this.apiKey = apiKey;
        }

        public void Authenticate( string username, string password )
        {
            var parameters = new Dictionary<string, object>
            {
                { "api_user_name", username },
                { "api_user_password", password },
            };

            this.userKey = this.Request( parameters, LoginUrl );
        }

        public string Request( Dictionary<string, object> parameters, string url )
        {
            if( parameters == null )
                parameters = new Dictionary<string, object>();

            parameters.Add( "api_dev_key", this.apiKey );

            var pairs = new List<string>( parameters.Count );

            foreach( var pair in parameters )
                pairs.Add( String.Format( "{0}={1}", pair.Key, HttpUtility.UrlEncode( pair.Value.ToString() ) ) );

            var query = String.Join( "&", pairs );
            var request = WebRequest.Create( url );

            using( var stream = request.GetRequestStream() )
            using( var writer = new StreamWriter( stream, Encoding.UTF8 ) )
            {
                writer.Write( query );
                writer.Flush();
            }

            var response = request.GetResponse();
            string text = null;

            using( var stream = response.GetResponseStream() )
            using( var reader = new StreamReader( stream, Encoding.UTF8 ) )
            {
                text = reader.ReadToEnd();
            }

            if( text.StartsWith( "Bad API request," ) )
                throw new PastebinException( text.Substring( text.IndexOf( ',' ) + 1 ) );

            return text;
        }

        public string Request( string option, Dictionary<string, object> parameters = null, bool authenticated = false )
        {
            if( parameters == null )
                parameters = new Dictionary<string, object>();

            parameters.Add( "api_option", option );

            if( authenticated && !this.Authenticated )
                throw new PastebinException( "User not logged in." );

            if( authenticated )
                parameters.Add( "api_user_key", this.userKey );

            return this.Request( parameters, ApiUrl );
        }

        public XDocument RequestXml( string option, Dictionary<string, object> parameters = null, bool authenticated = false )
        {
            var xml = this.Request( option, parameters, authenticated );
            return XDocument.Parse( String.Format( "<?xml version='1.0' encoding='utf-8'?><result>{0}</result>", xml ) );
        }
    }
}
