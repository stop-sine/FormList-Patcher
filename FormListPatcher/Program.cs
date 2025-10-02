using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Net;
using Mutagen.Bethesda.Fonts;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Exceptions;

namespace FormListPatcher
{
    public class Program
    {

        private static readonly List<ModKey> BethesdaModKeys = [
            new ModKey("Skyrim.esm", ModType.Master),
            new ModKey("Update.esm", ModType.Master),
            new ModKey("HearthFires.esm", ModType.Master),
            new ModKey("Dragonborn.esm", ModType.Master),
            new ModKey("ccasvsse001-almsivi.esm", ModType.Master),
            new ModKey("ccBGSSSE001-Fish.esm", ModType.Master),
            new ModKey("ccqdrsse001-survivalmode.esl ", ModType.Light),
            new ModKey("ccqdrsse002-firewood.esl", ModType.Light),
            new ModKey("ccbgssse018-shadowrend.esl", ModType.Light),
            new ModKey("ccedhsse001-norjewel.esl", ModType.Light),
            new ModKey("ccvsvsse002-pets.esl", ModType.Light),
            new ModKey("cceejsse003-hollow.esl", ModType.Light),
            new ModKey("ccbgssse016-umbra.esm", ModType.Master),
            new ModKey("ccbgssse040-advobgobs.esl", ModType.Light),
            new ModKey("ccedhsse002-splkntset.esl", ModType.Light),
            new ModKey("ccbgssse067-daedinv.esm ", ModType.Master),
            new ModKey("ccvsvsse003-necroarts.esl", ModType.Light),
            new ModKey("ccvsvsse004-beafarmer.esl", ModType.Light),
            new ModKey("ccbgssse025-advdsgs.esm", ModType.Master),
            new ModKey("ccrmssse001-necrohouse.esl", ModType.Light),
            new ModKey(" cceejsse004-hall.esl", ModType.Light),
            new ModKey("cceejsse005-cave.esm", ModType.Master),
            new ModKey("cckrtsse001_altar.esl", ModType.Light),
        ];


        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "FormListPatch.esp")
                .Run(args);
        }

        private static bool CompareFormLists(IFormListGetter flst1, IFormListGetter flst2)
        {
            if (flst1.Items.All(item => flst2.Items.Contains(item))) return true;
            return false;
        }

        private static bool FilterConflicts(List<IFormListGetter> formLists)
        {
            foreach (var formList in formLists.GetRange(0, formLists.Count - 1))
                if (!CompareFormLists(formList, formLists.Last())) return false;
            return true;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var cache = state.LinkCache;
            var patch = state.PatchMod;
            var records = state.LoadOrder.PriorityOrder.FormList().WinningOverrides().ToList();
            var patchRecords = new List<IFormListGetter>();

            Console.WriteLine($"Found {records.Count} FormList records");

            foreach (var record in records)
            {
                var overrides = record.FormKey.ToLinkGetter<IFormListGetter>().ResolveAll(cache).Reverse().ToList();
                if (FilterConflicts(overrides)) continue;
                patchRecords.Add(record);
            }

            Console.WriteLine($"Patching {patchRecords.Count} FormList records");

            foreach (var record in patchRecords)
            {
                var overrides = record.FormKey.ToLinkGetter<IFormListGetter>().ResolveAllSimpleContexts(cache).Reverse().ToList();
                //var modKeys = overrides.Select(flst => flst.Record.FormKey.ModKey).ToList();
                var formList = overrides.FindLast(context => BethesdaModKeys.Contains(context.ModKey));
                if (formList is null) continue;
                var flst = formList.Record.DeepCopy();
                for (var i = overrides.IndexOf(formList); i < overrides.Count; i++)
                {
                    if (overrides[i].Record.Equals(flst)) continue;
                    if (CompareFormLists(overrides[i].Record, flst)) continue;

                    for (var j = 0; j < overrides[i].Record.Items.Count; j++)
                    {
                        if (!flst.Items.Contains(overrides[i].Record.Items[j]))
                            if (j > 0 && flst.Items.Contains(overrides[i].Record.Items[j - 1]))
                                flst.Items.Insert(formList.Record.Items.IndexOf(overrides[i].Record.Items[j - 1]) + 1, overrides[i].Record.Items[j]);
                            else
                                flst.Items.Add(overrides[i].Record.Items[j]);
                    }
                }

                patch.FormLists.Add(flst);
            }

        }
    }
}
