using System;

namespace OrigoDB.Core
{
    /// <summary>
    /// Explicitly disallow when proxying, invocation will throw an
    /// Exception if called through the proxy
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NoProxyAttribute : Attribute
    {
    }
}