using Sitecore.Pipelines.Upload;
using Stendahls.Sc.MediaIndexing.Extractors;

namespace Stendahls.Sc.MediaIndexing.Pipelines
{
    public class AddMetaDataOnUploadFiles
    {
        public void Process(UploadArgs args)
        {
            if (args == null)
                return;

            foreach (var item in args.UploadedItems)
            {
                // Should replace this with a pipeline or similar
                var contentExtractor = new PdfContentExtractor();
                contentExtractor.LoadItemWithMetaData(item);
            }
        }
    }
}