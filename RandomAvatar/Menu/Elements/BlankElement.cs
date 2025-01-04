using BoneLib.BoneMenu;

using UnityEngine;

namespace RandomAvatar.Menu.Elements
{
    /// <summary>
    /// Element that basically makes an blank space
    /// </summary>
    public class BlankElement
    {
        /// <summary>
        /// Element used to create the blank space
        /// </summary>
        public readonly FunctionElement Element;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlankElement"/> class.
        /// </summary>
        public BlankElement()
        {
            Element = new FunctionElement(string.Empty, Color.white, null);
            Element.SetProperty(ElementProperties.NoBorder);
        }
    }
}