// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.RestApi.Swagger;

[Serializable]
public class ResponseObject
{
    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// Key is the mime type
    /// </summary>
    [YamlMember(Alias = "examples")]
    [JsonProperty("examples")]
    public Dictionary<string, object> Examples { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
