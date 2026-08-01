using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "Usage: Fotbiler.RuleGate.ApiSnapshot <assembly> <reference-list>");

    return 2;
}

try
{
    return Run(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        $"ERROR: {exception.Message}");

    return 1;
}

static int Run(string[] arguments)
{
    var assemblyPath =
        Path.GetFullPath(arguments[0]);

    var referenceListPath =
        Path.GetFullPath(arguments[1]);

    if (!File.Exists(assemblyPath))
    {
        Console.Error.WriteLine(
            $"Assembly not found: {assemblyPath}");

        return 3;
    }

    if (!File.Exists(referenceListPath))
    {
        Console.Error.WriteLine(
            $"Reference list not found: {referenceListPath}");

        return 4;
    }

    var referencePaths =
        File.ReadAllLines(referenceListPath)
            .Where(
                static value =>
                    !string.IsNullOrWhiteSpace(value))
            .Select(Path.GetFullPath)
            .Where(
                value =>
                    !string.Equals(
                        value,
                        assemblyPath,
                        StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(
                static value => value,
                StringComparer.Ordinal)
            .ToArray();

    foreach (var referencePath in referencePaths)
    {
        if (!File.Exists(referencePath))
        {
            Console.Error.WriteLine(
                $"Missing reference: {referencePath}");

            return 5;
        }
    }

    var references =
        referencePaths
            .Select(
                static value =>
                    MetadataReference.CreateFromFile(value))
            .Cast<MetadataReference>()
            .ToList();

    var targetReference =
        MetadataReference.CreateFromFile(
            assemblyPath);

    references.Add(targetReference);

    var compilation =
        CSharpCompilation.Create(
            "RuleGateApiSnapshotProbe",
            syntaxTrees: null,
            references: references,
            options:
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

    var assembly =
        compilation.GetAssemblyOrModuleSymbol(
            targetReference)
        as IAssemblySymbol;

    if (assembly is null)
    {
        Console.Error.WriteLine(
            "Could not load target assembly.");

        return 6;
    }

    var format =
        SymbolDisplayFormat
            .CSharpErrorMessageFormat
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions
                    .EscapeKeywordIdentifiers
                | SymbolDisplayMiscellaneousOptions
                    .UseSpecialTypes
                | SymbolDisplayMiscellaneousOptions
                    .IncludeNullableReferenceTypeModifier);

    var lines =
        new SortedSet<string>(
            StringComparer.Ordinal);

    lines.Add(
        $"ASSEMBLY | {assembly.Identity.Name}");

    VisitNamespace(
        assembly.GlobalNamespace);

    foreach (var line in lines)
    {
        Console.WriteLine(line);
    }

    return 0;

    void VisitNamespace(
        INamespaceSymbol namespaceSymbol)
    {
        foreach (
            var child
            in namespaceSymbol.GetNamespaceMembers())
        {
            VisitNamespace(child);
        }

        foreach (
            var type
            in namespaceSymbol.GetTypeMembers())
        {
            VisitType(type);
        }
    }

    void VisitType(
        INamedTypeSymbol type)
    {
        if (!IsVisible(type))
        {
            return;
        }

        CheckTypeAtom(
            type,
            $"public type {type.MetadataName}");

        if (type.BaseType is not null)
        {
            CheckSignatureType(
                type.BaseType,
                $"base type of {type.MetadataName}");
        }

        foreach (
            var interfaceType
            in type.Interfaces)
        {
            CheckSignatureType(
                interfaceType,
                $"interface of {type.MetadataName}");
        }

        CheckConstraints(
            type.TypeParameters,
            $"type {type.MetadataName}");

        CheckAttributes(
            type.GetAttributes(),
            $"type {type.MetadataName}");

        AddSymbol(
            "TYPE",
            type);

        AddTypeShape(type);

        AddAttributes(
            "TYPE-ATTR",
            type,
            type.GetAttributes());

        foreach (
            var member
            in type.GetMembers())
        {
            if (
                member is INamedTypeSymbol nested)
            {
                VisitType(nested);
                continue;
            }

            if (!IsVisible(member))
            {
                continue;
            }

            if (
                member is IMethodSymbol associated
                && associated.AssociatedSymbol is not null)
            {
                continue;
            }

            CheckMember(member);

            AddSymbol(
                member.Kind
                    .ToString()
                    .ToUpperInvariant(),
                member);

            AddAttributes(
                "MEMBER-ATTR",
                member,
                member.GetAttributes());

            switch (member)
            {
                case IMethodSymbol method:
                    AddMethodShape(method);

                    AddAttributes(
                        "RETURN-ATTR",
                        method,
                        method.GetReturnTypeAttributes());

                    AddParameters(
                        method,
                        method.Parameters);

                    break;

                case IPropertySymbol property:
                    AddPropertyShape(property);

                    AddParameters(
                        property,
                        property.Parameters);

                    break;

                case IEventSymbol eventSymbol:
                    AddEventShape(eventSymbol);
                    break;

                case IFieldSymbol field:
                    AddFieldShape(field);
                    break;
            }
        }
    }

    void CheckMember(
        ISymbol member)
    {
        var context =
            member.ToDisplayString();

        switch (member)
        {
            case IMethodSymbol method:
                CheckSignatureType(
                    method.ReturnType,
                    context);

                foreach (
                    var parameter
                    in method.Parameters)
                {
                    CheckSignatureType(
                        parameter.Type,
                        context);

                    CheckAttributes(
                        parameter.GetAttributes(),
                        context);
                }

                CheckConstraints(
                    method.TypeParameters,
                    context);

                CheckAttributes(
                    method.GetReturnTypeAttributes(),
                    context);

                break;

            case IPropertySymbol property:
                CheckSignatureType(
                    property.Type,
                    context);

                foreach (
                    var parameter
                    in property.Parameters)
                {
                    CheckSignatureType(
                        parameter.Type,
                        context);

                    CheckAttributes(
                        parameter.GetAttributes(),
                        context);
                }

                break;

            case IFieldSymbol field:
                CheckSignatureType(
                    field.Type,
                    context);
                break;

            case IEventSymbol eventSymbol:
                CheckSignatureType(
                    eventSymbol.Type,
                    context);
                break;
        }

        CheckAttributes(
            member.GetAttributes(),
            context);
    }

    void CheckConstraints(
        ImmutableArray<ITypeParameterSymbol> parameters,
        string context)
    {
        foreach (
            var parameter
            in parameters)
        {
            foreach (
                var constraint
                in parameter.ConstraintTypes)
            {
                CheckSignatureType(
                    constraint,
                    context);
            }
        }
    }

    void CheckSignatureType(
        ITypeSymbol type,
        string context)
    {
        CheckTypeAtom(
            type,
            context);

        switch (type)
        {
            case IArrayTypeSymbol array:
                CheckSignatureType(
                    array.ElementType,
                    context);
                break;

            case IPointerTypeSymbol pointer:
                CheckSignatureType(
                    pointer.PointedAtType,
                    context);
                break;

            case IFunctionPointerTypeSymbol function:
                CheckSignatureType(
                    function.Signature.ReturnType,
                    context);

                foreach (
                    var parameter
                    in function.Signature.Parameters)
                {
                    CheckSignatureType(
                        parameter.Type,
                        context);
                }

                break;

            case INamedTypeSymbol named:
                foreach (
                    var argument
                    in named.TypeArguments)
                {
                    CheckSignatureType(
                        argument,
                        context);
                }

                break;
        }
    }

    static void CheckTypeAtom(
        ITypeSymbol type,
        string context)
    {
        if (
            type is IErrorTypeSymbol
            || type.TypeKind == TypeKind.Error)
        {
            throw new InvalidOperationException(
                $"Unresolved API type in {context}: "
                + type.ToDisplayString());
        }
    }

    void CheckAttributes(
        ImmutableArray<AttributeData> attributes,
        string context)
    {
        foreach (
            var attribute
            in attributes)
        {
            if (
                attribute.AttributeClass
                is IErrorTypeSymbol)
            {
                throw new InvalidOperationException(
                    $"Unresolved attribute in {context}");
            }

            if (attribute.AttributeClass is not null)
            {
                CheckSignatureType(
                    attribute.AttributeClass,
                    context);
            }

            foreach (
                var argument
                in attribute.ConstructorArguments)
            {
                CheckTypedConstant(
                    argument,
                    context);
            }

            foreach (
                var argument
                in attribute.NamedArguments)
            {
                CheckTypedConstant(
                    argument.Value,
                    context);
            }
        }
    }

    void CheckTypedConstant(
        TypedConstant value,
        string context)
    {
        if (value.Type is not null)
        {
            CheckSignatureType(
                value.Type,
                context);
        }

        if (
            value.Kind
            == TypedConstantKind.Array)
        {
            foreach (
                var item
                in value.Values)
            {
                CheckTypedConstant(
                    item,
                    context);
            }
        }

        if (
            value.Kind
                == TypedConstantKind.Type
            && value.Value is ITypeSymbol type)
        {
            CheckSignatureType(
                type,
                context);
        }
    }

    static bool IsVisible(
        ISymbol symbol)
    {
        return symbol.DeclaredAccessibility
            is Accessibility.Public
            or Accessibility.Protected
            or Accessibility.ProtectedOrInternal;
    }

    void AddSymbol(
        string category,
        ISymbol symbol)
    {
        var modifiers =
            new List<string>();

        if (symbol.IsStatic)
        {
            modifiers.Add("static");
        }

        if (symbol.IsAbstract)
        {
            modifiers.Add("abstract");
        }

        if (symbol.IsVirtual)
        {
            modifiers.Add("virtual");
        }

        if (symbol.IsOverride)
        {
            modifiers.Add("override");
        }

        if (symbol.IsSealed)
        {
            modifiers.Add("sealed");
        }

        var modifierText =
            modifiers.Count == 0
                ? "-"
                : string.Join(
                    ",",
                    modifiers);

        lines.Add(
            $"{category} | "
            + $"{symbol.DeclaredAccessibility} | "
            + $"{modifierText} | "
            + $"{symbol.ToDisplayString(format)}");
    }

    void AddTypeShape(
        INamedTypeSymbol type)
    {
        var owner =
            type.ToDisplayString(format);

        lines.Add(
            $"TYPE-KIND | {owner} | "
            + $"{type.TypeKind} | "
            + $"record={type.IsRecord} | "
            + $"readonly={type.IsReadOnly} | "
            + $"reflike={type.IsRefLikeType}");

        if (
            type.BaseType is not null
            && type.SpecialType
                != SpecialType.System_Object)
        {
            lines.Add(
                $"TYPE-BASE | {owner} | "
                + type.BaseType
                    .ToDisplayString(format));
        }

        foreach (
            var interfaceType
            in type.Interfaces
                .OrderBy(
                    static value =>
                        value.ToDisplayString(),
                    StringComparer.Ordinal))
        {
            lines.Add(
                $"TYPE-INTERFACE | {owner} | "
                + interfaceType
                    .ToDisplayString(format));
        }

        if (
            type.TypeKind == TypeKind.Enum
            && type.EnumUnderlyingType is not null)
        {
            CheckSignatureType(
                type.EnumUnderlyingType,
                owner);

            lines.Add(
                $"ENUM-UNDERLYING | {owner} | "
                + type.EnumUnderlyingType
                    .ToDisplayString(format));
        }

        AddConstraints(
            owner,
            type.TypeParameters);
    }

    void AddMethodShape(
        IMethodSymbol method)
    {
        var owner =
            method.ToDisplayString(format);

        lines.Add(
            $"METHOD-SHAPE | {owner} | "
            + $"kind={method.MethodKind} | "
            + $"extension={method.IsExtensionMethod} | "
            + $"returnRef={method.RefKind}");

        AddConstraints(
            owner,
            method.TypeParameters);
    }

    void AddPropertyShape(
        IPropertySymbol property)
    {
        var owner =
            property.ToDisplayString(format);

        var getter =
            property.GetMethod is null
                ? "-"
                : property.GetMethod
                    .DeclaredAccessibility
                    .ToString();

        var setter =
            property.SetMethod is null
                ? "-"
                : property.SetMethod
                    .DeclaredAccessibility
                    .ToString();

        var initOnly =
            property.SetMethod?.IsInitOnly
            ?? false;

        lines.Add(
            $"PROPERTY-ACCESSORS | {owner} | "
            + $"get={getter} | "
            + $"set={setter} | "
            + $"init={initOnly}");
    }

    void AddEventShape(
        IEventSymbol eventSymbol)
    {
        var owner =
            eventSymbol.ToDisplayString(format);

        var add =
            eventSymbol.AddMethod is null
                ? "-"
                : eventSymbol.AddMethod
                    .DeclaredAccessibility
                    .ToString();

        var remove =
            eventSymbol.RemoveMethod is null
                ? "-"
                : eventSymbol.RemoveMethod
                    .DeclaredAccessibility
                    .ToString();

        lines.Add(
            $"EVENT-ACCESSORS | {owner} | "
            + $"add={add} | "
            + $"remove={remove}");
    }

    void AddFieldShape(
        IFieldSymbol field)
    {
        var owner =
            field.ToDisplayString(format);

        lines.Add(
            $"FIELD-SHAPE | {owner} | "
            + $"const={field.IsConst} | "
            + $"readonly={field.IsReadOnly} | "
            + $"volatile={field.IsVolatile}");

        if (field.HasConstantValue)
        {
            lines.Add(
                $"FIELD-VALUE | {owner} | "
                + RenderValue(
                    field.ConstantValue));
        }
    }

    void AddParameters(
        ISymbol ownerSymbol,
        ImmutableArray<IParameterSymbol> parameters)
    {
        var owner =
            ownerSymbol.ToDisplayString(format);

        for (
            var index = 0;
            index < parameters.Length;
            index++)
        {
            var parameter =
                parameters[index];

            lines.Add(
                $"PARAM | {owner} | "
                + $"{index} | "
                + $"{parameter.Name} | "
                + $"type={parameter.Type.ToDisplayString(format)} | "
                + $"ref={parameter.RefKind} | "
                + $"params={parameter.IsParams} | "
                + $"optional={parameter.IsOptional}");

            if (
                parameter.HasExplicitDefaultValue)
            {
                lines.Add(
                    $"PARAM-DEFAULT | {owner} | "
                    + $"{index} | "
                    + $"{parameter.Name} | "
                    + RenderValue(
                        parameter.ExplicitDefaultValue));
            }

            AddAttributes(
                $"PARAM[{index}]-ATTR",
                ownerSymbol,
                parameter.GetAttributes(),
                parameter.Name);
        }
    }

    void AddConstraints(
        string owner,
        ImmutableArray<ITypeParameterSymbol> parameters)
    {
        foreach (
            var parameter
            in parameters)
        {
            var constraints =
                new List<string>();

            if (
                parameter.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }

            if (
                parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            if (
                parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }

            if (
                parameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            foreach (
                var constraint
                in parameter.ConstraintTypes)
            {
                constraints.Add(
                    constraint
                        .ToDisplayString(format));
            }

            if (
                parameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count == 0)
            {
                continue;
            }

            lines.Add(
                $"TYPE-CONSTRAINT | {owner} | "
                + $"{parameter.Name} | "
                + string.Join(
                    ",",
                    constraints));
        }
    }

    void AddAttributes(
        string category,
        ISymbol owner,
        ImmutableArray<AttributeData> attributes,
        string? detail = null)
    {
        foreach (
            var attribute
            in attributes
                .Where(ShouldSnapshotAttribute)
                .Select(RenderAttribute)
                .OrderBy(
                    static value => value,
                    StringComparer.Ordinal))
        {
            var detailText =
                detail is null
                    ? string.Empty
                    : $" | {detail}";

            lines.Add(
                $"{category} | "
                + $"{owner.ToDisplayString(format)}"
                + detailText
                + $" | {attribute}");
        }
    }

    static bool ShouldSnapshotAttribute(
        AttributeData attribute)
    {
        var name =
            attribute.AttributeClass?
                .ToDisplayString(
                    SymbolDisplayFormat
                        .FullyQualifiedFormat)
                .Replace(
                    "global::",
                    string.Empty,
                    StringComparison.Ordinal);

        if (name is null)
        {
            return true;
        }

        return name
            is not
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute"
            and not
                "System.Runtime.CompilerServices.IteratorStateMachineAttribute"
            and not
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
            and not
                "System.Runtime.CompilerServices.NullableAttribute"
            and not
                "System.Runtime.CompilerServices.NullableContextAttribute"
            and not
                "System.Runtime.CompilerServices.RefSafetyRulesAttribute"
            and not
                "System.Runtime.CompilerServices.PreserveBaseOverridesAttribute";
    }

    string RenderAttribute(
        AttributeData attribute)
    {
        var attributeType =
            attribute.AttributeClass?
                .ToDisplayString(format)
            ?? "<unknown-attribute>";

        var constructorArguments =
            attribute.ConstructorArguments
                .Select(RenderTypedConstant);

        var namedArguments =
            attribute.NamedArguments
                .OrderBy(
                    static pair => pair.Key,
                    StringComparer.Ordinal)
                .Select(
                    pair =>
                        $"{pair.Key}="
                        + RenderTypedConstant(
                            pair.Value));

        return attributeType
            + "("
            + string.Join(
                ", ",
                constructorArguments
                    .Concat(namedArguments))
            + ")";
    }

    string RenderTypedConstant(
        TypedConstant value)
    {
        if (value.IsNull)
        {
            return "null";
        }

        if (
            value.Kind
            == TypedConstantKind.Array)
        {
            return "["
                + string.Join(
                    ", ",
                    value.Values.Select(
                        RenderTypedConstant))
                + "]";
        }

        if (
            value.Kind
            == TypedConstantKind.Type)
        {
            return value.Value
                is ITypeSymbol type
                    ? "typeof("
                        + type.ToDisplayString(format)
                        + ")"
                    : "typeof(?)";
        }

        return RenderValue(
            value.Value);
    }

    static string RenderValue(
        object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string text)
        {
            return "\""
                + text
                    .Replace(
                        "\\",
                        "\\\\",
                        StringComparison.Ordinal)
                    .Replace(
                        "\"",
                        "\\\"",
                        StringComparison.Ordinal)
                + "\"";
        }

        if (value is char character)
        {
            return "'"
                + character
                    .ToString()
                    .Replace(
                        "'",
                        "\\'",
                        StringComparison.Ordinal)
                + "'";
        }

        if (value is bool boolean)
        {
            return boolean
                ? "true"
                : "false";
        }

        return Convert.ToString(
                   value,
                   CultureInfo.InvariantCulture)
               ?? "null";
    }
}
