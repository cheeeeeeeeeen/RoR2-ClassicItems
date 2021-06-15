using Chen.ClassicItems.Artifacts;
using RoR2.ContentManagement;

namespace Chen.ClassicItems
{
    internal class ContentProvider : GenericContentPackProvider
    {
        protected override string ContentIdentifier() => ClassicItemsPlugin.ModGuid;

        protected override void LoadStaticContentAsyncActions(LoadStaticContentAsyncArgs args)
        {
            contentPack.bodyPrefabs.Add(Origin.bodyObjects.ToArray());
            contentPack.masterPrefabs.Add(Origin.masterObjects.ToArray());
        }
    }
}