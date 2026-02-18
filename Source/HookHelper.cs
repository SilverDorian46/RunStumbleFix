using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.RunStumbleFix;

public static class HookHelper
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    public const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    public static MethodInfo? GetOrigMethod(this Type type, string name, BindingFlags bindingAttr)
        => type.GetMethod($"orig_{name}", bindingAttr) ?? type.GetMethod(name, bindingAttr);

    public static void DisposeAndSetNull([MaybeNull] ref ILHook? hook)
    {
        hook?.Dispose();
        hook = null;
    }
}
