// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.YamlSerialization;

namespace Microsoft.DocAsCode.Build.RestApi.Swagger;

[Serializable]
public class ParameterObject
{
    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    public string Name { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
