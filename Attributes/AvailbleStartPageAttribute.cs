// ReSharper disable ConvertToPrimaryConstructor

using System.Diagnostics.CodeAnalysis;

namespace Tools.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AvailbleStartPageAttribute : Attribute {
    public int SortWeight => SortWeightValue;

    private int SortWeightValue { get; }

    public AvailbleStartPageAttribute(int sortWeight) => SortWeightValue = sortWeight;

    public override bool Equals([NotNullWhen(true)] object? obj) {
        return obj is AvailbleStartPageAttribute attribute && SortWeightValue == attribute.SortWeightValue;
    }

    public override int GetHashCode() {
        return SortWeight.GetHashCode();
    }
}