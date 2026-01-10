using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    // Open Library Author Response
    public class OpenLibraryAuthorResource
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("bio")]
        [JsonPropertyName("bio")]
        public object Bio { get; set; } // Can be string or object with "value"

        [JsonProperty("birth_date")]
        [JsonPropertyName("birth_date")]
        public string BirthDate { get; set; }

        [JsonProperty("death_date")]
        [JsonPropertyName("death_date")]
        public string DeathDate { get; set; }

        [JsonProperty("personal_name")]
        [JsonPropertyName("personal_name")]
        public string PersonalName { get; set; }

        [JsonProperty("photos")]
        [JsonPropertyName("photos")]
        public List<int> Photos { get; set; }

        [JsonProperty("links")]
        [JsonPropertyName("links")]
        public List<OpenLibraryLinkResource> Links { get; set; }

        [JsonProperty("remote_ids")]
        [JsonPropertyName("remote_ids")]
        public OpenLibraryRemoteIdsResource RemoteIds { get; set; }
    }

    // Open Library Work Response
    public class OpenLibraryWorkResource
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("subtitle")]
        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public object Description { get; set; } // Can be string or object with "value"

        [JsonProperty("covers")]
        [JsonPropertyName("covers")]
        public List<long> Covers { get; set; }

        [JsonProperty("subjects")]
        [JsonPropertyName("subjects")]
        public List<string> Subjects { get; set; }

        [JsonProperty("subject_people")]
        [JsonPropertyName("subject_people")]
        public List<string> SubjectPeople { get; set; }

        [JsonProperty("authors")]
        [JsonPropertyName("authors")]
        public List<OpenLibraryAuthorRefResource> Authors { get; set; }

        [JsonProperty("first_publish_date")]
        [JsonPropertyName("first_publish_date")]
        public string FirstPublishDate { get; set; }

        [JsonProperty("created")]
        [JsonPropertyName("created")]
        public OpenLibraryDateResource Created { get; set; }

        [JsonProperty("last_modified")]
        [JsonPropertyName("last_modified")]
        public OpenLibraryDateResource LastModified { get; set; }
    }

    // Open Library Edition Response
    public class OpenLibraryEditionResource
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("subtitle")]
        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("authors")]
        [JsonPropertyName("authors")]
        public List<OpenLibraryAuthorRefResource> Authors { get; set; }

        [JsonProperty("publishers")]
        [JsonPropertyName("publishers")]
        public List<string> Publishers { get; set; }

        [JsonProperty("publish_date")]
        [JsonPropertyName("publish_date")]
        public string PublishDate { get; set; }

        [JsonProperty("isbn_10")]
        [JsonPropertyName("isbn_10")]
        public List<string> Isbn10 { get; set; }

        [JsonProperty("isbn_13")]
        [JsonPropertyName("isbn_13")]
        public List<string> Isbn13 { get; set; }

        [JsonProperty("oclc_numbers")]
        [JsonPropertyName("oclc_numbers")]
        public List<string> OclcNumbers { get; set; }

        [JsonProperty("covers")]
        [JsonPropertyName("covers")]
        public List<long> Covers { get; set; }

        [JsonProperty("languages")]
        [JsonPropertyName("languages")]
        public List<OpenLibraryLanguageResource> Languages { get; set; }

        [JsonProperty("number_of_pages")]
        [JsonPropertyName("number_of_pages")]
        public int? NumberOfPages { get; set; }

        [JsonProperty("works")]
        [JsonPropertyName("works")]
        public List<OpenLibraryWorkRefResource> Works { get; set; }

        [JsonProperty("physical_format")]
        [JsonPropertyName("physical_format")]
        public string PhysicalFormat { get; set; }

        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public object Description { get; set; }
    }

    // Search Response
    public class OpenLibrarySearchResponse
    {
        [JsonProperty("numFound")]
        [JsonPropertyName("numFound")]
        public int NumFound { get; set; }

        [JsonProperty("start")]
        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonProperty("docs")]
        [JsonPropertyName("docs")]
        public List<OpenLibrarySearchDoc> Docs { get; set; }
    }

    public class OpenLibrarySearchDoc
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("author_name")]
        [JsonPropertyName("author_name")]
        public List<string> AuthorName { get; set; }

        [JsonProperty("author_key")]
        [JsonPropertyName("author_key")]
        public List<string> AuthorKey { get; set; }

        [JsonProperty("first_publish_year")]
        [JsonPropertyName("first_publish_year")]
        public int? FirstPublishYear { get; set; }

        [JsonProperty("cover_i")]
        [JsonPropertyName("cover_i")]
        public long? CoverId { get; set; }

        [JsonProperty("isbn")]
        [JsonPropertyName("isbn")]
        public List<string> Isbn { get; set; }

        [JsonProperty("edition_key")]
        [JsonPropertyName("edition_key")]
        public List<string> EditionKey { get; set; }

        [JsonProperty("publisher")]
        [JsonPropertyName("publisher")]
        public List<string> Publisher { get; set; }

        [JsonProperty("language")]
        [JsonPropertyName("language")]
        public List<string> Language { get; set; }

        [JsonProperty("number_of_pages_median")]
        [JsonPropertyName("number_of_pages_median")]
        public int? NumberOfPagesMedian { get; set; }

        [JsonProperty("subtitle")]
        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }
    }

    // Author Search Response
    public class OpenLibraryAuthorSearchResponse
    {
        [JsonProperty("numFound")]
        [JsonPropertyName("numFound")]
        public int NumFound { get; set; }

        [JsonProperty("start")]
        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonProperty("docs")]
        [JsonPropertyName("docs")]
        public List<OpenLibraryAuthorSearchDoc> Docs { get; set; }
    }

    public class OpenLibraryAuthorSearchDoc
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("birth_date")]
        [JsonPropertyName("birth_date")]
        public string BirthDate { get; set; }

        [JsonProperty("top_work")]
        [JsonPropertyName("top_work")]
        public string TopWork { get; set; }

        [JsonProperty("work_count")]
        [JsonPropertyName("work_count")]
        public int? WorkCount { get; set; }

        [JsonProperty("top_subjects")]
        [JsonPropertyName("top_subjects")]
        public List<string> TopSubjects { get; set; }
    }

    // Supporting types
    public class OpenLibraryAuthorRefResource
    {
        [JsonProperty("author")]
        [JsonPropertyName("author")]
        public OpenLibraryKeyResource Author { get; set; }

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public OpenLibraryKeyResource Type { get; set; }
    }

    public class OpenLibraryWorkRefResource
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }
    }

    public class OpenLibraryKeyResource
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }
    }

    public class OpenLibraryLinkResource
    {
        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public OpenLibraryKeyResource Type { get; set; }
    }

    public class OpenLibraryRemoteIdsResource
    {
        [JsonProperty("viaf")]
        [JsonPropertyName("viaf")]
        public string Viaf { get; set; }

        [JsonProperty("wikidata")]
        [JsonPropertyName("wikidata")]
        public string Wikidata { get; set; }

        [JsonProperty("isni")]
        [JsonPropertyName("isni")]
        public string Isni { get; set; }
    }

    public class OpenLibraryDateResource
    {
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public DateTime Value { get; set; }
    }

    public class OpenLibraryLanguageResource
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string Key { get; set; }
    }
}
