using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using MathCore.Interfaces.Entities;
using MathCore.Interfaces.Repositories;

using Microsoft.Extensions.Logging;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global

namespace MathCore.WebAPIClients.Repositories
{
    public class WebRepository<T> : WebRepository<T, int>, IEntityRepository<T> where T : IEntity
    {
        public WebRepository(HttpClient Client, ILogger<WebRepository<T>> Logger) : base(Client, Logger) { }
    }

    public class WebRepository<T, TKey> : IEntityRepository<T, TKey> where T : IEntity<TKey>
    {
        protected readonly HttpClient _Client;
        protected readonly ILogger<WebRepository<T, TKey>> _Logger;

        public WebRepository(HttpClient Client, ILogger<WebRepository<T, TKey>> Logger)
        {
            _Client = Client;
            _Logger = Logger;
        }

        public async Task<bool> ExistId(TKey Id, CancellationToken Cancel = default)
        {
            var response = await _Client.GetAsync($"exist/id/{Id}", Cancel).ConfigureAwait(false);
            return response.StatusCode != HttpStatusCode.NotFound && response.IsSuccessStatusCode;

        }

        public async Task<bool> Exist(T item, CancellationToken Cancel = default)
        {
            var response = await _Client.PostAsJsonAsync("exist", item, Cancel).ConfigureAwait(false);
            return response.StatusCode != HttpStatusCode.NotFound && response.IsSuccessStatusCode;
        }

        public async Task<int> GetCount(CancellationToken Cancel = default) =>
            await _Client.GetFromJsonAsync<int>("count", Cancel).ConfigureAwait(false);

        public async Task<IEnumerable<T>> GetAll(CancellationToken Cancel = default) => await _Client.GetFromJsonAsync<IEnumerable<T>>("", Cancel).ConfigureAwait(false);

        public async Task<IEnumerable<T>> Get(int Skip, int Count, CancellationToken Cancel = default) => await _Client.GetFromJsonAsync<IEnumerable<T>>($"items[{Skip}:{Count}]", Cancel).ConfigureAwait(false);

        public async Task<IPage<T>> GetPage(int PageNumber, int PageSize, CancellationToken Cancel = default)
        {
            //return await _Client.GetFromJsonAsync<PagedItems>($"page[{PageIndex}/{PageSize}]", Cancel).ConfigureAwait(false);
            var response = await _Client.GetAsync($"page[{PageNumber}/{PageSize}]", HttpCompletionOption.ResponseHeadersRead, Cancel);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new PagedItems
                {
                    Items = Enumerable.Empty<T>(),
                    ItemsCount = 0,
                    TotalCount = 0,
                    PageIndex = PageNumber,
                    PageSize = PageSize,
                };
            return await response
               .EnsureSuccessStatusCode()
               .Content
               .ReadFromJsonAsync<PagedItems>(cancellationToken: Cancel);
        }

        private class PagedItems : IPage<T>
        {
            public IEnumerable<T> Items { get; init; }
            public int ItemsCount { get; init; }
            public int TotalCount { get; init; }
            public int PageIndex { get; init; }
            public int PageSize { get; init; }
            public int TotalPagesCount => (int)Math.Ceiling((double)TotalCount / PageSize);
        }

        public async Task<T> GetById(TKey Id, CancellationToken Cancel = default) => await _Client.GetFromJsonAsync<T>($"{Id}", Cancel).ConfigureAwait(false);

        public async Task<T> Add(T item, CancellationToken Cancel = default)
        {
            _Logger.LogInformation("Add {0}", item);
            var response = await _Client.PostAsJsonAsync("", item, Cancel).ConfigureAwait(false);
            var result = await response
               .EnsureSuccessStatusCode()
               .Content
               .ReadFromJsonAsync<T>(cancellationToken: Cancel).ConfigureAwait(false);
            _Logger.LogInformation("Add {0} complete. Receive response {1}", item, (object)result ?? "<null>");
            return result;
        }

        public async Task<T> Update(T item, CancellationToken Cancel = default)
        {
            _Logger.LogInformation("Update {0}", item);
            var response = await _Client.PutAsJsonAsync("", item, Cancel).ConfigureAwait(false);
            var result = await response
               .EnsureSuccessStatusCode()
               .Content
               .ReadFromJsonAsync<T>(cancellationToken: Cancel).ConfigureAwait(false);
            _Logger.LogInformation("Update {0} complete. Receive response {1}", item, (object)result ?? "<null>");
            return result;
        }

        public async Task<T> Delete(T item, CancellationToken Cancel = default)
        {
            _Logger.LogInformation("Delete {0}", item);
            var response = await _Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "") { Content = JsonContent.Create(item) }, Cancel).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _Logger.LogInformation("Delete {0} - item not exist", item);
                return default;
            }
            var result = await response
               .EnsureSuccessStatusCode()
               .Content
               .ReadFromJsonAsync<T>(cancellationToken: Cancel).ConfigureAwait(false);
            _Logger.LogInformation("Delete {0} complete. Receive response {1}", item, (object)result ?? "<null>");
            return result;
        }

        public async Task<T> DeleteById(TKey id, CancellationToken Cancel = default)
        {
            _Logger.LogInformation("Delete id:{0}", id);
            var response = await _Client.DeleteAsync($"{id}", Cancel).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _Logger.LogInformation("Delete id:{0} - item not exist", id);
                return default;
            }
            var result = await response
               .EnsureSuccessStatusCode()
               .Content
               .ReadFromJsonAsync<T>(cancellationToken: Cancel).ConfigureAwait(false);
            _Logger.LogInformation("Delete id:{0} complete. Receive response {1}", id, (object)result ?? "<null>");
            return result;
        }
    }
}
