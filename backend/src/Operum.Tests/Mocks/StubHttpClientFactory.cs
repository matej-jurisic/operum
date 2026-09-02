using System.Net;
using System.Text;

namespace Operum.Tests.Mocks
{
    /// <summary>
    /// Answers every request from a canned response, and records what was asked, so a provider
    /// can be tested against a payload without a network or a real API key.
    /// </summary>
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> _respond;

        public StubHttpMessageHandler(HttpStatusCode status, string body)
            : this(_ => (status, body)) { }

        public StubHttpMessageHandler(Func<HttpRequestMessage, (HttpStatusCode, string)> respond) =>
            _respond = respond;

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var (status, body) = _respond(request);

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            });
        }
    }

    public class StubHttpClientFactory(HttpMessageHandler handler, string baseAddress) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            // DisposeHandler false so the handler survives being read after the call, the way
            // a pooled one would.
            new(handler, disposeHandler: false) { BaseAddress = new Uri(baseAddress) };
    }
}
