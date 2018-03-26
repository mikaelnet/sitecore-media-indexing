using System;
using System.Collections.Generic;
using Sitecore.Data.Items;

namespace Stendahls.Sc.MediaIndexing.Extractors
{
    interface IContentExtractor
    {
        void LoadItemWithMetaData(Item item);
    }
}
