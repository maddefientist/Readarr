using System;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    public interface IOpenLibraryRequestBuilder
    {
        HttpRequest GetAuthor(string authorKey);
        HttpRequest GetWork(string workKey);
        HttpRequest GetEdition(string editionKey);
        HttpRequest SearchBook(string query, int limit = 20);
        HttpRequest SearchBookByIsbn(string isbn);
        HttpRequest SearchAuthor(string query, int limit = 20);
        HttpRequest GetAuthorWorks(string authorKey, int limit = 50);
    }

    public class OpenLibraryRequestBuilder : IOpenLibraryRequestBuilder
    {
        private const string BaseUrl = "https://openlibrary.org";
        private const string CoversBaseUrl = "https://covers.openlibrary.org";

        public HttpRequest GetAuthor(string authorKey)
        {
            var cleanKey = CleanKey(authorKey, "/authors/");
            var url = $"{BaseUrl}/authors/{cleanKey}.json";
            return new HttpRequestBuilder(url).Build();
        }

        public HttpRequest GetWork(string workKey)
        {
            var cleanKey = CleanKey(workKey, "/works/");
            var url = $"{BaseUrl}/works/{cleanKey}.json";
            return new HttpRequestBuilder(url).Build();
        }

        public HttpRequest GetEdition(string editionKey)
        {
            var cleanKey = CleanKey(editionKey, "/books/");
            var url = $"{BaseUrl}/books/{cleanKey}.json";
            return new HttpRequestBuilder(url).Build();
        }

        public HttpRequest SearchBook(string query, int limit = 20)
        {
            var url = $"{BaseUrl}/search.json";
            var builder = new HttpRequestBuilder(url)
                .AddQueryParam("q", query)
                .AddQueryParam("limit", limit)
                .AddQueryParam("fields", "key,title,author_name,author_key,first_publish_year,cover_i,isbn,edition_key,publisher,language,number_of_pages_median,subtitle");

            return builder.Build();
        }

        public HttpRequest SearchBookByIsbn(string isbn)
        {
            // ISBN search uses the bibkeys endpoint
            var url = $"{BaseUrl}/api/books";
            var builder = new HttpRequestBuilder(url)
                .AddQueryParam("bibkeys", $"ISBN:{isbn}")
                .AddQueryParam("format", "json")
                .AddQueryParam("jscmd", "data");

            return builder.Build();
        }

        public HttpRequest SearchAuthor(string query, int limit = 20)
        {
            var url = $"{BaseUrl}/search/authors.json";
            var builder = new HttpRequestBuilder(url)
                .AddQueryParam("q", query)
                .AddQueryParam("limit", limit);

            return builder.Build();
        }

        public HttpRequest GetAuthorWorks(string authorKey, int limit = 50)
        {
            var cleanKey = CleanKey(authorKey, "/authors/");
            var url = $"{BaseUrl}/authors/{cleanKey}/works.json";
            var builder = new HttpRequestBuilder(url)
                .AddQueryParam("limit", limit);

            return builder.Build();
        }

        public static string GetCoverUrl(long coverId, string size = "L")
        {
            // size: S (small), M (medium), L (large)
            return $"{CoversBaseUrl}/b/id/{coverId}-{size}.jpg";
        }

        private static string CleanKey(string key, string prefix)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            // Remove prefix if it exists (e.g., "/authors/OL123A" -> "OL123A")
            if (key.StartsWith(prefix))
            {
                return key.Substring(prefix.Length);
            }

            // Remove leading slash if present
            return key.TrimStart('/');
        }
    }
}
