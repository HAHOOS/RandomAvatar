using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.Notifications;

using MelonLoader;

using System;

using UnityEngine;
using RandomAvatar.Menu.Elements;

namespace RandomAvatar.Helper
{
    internal static class BLHelper
    {
        #region BoneMenu (Code shared by @camobiwon on Discord)

        //You will need a PrefsCategory someone in your main class to reference here
        //Replace instances of "Main" with your desired class
        //Additionally you would need some method to save your preferences (Main.SavePreferences here)

        private readonly static MelonPreferences_Category prefs = Core.Preferences_Category;

        internal static IntElement CreateIntPref(this Page page, string name, Color color, ref MelonPreferences_Entry<int> value, int increment, int minValue, int maxValue, MelonPreferences_Category category = null, Action<int> callback = null, string prefName = null, int prefDefaultValue = default)
        {
            category ??= prefs;
            if (value == null || !category.HasEntry(value.Identifier))
            {
                prefName ??= name;

                if (!category.HasEntry(prefName))
                    value = category.CreateEntry(prefName, prefDefaultValue);
            }
            MelonPreferences_Entry<int> val = value;
            var element = page.CreateInt(name, color, val.Value, increment, minValue, maxValue, (x) =>
            {
                val.Value = x;
                category.SaveToFile(false);
                callback?.InvokeActionSafe(x);
            });
            if (!string.IsNullOrWhiteSpace(value.Description)) element.SetTooltip(value.Description);
            return element;
        }

        internal static FloatElement CreateFloatPref(this Page page, string name, Color color, ref MelonPreferences_Entry<float> value, float increment, float minValue, float maxValue, MelonPreferences_Category category = null, Action<float> callback = null, string prefName = null, float prefDefaultValue = default)
        {
            category ??= prefs;
            if (value == null || !category.HasEntry(value.Identifier))
            {
                prefName ??= name;

                if (!category.HasEntry(prefName))
                    value = category.CreateEntry(prefName, prefDefaultValue);
            }
            MelonPreferences_Entry<float> val = value;
            var element = page.CreateFloat(name, color, val.Value, increment, minValue, maxValue, (x) =>
            {
                val.Value = x;
                category.SaveToFile(false);
                callback?.InvokeActionSafe(x);
            });
            if (!string.IsNullOrWhiteSpace(value.Description)) element.SetTooltip(value.Description);
            return element;
        }

        internal static BoolElement CreateBoolPref(this Page page, string name, Color color, ref MelonPreferences_Entry<bool> value, MelonPreferences_Category category = null, Action<bool> callback = null, string prefName = null, bool prefDefaultValue = default)
        {
            category ??= prefs;
            if (value == null || !category.HasEntry(value.Identifier))
            {
                prefName ??= name;

                if (!category.HasEntry(prefName))
                    value = category.CreateEntry(prefName, prefDefaultValue);
            }
            MelonPreferences_Entry<bool> val = value;
            var element = page.CreateBool(name, color, val.Value, (x) =>
            {
                val.Value = x;
                category.SaveToFile(false);
                callback?.InvokeActionSafe(x);
            });
            if (!string.IsNullOrWhiteSpace(value.Description)) element.SetTooltip(value.Description);
            return element;
        }

        internal static EnumElement CreateEnumPref<T>(this Page page, string name, Color color, ref MelonPreferences_Entry<T> value, MelonPreferences_Category category = null, Action<Enum> callback = null, string prefName = null, Enum prefDefaultValue = default) where T : Enum
        {
            category ??= prefs;
            if (value == null || !category.HasEntry(value.Identifier))
            {
                prefName ??= name;

                if (!category.HasEntry(prefName))
                    value = category.CreateEntry(prefName, (T)prefDefaultValue);
            }
            MelonPreferences_Entry<T> val = value;
            var element = page.CreateEnum(name, color, val.Value, (x) =>
            {
                val.Value = (T)x;
                category.SaveToFile(false);
                callback?.InvokeActionSafe(x);
            });
            if (!string.IsNullOrWhiteSpace(value.Description)) element.SetTooltip(value.Description);
            return element;
        }

        internal static StringElement CreateStringPref(this Page page, string name, Color color, ref MelonPreferences_Entry<string> value, MelonPreferences_Category category = null, Action<string> callback = null, string prefName = null, string prefDefaultValue = default)
        {
            category ??= prefs;
            if (value == null || !category.HasEntry(value.Identifier))
            {
                prefName ??= name;

                if (!category.HasEntry(prefName))
                    value = category.CreateEntry(prefName, prefDefaultValue);
            }
            MelonPreferences_Entry<string> val = value;
            StringElement element = page.CreateString(name, color, val.Value, (x) =>
            {
                val.Value = x;
                category.SaveToFile(false);
                callback?.InvokeActionSafe(x);
            });
            element.Value = value.Value; //BoneMenu temp hack fix
            if (!string.IsNullOrWhiteSpace(value.Description)) element.SetTooltip(value.Description);
            return element;
        }

        /// <summary>
        /// Creates a label, which is just a function element set without borders
        /// </summary>
        /// <param name="page">Page to add the label to</param>
        /// <param name="text">Text for the label</param>
        /// <param name="color">Color of the text</param>
        /// <returns>A <see cref="FunctionElement"/> without borders</returns>
        public static FunctionElement CreateLabel(this Page page, string text, Color color)
        {
            var element = new FunctionElement(text, color, null);
            element.SetProperty(ElementProperties.NoBorder);
            page.Add(element);
            return element;
        }

        /// <summary>
        /// Creates the toggle function.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="name">The name.</param>
        /// <param name="offColor">The color used when function is off.</param>
        /// <param name="onColor">The color used when function is on.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The element that was added to the page</returns>
        public static ToggleFunctionElement CreateToggleFunction(this Page page, string name, Color offColor, Color onColor, Action<ToggleFunctionElement> callback)
        {
            var element = new ToggleFunctionElement(name, offColor, onColor, callback);
            page.Add(element.Element);
            return element;
        }

        /// <summary>
        /// Creates the toggle function.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="name">The name.</param>
        /// <param name="offColor">The color used when function is off.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The element that was added to the page</returns>
        public static ToggleFunctionElement CreateToggleFunction(this Page page, string name, Color offColor, Action<ToggleFunctionElement> callback)
        {
            var element = new ToggleFunctionElement(name, offColor, callback);
            page.Add(element.Element);
            return element;
        }

        /// <summary>
        /// Creates a blank space
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns><see cref="BlankElement"/> that creates the blank space</returns>
        public static BlankElement CreateBlank(this Page page)
        {
            var element = new BlankElement();
            page.Add(element.Element);
            return element;
        }

        #endregion BoneMenu (Code shared by @camobiwon on Discord)

        #region Notifications

        internal static void SendNotification(string title, string message, bool showTitleOnPopup = false, float popupLength = 2f, NotificationType type = NotificationType.Information, Texture2D customIcon = null)
        {
            Notifier.Send(new Notification()
            {
                Title = new NotificationText($"RandomAvatar | {title}", Color.white, true),
                Message = new NotificationText(message, Color.white, true),
                ShowTitleOnPopup = showTitleOnPopup,
                CustomIcon = customIcon,
                PopupLength = popupLength,
                Type = type
            });
        }

        internal static void SendNotification(NotificationText title, NotificationText message, bool showTitleOnPopup = false, float popupLength = 2f, NotificationType type = NotificationType.Information, Texture2D customIcon = null)
        {
            title.Text = $"RandomAvatar | {title.Text}";
            Notifier.Send(new Notification()
            {
                Title = title,
                Message = message,
                ShowTitleOnPopup = showTitleOnPopup,
                CustomIcon = customIcon,
                PopupLength = popupLength,
                Type = type
            });
        }

        internal static void SendNotification(Notification notification)
        {
            Notifier.Send(notification);
        }

        #endregion Notifications
    }
}