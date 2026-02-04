using System;

namespace Microsoft.VSDiagnostics;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class CPUUsageDiagnoserAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class DotNetObjectAllocDiagnoserAttribute : Attribute
{
}
