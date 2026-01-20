using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Pokedex.Models;
using Pokedex.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Pokedex.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPokeApiClient _client;
        private readonly IMemoryCache _cache;

        private const int DefaultPageSize = 20;
        private const int MaxDetailConcurrency = 8;
        private const int MaxLiteConcurrency = 10;

        private static readonly TimeSpan TypesTtl = TimeSpan.FromHours(12);
        private static readonly TimeSpan AllNamesTtl = TimeSpan.FromHours(12);
        private static readonly TimeSpan TypePoolTtl = TimeSpan.FromHours(12);
        private static readonly TimeSpan LiteByNameTtl = TimeSpan.FromDays(7);

        public HomeController(IPokeApiClient client, IMemoryCache cache)
        {
            _client = client;
            _cache = cache;
        }

        [HttpGet("/")]
        public IActionResult Root() => RedirectToAction(nameof(Index));

        [HttpGet("/Home/Index")]
        public async Task<IActionResult> Index(string? searchName, string? selectedType, int page = 1, int pageSize = DefaultPageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            var term = (searchName ?? "").Trim();
            var hasTerm = term.Length > 0;
            var hasType = !string.IsNullOrWhiteSpace(selectedType);

            // Start types load early (cached) so it overlaps other awaits
            var typesTask = GetTypesCachedAsync();

            var vm = new PokedexIndexViewModel
            {
                SearchName = searchName,
                SelectedType = selectedType,
                Page = page,
                PageSize = pageSize
            };

            // ==============================
            // A) TYPE FILTER (OPTIONAL NAME)
            // ==============================
            if (hasType)
            {
                var allInType = await GetTypePoolCachedAsync(selectedType!);

                var filtered = allInType
                    .Where(p => !hasTerm || ((string)p.Name).Contains(term, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Id)
                    .ToList();

                vm.TotalCount = filtered.Count;

                var totalPages = Math.Max(1, (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize));
                vm.Page = Math.Min(vm.Page, totalPages);

                var pageItems = filtered
                    .Skip((vm.Page - 1) * vm.PageSize)
                    .Take(vm.PageSize)
                    .ToList();

                var ids = pageItems.Select(p => (object?)p.Id).ToList();

                vm.Results = await FetchDetailsBoundedAsync(ids, MaxDetailConcurrency);
                vm.AllTypes = await typesTask;

                return View("~/Views/Home/Index.cshtml", vm);
            }

            // ==============================
            // B) NAME SEARCH ONLY
            // ==============================
            if (hasTerm)
            {
                var exact = await _client.GetDetailsAsync(term);
                if (exact != null)
                {
                    vm.TotalCount = 1;
                    vm.Page = 1;
                    vm.PageSize = 1;
                    vm.Results = new List<PokemonDetails> { exact };
                    vm.AllTypes = await typesTask;
                    return View("~/Views/Home/Index.cshtml", vm);
                }

                var allNames = await GetAllNamesCachedAsync();

                var matches = allNames
                    .Where(n => n.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 0)
                {
                    vm.TotalCount = 0;
                    vm.Results = new List<PokemonDetails>();
                    vm.AllTypes = await typesTask;
                    return View("~/Views/Home/Index.cshtml", vm);
                }

                var liteResults = await FetchLiteBoundedCachedByNameAsync(matches, MaxLiteConcurrency);

                var ordered = liteResults
                    .Where(x => x != null && x.Id != null)
                    .OrderBy(x => x.Id)
                    .ToList();

                vm.TotalCount = ordered.Count;

                var totalPages = Math.Max(1, (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize));
                vm.Page = Math.Min(vm.Page, totalPages);

                var pageItems = ordered
                    .Skip((vm.Page - 1) * vm.PageSize)
                    .Take(vm.PageSize)
                    .ToList();

                var ids = pageItems.Select(p => (object?)p.Id).ToList();

                vm.Results = await FetchDetailsBoundedAsync(ids, MaxDetailConcurrency);
                vm.AllTypes = await typesTask;

                return View("~/Views/Home/Index.cshtml", vm);
            }

            // ==============================
            // C) DEFAULT PAGING
            // ==============================
            vm.TotalCount = await _client.GetTotalPokemonCountAsync();

            var totalPagesAll = Math.Max(1, (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize));
            vm.Page = Math.Min(vm.Page, totalPagesAll);

            vm.Results = await _client.GetPageAsync(vm.Page, vm.PageSize);
            vm.AllTypes = await typesTask;

            return View("~/Views/Home/Index.cshtml", vm);
        }

        // ==============================
        // CACHED HELPERS
        // ==============================
        private Task<List<string>> GetAllNamesCachedAsync() =>
            _cache.GetOrCreateAsync("poke:all-names", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = AllNamesTtl;
                return await _client.GetAllNamesAsync() ?? new List<string>();
            })!;

        private Task<List<string>> GetTypesCachedAsync() =>
            _cache.GetOrCreateAsync("poke:types", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TypesTtl;
                return await _client.GetTypesAsync() ?? new List<string>();
            })!;

        private Task<List<dynamic>> GetTypePoolCachedAsync(string type) =>
            _cache.GetOrCreateAsync($"poke:type:{type.ToLowerInvariant()}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TypePoolTtl;

                var pool = await _client.GetPokemonByTypeAsync(type, max: 20000);
                return (pool ?? Enumerable.Empty<object>()).Select(x => (dynamic)x).ToList();
            })!;

        // ==============================
        // BOUNDED FETCHERS
        // ==============================
        private async Task<List<PokemonDetails>> FetchDetailsBoundedAsync(IEnumerable<object?> ids, int maxConcurrency)
        {
            var normalizedIds = NormalizeIds(ids).ToList();
            if (normalizedIds.Count == 0)
                return new List<PokemonDetails>();

            var sem = new SemaphoreSlim(Math.Max(1, maxConcurrency));

            var tasks = normalizedIds.Select(async id =>
            {
                await sem.WaitAsync();
                try
                {
                    return await _client.GetDetailsAsync(id.ToString());
                }
                finally
                {
                    sem.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            return results
                .Where(r => r != null)
                .OrderBy(r => r!.Id)
                .ToList()!;
        }

        private static IEnumerable<int> NormalizeIds(IEnumerable<object?> ids)
        {
            if (ids == null) yield break;

            foreach (var raw in ids)
            {
                if (raw == null) continue;

                if (raw is int i)
                {
                    yield return i;
                    continue;
                }

                if (raw is long l)
                {
                    if (l >= int.MinValue && l <= int.MaxValue)
                        yield return (int)l;
                    continue;
                }

                if (raw is string s)
                {
                    s = s.Trim();
                    if (int.TryParse(s, out var parsed))
                        yield return parsed;
                    continue;
                }

                // IMPORTANT FIX:
                // You can't "yield return" inside a try/catch in an iterator method in some C# versions.
                // So we do conversion in try/catch, store the result, then yield outside.
                if (raw is IConvertible)
                {
                    int conv;
                    try
                    {
                        conv = Convert.ToInt32(raw);
                    }
                    catch
                    {
                        continue;
                    }

                    yield return conv;
                }
            }
        }

        private async Task<List<dynamic?>> FetchLiteBoundedCachedByNameAsync(List<string> names, int maxConcurrency)
        {
            if (names == null || names.Count == 0)
                return new List<dynamic?>();

            var sem = new SemaphoreSlim(Math.Max(1, maxConcurrency));

            var tasks = names.Select(async name =>
            {
                var key = $"poke:lite:{name.ToLowerInvariant()}";

                if (_cache.TryGetValue(key, out object? cachedObj))
                    return cachedObj as dynamic;

                await sem.WaitAsync();
                try
                {
                    if (_cache.TryGetValue(key, out cachedObj))
                        return cachedObj as dynamic;

                    var lite = await _client.GetPokemonByNameAsync(name);

                    _cache.Set(key, lite, LiteByNameTtl);
                    return (dynamic?)lite;
                }
                finally
                {
                    sem.Release();
                }
            });

            return (await Task.WhenAll(tasks)).ToList();
        }
    }
}
