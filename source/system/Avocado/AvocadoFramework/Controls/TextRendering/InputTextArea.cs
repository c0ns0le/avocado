﻿using System;
using System.Windows;
using System.Windows.Input;

namespace AvocadoFramework.Controls.TextRendering
{
    public abstract class InputTextArea : TextArea
    {
        protected bool IsAltKeyDown
            => isModifierKeyDown(ModifierKeys.Alt);

        protected bool IsControlKeyDown
            => isModifierKeyDown(ModifierKeys.Control);

        protected bool IsShiftKeyDown
            => isModifierKeyDown(ModifierKeys.Shift);

        bool isModifierKeyDown(ModifierKeys key)
            => Keyboard.Modifiers.HasFlag(key);

        protected bool InputEnabled { get; set; }
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            TextBase.Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // Ignore input if the InputEnabled flag is false.
            // The exception to this is system key handling (ex: Alt+F4).
            if (!InputEnabled && e.Key != Key.System)
            {
                e.Handled = true;
                return;
            }

            // Handle any special key actions.
            HandleSpecialKeys(e);

            // Handle all other keys.
            base.OnPreviewKeyDown(e);

            // Disallow other styling when pasting.
            if (IsControlKeyDown && e.Key == Key.V)
            {
                e.Handled = true;
                paste();
            }
        }

        protected virtual void HandleSpecialKeys(KeyEventArgs e)
        {
            // Base implementation is empty.
        }

        void paste()
        {
            // Change any paragraph breaks to linebreaks.
            var text = Clipboard.GetText(TextDataFormat.Text)
                .Replace(Environment.NewLine, "\r");
            Write(text, Foreground);
        }
    }
}
