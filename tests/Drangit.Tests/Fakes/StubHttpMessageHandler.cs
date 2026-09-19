using System.Net;
using System.Text;

namespace Drangit.Tests.Fakes;

/// <summary>
/// Handler HTTP de test : enregistre les requêtes reçues et rejoue une réponse fixée.
/// On ne mocke pas <see cref="HttpClient"/> — on branche un vrai client sur ce handler.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    private StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        _respond = respond;

    public List<HttpRequestMessage> Requests { get; } = [];

    public static StubHttpMessageHandler Responding(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(respond);

    public static StubHttpMessageHandler ReturningJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => JsonResponse(json, status));

    /// <summary>
    /// Rejoue une réponse différente par appel, puis répète la dernière.
    /// </summary>
    /// <remarks>
    /// C'est ce qu'exige la pagination : la première page doit être pleine et la suivante
    /// incomplète, sans quoi la boucle ne s'arrête jamais sur la même réponse.
    /// </remarks>
    public static StubHttpMessageHandler ReturningJsonSequence(params string[] pages)
    {
        var index = 0;

        return new StubHttpMessageHandler(_ =>
        {
            var page = pages[Math.Min(index, pages.Length - 1)];
            index++;
            return JsonResponse(page, HttpStatusCode.OK);
        });
    }

    public static StubHttpMessageHandler ReturningStatus(
        HttpStatusCode status,
        IReadOnlyDictionary<string, string>? headers = null) =>
        new(_ =>
        {
            var response = new HttpResponseMessage(status);

            foreach (var (name, value) in headers ?? new Dictionary<string, string>(StringComparer.Ordinal))
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return response;
        });

    public static StubHttpMessageHandler Throwing(Exception exception) =>
        new(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        var response = _respond(request);

        // Un vrai pipeline HTTP rattache la requête à sa réponse ; Refit s'en sert pour
        // construire ses ApiException. Sans cela, un 401 se muerait en InvalidOperationException.
        response.RequestMessage = request;

        return Task.FromResult(response);
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode status) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
}
