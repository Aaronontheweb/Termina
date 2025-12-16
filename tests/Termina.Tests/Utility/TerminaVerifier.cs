// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Termina.Tests.Utility;

/// <summary>
/// Test verifier for Termina analyzers.
/// Inspired by xunit.analyzers and Akka.Analyzers patterns.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public sealed class TerminaVerifier<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Creates a diagnostic result for the diagnostic referenced in the analyzer.
    /// </summary>
    public static DiagnosticResult Diagnostic()
    {
        return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();
    }

    /// <summary>
    /// Creates a diagnostic result for the specified diagnostic ID.
    /// </summary>
    public static DiagnosticResult Diagnostic(string id)
    {
        return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(id);
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostics for the given source.
    /// </summary>
    public static Task VerifyAnalyzer(string source, params DiagnosticResult[] diagnostics)
    {
        return VerifyAnalyzer([source], diagnostics);
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostics for the given sources.
    /// </summary>
    public static Task VerifyAnalyzer(string[] sources, params DiagnosticResult[] diagnostics)
    {
        var test = new TerminaTest();

        foreach (var source in sources)
            test.TestState.Sources.Add(source);

        test.ExpectedDiagnostics.AddRange(diagnostics);
        return test.RunAsync();
    }

    private sealed class TerminaTest : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        public TerminaTest()
        {
            // Use .NET 8.0 reference assemblies (stable, widely available)
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

            // Diagnostics are reported in both normal and generated code
            TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;

            // Tests should run independent of current system culture
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}
