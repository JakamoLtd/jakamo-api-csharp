using System.Net.Http;

namespace Jakamo.Api.Client
{
    public abstract class CommonClient
    {

        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Instantiate a new Jakamo API Client
        ///
        /// If consuming in an ASP.NET Core web app, use a HTTPClientFactory to set the
        /// authentication headers, base URI etc, and pass a client from the factory to
        /// the constructor
        /// </summary>
        /// <param name="client"></param>
        protected CommonClient(HttpClient client)
        {
            HttpClient = client;
        }

    }
}