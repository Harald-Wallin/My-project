public readonly struct InteractionPresentation
{
    public InteractionCategory Category
    {
        get;
    }

    public string Text
    {
        get;
    }

    public string Status
    {
        get;
    }

    public InteractionPresentation(
        InteractionCategory category,
        string text,
        string status = "")
    {
        Category =
            category;

        Text =
            text ?? string.Empty;

        Status =
            status ?? string.Empty;
    }
}
