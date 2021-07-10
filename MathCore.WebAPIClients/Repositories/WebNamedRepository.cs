using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using MathCore.Interfaces.Entities;
using MathCore.Interfaces.Repositories;

using Microsoft.Extensions.Logging;
// ReSharper disable UnusedType.Global

namespace MathCore.WebAPIClients.Repositories
{
    public class WebNamedRepository<T> : WebNamedRepository<T, int>, INamedEntityRepository<T> where T : INamedEntity
    {
        public WebNamedRepository(HttpClient Client, ILogger<WebNamedRepository<T>> Logger) : base(Client, Logger) { }
    }

    public class WebNamedRepository<T, TKey> : WebRepository<T, TKey>, INamedEntityRepository<T, TKey> where T : INamedEntity<TKey>
    {
        public WebNamedRepository(HttpClient Client, ILogger<WebNamedRepository<T, TKey>> Logger) : base(Client, Logger) { }

        public async Task<bool> ExistName(string Name, CancellationToken Cancel = default)
        {
            var response = await _Client.GetAsync($"exist/name/{Name}", Cancel).ConfigureAwait(false);
            return response.StatusCode != HttpStatusCode.NotFound && response.IsSuccessStatusCode;
        }

        public async Task<T> GetByName(string Name, CancellationToken Cancel = default) =>
            await _Client.GetFromJsonAsync<T>($"name/{Name}", Cancel).ConfigureAwait(false);

        public async Task<T> DeleteByName(string Name, CancellationToken Cancel = default)
        {
            _Logger.LogInformation("Delete Name:{0}", Name);

            var response = await _Client.DeleteAsync($"name/{Name}", Cancel).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound) return default;
            var result = await response
               .EnsureSuccessStatusCode()
               .Content
               .ReadFromJsonAsync<T>(cancellationToken: Cancel)
               .ConfigureAwait(false);
            _Logger.LogInformation("Delete Name:{0} complete. Receive response {1}", Name, (object)result ?? "<null>");
            return result;
        }
    }
}