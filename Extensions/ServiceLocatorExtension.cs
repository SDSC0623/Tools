// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

namespace Tools.Extensions;

[MarkupExtensionReturnType(typeof(object))]
public class ServiceLocatorExtension : MarkupExtension {
    public Type Type { get; set; } = null!;

    public override object ProvideValue(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(Type);
        return App.GetService(Type)!;
    }
}