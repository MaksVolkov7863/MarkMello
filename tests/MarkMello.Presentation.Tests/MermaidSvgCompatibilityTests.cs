using System.Text;
using MarkMello.Application.Abstractions;
using MarkMello.Domain;
using MarkMello.Infrastructure.Diagrams;
using MarkMello.Presentation.Views.Markdown;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class MermaidSvgCompatibilityTests(AvaloniaHeadlessFixture fixture)
{
    // --- Real Naiad output, end-to-end -----------------------------------
    // These tests render an actual Mermaid source through the production
    // MermaidDiagramRenderer (Naiad-backed) and assert that the resulting
    // SVG is consumable by the viewer's AOT-safe path. They are the
    // regression-protection that "M5 SVG compatibility" claims to deliver.

    [Fact]
    public async Task RealNaiadFlowchartOutputLoadsWithNodesAndArrows()
    {
        var svg = await RenderMermaid(
            """
            flowchart LR
                A[Start] --> B{Decide}
                B -->|yes| C[End]
                B -->|no| D[Retry]
            """);

        var loaded = AotSafeSvgImage.TryLoad(Encoding.UTF8.GetBytes(svg), out var image);

        Assert.True(loaded);
        Assert.True(image.CountDrawables(AotSafeSvgImageDrawableKind.Text) > 0,
            "Flowchart labels are emitted via <foreignObject>; the viewer must surface them as text drawables.");
        Assert.True(image.CountDrawables(AotSafeSvgImageDrawableKind.MarkerInstance) > 0,
            "Flowchart connectors use marker-end arrowheads; the viewer must instantiate them.");
        var labels = image.EnumerateTextContents();
        Assert.Contains(labels, label => label.Contains("Start", StringComparison.Ordinal));
        Assert.Contains(labels, label => label.Contains("End", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RealNaiadSequenceOutputLoadsWithParticipantLabels()
    {
        var svg = await RenderMermaid(
            """
            sequenceDiagram
                Alice->>Bob: Hi
                Bob-->>Alice: Hey
            """);

        var loaded = AotSafeSvgImage.TryLoad(Encoding.UTF8.GetBytes(svg), out var image);

        Assert.True(loaded);
        Assert.True(image.CountDrawables(AotSafeSvgImageDrawableKind.Text) >= 2,
            "Sequence diagrams use <text> for participant boxes; both Alice and Bob must appear as text drawables.");
        var labels = image.EnumerateTextContents();
        Assert.Contains("Alice", labels);
        Assert.Contains("Bob", labels);
    }

    [Fact]
    public async Task RealNaiadStateOutputLoadsAndStaysParseable()
    {
        var svg = await RenderMermaid(
            """
            stateDiagram-v2
                [*] --> Idle
                Idle --> Working: start
                Working --> Idle: finish
                Working --> [*]
            """);

        var loaded = AotSafeSvgImage.TryLoad(Encoding.UTF8.GetBytes(svg), out var image);

        Assert.True(loaded);
        Assert.True(image.DrawableCount > 0);
        var labels = image.EnumerateTextContents();
        Assert.Contains("Idle", labels);
        Assert.Contains("Working", labels);
    }

    [Fact]
    public async Task RealNaiadClassOutputLoads()
    {
        var svg = await RenderMermaid(
            """
            classDiagram
                class Animal {
                    +String name
                    +eat()
                }
                class Dog
                Animal <|-- Dog
            """);

        var loaded = AotSafeSvgImage.TryLoad(Encoding.UTF8.GetBytes(svg), out var image);

        Assert.True(loaded);
        Assert.True(image.DrawableCount > 0);
    }

    private async Task<string> RenderMermaid(string source)
    {
        var renderer = new MermaidDiagramRenderer(new MarkMello.Presentation.Services.AvaloniaDiagramTextMeasurer());
        var result = await fixture.Session.Dispatch(() => renderer.Render(new DiagramRenderRequest(source)), CancellationToken.None);
        var success = Assert.IsType<DiagramRenderResult.Success>(result);
        return success.Svg;
    }
}
