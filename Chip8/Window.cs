﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Input;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Graphics.OpenGL;
using OpenToolkit.Mathematics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Chip8
{
    // These methods gets called by the vm.
    interface IWindow
    {
        public void Render();
        public void ProcessEvents();

        public void Beep();
    }

    class Window : GameWindow, IWindow
    {
        // https://stackoverflow.com/questions/3571627/show-hide-the-console-window-of-a-c-sharp-console-application
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        bool running, console, playingSound;

        private Dictionary<Key, byte> KeyboardMap = new Dictionary<Key, byte>
        {
            { Key.Number0, 0x0 },
            { Key.Number1, 0x1 },
            { Key.Number2, 0x2 },
            { Key.Number3, 0x3 },
            { Key.Number4, 0x4 },
            { Key.Number5, 0x5 },
            { Key.Number6, 0x6 },
            { Key.Number7, 0x7 },
            { Key.Number8, 0x8 },
            { Key.Number9, 0x9 },
            { Key.A, 0xA },
            { Key.B, 0xB },
            { Key.C, 0xC },
            { Key.D, 0xD },
            { Key.E, 0xE },
            { Key.F, 0xF }
        };

        private Vm vm;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            FileDrop += Window_FileDrop;
        }

        private void Window_FileDrop(FileDropEventArgs obj)
        {
            string rom = obj.FileNames[0];
            vm = Vm.NewVm(this, rom);

            running = true;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color.Black);
            GL.Color3(Color.White);
            GL.Ortho(0, 64, 32, 0, -1, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            SwapBuffers();

            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            
            if (KeyboardMap.TryGetValue(e.Key, out byte value))
            {
                vm?.KeyUp(value);
            }            
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (KeyboardMap.TryGetValue(e.Key, out byte value))
            {
                vm?.KeyDown(value);
                return;
            }  
            
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.G:
                    vm?.DebugGraphics();
                    break;
                case Key.M:
                    vm?.DebugMemory();
                    break;
                case Key.R:
                    vm?.DebugRegisters();
                    break;
                case Key.P:
                    running = !running;
                    break;
                case Key.S:
                    vm?.Step();
                    break;
                case Key.O:
                    ToggleConsole();
                    break;
                case Key.BackSpace:
                    vm?.Reset();
                    break;
                default:
                    break;
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (running)
            {
                vm?.EmulateCycle();
            }
        }

        // This method does not render the screen, it only updates the 60hz timers. Render() is called from the VM when screen has been updated.
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // TODO: Timers should be properly updated when single stepping.
            if (running)
            {
                vm?.UpdateTimers();
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        public void Render()
        {
            var buffer = vm?.GetScreenBuffer();
            if (buffer != null)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);
                
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        if (buffer[y * 64 + x] > 0)
                        {
                            GL.Rect(x, y, x + 1, y + 1);
                        }
                    }
                }
                SwapBuffers();
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();

            Environment.Exit(0);
        }

        private void ToggleConsole()
        {
            var handle = GetConsoleWindow();
            if (console)
            {
                ShowWindow(handle, SW_HIDE);
                console = false;
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
                console = true;
            }
        }

        public void Beep()
        {
            Task.Run(() =>
            {
                if (!playingSound)
                {
                    playingSound = true;
                    Console.Beep(440, 500);
                    playingSound = false;
                }
            });
        }
    }
}
