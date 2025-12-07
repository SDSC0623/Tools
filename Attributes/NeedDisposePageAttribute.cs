// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Tools.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NeedDisposePageAttribute(string disposeActionFuncName) : Attribute {
    public string DisposeAction => DisposeActionFuncName;

    private string DisposeActionFuncName { get; } = disposeActionFuncName;
}