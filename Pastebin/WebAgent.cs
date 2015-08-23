using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
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

            this.userKey = this.CreateAndExecute( LoginUrl, "POST", parameters );
        }

        public string Get( string url, Dictionary<string, object> parameters )
        {
            return this.CreateAndExecute( url, "GET", parameters );
        }

        public string Post( Dictionary<string, object> parameters )
        {
            return this.CreateAndExecute( ApiUrl, "POST", parameters );
        }

        public string Post( string option, Dictionary<string, object> parameters )
        {
            parameters = parameters ?? new Dictionary<string, object>();
            parameters.Add( "api_option", option );

            return this.Post( parameters );
        }

        public XDocument PostAndReturnXml( string option, Dictionary<string, object> parameters = null  )
        {
            var xml = this.Post( option, parameters );
            return XDocument.Parse( String.Format( "<?xml version='1.0' encoding='utf-8'?><result>{0}</result>", xml ) );
        }

        private WebRequest CreateRequest( string endPoint, string method, Dictionary<string, object> parameters )
        {
            parameters = parameters ?? new Dictionary<string, object>();
            parameters.Add( "api_dev_key", this.apiKey );

            if( this.Authenticated )
                parameters.Add( "api_user_key", this.userKey );

            var pairs = new List<string>( parameters.Count );
            foreach( var pair in parameters )
            {
                var key = HttpUtility.UrlEncode( pair.Key );
                var value = HttpUtility.UrlEncode( pair.Value.ToString() );
                pairs.Add( String.Format( "{0}={1}", key, value ) );
            }

            var query = String.Join( "&", pairs );

            if( method == "GET" )
                endPoint = String.Format( "{0}?{1}", endPoint, query );

            var request = WebRequest.Create( endPoint );
            request.Method = method;

            if( method == "POST" )
            {
                var data = Encoding.UTF8.GetBytes( query );
                using( var stream = request.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                    stream.Flush();
                }

                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
            }

            return request;
        }

        private string ExecuteRequest( WebRequest request )
        {
            var response = request.GetResponse();
            var text = null as string;

            using( var stream = response.GetResponseStream() )
            using( var reader = new StreamReader( stream, Encoding.UTF8 ) )
                text = reader.ReadToEnd();

            if( text.StartsWith( "Bad API request," ) )
            {
                var error = text.Substring( text.IndexOf( ',' ) + 2 );
                switch( error )
                {
                    case "invalid api_user_key":
                        throw new PastebinException( "User not logged in" );

                    case "invalid api_dev_key":
                        throw new PastebinException( "Invalid API key" );

                    default:
                        throw new PastebinException( error );
                }
            }

            return text;
        }

        private string CreateAndExecute( string url, string method, Dictionary<string, object> parameters )
        {
            var request = this.CreateRequest( url, method, parameters );
            return this.ExecuteRequest( request );
        }
    }
}
