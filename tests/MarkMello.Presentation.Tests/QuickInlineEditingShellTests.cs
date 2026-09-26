namespace MarkMello.Presentation.Tests;

public sealed partial class ShellViewModelTests
{
    [Fact]
    public async Task ApplyQuickDocumentEditInViewerModeUpdatesContentWithoutOpeningTwoPaneEditor()
    {
        var harness = CreateHarness();
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "quick_edit.md");
        harness.Loader.Sources[path] = CreateSource(path, "Hello original world");

        await harness.ViewModel.OpenPathAsync(path);

        Assert.False(harness.ViewModel.IsEditMode);
        Assert.False(harness.ViewModel.IsDirty);
        Assert.Same(harness.ViewModel, harness.ViewModel.ActiveDocumentContent);
        Assert.Equal("quick_edit.md", harness.ViewModel.TitleFileDisplayName);

        // Применяем быстрое редактирование инлайн в режиме просмотра
        harness.ViewModel.ApplyQuickDocumentEdit("Hello edited world");

        // Режим остаётся режимом просмотра (две панели НЕ открываются!)
        Assert.False(harness.ViewModel.IsEditMode);
        Assert.Same(harness.ViewModel, harness.ViewModel.ActiveDocumentContent);

        // Документ обновлён и помечен как изменённый
        Assert.True(harness.ViewModel.IsDirty);
        Assert.Equal("quick_edit.md •", harness.ViewModel.TitleFileDisplayName);
        Assert.Equal("Hello edited world", harness.ViewModel.Document?.Content);
        Assert.Equal("Hello edited world", harness.ViewModel.SourceText);
        Assert.True(harness.ViewModel.SaveCommand.CanExecute(null));

        // Вкладка также помечена dirty
        Assert.True(harness.ViewModel.OpenDocuments.ActiveTab?.IsDirty);

        // Сохранение документа
        await harness.ViewModel.SaveCommand.ExecuteAsync(null);

        // После сохранения статус dirty сброшен
        Assert.False(harness.ViewModel.IsDirty);
        Assert.Equal("quick_edit.md", harness.ViewModel.TitleFileDisplayName);
        Assert.Equal("Hello edited world", harness.DocumentSaver.Saved[path]);
    }

    [Fact]
    public async Task ApplyQuickDocumentEditThenToggleEditModeKeepsUpdatedText()
    {
        var harness = CreateHarness();
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", "quick_edit_switch.md");
        harness.Loader.Sources[path] = CreateSource(path, "Start text");

        await harness.ViewModel.OpenPathAsync(path);

        harness.ViewModel.ApplyQuickDocumentEdit("Updated text via quick edit");

        // Пользователь решает позже переключиться в полный двухпанельный режим
        await harness.ViewModel.ToggleEditModeCommand.ExecuteAsync(null);

        Assert.True(harness.ViewModel.IsEditMode);
        Assert.NotNull(harness.ViewModel.EditorSession);
        Assert.Equal("Updated text via quick edit", harness.ViewModel.EditorSession.SourceText);
        Assert.True(harness.ViewModel.IsDirty);
    }
}
