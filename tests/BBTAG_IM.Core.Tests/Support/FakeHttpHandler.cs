using System.Net;
using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _answers = new(StringComparer.Ordinal);

    public List<HttpRequestMessage> Requests { get; } = [];

    public FakeHttpHandler Answer(string address, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _answers[address] = () => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8) };
        return this;
    }

    public FakeHttpHandler Answer(string address, byte[] body)
    {
        _answers[address] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };
        return this;
    }

    public FakeHttpHandler Answer(string address, Func<HttpResponseMessage> response)
    {
        _answers[address] = response;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var address = request.RequestUri!.AbsoluteUri;

        return Task.FromResult(_answers.TryGetValue(address, out var answer) ? answer() : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
