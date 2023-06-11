using Dalamud.Interface.Windowing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SubmarineTracker.Data;
using static SubmarineTracker.Utils;

namespace SubmarineTracker.Windows;

public class LootWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    private static ExcelSheet<Item> ItemSheet = null!;
    private static ExcelSheet<SubmarineExploration> ExplorationSheet = null!;

    private int SelectedSubmarine;
    private int SelectedVoyage;

    private static Vector2 IconSize = new(28, 28);

    public LootWindow(Plugin plugin, Configuration configuration) : base("Custom Loot Overview")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = configuration;

        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>()!;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var buttonHeight = ImGui.CalcTextSize("XXX").Y + 10.0f;
        if (ImGui.BeginChild("SubContent", new Vector2(0, -(buttonHeight + (30.0f * ImGuiHelpers.GlobalScale)))))
        {
            if (ImGui.BeginTabBar("##LootTabBar"))
            {
                CustomLootTab();

                VoyageTab();
            }
            ImGui.EndTabBar();
        }
        ImGui.EndChild();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            if (ImGui.Button("Settings"))
                Plugin.DrawConfigUI();
        }
        ImGui.EndChild();
    }

    private void CustomLootTab()
    {
        if (ImGui.BeginTabItem("Custom"))
        {
            if (!Configuration.CustomLootWithValue.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "No Custom Loot");
                ImGui.TextColored(ImGuiColors.ParsedOrange, "You can add selected items via the loot tab under settings.");

                ImGui.EndTabItem();
                return;
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            var numSubs = 0;
            var numVoyages = 0;
            var moneyMade = 0;
            var bigList = new Dictionary<Item, int>();
            foreach (var fc in Submarines.KnownSubmarines.Values)
            {
                fc.RebuildStats();
                var dateLimit = DateUtil.LimitToDate(Configuration.DateLimit);

                numSubs += fc.Submarines.Count;
                numVoyages += fc.SubLoot.Values.SelectMany(subLoot => subLoot.Loot.Where(loot => loot.Value.First().Date >= dateLimit)).Count();

                foreach (var (item, count) in fc.TimeLoot.Where(r => r.Key >= dateLimit).SelectMany(kv=>kv.Value))
                {
                    if (!Configuration.CustomLootWithValue.ContainsKey(item.RowId))
                        continue;

                    if(!bigList.ContainsKey(item)){
                        bigList.Add(item, count);
                    }
                    else
                    {
                        bigList[item] += count;
                    }
                }
            }

            if (!bigList.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, Configuration.DateLimit != DateLimit.None
                                                                ? "None of the selected items have been looted in the time frame."
                                                                : "None of the selected items have been looted yet.");
                ImGui.EndTabItem();
                return;
            }

            var textHeight = ImGui.CalcTextSize("XXXX").Y * 4.0f; // giving space for 4.0 lines
            if (ImGui.BeginChild("##customLootTableChild", new Vector2(0, -textHeight)))
            {
                if (ImGui.BeginTable($"##customLootTable", 3))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.15f);
                    ImGui.TableSetupColumn("##item");
                    ImGui.TableSetupColumn("##amount", 0, 0.3f);

                    foreach (var (item, count) in bigList)
                    {
                        ImGui.TableNextColumn();
                        DrawIcon(item.Icon);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(ToStr(item.Name));
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{count}");
                        ImGui.TableNextRow();

                        moneyMade += count * Configuration.CustomLootWithValue[item.RowId];
                    }
                }
                ImGui.EndTable();
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("##customLootTextChild", new Vector2(0, 0), false, 0))
            {
                var limit = Configuration.DateLimit != DateLimit.None
                                ? $"over {DateUtil.GetDateLimitName(Configuration.DateLimit)}"
                                : "";
                ImGui.TextWrapped($"The above rewards have been obtained {limit} from a total of {numVoyages} voyages via {numSubs} submarines.");
                ImGui.TextWrapped($"This made you a total of {moneyMade:N0} gil.");
            }
            ImGui.EndChild();

            ImGui.EndTabItem();
        }
    }

    private void VoyageTab()
    {
        if (ImGui.BeginTabItem("Voyage"))
        {
            var existingSubs = Submarines.KnownSubmarines.Values
                                         .SelectMany(fc => fc.Submarines.Select(s => $"{s.Name} ({s.Build.FullIdentifier()})"))
                                         .ToArray();
            if (!existingSubs.Any())
            {
                Helper.NoData();
                ImGui.EndTabItem();
                return;
            }

            var selectedSubmarine = SelectedSubmarine;
            ImGui.Combo("##existingSubs", ref selectedSubmarine, existingSubs, existingSubs.Length);
            if (selectedSubmarine != SelectedSubmarine)
            {
                SelectedSubmarine = selectedSubmarine;
                SelectedVoyage = 0;
            }

            var selectedSub = Submarines.KnownSubmarines.Values.SelectMany(fc => fc.Submarines).ToList()[SelectedSubmarine];

            var fc = Submarines.KnownSubmarines.Values.First(fcLoot => fcLoot.SubLoot.Values.Any(loot => loot.Loot.ContainsKey(selectedSub.Return)));
            var submarineLoot = fc.SubLoot.Values.First(loot => loot.Loot.ContainsKey(selectedSub.Return));

            var submarineVoyage = submarineLoot.Loot
                                               .SkipLast(1)
                                               .Select(kv => $"{kv.Value.First().Date}")
                                               .ToArray();
            if (!submarineVoyage.Any())
            {
                ImGui.TextColored(ImGuiColors.ParsedOrange, "Tracking starts when you send your subs on voyage again.");

                ImGui.EndTabItem();
                return;
            }

            ImGui.Combo("##voyageSelection", ref SelectedVoyage, submarineVoyage, submarineVoyage.Length);

            ImGuiHelpers.ScaledDummy(5.0f);

            var loot = submarineLoot.Loot.ToArray()[SelectedVoyage];
            var stats = loot.Value.First();
            if (stats.Valid)
                ImGui.TextUnformatted($"Rank: {stats.Rank} SRF: {stats.Surv}, {stats.Ret}, {stats.Fav}");
            else
                ImGui.TextColored(ImGuiColors.ParsedOrange, "-Legacy Data-");

            ImGuiHelpers.ScaledDummy(5.0f);

            foreach (var detailedLoot in loot.Value)
            {
                var primaryItem = ItemSheet.GetRow(detailedLoot.Primary)!;
                var additionalItem = ItemSheet.GetRow(detailedLoot.Additional)!;

                ImGui.TextUnformatted(UpperCaseStr(ExplorationSheet.GetRow(detailedLoot.Sector)!.Destination));
                if (ImGui.BeginTable($"##VoyageLootTable", 3))
                {
                    ImGui.TableSetupColumn("##icon", 0, 0.15f);
                    ImGui.TableSetupColumn("##item");
                    ImGui.TableSetupColumn("##amount", 0, 0.3f);

                    ImGui.TableNextColumn();
                    DrawIcon(primaryItem.Icon);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(ToStr(primaryItem.Name));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{detailedLoot.PrimaryCount}");
                    ImGui.TableNextRow();

                    if (detailedLoot.ValidAdditional)
                    {
                        ImGui.TableNextColumn();
                        DrawIcon(additionalItem.Icon);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(ToStr(additionalItem.Name));
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{detailedLoot.AdditionalCount}");
                        ImGui.TableNextRow();
                    }
                }
                ImGui.EndTable();

                if (stats.Valid)
                {
                    ImGui.TextUnformatted($"Favor Proc: {Loot.ProcToText(detailedLoot.FavProc)}");
                    ImGui.TextUnformatted($"Retrieval Proc: {Loot.ProcToText(detailedLoot.PrimaryRetProc)}");
                    ImGui.TextUnformatted($"Primary Surv Proc: {Loot.ProcToText(detailedLoot.PrimarySurvProc)}");

                    if (detailedLoot.ValidAdditional)
                        ImGui.TextUnformatted($"Additional Surveillance Proc: {Loot.ProcToText(detailedLoot.AdditionalSurvProc)}");
                }

                ImGuiHelpers.ScaledDummy(5.0f);
            }
            ImGui.EndTabItem();
        }
    }

    private static void DrawIcon(uint iconId)
    {
        var texture = TexturesCache.Instance!.GetTextureFromIconId(iconId);
        ImGui.Image(texture.ImGuiHandle, IconSize);
    }
}
