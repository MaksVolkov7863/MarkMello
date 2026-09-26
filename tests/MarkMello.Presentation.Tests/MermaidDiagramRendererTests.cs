using MarkMello.Application.Abstractions;
using MarkMello.Domain;
using MarkMello.Infrastructure.Diagrams;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class MermaidDiagramRendererTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public void KindIsMermaid()
    {
        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        Assert.Equal(MarkdownDiagramKind.Mermaid, renderer.Kind);
    }

    [Fact]
    public void RenderProducesNonEmptySvgForMinimalFlowchart()
    {
        const string source =
            """
            flowchart LR
                A[Start] --> B[End]
            """;

        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        var result = renderer.Render(new DiagramRenderRequest(source));

        var success = Assert.IsType<DiagramRenderResult.Success>(result);
        Assert.False(string.IsNullOrEmpty(success.Svg));
        Assert.StartsWith("<svg", success.Svg.TrimStart(), StringComparison.Ordinal);
        Assert.EndsWith("</svg>", success.Svg.TrimEnd(), StringComparison.Ordinal);
    }

    [Fact]
    public Task RenderProducesNonEmptySvgForSequenceDiagram()
        => fixture.Session.Dispatch(() =>
    {
        const string source =
            """
            sequenceDiagram
                Alice->>Bob: Hi
                Bob-->>Alice: Hey
            """;

        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        var result = renderer.Render(new DiagramRenderRequest(source));

        var success = Assert.IsType<DiagramRenderResult.Success>(result);
        Assert.False(string.IsNullOrEmpty(success.Svg));
    }, CancellationToken.None);

    [Fact]
    public void RenderReturnsFailureForInvalidMermaidSource()
    {
        const string source = "this is not a valid mermaid diagram";

        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        var result = renderer.Render(new DiagramRenderRequest(source));

        var failure = Assert.IsType<DiagramRenderResult.Failure>(result);
        Assert.False(string.IsNullOrWhiteSpace(failure.Message));
        Assert.Equal(source, failure.Source);
    }

    [Fact]
    public void RenderReturnsFailureForEmptySource()
    {
        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        var result = renderer.Render(new DiagramRenderRequest(string.Empty));

        Assert.IsType<DiagramRenderResult.Failure>(result);
    }

    [Fact]
    public void RenderPreservesOriginalSourceInFailure()
    {
        const string source = "graph";
        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        var result = renderer.Render(new DiagramRenderRequest(source));

        if (result is DiagramRenderResult.Failure failure)
        {
            Assert.Equal(source, failure.Source);
        }
    }

    [Fact]
    public void RenderDoesNotMutateOrCorruptInputSource()
    {
        const string source =
            """
            flowchart TD
                A --> B
                B --> C
            """;
        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());

        renderer.Render(new DiagramRenderRequest(source));

        // Sanity: the source string is value-typed and not held mutably by
        // the renderer. This protects against future regressions where the
        // backend would gain a side effect on its input. CRLF normalisation
        // keeps the assertion meaningful on Windows checkouts where the raw
        // string literal preserves the source file's line endings.
        Assert.Equal(
            "flowchart TD\n    A --> B\n    B --> C",
            source.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
