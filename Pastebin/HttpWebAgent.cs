using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Pastebin
{
    internal sealed class HttpWebAgent
    {
        private const string ApiUrl = "https://pastebin.com/api/api_post.php";
        private const string LoginUrl = "https://pastebin.com/api/api_login.php";

        private const string UserAgent = "Pastebin.cs v" + AssemblyVersion.InformationalVersion;

        private const double BurstDuration = 60; // seconds
        private const uint MaxRequestsPerBurst = 30;
        private const double PaceRequestTimeout = 2000;

        public readonly string ApiKey;
        private readonly RateLimitMode _rateLimitMode;

        private DateTime? _burstStart;
        private DateTime? _lastRequest;
        private uint _requestsThisBurst;
        private string _userKey;

        public bool Authenticated => this._userKey != null;

        public HttpWebAgent( string apiKey, RateLimitMode mode )
        {
            this.ApiKey = apiKey;
            this._rateLimitMode = mode;
        }

        public void Authenticate( string username, string password )
        {
            var parameters = new Dictionary<string, object>
            {
                { "api_user_name", username },
                { "api_user_password", password }
            };

            this._userKey = this.CreateAndExecute( HttpWebAgent.LoginUrl, "POST", parameters );
        }

        public async Task AuthenticateAsync( string username, string password )
        {
            var parameters = new Dictionary<string, object>
            {
                { "api_user_name", username },
                { "api_user_password", password }
            };

            this._userKey = await this.CreateAndExecuteAsync( HttpWebAgent.LoginUrl, "POST", parameters );
        }

        public Task<string> GetAsync( string url, Dictionary<string, object> parameters )
            => this.CreateAndExecuteAsync( url, "GET", parameters );

        public Task<string> PostAsync( Dictionary<string, object> parameters )
            => this.CreateAndExecuteAsync( HttpWebAgent.ApiUrl, "POST", parameters );

        public Task<string> PostAsync( string option, Dictionary<string, object> parameters )
        {
            parameters = parameters ?? new Dictionary<string, object>();
            parameters.Add( "api_option", option );

            return this.PostAsync( parameters );
        }

        public string Get( string url, Dictionary<string, object> parameters )
            => this.CreateAndExecute( url, "GET", parameters );

        public string Post( Dictionary<string, object> parameters )
            => this.CreateAndExecute( HttpWebAgent.ApiUrl, "POST", parameters );

        public string Post( string option, Dictionary<string, object> parameters )
        {
            parameters = parameters ?? new Dictionary<string, object>();
            parameters.Add( "api_option", option );

            return this.Post( parameters );
        }

        public XDocument PostAndReturnXml( string option, Dictionary<string, object> parameters = null )
        {
            var xml = this.Post( option, parameters );
            return XDocument.Parse( $"<?xml version='1.0' encoding='utf-8'?><result>{xml}</result>" );
        }

        public async Task<XDocument> PostAndReturnXmlAsync( string option, Dictionary<string, object> parameters = null )
        {
            var xml = await this.PostAsync( option, parameters );
            return XDocument.Parse( $"<?xml version='1.0' encoding='utf-8'?><result>{xml}</result>" );
        }

        public WebRequest CreateRequest( string endPoint, string method, Dictionary<string, object> parameters )
        {
            this.EnforceRateLimit();
            var request = this.CreateRequestImpl( endPoint, method, parameters, out var query );

            if( method != "POST" ) return request;
            
            HttpWebAgent.WritePostData( request, query );
            return request;
        }

        public async Task<WebRequest> CreateRequestAsync( string endPoint, string method, Dictionary<string, object> parameters )
        {
            await this.EnforceRateLimitAsync();
            var request = this.CreateRequestImpl( endPoint, method, parameters, out var query );

            if( method != "POST" ) return request;

            await HttpWebAgent.WritePostDataAsync( request, query );
            return request;
        }

        private WebRequest CreateRequestImpl( string endPoint, string method, Dictionary<string, object> parameters, out string query )
        {
            parameters = parameters ?? new Dictionary<string, object>();
            parameters.Add( "api_dev_key", this.ApiKey );

            if( this.Authenticated )
                parameters.Add( "api_user_key", this._userKey );

            var pairs = new List<string>( parameters.Count );
            pairs.AddRange(
                from pair in parameters
                let key = HttpUtility.UrlEncode( pair.Key )
                let value = HttpUtility.UrlEncode( pair.Value.ToString() )
                select $"{key}={value}"
            );

            query = String.Join( "&", pairs );

            if( method == "GET" )
                endPoint = $"{endPoint}?{query}";

            var request = WebRequest.CreateHttp( endPoint );
            request.Method = method;
            request.UserAgent = HttpWebAgent.UserAgent;

            return request;
        }

        private static void WritePostData( WebRequest request, string query )
        {
            var data = Encoding.UTF8.GetBytes( query );
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            using( var stream = request.GetRequestStream() )
            {
                stream.Write( data, 0, data.Length );
                stream.Flush();
            }
        }

        private static async Task WritePostDataAsync( WebRequest request, string query )
        {
            var data = Encoding.UTF8.GetBytes( query );
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            using( var stream = await request.GetRequestStreamAsync() )
            {
                await stream.WriteAsync( data, 0, data.Length );
                await stream.FlushAsync();
            }
        }

        public static string ExecuteRequest( WebRequest request )
        {
            var response = request.GetResponse();
            string text;

            // ReSharper disable once AssignNullToNotNullAttribute
            using( var stream = response.GetResponseStream() )
            using( var reader = new StreamReader( stream, Encoding.UTF8 ) )
            {
                text = reader.ReadToEnd();
            }

            HttpWebAgent.HandleResponseString( text );
            return text;
        }

        public static async Task<string> ExecuteRequestAsync( WebRequest request )
        {
            var response = await request.GetResponseAsync();
            string text;

            // ReSharper disable once AssignNullToNotNullAttribute
            using( var stream = response.GetResponseStream() )
            using( var reader = new StreamReader( stream, Encoding.UTF8 ) )
            {
                text = await reader.ReadToEndAsync();
            }

            HttpWebAgent.HandleResponseString( text );
            return text;
        }

        private static void HandleResponseString( string text )
        {
            if( !text.StartsWith( "Bad API request," ) ) return;

            var error = text.Substring( text.IndexOf( ',' ) + 2 );
            switch( error )
            {
                case "invalid api_user_key": throw new PastebinException( "User not logged in" );
                case "invalid api_dev_key": throw new PastebinException( "Invalid API key" );

                default: throw new PastebinException( error );
            }
        }

        public string CreateAndExecute( string url, string method, Dictionary<string, object> parameters )
        {
            var request = this.CreateRequest( url, method, parameters );
            return HttpWebAgent.ExecuteRequest( request );
        }

        public async Task<string> CreateAndExecuteAsync( string url, string method, Dictionary<string, object> parameters )
        {
            var request = await this.CreateRequestAsync( url, method, parameters );
            return await HttpWebAgent.ExecuteRequestAsync( request );
        }

        private void EnforceRateLimit()
        {
            if( this.CheckRequestRate( out var duration ) )
                Thread.Sleep( duration );
        }

        private Task EnforceRateLimitAsync()
        {
            var needsLimit = this.CheckRequestRate( out var duration );
            return needsLimit ? Task.Delay( duration ) : Task.CompletedTask;
        }

        private bool CheckRequestRate( out TimeSpan duration )
        {
            duration = TimeSpan.Zero;
            // TODO: this deprecation warning is silenced Disabled is removed
            if( ( this._burstStart == null ) &&
                ( this._lastRequest == null ) )
            {
                this._lastRequest = DateTime.UtcNow;
                this._burstStart = DateTime.UtcNow;
                return false;
            }

            switch( this._rateLimitMode )
            {
                case RateLimitMode.None:
                case RateLimitMode.Burst:
                {
                    if( this._burstStart == null )
                    {
                        this._burstStart = DateTime.UtcNow;
                        this._requestsThisBurst = 1;
                        return false;
                    }

                    var diff = DateTime.UtcNow - this._burstStart.Value;
                    if( diff.TotalSeconds >= HttpWebAgent.BurstDuration )
                    {
                        this._burstStart = DateTime.UtcNow;
                        this._requestsThisBurst = 0;
                        return false;
                    }

                    if( this._requestsThisBurst >= HttpWebAgent.MaxRequestsPerBurst )
                    {
                        var timeLeft = TimeSpan.FromSeconds( HttpWebAgent.BurstDuration ) -
                                       ( DateTime.UtcNow - this._burstStart.Value );

                        if( this._rateLimitMode != RateLimitMode.Burst )
                            throw new PastebinRateLimitException( timeLeft );

                        duration = timeLeft;
                        return true;
                    }

                    ++this._requestsThisBurst;
                    return false;
                }

                case RateLimitMode.Pace:
                {
                    if( this._lastRequest == null )
                    {
                        this._lastRequest = DateTime.UtcNow;
                        return false;
                    }

                    var diff = DateTime.UtcNow - this._lastRequest.Value;
                    if( diff.TotalMilliseconds < HttpWebAgent.PaceRequestTimeout )
                    {
                        duration = TimeSpan.FromMilliseconds( HttpWebAgent.PaceRequestTimeout ) - diff;
                        return true;
                    }

                    this._lastRequest = DateTime.UtcNow;
                    return false;
                }

                default: throw new NotSupportedException();
            }
        }
    }
}
