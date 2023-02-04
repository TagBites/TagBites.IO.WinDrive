// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable IntroduceOptionalParameters.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

using System.Diagnostics.Contracts;

namespace JetBrains.Annotations;

/// <summary>
///     Indicates that the integral value never falls below zero.
/// </summary>
/// <example>
///     <code>
/// void Foo([NonNegativeValue] int value) {
///   if (value == -1) { // Warning: Expression is always 'false'
///     ...
///   }
/// }
/// </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property |
    AttributeTargets.Method | AttributeTargets.Delegate)]
internal sealed class NonNegativeValueAttribute : Attribute { }

/// <summary>
///     When applied to a target attribute, specifies a requirement for any type marked
///     with the target attribute to implement or inherit specific type or types.
/// </summary>
/// <example>
///     <code>
/// [BaseTypeRequired(typeof(IComponent)] // Specify requirement
/// class ComponentAttribute : Attribute { }
/// 
/// [Component] // ComponentAttribute requires implementing IComponent interface
/// class MyComponent : IComponent { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[BaseTypeRequired(typeof(Attribute))]
internal sealed class BaseTypeRequiredAttribute : Attribute
{
    public Type BaseType { get; }
    public BaseTypeRequiredAttribute(Type baseType)
    {
        BaseType = baseType;
    }
}

/// <summary>
///     Indicates that the marked symbol is used implicitly (e.g. via reflection, in external library),
///     so this symbol will be ignored by usage-checking inspections. <br />
///     You can use <see cref="ImplicitUseKindFlags" /> and <see cref="ImplicitUseTargetFlags" />
///     to configure how this attribute is applied.
/// </summary>
/// <example>
///     <code>
/// [UsedImplicitly]
/// public class TypeConverter {}
/// 
/// public class SummaryData
/// {
///   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
///   public SummaryData() {}
/// }
/// 
/// [UsedImplicitly(ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.Default)]
/// public interface IService {}
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.All)]
internal sealed class UsedImplicitlyAttribute : Attribute
{
    public ImplicitUseTargetFlags TargetFlags { get; }

    public ImplicitUseKindFlags UseKindFlags { get; }
    public UsedImplicitlyAttribute()
        : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

    public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
        : this(useKindFlags, ImplicitUseTargetFlags.Default) { }

    public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
        : this(ImplicitUseKindFlags.Default, targetFlags) { }

    public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
    {
        UseKindFlags = useKindFlags;
        TargetFlags = targetFlags;
    }
}

/// <summary>
///     Can be applied to attributes, type parameters, and parameters of a type assignable from <see cref="System.Type" /> .
///     When applied to an attribute, the decorated attribute behaves the same as <see cref="UsedImplicitlyAttribute" />.
///     When applied to a type parameter or to a parameter of type <see cref="System.Type" />,
///     indicates that the corresponding type is used implicitly.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.GenericParameter | AttributeTargets.Parameter)]
internal sealed class MeansImplicitUseAttribute : Attribute
{
    [UsedImplicitly]
    public ImplicitUseTargetFlags TargetFlags { get; }

    [UsedImplicitly]
    public ImplicitUseKindFlags UseKindFlags { get; }
    public MeansImplicitUseAttribute()
        : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

    public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags)
        : this(useKindFlags, ImplicitUseTargetFlags.Default) { }

    public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
        : this(ImplicitUseKindFlags.Default, targetFlags) { }

    public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
    {
        UseKindFlags = useKindFlags;
        TargetFlags = targetFlags;
    }
}

/// <summary>
///     Specifies the details of implicitly used symbol when it is marked
///     with <see cref="MeansImplicitUseAttribute" /> or <see cref="UsedImplicitlyAttribute" />.
/// </summary>
[Flags]
internal enum ImplicitUseKindFlags
{
    Default = Access | Assign | InstantiatedWithFixedConstructorSignature,
    /// <summary>Only entity marked with attribute considered used.</summary>
    Access = 1,
    /// <summary>Indicates implicit assignment to a member.</summary>
    Assign = 2,
    /// <summary>
    ///     Indicates implicit instantiation of a type with fixed constructor signature.
    ///     That means any unused constructor parameters won't be reported as such.
    /// </summary>
    InstantiatedWithFixedConstructorSignature = 4,
    /// <summary>Indicates implicit instantiation of a type.</summary>
    InstantiatedNoFixedConstructorSignature = 8
}

/// <summary>
///     Specifies what is considered to be used implicitly when marked
///     with <see cref="MeansImplicitUseAttribute" /> or <see cref="UsedImplicitlyAttribute" />.
/// </summary>
[Flags]
internal enum ImplicitUseTargetFlags
{
    Default = Itself,
    Itself = 1,
    /// <summary>Members of the type marked with the attribute are considered used.</summary>
    Members = 2,
    /// <summary> Inherited entities are considered used. </summary>
    WithInheritors = 4,
    /// <summary>Entity marked with the attribute and all its members considered used.</summary>
    WithMembers = Itself | Members
}

/// <summary>
///     This attribute is intended to mark publicly available API,
///     which should not be removed and so is treated as used.
/// </summary>
[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class PublicAPIAttribute : Attribute
{
    public string? Comment { get; }
    public PublicAPIAttribute() { }
    public PublicAPIAttribute(string comment)
    {
        Comment = comment;
    }
}

/// <summary>
///     Indicates that the return value of the method invocation must be used.
/// </summary>
/// <remarks>
///     Methods decorated with this attribute (in contrast to pure methods) might change state,
///     but make no sense without using their return value. <br />
///     Similarly to <see cref="PureAttribute" />, this attribute
///     will help to detect usages of the method when the return value is not used.
///     Optionally, you can specify a message to use when showing warnings, e.g.
///     <code>[MustUseReturnValue("Use the return value to...")]</code>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class MustUseReturnValueAttribute : Attribute
{
    public string? Justification { get; }
    public MustUseReturnValueAttribute() { }
    public MustUseReturnValueAttribute(string justification)
    {
        Justification = justification;
    }
}
