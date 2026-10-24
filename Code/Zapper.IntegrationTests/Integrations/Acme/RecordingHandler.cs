using System.Collections.Concurrent;
using System.Net.Http.Headers;

namespace Zapper.IntegrationTests.Integrations.Acme;

public sealed class RecordingHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpResponseMessage> answers = [];

    private readonly TaskCompletionSource requestReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConcurrentQueue<(HttpMethod Method, Uri RequestUri, AuthenticationHeaderValue? Authorization, string? Body)> Requests { get; } = [];

    public Task RequestReceived => requestReceived.Task;

    public void AnswerWith(HttpResponseMessage answer) => answers.Enqueue(answer);

    // with no answer queued the call never answers, as an unresponsive issuer at Acme may not
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Enqueue((request.Method, request.RequestUri!, request.Headers.Authorization, body));
        requestReceived.TrySetResult();

        if (answers.TryDequeue(out var answer))
        {
            return answer;
        }

        var never = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var cancellation = cancellationToken.Register(() => never.TrySetCanceled(cancellationToken));

        return await never.Task;
    }
}
