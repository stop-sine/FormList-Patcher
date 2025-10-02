using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Net;
using Mutagen.Bethesda.Fonts;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Exceptions;

namespace FormListPatcher
{
    public class Program
    {

        private static readonly List<ModKey> BethesdaModKeys = [
            new ModKey("Skyrim", ModType.Master),
            new ModKey("Update", ModType.Master),
            new ModKey("Dawnguard", ModType.Master),
            new ModKey("HearthFires", ModType.Master),
            new ModKey("Dragonborn", ModType.Master),
            new ModKey("ccasvsse001-almsivi", ModType.Master),
            new ModKey("ccBGSSSE001-Fish", ModType.Master),
            new ModKey("ccqdrsse001-survivalmode", ModType.Light),
            new ModKey("ccqdrsse002-firewood", ModType.Light),
            new ModKey("ccbgssse018-shadowrend", ModType.Light),
            new ModKey("ccedhsse001-norjewel", ModType.Light),
            new ModKey("ccvsvsse002-pets", ModType.Light),
            new ModKey("cceejsse003-hollow", ModType.Light),
            new ModKey("ccbgssse016-umbra", ModType.Master),
            new ModKey("ccbgssse040-advobgobs", ModType.Light),
            new ModKey("ccedhsse002-splkntset", ModType.Light),
            new ModKey("ccbgssse067-daedinv", ModType.Master),
            new ModKey("ccvsvsse003-necroarts", ModType.Light),
            new ModKey("ccvsvsse004-beafarmer", ModType.Light),
            new ModKey("ccbgssse025-advdsgs", ModType.Master),
            new ModKey("ccrmssse001-necrohouse", ModType.Light),
            new ModKey(" cceejsse004-hall", ModType.Light),
            new ModKey("cceejsse005-cave", ModType.Master),
            new ModKey("cckrtsse001_altar", ModType.Light),
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

        private static bool FilterConflicts(List<IModContext<IFormListGetter>> formLists)
        {
            //if (formLists.Count <= 2) return true;
            if (formLists.Select(context => context.ModKey).All(BethesdaModKeys.Contains)) return true;
            foreach (var formList in formLists.GetRange(0, formLists.Count - 1))
                if (!CompareFormLists(formList.Record, formLists.Last().Record)) return false;
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
                var overrides = record.FormKey.ToLinkGetter<IFormListGetter>().ResolveAllSimpleContexts(cache).Reverse().ToList();
                if (FilterConflicts(overrides)) continue;
                patchRecords.Add(record);
            }

            Console.WriteLine($"Patching {patchRecords.Count} FormList records");

            foreach (var record in patchRecords)
            {
                var overrides = record.FormKey.ToLinkGetter<IFormListGetter>().ResolveAllSimpleContexts(cache).Reverse().ToList();
                var formList = overrides.FindLast(context => BethesdaModKeys.Contains(context.ModKey)) ?? overrides.First();
                var flst = formList!.Record.DeepCopy();
                for (var i = overrides.IndexOf(formList!); i < overrides.Count; i++)
                {
                    if (overrides[i].Record.Equals(flst)) continue;
                    if (CompareFormLists(overrides[i].Record, flst))
                    {

                    }
                    overrides[i].Record.Items.Where(item => !flst.Items.Contains(item)).ForEach(flst.Items.Add);
                }
                if (flst.Equals(record)) continue;
                patch.FormLists.Add(flst);
            }
        }
    }
}
