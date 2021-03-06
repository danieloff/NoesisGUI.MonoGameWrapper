﻿namespace NoesisGUI.MonoGameWrapper
{
    using System;
    using System.Diagnostics;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Noesis;
    using NoesisGUI.MonoGameWrapper.Input;
    using NoesisGUI.MonoGameWrapper.Providers;

    /// <summary>
    /// Wrapper usage:
    /// 1. at game LoadContent() create wrapper instance
    /// 2. at game Update() invoke:
    /// - 2.1. wrapper.UpdateInput(gameTime)
    /// - 2.2. your game update (game logic)
    /// - 2.3. wrapper.Update(gameTime)
    /// 3. at game Draw() invoke:
    /// - 3.1. wrapper.PreRender(gameTime)
    /// - 3.2. clear graphics device (including stencil buffer)
    /// - 3.3. your game drawing code
    /// - 3.4. wrapper.Render()
    /// 4. at game UnloadContent() call wrapper.Dispose() method.
    /// Please be sure you have IsMouseVisible=true at the MonoGame Game class instance.
    /// </summary>
    public class NoesisWrapper : IDisposable
    {
        private readonly NoesisConfig config;

        private readonly GameWindow gameWindow;

        private readonly GraphicsDevice graphicsDevice;

        private InputManager inputManager;

        private Size lastSize;

        private NoesisProviderManager providerManager;

        private NoesisViewWrapper view;

        static NoesisWrapper()
        {
            // init NoesisGUI (called only once during the game lifetime)
            GUI.Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoesisWrapper" /> class.
        /// </summary>
        public NoesisWrapper(NoesisConfig config)
        {
            config.Validate();
            this.config = config;
            this.gameWindow = config.GameWindow;

            // setup Noesis Debug callbacks
            Log.SetLogCallback(this.NoesisLogCallbackHandler);

            this.graphicsDevice = config.Graphics.GraphicsDevice;
            this.providerManager = config.NoesisProviderManager;
            var provider = this.providerManager.Provider;
            GUI.SetFontProvider(provider.FontProvider);
            GUI.SetTextureProvider(provider.TextureProvider);
            GUI.SetXamlProvider(provider.XamlProvider);

            // setup theme
            if (config.ThemeXamlFilePath != null)
            {
                var themeResourceDictionary = (ResourceDictionary)GUI.LoadXaml(config.ThemeXamlFilePath);
                if (themeResourceDictionary == null)
                {
                    throw new Exception(
                        $"Theme is not found or was not able to load by NoesisGUI: {config.ThemeXamlFilePath}");
                }

                GUI.SetApplicationResources(themeResourceDictionary);
                this.Theme = themeResourceDictionary;
            }

            // create and prepare view
            var controlTreeRoot = (FrameworkElement)GUI.LoadXaml(config.RootXamlFilePath);
            this.ControlTreeRoot = controlTreeRoot
                                   ?? throw new Exception(
                                       $"UI file \"{config.RootXamlFilePath}\" is not found - cannot initialize UI");

            this.view = new NoesisViewWrapper(
                controlTreeRoot,
                this.graphicsDevice,
                this.config.CurrentTotalGameTime);
            this.RefreshSize();

            this.inputManager = this.view.CreateInputManager(config);

            // subscribe to MonoGame events
            this.EventsSubscribe();
        }

        /// <summary>
        /// Gets root element.
        /// </summary>
        public FrameworkElement ControlTreeRoot { get; private set; }

        /// <summary>
        /// Gets the input manager.
        /// </summary>
        public InputManager Input => this.inputManager;

        /// <summary>
        /// Gets resource dictionary of theme.
        /// </summary>
        public ResourceDictionary Theme { get; private set; }

        public NoesisViewWrapper View => this.view;

        public void Dispose()
        {
            this.Shutdown();
        }

        public void PreRender()
        {
            this.view.PreRender();
        }

        public void Render()
        {
            this.view.Render();
        }

        /// <summary>
        /// Updates NoesisGUI.
        /// </summary>
        /// <param name="gameTime">Current game time.</param>
        public void Update(GameTime gameTime)
        {
            this.RefreshSize();
            this.view.Update(gameTime);
        }

        /// <summary>
        /// Updates NoesisGUI input.
        /// </summary>
        /// <param name="gameTime">Current game time.</param>
        /// <param name="isWindowActive">Is game focused?</param>
        public void UpdateInput(GameTime gameTime, bool isWindowActive)
        {
            this.inputManager.Update(gameTime, isWindowActive);
        }

        private void DestroyRoot()
        {
            if (this.view == null)
            {
                // already destroyed
                return;
            }

            this.EventsUnsubscribe();

            this.view.Shutdown();
            this.view = null;
            var viewWeakRef = new WeakReference(this.view);
            this.view = null;
            this.inputManager = null;
            this.ControlTreeRoot = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // ensure the view is GC'ollected
            Debug.Assert(viewWeakRef.Target == null);
        }

        private void DeviceLostHandler(object sender, System.EventArgs eventArgs)
        {
            // TODO: restore this? not sure where it went in NoesisGUI 2.0
            //Noesis.GUI.DeviceLost();
        }

        private void DeviceResetHandler(object sender, System.EventArgs e)
        {
            // TODO: restore this? not sure where it went in NoesisGUI 2.0
            //Noesis.GUI.DeviceReset();
            this.RefreshSize();
        }

        private void EventsSubscribe()
        {
            this.graphicsDevice.DeviceReset += this.DeviceResetHandler;
            this.graphicsDevice.DeviceLost += this.DeviceLostHandler;
        }

        private void EventsUnsubscribe()
        {
            this.graphicsDevice.DeviceReset -= this.DeviceResetHandler;
            this.graphicsDevice.DeviceLost -= this.DeviceLostHandler;
        }

        private void NoesisLogCallbackHandler(LogLevel level, string channel, string message)
        {
            // NoesisGUI 2.1 doesn't have the exception callback anymore
            //this.config.OnExceptionThrown?.Invoke(exception);
            if (level == LogLevel.Error)
            {
                this.config.OnErrorMessageReceived?.Invoke(message);
            }
        }

        private void RefreshSize()
        {
            var viewport = this.graphicsDevice.Viewport;
            var size = new Size((uint)viewport.Width, (uint)viewport.Height);
            if (this.lastSize == size)
            {
                return;
            }

            this.lastSize = size;
            this.view.SetSize((ushort)viewport.Width, (ushort)viewport.Height);
        }

        private void Shutdown()
        {
            this.DestroyRoot();
            this.Theme = null;
            this.providerManager.Dispose();
            this.providerManager = null;
            GUI.UnregisterNativeTypes();
        }
    }
}