using System.Globalization;
using System.Text;
using System.Xml.Linq;
using MarkMello.Application.Abstractions;
using MarkMello.Domain;
using MarkMello.Infrastructure.Diagrams;
using MarkMello.Presentation.Services;
using MarkMello.Presentation.Views.Markdown;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class SequenceDiagramLayoutTests(AvaloniaHeadlessFixture fixture)
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    [Fact]
    public Task Issue35LabelsFitTheCanvasAndParticipantBoxes()
        => fixture.Session.Dispatch(() =>
        {
            var svg = Render(ReadIssueSource());
            var canvasWidth = Canvas(svg)[2];
            var canvasHeight = Canvas(svg)[3];
            foreach (var text in svg.Elements(SvgNamespace + "text"))
            {
                var size = Measure(text);
                var x = Number(text, "x");
                var left = (string?)text.Attribute("text-anchor") == "middle" ? x - size.Width / 2 : x;
                Assert.True(left >= 0 && left + size.Width <= canvasWidth,
                    $"Label leaves the canvas: {text.Value}");
                Assert.InRange(Number(text, "y"), 0, canvasHeight - size.Height / 2);
            }

            var boxes = svg.Elements(SvgNamespace + "rect")
                .Where(static rect => (string?)rect.Attribute("fill") == "#ECECFF").ToList();
            Assert.Equal(2, boxes.Count);
            Assert.All(boxes, box => Assert.True(Number(box, "width") >= 240));
            var serverLabels = svg.Elements(SvgNamespace + "text")
                .Where(static text => text.Value == "Remote Server (Port 8080)").ToList();
            foreach (var label in serverLabels)
            {
                Assert.True(Measure(label).Width + 20 <= Number(boxes[0], "width"));
            }

            Assert.True(AotSafeSvgImage.TryLoad(Encoding.UTF8.GetBytes(svg.ToString()), out var image));
            Assert.Equal(canvasWidth, image.Size.Width, precision: 2);
        }, CancellationToken.None);

    [Fact]
    public Task InitControlsParticipantWidthAndMargin()
        => fixture.Session.Dispatch(() =>
        {
            var original = Render(ReadIssueSource());
            var wider = Render(ReadIssueSource()
                .Replace("\"width\": 240", "\"width\": 300", StringComparison.Ordinal)
                .Replace("\"actorMargin\": 120", "\"actorMargin\": 320", StringComparison.Ordinal));
            Assert.True(Canvas(wider)[2] > Canvas(original)[2]);
            Assert.All(wider.Elements(SvgNamespace + "rect")
                .Where(static rect => (string?)rect.Attribute("fill") == "#ECECFF"),
                box => Assert.True(Number(box, "width") >= 300));
        }, CancellationToken.None);

    [Fact]
    public Task NotesSpanBothParticipantsAndDoNotOverlapTheNextMessage()
        => fixture.Session.Dispatch(() =>
        {
            var svg = Render(ReadIssueSource());
            var lifelines = svg.Elements(SvgNamespace + "line")
                .Where(static line => (string?)line.Attribute("stroke") == "#999").ToList();
            var left = lifelines.Min(line => Number(line, "x1"));
            var right = lifelines.Max(line => Number(line, "x1"));
            var children = svg.Elements().ToList();
            var notes = children.Where(static element => element.Name.LocalName == "path"
                && (string?)element.Attribute("fill") == "#FFFFCC").ToList();
            Assert.Equal(3, notes.Count);
            foreach (var note in notes)
            {
                var noteIndex = children.IndexOf(note);
                var fold = children[noteIndex + 2];
                var noteLabel = children[noteIndex + 3];
                var noteRight = Number(fold, "x2");
                var noteLeft = Number(noteLabel, "x") * 2 - noteRight;
                Assert.True(noteLeft <= left && noteRight >= right);
                Assert.True(Measure(noteLabel).Width + 20 <= noteRight - noteLeft);
                var nextMessage = children.Skip(noteIndex + 4).First(element => element.Name.LocalName == "text");
                var noteBottom = Number(noteLabel, "y") + 20;
                Assert.True(Number(nextMessage, "y") - Measure(nextMessage).Height > noteBottom);
            }
        }, CancellationToken.None);

    [Theory]
    [InlineData("Note left of Client: 1. Connection Initialization")]
    [InlineData("Note right of Server: 1. Connection Initialization")]
    public Task NotesBesideOuterParticipantsStayInsideCanvas(string note)
        => fixture.Session.Dispatch(() =>
        {
            var svg = Render(ReadIssueSource().Replace(
                "Note over Client, Server: 1. Connection Initialization", note, StringComparison.Ordinal));
            var text = svg.Elements(SvgNamespace + "text").Single(element => element.Value == "1. Connection Initialization");
            var halfWidth = Measure(text).Width / 2;
            Assert.InRange(Number(text, "x") - halfWidth, 0, Canvas(svg)[2]);
            Assert.InRange(Number(text, "x") + halfWidth, 0, Canvas(svg)[2]);
        }, CancellationToken.None);

    [Fact]
    public Task MessageMarginChangesVerticalSpacing()
        => fixture.Session.Dispatch(() =>
        {
            var original = Render(ReadIssueSource());
            var spacious = Render(ReadIssueSource().Replace("\"messageMargin\": 45", "\"messageMargin\": 120", StringComparison.Ordinal));
            Assert.True(Canvas(spacious)[3] > Canvas(original)[3]);
        }, CancellationToken.None);

    [Fact]
    public Task SelfMessageRetainsItsLoopGeometryAndLabelFits()
        => fixture.Session.Dispatch(() =>
        {
            var svg = Render(ReadIssueSource().Replace("Client->>Server: Init Request", "Client->>Client: Init Request", StringComparison.Ordinal));
            var loop = svg.Elements(SvgNamespace + "path").Single(element => (string?)element.Attribute("stroke") == "#333");
            var points = loop.Attribute("d")!.Value.Split(' ').Select(point => point[1..].Split(',')
                .Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray()).ToArray();
            Assert.Equal(40, points[1][0] - points[0][0], precision: 2);
            Assert.Equal(30, points[2][1] - points[0][1], precision: 2);
            var label = svg.Elements(SvgNamespace + "text").Single(element => element.Value.Contains("Init Request", StringComparison.Ordinal));
            Assert.True(Number(label, "x") + Measure(label).Width <= Canvas(svg)[2]);
        }, CancellationToken.None);

    [Fact]
    public Task DefaultLightPaletteHasAnOpaqueCanvasInADarkShell()
        => fixture.Session.Dispatch(() =>
        {
            var svg = Render(ReadIssueSource());
            var background = svg.Elements().First();
            Assert.Equal("rect", background.Name.LocalName);
            Assert.Equal("white", (string?)background.Attribute("fill"));
            Assert.Equal(Canvas(svg)[2], Number(background, "width"));
            Assert.Equal(Canvas(svg)[3], Number(background, "height"));
        }, CancellationToken.None);

    private static XElement Render(string source)
    {
        var renderer = new MermaidDiagramRenderer(new AvaloniaDiagramTextMeasurer());
        var result = Assert.IsType<DiagramRenderResult.Success>(renderer.Render(new DiagramRenderRequest(source)));
        return XDocument.Parse(result.Svg).Root!;
    }

    private static string ReadIssueSource()
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "issue-35.md"))
            .Replace("```mermaid", string.Empty, StringComparison.Ordinal)
            .Replace("```", string.Empty, StringComparison.Ordinal).Trim();

    private static double Number(XElement element, string name)
        => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    private static double[] Canvas(XElement svg)
        => svg.Attribute("viewBox")!.Value.Split(' ').Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();

    private static DiagramTextSize Measure(XElement text)
        => new AvaloniaDiagramTextMeasurer().Measure(text.Value, text.Attribute("font-family")!.Value,
            double.Parse(text.Attribute("font-size")!.Value.Replace("px", string.Empty, StringComparison.Ordinal),
                CultureInfo.InvariantCulture));
}
