using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using LazyCache;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using Polly;
using Polly.CircuitBreaker;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    public class OpenLibraryProxy : IProvideAuthorInfo, IProvideBookInfo, ISearchForNewBook, ISearchForNewAuthor, ISearchForNewEntity
    {
        private static readonly JsonSerializerOptions SerializerSettings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClient _httpClient;
        private readonly IOpenLibraryRequestBuilder _requestBuilder;
        private readonly Logger _logger;
        private readonly ICached<Author> _authorCache;
        private readonly ICached<Book> _bookCache;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncPolicy _retryPolicy;

        public OpenLibraryProxy(
            IHttpClient httpClient,
            ICacheManager cacheManager,
            Logger logger)
        {
            _httpClient = httpClient;
            _requestBuilder = new OpenLibraryRequestBuilder();
            _logger = logger;
            _authorCache = cacheManager.GetCache<Author>(GetType(), "authors");
            _bookCache = cacheManager.GetCache<Book>(GetType(), "books");

            // Retry policy: 3 retries with exponential backoff + jitter
            _retryPolicy = Policy
                .Handle<WebException>()
                .Or<HttpException>(ex => ex.Response.StatusCode == HttpStatusCode.TooManyRequests ||
                                        ex.Response.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) +
                                                     TimeSpan.FromMilliseconds(new Random().Next(0, 1000)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.Warn(exception, "Retry {0} after {1}s due to: {2}", retryCount, timeSpan.TotalSeconds, exception.Message);
                    });

            // Circuit breaker: open after 5 consecutive failures, stay open for 60s
            _circuitBreakerPolicy = Policy
                .Handle<WebException>()
                .Or<HttpException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(60),
                    onBreak: (exception, duration) =>
                    {
                        _logger.Error(exception, "Circuit breaker opened for {0}s", duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        _logger.Info("Circuit breaker reset");
                    });
        }

        public Author GetAuthorInfo(string openLibraryId, bool useCache = true)
        {
            var cacheKey = $"author:{openLibraryId}";

            if (useCache)
            {
                var cached = _authorCache.Find(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
            }

            try
            {
                var author = ExecuteWithResilience(() =>
                {
                    var request = _requestBuilder.GetAuthor(openLibraryId);
                    var response = _httpClient.Get<OpenLibraryAuthorResource>(request);

                    return MapAuthor(response.Resource);
                }).GetAwaiter().GetResult();

                _authorCache.Set(cacheKey, author, TimeSpan.FromDays(30));
                return author;
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Warn("Author not found: {0}", openLibraryId);
                throw new AuthorNotFoundException(openLibraryId);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.Error(ex, "Circuit breaker is open, serving from cache if available");
                var cached = _authorCache.Find(cacheKey);
                if (cached != null)
                {
                    return cached;
                }

                throw new MetadataException("Open Library is temporarily unavailable", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching author info for: {0}", openLibraryId);
                throw new MetadataException($"Failed to get author info: {openLibraryId}", ex);
            }
        }

        public HashSet<string> GetChangedAuthors(DateTime startTime)
        {
            // OpenLibrary doesn't provide a "changed since" API
            // Return empty set - full refresh will be needed
            _logger.Debug("OpenLibrary doesn't support incremental changes, returning empty set");
            return new HashSet<string>();
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string openLibraryWorkId)
        {
            var cacheKey = $"book:{openLibraryWorkId}";
            var cached = _bookCache.Find(cacheKey);

            if (cached != null)
            {
                return new Tuple<string, Book, List<AuthorMetadata>>(
                    cached.ForeignBookId,
                    cached,
                    new List<AuthorMetadata>());
            }

            try
            {
                var result = ExecuteWithResilience(async () =>
                {
                    var workRequest = _requestBuilder.GetWork(openLibraryWorkId);
                    var workResponse = await _httpClient.GetAsync<OpenLibraryWorkResource>(workRequest);
                    var work = workResponse.Resource;

                    var book = MapWork(work);
                    var authorMetadata = new List<AuthorMetadata>();

                    // Fetch author details
                    if (work.Authors != null && work.Authors.Any())
                    {
                        foreach (var authorRef in work.Authors.Take(5)) // Limit to 5 authors
                        {
                            if (authorRef?.Author?.Key != null)
                            {
                                try
                                {
                                    var author = GetAuthorInfo(authorRef.Author.Key);
                                    if (author?.Metadata?.Value != null)
                                    {
                                        authorMetadata.Add(author.Metadata.Value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Warn(ex, "Failed to fetch author: {0}", authorRef.Author.Key);
                                }
                            }
                        }
                    }

                    _bookCache.Set(cacheKey, book, TimeSpan.FromDays(14));
                    return new Tuple<string, Book, List<AuthorMetadata>>(book.ForeignBookId, book, authorMetadata);
                }).GetAwaiter().GetResult();

                return result;
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Warn("Work not found: {0}", openLibraryWorkId);
                throw new BookNotFoundException(openLibraryWorkId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching book info for: {0}", openLibraryWorkId);
                throw new MetadataException($"Failed to get book info: {openLibraryWorkId}", ex);
            }
        }

        public List<Book> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            try
            {
                var query = string.IsNullOrWhiteSpace(author) ? title : $"{title} {author}";

                return ExecuteWithResilience(async () =>
                {
                    var request = _requestBuilder.SearchBook(query, limit: 20);
                    var response = await _httpClient.GetAsync<OpenLibrarySearchResponse>(request);

                    var books = new List<Book>();
                    foreach (var doc in response.Resource.Docs.Take(20))
                    {
                        try
                        {
                            var book = MapSearchDocToBook(doc);
                            if (book != null)
                            {
                                books.Add(book);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Failed to map search result: {0}", doc.Title);
                        }
                    }

                    return books;
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error searching for book: {0} by {1}", title, author);
                return new List<Book>();
            }
        }

        public List<Book> SearchByIsbn(string isbn)
        {
            try
            {
                var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");

                return ExecuteWithResilience(async () =>
                {
                    var request = _requestBuilder.SearchBook($"isbn:{cleanIsbn}", limit: 10);
                    var response = await _httpClient.GetAsync<OpenLibrarySearchResponse>(request);

                    var books = new List<Book>();
                    foreach (var doc in response.Resource.Docs)
                    {
                        try
                        {
                            var book = MapSearchDocToBook(doc);
                            if (book != null)
                            {
                                books.Add(book);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Failed to map ISBN search result: {0}", doc.Title);
                        }
                    }

                    return books;
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error searching by ISBN: {0}", isbn);
                return new List<Book>();
            }
        }

        public List<Book> SearchByAsin(string asin)
        {
            // OpenLibrary doesn't directly support ASIN search
            // Try searching by ASIN as a general query
            _logger.Debug("ASIN search not directly supported, attempting general search");
            return SearchForNewBook(asin, null);
        }

        public List<Book> SearchByGoodreadsBookId(int goodreadsId, bool getAllEditions)
        {
            // OpenLibrary doesn't have Goodreads ID mapping
            _logger.Debug("Goodreads ID search not supported in OpenLibrary");
            return new List<Book>();
        }

        public List<Author> SearchForNewAuthor(string query)
        {
            try
            {
                return ExecuteWithResilience(async () =>
                {
                    var request = _requestBuilder.SearchAuthor(query, limit: 20);
                    var response = await _httpClient.GetAsync<OpenLibraryAuthorSearchResponse>(request);

                    var authors = new List<Author>();
                    foreach (var doc in response.Resource.Docs.Take(20))
                    {
                        try
                        {
                            var author = MapAuthorSearchDoc(doc);
                            if (author != null)
                            {
                                authors.Add(author);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Failed to map author search result: {0}", doc.Name);
                        }
                    }

                    return authors;
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error searching for author: {0}", query);
                return new List<Author>();
            }
        }

        public List<object> SearchForNewEntity(string query)
        {
            var results = new List<object>();

            // Search both authors and books
            results.AddRange(SearchForNewAuthor(query).Cast<object>());
            results.AddRange(SearchForNewBook(query, null).Cast<object>());

            return results;
        }

        private async System.Threading.Tasks.Task<T> ExecuteWithResilience<T>(Func<System.Threading.Tasks.Task<T>> action)
        {
            return await Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy).ExecuteAsync(action);
        }

        private Author MapAuthor(OpenLibraryAuthorResource resource)
        {
            var bio = ExtractTextValue(resource.Bio);

            return new Author
            {
                ForeignAuthorId = resource.Key?.TrimStart('/'),
                Name = resource.Name ?? resource.PersonalName,
                Overview = bio,
                Metadata = new Lazy<AuthorMetadata>(() => new AuthorMetadata
                {
                    ForeignAuthorId = resource.Key?.TrimStart('/'),
                    Name = resource.Name ?? resource.PersonalName,
                    Overview = bio
                }),
                Images = MapAuthorImages(resource.Photos)
            };
        }

        private Author MapAuthorSearchDoc(OpenLibraryAuthorSearchDoc doc)
        {
            return new Author
            {
                ForeignAuthorId = doc.Key?.TrimStart('/'),
                Name = doc.Name,
                Metadata = new Lazy<AuthorMetadata>(() => new AuthorMetadata
                {
                    ForeignAuthorId = doc.Key?.TrimStart('/'),
                    Name = doc.Name
                })
            };
        }

        private Book MapWork(OpenLibraryWorkResource work)
        {
            var description = ExtractTextValue(work.Description);

            return new Book
            {
                ForeignBookId = work.Key?.TrimStart('/'),
                Title = work.Title,
                Overview = description,
                ReleaseDate = ParseDate(work.FirstPublishDate),
                Images = MapWorkImages(work.Covers),
                Editions = new List<Edition>()
            };
        }

        private Book MapSearchDocToBook(OpenLibrarySearchDoc doc)
        {
            if (string.IsNullOrWhiteSpace(doc.Key))
            {
                return null;
            }

            return new Book
            {
                ForeignBookId = doc.Key.TrimStart('/'),
                Title = doc.Title,
                ReleaseDate = doc.FirstPublishYear.HasValue ? new DateTime(doc.FirstPublishYear.Value, 1, 1) : (DateTime?)null,
                Images = MapCoverIdImages(doc.CoverId),
                Editions = new List<Edition>(),
                AuthorMetadata = MapSearchAuthors(doc)
            };
        }

        private List<AuthorMetadata> MapSearchAuthors(OpenLibrarySearchDoc doc)
        {
            var authors = new List<AuthorMetadata>();

            if (doc.AuthorName != null && doc.AuthorKey != null)
            {
                for (int i = 0; i < Math.Min(doc.AuthorName.Count, doc.AuthorKey.Count); i++)
                {
                    authors.Add(new AuthorMetadata
                    {
                        ForeignAuthorId = doc.AuthorKey[i].TrimStart('/'),
                        Name = doc.AuthorName[i]
                    });
                }
            }

            return authors;
        }

        private List<MediaCover.MediaCover> MapAuthorImages(List<int> photoIds)
        {
            if (photoIds == null || !photoIds.Any())
            {
                return new List<MediaCover.MediaCover>();
            }

            return new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover
                {
                    Url = $"https://covers.openlibrary.org/a/id/{photoIds.First()}-L.jpg",
                    CoverType = MediaCoverTypes.Poster
                }
            };
        }

        private List<MediaCover.MediaCover> MapWorkImages(List<long> coverIds)
        {
            if (coverIds == null || !coverIds.Any())
            {
                return new List<MediaCover.MediaCover>();
            }

            return new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover
                {
                    Url = OpenLibraryRequestBuilder.GetCoverUrl(coverIds.First()),
                    CoverType = MediaCoverTypes.Cover
                }
            };
        }

        private List<MediaCover.MediaCover> MapCoverIdImages(long? coverId)
        {
            if (!coverId.HasValue)
            {
                return new List<MediaCover.MediaCover>();
            }

            return new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover
                {
                    Url = OpenLibraryRequestBuilder.GetCoverUrl(coverId.Value),
                    CoverType = MediaCoverTypes.Cover
                }
            };
        }

        private string ExtractTextValue(object field)
        {
            if (field == null)
            {
                return null;
            }

            if (field is string str)
            {
                return str;
            }

            // Handle object with "value" property
            try
            {
                var element = JsonSerializer.SerializeToElement(field);
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var valueProp))
                {
                    return valueProp.GetString();
                }
            }
            catch
            {
                // Ignore
            }

            return field.ToString();
        }

        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // Try to parse various date formats
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }

            // Try to extract year
            if (int.TryParse(dateString, out var year) && year > 0 && year < 9999)
            {
                return new DateTime(year, 1, 1);
            }

            return null;
        }
    }
}
