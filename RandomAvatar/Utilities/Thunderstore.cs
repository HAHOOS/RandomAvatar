using Semver;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RandomAvatar.Utilities
{
    /// <summary>
    /// Class that contains functionality for interacting with Thunderstore
    /// </summary>
    public class Thunderstore
    {
        /// <summary>
        /// User Agent header that will be associated with every request
        /// </summary>
        public readonly string UserAgent;

        /// <summary>
        /// At the time of writing this code, V1 of Thunderstore API is not yet deprecated and the new version is still experimental
        /// <para>In the case of deprecation, make this <see langword="true"/></para>
        /// </summary>
        public bool IsV1Deprecated = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thunderstore"/> class.
        /// </summary>
        /// <param name="userAgent"><inheritdoc cref="UserAgent"/></param>
        public Thunderstore(string userAgent)
        {
            UserAgent = userAgent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thunderstore"/> class.
        /// </summary>
        /// <param name="userAgent"><inheritdoc cref="UserAgent"/></param>
        /// <param name="isV1Deprecated"><inheritdoc cref="IsV1Deprecated"/></param>
        public Thunderstore(string userAgent, bool isV1Deprecated) : this(userAgent)
        {
            IsV1Deprecated = isV1Deprecated;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thunderstore"/> class with UserAgent configured with information from assembly
        /// </summary>
        public Thunderstore()
        {
            var executing = System.Reflection.Assembly.GetExecutingAssembly();
            if (executing != null)
            {
                var name = executing.GetName();
                if (name != null)
                {
                    UserAgent = $"{name.Name} / {name.Version} C# Application";
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thunderstore"/> class with UserAgent configured with information from assembly
        /// </summary>
        /// <param name="isV1Deprecated"><inheritdoc cref="IsV1Deprecated"/></param>
        public Thunderstore(bool isV1Deprecated)
        {
            this.IsV1Deprecated = isV1Deprecated;
            var executing = System.Reflection.Assembly.GetExecutingAssembly();
            if (executing != null)
            {
                var name = executing.GetName();
                if (name != null)
                {
                    UserAgent = $"{name.Name} / {name.Version}";
                }
            }
        }

        /// <summary>
        /// Get a package
        /// </summary>
        /// <param name="namespace">Namespace of the package</param>
        /// <param name="name">Name of the package</param>
        /// <returns><see cref="Package"/> if found</returns>
        public Package GetPackage(string @namespace, string name)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", this.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var request = client.GetAsync($"https://thunderstore.io/api/experimental/package/{@namespace}/{name}/");
            request.Wait();
            var result = request?.Result;
            if (request != null && result != null && request.IsCompletedSuccessfully)
            {
                if (result.IsSuccessStatusCode)
                {
                    var content = result.Content.ReadAsStringAsync();
                    content.Wait();
                    var result2 = content?.Result;
                    if (content != null && result2 != null && content.IsCompletedSuccessfully)
                    {
                        var json = JsonSerializer.Deserialize<Package>(result2);
                        if (!IsV1Deprecated && json != null)
                        {
                            var metrics = GetPackageMetrics(@namespace, name);
                            if (metrics != null)
                            {
                                // At the time of writing this code, the package endpoint returns rating score and downloads as -1. V1 Package Metrics endpoint doesn't

                                json.TotalDownloads = metrics.Downloads;
                                json.RatingScore = metrics.RatingScore;
                            }
                        }
                        return json;
                    }
                }
                else
                {
                    if (IsThunderstoreError(result))
                    {
                        if (IsPackageNotFound(result))
                        {
                            throw new ThunderstorePackageNotFoundException($"Thunderstore could not find a package with name '{name}' & namespace '{@namespace}'", @namespace, name, result);
                        }
                        else
                        {
                            throw new ThunderstoreErrorException("Thunderstore API has thrown an unexpected error!", result);
                        }
                    }
                    else
                    {
                        result.EnsureSuccessStatusCode();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get metrics of package
        /// </summary>
        /// <param name="namespace">Namespace of the package</param>
        /// <param name="name">Name of the package</param>
        /// <returns><see cref="V1PackageMetrics"/> if found</returns>
        public V1PackageMetrics GetPackageMetrics(string @namespace, string name)
        {
            if (IsV1Deprecated) return null;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", this.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var request = client.GetAsync($"https://thunderstore.io/api/v1/package-metrics/{@namespace}/{name}/");
            request.Wait();
            var result = request?.Result;
            if (request != null && result != null && request.IsCompletedSuccessfully)
            {
                if (result.IsSuccessStatusCode)
                {
                    var content = result.Content.ReadAsStringAsync();
                    content.Wait();
                    var result2 = content?.Result;
                    if (content != null && result2 != null && content.IsCompletedSuccessfully)
                    {
                        var json = JsonSerializer.Deserialize<V1PackageMetrics>(result2);
                        return json;
                    }
                }
                else
                {
                    if (IsThunderstoreError(result))
                    {
                        if (IsPackageNotFound(result))
                        {
                            throw new ThunderstorePackageNotFoundException($"Thunderstore could not find a package with name '{name}' & namespace '{@namespace}'", @namespace, name, result);
                        }
                        else
                        {
                            throw new ThunderstoreErrorException("Thunderstore API has thrown an unexpected error!", result);
                        }
                    }
                    else
                    {
                        result.EnsureSuccessStatusCode();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get a package of a specific version
        /// </summary>
        /// <param name="namespace">Namespace of the package</param>
        /// <param name="name">Name of the package</param>
        /// <param name="version">Version of the package</param>
        /// <returns><see cref="PackageVersion"/> if found</returns>
        public PackageVersion GetPackage(string @namespace, string name, string version)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", this.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var request = client.GetAsync($"https://thunderstore.io/api/experimental/package/{@namespace}/{name}/{version}");
            request.Wait();
            var result = request?.Result;
            if (request != null && result != null && request.IsCompletedSuccessfully)
            {
                if (result.IsSuccessStatusCode)
                {
                    var content = result.Content.ReadAsStringAsync();
                    content.Wait();
                    var result2 = content?.Result;
                    if (content != null && result2 != null && content.IsCompletedSuccessfully)
                    {
                        var json = JsonSerializer.Deserialize<PackageVersion>(result2);
                        return json;
                    }
                }
                else
                {
                    if (IsThunderstoreError(result))
                    {
                        if (IsPackageNotFound(result))
                        {
                            throw new ThunderstorePackageNotFoundException($"Thunderstore could not find a package with name '{name}', namespace '{@namespace}' & version '{version}'", @namespace, name, version, result);
                        }
                        else
                        {
                            throw new ThunderstoreErrorException("Thunderstore API has thrown an unexpected error!", result);
                        }
                    }
                    else
                    {
                        result.EnsureSuccessStatusCode();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Check if the current version of package is the latest version
        /// </summary>
        /// <param name="namespace">Namespace of the package</param>
        /// <param name="name">Name of the package</param>
        /// <param name="currentVersion">Current version of the package</param>
        /// <returns><see langword="true"/> if is latest, otherwise <see langword="false"/></returns>
        public bool IsLatestVersion(string @namespace, string name, string currentVersion)
        {
            if (SemVersion.TryParse(currentVersion, out var version))
            {
                return IsLatestVersion(@namespace, name, version);
            }
            return false;
        }

        /// <summary>
        /// Check if the current version of package is the latest version
        /// </summary>
        /// <param name="namespace">Namespace of the package</param>
        /// <param name="name">Name of the package</param>
        /// <param name="currentVersion">Current version of the package</param>
        /// <returns><see langword="true"/> if is latest, otherwise <see langword="false"/></returns>
        public bool IsLatestVersion(string @namespace, string name, Version currentVersion)
        {
            return IsLatestVersion(@namespace, name, new SemVersion(currentVersion));
        }

        /// <summary>
        /// Check if the current version of package is the latest version
        /// </summary>
        /// <param name="namespace">Namespace of the package</param>
        /// <param name="name">Name of the package</param>
        /// <param name="currentVersion">Current version of the package</param>
        /// <returns><see langword="true"/> if is latest, otherwise <see langword="false"/></returns>
        public bool IsLatestVersion(string @namespace, string name, SemVersion currentVersion)
        {
            if (!IsV1Deprecated)
            {
                var package = GetPackageMetrics(@namespace, name);
                if (package == null) return false;
                return package.IsLatestVersion(currentVersion);
            }
            else
            {
                var package = GetPackage(@namespace, name);
                if (package == null) return false;
                return package.IsLatestVersion(currentVersion);
            }
        }

        private static bool IsPackageNotFound(HttpResponseMessage response)
        {
            const string detect = "Not found.";
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                return false;
            }
            else
            {
                var @string = response.Content.ReadAsStringAsync();
                @string.Wait();
                var _string = @string.Result;
                if (string.IsNullOrWhiteSpace(_string))
                {
                    return false;
                }
                else
                {
                    ThunderstoreErrorResponse error;
                    try
                    {
                        error = JsonSerializer.Deserialize<ThunderstoreErrorResponse>(_string);
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                    if (error != null)
                    {
                        return string.Equals(error.Details, detect, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return false;
        }

        private static bool IsThunderstoreError(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return false;
            }
            else
            {
                var @string = response.Content.ReadAsStringAsync();
                @string.Wait();
                var _string = @string.Result;
                if (string.IsNullOrWhiteSpace(_string))
                {
                    return false;
                }
                else
                {
                    ThunderstoreErrorResponse error;
                    try
                    {
                        error = JsonSerializer.Deserialize<ThunderstoreErrorResponse>(_string);
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                    if (error != null)
                    {
                        return !string.IsNullOrWhiteSpace(error.Details);
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Class that contains the data about a thunderstore package
    /// </summary>
    public class Package
    {
        /// <summary>
        /// Namespace of the package
        /// </summary>
        [JsonPropertyName("namespace")]
        [JsonInclude]
        public string Namespace { get; internal set; }

        /// <summary>
        /// Name of the package
        /// </summary>
        [JsonPropertyName("name")]
        [JsonInclude]
        public string Name { get; internal set; }

        /// <summary>
        /// Full name, which is name and namespace combined in the following format: "Namespace-Name"
        /// </summary>
        [JsonPropertyName("full_name")]
        [JsonInclude]
        public string FullName { get; internal set; }

        /// <summary>
        /// Owner of the package
        /// </summary>
        [JsonPropertyName("owner")]
        [JsonInclude]
        public string Owner { get; internal set; }

        /// <summary>
        /// URL to the package
        /// </summary>
        [JsonPropertyName("package_url")]
        [JsonInclude]
        public string PackageUrl { get; internal set; }

        /// <summary>
        /// <see cref="DateTime"/> at which the package was created
        /// </summary>
        [JsonPropertyName("date_created")]
        [JsonInclude]
        public DateTime CreatedAt { get; internal set; }

        /// <summary>
        /// <see cref="DateTime"/> at which the package was last updated
        /// </summary>
        [JsonPropertyName("date_updated")]
        [JsonInclude]
        public DateTime UpdatedAt { get; internal set; }

        /// <summary>
        /// Rating score of the package, as of now the number is not updated correctly
        /// </summary>
        [JsonPropertyName("rating_score")]
        [JsonInclude]
        public int RatingScore { get; internal set; }

        /// <summary>
        /// Is the mod pinned
        /// </summary>
        [JsonPropertyName("is_pinned")]
        [JsonInclude]
        public bool IsPinned { get; internal set; }

        /// <summary>
        /// Is the mod deprecated
        /// </summary>
        [JsonPropertyName("is_deprecated")]
        [JsonInclude]
        public bool IsDeprecated { get; internal set; }

        /// <summary>
        /// Total downloads of all the versions of this package
        /// </summary>
        [JsonPropertyName("total_downloads")]
        [JsonInclude]
        public int TotalDownloads { get; internal set; }

        /// <summary>
        /// Latest <see cref="PackageVersion"/> of the package
        /// </summary>
        [JsonPropertyName("latest")]
        [JsonInclude]
        public PackageVersion Latest { get; internal set; }

        /// <summary>
        /// Array of all listings of the package
        /// </summary>
        [JsonPropertyName("community_listings")]
        [JsonInclude]
        public PackageListing[] CommunityListings { get; internal set; }

        /// <summary>
        /// Check if the current version is the latest
        /// </summary>
        /// <param name="current">The current version of the package</param>
        /// <returns>Boolean value indicating whether or not the current version is the latest</returns>
        public bool IsLatestVersion(string current)
        {
            if (string.IsNullOrWhiteSpace(current)) return false;
            if (this.Latest == null || this.Latest.SemVersion == null) return false;
            if (SemVersion.TryParse(current, out var version))
            {
                return version >= this.Latest.SemVersion;
            }
            return false;
        }

        /// <summary>
        /// Check if the current version is the latest
        /// </summary>
        /// <param name="current">The current version of the package</param>
        /// <returns>Boolean value indicating whether or not the current version is the latest</returns>
        public bool IsLatestVersion(SemVersion current)
        {
            if (current == null) return false;
            if (this.Latest == null || this.Latest.SemVersion == null) return false;
            return current >= this.Latest.SemVersion;
        }

        /// <summary>
        /// Check if the current version is the latest
        /// </summary>
        /// <param name="current">The current version of the package</param>
        /// <returns>Boolean value indicating whether or not the current version is the latest</returns>
        public bool IsLatestVersion(Version current)
        {
            if (current == null) return false;
            if (this.Latest == null || this.Latest.SemVersion == null) return false;
            return new SemVersion(current) >= this.Latest.SemVersion;
        }
    }

    /// <summary>
    /// Class that contains data about a specific version of a <see cref="Package"/>
    /// </summary>
    public class PackageVersion
    {
        /// <summary>
        /// Namespace of the package
        /// </summary>
        [JsonPropertyName("namespace")]
        [JsonInclude]
        public string Namespace { get; internal set; }

        /// <summary>
        /// Name of the package
        /// </summary>
        [JsonPropertyName("name")]
        [JsonInclude]
        public string Name { get; internal set; }

        /// <summary>
        /// Version of the package
        /// </summary>
        [JsonPropertyName("version_number")]
        [JsonInclude]
        public string Version
        { get { return SemVersion.ToString(); } internal set { SemVersion = Semver.SemVersion.Parse(value); } }

        /// <summary>
        /// <see cref="Semver.SemVersion"/> converted from <see cref="Version"/>
        /// </summary>
        [JsonIgnore]
        public SemVersion SemVersion { get; internal set; }

        /// <summary>
        /// Full name, which is name and namespace combined in the following format: "Namespace-Name-Version"
        /// </summary>
        [JsonPropertyName("full_name")]
        [JsonInclude]
        public string FullName { get; internal set; }

        /// <summary>
        /// Description of the package
        /// </summary>
        [JsonPropertyName("description")]
        [JsonInclude]
        public string Description { get; internal set; }

        /// <summary>
        /// URL of the CDN containing the icon of the package
        /// </summary>
        [JsonPropertyName("icon")]
        [JsonInclude]
        public string Icon { get; internal set; }

        /// <summary>
        /// List of the dependencies in the full name format "Namespace-Name-Version"
        /// </summary>
        [JsonPropertyName("dependencies")]
        [JsonInclude]
        public List<string> Dependencies { get; internal set; }

        /// <summary>
        /// URL that will allow to download that specific version of the package
        /// </summary>
        [JsonPropertyName("download_url")]
        [JsonInclude]
        public string DownloadUrl { get; internal set; }

        /// <summary>
        /// <see cref="DateTime"/> at which the package was created
        /// </summary>
        [JsonPropertyName("date_created")]
        [JsonInclude]
        public DateTime CreatedAt { get; internal set; }

        /// <summary>
        /// The amount of downloads this version of the package has
        /// </summary>
        [JsonPropertyName("downloads")]
        [JsonInclude]
        public int Downloads { get; internal set; }

        /// <summary>
        /// URL to the website the package has linked to
        /// </summary>
        [JsonPropertyName("website_url")]
        [JsonInclude]
        public string WebsiteURL { get; internal set; }

        /// <summary>
        /// Is this version active (not sure what this means)
        /// </summary>
        [JsonPropertyName("is_active")]
        [JsonInclude]
        public bool IsActive { get; internal set; }
    }

    /// <summary>
    /// Class that contains data about a listing of a <see cref="Package"/>
    /// </summary>
    public class PackageListing
    {
        /// <summary>
        /// Is the package marked to contain NSFW (Not Safe To Watch) content
        /// </summary>
        [JsonPropertyName("has_nsfw_content")]
        [JsonInclude]
        public bool HasNSFWContent { get; internal set; }

        /// <summary>
        /// The categories the package is in the following community
        /// </summary>
        [JsonPropertyName("categories")]
        [JsonInclude]
        public List<string> Categories { get; internal set; }

        /// <summary>
        /// The community the listing appears in
        /// </summary>
        [JsonPropertyName("community")]
        [JsonInclude]
        public string Community { get; internal set; }

        /// <summary>
        /// Review status of the package in the community
        /// </summary>
        [JsonPropertyName("review_status")]
        [JsonInclude]
        public string ReviewStatusString
        {
            get { return ReviewStatus.ToString(); }
            internal set
            {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                else
                {
                    if (string.Equals(value, "unreviewed", StringComparison.OrdinalIgnoreCase)) ReviewStatus = ReviewStatusEnum.UNREVIEWED;
                    else if (string.Equals(value, "approved", StringComparison.OrdinalIgnoreCase)) ReviewStatus = ReviewStatusEnum.APPROVED;
                    else if (string.Equals(value, "rejected", StringComparison.OrdinalIgnoreCase)) ReviewStatus = ReviewStatusEnum.REJECTED;
                }
            }
        }

        /// <summary>
        /// Review status of the package in the community
        /// </summary>
        [JsonIgnore]
        public ReviewStatusEnum ReviewStatus { get; internal set; }

        /// <summary>
        /// Review status of the package
        /// </summary>
        public enum ReviewStatusEnum
        {
            /// <summary>
            /// The package hasn't been reviewed yet
            /// </summary>
            UNREVIEWED,

            /// <summary>
            /// The package got approved by a moderator
            /// </summary>
            APPROVED,

            /// <summary>
            /// The package got rejected by a moderator
            /// </summary>
            REJECTED
        }
    }

    /// <summary>
    /// Class that contains data about the metrics of a package
    /// </summary>
    public class V1PackageMetrics
    {
        /// <summary>
        /// The amount of downloads it has
        /// </summary>
        [JsonPropertyName("downloads")]
        [JsonInclude]
        public int Downloads { get; internal set; }

        /// <summary>
        /// Rating score of the package (how many upvotes/likes does it have)
        /// </summary>
        [JsonPropertyName("rating_score")]
        [JsonInclude]
        public int RatingScore { get; internal set; }

        /// <summary>
        /// Latest version of the package
        /// </summary>
        [JsonPropertyName("latest_version")]
        [JsonInclude]
        public string LatestVersion
        { get { return LatestSemVersion.ToString(); } internal set { LatestSemVersion = Semver.SemVersion.Parse(value); } }

        /// <summary>
        /// <see cref="Semver.SemVersion"/> converted from <see cref="LatestVersion"/>
        /// </summary>
        [JsonIgnore]
        public SemVersion LatestSemVersion { get; internal set; }

        /// <summary>
        /// Check if the current version is the latest
        /// </summary>
        /// <param name="current">The current version of the package</param>
        /// <returns>Boolean value indicating whether or not the current version is the latest</returns>
        public bool IsLatestVersion(string current)
        {
            if (string.IsNullOrWhiteSpace(current)) return false;
            if (this.LatestSemVersion == null) return false;
            if (SemVersion.TryParse(current, out var version))
            {
                return version >= this.LatestSemVersion;
            }
            return false;
        }

        /// <summary>
        /// Check if the current version is the latest
        /// </summary>
        /// <param name="current">The current version of the package</param>
        /// <returns>Boolean value indicating whether or not the current version is the latest</returns>
        public bool IsLatestVersion(SemVersion current)
        {
            if (current == null) return false;
            if (this.LatestSemVersion == null) return false;
            return current >= this.LatestSemVersion;
        }

        /// <summary>
        /// Check if the current version is the latest
        /// </summary>
        /// <param name="current">The current version of the package</param>
        /// <returns>Boolean value indicating whether or not the current version is the latest</returns>
        public bool IsLatestVersion(Version current)
        {
            if (current == null) return false;
            if (this.LatestSemVersion == null) return false;
            return new SemVersion(current) >= this.LatestSemVersion;
        }
    }

    /// <summary>
    /// Class that contains data about an error received from thunderstore
    /// </summary>
    public class ThunderstoreErrorResponse
    {
        /// <summary>
        /// Details of the error
        /// </summary>
        [JsonPropertyName("detail")]
        [JsonInclude]
        public string Details { get; internal set; }
    }

    /// <summary>
    /// Exception thrown when received an error from Thunderstore
    /// </summary>
    public class ThunderstoreErrorException : Exception
    {
        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstoreErrorException() : base()
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstoreErrorException(string message) : base(message)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstoreErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstoreErrorException(string message, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, innerException)
        {
            Details = details;
            HttpStatusCode = httpStatusCode;
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/> from a <see cref="HttpResponseMessage"/>
        /// </summary>
        public ThunderstoreErrorException(string message, HttpResponseMessage response) : base(message)
        {
            if (!response.IsSuccessStatusCode)
            {
                HttpStatusCode = response.StatusCode;
                var @string = response.Content.ReadAsStringAsync();
                @string.Wait();
                var _string = @string.Result;
                if (string.IsNullOrWhiteSpace(_string))
                {
                    Details = string.Empty;
                }
                else
                {
                    ThunderstoreErrorResponse error;
                    try
                    {
                        error = JsonSerializer.Deserialize<ThunderstoreErrorResponse>(_string);
                    }
                    catch (JsonException)
                    {
                        Details = string.Empty;
                        return;
                    }
                    if (error != null)
                    {
                        Details = error.Details;
                    }
                }
            }
        }

        /// <summary>
        /// Details of the HTTP error
        /// </summary>
        public string Details { get; }

        /// <summary>
        /// Status code of the HTTP request
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; }
    }

    /// <summary>
    /// Exception thrown when a package was not found by Thunderstore
    /// </summary>
    public class ThunderstorePackageNotFoundException : ThunderstoreErrorException
    {
        /// <summary>
        /// Namespace that was provided in the initial request
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Namespace that was provided in the initial request
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Version that was provided in the initial request
        /// <para><see langword="null"/> if none was provided</para>
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, details, httpStatusCode, innerException)
        {
            Namespace = @namespace;
            Name = name;
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, string version, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, details, httpStatusCode, innerException)
        {
            Namespace = @namespace;
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, HttpResponseMessage response) : base(message, response)
        {
            Namespace = @namespace;
            Name = name;
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, string version, HttpResponseMessage response) : base(message, response)
        {
            Namespace = @namespace;
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException() : base()
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, details, httpStatusCode, innerException)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="ThunderstoreErrorException"/>
        /// </summary>
        public ThunderstorePackageNotFoundException(string message, HttpResponseMessage response) : base(message, response)
        {
        }
    }
}