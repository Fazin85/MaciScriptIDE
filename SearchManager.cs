using ICSharpCode.AvalonEdit;

public class SearchManager(TextEditor editor)
{
    private readonly TextEditor textEditor = editor;
    private string searchText = string.Empty;
    private readonly List<SearchResult> searchResults = [];
    private int currentResultIndex = -1;

    public int ResultCount => searchResults.Count;
    public int CurrentIndex => currentResultIndex >= 0 ? currentResultIndex + 1 : 0;
    public bool HasResults => searchResults.Count > 0;

    // Expose the current search text
    public string SearchText => searchText;

    public class SearchResult
    {
        public int StartOffset { get; set; }
        public int Length { get; set; }
    }

    public void ClearHighlighting()
    {
        textEditor.Select(textEditor.CaretOffset, 0);
    }

    public void Search(string text)
    {
        searchText = text;
        searchResults.Clear();
        currentResultIndex = -1;

        if (string.IsNullOrEmpty(searchText))
            return;

        string documentText = textEditor.Text;
        int index = 0;

        // Case-insensitive search
        StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        while ((index = documentText.IndexOf(searchText, index, comparison)) >= 0)
        {
            searchResults.Add(new SearchResult
            {
                StartOffset = index,
                Length = searchText.Length
            });

            index += searchText.Length;
        }

        // If we found results, select the first one
        if (searchResults.Count > 0)
            NavigateToResult(0);
    }

    public bool NavigateToNextResult()
    {
        if (searchResults.Count == 0)
            return false;

        int newIndex = currentResultIndex + 1;
        if (newIndex >= searchResults.Count)
            newIndex = 0; // Wrap around

        return NavigateToResult(newIndex);
    }

    public bool NavigateToPreviousResult()
    {
        if (searchResults.Count == 0)
            return false;

        int newIndex = currentResultIndex - 1;
        if (newIndex < 0)
            newIndex = searchResults.Count - 1; // Wrap around

        return NavigateToResult(newIndex);
    }

    private bool NavigateToResult(int index)
    {
        if (index < 0 || index >= searchResults.Count)
            return false;

        currentResultIndex = index;
        var result = searchResults[index];

        // Select the text in the editor
        textEditor.Select(result.StartOffset, result.Length);

        // Ensure the selection is visible
        textEditor.ScrollToLine(textEditor.Document.GetLineByOffset(result.StartOffset).LineNumber);

        return true;
    }

    public void ClearSearch()
    {
        searchResults.Clear();
        currentResultIndex = -1;
        searchText = string.Empty;
    }
}