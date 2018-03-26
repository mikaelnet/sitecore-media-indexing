using Sitecore.Pipelines.Attach;
using Stendahls.Sc.MediaIndexing.Extractors;

namespace Stendahls.Sc.MediaIndexing.Pipelines
{
    public class AddMetaDataOnAttach
    {
        public void Process(AttachArgs args)
        {
            if (args?.MediaItem == null)
                return;

            var item = args.MediaItem.InnerItem;
            
            // Should replace this with a pipeline or similar
            var contentExtractor = new PdfContentExtractor();
            contentExtractor.LoadItemWithMetaData(item);
        }
    }
}