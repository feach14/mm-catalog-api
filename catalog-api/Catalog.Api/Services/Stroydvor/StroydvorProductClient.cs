using System.Text;
using System.Text.RegularExpressions;

namespace Catalog.Api.Services.Stroydvor;

public sealed record StroydvorProductReadResult(StroydvorProductData? Product, string? Error)
{
    public bool Success => Product is not null;
}

public sealed class StroydvorProductClient(HttpClient httpClient)
{
    public static string ProductUrl(int productId) => $"https://stroydvor21.ru/products/{productId}";

    public async Task<StroydvorProductReadResult> Read(int productId, CancellationToken ct)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var productUri = new Uri(ProductUrl(productId));
        var currentUri = productUri;

        try
        {
            for (var redirect = 0; redirect <= 3; redirect++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.Accept.ParseAdd("text/html");
                request.Headers.UserAgent.ParseAdd("Catalog-Stroydvor-Sync/1.0");
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Found
                    or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect)
                {
                    var location = response.Headers.Location;
                    if (location is null || redirect == 3)
                        return new StroydvorProductReadResult(null, "Стройдвор не вернул конечную карточку товара.");
                    var target = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                    if (!IsSameProduct(target, productUri))
                        return new StroydvorProductReadResult(null, "Стройдвор перенаправил запрос на другой товар или адрес. Проверьте привязку.");
                    currentUri = target;
                    continue;
                }
                if (!response.IsSuccessStatusCode)
                    return new StroydvorProductReadResult(null, $"Стройдвор вернул ошибку HTTP {(int)response.StatusCode}.");
                if (response.Content.Headers.ContentLength > 3_000_000)
                    return new StroydvorProductReadResult(null, "Страница Стройдвора превышает допустимый размер.");

                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096);
                var html = new StringBuilder();
                var buffer = new char[4096];
                int count;
                while ((count = await reader.ReadAsync(buffer.AsMemory(), timeout.Token)) > 0)
                {
                    html.Append(buffer, 0, count);
                    if (html.Length > 3_000_000)
                        return new StroydvorProductReadResult(null, "Страница Стройдвора превышает допустимый размер.");
                }
                return new StroydvorProductReadResult(StroydvorProductParser.Parse(html.ToString(), productId), null);
            }
        }
        catch (HttpRequestException)
        {
            return new StroydvorProductReadResult(null, "Не удалось подключиться к Стройдвору.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new StroydvorProductReadResult(null, "Стройдвор не ответил за 15 секунд.");
        }
        catch (InvalidDataException error)
        {
            // A changed supplier page must not leave a successful synchronization timestamp.
            return new StroydvorProductReadResult(null, error.Message);
        }
        catch (IOException)
        {
            return new StroydvorProductReadResult(null, "Соединение со Стройдвором прервалось при получении карточки.");
        }
        catch (RegexMatchTimeoutException)
        {
            return new StroydvorProductReadResult(null, "Не удалось разобрать формат страницы Стройдвора.");
        }
        return new StroydvorProductReadResult(null, "Не удалось получить карточку товара Стройдвора.");
    }

    private static bool IsSameProduct(Uri target, Uri expected) =>
        target.Scheme == Uri.UriSchemeHttps
        && target.Host.Equals("stroydvor21.ru", StringComparison.OrdinalIgnoreCase)
        && target.IsDefaultPort
        && target.UserInfo.Length == 0
        && target.Query.Length == 0
        && target.Fragment.Length == 0
        && target.AbsolutePath == expected.AbsolutePath;
}