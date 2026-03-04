namespace PluginEstructural
{
    public sealed partial class ThisApplication : Autodesk.Revit.UI.Macros.ApplicationEntryPoint
    {
        public event System.EventHandler Startup;

        public event System.EventHandler Shutdown;

        private void InternalStartup()
        {
            Startup += new System.EventHandler(Module_Startup);
            Shutdown += new System.EventHandler(Module_Shutdown);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private void OnStartup()
        {
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        protected override void FinishInitialization()
        {
            base.FinishInitialization();
            OnStartup();
            InternalStartup();
            if ((Startup != null))
            {
                Startup(this, System.EventArgs.Empty);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        protected override void OnShutdown()
        {
            if ((Shutdown != null))
            {
                Shutdown(this, System.EventArgs.Empty);
            }
            base.OnShutdown();
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        protected override string PrimaryCookie => "ThisApplication";
    }
}