using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ControleEstoque.Desktop.Services;

public class ApiException : Exception
{
    public int Status { get; }
    public ApiException(string mensagem, int status = 0) : base(mensagem) => Status = status;
}

/// <summary>Cliente HTTP da API: envia o token JWT e traduz erros em mensagens legíveis.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private string? _token;

    /// <summary>Disparado quando a API responde 401 com o usuário já logado.</summary>
    public event Action? SessaoExpirada;

    public string? Token
    {
        get => _token;
        set
        {
            _token = value;
            _http.DefaultRequestHeaders.Authorization =
                string.IsNullOrEmpty(value) ? null : new AuthenticationHeaderValue("Bearer", value);
        }
    }

    public void Configurar(string url)
    {
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
        }
    }

    public Task<T> GetAsync<T>(string url) => EnviarAsync<T>(HttpMethod.Get, url, null);
    public Task<T> PostAsync<T>(string url, object? corpo) => EnviarAsync<T>(HttpMethod.Post, url, corpo);
    public Task<T> PutAsync<T>(string url, object? corpo) => EnviarAsync<T>(HttpMethod.Put, url, corpo);
    public Task DeleteAsync(string url) => EnviarAsync<object>(HttpMethod.Delete, url, null);

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string url, object? corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, url);
        if (corpo != null)
        {
            var json = JsonSerializer.Serialize(corpo, corpo.GetType(), Opcoes);
            requisicao.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage resposta;
        try
        {
            resposta = await _http.SendAsync(requisicao);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("A API demorou demais para responder. Tente novamente.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException("Não foi possível conectar à API. Verifique o endereço e se ela está em execução.");
        }

        using (resposta)
        {
            var texto = await resposta.Content.ReadAsStringAsync();
            var status = (int)resposta.StatusCode;

            if (!resposta.IsSuccessStatusCode)
            {
                if (status == 401 && _token != null)
                    SessaoExpirada?.Invoke();

                throw new ApiException(ExtrairMensagem(status, texto), status);
            }

            if (string.IsNullOrWhiteSpace(texto) || typeof(T) == typeof(object))
                return default!;

            return JsonSerializer.Deserialize<T>(texto, Opcoes)!;
        }
    }

    private static string ExtrairMensagem(int status, string texto)
    {
        string? mensagem = null;

        try
        {
            using var doc = JsonDocument.Parse(texto);
            var raiz = doc.RootElement;

            if (raiz.ValueKind == JsonValueKind.Object)
            {
                // Erros de validação do ASP.NET: { "errors": { "Campo": ["mensagem"] } }
                if (raiz.TryGetProperty("errors", out var erros) && erros.ValueKind == JsonValueKind.Object)
                {
                    mensagem = string.Join("\n", erros.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(e => e.GetString())));
                }
                else
                {
                    foreach (var chave in new[] { "detail", "mensagem", "message", "title" })
                    {
                        if (raiz.TryGetProperty(chave, out var v) && v.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(v.GetString()))
                        {
                            mensagem = v.GetString();
                            break;
                        }
                    }
                }
            }
        }
        catch (JsonException) { /* corpo vazio ou não JSON */ }

        return status switch
        {
            401 => mensagem ?? "Sessão expirada ou credenciais inválidas.",
            403 => "Você não tem permissão para esta ação.",
            404 => mensagem ?? "Recurso não encontrado.",
            429 => "Muitas tentativas. Aguarde um minuto e tente de novo.",
            _ => mensagem ?? $"Erro {status} ao falar com a API."
        };
    }
}