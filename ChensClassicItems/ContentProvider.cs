using Chen.ClassicItems.Artifacts;
using RoR2.ContentManagement;
using System.Collections;

namespace Chen.ClassicItems
{
    internal class ContentProvider : IContentPackProvider
    {
        internal ContentPack contentPack = new ContentPack();

        public string identifier => ClassicItemsPlugin.ModGuid;

        public void Initialize()
        {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            contentPack.identifier = identifier;
            contentPack.bodyPrefabs.Add(Origin.bodyObjects.ToArray());
            contentPack.masterPrefabs.Add(Origin.masterObjects.ToArray());

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}