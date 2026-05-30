using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class SpatialPageRegistry : MonoBehaviour
    {
        private readonly List<SpatialPage> pages = new();
        private readonly List<ISpatialTarget> targets = new();

        public IReadOnlyList<SpatialPage> Pages => pages;

        public IReadOnlyList<ISpatialTarget> Targets => targets;

        public void Initialize(IEnumerable<SpatialPage> spatialPages)
        {
            pages.Clear();
            targets.Clear();

            foreach (var page in spatialPages.Where(page => page != null))
            {
                pages.Add(page);
                targets.Add(page);
            }
        }
    }
}
