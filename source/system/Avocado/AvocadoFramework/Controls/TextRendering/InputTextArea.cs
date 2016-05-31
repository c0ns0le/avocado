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

        protected bool InputEnabled
        {
            get { return inputEnabled; }
            set
            {
                TextBase.CaretBrush = value 
                    ? Config.CaretBrush 
                    : Config.DisabledCaretBrush;
                inputEnabled = value;
            }
        }

        bool inputEnabled = false;

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
        }

        protected virtual void HandleSpecialKeys(KeyEventArgs e)
        {
            switch (e.Key)
            {
                // Sanitize linebreak.
                case Key.Enter:
                    WriteLine();
                    e.Handled = true;
                    break;
                    
                // Disallow other styling when pasting.
                case Key.V:
                    if (!IsControlKeyDown) break;
                    var text = Clipboard.GetText(TextDataFormat.Text)
                        .Replace(Environment.NewLine, "\r");
                    Write(text, Foreground);
                    e.Handled = true;
                    break;
            }
        }
    }
}
