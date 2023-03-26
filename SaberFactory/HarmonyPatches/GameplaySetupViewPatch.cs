﻿using System.Collections.Generic;
using HarmonyLib;
using HMUI;
using IPA.Utilities;

namespace SaberFactory.HarmonyPatches
{
    [HarmonyPatch(typeof(GameplaySetupViewController), "RefreshContent")]
    public class GameplaySetupViewPatch
    {
        private const string TabName = "Sabers(SF)";
        
        public static bool EntryEnabled;
        public static int SaberPanelIdx = 4;

        public static void Postfix(TextSegmentedControl ____selectionSegmentedControl)
        {
            if (!EntryEnabled)
            {
                return;
            }

            var texts = ____selectionSegmentedControl.GetField<IReadOnlyList<string>, TextSegmentedControl>("_texts");
            var list = new List<string>(texts) { TabName };
            SaberPanelIdx = list.Count - 1;
            ____selectionSegmentedControl.SetTexts(list);
        }
    }
}