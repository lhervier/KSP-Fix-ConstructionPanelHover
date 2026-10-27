using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace com.github.lhervier.ksp.fix.constructionpanelhover
{
    /// <summary>
    /// Keeps a part held in EVA construction mode from staying red for good after it was hidden. The game hides the
    /// held part (deactivates it) while the cursor is over the construction panel or a part action window. Unity
    /// reports no end of contact for a deactivated collider, so on stock the contacts the part had when it was hidden
    /// stay listed, and the part stays red wherever it comes back. The held part's contacts are forgotten each time it
    /// is hidden or shown again; the physics engine reports again the contacts that are real once it is shown.
    /// </summary>
    internal static class HiddenPartContactsReset
    {
        private static readonly MethodInfo UpdateSelectedPartVisibility =
            AccessTools.Method(typeof(UIPartActionControllerInventory), "UpdateSelectedPartVisibility");
        private static readonly FieldInfo CargoCollidersField = AccessTools.Field(typeof(Part), "cargoColliders");

        /// <summary>Patches the visibility update of the held part.</summary>
        /// <exception cref="MissingMemberException">The game does not have the members patched.</exception>
        public static void Install(Harmony harmony)
        {
            if (UpdateSelectedPartVisibility == null || CargoCollidersField == null)
            {
                throw new MissingMemberException("UIPartActionControllerInventory.UpdateSelectedPartVisibility or " +
                    "Part.cargoColliders");
            }
            harmony.Patch(UpdateSelectedPartVisibility,
                prefix: new HarmonyMethod(typeof(HiddenPartContactsReset), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(HiddenPartContactsReset), nameof(Postfix)));
        }

        /// <summary>Tells the postfix whether the held part is visible before the game updates it.</summary>
        private static void Prefix(UIPartActionControllerInventory __instance, out bool __state)
        {
            Part held = __instance.CurrentCargoPart;
            __state = held != null && held.gameObject.activeInHierarchy;
        }

        /// <summary>Forgets the held part's contacts when the game has just hidden it or shown it again.</summary>
        private static void Postfix(UIPartActionControllerInventory __instance, bool __state)
        {
            Part held = __instance.CurrentCargoPart;
            if (held == null)
            {
                return;
            }
            bool visible = held.gameObject.activeInHierarchy;
            if (visible == __state)
            {
                return;
            }
            List<Collider> contacts = CargoCollidersField.GetValue(held) as List<Collider>;
            if (contacts == null || contacts.Count == 0)
            {
                return;
            }
            // Trigger events only come from a physics step, which runs after this frame's update: clearing now
            // cannot lose an entry PhysX reports for the part just shown.
            if (Log.IsDebugEnabled)
            {
                string names = string.Join(", ", contacts.ConvertAll(c => c == null ? "null" : c.name).ToArray());
                Log.Debug($"Held part {held.partInfo?.title} {(visible ? "shown" : "hidden")}, " +
                    $"{contacts.Count} contact(s) forgotten: {names}");
            }
            contacts.Clear();
        }
    }
}
