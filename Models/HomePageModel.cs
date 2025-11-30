// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Tools.Models;

public class ToolItem {
    public string Name { get; private init; } = string.Empty;
    public string Description { get; private init; } = string.Empty;
    public ObservableCollection<string> Features { get; private init; } = [];

    public static ToolItem Create(string name, string description, IEnumerable<string> features) {
        var formattedFeatures = features.Select(f => f.StartsWith("• ") ? f : "• " + f);

        return new ToolItem {
            Name = name,
            Description = description,
            Features = new ObservableCollection<string>(formattedFeatures)
        };
    }
}