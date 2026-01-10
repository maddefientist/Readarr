using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.MetadataSource.OpenLibrary;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MetadataSource.OpenLibrary
{
    [TestFixture]
    public class OpenLibraryProxyFixture : CoreTest<OpenLibraryProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            Mocker.GetMock<ICacheManager>()
                  .Setup(s => s.GetCache<Author>(It.IsAny<Type>(), It.IsAny<string>()))
                  .Returns(new CachedDictionary<Author>());

            Mocker.GetMock<ICacheManager>()
                  .Setup(s => s.GetCache<Book>(It.IsAny<Type>(), It.IsAny<string>()))
                  .Returns(new CachedDictionary<Book>());
        }

        [Test]
        [Ignore("Integration test - requires network")]
        public void should_be_able_to_get_author_by_id()
        {
            // J.K. Rowling
            var author = Subject.GetAuthorInfo("/authors/OL23919A");

            author.Should().NotBeNull();
            author.Name.Should().Be("J. K. Rowling");
            author.ForeignAuthorId.Should().Be("/authors/OL23919A");
        }

        [Test]
        [Ignore("Integration test - requires network")]
        public void should_be_able_to_get_book_by_id()
        {
            // Harry Potter and the Philosopher's Stone
            var result = Subject.GetBookInfo("/works/OL82563W");

            result.Should().NotBeNull();
            result.Item2.Should().NotBeNull();
            result.Item2.Title.Should().Contain("Harry Potter");
        }

        [Test]
        [Ignore("Integration test - requires network")]
        public void should_be_able_to_search_for_book()
        {
            var results = Subject.SearchForNewBook("Harry Potter", "J.K. Rowling");

            results.Should().NotBeNull();
            results.Should().NotBeEmpty();
            results[0].Title.Should().Contain("Harry Potter");
        }

        [Test]
        [Ignore("Integration test - requires network")]
        public void should_be_able_to_search_for_author()
        {
            var results = Subject.SearchForNewAuthor("J.K. Rowling");

            results.Should().NotBeNull();
            results.Should().NotBeEmpty();
            results[0].Name.Should().Contain("Rowling");
        }

        [Test]
        [Ignore("Integration test - requires network")]
        public void should_be_able_to_search_by_isbn()
        {
            // ISBN for Harry Potter and the Philosopher's Stone
            var results = Subject.SearchByIsbn("9780439708180");

            results.Should().NotBeNull();
            // ISBN searches may return no results if not in OpenLibrary
        }

        [Test]
        public void should_handle_missing_author_gracefully()
        {
            // Use mocked HTTP client to return 404
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get<OpenLibraryAuthorResource>(It.IsAny<HttpRequest>()))
                  .Throws(new HttpException(new HttpResponse(new HttpRequest("http://test.com"), new HttpHeader(), Array.Empty<byte>(), HttpStatusCode.NotFound)));

            Action act = () => Subject.GetAuthorInfo("/authors/INVALID");

            act.Should().Throw<AuthorNotFoundException>();
        }

        [Test]
        public void should_handle_missing_book_gracefully()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get<OpenLibraryWorkResource>(It.IsAny<HttpRequest>()))
                  .Throws(new HttpException(new HttpResponse(new HttpRequest("http://test.com"), new HttpHeader(), Array.Empty<byte>(), HttpStatusCode.NotFound)));

            Action act = () => Subject.GetBookInfo("/works/INVALID");

            act.Should().Throw<BookNotFoundException>();
        }
    }
}
