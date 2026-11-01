namespace PaymentsDemo.Tests.Features.Cards;
public class CardsHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<HttpResponseMessage> CreateCardAsync(string cardNumber, string cardHolderName, string expirationDate, string cvv)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cards");
        request.Content = new StringContent($"{{\"cardNumber\":\"{cardNumber}\",\"cardHolderName\":\"{cardHolderName}\",\"expirationDate\":\"{expirationDate}\",\"cvv\":\"{cvv}\"}}", System.Text.Encoding.UTF8, "application/json");

        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetCardAsync(string cardId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/cards/{cardId}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateCardAsync(string cardId, string cardNumber, string cardHolderName, string expirationDate, string cvv)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/cards/{cardId}");
        request.Content = new StringContent($"{{\"cardNumber\":\"{cardNumber}\",\"cardHolderName\":\"{cardHolderName}\",\"expirationDate\":\"{expirationDate}\",\"cvv\":\"{cvv}\"}}", System.Text.Encoding.UTF8, "application/json");

        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteCardAsync(string cardId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/cards/{cardId}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> ListCardsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cards");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetCardByTokenAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/cards/token/{token}");
        return await _httpClient.SendAsync(request);
    }
}