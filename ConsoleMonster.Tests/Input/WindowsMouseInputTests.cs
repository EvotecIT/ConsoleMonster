using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Interop;
using Xunit;

namespace ConsoleMonster.Tests.Input {
    /// <summary>Native record layout and stateful button translation without a real console handle.</summary>
    public sealed class WindowsMouseInputTests {
        [Fact]
        public void Translate_MouseAcrossReads_EmitsTransitionsAndKeepsKeyboardOrder() {
            char high = '\0';
            uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            Translate(new[] { Mouse(1), Mouse(1), Key('a') }, events, ref high, ref buttons);
            Translate(new[] { Mouse(1, flags: 1), Mouse(0), Key('\t') }, events, ref high, ref buttons);
            Assert.Equal(5, events.Count);
            AssertMouse(events[0], 1, true);
            Assert.True(events[1].IsChar('a'));
            Assert.Equal(MouseAction.Move, events[2].MouseAction);
            Assert.Equal(1, events[2].HeldButtons);
            AssertMouse(events[3], 1, false);
            Assert.True(events[4].Is(ConsoleKey.Tab));
            Assert.Equal(0u, buttons);
        }

        [Fact]
        public void Translate_MultipleButtons_MapsMiddleAndRightAndRetainsModifiers() {
            char high = '\0';
            uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            uint modifiers = ConsoleInputNative.ShiftPressed | ConsoleInputNative.RightAlt | ConsoleInputNative.LeftCtrl;
            Translate(new[] { Mouse(7, modifiers: modifiers), Mouse(0, modifiers: modifiers) }, events, ref high, ref buttons);
            Assert.Equal(6, events.Count);
            for (int i = 0; i < 3; i++) {
                AssertMouse(events[i], i + 1, true);
                AssertMouse(events[i + 3], i + 1, false);
            }
            Assert.All(events, e => Assert.Equal(KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control, e.Modifiers));
        }

        [Fact]
        public void Translate_ButtonChordsKeepTheFullHeldMaskOnEveryTransition() {
            char high = '\0'; uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            Translate(new[] { Mouse(1), Mouse(3), Mouse(1), Mouse(0) }, events, ref high, ref buttons);
            Assert.Equal(new[] { 1, 5, 1, 0 }, events.Select(input => input.HeldButtons));
            Assert.Equal(new[] { 1, 3, 3, 1 }, events.Select(input => input.MouseButton));
        }

        [Fact]
        public void Translate_DoubleClick_EmitsSecondPressAndRelease() {
            char high = '\0';
            uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            Translate(new[] { Mouse(1), Mouse(0), Mouse(1, flags: 2), Mouse(0) }, events, ref high, ref buttons);
            Assert.Equal(4, events.Count);
            for (int i = 0; i < events.Count; i++) AssertMouse(events[i], 1, i % 2 == 0);
        }

        [Fact]
        public void Translate_FocusLostBeforeRelease_DoesNotSuppressNextPress() {
            char high = '\0';
            uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            Translate(new[] { Mouse(1), new ConsoleInputRecord { EventType = ConsoleInputNative.FocusEvent, SetFocus = 0 } },
                events, ref high, ref buttons);
            Translate(new[] { new ConsoleInputRecord { EventType = ConsoleInputNative.FocusEvent, SetFocus = 1 }, Mouse(1) },
                events, ref high, ref buttons);
            Assert.Equal(4, events.Count);
            AssertMouse(events[0], 1, true);
            Assert.Equal(InputEventKind.FocusLost, events[1].Kind);
            Assert.Equal(InputEventKind.FocusGained, events[2].Kind);
            AssertMouse(events[3], 1, true);
        }

        [Fact]
        public void Translate_MotionAndWheel_AreNormalizedWithoutInventingClicks() {
            char high = '\0';
            uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            Translate(new[] {
                Mouse(1, flags: 1), Mouse(0, flags: 1), Mouse(0x00780000, flags: 4),
                Mouse(0xff880000, flags: 8), Mouse(8), Mouse(0),
                Mouse(1, column: -1), Mouse(0, row: -1), Key('x')
            }, events, ref high, ref buttons);
            Assert.Equal(5, events.Count);
            Assert.Equal(MouseAction.Move, events[0].MouseAction);
            Assert.Equal(1, events[0].HeldButtons);
            Assert.Equal(MouseAction.Move, events[1].MouseAction);
            Assert.Equal(0, events[1].HeldButtons);
            Assert.Equal(MouseAction.Wheel, events[2].MouseAction);
            Assert.Equal(1, events[2].WheelDelta);
            Assert.Equal(-1, events[3].HorizontalWheelDelta);
            Assert.True(events[4].IsChar('x'));
        }

        [Fact]
        public void Translate_MouseBetweenCharacterBursts_PreservesPasteBoundaries() {
            char high = '\0';
            uint buttons = 0;
            List<InputEvent> events = new List<InputEvent>();
            Translate(new[] { Key('a'), Key('b'), Key('c'), Mouse(1), Key('d'), Key('e'), Key('f'), Key('\r') },
                events, ref high, ref buttons);
            Assert.Equal(4, events.Count);
            Assert.Equal(InputEventKind.Paste, events[0].Kind);
            Assert.Equal("abc", events[0].Text);
            AssertMouse(events[1], 1, true);
            Assert.Equal(InputEventKind.Paste, events[2].Kind);
            Assert.Equal("def", events[2].Text);
            Assert.True(events[3].Is(ConsoleKey.Enter));
        }

        private static ConsoleInputRecord Mouse(uint buttons, uint flags = 0, uint modifiers = 0, short column = 12, short row = 4) {
            return new ConsoleInputRecord {
                EventType = ConsoleInputNative.MouseEvent, MouseX = column, MouseY = row,
                MouseButtonState = buttons, MouseControlKeyState = modifiers, MouseEventFlags = flags
            };
        }

        private static ConsoleInputRecord Key(char character) {
            return new ConsoleInputRecord {
                EventType = ConsoleInputNative.KeyEvent, KeyDown = 1, RepeatCount = 1,
                VirtualKeyCode = (ushort)InputEvent.KeyFor(character), UnicodeChar = character
            };
        }

        private static void Translate(ConsoleInputRecord[] records, List<InputEvent> output, ref char high, ref uint buttons) {
            WindowsConsoleInput.Translate(records, records.Length, output, ref high, ref buttons);
        }

        private static void AssertMouse(InputEvent actual, int button, bool pressed) {
            Assert.Equal(InputEventKind.Mouse, actual.Kind);
            Assert.Equal(12, actual.Column);
            Assert.Equal(4, actual.Row);
            Assert.Equal(button, actual.MouseButton);
            Assert.Equal(pressed, actual.MousePressed);
        }
    }
}
