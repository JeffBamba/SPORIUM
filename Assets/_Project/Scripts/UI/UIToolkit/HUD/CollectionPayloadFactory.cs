using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.Lab;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using Sporae.UI.UIToolkit.PlayerInventory;
using Sporae.Core.Localization;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Costruisce <see cref="NotificationPayload"/> per Collection box / toast con metadati reali da <see cref="Item"/>.
    /// </summary>
    public static class CollectionPayloadFactory
    {
        public const string MetaGenetic = "meta_genetic";
        public const string MetaMutatePct = "meta_mutate_pct";
        public const string MetaStage = "meta_stage";
        public const string MetaFamily = "meta_family";
        public const string MetaSource = "meta_source";
        public const string MetaQuality = "meta_quality";
        public const string MetaActive = "meta_active";
        public const string MetaPassive = "meta_passive";

        /// <summary>
        /// Payload per un item reale (es. spora appena creata dall'extractor) con metadati inventario.
        /// </summary>
        public static NotificationPayload FromItem(Item item, int quantity, string roomDisplayName)
        {
            var p = new NotificationPayload
            {
                ItemTypeId = item != null ? item.TypeId : string.Empty,
                ItemQuantity = quantity,
                ItemLocation = string.IsNullOrEmpty(roomDisplayName) ? "—" : roomDisplayName
            };

            if (item == null)
            {
                p.ItemName = "—";
                return p;
            }

            p.ItemName = BuildDisplayTitle(item);
            p.ItemSporeStage = item.SporeStageValue;
            p.ItemIcon = NotificationItemIconResolver.GetIcon(item.TypeId, p.ItemSporeStage);

            if (item.TypeId == Items.SporeGeneric)
                ApplySporeMetadata(p, item);
            else
                ApplyGenericItemMetadata(p, item);

            return p;
        }

        private static string BuildDisplayTitle(Item item)
        {
            if (item.TypeId == Items.SporeGeneric)
            {
                var plantLabel = ItemFabric.ResolveSourcePlantDisplayNameForUi(item);
                string baseName = ItemDisplayNameLocalization.GetSporeTitle(item.SporeStageValue);
                if (!string.IsNullOrWhiteSpace(plantLabel))
                    return baseName + " — " + plantLabel;
                return baseName;
            }

            return PlayerInventoryPanelController.GetItemDisplayName(item.TypeId, item);
        }

        private static void ApplySporeMetadata(NotificationPayload p, Item item)
        {
            p.Args[MetaGenetic] = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            p.Args[MetaMutatePct] = ExtractorTooltipTexts.GeneticTypeToPercentMutare(item.GeneticTypeValue);
            p.Args[MetaStage] = item.SporeStageValue.HasValue
                ? ItemDisplayNameLocalization.GetSporeStageSubLabel(item.SporeStageValue.Value)
                : "—";
            p.Args[MetaFamily] = string.IsNullOrWhiteSpace(item.FamilyMetadata) ? "—" : item.FamilyMetadata;
            var src = ItemFabric.ResolveSourcePlantDisplayNameForUi(item);
            p.Args[MetaSource] = string.IsNullOrWhiteSpace(src) ? "—" : src;
            p.Args[MetaQuality] = item.Quality.ToString("F1");
            p.Args[MetaActive] = string.IsNullOrWhiteSpace(item.ActivePowerLabel) ? "—" : item.ActivePowerLabel;
            p.Args[MetaPassive] = string.IsNullOrWhiteSpace(item.PassivePowerLabel) ? "—" : item.PassivePowerLabel;
        }

        private static void ApplyGenericItemMetadata(NotificationPayload p, Item item)
        {
            p.Args[MetaGenetic] = ExtractorTooltipTexts.GeneticTypeToTrattiLabel(item.GeneticTypeValue);
            p.Args[MetaMutatePct] = ExtractorTooltipTexts.GeneticTypeToPercentMutare(item.GeneticTypeValue);
            p.Args[MetaStage] = "—";
            p.Args[MetaFamily] = string.IsNullOrWhiteSpace(item.FamilyMetadata) ? "—" : item.FamilyMetadata;
            p.Args[MetaSource] = string.IsNullOrWhiteSpace(item.SourcePlantDisplayName)
                ? "—"
                : item.SourcePlantDisplayName;
            p.Args[MetaQuality] = item.Quality.ToString("F1");
            p.Args[MetaActive] = string.IsNullOrWhiteSpace(item.ActivePowerLabel) ? "—" : item.ActivePowerLabel;
            p.Args[MetaPassive] = string.IsNullOrWhiteSpace(item.PassivePowerLabel) ? "—" : item.PassivePowerLabel;
        }
    }
}
