using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class LayeredSearchPageContentData
    {
        public LayeredSearchPageContentData(
            string engineName,
            Color accentColor,
            string query,
            string resultSummary,
            string[] tabs,
            LayeredSearchResultEntryData[] results,
            string knowledgeTitle,
            string[] knowledgeLines,
            string[] relatedQueries)
        {
            EngineName = engineName;
            AccentColor = accentColor;
            Query = query;
            ResultSummary = resultSummary;
            Tabs = tabs;
            Results = results;
            KnowledgeTitle = knowledgeTitle;
            KnowledgeLines = knowledgeLines;
            RelatedQueries = relatedQueries;
        }

        public string EngineName { get; }

        public Color AccentColor { get; }

        public string Query { get; }

        public string ResultSummary { get; }

        public string[] Tabs { get; }

        public LayeredSearchResultEntryData[] Results { get; }

        public string KnowledgeTitle { get; }

        public string[] KnowledgeLines { get; }

        public string[] RelatedQueries { get; }
    }

    public readonly struct LayeredSearchResultEntryData
    {
        public LayeredSearchResultEntryData(string title, string displayUrl, string snippet)
        {
            Title = title;
            DisplayUrl = displayUrl;
            Snippet = snippet;
        }

        public string Title { get; }

        public string DisplayUrl { get; }

        public string Snippet { get; }
    }
}
