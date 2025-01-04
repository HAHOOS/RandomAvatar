using System;
using System.Threading;

using BoneLib.BoneMenu;

using UnityEngine;

namespace RandomAvatar.Menu.Elements
{
    /// <summary>
    /// An element for <see cref="BoneLib.BoneMenu"/> that makes it able to toggle functions easily
    /// </summary>
    public class ToggleFunctionElement
    {
        /// <summary>
        /// Main element
        /// </summary>
        public readonly FunctionElement Element;

        /// <summary>
        /// The callback that will be used when the element is toggled on and providing the current element
        /// </summary>
        public Action<ToggleFunctionElement> Callback;

        /// <summary>
        /// Color of the element when the function is turned off
        /// </summary>
        public Color OffColor;

        /// <summary>
        /// Color of the element when the function is turned on
        /// </summary>
        public Color OnColor;

        /// <summary>
        /// Gets a value indicating whether the function is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the function is currently running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Occurs when <see cref="Start()"/> gets successfully run.
        /// </summary>
        public event Action Started;

        /// <summary>
        /// Occurs when <see cref="Cancel()"/> gets successfully run.
        /// </summary>
        public event Action Cancelled;

        /// <summary>
        /// Executes the <see cref="Callback"/>
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Element.ElementColor = OnColor;
            Started?.Invoke();
            Callback?.Invoke(this);
        }

        /// <summary>
        /// Terminates the <see cref="Callback"/>
        /// </summary>
        public void Cancel()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Element.ElementColor = OffColor;
            Cancelled?.Invoke();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleFunctionElement"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="offColor">The color used when function is off.</param>
        /// <param name="onColor">The color used when function is on.</param>
        /// <param name="callback">The callback.</param>
        public ToggleFunctionElement(string name, Color offColor, Color onColor, Action<ToggleFunctionElement> callback)
        {
            Callback = callback;
            OffColor = offColor;
            OnColor = onColor;
            Element = new FunctionElement(name, offColor, () =>
            {
                if (IsRunning) Cancel();
                else Start();
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleFunctionElement"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="offColor">The color used when function is off.</param>
        /// <param name="callback">The callback.</param>
        public ToggleFunctionElement(string name, Color offColor, Action<ToggleFunctionElement> callback)
        {
            Callback = callback;
            OffColor = offColor;
            OnColor = Color.red;
            Element = new FunctionElement(name, offColor, () =>
            {
                if (IsRunning) Cancel();
                else Start();
            });
        }
    }
}