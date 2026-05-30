using System.Collections.Generic;

namespace ProjectGaze.Gaze
{
    public interface ILayeredPageContentProvider
    {
        LayeredSearchPageContentData GetContent(string pageId);
    }

    public sealed class MockLayeredPageContentProvider : ILayeredPageContentProvider
    {
        private readonly Dictionary<string, LayeredSearchPageContentData> pageMap =
            LayeredSearchPageMockContentLibrary.CreatePageMap();

        public LayeredSearchPageContentData GetContent(string pageId)
        {
            if (pageMap.TryGetValue(pageId, out var content))
            {
                return content;
            }

            return pageMap[LayeredPagesSceneDefaults.InitialMainPageId];
        }
    }
}
