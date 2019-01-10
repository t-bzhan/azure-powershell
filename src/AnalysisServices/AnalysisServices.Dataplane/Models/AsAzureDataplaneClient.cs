﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace Microsoft.Azure.Commands.AnalysisServices.Dataplane.Models
{
    /// <summary>
    /// Provides methods for sending HTTP requests and receiving HTTP responses from a resource identified by a URI.
    /// </summary>
    public class AsAzureDataplaneClient : ServiceClient<AsAzureDataplaneClient>, IAsAzureHttpClient
    {
        /// <summary>
        /// The base Uri of the service.
        /// </summary>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Credentials needed for the client to connect to Azure.
        /// </summary>
        public ServiceClientCredentials Credentials { get; private set; }

        private Func<HttpClient> HttpClientProvider { get; set; }

        public AsAzureDataplaneClient(Uri baseUri, ServiceClientCredentials credentials, Func<HttpClient> httpClientProvider, params DelegatingHandler[] handlers)
            : base(handlers)
        {
            this.BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            this.Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this.HttpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
            this.Credentials.InitializeServiceClient(this);
            this.ResetHttpClient();
        }

        public AsAzureDataplaneClient(Uri baseUri, ServiceClientCredentials credentials, Func<HttpClient> httpClientProvider, HttpClientHandler rootHandler, params DelegatingHandler[] handlers)
            : base(rootHandler, handlers)
        {
            this.BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            this.Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this.HttpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
            this.Credentials.InitializeServiceClient(this);
            this.ResetHttpClient();
        }

        public void ResetHttpClient()
        {
            this.HttpClient = this.HttpClientProvider();
        }

        #region CallHttpMethodAsyncOverloads

        /// <summary>
        /// Calls SendRequestAsync() for a GET.
        /// </summary>
        /// <param name="baseUri">The base Uri to call.</param>
        /// <param name="requestUrl">The request Url.</param>
        /// <param name="correlationId">The CorrelationId</param>
        /// <returns>The http response message.</returns>
        public async Task<HttpResponseMessage> CallGetAsync(Uri baseUri, string requestUrl, Guid correlationId = new Guid())
        {
            return await SendRequestAsync(HttpMethod.Get, baseUri: baseUri, requestUrl: requestUrl, correlationId: correlationId);
        }

        /// <summary>
        /// Calls SendRequestAsync() for a GET using the default BaseUri and a blank correlationId.
        /// </summary>
        /// <param name="requestUrl">The Request Url.</param>
        /// <returns>The http response message.</returns>
        public async Task<HttpResponseMessage> CallGetAsync(string requestUrl)
        {
            return await CallGetAsync(BaseUri, requestUrl, new Guid());
        }

        /// <summary>
        /// Calls SendRequestAsync() for a POST.
        /// </summary>
        /// <param name="baseUri">The base Uri to call.</param>
        /// <param name="requestUrl">The request Url.</param>
        /// <param name="correlationId">The CorrelationId</param>
        /// <param name="content">The content to post (optional).</param>
        /// <returns>The http response message.</returns>
        public async Task<HttpResponseMessage> CallPostAsync(Uri baseUri, string requestUrl, Guid correlationId, HttpContent content = null)
        {
            return await SendRequestAsync(HttpMethod.Post, baseUri, requestUrl, correlationId, content);
        }

        /// <summary>
        /// Calls SendRequestAsync() for a POST using a blank correlationId.
        /// </summary>
        /// <param name="baseUri">The base Uri to call.</param>
        /// <param name="requestUrl">The request Url.</param>
        /// <param name="content">The content to post (optional).</param>
        /// <returns>The http response message.</returns>
        public async Task<HttpResponseMessage> CallPostAsync(Uri baseUri, string requestUrl, HttpContent content = null)
        {
            return await CallPostAsync(baseUri, requestUrl, new Guid(), content);
        }

        /// <summary>
        /// Calls SendRequestAsync() for a POST using the default BaseUri and a blank correlationId.
        /// </summary>
        /// <param name="requestUrl">The Request Url.</param>
        /// <param name="content">The content to post (optional).</param>
        /// <returns>The http response message.</returns>
        public async Task<HttpResponseMessage> CallPostAsync(string requestUrl, HttpContent content = null)
        {
            return await CallPostAsync(BaseUri, requestUrl, content);
        }

        #endregion

        private async Task<HttpResponseMessage> SendRequestAsync(
            HttpMethod method,
            Uri baseUri,
            string requestUrl,
            Guid correlationId,
            HttpContent content = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Create HTTP transport objects
            HttpRequestMessage httpRequest = new HttpRequestMessage()
            {
                Method = method,
                RequestUri = new Uri(baseUri, requestUrl),
                Content = content
            };

            // Set Headers
            AddHeader(httpRequest.Headers, "x-ms-client-request-id", correlationId.ToString());

            // Set Credentials
            if (Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }

            return await this.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a header to the <see cref="HttpRequestHeaders"/> list object.
        /// </summary>
        /// <param name="headers">The request headers list object.</param>
        /// <param name="name">The name of the header to add.</param>
        /// <param name="value">The value of the header.</param>
        private static void AddHeader(HttpRequestHeaders headers, string name, string value)
        {
            if (headers.Contains(name))
            {
                headers.Remove(name);
            }
            headers.TryAddWithoutValidation(name, value);
        }
    }
}
